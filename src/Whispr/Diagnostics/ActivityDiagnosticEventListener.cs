using System.Diagnostics;
using Whispr.Diagnostics.Scopes;
using static Whispr.Diagnostics.WhisprActivitySource;

namespace Whispr.Diagnostics;

internal sealed class ActivityDiagnosticEventListener : IDiagnosticEventListener
{
    public IDisposable Start(string busName)
    {
        var activity = Source.CreateActivity(StartScope.ActivityName, ActivityKind.Internal);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new StartScope(activity)
            .WithBusName(busName);
    }

    public IDisposable Publish<TMessage>(string busName, Envelope<TMessage> envelope)
        where TMessage : class
    {
        var activity = Source.CreateActivity(PublishScope.ActivityName, ActivityKind.Internal);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new PublishScope(activity)
            .WithBusName(busName)
            .WithEnvelope(envelope);
    }

    public IDisposable Send(string busName, string topicName, SerializedEnvelope envelope)
    {
        var activity = Source.CreateActivity(SendScope.ActivityName, ActivityKind.Producer);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new SendScope(activity)
            .WithBusName(busName)
            .WithTopicName(topicName)
            .WithEnvelope(envelope);
    }

    public IDisposable Consume(string busName, string messageHandlerName, string queueName, SerializedEnvelope envelope)
    {
        var activity = Source.CreateActivity(ConsumeScope.ActivityName, ActivityKind.Consumer);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new ConsumeScope(activity)
            .WithBusName(busName)
            .WithHandlerName(messageHandlerName)
            .WithQueueName(queueName)
            .WithEnvelope(envelope);
    }
}
