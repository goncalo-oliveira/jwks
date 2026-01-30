using System.Collections;

namespace Jwks.Store;

/// <summary>
/// Collection to hold private keys for export.
/// </summary>
internal sealed class PrivateKeyCollection : IEnumerable<KeyValuePair<string, ReadOnlyMemory<byte>>>, IPrivateKeyCollection
{
    private readonly Dictionary<string, ReadOnlyMemory<byte>> privateKeys = [];

    public void Add( string kid, ReadOnlyMemory<byte> privateKey )
    {
        privateKeys[kid] = privateKey;
    }

    public ReadOnlySpan<byte> ExportPrivateKey( string kid )
    {
        if ( privateKeys.TryGetValue( kid, out var privateKey ) )
        {
            return privateKey.Span;
        }

        throw new KeyNotFoundException( $"Private key with kid '{kid}' not found." );
    }

    public IEnumerator<KeyValuePair<string, ReadOnlyMemory<byte>>> GetEnumerator()
        => privateKeys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
