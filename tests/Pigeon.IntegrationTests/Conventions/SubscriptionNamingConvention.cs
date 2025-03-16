using Pigeon.AzureServiceBus.Conventions;

namespace Pigeon.IntegrationTests.Tests.Conventions;

public sealed class SubscriptionNamingConvention : ISubscriptionNamingConvention
{
    public string Format(string queueName)
        => $"sub-{queueName.Replace("queue-", string.Empty)}";
}
