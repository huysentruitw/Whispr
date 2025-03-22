namespace Whispr.IntegrationTests.Tests.Messages;

public sealed record ChirpHeard(string BirdName, TimeSpan Duration);
