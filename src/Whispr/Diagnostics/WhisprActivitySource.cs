using System.Diagnostics;

namespace Whispr.Diagnostics;

/// <summary>
/// Whispr activity source.
/// </summary>
public static class WhisprActivitySource
{
    /// <summary>
    /// Gets the name of the activity source for this library.
    /// </summary>
    public static string Name { get; } = typeof(WhisprActivitySource).Assembly.GetName().Name ?? "Whispr";

    private static string Version { get; } = typeof(WhisprActivitySource).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    internal static ActivitySource Source { get; } = new(Name, Version);
}
