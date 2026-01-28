using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Jwks;

internal sealed class ApplicationBuilder : ITypeRegistrar
{
    public ApplicationBuilder()
    {
        Services.AddSingleton<ICancellationTokenProvider, CancellationTokenProvider>();
    }

    public IServiceCollection Services { get; } = new ServiceCollection();

    public ITypeResolver Build()
        => new ApplicationTypeResolver( Services.BuildServiceProvider() );

    public void Register( Type service, Type implementation )
        => Services.AddSingleton( service, implementation );

    public void RegisterInstance( Type service, object implementation )
        => Services.AddSingleton( service, implementation );

    public void RegisterLazy( Type service, Func<object> factory )
    {
        ArgumentNullException.ThrowIfNull( service );

        Services.AddSingleton( service, factory() );
    }
}
