namespace Whispr.Builder;

/// <summary>
/// Registry for managing multiple bus configurations.
/// </summary>
internal sealed class BusRegistry : IBusRegistry
{
    private readonly Dictionary<string, BusConfiguration> _buses = new();
    private readonly object _lock = new();

    public void Register(string busName, BusConfiguration configuration)
    {
        lock (_lock)
        {
            if (_buses.ContainsKey(busName))
                throw new InvalidOperationException($"A bus with name '{busName}' is already registered.");

            _buses[busName] = configuration;
        }
    }

    public BusConfiguration Get(string busName)
    {
        lock (_lock)
        {
            if (!_buses.TryGetValue(busName, out var configuration))
                throw new InvalidOperationException($"No bus with name '{busName}' is registered.");

            return configuration;
        }
    }

    public IEnumerable<string> GetBusNames()
    {
        lock (_lock)
        {
            return _buses.Keys.ToArray();
        }
    }

    public IEnumerable<BusConfiguration> GetAllConfigurations()
    {
        lock (_lock)
        {
            return _buses.Values.ToArray();
        }
    }
}
