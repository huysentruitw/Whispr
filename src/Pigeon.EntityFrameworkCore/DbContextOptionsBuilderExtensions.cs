namespace Pigeon.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring <see cref="DbContextOptionsBuilder"/> with outbox support.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="DbContextOptionsBuilder"/> with outbox support.
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder"/>.</param>
    /// <returns>The <see cref="DbContextOptionsBuilder"/>.</returns>
    public static DbContextOptionsBuilder UseOutbox(this DbContextOptionsBuilder optionsBuilder)
    {
        return optionsBuilder.AddInterceptors(new OutboxSaveChangesInterceptor());
    }
}
