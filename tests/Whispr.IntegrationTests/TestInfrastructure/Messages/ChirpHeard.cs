namespace Whispr.IntegrationTests.TestInfrastructure.Messages;

public sealed record ChirpHeard(Guid BirdId, DateTime TimeUtc = default);
