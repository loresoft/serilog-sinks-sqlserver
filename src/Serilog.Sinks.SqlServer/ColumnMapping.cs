namespace Serilog.Sinks.SqlServer;

public record ColumnMapping<T>
(
    string ColumnName,
    Type ColumnType,
    Func<T, object?> GetValue,
    bool Nullable = true,
    int? Size = null
);
