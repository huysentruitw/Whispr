using System.Collections.Concurrent;

namespace Whispr.IntegrationTests.TestInfrastructure.Handlers;

internal sealed class ChirpHandler
    : IMessageHandler<ChirpHeard>
{
    private static readonly ConcurrentBag<object> HandledMessages = [];
    private static readonly ConcurrentDictionary<Guid, DateTime> MessageProcessedTimes = [];

    public ValueTask Handle(Envelope<ChirpHeard> envelope, CancellationToken cancellationToken)
    {
        HandledMessages.Add(envelope.Message);
        MessageProcessedTimes[envelope.Message.BirdId] = DateTime.UtcNow;
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

    public static DateTime? GetMessageProcessedTime(Guid birdId)
    {
        return MessageProcessedTimes.TryGetValue(birdId, out var time) ? time : null;
    }
}
