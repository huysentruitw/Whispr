using Microsoft.EntityFrameworkCore.Metadata;

namespace Whispr.EntityFrameworkCore.Processing.SqlStatementBuilders;

/// <summary>
/// The quoted table and column names of the outbox table, as mapped in the EF model.
/// </summary>
internal sealed record OutboxTable(
    string Name,
    string Id,
    string CreatedAtUtc,
    string ProcessedAtUtc,
    string ParkedAtUtc,
    string NextAttemptAtUtc)
{
    public static OutboxTable Create(IModel model, SqlDialect sqlDialect)
    {
        var entityType = model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException($"Entity type not found: {nameof(OutboxMessage)}");

        var tableName = entityType.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
            throw new InvalidOperationException($"Table name not found for entity type: {nameof(OutboxMessage)}");

        var schema = entityType.GetSchema();
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);

        return new OutboxTable(
            Name: schema is null ? sqlDialect.QuoteIdentifier(tableName) : $"{sqlDialect.QuoteIdentifier(schema)}.{sqlDialect.QuoteIdentifier(tableName)}",
            Id: GetColumnName(nameof(OutboxMessage.Id)),
            CreatedAtUtc: GetColumnName(nameof(OutboxMessage.CreatedAtUtc)),
            ProcessedAtUtc: GetColumnName(nameof(OutboxMessage.ProcessedAtUtc)),
            ParkedAtUtc: GetColumnName(nameof(OutboxMessage.ParkedAtUtc)),
            NextAttemptAtUtc: GetColumnName(nameof(OutboxMessage.NextAttemptAtUtc)));

        string GetColumnName(string propertyName)
            => sqlDialect.QuoteIdentifier(entityType.FindProperty(propertyName)?.GetColumnName(storeObject)
                ?? throw new InvalidOperationException($"Column not found for property: {nameof(OutboxMessage)}.{propertyName}"));
    }
}
