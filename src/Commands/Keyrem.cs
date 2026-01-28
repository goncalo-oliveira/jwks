using System.ComponentModel;
using Jwks.Serialization;
using Jwks.Utils;
using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jwks.Commands;

public sealed partial class KeyremCommand : Command<KeyremCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument( 0, "<kid>" )]
        public string? Kid { get; set; }

        [CommandOption( "--jwks-path" )]
        [Description( "Path to the JWKS source." )]
        public string? Path { get; set; }

        [CommandOption( "-y|--yes" )]
        [Description( "Assume 'yes' as answer to all prompts and run non-interactively." )]
        public bool Yes { get; set; }
    }

    public override int Execute( CommandContext context, Settings settings, CancellationToken cancellationToken )
    {
        if ( string.IsNullOrEmpty( settings.Kid ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] KID is required." );

            return 1;
        }

        var path = JwksDefaults.GetJwksSourcePath( settings.Path );

        if ( string.IsNullOrEmpty( path ) || !File.Exists( Path.Combine( path, "jwks.json" ) ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Not initialized." );
            
            return 1;
        }

        Console.WriteLine( $"Source: {PathHelper.ShrinkHomePath( path )}" );
        Console.WriteLine();

        var keys = SelectJsonWebKeys( settings.Kid, path );

        if ( keys.Length == 0 )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Key(s) not found." );

            return 1;
        }

        if ( !RemoveJsonWebKeys( keys, path, !settings.Yes ) )
        {
            return 1;
        }

        foreach ( var key in keys )
        {
            AnsiConsole.MarkupLine( $"[green]✔[/] Key removed (kid=[green]{key.Kid[..12]}[/]...)" );
        }

        return 0;
    }

    private static JsonWebKey[] SelectJsonWebKeys( string kid, string path )
    {
        var jwksPath = Path.Combine( path, "jwks.json" );

        var jwks = JsonWebKeySet.Create( File.ReadAllText( jwksPath ) );

        return jwks.Keys.Where( k => k.Kid.StartsWith( kid, StringComparison.Ordinal ) ).ToArray();
    }

    private static bool RemoveJsonWebKeys( JsonWebKey[] keys, string path, bool prompt = true )
    {
        if ( prompt )
        {
            AnsiConsole.MarkupLine( $"The following key(s) will be removed:" );

            foreach ( var key in keys )
            {
                AnsiConsole.MarkupLine( $"- [yellow]{key.Kid[..12]}[/]..." );
            }

            if ( !AnsiConsole.Confirm( "Continue?", false ) )
            {
                return false;
            }
        }

        var jwksPath = Path.Combine( path, "jwks.json" );

        var jwks = JsonWebKeySet.Create( File.ReadAllText( jwksPath ) );

        foreach ( var key in keys )
        {
            jwks.Keys.Remove( jwks.Keys.Single( k => k.Kid == key.Kid ) );

            // delete private key file
            File.Delete( Path.Combine( path, $"{key.Kid}.key" ) );
        }

        File.WriteAllBytes(
            jwksPath,
            Json.SerializeToUtf8Bytes( jwks )
        );

        return true;
    }

    [System.Text.RegularExpressions.GeneratedRegex( @"^[a-zA-Z0-9_-]+$" )]
    private static partial System.Text.RegularExpressions.Regex KeyNameRegex();
}
