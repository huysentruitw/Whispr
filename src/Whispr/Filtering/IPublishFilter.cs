namespace Whispr.Filtering;

/// <summary>
/// Represents a filter that is executed when a message is published.
/// </summary>
public interface IPublishFilter
{
    /// <summary>
    /// Called when a message is published.
    /// </summary>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TMessage">The message type.</typeparam>
    ValueTask Publish<TMessage>(Envelope<TMessage> envelope, Func<Envelope<TMessage>, CancellationToken, ValueTask> next, CancellationToken cancellationToken)
        where TMessage : class;
}
