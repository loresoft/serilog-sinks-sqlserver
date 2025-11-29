using Microsoft.Data.SqlClient;

using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

public class SqlServerSink : IBatchedLogEventSink
{
    private readonly SqlServerSinkOptions _options;
    private readonly MappingContext<LogEvent> _mappingContext;
    private readonly string _destinationTable;

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

    public Task OnEmptyBatchAsync() => Task.CompletedTask;


    private static string GetTableName(string? tableSchema, string? tableName)
    {
        tableSchema = QuoteIdentifier(tableSchema ?? MappingDefaults.TableSchema);
        tableName = QuoteIdentifier(tableName ?? MappingDefaults.TableName);

        return $"{tableSchema}.{tableName}";
    }

    private static string QuoteIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        if (name.StartsWith('[') && name.EndsWith(']'))
            return name;

        return $"[{name.Replace("]", "]]")}]";
    }
}
