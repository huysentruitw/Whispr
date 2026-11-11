namespace Whispr.EntityFrameworkCore;

/// <summary>
/// The SQL dialect of the database that holds the outbox table.
/// </summary>
public enum SqlDialect
{
    /// <summary>
    /// Microsoft SQL Server and Azure SQL.
    /// </summary>
    MsSql = 0,

    /// <summary>
    /// PostgreSQL.
    /// </summary>
    PostgreSql = 1,
}
