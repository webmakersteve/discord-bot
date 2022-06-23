using System.Text.Json.Serialization;

namespace Myamtech.Terraria.DiscordBot.Terraria;

public static class ApiTypes
{
    public record class ServerStatus(
        [property:JsonPropertyName("name")] string Name, 
        [property:JsonPropertyName("port")] int Port, 
        [property:JsonPropertyName("serverversion")] string ServerVersion,
        
        [property:JsonPropertyName("playercount")] int PlayerCount, 
        [property:JsonPropertyName("maxplayers")] int MaxPlayers, 
        
        [property:JsonPropertyName("world")] string WorldName
    );
    
    public record class User(
        [property:JsonPropertyName("name")] string Name, 
        [property:JsonPropertyName("id")] long Id, 
        [property:JsonPropertyName("group")] string? Group
    );
    
    public record class Player(
        [property:JsonPropertyName("nickname")] string Nickname, 
        [property:JsonPropertyName("username")] string Username, 
        [property:JsonPropertyName("active")] bool Active, 
        [property:JsonPropertyName("group")] string Group,
        [property:JsonPropertyName("state")] int State
    );

    public record class UsersList(
        [property:JsonPropertyName("users")] List<User> Users
    );
    
    public record class ServerStatusV2(
        [property:JsonPropertyName("name")] string Name, 
        [property:JsonPropertyName("port")] int Port, 
        [property:JsonPropertyName("serverversion")] string ServerVersion,
        
        [property:JsonPropertyName("playercount")] int PlayerCount, 
        [property:JsonPropertyName("maxplayers")] int MaxPlayers, 
        
        [property:JsonPropertyName("world")] string WorldName,
        [property:JsonPropertyName("players")] List<Player> Player
    );
    
    public record class GenericResponse(
        [property:JsonPropertyName("response")] List<string> Response, 
        [property:JsonPropertyName("status")] string Status
    );
}