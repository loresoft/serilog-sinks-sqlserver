using Microsoft.Data.SqlClient;

using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Configuration options for the SQL Server sink.
/// </summary>
/// <remarks>
/// This class extends <see cref="BatchingOptions"/> to provide SQL Server-specific configuration
/// including connection settings, table information, column mappings, and bulk copy options.
/// </remarks>
public class SqlServerSinkOptions : BatchingOptions
{
    /// <summary>
    /// Gets or sets the minimum log event level required to write an event to the sink.
    /// </summary>
    /// <value>The minimum log event level. Defaults to <see cref="LevelAlias.Minimum"/>.</value>
    public LogEventLevel MinimumLevel { get; set; } = LevelAlias.Minimum;

    /// <summary>
    /// Gets or sets the <see cref="LoggingLevelSwitch"/> that dynamically controls the log event level
    /// required to write an event to the sink.
    /// </summary>
    /// <value>
    /// The <see cref="LoggingLevelSwitch"/> instance, allowing runtime adjustments to the filtering level.
    /// If null, the level is determined by the <see cref="MinimumLevel"/> property.
    /// </value>
    public LoggingLevelSwitch? LevelSwitch { get; set; }

    /// <summary>
    /// Gets or sets the connection string to the SQL Server database.
    /// </summary>
    /// <value>The SQL Server connection string.</value>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the database schema name for the log table.
    /// </summary>
    /// <value>The schema name. Defaults to <see cref="MappingDefaults.TableSchema"/>.</value>
    public string? TableSchema { get; set; } = MappingDefaults.TableSchema;

    /// <summary>
    /// Gets or sets the name of the table to write log events to.
    /// </summary>
    /// <value>The table name. Defaults to <see cref="MappingDefaults.TableName"/>.</value>
    public string? TableName { get; set; } = MappingDefaults.TableName;

    /// <summary>
    /// Gets or sets options for the SQL bulk copy operation.
    /// </summary>
    /// <value>The bulk copy options. Defaults to <see cref="SqlBulkCopyOptions.Default"/>.</value>
    public SqlBulkCopyOptions BulkCopyOptions { get; set; } = SqlBulkCopyOptions.Default;

    /// <summary>
    /// Gets the list of column mappings that define how log event properties are mapped to database columns.
    /// </summary>
    /// <value>A list of column mappings. Defaults to <see cref="MappingDefaults.StandardMappings"/>.</value>
    /// <remarks>
    /// This list can be modified to add custom property mappings or remove default mappings.
    /// </remarks>
    public List<ColumnMapping<LogEvent>> Mappings { get; } = MappingDefaults.StandardMappings;

    /// <summary>
    /// Adds a custom property mapping to the column mappings list.
    /// </summary>
    /// <param name="propertyName">The name of the log event property to map.</param>
    /// <param name="propertyType">The data type of the database column. Defaults to <see cref="string"/> if not specified.</param>
    /// <param name="columnName">The name of the database column. Defaults to <paramref name="propertyName"/> if not specified.</param>
    /// <param name="size">The maximum size of the database column. Defaults to <c>null</c> if not specified.</param>
    /// <returns>The current <see cref="SqlServerSinkOptions"/> instance for method chaining.</returns>
    /// <remarks>
    /// This method provides a convenient way to add custom property mappings while configuring the sink.
    /// The property value is extracted using the <see cref="LogEventExtensions.GetPropertyValue"/> method.
    /// </remarks>
    public SqlServerSinkOptions AddPropertyMapping(
        string propertyName,
        Type? propertyType = null,
        string? columnName = null,
        int? size = null)
    {
        var mapping = new ColumnMapping<LogEvent>
        (
            ColumnName: columnName ?? propertyName,
            ColumnType: propertyType ?? typeof(string),
            GetValue: logEvent => logEvent.GetPropertyValue(propertyName),
            Nullable: true,
            Size: size
        );

        Mappings.Add(mapping);

        return this;
    }
}
