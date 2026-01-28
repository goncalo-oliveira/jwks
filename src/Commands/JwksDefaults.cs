namespace Jwks.Commands;

internal static class JwksDefaults
{
    public static readonly string Issuer = "https://jwks.local";

    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile ),
        ".jwks"
    );

    /// <summary>
    /// Combines the explicit path with the default .jwks directory.
    /// </summary>
    /// <param name="explicitPath">An explicit path provided by the user.</param>
    public static string CombinePath( string? explicitPath )
    {
        if ( !string.IsNullOrEmpty( explicitPath ) )
        {
            return System.IO.Path.Combine( explicitPath, ".jwks" );
        }

        return Path;
    }

    /// <summary>
    /// Gets the JWKS source path based on the provided explicit path or defaults.
    /// </summary>
    /// <param name="explicitPath">An explicit path provided by the user.</param>
    /// <returns>The resolved JWKS source path, or null if not found.</returns>
    public static string? GetJwksSourcePath( string? explicitPath )
    {
        /*
        We look for the signing key file in the following order:
        1. Explicit path provided via command-line argument
            ("[explicit_path]/.jwks/jwks.json" or "[explicit_path]/jwks.json").
        2. Current working directory ("./jwks/jwks.json").
        3. Default path in the user's home directory ("~/.jwks/jwks.json").
        */

        var pathsToTry = new List<string>();

        // 1. Explicit path (either to .jwks or signing.key)
        if ( !string.IsNullOrEmpty( explicitPath ) )
        {
            pathsToTry.Add( System.IO.Path.Combine( explicitPath, ".jwks/jwks.json" ) );
            pathsToTry.Add( System.IO.Path.Combine( explicitPath, "jwks.json" ) );
        }

        // 2. Current working directory
        pathsToTry.Add(
            System.IO.Path.Combine( Environment.CurrentDirectory, ".jwks/jwks.json" )
            );

        // 3. Default home directory path
        pathsToTry.Add( System.IO.Path.Combine( JwksDefaults.Path, "jwks.json" ) );
        foreach ( var path in pathsToTry )
        {
            if ( File.Exists( path ) )
            {
                return System.IO.Path.GetDirectoryName( path );
            }
        }

        return null;
    }
}
