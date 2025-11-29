using Microsoft.Data.SqlClient;

using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// A Serilog sink that writes log events to a SQL Server database using bulk insert operations.
/// </summary>
/// <remarks>
/// This sink implements <see cref="IBatchedLogEventSink"/> to efficiently write batches of log events
/// using <see cref="SqlBulkCopy"/> for optimal performance and reduced memory overhead.
/// </remarks>
public class SqlServerSink : IBatchedLogEventSink
{
    private readonly SqlServerSinkOptions _options;
    private readonly MappingContext<LogEvent> _mappingContext;
    private readonly string _destinationTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSink"/> class.
    /// </summary>
    /// <param name="options">The configuration options for the SQL Server sink.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <remarks>
    /// The constructor initializes the mapping context and pre-computes the destination table name
    /// to optimize performance across multiple batches.
    /// </remarks>
    public SqlServerSink(SqlServerSinkOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _options = options;

        // mapping context shared between batches
        _mappingContext = new MappingContext<LogEvent>(options.Mappings);

        // pre-compute destination table name, shared between batches
        _destinationTable = GetTableName(options.TableSchema, options.TableName);
    }

    /// <summary>
    /// Asynchronously emits a batch of log events to the SQL Server database.
    /// </summary>
    /// <param name="batch">The collection of log events to write to the database.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method uses <see cref="SqlBulkCopy"/> with streaming enabled to efficiently insert
    /// large batches of log events while minimizing memory usage. Any errors during the operation
    /// are logged to <see cref="SelfLog"/> and do not throw exceptions.
    /// </remarks>
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        if (batch == null || batch.Count == 0)
            return;

        try
        {
            // allow reading the items without having to create a DataTable
            using var dataReader = new ListDataReader<LogEvent>(batch, _mappingContext);

            // use sql bulk copy for most efficient batch inserts
            using var sqlBulkCopy = new SqlBulkCopy(_options.ConnectionString, _options.BulkCopyOptions);

            sqlBulkCopy.DestinationTableName = _destinationTable;

            // use the same batch size as the sink's batch size limit
            sqlBulkCopy.BatchSize = _options.BatchSizeLimit;

            // enable streaming to reduce memory usage for large batches
            sqlBulkCopy.EnableStreaming = true;

            foreach (var mapping in _options.Mappings)
                sqlBulkCopy.ColumnMappings.Add(mapping.ColumnName, mapping.ColumnName);

            // using IDataReader to efficiently bulk load data compared to DataTable.
            // DataTable has a large memory and allocation footprint.
            await sqlBulkCopy.WriteToServerAsync(dataReader);

            sqlBulkCopy.Close();
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Error while emitting log events to SQL Server: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Called when an empty batch is processed. This implementation performs no action.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    /// <summary>
    /// Constructs a fully qualified and quoted table name from schema and table name components.
    /// </summary>
    /// <param name="tableSchema">The database schema name. Defaults to <see cref="MappingDefaults.TableSchema"/> if null or empty.</param>
    /// <param name="tableName">The table name. Defaults to <see cref="MappingDefaults.TableName"/> if null or empty.</param>
    /// <returns>A fully qualified table name in the format [schema].[table].</returns>
    private static string GetTableName(string? tableSchema, string? tableName)
    {
        tableSchema = QuoteIdentifier(tableSchema ?? MappingDefaults.TableSchema);
        tableName = QuoteIdentifier(tableName ?? MappingDefaults.TableName);

        return $"{tableSchema}.{tableName}";
    }

    /// <summary>
    /// Quotes a SQL identifier by wrapping it in square brackets and escaping any existing brackets.
    /// </summary>
    /// <param name="name">The identifier name to quote.</param>
    /// <returns>The quoted identifier, or an empty string if <paramref name="name"/> is null or empty.</returns>
    /// <remarks>
    /// If the identifier is already wrapped in square brackets, it is returned as-is.
    /// Any closing brackets (]) within the identifier are escaped by doubling them (]]).
    /// </remarks>
    private static string QuoteIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        if (name.StartsWith('[') && name.EndsWith(']'))
            return name;

        return $"[{name.Replace("]", "]]")}]";
    }
}
