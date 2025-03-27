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
    /// The host name of the Azure Service Bus to used with managed identity.
    /// </summary>
    public string? HostName { get; set; } = null!;

    /// <summary>
    /// The token credential to use for managed identity authentication.
    /// This is used when <see cref="HostName"/> is set.
    /// When not set, the default Azure credential is used.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; } = null!;
}
