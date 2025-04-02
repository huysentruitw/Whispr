using Whispr.Diagnostics;
using Whispr.Diagnostics.Scopes;

namespace Whispr.Tests.TestInfrastructure;

public sealed class NoOpDiagnosticsEventListener : IDiagnosticEventListener
{
    public IDisposable Start() => new EmptyScope();

    public IDisposable Publish<TMessage>(Envelope<TMessage> envelope) where TMessage : class => new EmptyScope();

    public IDisposable Send(string topicName, SerializedEnvelope envelope) => new EmptyScope();

    public IDisposable Consume(string handlerName, string queueName, SerializedEnvelope envelope) => new EmptyScope();
}
