using System.ComponentModel;
using Jwks.Store;
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

        if ( !JwksStore.TryGetValue( settings.Path, out var store ) )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Not initialized." );

            return 1;
        }

        Console.WriteLine( $"Store: {store}" );
        Console.WriteLine();

        var keys = store.SelectKeys( settings.Kid );

        if ( keys.Length == 0 )
        {
            StdErr.Out.MarkupLine( "[red]Error:[/] Key(s) not found." );

            return 1;
        }

        if ( !settings.Yes )
        {
            AnsiConsole.MarkupLine( $"The following key(s) will be removed:" );

            foreach ( var key in keys )
            {
                AnsiConsole.MarkupLine( $"- [yellow]{key.Kid[..16]}[/]" );
            }

            if ( !AnsiConsole.Confirm( "Continue?", false ) )
            {
                return 1;
            }
        }


        store.RemoveKeys( keys );

        foreach ( var key in keys )
        {
            AnsiConsole.MarkupLine( $"[green]✔[/] Key removed (kid=[green]{key.Kid[..16]}[/])" );
        }

        return 0;
    }

    [System.Text.RegularExpressions.GeneratedRegex( @"^[a-zA-Z0-9_-]+$" )]
    private static partial System.Text.RegularExpressions.Regex KeyNameRegex();
}
