namespace Whispr.Diagnostics;

/// <summary>
/// Represents a listener for diagnostic events.
/// </summary>
internal interface IDiagnosticEventListener
{
    /// <summary>
    /// Called when the message bus is started.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Start();

    /// <summary>
    /// Called when a message is published.
    /// </summary>
    /// <param name="envelope">The envelope containing the message being published.</param>
    /// <typeparam name="TMessage">The type of the message being published.</typeparam>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Publish<TMessage>(Envelope<TMessage> envelope)
        where TMessage : class;

    /// <summary>
    /// Called when a message is sent to the transport.
    /// </summary>
    /// <param name="topicName">The name of the destination topic.</param>
    /// <param name="envelope">The serialized envelope containing the message being sent.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Send(string topicName, SerializedEnvelope envelope);

    /// <summary>
    /// Called when a message is consumed.
    /// </summary>
    /// <param name="queueName">The name of the queue from which the message is consumed.</param>
    /// <param name="envelope">The serialized envelope containing the message being consumed.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Consume(string queueName, SerializedEnvelope envelope);
}
