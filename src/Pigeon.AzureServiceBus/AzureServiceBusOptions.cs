namespace Pigeon.AzureServiceBus;

public sealed record AzureServiceBusOptions
{
    public string ConnectionString { get; set; } = null!;
}
