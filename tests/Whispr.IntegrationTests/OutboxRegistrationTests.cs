namespace Whispr.IntegrationTests;

public sealed class OutboxRegistrationTests
{
    [Fact]
    public void Given_DbContextUsedByOutboxOfOtherBus_When_AddOutbox_Then_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWhispr("BusA").AddOutbox<DataContext>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddWhispr("BusB").AddOutbox<DataContext>());

        Assert.Contains("BusA", exception.Message);
    }

    [Fact]
    public void Given_DifferentDbContextPerBus_When_AddOutbox_Then_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWhispr("BusA").AddOutbox<DataContext>();

        // Act
        var exception = Record.Exception(() => services.AddWhispr("BusB").AddOutbox<OtherDataContext>());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Given_DbContextUsedByOutboxOfSameBus_When_AddOutbox_Then_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddWhispr("BusA").AddOutbox<DataContext>();

        // Act
        var exception = Record.Exception(() => builder.AddOutbox<DataContext>());

        // Assert
        Assert.Null(exception);
    }

    private sealed class OtherDataContext(DbContextOptions<OtherDataContext> options) : DbContext(options);
}
