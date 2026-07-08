using System.Threading.RateLimiting;

namespace ApprovalFlow.Reliability.Throttling;

public sealed class RateLimitedGate : IDisposable
{
    private readonly RateLimiter rateLimiter;

    public RateLimitedGate(int permitLimit, TimeSpan window)
    {
        rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            Window = window
        });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("Rate limit exceeded.");
        }

        return await action();
    }

    public void Dispose()
    {
        rateLimiter.Dispose();
    }
}
