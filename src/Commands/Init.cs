using System.ComponentModel;
using System.Text.Json;
using Jwks.Serialization;
using Jwks.Utils;
using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jwks.Commands;

public sealed partial class InitCommand : Command<InitCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument( 0, "[path]" )]
        [Description( "Path to initialize the JWKS. Default is `~/`." )]
        public string? Path { get; set; }

        [CommandOption( "--force" )]
        [Description( "Force re-initialization even if already initialized." )]
        public bool Force { get; set; }
    }

    public override int Execute( CommandContext context, Settings settings, CancellationToken cancellationToken )
    {
        var path = JwksDefaults.CombinePath( settings.Path );

        if ( File.Exists( Path.Combine( path, "jwks.json" ) ) && !settings.Force )
        {
            AnsiConsole.MarkupLine( "[red]Error:[/] JWKS already initialized. Use --force to re-initialize." );
            return 1;
        }

        CreateLocalJwksAuthority( path );

        Console.WriteLine( $"Source: {PathHelper.ShrinkHomePath( path )}" );
        Console.WriteLine();
        AnsiConsole.MarkupLine( "[green]Success:[/] JWKS initialized successfully." );

        return 0;
    }

    private static void CreateLocalJwksAuthority( string path )
    {
        Directory.CreateDirectory( path );

        var jwksPath = Path.Combine( path, "jwks.json" );

        // delete jwks.json if it exists, to start fresh
        File.Delete( jwksPath );

        // delete all .key files in the directory
        var existingKeyFiles = Directory.GetFiles( path, "*.key" );
        foreach ( var keyFile in existingKeyFiles )
        {
            File.Delete( keyFile );
        }

        // write empty JWKS
        File.WriteAllBytes(
            jwksPath,
            Json.SerializeToUtf8Bytes( new JsonWebKeySet() )
        );
    }
}
