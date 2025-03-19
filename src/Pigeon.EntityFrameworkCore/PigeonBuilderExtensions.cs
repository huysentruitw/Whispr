using Microsoft.Extensions.DependencyInjection;

namespace Pigeon.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Pigeon with Entity Framework Core integration.
/// </summary>
public static class PigeonBuilderExtensions
{
    /// <summary>
    /// Adds the outbox send filter to the <see cref="PigeonBuilder"/>.
    /// </summary>
    /// <param name="pigeonBuilder">The <see cref="PigeonBuilder"/>.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddOutboxSendFilter<TDbContext>(this PigeonBuilder pigeonBuilder)
        where TDbContext : DbContext
    {
        pigeonBuilder.Services
            .AddHostedService<OutboxProcessor<TDbContext>>()
            .AddSingleton<OutboxProcessorTrigger<TDbContext>>();

        return pigeonBuilder.AddSendFilter<OutboxSendFilter<TDbContext>>();
    }
}
