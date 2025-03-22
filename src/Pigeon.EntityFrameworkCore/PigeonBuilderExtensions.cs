using Microsoft.Extensions.DependencyInjection;
using Pigeon.Builder;
using Pigeon.EntityFrameworkCore.Cleaning;
using Pigeon.EntityFrameworkCore.Processing;
using Pigeon.Outbox;

namespace Pigeon.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Pigeon with Entity Framework Core integration.
/// </summary>
public static class PigeonBuilderExtensions
{
    /// <summary>
    /// Adds the EF Core outbox implementation to the <see cref="PigeonBuilder"/>.
    /// </summary>
    /// <param name="pigeonBuilder">The <see cref="PigeonBuilder"/>.</param>
    /// <param name="optionsAction">The action to configure the <see cref="OutboxOptions"/>.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddOutbox<TDbContext>(this PigeonBuilder pigeonBuilder, Action<OutboxOptions> optionsAction)
        where TDbContext : DbContext
    {
        pigeonBuilder.Services
            .Configure(optionsAction)
            .AddHostedService<OutboxProcessor<TDbContext>>()
            .AddSingleton<OutboxProcessorTrigger<TDbContext>>()
            .AddHostedService<OutboxCleanupService<TDbContext>>()
            .AddScoped<IOutbox, Outbox<TDbContext>>();

        return pigeonBuilder;
    }
}
