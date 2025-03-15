using Pigeon.Conventions;

namespace Pigeon.AzureServiceBus.Tests.Conventions;

public sealed class QueueNamingConvention : IQueueNamingConvention
{
    public string Format(Type handlerType) => $"queue-{handlerType.Name.ToLowerInvariant()}";
}
