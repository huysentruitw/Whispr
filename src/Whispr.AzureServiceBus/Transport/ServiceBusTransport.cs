using Whispr.AzureServiceBus.Conventions;
using Whispr.AzureServiceBus.Factories;

namespace Whispr.AzureServiceBus.Transport;

internal sealed partial class ServiceBusTransport(
    SenderFactory senderFactory,
    ProcessorFactory processorFactory,
    EntityManager entityManager,
    ISubscriptionNamingConvention subscriptionNamingConvention) : ITransport
{
    private const string MessageTypePropertyName = "MessageType";
}
