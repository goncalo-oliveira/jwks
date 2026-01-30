namespace Jwks.Store;

/// <summary>
/// Interface for a collection that supports exporting private keys.
/// </summary>
public interface IPrivateKeyCollection
{
    /// <summary>
    /// Exports the private key associated with the given Key ID (kid).
    /// </summary>
    /// <param name="kid">The Key ID of the key to export.</param>
    /// <returns>>The private key in PKCS#8 format.</returns>
    ReadOnlySpan<byte> ExportPrivateKey( string kid );
}
