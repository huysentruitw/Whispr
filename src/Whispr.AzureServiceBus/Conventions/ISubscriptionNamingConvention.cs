namespace Whispr.AzureServiceBus.Conventions;

/// <summary>
/// Subscription naming convention.
/// </summary>
public interface ISubscriptionNamingConvention
{
    /// <summary>
    /// Format the subscription name for the specified queue name.
    /// </summary>
    /// <param name="queueName">The name of the queue.</param>
    /// <returns>The formatted subscription name.</returns>
    string Format(string queueName);
}
