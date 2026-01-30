using Jwks.Commands;
using Jwks.Store;

namespace tests;

public sealed class JwksKeyrmTests
{
    [Fact]
    public void Keyrm_RemovesMatchingKey()
    {
        using var tmp = new TempFs();

        TestCommand.Run<InitCommand>( [tmp.Path] );
        TestCommand.Run<KeygenCommand>( ["--jwks-path", tmp.Path] );

        if ( !JwksStore.TryGetValue( tmp.Path, out var store ) )
        {
            Assert.Fail( "JWKS store not found after initialization." );
        }

        var key = Assert.Single( store.KeySet.Keys );

        TestCommand.Run<KeyremCommand>( [key.Kid[..4], "--jwks-path", tmp.Path, "--yes"] );

        if ( !JwksStore.TryGetValue( tmp.Path, out var updated ) )
        {
            Assert.Fail( "JWKS store not found after initialization." );
        }

        Assert.Empty( updated.KeySet.Keys );
    }
}
