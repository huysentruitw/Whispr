namespace Pigeon.AzureServiceBus.Tests.Messages;

public sealed record ChirpHeard(string BirdName, TimeSpan Duration);
