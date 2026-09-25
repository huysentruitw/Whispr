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
    /// <param name="builder">The <see cref="WhisprBuilder"/>.</param>
    /// <param name="configureOptions">The action to configure the <see cref="OutboxOptions"/>.</param>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddOutbox<TDbContext>(this WhisprBuilder builder, Action<OutboxOptions>? configureOptions = null)
        where TDbContext : DbContext
    {
        EnsureDbContextNotUsedByOtherBus<TDbContext>(builder);

        var optionsName = $"Outbox_{builder.BusName}";
        builder.Services.Configure(optionsName, configureOptions ?? (_ => { }));

        builder.Services.TryAddSingleton<IDiagnosticEventListener, ActivityDiagnosticEventListener>();
        
        builder.Services.TryAddKeyedSingleton<OutboxProcessorTrigger<TDbContext>>(builder.BusName);

        builder.Services.AddHostedService(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<OutboxOptions>>().Get(optionsName);
            
            return new OutboxProcessor<TDbContext>(
                builder.BusName,
                serviceProvider.GetRequiredKeyedService<OutboxProcessorTrigger<TDbContext>>(builder.BusName),
                serviceProvider.GetRequiredKeyedService<IMessageSender>(builder.BusName),
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                options,
                serviceProvider.GetRequiredService<IDiagnosticEventListener>(),
                serviceProvider.GetRequiredService<ILogger<OutboxProcessor<TDbContext>>>());
        });

        builder.Services.AddHostedService(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<OutboxOptions>>().Get(optionsName);

            return new OutboxCleanupService<TDbContext>(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                options,
                serviceProvider.GetRequiredService<ILogger<OutboxCleanupService<TDbContext>>>());
        });

        builder.Services.TryAddKeyedScoped<IOutbox>(
            builder.BusName,
            (serviceProvider, key) => new Outbox<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetRequiredKeyedService<OutboxProcessorTrigger<TDbContext>>(key)));

        return builder;
    }

    private static void EnsureDbContextNotUsedByOtherBus<TDbContext>(WhisprBuilder builder)
        where TDbContext : DbContext
    {
        // The outbox table has no bus discriminator, so sharing a DbContext between buses
        // would make the outbox processors pick up (and send) each other's messages.
        var otherBusName = builder.Services
            .Where(descriptor => !descriptor.IsKeyedService && descriptor.ServiceType == typeof(OutboxRegistration<TDbContext>))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<OutboxRegistration<TDbContext>>()
            .Select(registration => registration.BusName)
            .FirstOrDefault(busName => busName != builder.BusName);

        if (otherBusName is not null)
        {
            throw new InvalidOperationException(
                $"DbContext '{typeof(TDbContext).Name}' is already used by the outbox of bus '{otherBusName}'. " +
                $"Each bus requires its own DbContext for the outbox (bus '{builder.BusName}').");
        }

        builder.Services.AddSingleton(new OutboxRegistration<TDbContext>(builder.BusName));
    }

    private sealed record OutboxRegistration<TDbContext>(string BusName)
        where TDbContext : DbContext;
}
