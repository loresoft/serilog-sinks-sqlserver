namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Represents the mapping configuration for a database column.
/// </summary>
/// <typeparam name="T">The type of the source object to map from.</typeparam>
/// <param name="ColumnName">The name of the database column.</param>
/// <param name="ColumnType">The data type of the database column.</param>
/// <param name="GetValue">A function that extracts the value from the source object.</param>
/// <param name="Nullable">Indicates whether the column allows null values. Default is <c>true</c>.</param>
/// <param name="Size">The optional size constraint for the column (e.g., varchar length).</param>
public record ColumnMapping<T>
(
    string ColumnName,
    Type ColumnType,
    Func<T, object?> GetValue,
    bool Nullable = true,
    int? Size = null
);
