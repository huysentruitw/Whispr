namespace Whispr.Bus;

internal sealed class MessageSender(
    IServiceProvider serviceProvider,
    ITransport transport,
    IDiagnosticEventListener diagnosticEventListener,
    string busName) : IMessageSender
{
    public ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken)
    {
        using var _ = diagnosticEventListener.Send(topicName, envelope);

        using var scope = serviceProvider.CreateScope();
        var sendFilters = scope.ServiceProvider.GetKeyedServices<ISendFilter>(busName).ToArray();

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
