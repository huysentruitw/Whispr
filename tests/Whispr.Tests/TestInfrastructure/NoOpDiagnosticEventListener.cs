using Whispr.Diagnostics;
using Whispr.Diagnostics.Scopes;

namespace Whispr.Tests.TestInfrastructure;

public sealed class NoOpDiagnosticsEventListener : IDiagnosticEventListener
{
    public IDisposable Start(string busName) => new EmptyScope();

    public IDisposable Publish<TMessage>(string busName, Envelope<TMessage> envelope) where TMessage : class => new EmptyScope();

    public IDisposable Send(string busName, string topicName, SerializedEnvelope envelope) => new EmptyScope();

    public IDisposable Consume(string busName, string handlerName, string queueName, SerializedEnvelope envelope) => new EmptyScope();
}
