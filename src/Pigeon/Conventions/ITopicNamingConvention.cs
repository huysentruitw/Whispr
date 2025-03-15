namespace Pigeon.Conventions;

public interface ITopicNamingConvention
{
    string Format(Type messageType);
}
