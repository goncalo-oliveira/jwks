using System.Text.Json;

namespace Jwks.Serialization;

internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static string Serialize<T>( T value )
        => JsonSerializer.Serialize( value, Options );

    public static byte[] SerializeToUtf8Bytes<T>( T value )
        => JsonSerializer.SerializeToUtf8Bytes( value, Options );

    public static T? Deserialize<T>( byte[] json )
        => JsonSerializer.Deserialize<T>( json, Options );
}
