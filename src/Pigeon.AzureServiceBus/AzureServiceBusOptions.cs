namespace Pigeon.AzureServiceBus;

/// <summary>
/// Options for Azure Service Bus.
/// </summary>
public sealed record AzureServiceBusOptions
{
    /// <summary>
    /// The connection string to the Azure Service Bus.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
}
