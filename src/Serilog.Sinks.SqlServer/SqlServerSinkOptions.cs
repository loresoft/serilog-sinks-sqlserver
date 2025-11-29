using Microsoft.Data.SqlClient;

using Serilog.Configuration;
using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

public class SqlServerSinkOptions : BatchingOptions
{
    public LogEventLevel MinimumLevel { get; set; } = LevelAlias.Minimum;

    public string? ConnectionString { get; set; }

    public string? TableSchema { get; set; } = MappingDefaults.TableSchema;

    public string? TableName { get; set; } = MappingDefaults.TableName;

    public SqlBulkCopyOptions BulkCopyOptions { get; set; } = SqlBulkCopyOptions.Default;

    public List<ColumnMapping<LogEvent>> Mappings { get; } = MappingDefaults.StandardMappings;


    public SqlServerSinkOptions AddPropertyMapping(
        string propertyName,
        Type? propertyType = null,
        string? columnName = null)
    {
        var mapping = new ColumnMapping<LogEvent>
        (
            ColumnName: columnName ?? propertyName,
            ColumnType: propertyType ?? typeof(string),
            GetValue: logEvent => logEvent.GetPropertyValue(propertyName)
        );

        Mappings.Add(mapping);

        return this;
    }
}
