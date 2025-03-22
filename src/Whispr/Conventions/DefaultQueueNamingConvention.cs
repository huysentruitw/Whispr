namespace Whispr.Conventions;

/// <summary>
/// Default queue naming convention.
/// </summary>
public sealed class DefaultQueueNamingConvention : IQueueNamingConvention
{
    private static readonly JsonNamingPolicy DefaultNamingPolicy = JsonNamingPolicy.KebabCaseLower;

    /// <inheritdoc />
    public string Format(Type handlerType)
        => DefaultNamingPolicy.ConvertName($"{handlerType.Namespace}.{handlerType.Name}".Replace(".", "--"));
}
