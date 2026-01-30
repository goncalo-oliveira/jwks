using Jwks.Commands;
using Jwks.Store;

namespace tests;

public sealed class PrivateKeyExportTests
{
    [Fact]
    public void ExportedPrivateKey_IsUsable()
    {
        using var tmp = new TempFs();

        JwksInitTests.Init( tmp );

        var exitCode = TestCommand.Run<KeygenCommand>( ["--jwks-path", tmp.Path] );

        Assert.Equal( 0, exitCode );

        if ( !JwksStore.TryGetValue( tmp.Path, out var store ) )
        {
            Assert.Fail( "JWKS store not found after initialization." );
        }

        var key = Assert.Single( store.KeySet.Keys );

        var pkcs8 = store.ExportPrivateKey( key );

        using var ecdsa = System.Security.Cryptography.ECDsa.Create();

        ecdsa.ImportPkcs8PrivateKey( pkcs8, out _ );

        Assert.NotNull( ecdsa.ExportParameters( false ).Q.X );
        Assert.NotNull( ecdsa.ExportParameters( false ).Q.Y );
    }
}
