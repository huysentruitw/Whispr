using Pigeon.AzureServiceBus.Conventions;

namespace Pigeon.AzureServiceBus.Tests.Conventions;

public sealed class SubscriptionNamingConvention : ISubscriptionNamingConvention
{
    public string Format(string queueName)
        => $"sub-{queueName.Replace("queue-", string.Empty)}";
}
