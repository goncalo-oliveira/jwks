using System.Diagnostics.CodeAnalysis;
using Jwks.Serialization;
using Jwks.Utils;
using Microsoft.IdentityModel.Tokens;

namespace Jwks.Store;

internal class JwksStore : IPrivateKeyCollection
{
    private readonly string jwksPath;

    private JwksStore( string path, JsonWebKeySet keySet )
    {
        Path = path;
        jwksPath = System.IO.Path.Combine( path, "jwks.json" );
        KeySet = keySet;
    }

    /// <summary>
    /// The JWKS source path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The JSON Web Key Set.
    /// </summary>
    public JsonWebKeySet KeySet { get; }

    /// <summary>
    /// Adds a new JSON Web Key to the store and saves the associated private key.
    /// </summary>
    /// <param name="jwk">The JSON Web Key to add.</param>
    /// <param name="privateKey">The private key in PKCS#8 format.</param>
    public void AddKey( JsonWebKey jwk, ReadOnlySpan<byte> privateKey )
    {
        // Save the private key to a PEM file
        var privateKeyPem = System.Security.Cryptography.PemEncoding.Write(
            "PRIVATE KEY",
            privateKey
        );

        // Write the private key PEM to a file
        File.WriteAllText(
            GetPrivateKeyPath( jwk.Kid ),
            privateKeyPem
        );

        // Add the JWK to the JWKS and commit changes
        KeySet.Keys.Add( jwk );

        CommitChanges();
    }

    /// <summary>
    /// Removes the specified JSON Web Keys from the store and deletes their associated private key files.
    /// </summary>
    /// <param name="keys">The JSON Web Keys to remove.</param>
    public void RemoveKeys( JsonWebKey[] keys )
    {
        foreach ( var key in keys )
        {
            // remove JWK from JWKS
            // we use a new selector to allow portable keys (different object references)
            KeySet.Keys.Remove( KeySet.Keys.Single( k => k.Kid == key.Kid ) );

            // delete private key file
            var privateKeyPath = GetPrivateKeyPath( key.Kid );

            File.Delete( privateKeyPath );
        }

        CommitChanges();
    }

    /// <summary>
    /// Selects keys matching the given key ID (kid) prefix.
    /// </summary>
    /// <param name="kidPrefix">The key ID (kid) prefix to match.</param>
    /// <returns>>An array of matching JSON Web Keys.</returns>
    public JsonWebKey[] SelectKeys( string kidPrefix )
        => KeySet.Keys.Where( k => k.Kid.StartsWith( kidPrefix, StringComparison.Ordinal ) )
            .ToArray();

    /// <summary>
    /// Exports the private key associated with the given JSON Web Key.
    /// </summary>
    /// <param name="jwk">The JSON Web Key.</param>
    /// <returns>>The private key in PKCS#8 format.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the private key file is not found.</exception>
    public ReadOnlySpan<byte> ExportPrivateKey( JsonWebKey jwk )
        => ExportPrivateKey( jwk.Kid );

    /// <summary>
    /// Exports the private key associated with the given key ID (kid).
    /// </summary>
    /// <param name="kid">The key ID (kid) of the key to export.</param>
    /// <returns>>The private key in PKCS#8 format.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the private key file is not found.</exception>
    public ReadOnlySpan<byte> ExportPrivateKey( string kid )
    {
        var privateKeyPath = GetPrivateKeyPath( kid );

        if ( !File.Exists( privateKeyPath ) )
        {
            throw new FileNotFoundException( "Private key file not found.", privateKeyPath );
        }

        var pem = File.ReadAllText( privateKeyPath );

        using var ecdsa = System.Security.Cryptography.ECDsa.Create();
        ecdsa.ImportFromPem( pem );

        return ecdsa.ExportPkcs8PrivateKey();
    }

    /// <summary>
    /// Returns a user-friendly representation of the JWKS store path.
    /// </summary>
    public override string ToString()
        => PathHelper.ShrinkHomePath( Path );

    /// <summary>
    /// Gets the private key file path for the given key ID (kid).
    /// </summary>
    /// <param name="kid">The key ID (kid).</param>
    /// <returns>>The private key file path.</returns>
    private string GetPrivateKeyPath( string kid )
    {
        // The filename is capped to 32 characters to avoid filesystem limits
        var filename = kid.Length > 32 ? kid[..32] : kid;

        return System.IO.Path.Combine( Path, $"{filename}.key" );
    }

    private void CommitChanges()
    {
        File.WriteAllBytes(
            jwksPath,
            Json.SerializeToUtf8Bytes( KeySet )
        );
    }

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

        return JwksDefaults.Path;
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
            This is an exclusive check: if an explicit path is provided, we only check there.
        2. Default paths:
            2.1. Current working directory ("./jwks/jwks.json").
            2.2. Default path in the user's home directory ("~/.jwks/jwks.json").
        */

        var pathsToTry = new List<string>();

        // 1. Explicit path (either to .jwks or signing.key)
        if ( !string.IsNullOrEmpty( explicitPath ) )
        {
            pathsToTry.Add( System.IO.Path.Combine( explicitPath, ".jwks/jwks.json" ) );
            pathsToTry.Add( System.IO.Path.Combine( explicitPath, "jwks.json" ) );
        }
        else
        {
            // 2. only check defaults if no explicit path is provided

            // 2.1. Current working directory
            pathsToTry.Add(
                System.IO.Path.Combine( Environment.CurrentDirectory, ".jwks/jwks.json" )
                );

            // 2.2. Default home directory path
            pathsToTry.Add( System.IO.Path.Combine( JwksDefaults.Path, "jwks.json" ) );
        }

        foreach ( var path in pathsToTry )
        {
            if ( File.Exists( path ) )
            {
                return System.IO.Path.GetDirectoryName( path );
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to get the JWKS source path based on the provided explicit path or defaults.
    /// </summary>
    /// <param name="explicitPath">An explicit path provided by the user.</param>
    /// <param name="path">The resolved JWKS source path, or null if not found.</param>
    /// <returns>True if the path was found, false otherwise.</returns>
    public static bool TryGetValue( string? explicitPath, [MaybeNullWhen( false )] out JwksStore store )
    {
        var path = GetJwksSourcePath( explicitPath );

        if ( string.IsNullOrEmpty( path ) )
        {
            store = default;

            return false;
        }

        var filepath = System.IO.Path.Combine( path, "jwks.json" );

        if ( !File.Exists( filepath ) )
        {
            store = default;

            return false;
        }

        store = Load( path );

        return true;
    }

    /// <summary>
    /// Loads the JWKS store from the specified path.
    /// </summary>
    /// <param name="path">The JWKS source path.</param>
    /// <returns>>The loaded JWKS store.</returns>
    public static JwksStore Load( string path )
    {
        var filepath = System.IO.Path.Combine( path, "jwks.json" );
        var keySet = File.Exists( filepath )
            ? JsonWebKeySet.Create( File.ReadAllText( filepath ) )
            : new JsonWebKeySet();

        return new JwksStore( path, keySet );
    }
}
