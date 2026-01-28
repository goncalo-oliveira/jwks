using Spectre.Console;

namespace Jwks;

internal static class StdErr
{
    private static readonly Lazy<IAnsiConsole> stderr =  new( () => AnsiConsole.Create(
        new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput( System.Console.Error )
        }
    ) );

    /// <summary>
    /// Gets the standard error console.
    /// </summary>
    public static IAnsiConsole Out => stderr.Value;
}
