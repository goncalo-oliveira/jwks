namespace Jwks.Utils;

internal static class PathHelper
{
    /// <summary>
    /// Shrinks the given path by replacing the user's home directory with `~` if applicable.
    /// </summary>
    public static string ShrinkHomePath( string path )
    {
        var home =
            Environment.GetFolderPath( Environment.SpecialFolder.UserProfile )
                .TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );

        if ( path.StartsWith( home, StringComparison.OrdinalIgnoreCase ) )
        {
            return string.Concat( "~", path.AsSpan( home.Length ) );
        }

        return path;
    }    
}
