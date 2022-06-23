using System.Net;

namespace Myamtech.Terraria.DiscordBot.Terraria;

public class TShockException : IOException
{

    public HttpStatusCode? StatusCode { get; }
    
    public TShockException(string message) : base(message)
    {
        StatusCode = default;
    }
    
    public TShockException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
    
}