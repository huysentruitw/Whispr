using Pigeon.Conventions;

namespace Pigeon.IntegrationTests.Tests.Conventions;

public sealed class QueueNamingConvention : IQueueNamingConvention
{
    public string Format(Type handlerType) => $"queue-{handlerType.Name.ToLowerInvariant()}";
}
