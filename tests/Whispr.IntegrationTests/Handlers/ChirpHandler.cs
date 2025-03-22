using System.Collections.Concurrent;
using Whispr.IntegrationTests.Tests.Messages;

namespace Whispr.IntegrationTests.Tests.Handlers;

internal sealed class ChirpHandler
    : IMessageHandler<ChirpHeard>
{
    private static readonly ConcurrentBag<object> HandledMessages = [];

    public ValueTask Handle(Envelope<ChirpHeard> envelope, CancellationToken cancellationToken)
    {
        HandledMessages.Add(envelope.Message);
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
}
