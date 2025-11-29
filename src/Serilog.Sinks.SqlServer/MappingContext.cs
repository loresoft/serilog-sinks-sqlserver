using System.Data;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Provides a context for managing column mappings and schema information for data reading operations.
/// </summary>
/// <typeparam name="T">The type of items being mapped to database columns.</typeparam>
/// <remarks>
/// This class maintains column mappings, ordinal lookups, and generates schema information for use with <see cref="IDataReader"/> implementations.
/// The schema table is cached after first generation for performance.
/// </remarks>
public class MappingContext<T>
{
    // cached schema table
    private DataTable? _schemaTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingContext{T}"/> class.
    /// </summary>
    /// <param name="mappings">The list of column mappings to manage.</param>
    /// <remarks>
    /// The constructor builds an ordinal lookup dictionary for efficient column name to index resolution.
    /// Column name comparisons are case-insensitive.
    /// </remarks>
    public MappingContext(IReadOnlyList<ColumnMapping<T>> mappings)
    {
        Mappings = mappings;

        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < mappings.Count; i++)
            ordinals[mappings[i].ColumnName] = i;

        Ordinals = ordinals;
    }

    /// <summary>
    /// Gets the list of column mappings.
    /// </summary>
    public IReadOnlyList<ColumnMapping<T>> Mappings { get; }

    /// <summary>
    /// Gets a dictionary that maps column names to their ordinal positions.
    /// </summary>
    /// <remarks>
    /// Column name lookups are case-insensitive.
    /// </remarks>
    public IReadOnlyDictionary<string, int> Ordinals { get; }

    /// <summary>
    /// Gets the column mapping for the specified column name.
    /// </summary>
    /// <param name="name">The name of the column. Case-insensitive.</param>
    /// <returns>The <see cref="ColumnMapping{T}"/> for the specified column, or <c>null</c> if not found.</returns>
    public ColumnMapping<T>? GetMapping(string name)
    {
        if (Ordinals.TryGetValue(name, out int ordinal))
            return GetMapping(ordinal);

        return null;
    }

    /// <summary>
    /// Gets the column mapping for the specified ordinal position.
    /// </summary>
    /// <param name="ordinal">The zero-based ordinal position of the column.</param>
    /// <returns>The <see cref="ColumnMapping{T}"/> for the specified ordinal, or <c>null</c> if the ordinal is out of range.</returns>
    public ColumnMapping<T>? GetMapping(int ordinal)
    {
        if (ordinal >= 0 && ordinal < Mappings.Count)
            return Mappings[ordinal];

        return null;
    }

    /// <summary>
    /// Gets a <see cref="DataTable"/> that describes the schema of the mapped columns.
    /// </summary>
    /// <returns>A <see cref="DataTable"/> containing schema information including column ordinals, names, data types, sizes, and nullability.</returns>
    /// <remarks>
    /// The schema table is generated once and cached for subsequent calls.
    /// The schema includes the following columns: ColumnOrdinal, ColumnName, DataType, ColumnSize, and AllowDBNull.
    /// </remarks>
    public DataTable GetSchemaTable()
    {
        // only build once
        if (_schemaTable != null)
            return _schemaTable;

        // these are the columns used by DataTable load
        var table = new DataTable
        {
            Columns =
            {
                {"ColumnOrdinal", typeof(int)},
                {"ColumnName", typeof(string)},
                {"DataType", typeof(Type)},
                {"ColumnSize", typeof(int)},
                {"AllowDBNull", typeof(bool)}
            }
        };

        // populate rows
        for (int i = 0; i < Mappings.Count; i++)
        {
            var rowData = new object[5];
            rowData[0] = i;
            rowData[1] = Mappings[i].ColumnName;
            rowData[2] = Mappings[i].ColumnType;
            rowData[3] = -1;
            rowData[4] = Mappings[i].Nullable;

            table.Rows.Add(rowData);
        }

        // cache for next time
        _schemaTable = table;

        return table;
    }
}
