namespace Whispr.Builder;

/// <summary>
/// Registry for managing multiple bus configurations.
/// </summary>
public interface IBusRegistry
{
    /// <summary>
    /// Registers a bus configuration.
    /// </summary>
    /// <param name="busName">The name of the bus.</param>
    /// <param name="configuration">The bus configuration.</param>
    void Register(string busName, BusConfiguration configuration);

    /// <summary>
    /// Gets a bus configuration by name.
    /// </summary>
    /// <param name="busName">The name of the bus.</param>
    /// <returns>The bus configuration.</returns>
    BusConfiguration Get(string busName);

    /// <summary>
    /// Gets all registered bus names.
    /// </summary>
    /// <returns>The bus names.</returns>
    IEnumerable<string> GetBusNames();

    /// <summary>
    /// Gets all registered bus configurations.
    /// </summary>
    /// <returns>The bus configurations.</returns>
    IEnumerable<BusConfiguration> GetAllConfigurations();
}
