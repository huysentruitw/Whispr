namespace Whispr.Builder;

/// <summary>
/// A builder for configuring Whispr.
/// </summary>
public sealed class WhisprBuilder
{
    internal WhisprBuilder(IServiceCollection services, string busName)
    {
        Services = services;
        BusName = busName;
    }

    internal List<MessageHandlerDescriptor> MessageHandlerDescriptors { get; } = [];

    /// <summary>
    /// Gets the name of the bus being configured.
    /// </summary>
    public string BusName { get; }

    /// <summary>
    /// Gets the original <see cref="IServiceCollection"/> to continue configuring other services.
    /// </summary>
    public IServiceCollection Services { get; }
}
