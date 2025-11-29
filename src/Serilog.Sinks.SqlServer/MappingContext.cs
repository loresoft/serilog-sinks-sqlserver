using System.Data;

namespace Serilog.Sinks.SqlServer;

public class MappingContext<T>
{
    // cached schema table
    private DataTable? _schemaTable;

    public MappingContext(IReadOnlyList<ColumnMapping<T>> mappings)
    {
        Mappings = mappings;

        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < mappings.Count; i++)
            ordinals[mappings[i].ColumnName] = i;

        Ordinals = ordinals;
    }

    public IReadOnlyList<ColumnMapping<T>> Mappings { get; }

    public IReadOnlyDictionary<string, int> Ordinals { get; }

    public ColumnMapping<T>? GetMapping(string name)
    {
        if (Ordinals.TryGetValue(name, out int ordinal))
            return GetMapping(ordinal);

        return null;
    }

    public ColumnMapping<T>? GetMapping(int ordinal)
    {
        if (ordinal >= 0 && ordinal < Mappings.Count)
            return Mappings[ordinal];

        return null;
    }

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
