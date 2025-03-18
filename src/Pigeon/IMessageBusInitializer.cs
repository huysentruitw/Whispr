namespace Pigeon;

public interface IMessageBusInitializer
{
    ValueTask Start(CancellationToken cancellationToken = default);
}
