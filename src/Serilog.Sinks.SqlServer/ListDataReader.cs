using System.Data;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Provides an <see cref="IDataReader"/> implementation for reading a list of items with configurable column mappings.
/// </summary>
/// <typeparam name="T">The type of items to read.</typeparam>
/// <remarks>
/// This class enables bulk insert operations by wrapping an enumerable collection and exposing it through the IDataReader interface.
/// Column mappings are defined through the <see cref="MappingContext{T}"/> which specifies how to extract values from each item.
/// The <see cref="MappingContext{T}"/> instance should be created once and reused across multiple <see cref="ListDataReader{T}"/> instances
/// to avoid the overhead of recreating column mappings and schema information for each operation.
/// </remarks>
public class ListDataReader<T> : IDataReader
{
    private readonly IEnumerator<T> _iterator;
    private readonly MappingContext<T> _mappingContext;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDataReader{T}"/> class.
    /// </summary>
    /// <param name="logEvents">The collection of items to read.</param>
    /// <param name="mappingContext">The mapping context that defines how to map items to columns.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logEvents"/> or <paramref name="mappingContext"/> is null.</exception>
    public ListDataReader(
        IEnumerable<T> logEvents,
        MappingContext<T> mappingContext)
    {
        if (logEvents == null)
            throw new ArgumentNullException(nameof(logEvents));

        if (mappingContext == null)
            throw new ArgumentNullException(nameof(mappingContext));

        _iterator = logEvents.GetEnumerator();
        _mappingContext = mappingContext;
    }

    /// <inheritdoc/>
    public int Depth => 0;

    /// <inheritdoc/>
    public bool IsClosed { get; private set; }

    /// <inheritdoc/>
    public int RecordsAffected => 0;

    /// <inheritdoc/>
    public void Close()
        => Dispose(true);

    /// <inheritdoc/>
    public DataTable GetSchemaTable()
        => _mappingContext.GetSchemaTable();

    /// <inheritdoc/>
    public bool NextResult() => false;

    /// <inheritdoc/>
    public bool Read()
        => _iterator.MoveNext();

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="ListDataReader{T}"/>.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing && !IsClosed)
        {
            _iterator.Dispose();
            IsClosed = true;
        }

        _disposed = true;
    }

    /// <inheritdoc/>
    public string GetName(int i)
    {
        var columnMapping = _mappingContext.GetMapping(i);
        if (columnMapping == null)
            throw new IndexOutOfRangeException($"No column mapping found for ordinal {i}.");

        return columnMapping.ColumnName ?? string.Empty;
    }

    /// <inheritdoc/>
    public string GetDataTypeName(int i)
    {
        var columnMapping = _mappingContext.GetMapping(i);
        if (columnMapping == null)
            throw new IndexOutOfRangeException($"No column mapping found for ordinal {i}.");

        return columnMapping.ColumnType.Name ?? string.Empty;
    }

    /// <inheritdoc/>
    public Type GetFieldType(int i)
    {
        var columnMapping = _mappingContext.GetMapping(i);
        if (columnMapping == null)
            throw new IndexOutOfRangeException($"No column mapping found for ordinal {i}.");

        return columnMapping.ColumnType;
    }

    /// <summary>
    /// Gets the value of the specified column in its native format.
    /// </summary>
    /// <param name="i">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column, or <see cref="DBNull.Value"/> if the value is null.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when no column mapping is found for the specified ordinal.</exception>
    /// <remarks>
    /// String values that exceed the column's defined size are automatically truncated to fit the maximum length.
    /// </remarks>
    public object GetValue(int i)
    {
        var columnMapping = _mappingContext.GetMapping(i);
        if (columnMapping == null)
            throw new IndexOutOfRangeException($"No column mapping found for ordinal {i}.");

        var value = columnMapping.GetValue(_iterator.Current);
        if (value == null)
            return DBNull.Value;

        // auto truncate strings that exceed the defined size
        if (columnMapping.Size.HasValue && value is string stringValue && stringValue.Length > columnMapping.Size.Value)
            return stringValue[..columnMapping.Size.Value];

        return value;
    }

    /// <inheritdoc/>
    public int GetValues(object[] values)
    {
        int count = Math.Min(_mappingContext.Mappings.Count, values.Length);
        for (int i = 0; i < count; i++)
            values[i] = GetValue(i);

        return count;
    }

    /// <inheritdoc/>
    public int GetOrdinal(string name)
    {
        if (_mappingContext.Ordinals.TryGetValue(name, out int ordinal))
            return ordinal;

        return -1;
    }

    /// <inheritdoc/>
    public bool GetBoolean(int i)
        => (bool)GetValue(i);

    /// <inheritdoc/>
    public byte GetByte(int i)
        => (byte)GetValue(i);

    /// <inheritdoc/>
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferOffset, int length)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        byte[] value = (byte[])GetValue(i);

        int available = value.Length - (int)fieldOffset;
        if (available <= 0)
            return 0;

        int count = Math.Min(length, available);

        Buffer.BlockCopy(value, (int)fieldOffset, buffer, bufferOffset, count);

        return count;
    }

    /// <inheritdoc/>
    public char GetChar(int i)
        => (char)GetValue(i);

    /// <inheritdoc/>
    public long GetChars(int i, long fieldOffset, char[]? buffer, int bufferOffset, int length)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        string value = (string)GetValue(i);

        int available = value.Length - (int)fieldOffset;
        if (available <= 0)
            return 0;

        int count = Math.Min(length, available);

        value.CopyTo((int)fieldOffset, buffer, bufferOffset, count);

        return count;
    }

    /// <inheritdoc/>
    public Guid GetGuid(int i)
        => (Guid)GetValue(i);

    /// <inheritdoc/>
    public short GetInt16(int i)
        => (short)GetValue(i);

    /// <inheritdoc/>
    public int GetInt32(int i)
        => (int)GetValue(i);

    /// <inheritdoc/>
    public long GetInt64(int i)
        => (long)GetValue(i);

    /// <inheritdoc/>
    public float GetFloat(int i)
        => (float)GetValue(i);

    /// <inheritdoc/>
    public double GetDouble(int i)
        => (double)GetValue(i);

    /// <inheritdoc/>
    public string GetString(int i)
        => (string)GetValue(i);

    /// <inheritdoc/>
    public decimal GetDecimal(int i)
        => (decimal)GetValue(i);

    /// <inheritdoc/>
    public DateTime GetDateTime(int i)
        => (DateTime)GetValue(i);

    /// <inheritdoc/>
    public IDataReader GetData(int i)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool IsDBNull(int i)
    {
        var value = GetValue(i);
        return value == null || value == DBNull.Value;
    }

    /// <inheritdoc/>
    public int FieldCount
        => _mappingContext.Mappings.Count;

    /// <inheritdoc/>
    public object this[int i]
        => GetValue(i);

    /// <inheritdoc/>
    public object this[string name]
        => GetValue(GetOrdinal(name));

}
