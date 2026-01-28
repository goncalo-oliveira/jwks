namespace Jwks;

public interface ICancellationTokenProvider
{
    CancellationToken Token { get; }

    void Cancel();
}

public sealed class CancellationTokenProvider : ICancellationTokenProvider, IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public CancellationTokenProvider()
    {
        Console.CancelKeyPress += ( sender, e ) =>
        {
            e.Cancel = true; // Prevent the process from terminating.
            Cancel();
        };
    }

    public CancellationToken Token => cancellationTokenSource.Token;

    public void Cancel()
    {
        cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        cancellationTokenSource.Dispose();
    }
}
