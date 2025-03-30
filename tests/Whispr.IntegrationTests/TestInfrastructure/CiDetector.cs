namespace Whispr.IntegrationTests.TestInfrastructure;

public static class CiDetector
{
    public static bool IsCi() => bool.TryParse(Environment.GetEnvironmentVariable("CI"), out var ci) && ci;
}
