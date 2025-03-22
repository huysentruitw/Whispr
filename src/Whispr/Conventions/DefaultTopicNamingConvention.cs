namespace Whispr.Conventions;

/// <summary>
/// Default topic naming convention.
/// </summary>
public sealed class DefaultTopicNamingConvention : ITopicNamingConvention
{
    private static readonly JsonNamingPolicy DefaultNamingPolicy = JsonNamingPolicy.KebabCaseLower;

    /// <inheritdoc />
    public string Format(Type messageType)
        => DefaultNamingPolicy.ConvertName($"{messageType.Namespace}.{messageType.Name}".Replace(".", "--"));
}
