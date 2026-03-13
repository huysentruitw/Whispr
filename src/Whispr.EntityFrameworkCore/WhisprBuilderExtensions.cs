using Microsoft.Extensions.DependencyInjection.Extensions;
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
        // Configure options with a unique key per bus
        var optionsName = $"Outbox_{whisprBuilder.BusName}";
        if (optionsAction is not null)
            whisprBuilder.Services.Configure<OutboxOptions>(optionsName, optionsAction);

        // Register per-bus outbox services
        whisprBuilder.Services.TryAddKeyedSingleton<OutboxProcessorTrigger<TDbContext>>(whisprBuilder.BusName);
        
        // Register hosted service for outbox processor (one per bus)
        whisprBuilder.Services.AddSingleton<IHostedService>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsSnapshot<OutboxOptions>>().Get(optionsName);
            var logger = sp.GetRequiredService<ILogger<OutboxProcessor<TDbContext>>>();
            var trigger = sp.GetRequiredKeyedService<OutboxProcessorTrigger<TDbContext>>(whisprBuilder.BusName);
            var sender = sp.GetRequiredKeyedService<IMessageSender>(whisprBuilder.BusName);
            
            return new OutboxProcessor<TDbContext>(
                sp,
                Options.Create(options),
                logger,
                trigger,
                sender,
                whisprBuilder.BusName);
        });

        // Register hosted service for outbox cleanup (one per bus)
        whisprBuilder.Services.AddSingleton<IHostedService>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsSnapshot<OutboxOptions>>().Get(optionsName);
            var logger = sp.GetRequiredService<ILogger<OutboxCleanupService<TDbContext>>>();
            
            return new OutboxCleanupService<TDbContext>(
                Options.Create(options),
                logger,
                sp);
        });

        whisprBuilder.Services.TryAddKeyedSingleton<IDiagnosticEventListener, ActivityDiagnosticEventListener>(whisprBuilder.BusName);
        whisprBuilder.Services.TryAddScoped<IOutbox, Outbox<TDbContext>>();

        return whisprBuilder;
    }
}
