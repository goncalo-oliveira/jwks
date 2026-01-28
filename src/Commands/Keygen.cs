using System.ComponentModel;
using System.Security.Cryptography;
using Jwks.Serialization;
using Jwks.Utils;
using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jwks.Commands;

public sealed partial class KeygenCommand : Command<KeygenCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption( "--jwks-path" )]
        [Description( "Path to the JWKS source." )]
        public string? Path { get; set; }

        [CommandOption( "--alg" )]
        [Description( "Algorithm to use for initialization. Default is ES256." )]
        public string Algorithm { get; set; } = "ES256";

        [CommandOption( "--name" )]
        [Description( "Name to associate with the generated key." )]
        public string? Name { get; set; }

        [CommandOption( "--export" )]
        [Description( "Export the generated key to a PEM and JWKS files." )]
        public bool Export { get; set; }

        [CommandOption( "--out" )]
        [Description( "Output path for exported key files." )]
        public string? OutputPath { get; set; }
    }

    public override int Execute( CommandContext context, Settings settings, CancellationToken cancellationToken )
    {
        var path = JwksDefaults.GetJwksSourcePath( settings.Path );

        if ( string.IsNullOrEmpty( path ) || !File.Exists( Path.Combine( path, "jwks.json" ) ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Not initialized." );
            
            return 1;
        }

        if ( !settings.Algorithm.Equals( "ES256", StringComparison.OrdinalIgnoreCase ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Unsupported algorithm. Only ES256 is supported." );
            return 1;
        }

        if ( !string.IsNullOrEmpty( settings.Name ) && !KeyNameRegex().IsMatch( settings.Name ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Invalid name. Only alphanumeric characters, hyphens, and underscores are allowed." );
            return 1;
        }

        if ( !string.IsNullOrEmpty( settings.OutputPath ) && settings.Export == false )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] --out requires --export to be specified." );
            return 1;
        }

        // export to stdout only if --export is specified without --out
        // when exporting to stdout, we use a quiet output (no verbose messages)
        var exportToStdout = settings.Export && string.IsNullOrEmpty( settings.OutputPath );

        if ( !exportToStdout )
        {
            Console.WriteLine( $"Source: {PathHelper.ShrinkHomePath( path )}" );
            Console.WriteLine();
        }

        var (jwk,keyBase64) = CreateJsonWebKey( settings.Name, path, !settings.Export );

        if ( !settings.Export )
        {
            AddJsonWebKey( jwk, path );
        }

        if ( !exportToStdout )
        {
            AnsiConsole.MarkupLine( $"[green]✔[/] Key generated (kid=[green]{jwk.Kid![..12]}[/]...)" );
        }

        if ( settings.Export )
        {
            ExportJsonWebKey( jwk, keyBase64, settings.OutputPath );

            if ( !exportToStdout )
            {
                AnsiConsole.MarkupLine( "[green]✔[/] Key exported" );
            }
        }

        return 0;
    }

    private static (JsonWebKey,string) CreateJsonWebKey( string? name, string path, bool writeToFile )
    {
        using var ecdsa = ECDsa.Create( ECCurve.NamedCurves.nistP256 );

        // Compute KID from DER SPKI
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        var kid = Convert.ToHexStringLower( SHA256.HashData( spki ) );

        // Private key (PKCS#8 PEM)
        var privateKeyBytes = ecdsa.ExportPkcs8PrivateKey();

        if ( writeToFile )
        {
            var privateKeyPath = Path.Combine( path, $"{kid}.key" );
            var privateKeyPem = PemEncoding.Write(
                "PRIVATE KEY",
                privateKeyBytes
            );

            File.WriteAllText( privateKeyPath, privateKeyPem );
        }

        // Convert to JWK
        var key = new ECDsaSecurityKey( ecdsa ) { KeyId = kid };
        var jwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey( key );

        jwk.Use = "sig";
        jwk.Alg = "ES256";
        jwk.D = null; // REMOVE private material

        if ( name != null )
        {
            jwk.AdditionalData["name"] = name;
        }

        return (jwk, Convert.ToBase64String( privateKeyBytes ) );
    }

    private static void AddJsonWebKey( JsonWebKey jwk, string path )
    {
        var jwksPath = Path.Combine( path, "jwks.json" );

        var jwks = JsonWebKeySet.Create( File.ReadAllText( jwksPath ) );

        jwks.Keys.Add( jwk );

        File.WriteAllBytes(
            jwksPath,
            Json.SerializeToUtf8Bytes( jwks )
        );
    }

    private static void ExportJsonWebKey( JsonWebKey jwk, string keyBase64, string? outputPath )
    {
        var jwksJson = Json.Serialize( new JsonWebKeySet { Keys = { jwk } } );

        if ( !string.IsNullOrEmpty( outputPath ) )
        {
            var jskwFilepath = Path.Combine( outputPath, "jwks.json" );
            var keyFilepath = Path.Combine( outputPath, $"private_key.pem" );

            File.WriteAllText( jskwFilepath, jwksJson );

            var privateKeyBytes = Convert.FromBase64String( keyBase64 );
            var privateKeyPem = PemEncoding.Write(
                "PRIVATE KEY",
                privateKeyBytes
            );

            File.WriteAllText( keyFilepath, privateKeyPem );
        }
        else
        {
            Console.WriteLine( "# JWKS" );
            Console.WriteLine( jwksJson );
            Console.WriteLine();
            Console.WriteLine( "# PEM Private Key Base64" );
            Console.WriteLine( keyBase64 );
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex( @"^[a-zA-Z0-9_-]+$" )]
    private static partial System.Text.RegularExpressions.Regex KeyNameRegex();
}
