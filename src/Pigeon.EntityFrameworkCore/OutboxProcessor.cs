using Microsoft.Extensions.Hosting;

namespace Pigeon.EntityFrameworkCore;

internal sealed class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
