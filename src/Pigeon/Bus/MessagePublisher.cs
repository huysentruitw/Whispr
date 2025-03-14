using System.Text.Json;

namespace Pigeon.Bus;

internal sealed class MessagePublisher(
    IEnumerable<IPublishFilter> publishFilters,
    IEnumerable<ISendFilter> sendFilters,
    ITransport transport)
{
    public Task Publish<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        var envelope = new Envelope<TMessage>
        {
            Message = message,
        };

        // Build the publishing pipeline
        Func<Envelope<TMessage>, CancellationToken, Task> first = Send;
        foreach (var publishFilter in publishFilters.Reverse())
        {
            var next = first;
            first = (e, ct) => publishFilter.Publish(e, next, ct);
        }

        // Execute the publishing pipeline
        return first(envelope, cancellationToken);
    }

    private Task Send<TMessage>(Envelope<TMessage> envelope, CancellationToken cancellationToken)
        where TMessage : class
    {
        var serializedEnvelope = new SerializedEnvelope
        {
            Message = JsonSerializer.Serialize(envelope.Message),
        };

        // Build the sending pipeline
        Func<SerializedEnvelope, CancellationToken, Task> first = transport.Send;
        foreach (var sendFilter in sendFilters.Reverse())
        {
            var next = first;
            first = (e, ct) => sendFilter.Send(e, next, ct);
        }

        // Execute the sending pipeline
        return first(serializedEnvelope, cancellationToken);
    }
}
