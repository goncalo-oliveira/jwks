using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Faactory.Types;
using Jwks.Utils;
using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jwks.Commands;

public sealed class TokenCommand : Command<TokenCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption( "--jwks-path" )]
        [Description( "Path to the JWKS source." )]
        public string? Path { get; set; }

        [CommandOption( "--kid" )]
        [Description( "Key ID of the signing key to use." )]
        public string? Kid { get; set; }

        [CommandOption( "--aud" )]
        [Description( "Audience claim for the issued token. Same as `--claim aud=`." )]
        public string? Audience { get; set; }

        [CommandOption( "--sub" )]
        [Description( "Subject claim for the issued token. Same as `--claim sub=`." )]
        public string? Subject { get; set; }

        [CommandOption( "--claim" )]
        [Description( "Additional custom claims in the format 'type=value'." )]
        public List<string> Claims { get; set; } = [];

        [CommandOption( "--ttl" )]
        [Description( "Time-to-live for the token (e.g., '15m', '1h'). Default is 1 hour." )]
        public string? Ttl { get; set; }

        [CommandOption( "-q|--quiet" )]
        [Description( "Suppress output except for the token value." )]
        public bool Quiet { get; set; }
    }

    public override int Execute( CommandContext context, Settings settings, CancellationToken cancellationToken )
    {
        var path = JwksDefaults.GetJwksSourcePath( settings.Path );

        if ( string.IsNullOrEmpty( path ) || !File.Exists( Path.Combine( path, "jwks.json" ) ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Not initialized." );
            
            return 1;
        }

        if ( !settings.Quiet )
        {
            Console.WriteLine( $"Source: {PathHelper.ShrinkHomePath( path )}" );
            Console.WriteLine();
        }

        var claimsList = new List<Claim>()
        {
            new( "jti", GenerateRandomToken() ),
            new( "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() )
        };

        if ( !string.IsNullOrEmpty( settings.Audience ) )
        {
            claimsList.Add( new Claim( "aud", settings.Audience! ) );
        }
        else if ( !settings.Claims.Any( c => c.StartsWith( "aud=" ) ) )
        {
            StdErr.Out.MarkupLine( "[yellow]Warning:[/] No audience specified." );
        }

        if ( !string.IsNullOrEmpty( settings.Subject ) )
        {
            claimsList.Add( new Claim( "sub", settings.Subject! ) );
        }
        else if ( !settings.Claims.Any( c => c.StartsWith( "sub=" ) ) )
        {
            StdErr.Out.MarkupLine( "[yellow]Warning:[/] No subject specified." );
        }

        foreach ( var claim in settings.Claims )
        {
            var parts = claim.Split( '=', 2 );

            if ( parts.Length != 2 )
            {
                StdErr.Out.MarkupLine( $"[red]Error:[/] Invalid claim format: {claim}. Expected 'type=value'." );

                return 1;
            }

            claimsList.Add( new Claim( parts[0], parts[1] ) );
        }

        if ( string.IsNullOrEmpty( settings.Ttl ) || !TimeSpanHelper.TryParseDuration( settings.Ttl, out TimeSpan ttl ) )
        {
            ttl = TimeSpan.FromHours(1);
        }

        var jwks = JsonWebKeySet.Create( File.ReadAllText( Path.Combine( path, "jwks.json" ) ) );

        if ( jwks.Keys.Count == 0 )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] No keys available in the JWKS." );

            return 1;
        }
        else if ( jwks.Keys.Count > 1 && string.IsNullOrEmpty( settings.Kid ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Multiple keys available. Please specify a key ID using --kid." );

            return 1;
        }

        var selectedJwk = string.IsNullOrEmpty( settings.Kid )
            ? [jwks.Keys.First()]
            : jwks.Keys.Where( k => k.Kid.StartsWith( settings.Kid ) ).ToArray();

        if ( selectedJwk.Length == 0 )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Specified key ID not found in the JWKS." );

            return 1;
        }
        else if ( selectedJwk.Length > 1 )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Multiple keys found with the specified key ID." );

            return 1;
        }

        var kid = selectedJwk.Single().Kid;

        var signingCredentials = CreateSigningCredentials( kid, path );

        if ( signingCredentials is null )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] No valid signing key found." );

            return 1;
        }

        try
        {
            var token = GenerateJwtToken( signingCredentials, claimsList, ttl );

            if ( string.IsNullOrEmpty( token ) )
            {
                return 1;
            }

            var expires = ReadTokenExpiration( token );

            if ( !settings.Quiet )
            {
                AnsiConsole.MarkupLine( $"[green]✔[/] Token issued (kid=[green]{kid![..12]}[/]..., exp=[blue]{expires:yyyy-MM-ddTHH:mmZ}[/])" );
                Console.WriteLine();
            }

            Console.WriteLine( token );

            return 0;
        }
        catch ( Exception ex )
        {
            StdErr.Out.MarkupLine( $"[red]Error:[/] {ex.Message}" );

            return 1;
        }
    }

    private static string GenerateJwtToken( SigningCredentials signingCredentials, IEnumerable<Claim> claims, TimeSpan ttl )
    {
        JwtSecurityTokenHandler tokenHandler = new();

        var token = new JwtSecurityToken(
            issuer: JwksDefaults.Issuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.Add( ttl ),
            signingCredentials: signingCredentials
        );

        return tokenHandler.WriteToken( token );
    }

    private static SigningCredentials? CreateSigningCredentials( string kid, string path )
    {
        var keyFilePath = Path.Combine( path, $"{kid}.key" );

        if ( !File.Exists( keyFilePath ) )
        {
            AnsiConsole.MarkupLine( "[red]Error:[/] Signing key file not found." );

            return null;
        }

        var ecdsa = ECDsa.Create();

        ecdsa.ImportFromPem( File.ReadAllText( keyFilePath ) );

        // export public key to validate key parameters
        var publicParams = ecdsa.ExportParameters( includePrivateParameters: false );

        if ( publicParams.Q.X is null || publicParams.Q.Y is null )
        {
            AnsiConsole.MarkupLine( "[red]Error:[/] Invalid EC public key parameters." );

            return null;
        }

        if ( publicParams.Q.X.Length != 32 || publicParams.Q.Y.Length != 32 )
        {
            AnsiConsole.MarkupLine( "[red]Error:[/] Unexpected EC key size. Expected P-256 key." );

            return null;
        }

        // Compute KID as SHA-256 hash of the DER SubjectPublicKeyInfo
        byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();
        kid = Convert.ToHexStringLower( SHA256.HashData( spki ) );

        // create signing credentials
        var securityKey = new ECDsaSecurityKey( ecdsa ) { KeyId = kid };

        return new SigningCredentials( securityKey, SecurityAlgorithms.EcdsaSha256 );
    }

    private static string GenerateRandomToken()
        => Base64Encoder.ToBase64String(
            RandomNumberGenerator.GetBytes( 16 )
        );

    public static DateTime? ReadTokenExpiration( string? tokenValue )
    {
        JwtSecurityTokenHandler tokenHandler = new();

        if ( string.IsNullOrEmpty( tokenValue ) )
        {
            return null;
        }

        try
        {
            var jwtToken = tokenHandler.ReadJwtToken( tokenValue );

            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }
}
