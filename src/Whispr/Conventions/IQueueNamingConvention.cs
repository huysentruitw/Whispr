namespace Whispr.Conventions;

/// <summary>
/// Queue naming convention.
/// </summary>
public interface IQueueNamingConvention
{
    /// <summary>
    /// Format the queue name based on the handler type.
    /// </summary>
    /// <param name="handlerType">The handler type.</param>
    /// <returns>The formatted queue name.</returns>
    string Format(Type handlerType);
}
