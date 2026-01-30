using System.ComponentModel;
using Jwks.Store;
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
        if ( !JwksStore.TryGetValue( settings.Path, out var store ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Not initialized." );

            return 1;
        }

        Console.WriteLine( $"Store: {store}" );

        Console.WriteLine();
        if ( store.KeySet.Keys.Count == 0 )
        {
            AnsiConsole.MarkupLine( "[yellow]No keys found.[/]" );
        }
        else
        {
            AnsiConsole.MarkupLine( "Keys:" );
        }

        foreach ( var key in store.KeySet.Keys )
        {
            var name = string.Empty;
            if ( key.AdditionalData.TryGetValue( "name", out var nameObj ) && nameObj is string nameStr )
            {
                name = $", name: [blue]{nameStr}[/]";
            }

            AnsiConsole.MarkupLine( $"- kid: [green]{key.Kid[..16]}[/] ({key.Alg}){name}" );
        }

        Console.WriteLine();
        AnsiConsole.MarkupLine( $"Issuer: [green]{JwksDefaults.Issuer}[/]" );

        return 0;
    }
}
