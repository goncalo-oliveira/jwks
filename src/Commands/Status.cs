using System.ComponentModel;
using Jwks.Utils;
using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Jwks.Commands;

public sealed class StatusCommand : Command<StatusCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption( "--jwks-path" )]
        [Description( "Path to the JWKS source." )]
        public string? Path { get; set; }
    }

    public override int Execute( CommandContext context, Settings settings, CancellationToken cancellationToken )
    {
        var path = JwksDefaults.GetJwksSourcePath( settings.Path );
        
        if ( string.IsNullOrEmpty( path ) || !File.Exists( Path.Combine( path, "jwks.json" ) ) )
        {
            AnsiConsole.MarkupLine( "[red]Error:[/] Not initialized." );
            
            return 1;
        }

        var json = File.ReadAllText( Path.Combine( path, "jwks.json" ) );
        var jwks = JsonWebKeySet.Create( json );

        if ( jwks == null )
        {
            AnsiConsole.MarkupLine( "[red]Error:[/] Failed to parse JWKS file." );
            return 1;
        }

        Console.WriteLine( $"Source: {PathHelper.ShrinkHomePath( path )}" );

        Console.WriteLine();
        if ( jwks.Keys.Count == 0 )
        {
            AnsiConsole.MarkupLine( "[yellow]No keys found.[/]" );
        }
        else
        {
            AnsiConsole.MarkupLine( "Keys:" );
        }

        foreach ( var key in jwks.Keys )
        {
            var name = string.Empty;
            if ( key.AdditionalData.TryGetValue( "name", out var nameObj ) && nameObj is string nameStr )
            {
                name = $", name: [blue]{nameStr}[/]";
            }

            AnsiConsole.MarkupLine( $"- kid: [green]{key.Kid[..12]}[/]... ({key.Alg}){name}" );
        }

        Console.WriteLine();
        AnsiConsole.MarkupLine( $"Issuer: [green]{JwksDefaults.Issuer}[/]" );

        return 0;
    }
}
