using System.Text.Json;

namespace Whispr.Tests.TestInfrastructure;

public static class SerializedEnvelopeFactory
{
    public static SerializedEnvelope Create<TMessage>(TMessage message)
        where TMessage : class
    {
        var envelope = new Envelope<TMessage>
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Message = message,
            MessageType = typeof(TMessage).FullName!,
            Headers = [],
            DestinationTopicName = "topic-abc",
            CorrelationId = Guid.NewGuid().ToString("N"),
            DeferredUntil = null,
        };

        return new SerializedEnvelope
        {
            Body = JsonSerializer.Serialize(envelope),
            MessageType = envelope.MessageType,
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            DeferredUntil = envelope.DeferredUntil,
        };
    }
}
