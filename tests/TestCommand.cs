using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace tests;

internal static class TestCommand
{
    private static readonly string[] root = ["cmd"];

    public static int Run<TCommand>( string[] args ) where TCommand : class, ICommand
    {
        var console = new TestConsole();
        var app = new CommandApp();

        app.Configure( config =>
        {
            config.AddCommand<TCommand>( root[0] );
        } );

        return app.Run( [.. root, .. args] );
    }
}
