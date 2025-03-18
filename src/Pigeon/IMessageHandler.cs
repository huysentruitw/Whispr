namespace Pigeon;

/// <summary>
/// Represents a message handler.
/// </summary>
/// <typeparam name="TMessage">The type of the message to handle.</typeparam>
public interface IMessageHandler<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Called when a message of type <typeparamref name="TMessage"/> is received.
    /// </summary>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    ValueTask Handle(Envelope<TMessage> envelope, CancellationToken cancellationToken);
}
