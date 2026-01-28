using Jwks;
using Jwks.Commands;
using Spectre.Console.Cli;

var builder = new ApplicationBuilder();

var app = new CommandApp( builder );

app.Configure( config =>
{
    config.SetApplicationVersion( AppVersion.Value );

    // config.AddBranch( "branch", config =>
    // {
    //     config.SetDescription( "Branch description." );
    //
    //     config.AddCommand<SampleCommand>( "sample" )
    //         .WithDescription( "Sample command description." )
    //         .WithExample( ["sample", "--foo", "bar"] );
    //
    // } );

    config.AddCommand<InitCommand>( "init" )
        .WithDescription( "Initialize a new JWKS." )
        .WithExample( ["init"] );

    config.AddCommand<KeygenCommand>( "keygen" )
        .WithDescription( "Generate a new key and add it to the JWKS." )
        .WithExample( ["keygen", "--name", "my-key"] );

    config.AddCommand<KeyremCommand>( "keyrm" )
        .WithDescription( "Remove a key from the JWKS." )
        .WithExample( ["keyrm", "your-key-id"] );

    config.AddCommand<StatusCommand>( "status" )
        .WithDescription( "Show the status of the JWKS." )
        .WithExample( ["status"] );

    config.AddCommand<TokenCommand>( "token" )
        .WithDescription( "Issue a JWT token from a local JWKS." )
        .WithExample( ["token", "--dry-run"] );
} );

if ( Console.IsOutputRedirected )
{
    Spectre.Console.AnsiConsole.Profile.Capabilities.Ansi = false;
}

return app.Run( args );
