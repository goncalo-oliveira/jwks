using System.IdentityModel.Tokens.Jwt;
using Jwks.Commands;
using Jwks.Store;
using Microsoft.IdentityModel.Tokens;

namespace tests;

public sealed class TokenIntegrationTests
{
    [Fact]
    public void IssuedToken_ValidatesAgainstJwks()
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

        var creds = TokenCommand.CreateSigningCredentials(
            key.Kid,
            store
        );

        var token = new JwtSecurityToken(
            issuer: "https://jwks.local.tests",
            audience: "test",
            expires: DateTime.UtcNow.AddMinutes( 5 ),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var keys = store.KeySet.GetSigningKeys();

        new JwtSecurityTokenHandler().ValidateToken(
            jwt,
            new TokenValidationParameters
            {
                ValidIssuer = "https://jwks.local.tests",
                ValidAudience = "test",
                IssuerSigningKeys = keys,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true
            },
            out _
        );
    }

    [Fact]
    public void Token_FailsWithoutKid_WhenMultipleKeysExist()
    {
        using var tmp = new TempFs();

        JwksInitTests.Init( tmp );

        var kg1 = TestCommand.Run<KeygenCommand>( ["--jwks-path", tmp.Path] );
        var kg2 = TestCommand.Run<KeygenCommand>( ["--jwks-path", tmp.Path] );

        Assert.Equal( 0, kg1 );
        Assert.Equal( 0, kg2 );

        var exitCode = TestCommand.Run<TokenCommand>( ["--jwks-path", tmp.Path, "--sub", "test", "--aud", "test"] );

        Assert.Equal( 1, exitCode );
    }

    [Fact]
    public void Token_WithKid_IssuesJwt_WhenMultipleKeysExist()
    {
        using var tmp = new TempFs();

        JwksInitTests.Init( tmp );

        var kg1 = TestCommand.Run<KeygenCommand>( ["--jwks-path", tmp.Path] );
        var kg2 = TestCommand.Run<KeygenCommand>( ["--jwks-path", tmp.Path] );

        Assert.Equal( 0, kg1 );
        Assert.Equal( 0, kg2 );

        if ( !JwksStore.TryGetValue( tmp.Path, out var store ) )
        {
            Assert.Fail( "JWKS store not found after initialization." );
        }

        Assert.Equal( 2, store.KeySet.Keys.Count );

        var key = store.KeySet.Keys.First();

        var exitCode = TestCommand.Run<TokenCommand>(
            [
                "--jwks-path",
                tmp.Path,
                "--sub", "test",
                "--aud", "test",
                "--kid", key.Kid
            ]
        );

        Assert.Equal( 0, exitCode );
    }
}
