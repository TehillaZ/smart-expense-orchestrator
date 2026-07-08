namespace ApprovalFlow.Reliability.Throttling;

public sealed class BulkheadGate : IDisposable
{
    private readonly SemaphoreSlim semaphore;

    public BulkheadGate(int maxConcurrency)
    {
        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        }

        semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }
}
