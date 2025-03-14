namespace Pigeon;

public sealed class PigeonBuilder
{
    public PigeonBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}
