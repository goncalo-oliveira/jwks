using Spectre.Console.Cli;

namespace Jwks;

internal sealed class ApplicationTypeResolver( IServiceProvider provider ) : ITypeResolver, IDisposable
{
    public void Dispose()
    {
        if ( provider is IDisposable disposable )
        {
            disposable.Dispose();
        }
    }

    public object? Resolve(Type? type)
    {
        if ( type is null )
        {
            return null;
        }

        return provider.GetService( type );
    }
}
