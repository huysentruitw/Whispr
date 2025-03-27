namespace Whispr.AzureServiceBus;

/// <summary>
/// Options for Azure Service Bus.
/// </summary>
public sealed record AzureServiceBusOptions
{
    /// <summary>
    /// The connection string to the Azure Service Bus.
    /// </summary>
    public string? ConnectionString { get; set; } = null!;

    /// <summary>
    /// The host address of the Azure Service Bus to used with managed identity.
    /// </summary>
    public string? HostAddress { get; set; } = null!;
}
