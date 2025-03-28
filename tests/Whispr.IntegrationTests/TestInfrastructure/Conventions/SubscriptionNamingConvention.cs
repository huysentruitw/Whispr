using Whispr.AzureServiceBus.Conventions;

namespace Whispr.IntegrationTests.TestInfrastructure.Conventions;

public sealed class SubscriptionNamingConvention : ISubscriptionNamingConvention
{
    public string Format(string queueName)
        => $"sub-{queueName.Replace("queue-", string.Empty)}";
}
