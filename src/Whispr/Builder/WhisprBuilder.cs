namespace Whispr.Builder;

/// <summary>
/// A builder for configuring Whispr.
/// </summary>
public sealed class WhisprBuilder
{
    internal WhisprBuilder(IServiceCollection services, string busName)
    {
        Services = services.AddKeyedSingleton<IEnumerable<MessageHandlerDescriptor>>(busName, MessageHandlerDescriptors);
        BusName = busName;
    }

    internal List<MessageHandlerDescriptor> MessageHandlerDescriptors { get; } = [];

    /// <summary>
    /// Gets the original <see cref="IServiceCollection"/> to continue configuring other services.
    /// </summary>
    public IServiceCollection Services { get; }
    
    /// <summary>
    /// Gets the name of the bus.
    /// </summary>
    public string BusName { get; }
}
