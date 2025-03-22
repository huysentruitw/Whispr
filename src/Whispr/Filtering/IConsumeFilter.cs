namespace Whispr.Filtering;

/// <summary>
/// Represents a filter that is executed when right before a message is consumed.
/// </summary>
public interface IConsumeFilter
{
    /// <summary>
    /// Called right before a message is consumed.
    /// </summary>
    /// <param name="envelope">The message envelope.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns></returns>
    ValueTask Consume<TMessage>(Envelope<TMessage> envelope, Func<Envelope<TMessage>, CancellationToken, ValueTask> next, CancellationToken cancellationToken)
        where TMessage : class;
}
