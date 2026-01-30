using Jwks.Commands;
using Jwks.Store;
using Microsoft.IdentityModel.Tokens;

namespace tests;

public sealed class JwksInitTests
{
    [Fact]
    public void Init_CreatesEmptyJwks()
    {
        using var tmp = new TempFs();

        Init( tmp );

        var jwksPath = Path.Combine( tmp.Path, ".jwks", "jwks.json" );

        Assert.True( File.Exists( jwksPath ) );

        var json = File.ReadAllText( jwksPath );

        var keySet = JsonWebKeySet.Create( json );

        Assert.Empty( keySet.Keys );
    }

    internal static JwksStore Init( TempFs fs )
    {
        var exitCode = TestCommand.Run<InitCommand>( [fs.Path] );

        Assert.Equal( 0, exitCode );

        var hasStore = JwksStore.TryGetValue( fs.Path, out var store );

        Assert.True( hasStore );
        Assert.NotNull( store );

        return store;
    }   
}
