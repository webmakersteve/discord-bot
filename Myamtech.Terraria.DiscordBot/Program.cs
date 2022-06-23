using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Myamtech.Terraria.DiscordBot.Configuration;
using Myamtech.Terraria.DiscordBot.Database;
using Myamtech.Terraria.DiscordBot.Health;
using Myamtech.Terraria.DiscordBot.Services;
using Myamtech.Terraria.DiscordBot.Terraria;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile((src) =>
{
    src.Optional = false;
    src.Path = "terraria.json";
    src.ReloadOnChange = true;
    src.ReloadDelay = 5000;
});

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog(dispose: true);
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<DiscordConfiguration>(
    builder.Configuration.GetSection(nameof(Discord)), (b) =>
    {
        b.ErrorOnUnknownConfiguration = true;
    }
).PostConfigure<DiscordConfiguration>(x =>
{
    var context = new ValidationContext(x);
    Validator.ValidateObject(x, context);
});

builder.Services.Configure<TerrariaTargetsConfiguration>(
    builder.Configuration.GetSection(nameof(TerrariaTargetsConfiguration.TerrariaTarget) + "s"), (b) =>
    {
        b.ErrorOnUnknownConfiguration = true;
    }
).PostConfigure<TerrariaTargetsConfiguration>(x =>
{
    var context = new ValidationContext(x);
    Validator.ValidateObject(x, context);
});

// Add the interaction server as a transient
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddSingleton<TerrariaServerCache>();

builder.Services.AddSingleton((_) => new DiscordSocketClient(new DiscordSocketConfig()
{
    GatewayIntents =
        GatewayIntents.AllUnprivileged |
        GatewayIntents.GuildMembers |
        GatewayIntents.GuildMessages |
        GatewayIntents.GuildMessageReactions,
    AlwaysDownloadUsers = true,
}));

builder.Services.AddDbContext<DiscordBotDataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddSingleton<DiscordUserRepository>();
builder.Services.AddSingleton<TerrariaEmbedRepository>();

builder.Services.AddHttpClient<TShockHttpClient>()
    .UseHttpClientMetrics();

builder.Services.AddHostedService<InteractionHandlerService>();
builder.Services.AddHostedService<BotService>();
builder.Services.AddHostedService<TShockScraper>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddCheck<DiscordHealthCheck>(nameof(DiscordHealthCheck))
    .AddCheck<TShockUpstreamHealthCheck>(nameof(TShockUpstreamHealthCheck))
    .ForwardToPrometheus();

var app = builder.Build();

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        // Degraded is still "healthy"
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
app.MapControllers();
app.UseRouting();
app.UseHttpMetrics((o) =>
{
    o.ReduceStatusCodeCardinality();
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DiscordBotDataContext>();
    context.Database.EnsureCreated();
}

app.Run();