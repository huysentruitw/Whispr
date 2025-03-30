using Microsoft.Extensions.Logging;
using Whispr.AzureServiceBus.Conventions;
using Whispr.AzureServiceBus.Management;

namespace Whispr.AzureServiceBus.Transport;

internal sealed partial class ServiceBusTransport(
    SenderFactory senderFactory,
    ProcessorFactory processorFactory,
    EntityManager entityManager,
    ISubscriptionNamingConvention subscriptionNamingConvention,
    IOptions<AzureServiceBusOptions> options,
    ILogger<ServiceBusTransport> logger) : ITransport
{
    private const string MessageTypePropertyName = "MessageType";
}
