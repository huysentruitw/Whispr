namespace Pigeon.EntityFrameworkCore;

internal sealed class OutboxProcessorTrigger<TDbContext>
    where TDbContext : DbContext
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async ValueTask Wait(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
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
