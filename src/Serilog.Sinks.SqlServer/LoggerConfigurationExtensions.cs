using Microsoft.Data.SqlClient;

using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Provides extension methods for configuring the SQL Server sink in Serilog.
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Configures the logger to write log events to a SQL Server database using the specified options.
    /// </summary>
    /// <param name="loggerConfiguration">The logger sink configuration.</param>
    /// <param name="configure">A delegate to configure the <see cref="SqlServerSinkOptions"/>.</param>
    /// <returns>The logger configuration, allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerConfiguration"/> or <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// This overload provides full control over sink configuration through the <see cref="SqlServerSinkOptions"/> object.
    /// </remarks>
    public static LoggerConfiguration SqlServer(
        this LoggerSinkConfiguration loggerConfiguration,
        Action<SqlServerSinkOptions> configure)
    {
        if (loggerConfiguration is null)
            throw new ArgumentNullException(nameof(loggerConfiguration));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        var options = new SqlServerSinkOptions();
        configure.Invoke(options);

        var sink = new SqlServerSink(options);

        return loggerConfiguration.Sink(sink, options, options.MinimumLevel, levelSwitch: options.LevelSwitch);
    }

    /// <summary>
    /// Configures the logger to write log events to a SQL Server database with the specified connection and table settings.
    /// </summary>
    /// <param name="loggerConfiguration">The logger sink configuration.</param>
    /// <param name="connectionString">The connection string to the SQL Server database.</param>
    /// <param name="tableName">The name of the table to write log events to. Defaults to <see cref="MappingDefaults.TableName"/>.</param>
    /// <param name="tableSchema">The schema of the table. Defaults to <see cref="MappingDefaults.TableSchema"/>.</param>
    /// <param name="minimumLevel">The minimum log event level required to write an event to the sink. Defaults to <see cref="LevelAlias.Minimum"/>.</param>
    /// <param name="bulkCopyOptions">Options for the SQL bulk copy operation. Defaults to <see cref="SqlBulkCopyOptions.Default"/>.</param>
    /// <param name="levelSwitch">The <see cref="LoggingLevelSwitch"/> instance, allowing runtime adjustments to the filtering level.</param>
    /// <returns>The logger configuration, allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerConfiguration"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    /// <remarks>
    /// This overload provides a simplified configuration for common scenarios with default column mappings.
    /// </remarks>
    public static LoggerConfiguration SqlServer(
        this LoggerSinkConfiguration loggerConfiguration,
        string connectionString,
        string tableName = MappingDefaults.TableName,
        string tableSchema = MappingDefaults.TableSchema,
        LogEventLevel minimumLevel = LevelAlias.Minimum,
        SqlBulkCopyOptions bulkCopyOptions = SqlBulkCopyOptions.Default,
        LoggingLevelSwitch? levelSwitch = null
    )
    {
        if (loggerConfiguration is null)
            throw new ArgumentNullException(nameof(loggerConfiguration));

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.",
                nameof(connectionString));

        return SqlServer(loggerConfiguration, sinkOptions =>
        {
            sinkOptions.ConnectionString = connectionString;
            sinkOptions.TableName = tableName;
            sinkOptions.TableSchema = tableSchema;
            sinkOptions.MinimumLevel = minimumLevel;
            sinkOptions.LevelSwitch = levelSwitch;
            sinkOptions.BulkCopyOptions = bulkCopyOptions;
        });
    }
}
