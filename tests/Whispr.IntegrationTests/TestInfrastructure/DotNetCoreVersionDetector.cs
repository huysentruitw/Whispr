namespace Whispr.IntegrationTests.TestInfrastructure;

public static class DotNetCoreVersionDetector
{
    public static int GetMajorVersion()
        => typeof(object).Assembly.GetName().Version!.Major;
}
