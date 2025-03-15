namespace Pigeon.Conventions;

public sealed class DefaultTopicNamingConvention : ITopicNamingConvention
{
    private static readonly JsonNamingPolicy DefaultNamingPolicy = JsonNamingPolicy.KebabCaseLower;

    public string Format(Type messageType)
        => DefaultNamingPolicy.ConvertName($"{messageType.Namespace}.{messageType.Name}".Replace(".", "--"));
}
