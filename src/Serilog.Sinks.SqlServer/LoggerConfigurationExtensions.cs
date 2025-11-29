using Microsoft.Data.SqlClient;

using Serilog.Configuration;
using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

public static class LoggerConfigurationExtensions
{
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

        return loggerConfiguration.Sink(sink, options, options.MinimumLevel);
    }

    public static LoggerConfiguration SqlServer(
        this LoggerSinkConfiguration loggerConfiguration,
        string connectionString,
        string tableName = MappingDefaults.TableName,
        string tableSchema = MappingDefaults.TableSchema,
        LogEventLevel minimumLevel = LevelAlias.Minimum,
        SqlBulkCopyOptions bulkCopyOptions = SqlBulkCopyOptions.Default)
    {
        if (loggerConfiguration is null)
            throw new ArgumentNullException(nameof(loggerConfiguration));

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));

        return SqlServer(loggerConfiguration, sinkOptions =>
        {
            sinkOptions.ConnectionString = connectionString;
            sinkOptions.TableName = tableName;
            sinkOptions.TableSchema = tableSchema;
            sinkOptions.MinimumLevel = minimumLevel;
            sinkOptions.BulkCopyOptions = bulkCopyOptions;
        });
    }
}
