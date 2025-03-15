namespace Pigeon.Conventions;

public sealed class DefaultQueueNamingConvention : IQueueNamingConvention
{
    private static readonly JsonNamingPolicy DefaultNamingPolicy = JsonNamingPolicy.KebabCaseLower;

    public string Format(Type handlerType)
        => DefaultNamingPolicy.ConvertName($"{handlerType.Namespace}.{handlerType.Name}".Replace(".", "--"));
}
