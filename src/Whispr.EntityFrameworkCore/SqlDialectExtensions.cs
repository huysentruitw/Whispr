namespace Whispr.EntityFrameworkCore;

internal static class SqlDialectExtensions
{
    /// <summary>
    /// Quotes and escapes a table, schema or column name.
    /// </summary>
    public static string QuoteIdentifier(this SqlDialect sqlDialect, string name)
        => sqlDialect switch
        {
            SqlDialect.MsSql => $"[{name.Replace("]", "]]")}]",
            SqlDialect.PostgreSql => $"\"{name.Replace("\"", "\"\"")}\"",
            _ => throw new NotSupportedException($"SQL dialect not supported: {sqlDialect}"),
        };
}
