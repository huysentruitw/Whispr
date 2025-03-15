namespace Pigeon.Conventions;

public interface IQueueNamingConvention
{
    string Format(Type handlerType);
}
