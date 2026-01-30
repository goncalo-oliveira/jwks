namespace Jwks.Store;

internal static class JwksDefaults
{
    public static readonly string Issuer = "https://jwks.local";

    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile ),
        ".jwks"
    );
}
