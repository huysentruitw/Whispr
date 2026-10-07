using System.Numerics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Whispr.EntityFrameworkCore.Processing.SqlStatementBuilders;

internal sealed class MsSqlStatementsBuilder : ISqlStatementsBuilder
{
    private readonly OutboxTable _table;

    private MsSqlStatementsBuilder(OutboxTable table)
    {
        _table = table;
    }

    public static MsSqlStatementsBuilder Create(IModel model)
        => new(GetOutboxTable(model));

    /// <inheritdoc />
    public SqlStatement BuildSelectBatch(int maxMessageBatchSize, DateTimeOffset nowUtc)
        => new(
            $$"""
                SELECT TOP {{maxMessageBatchSize}} * FROM {{_table.Name}} WITH (UPDLOCK, ROWLOCK, READPAST)
                WHERE {{_table.ProcessedAtUtc}} IS NULL AND {{_table.ParkedAtUtc}} IS NULL AND ({{_table.NextAttemptAtUtc}} IS NULL OR {{_table.NextAttemptAtUtc}} <= {0})
                ORDER BY {{_table.CreatedAtUtc}}
                """,
            [nowUtc]);

    /// <inheritdoc />
    public IEnumerable<SqlStatement> BuildMarkAsProcessed(long[] ids, DateTimeOffset processedAtUtc)
        => GetPaddedIdChunks(ids).Select(paddedIds => new SqlStatement(
            $"UPDATE o SET {_table.ProcessedAtUtc} = {{0}} FROM {_table.Name} AS o WITH (FORCESEEK) WHERE o.{_table.Id} IN ({GetIdList(firstIdIndex: 1, paddedIds.Length)})",
            [processedAtUtc, .. paddedIds]));

    /// <inheritdoc />
    public IEnumerable<SqlStatement> BuildDelete(long[] ids)
        => GetPaddedIdChunks(ids).Select(paddedIds => new SqlStatement(
            $"DELETE o FROM {_table.Name} AS o WITH (FORCESEEK) WHERE o.{_table.Id} IN ({GetIdList(firstIdIndex: 0, paddedIds.Length)})",
            [.. paddedIds]));

    // Padded with the last id to a power of two, so a handful of cached plans covers every batch size
    private static IEnumerable<object[]> GetPaddedIdChunks(long[] ids)
    {
        const int maxIdsPerStatement = 1024; // Stays well under the 2100 limit of MS SQL
        
        return ids.Chunk(maxIdsPerStatement).Select(chunk =>
        {
            var paddedIdCount = (int)BitOperations.RoundUpToPowerOf2((uint)chunk.Length);
            return chunk.Concat(Enumerable.Repeat(chunk[^1], paddedIdCount - chunk.Length)).Cast<object>().ToArray();
        });
    }

    private static string GetIdList(int firstIdIndex, int idCount)
        => string.Join(", ", Enumerable.Range(firstIdIndex, idCount).Select(i => $"{{{i}}}"));

    private static OutboxTable GetOutboxTable(IModel model)
    {
        var entityType = model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException($"Entity type not found: {nameof(OutboxMessage)}");

        var tableName = entityType.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
            throw new InvalidOperationException($"Table name not found for entity type: {nameof(OutboxMessage)}");

        var schema = entityType.GetSchema();
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);

        return new OutboxTable(
            Name: schema is null ? Quote(tableName) : $"{Quote(schema)}.{Quote(tableName)}",
            Id: GetColumnName(nameof(OutboxMessage.Id)),
            CreatedAtUtc: GetColumnName(nameof(OutboxMessage.CreatedAtUtc)),
            ProcessedAtUtc: GetColumnName(nameof(OutboxMessage.ProcessedAtUtc)),
            ParkedAtUtc: GetColumnName(nameof(OutboxMessage.ParkedAtUtc)),
            NextAttemptAtUtc: GetColumnName(nameof(OutboxMessage.NextAttemptAtUtc)));

        string GetColumnName(string propertyName)
            => Quote(entityType.FindProperty(propertyName)?.GetColumnName(storeObject)
                ?? throw new InvalidOperationException($"Column not found for property: {nameof(OutboxMessage)}.{propertyName}"));

        static string Quote(string name)
            => $"[{name.Replace("]", "]]")}]";
    }

    // Quoted names
    private sealed record OutboxTable(
        string Name,
        string Id,
        string CreatedAtUtc,
        string ProcessedAtUtc,
        string ParkedAtUtc,
        string NextAttemptAtUtc);
}
