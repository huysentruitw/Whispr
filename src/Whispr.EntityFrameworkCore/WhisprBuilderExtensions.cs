using Whispr.Builder;
using Whispr.EntityFrameworkCore.Cleaning;

namespace Whispr.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Whispr with Entity Framework Core integration.
/// </summary>
public static class WhisprBuilderExtensions
{
    /// <summary>
    /// Adds the EF Core outbox implementation to the <see cref="WhisprBuilder"/>.
    /// </summary>
    /// <param name="whisprBuilder">The <see cref="WhisprBuilder"/>.</param>
    /// <param name="optionsAction">The action to configure the <see cref="OutboxOptions"/>.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddOutbox<TDbContext>(this WhisprBuilder whisprBuilder, Action<OutboxOptions>? optionsAction = null)
        where TDbContext : DbContext
    {
        if (optionsAction is not null)
            whisprBuilder.Services.Configure(optionsAction);

        whisprBuilder.Services
            .AddHostedService<OutboxProcessor<TDbContext>>()
            .AddSingleton<OutboxProcessorTrigger<TDbContext>>()
            .AddSingleton<IDiagnosticEventListener, ActivityDiagnosticEventListener>()
            .AddHostedService<OutboxCleanupService<TDbContext>>()
            .AddScoped<IOutbox, Outbox<TDbContext>>();

        return whisprBuilder;
    }
}
