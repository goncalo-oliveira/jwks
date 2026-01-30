using Jwks.Commands;
using Jwks.Store;
using Microsoft.IdentityModel.Tokens;

namespace tests;

public sealed class KidResolutionTests
{
    [Fact]
    public void PartialKid_UniqueMatch_Works()
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

        var resolved = store.SelectKeys( key.Kid[..8] ).Single();

        Assert.Equal( key.Kid, resolved.Kid );
    }

    [Fact]
    public void PartialKid_Ambiguous_ReturnsMultiple()
    {
        using var tmp = new TempFs();

        var store = JwksInitTests.Init( tmp );

        var jwk1 = CreateTempKey( "aa" );
        var jwk2 = CreateTempKey( "ab" );

        store.AddKey( jwk1, [] );
        store.AddKey( jwk2, [] );

        var selected = store.SelectKeys( "a" );

        Assert.Equal( 2, selected.Length );
    }

    private static JsonWebKey CreateTempKey( string kid )
    {
        using var ecdsa = System.Security.Cryptography.ECDsa.Create(
            System.Security.Cryptography.ECCurve.NamedCurves.nistP256
        );

        // Convert to JWK
        var key = new ECDsaSecurityKey( ecdsa ) { KeyId = kid };
        var jwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey( key );

        return jwk;
    }
}
