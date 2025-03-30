using System.Collections.Concurrent;

namespace Whispr.IntegrationTests.TestInfrastructure.Handlers;

internal sealed class NestHandler
    : IMessageHandler<NestBuilt>
    , IMessageHandler<NestAbandoned>
{
    private static readonly ConcurrentBag<object> HandledMessages = [];
    private static readonly ConcurrentDictionary<Guid, int> MessageDeliveryCount = [];

    public ValueTask Handle(Envelope<NestBuilt> envelope, CancellationToken cancellationToken)
    {
        HandledMessages.Add(envelope.Message);
        var count = MessageDeliveryCount.GetOrAdd(envelope.Message.NestId, 1);
        MessageDeliveryCount[envelope.Message.NestId] = count + 1;
        throw new InvalidOperationException();
    }

    public ValueTask Handle(Envelope<NestAbandoned> envelope, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public static TMessage? WaitForMessage<TMessage>(Func<TMessage, bool> predicate, TimeSpan timeout)
        where TMessage : class
    {
        var cts = new CancellationTokenSource(timeout);

        while (!cts.Token.IsCancellationRequested)
        {
            if (HandledMessages.OfType<TMessage>().FirstOrDefault(predicate) is { } message)
                return message;

            Thread.Sleep(10);
        }

        return null;
    }

    public static int? GetMessageDeliveryCount(Guid birdId)
    {
        return MessageDeliveryCount.TryGetValue(birdId, out var count) ? count : null;
    }
}
