using Whispr.Diagnostics;

namespace Whispr.Tests.TestInfrastructure;

public sealed class NoOpDiagnosticsEventListener : IDiagnosticEventListener
{
    public IDisposable Start() => new EmptyScope();

    public IDisposable Publish() => new EmptyScope();

    public IDisposable Send() => new EmptyScope();

    public IDisposable Consume() => new EmptyScope();

    public IDisposable OutboxProcess() => new EmptyScope();

    private sealed class EmptyScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
