using Whispr.Filtering;

namespace Whispr.IntegrationTests.TestInfrastructure.Filters;

public sealed class FailingSendFilter : ISendFilter
{
    public ValueTask Send(
        string topicName,
        SerializedEnvelope envelope,
        Func<string, SerializedEnvelope, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken)
    {
        if (envelope.MessageType == typeof(FeatherDropped).FullName)
            throw new InvalidOperationException("Sending FeatherDropped always fails");

        return next(topicName, envelope, cancellationToken);
    }
}
