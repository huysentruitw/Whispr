namespace Whispr.EntityFrameworkCore.Processing;

internal sealed class OutboxProcessorTrigger<TDbContext>
    where TDbContext : DbContext
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public async ValueTask Wait(TimeSpan timeout, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(timeout, cancellationToken);
    }

    public void Release()
    {
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }
}
