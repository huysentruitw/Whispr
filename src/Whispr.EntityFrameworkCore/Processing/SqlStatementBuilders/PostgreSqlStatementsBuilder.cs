using Microsoft.EntityFrameworkCore.Metadata;

namespace Whispr.EntityFrameworkCore.Processing.SqlStatementBuilders;

internal sealed class PostgreSqlStatementsBuilder : ISqlStatementsBuilder
{
    private readonly OutboxTable _table;

    private PostgreSqlStatementsBuilder(OutboxTable table)
    {
        _table = table;
    }

    public static PostgreSqlStatementsBuilder Create(IModel model)
        => new(OutboxTable.Create(model, SqlDialect.PostgreSql));

    /// <inheritdoc />
    public SqlStatement BuildSelectBatch(int maxMessageBatchSize, DateTimeOffset nowUtc)
        => new(
            $$"""
                SELECT * FROM {{_table.Name}}
                WHERE {{_table.ProcessedAtUtc}} IS NULL AND {{_table.ParkedAtUtc}} IS NULL AND ({{_table.NextAttemptAtUtc}} IS NULL OR {{_table.NextAttemptAtUtc}} <= {0})
                ORDER BY {{_table.CreatedAtUtc}}
                LIMIT {{maxMessageBatchSize}}
                FOR UPDATE SKIP LOCKED
                """,
            [nowUtc]);

    // The ids are passed as a single array parameter, so there is no parameter limit and the statement text never changes

    /// <inheritdoc />
    public IEnumerable<SqlStatement> BuildMarkAsProcessed(long[] ids, DateTimeOffset processedAtUtc)
        => [new SqlStatement($"UPDATE {_table.Name} SET {_table.ProcessedAtUtc} = {{0}} WHERE {_table.Id} = ANY({{1}})", [processedAtUtc, ids])];

    /// <inheritdoc />
    public IEnumerable<SqlStatement> BuildDelete(long[] ids)
        => [new SqlStatement($"DELETE FROM {_table.Name} WHERE {_table.Id} = ANY({{0}})", [ids])];
}
