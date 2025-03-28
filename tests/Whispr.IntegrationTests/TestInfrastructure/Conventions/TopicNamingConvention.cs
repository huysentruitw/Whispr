using Whispr.Conventions;

namespace Whispr.IntegrationTests.TestInfrastructure.Conventions;

public sealed class TopicNamingConvention : ITopicNamingConvention
{
    public string Format(Type messageType) => $"topic-{messageType.Name.ToLowerInvariant()}";
}
