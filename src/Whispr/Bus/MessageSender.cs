namespace Whispr.Bus;

internal sealed class MessageSender(
    IServiceProvider serviceProvider,
    ITransport transport) : IMessageSender
{
    public ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sendFilters = scope.ServiceProvider.GetServices<ISendFilter>().ToArray();

        // Build the sending pipeline
        Func<string, SerializedEnvelope, CancellationToken, ValueTask> pipeline = transport.Send;
        foreach (var sendFilter in sendFilters.Reverse())
        {
            var next = pipeline;
            pipeline = (t, e, ct) => sendFilter.Send(t, e, next, ct);
        }

        // Execute the sending pipeline
        return pipeline(topicName, envelope, cancellationToken);
    }
}
