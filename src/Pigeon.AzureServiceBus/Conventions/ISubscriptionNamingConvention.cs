namespace Pigeon.AzureServiceBus.Conventions;

public interface ISubscriptionNamingConvention
{
    string Format(string queueName);
}
