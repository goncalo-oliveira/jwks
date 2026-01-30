using Microsoft.IdentityModel.Tokens;

namespace tests;

/// <summary>
/// Creates and manages a temporary filesystem directory for testing purposes.
/// The directory is automatically deleted when disposed.
/// </summary>
internal sealed class TempFs : IDisposable
{
    private readonly string path;

    public TempFs()
    {
        path = CreateTempDir();
    }

    /// <summary>
    /// Gets the path to the temporary directory.
    /// </summary>
    public string Path => path;

    private static string CreateTempDir()
    {
        // Generate a friendly but random temporary directory name
        Span<byte> bytes = stackalloc byte[16];
        Random.Shared.NextBytes( bytes );
        var tmpId = Base64UrlEncoder.Encode( bytes.ToArray() );

        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"jwk_{tmpId}"
        );

        Directory.CreateDirectory( path );

        return path;
    }

    public void Dispose()
    {
        if ( Directory.Exists( path ) )
        {
            Directory.Delete( path, true );
        }
    }
}
