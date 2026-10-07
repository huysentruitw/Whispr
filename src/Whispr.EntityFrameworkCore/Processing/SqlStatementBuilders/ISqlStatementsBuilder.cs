namespace Whispr.EntityFrameworkCore.Processing.SqlStatementBuilders;

/// <summary>
/// Builds the handwritten SQL statements of the outbox processor for a specific database provider.
/// </summary>
internal interface ISqlStatementsBuilder
{
    /// <summary>
    /// Builds the statement that selects and locks the next batch of pending messages, with its parameters.
    /// Messages scheduled for a next attempt after <paramref name="nowUtc"/> are skipped.
    /// </summary>
    SqlStatement BuildSelectBatch(int maxMessageBatchSize, DateTimeOffset nowUtc);

    /// <summary>
    /// Builds the statements that mark the given messages as processed, with their parameters.
    /// Can return multiple statements for a large number of ids.
    /// </summary>
    IEnumerable<SqlStatement> BuildMarkAsProcessed(long[] ids, DateTimeOffset processedAtUtc);

    /// <summary>
    /// Builds the statements that delete the given messages, with their parameters.
    /// Can return multiple statements for a large number of ids.
    /// </summary>
    IEnumerable<SqlStatement> BuildDelete(long[] ids);
}

/// <summary>
/// A SQL statement with its positional parameters. 
/// </summary>
internal sealed record SqlStatement(string Sql, object[] Parameters);
