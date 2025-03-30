using Whispr.Conventions;

namespace Whispr.IntegrationTests.TestInfrastructure.Conventions;

public sealed class QueueNamingConvention : IQueueNamingConvention
{
    public string Format(Type handlerType) => $"queue{DotNetCoreVersionDetector.GetMajorVersion()}-{handlerType.Name.ToLowerInvariant()}";
}
