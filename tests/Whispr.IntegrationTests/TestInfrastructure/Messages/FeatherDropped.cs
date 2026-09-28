namespace Whispr.IntegrationTests.TestInfrastructure.Messages;

// Sending this message always fails, see FailingSendFilter
public sealed record FeatherDropped(Guid FeatherId);
