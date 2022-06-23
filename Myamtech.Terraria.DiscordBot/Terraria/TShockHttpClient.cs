using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Myamtech.Terraria.DiscordBot.Terraria;

public class TShockHttpClient
{
    public Uri? BaseAddress
    {
        get => _httpClient.BaseAddress;
        set => _httpClient.BaseAddress = value;
    }
    private readonly HttpClient _httpClient;

    public TShockHttpClient(
        HttpClient httpClient
    )
    {
        _httpClient = httpClient;
    }

    public async Task<ApiTypes.ServerStatus> GetStatusAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var response = await _httpClient.GetAsync("/status", cancellationToken);
        return await DeserializeAsync<ApiTypes.ServerStatus>(response, cancellationToken:cancellationToken);
    }
    
    public async Task<bool> TestTokenAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        var query = new Dictionary<string, string?>()
        {
            ["token"] = token,
        };
        
        var uri = QueryHelpers.AddQueryString(
            "/tokentest", 
            query
        );

        using var response = await _httpClient.GetAsync(uri, cancellationToken);

        try
        {
            await AssertSuccessAsync(response, cancellationToken: cancellationToken);
            return true;
        }
        catch (TShockException e) when (
            e.StatusCode == HttpStatusCode.Forbidden || 
            e.StatusCode == HttpStatusCode.BadRequest || 
            e.StatusCode == HttpStatusCode.Unauthorized
        )
        {
            return false;
        }
    }
    
    public async Task<ApiTypes.ServerStatusV2> GetAuthenticatedServerStatus(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        var query = new Dictionary<string, string?>()
        {
            ["token"] = token,
            ["players"] = "true"
        };
        
        var uri = QueryHelpers.AddQueryString(
            "/v2/server/status", 
            query
        );

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await DeserializeAsync<ApiTypes.ServerStatusV2>(response, cancellationToken:cancellationToken);
    }
    
    public async Task<ApiTypes.GenericResponse> ExecuteRawCommandAsync(
        string command,
        string token,
        CancellationToken cancellationToken = default
    )
    {
        var query = new Dictionary<string, string?>()
        {
            ["token"] = token,
            ["cmd"] = command
        };
        
        var uri = QueryHelpers.AddQueryString(
            "/v3/server/rawcmd", 
            query
        );

        // /v2/users/list
        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await DeserializeAsync<ApiTypes.GenericResponse>(response, cancellationToken:cancellationToken);
    }
    
    public async Task<ApiTypes.UsersList> GetUsersList(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        var query = new Dictionary<string, string?>()
        {
            ["token"] = token,
        };
        
        var uri = QueryHelpers.AddQueryString(
            "/v2/users/list", 
            query
        );

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        return await DeserializeAsync<ApiTypes.UsersList>(response, cancellationToken:cancellationToken);
    }

    private static async Task AssertSuccessAsync(
        HttpResponseMessage responseMessage,
        Action<string>? badRequestThrower = null,
        CancellationToken cancellationToken = default
    )
    {
        if (responseMessage.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
        {
            if (badRequestThrower == null || responseMessage.Content.Headers.ContentLength.GetValueOrDefault() == 0)
            {
                // Throw the bad request exception in this case with no special content
                throw new TShockException(HttpStatusCode.BadRequest, "Failed to make call because the request was bad (No context)");
            }
            
            string stringValue = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            badRequestThrower.Invoke(stringValue);
            
            // Above method should throw but this is the fallback
            throw new TShockException(
                HttpStatusCode.BadRequest, 
                "Failed to make call because the request was bad (No context)"
            );
        }
        
        switch (responseMessage.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                // A secure endpoint (which requires an authenticated token) was used without supplying an authenticated token.
                throw new TShockException(
                    HttpStatusCode.Unauthorized, 
                    "Token was not provided but is required"
                );
            case HttpStatusCode.Forbidden:
                // Returned solely by the token creation endpoint, this value indicates that the supplied credentials are invalid.
                throw new TShockException(HttpStatusCode.Forbidden, "The token provided was invalid");
            default:
                throw new TShockException(responseMessage.StatusCode, "Unknown error was provided: " + responseMessage.StatusCode);
        }
    }

    private static async Task<T> DeserializeAsync<T>(
        HttpResponseMessage responseMessage,
        Action<string>? badRequestThrower = null,
        CancellationToken cancellationToken = default
    ) where T : class
    {
        if (responseMessage.StatusCode == HttpStatusCode.OK)
        {
            string stringValue = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(stringValue)!;
        }

        await AssertSuccessAsync(responseMessage, badRequestThrower, cancellationToken);

        // Will always throw above since we checked if it was successful already
        return null!;
    }
    
}