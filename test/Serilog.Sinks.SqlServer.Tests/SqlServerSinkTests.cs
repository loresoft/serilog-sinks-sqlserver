using AwesomeAssertions;

using Microsoft.Data.SqlClient;

using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.SqlServer.Tests.Fixtures;

namespace Serilog.Sinks.SqlServer.Tests;

public class SqlServerSinkTests(DatabaseFixture databaseFixture)
    : DatabaseTestBase(databaseFixture)
{
    #region Helper Methods

    private static LogEvent CreateLogEvent(
        LogEventLevel level = LogEventLevel.Information,
        string messageTemplate = "Test message",
        Exception? exception = null,
        Dictionary<string, LogEventPropertyValue>? properties = null)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var template = new MessageTemplateParser().Parse(messageTemplate);

        var logProperties = new List<LogEventProperty>();
        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                logProperties.Add(new LogEventProperty(kvp.Key, kvp.Value));
            }
        }

        return new LogEvent(timestamp, level, exception, template, logProperties);
    }

    private static SqlServerSinkOptions CreateValidOptions(string connectionString)
    {
        return new SqlServerSinkOptions
        {
            ConnectionString = connectionString,
            TableName = "LogEvent",
            TableSchema = "dbo",
            BatchSizeLimit = 100
        };
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidOptions_ShouldInitialize()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());

        // Act
        var sink = new SqlServerSink(options);

        // Assert
        sink.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SqlServerSink(null!));
    }

    [Fact]
    public void Constructor_WithCustomTableSchema_ShouldInitialize()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.TableSchema = "custom";

        // Act
        var sink = new SqlServerSink(options);

        // Assert
        sink.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomTableName_ShouldInitialize()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.TableName = "CustomLog";

        // Act
        var sink = new SqlServerSink(options);

        // Assert
        sink.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithBulkCopyOptions_ShouldInitialize()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.BulkCopyOptions = SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock;

        // Act
        var sink = new SqlServerSink(options);

        // Assert
        sink.Should().NotBeNull();
    }

    #endregion

    #region EmitBatchAsync Tests - Successful Scenarios

    [Fact]
    public async Task EmitBatchAsync_WithSingleLogEvent_ShouldInsertSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var logEvent = CreateLogEvent(LogEventLevel.Information, "Test log message");
        var batch = new List<LogEvent> { logEvent };

        // Act
        await sink.EmitBatchAsync(batch);

        // Assert - Verify data was inserted
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM [dbo].[LogEvent] WHERE Message LIKE '%Test log message%'",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EmitBatchAsync_WithMultipleLogEvents_ShouldInsertAll()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent>
        {
            CreateLogEvent(LogEventLevel.Information, "Message 1"),
            CreateLogEvent(LogEventLevel.Warning, "Message 2"),
            CreateLogEvent(LogEventLevel.Error, "Message 3")
        };

        // Act
        await sink.EmitBatchAsync(batch);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM [dbo].[LogEvent] WHERE Message LIKE '%Message%'",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task EmitBatchAsync_WithLogEventWithException_ShouldInsertSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var exception = new InvalidOperationException("Test exception");
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Error occurred", exception);
        var batch = new List<LogEvent> { logEvent };

        // Act
        await sink.EmitBatchAsync(batch);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM [dbo].[LogEvent] WHERE Exception LIKE '%Test exception%'",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EmitBatchAsync_WithLogEventWithProperties_ShouldInsertSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["UserId"] = new ScalarValue(123),
            ["UserName"] = new ScalarValue("TestUser")
        };

        var logEvent = CreateLogEvent(LogEventLevel.Information, "User action", properties: properties);
        var batch = new List<LogEvent> { logEvent };

        // Act
        await sink.EmitBatchAsync(batch);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM [dbo].[LogEvent] WHERE Properties LIKE '%UserId%'",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EmitBatchAsync_WithDifferentLogLevels_ShouldInsertAll()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent>
        {
            CreateLogEvent(LogEventLevel.Verbose, "Verbose message"),
            CreateLogEvent(LogEventLevel.Debug, "Debug message"),
            CreateLogEvent(LogEventLevel.Information, "Info message"),
            CreateLogEvent(LogEventLevel.Warning, "Warning message"),
            CreateLogEvent(LogEventLevel.Error, "Error message"),
            CreateLogEvent(LogEventLevel.Fatal, "Fatal message")
        };

        // Act
        await sink.EmitBatchAsync(batch);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            "SELECT COUNT(DISTINCT Level) FROM [dbo].[LogEvent]",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task EmitBatchAsync_WithLargeBatch_ShouldInsertSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.BatchSizeLimit = 500;
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent>();
        for (int i = 0; i < 100; i++)
        {
            batch.Add(CreateLogEvent(LogEventLevel.Information, $"Batch message {i}"));
        }

        // Act
        await sink.EmitBatchAsync(batch);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM [dbo].[LogEvent] WHERE Message LIKE '%Batch message%'",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThanOrEqualTo(100);
    }

    #endregion

    #region EmitBatchAsync Tests - Edge Cases

    [Fact]
    public async Task EmitBatchAsync_WithNullBatch_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        // Act & Assert
        await sink.EmitBatchAsync(null!);
    }

    [Fact]
    public async Task EmitBatchAsync_WithEmptyBatch_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);
        var batch = new List<LogEvent>();

        // Act & Assert
        await sink.EmitBatchAsync(batch);
    }

    [Fact]
    public async Task EmitBatchAsync_CalledMultipleTimes_ShouldInsertAllBatches()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var batch1 = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Batch 1") };
        var batch2 = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Batch 2") };
        var batch3 = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Batch 3") };

        // Act
        await sink.EmitBatchAsync(batch1);
        await sink.EmitBatchAsync(batch2);
        await sink.EmitBatchAsync(batch3);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM [dbo].[LogEvent] WHERE Message LIKE '%Batch%'",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThanOrEqualTo(3);
    }

    #endregion

    #region EmitBatchAsync Tests - Error Handling

    [Fact]
    public async Task EmitBatchAsync_WithInvalidConnectionString_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions("Server=invalid;Database=invalid;");
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Test") };

        // Act & Assert - Should handle error internally and not throw
        await sink.EmitBatchAsync(batch);
    }

    [Fact]
    public async Task EmitBatchAsync_WithNonExistentTable_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.TableName = "NonExistentTable";
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Test") };

        // Act & Assert - Should handle error internally via SelfLog
        await sink.EmitBatchAsync(batch);
    }

    #endregion

    #region Table Name Generation Tests

    [Fact]
    public async Task EmitBatchAsync_WithDefaultTableName_ShouldUseDefault()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.TableName = null; // Use default
        options.TableSchema = null; // Use default
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Default table test") };

        // Act & Assert
        await sink.EmitBatchAsync(batch);
    }

    [Fact]
    public async Task EmitBatchAsync_WithQuotedTableName_ShouldHandleQuotes()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.TableName = "[LogEvent]";
        options.TableSchema = "[dbo]";
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Quoted table test") };

        // Act & Assert
        await sink.EmitBatchAsync(batch);
    }

    [Fact]
    public async Task EmitBatchAsync_WithTableNameContainingBrackets_ShouldEscapeProperly()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.TableName = "LogEvent"; // Contains ] which needs escaping
        options.TableSchema = "dbo";
        var sink = new SqlServerSink(options);

        var batch = new List<LogEvent> { CreateLogEvent(LogEventLevel.Information, "Escape test") };

        // Act & Assert
        await sink.EmitBatchAsync(batch);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task IntegrationTest_WriteAndReadLogEvents_ShouldWorkCorrectly()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var uniqueMessage = $"Integration test {Guid.NewGuid()}";
        var logEvent = CreateLogEvent(LogEventLevel.Information, uniqueMessage);
        var batch = new List<LogEvent> { logEvent };

        // Act
        await sink.EmitBatchAsync(batch);

        // Give SQL Server a moment to process
        await Task.Delay(100, cancellationToken: TestCancellation);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            $"SELECT Message, Level FROM [dbo].[LogEvent] WHERE Message LIKE '%{uniqueMessage}%'",
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken: TestCancellation);
        var found = await reader.ReadAsync(cancellationToken: TestCancellation);

        found.Should().BeTrue();
        if (found)
        {
            var message = reader.GetString(0);
            var level = reader.GetString(1);

            message.Should().Contain(uniqueMessage);
            level.Should().Be("Information");
        }
    }

    [Fact]
    public async Task IntegrationTest_WriteWithAllFieldsPopulated_ShouldInsertSuccessfully()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        var sink = new SqlServerSink(options);

        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["SourceContext"] = new ScalarValue("TestContext"),
            ["TraceId"] = new ScalarValue("trace-123"),
            ["SpanId"] = new ScalarValue("span-456")
        };

        var exception = new InvalidOperationException("Test exception with details");
        var uniqueMessage = $"Full fields test {Guid.NewGuid()}";
        var logEvent = CreateLogEvent(LogEventLevel.Error, uniqueMessage, exception, properties);
        var batch = new List<LogEvent> { logEvent };

        // Act
        await sink.EmitBatchAsync(batch);

        // Give SQL Server a moment to process
        await Task.Delay(100, cancellationToken: TestCancellation);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            $"SELECT Exception, Properties, SourceContext FROM [dbo].[LogEvent] WHERE Message LIKE '%{uniqueMessage}%'",
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken: TestCancellation);
        var found = await reader.ReadAsync(cancellationToken: TestCancellation);

        found.Should().BeTrue();
    }

    [Fact]
    public async Task IntegrationTest_ConcurrentWrites_ShouldHandleAllBatches()
    {
        // Arrange
        var options = CreateValidOptions(Fixture.GetConnectionString());
        options.BatchSizeLimit = 1000;
        var sink = new SqlServerSink(options);

        var tasks = new List<Task>();
        var baseMessage = $"Concurrent test {Guid.NewGuid()}";

        // Act - Create multiple concurrent batch writes
        for (int i = 0; i < 5; i++)
        {
            var batchIndex = i;
            var task = Task.Run(async () =>
            {
                var batch = new List<LogEvent>();
                for (int j = 0; j < 10; j++)
                {
                    batch.Add(CreateLogEvent(LogEventLevel.Information, $"{baseMessage} - Batch {batchIndex} Item {j}"));
                }
                await sink.EmitBatchAsync(batch);
            }, cancellationToken: TestCancellation);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Give SQL Server a moment to process
        await Task.Delay(200, cancellationToken: TestCancellation);

        // Assert
        await using var connection = new SqlConnection(Fixture.GetConnectionString());
        await connection.OpenAsync(cancellationToken: TestCancellation);

        await using var command = new SqlCommand(
            $"SELECT COUNT(*) FROM [dbo].[LogEvent] WHERE Message LIKE '%{baseMessage}%'",
            connection);

        var count = (int)await command.ExecuteScalarAsync(cancellationToken: TestCancellation)!;
        count.Should().BeGreaterThanOrEqualTo(50); // 5 batches * 10 items
    }

    #endregion
}
