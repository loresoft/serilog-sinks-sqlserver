using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;

using Serilog.Events;

namespace Serilog.Sinks.SqlServer.Benchmark;

[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[HideColumns(Column.Error, Column.StdDev)]
public class SinkBenchmark
{
    private const string SqlConnectionString = "Data Source=(local);Initial Catalog=SerilogBenchmark;Integrated Security=True;TrustServerCertificate=True;";
    private const int BatchSize = 100;

    private MSSqlServer.MSSqlServerSink _msSqlServerSink;
    private SqlServerSink _sqlServerSink;

    private LogEvent[] _logEvents;

    [GlobalSetup]
    public void Setup()
    {
        // Create a sample exception to include in log events
        var exception = new InvalidOperationException("Benchmark exception");

        // Create log events to batch
        _logEvents = new LogEvent[BatchSize];
        for (int i = 0; i < BatchSize; i++)
        {
            var template = new Parsing.MessageTemplateParser()
                .Parse("Batch message {Counter} user {UserId} action {Action}");

            var properties = new List<LogEventProperty>
                {
                    new("Counter", new ScalarValue(i)),
                    new("UserId", new ScalarValue($"user{i}")),
                    new("Action", new ScalarValue("BatchTest")),
                    new("SourceContext", new ScalarValue("Serilog.Sinks.SqlServer.Benchmark.SinkBenchmark"))
                };

            _logEvents[i] = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                exception,
                template,
                properties);
        }


        // Serilog.Sinks.MSSqlServer
        _msSqlServerSink = new MSSqlServer.MSSqlServerSink(
            connectionString: SqlConnectionString,
            sinkOptions: new MSSqlServer.MSSqlServerSinkOptions
            {
                TableName = "LogsMSSqlServerBatch",
                AutoCreateSqlTable = false,
                BatchPostingLimit = BatchSize,
                BatchPeriod = TimeSpan.FromSeconds(2),
                UseSqlBulkCopy = true
            });

        // Serilog.Sinks.SqlServer
        var options = new SqlServerSinkOptions
        {
            ConnectionString = SqlConnectionString,
            TableName = "LogsSqlServerBatch",
            BatchSizeLimit = BatchSize,
            BufferingTimeLimit = TimeSpan.FromSeconds(2)
        };

        // Exclude SourceContext column for fair comparison
        options.Mappings.RemoveAll(m => m.ColumnName == "SourceContext");

        _sqlServerSink = new SqlServerSink(options);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_msSqlServerSink as IDisposable)?.Dispose();
        (_sqlServerSink as IDisposable)?.Dispose();
    }


    [Benchmark]
    public async Task MSSqlServerSink()
    {
        await _msSqlServerSink.EmitBatchAsync(_logEvents);
    }

    [Benchmark]
    public async Task SqlServerSink()
    {
        await _sqlServerSink.EmitBatchAsync(_logEvents);
    }
}
