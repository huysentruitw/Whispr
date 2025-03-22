namespace Whispr.Builder;

/// <summary>
/// A builder for configuring Whispr.
/// </summary>
public sealed class WhisprBuilder
{
    internal WhisprBuilder(IServiceCollection services)
    {
        Services = services.AddSingleton<IEnumerable<MessageHandlerDescriptor>>(MessageHandlerDescriptors);
    }

    internal List<MessageHandlerDescriptor> MessageHandlerDescriptors { get; } = [];

    /// <summary>
    /// Gets the original <see cref="IServiceCollection"/> to continue configuring other services.
    /// </summary>
    public IServiceCollection Services { get; }
}
