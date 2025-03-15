using Pigeon.Conventions;

namespace Pigeon.AzureServiceBus.Tests.Conventions;

public sealed class TopicNamingConvention : ITopicNamingConvention
{
    public string Format(Type messageType) => $"topic-{messageType.Name.ToLowerInvariant()}";
}
