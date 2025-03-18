namespace Pigeon.Conventions;

/// <summary>
/// Topic naming convention.
/// </summary>
public interface ITopicNamingConvention
{
    /// <summary>
    /// Format topic name based on message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The formatted topic name.</returns>
    string Format(Type messageType);
}
