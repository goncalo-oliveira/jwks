using System.ComponentModel;
using System.Security.Cryptography;
using Jwks.Serialization;
using Jwks.Store;
using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jwks.Commands;

public sealed class ExportCommand : Command<ExportCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument( 0, "[kid]" )]
        [Description( "Key ID (kid) of the key to export." )]
        public string? Kid { get; set; }

        [CommandOption( "--jwks-path" )]
        [Description( "Path to the JWKS source." )]
        public string? Path { get; set; }

        [CommandOption( "--out" )]
        [Description( "Output path for exported key files." )]
        public string? OutputPath { get; set; }

        [CommandOption( "--all" )]
        [Description( "Export all keys in the JWKS." )]
        public bool ExportAll { get; set; }

        [CommandOption( "--private" )]
        [Description( "Include private key material in the export." )]
        public bool Private { get; set; }
    }

    public override int Execute( CommandContext context, Settings settings, CancellationToken cancellationToken )
    {
        if ( string.IsNullOrEmpty( settings.Kid ) && !settings.ExportAll )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Key ID (kid) is required unless --all is specified." );

            return 1;
        }

        if ( !JwksStore.TryGetValue( settings.Path, out var store ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Not initialized." );

            return 1;
        }

        if ( store.KeySet.Keys.Count == 0 )
        {
            AnsiConsole.MarkupLine( "[yellow]No keys found.[/]" );

            return 1;
        }

        // when exporting to stdout, we use a silent output (no verbose messages)
        var exportToStdout = string.IsNullOrEmpty( settings.OutputPath );

        if ( !exportToStdout )
        {
            Console.WriteLine( $"Store: {store}" );
            Console.WriteLine();
        }

        if ( settings.ExportAll )
        {
            try
            {
                ExportKeySet( store.KeySet, store, settings.OutputPath, settings.Private );

                if ( !exportToStdout )
                {
                    AnsiConsole.MarkupLine( "[green]✔[/] Keys exported" );
                }

                return 0;
            }
            catch ( FileNotFoundException ex )
            {
                StdErr.Out.MarkupLine( $"[red]Error:[/] Private key file not found ({ex.FileName})." );

                return 1;
            }
        }

        var keys = store.SelectKeys( settings.Kid! );

        if ( keys.Length == 0 )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Key not found." );

            return 1;
        }
        else if ( keys.Length > 1 )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Multiple keys found with the specified KID. Please specify a more specific KID." );

            return 1;
        }

        var jwk = keys.Single();

        try
        {
            var privateKey = settings.Private
                ? store.ExportPrivateKey( jwk )
                : [];

            // ExportJsonWebKey( store, jwk, settings.OutputPath, settings.Private );

            ExportKeySet( new JsonWebKeySet { Keys = { jwk } }, store, settings.OutputPath, settings.Private );

            if ( !exportToStdout )
            {
                AnsiConsole.MarkupLine( "[green]✔[/] Key exported" );
            }
        }
        catch ( FileNotFoundException )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Private key file not found." );

            return 1;
        }

        return 0;
    }

    internal static void ExportKeySet( JsonWebKeySet keySet, IPrivateKeyCollection privateKeyExport, string? outputPath, bool includePrivate )
    {
        var jwks = JsonWebKeySet.Create( Json.Serialize( keySet ) );
        var privateKeys = new List<PrivateKeyExport>();

        if ( includePrivate )
        {
            foreach ( var jwk in keySet.Keys )
            {
                var privateKey = privateKeyExport.ExportPrivateKey( jwk.Kid );

                if ( !string.IsNullOrEmpty( outputPath ) )
                {
                    var keyFilepath = Path.Combine( outputPath, $"jwk_{jwk.Kid[..16]}.pem" );

                    var privateKeyPem = PemEncoding.Write(
                        "PRIVATE KEY",
                        privateKey
                    );

                    File.WriteAllText( keyFilepath, privateKeyPem );
                }
                else
                {
                    privateKeys.Add( new PrivateKeyExport
                    {
                        Kid = jwk.Kid,
                        Pkcs8 = Convert.ToBase64String( privateKey )
                    } );
                }
            }

            if ( privateKeys.Count > 0 )
            {
                jwks.AdditionalData["private_keys"] = privateKeys;
            }
        }

        var jwksJson = Json.Serialize( jwks );

        if ( !string.IsNullOrEmpty( outputPath ) )
        {
            var jskwFilepath = Path.Combine( outputPath, "jwks.json" );

            File.WriteAllText( jskwFilepath, jwksJson );
        }
        else
        {
            Console.WriteLine( jwksJson );
        }
    }

    // internal static void ExportJsonWebKey( JwksStore store, JsonWebKey jwk, string? outputPath, bool includePrivate )
    // {
    //     var jwks = new JsonWebKeySet { Keys = { jwk } };

    //     ExportAll( store, jwks, outputPath, includePrivate );

    //     // var jwksJson = Json.Serialize( new JsonWebKeySet { Keys = { jwk } } );

    //     // if ( !string.IsNullOrEmpty( outputPath ) )
    //     // {
    //     //     var jskwFilepath = Path.Combine( outputPath, "jwks.json" );
    //     //     var keyFilepath = Path.Combine( outputPath, $"private_key.pem" );

    //     //     File.WriteAllText( jskwFilepath, jwksJson );

    //     //     var privateKeyPem = PemEncoding.Write(
    //     //         "PRIVATE KEY",
    //     //         privateKey
    //     //     );

    //     //     File.WriteAllText( keyFilepath, privateKeyPem );
    //     // }
    //     // else
    //     // {
    //     //     var keyset = new JsonWebKeySet { Keys = { jwk } };

    //     //     keyset.AdditionalData["private_keys"] = new[]
    //     //     {
    //     //         new PrivateKeyExport
    //     //         {
    //     //             Kid = jwk.Kid,
    //     //             Pkcs8 = Convert.ToBase64String( privateKey )
    //     //         }
    //     //     };

    //     //     Console.WriteLine( Json.Serialize( keyset ) );
    //     // }
    // }

    private class PrivateKeyExport
    {
        public string? Kid { get; set; }

        public string? Pkcs8 { get; set; }
    }
}
