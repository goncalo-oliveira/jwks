using Jwks.Commands;
using Jwks.Store;

namespace tests;

public sealed class JwksKeygenTests
{
    [Fact]
    public void Keygen_AddsPublicKeyOnly()
    {
        using var tmp = new TempFs();

        JwksInitTests.Init( tmp );

        var exitCode = TestCommand.Run<KeygenCommand>( ["--jwks-path", tmp.Path, "--name", "test"] );

        Assert.Equal( 0, exitCode );

        if ( !JwksStore.TryGetValue( tmp.Path, out var store ) )
        {
            Assert.Fail( "JWKS store not found after initialization." );
        }

        var key = Assert.Single( store.KeySet.Keys );

        Assert.True( key.AdditionalData.TryGetValue( "name", out var name ) );
        Assert.Equal( "test", name.ToString() );
        Assert.NotNull( key.X );
        Assert.NotNull( key.Y );
        Assert.Null( key.D ); // must not have private material
    }
}
