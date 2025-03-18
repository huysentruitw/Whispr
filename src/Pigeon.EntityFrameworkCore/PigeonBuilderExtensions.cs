using Microsoft.Extensions.DependencyInjection;

namespace Pigeon.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Pigeon with Entity Framework Core integration.
/// </summary>
public static class PigeonBuilderExtensions
{
    /// <summary>
    /// Adds Entity Framework Core integration to Pigeon.
    /// </summary>
    /// <param name="pigeonBuilder">The <see cref="PigeonBuilder"/>.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddEntityFrameworkCoreIntegration<TDbContext>(this PigeonBuilder pigeonBuilder)
        where TDbContext : DbContext
    {
        pigeonBuilder.AddSendFilter<OutboxSendFilter<TDbContext>>();
        pigeonBuilder.Services.AddHostedService<OutboxProcessor<TDbContext>>();
        return pigeonBuilder;
    }
}
