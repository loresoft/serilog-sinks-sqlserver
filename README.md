# Serilog.Sinks.SqlServer

A high-performance [Serilog](https://serilog.net/) sink that writes log events to Microsoft SQL Server using bulk insert operations.

[![NuGet](https://img.shields.io/nuget/v/Serilog.Sinks.SqlServer.svg)](https://www.nuget.org/packages/Serilog.Sinks.SqlServer/)
[![License](https://img.shields.io/github/license/loresoft/serilog-sinks-sqlserver.svg)](https://github.com/loresoft/serilog-sinks-sqlserver/blob/main/LICENSE)

## Features

- **High Performance**: Uses `SqlBulkCopy` for efficient bulk insert operations
- **Flexible Column Mapping**: Customize which log event properties map to which columns
- **Configurable Batching**: Control batch size and timeout for optimal performance
- **Standard Mappings**: Includes default mappings for common log properties
- **Custom Properties**: Easily add custom property mappings
- **Rich Data Types**: Support for various data types including structured properties as JSON
- **Distributed Tracing**: Built-in support for TraceId and SpanId
- **Auto Truncation**: Automatically truncates string values to match column size constraints, preventing insert errors

## Performance Comparison

This sink is designed to be a **high-performance, lightweight alternative** to `Serilog.Sinks.MSSqlServer` with significant improvements in speed and memory efficiency.

### Benchmark Results

Based on `Serilog.Sinks.SqlServer.Benchmark` tests (100 log events per batch):

| Method          |     Mean | Rank |     Gen0 |    Gen1 |  Allocated |
| --------------- | -------: | ---: | -------: | ------: | ---------: |
| SqlServerSink   | 2.082 ms |    1 |   7.8125 |       - |  438.31 KB |
| MSSqlServerSink | 2.666 ms |    2 | 117.1875 | 27.3438 | 5773.93 KB |

**Key Performance Benefits:**

- **~22% faster** execution time (2.082 ms vs 2.666 ms)
- **~92% fewer allocations** (438 KB vs 5,774 KB per batch)
- **Significantly reduced GC pressure** from 13x lower memory allocations
- **Optimized bulk copy** operations with minimal overhead

### Why This Sink is Faster

1. **Streamlined Architecture**: Focused solely on high-performance SQL Server logging without legacy compatibility layers
2. **Efficient Memory Usage**: Minimal allocations through careful use of `ArrayBufferWriter`, `Span<T>`, and modern .NET APIs
3. **Optimized JSON Serialization**: Custom `JsonWriter` using `Utf8JsonWriter` for zero-copy serialization
4. **Direct Bulk Copy**: Simplified data pipeline from log events to `SqlBulkCopy` with fewer intermediate transformations
5. **No Reflection Overhead**: Uses pre-defined mappings with delegate-based value extraction
6. **Avoids DataTable**: Uses lightweight `IDataReader` implementation instead of `DataTable`, eliminating the overhead of DataTable's internal structures

### Simplified Codebase

- **Fewer dependencies**: Minimal external packages (only `Serilog`, `Microsoft.Data.SqlClient`, and polyfills)
- **Smaller footprint**: Focused implementation without legacy features
- **Easier to understand**: Clear, modern C# code using latest language features
- **Better maintainability**: Single-purpose design makes updates and fixes straightforward

## Installation

Install the sink via NuGet:

```bash
dotnet add package Serilog.Sinks.SqlServer
```

Or via Package Manager Console:

```powershell
Install-Package Serilog.Sinks.SqlServer
```

## Quick Start

### 1. Create the Database Table

Create a table in your SQL Server database to store log events:

```sql
CREATE TABLE [dbo].[LogEvent]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [TraceId] NVARCHAR(100) NULL,
    [SpanId] NVARCHAR(100) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [SourceContext] NVARCHAR(1000) NULL,
    CONSTRAINT [PK_LogEvent] PRIMARY KEY CLUSTERED ([Id] ASC),
    INDEX [IX_LogEvent_TimeStamp] NONCLUSTERED ([Timestamp] DESC),
    INDEX [IX_LogEvent_Level] NONCLUSTERED ([Level] ASC),
    INDEX [IX_LogEvent_TraceId] NONCLUSTERED ([TraceId] ASC),
)
WITH (DATA_COMPRESSION = PAGE);
```

### 2. Configure Serilog

#### Simple Configuration

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.SqlServer(
        connectionString: "Data Source=(local);Initial Catalog=Serilog;Integrated Security=True;TrustServerCertificate=True;",
        tableName: "LogEvent",
        tableSchema: "dbo"
    )
    .CreateLogger();

Log.Information("Hello, SQL Server!");
Log.CloseAndFlush();
```

#### Advanced Configuration with Options

```csharp
using Serilog;
using Serilog.Sinks.SqlServer;

Log.Logger = new LoggerConfiguration()
    .WriteTo.SqlServer(config =>
    {
        config.ConnectionString = "Data Source=(local);Initial Catalog=Serilog;Integrated Security=True;TrustServerCertificate=True;";
        config.TableName = "LogEvent";
        config.TableSchema = "dbo";
        config.MinimumLevel = LogEventLevel.Information;
        config.BatchSizeLimit = 100;
        config.BufferingTimeLimit = TimeSpan.FromSeconds(5);
    })
    .CreateLogger();
```

#### Configuration from appsettings.json

You can configure the sink using `appsettings.json` with the `Serilog.Settings.Configuration` package:

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "Serilog": "Data Source=(local);Initial Catalog=Serilog;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.SqlServer" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "SqlServer",
        "Args": {
          "connectionString": "Data Source=(local);Initial Catalog=Serilog;Integrated Security=True;TrustServerCertificate=True;",
          "tableName": "LogEvent",
          "tableSchema": "dbo"
        }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

**Program.cs:**

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
    );

var app = builder.Build();
app.UseSerilogRequestLogging();
app.Run();
```

Or for a console application:

```csharp
using Serilog;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Application starting");
    // Your application code
}
finally
{
    Log.CloseAndFlush();
}
```

## Configuration Options

### SqlServerSinkOptions

The `SqlServerSinkOptions` class inherits from `BatchingOptions` and provides the following properties:

| Property             | Default                            | Description                                              |
| -------------------- | ---------------------------------- | -------------------------------------------------------- |
| `ConnectionString`   | -                                  | SQL Server connection string (required)                  |
| `TableName`          | `"LogEvent"`                       | Name of the table to write logs to                       |
| `TableSchema`        | `"dbo"`                            | Schema of the table                                      |
| `MinimumLevel`       | `LevelAlias.Minimum`               | Minimum log event level                                  |
| `LevelSwitch`        | `null`                             | Dynamic level switch for runtime level adjustments       |
| `BulkCopyOptions`    | `SqlBulkCopyOptions.Default`       | SqlBulkCopy options for bulk insert operations           |
| `Mappings`           | `MappingDefaults.StandardMappings` | Column mappings for log event properties                 |
| `BatchSizeLimit`     | `1000`                             | Number of log events to batch before writing (inherited) |
| `BufferingTimeLimit` | `2 seconds`                        | Maximum time to wait before flushing a batch (inherited) |

## Column Mappings

### Standard Mappings

The sink includes the following standard column mappings:

| Column Name     | Type             | Description                              | Nullable | Size |
| --------------- | ---------------- | ---------------------------------------- | -------- | ---- |
| `Timestamp`     | `DateTimeOffset` | UTC timestamp of the log event           | No       | -    |
| `Level`         | `string`         | Log level (e.g., "Information", "Error") | No       | 50   |
| `Message`       | `string`         | Rendered log message                     | Yes      | MAX  |
| `TraceId`       | `string`         | Distributed tracing trace ID             | Yes      | 100  |
| `SpanId`        | `string`         | Distributed tracing span ID              | Yes      | 100  |
| `Exception`     | `string`         | Exception details as JSON                | Yes      | MAX  |
| `Properties`    | `string`         | Additional properties as JSON            | Yes      | MAX  |
| `SourceContext` | `string`         | Source context (typically class name)    | Yes      | 1000 |

### JSON Structure for Exception and Properties

#### Exception Column

The `Exception` column stores exception details as a JSON object with the following structure:

```json
{
  "Message": "The error message",
  "BaseMessage": "Inner exception message (if present)",
  "Type": "System.InvalidOperationException",
  "Text": "Full exception text including stack trace",
  "HResult": -2146233079,
  "ErrorCode": -2147467259,
  "Source": "MyApplication",
  "MethodName": "MyMethod",
  "ModuleName": "MyAssembly",
  "ModuleVersion": "1.0.0.0"
}
```

**Key fields:**

- `Message` - The exception's primary error message
- `BaseMessage` - Message from the innermost exception (if there's an inner exception chain)
- `Type` - Fully qualified type name of the exception
- `Text` - Complete exception text including stack trace (from `ToString()`)
- `HResult` - The HRESULT error code
- `ErrorCode` - Error code for `ExternalException` types
- `Source` - The application or object that caused the error
- `MethodName` - Name of the method that threw the exception
- `ModuleName` - Name of the assembly containing the throwing method
- `ModuleVersion` - Version of the assembly

> **Note:** Aggregate exceptions with a single inner exception are automatically flattened to the inner exception.

#### Properties Column

The `Properties` column stores log event properties as a JSON object. Property values are serialized according to their type:

**Scalar values:**

```json
{
  "UserId": 123,
  "UserName": "John Doe",
  "IsActive": true,
  "Amount": 99.99,
  "RequestId": "550e8400-e29b-41d4-a716-446655440000",
  "Timestamp": "2024-01-15T10:30:45Z"
}
```

**Structured values:**

```json
{
  "User": {
    "Id": 123,
    "Name": "John Doe",
    "Email": "john@example.com"
  }
}
```

**Arrays/Sequences:**

```json
{
  "Roles": ["Admin", "User", "Manager"],
  "Numbers": [1, 2, 3, 4, 5]
}
```

**Supported scalar types:**

- Primitive types: `string`, `bool`, `int`, `long`, `double`, `float`, `decimal`, `byte`, `short`, etc.
- Date/time types: `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` (as ISO strings)
- Other types: `Guid` (as string), `Enum` (as string), `BigInteger` (as string), `char` (as string)
- `null` values are preserved

> **Note:** By default, properties enriched via `FromLogContext()` or `WithProperty()` are included in this column. You can extract specific properties to dedicated columns using custom mappings (see below).

### Custom Property Mappings

Add custom property mappings to log additional data:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("ApplicationName", "MyApp")
    .Enrich.WithProperty("ApplicationVersion", "1.0.0")
    .Enrich.WithProperty("EnvironmentName", "Production")
    .WriteTo.SqlServer(config =>
    {
        config.ConnectionString = connectionString;
        config.TableName = "LogExtended";
        
        // Add custom property mappings
        config.AddPropertyMapping("ApplicationName", size: 500);
        config.AddPropertyMapping("ApplicationVersion", size: 500);
        config.AddPropertyMapping("EnvironmentName", size: 500);
    })
    .CreateLogger();
```

Corresponding table structure:

```sql
CREATE TABLE [dbo].[LogExtended]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [TraceId] NVARCHAR(100) NULL,
    [SpanId] NVARCHAR(100) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [SourceContext] NVARCHAR(1000) NULL,
    [ApplicationName] NVARCHAR(500) NULL,
    [ApplicationVersion] NVARCHAR(500) NULL,
    [EnvironmentName] NVARCHAR(500) NULL,
    CONSTRAINT [PK_LogExtended] PRIMARY KEY CLUSTERED ([Id] ASC)
);
```

### Advanced Custom Mappings

For more control, define custom mappings manually:

```csharp
config.Mappings.Add(
    new ColumnMapping<LogEvent>(
        ColumnName: "MachineName",
        ColumnType: typeof(string),
        GetValue: logEvent => Environment.MachineName,
        Nullable: true,
        Size: 100
    )
);
```

> **Note:** When you specify a `Size` for string columns, the sink automatically truncates values that exceed the specified length to prevent SQL insert errors. For example, if `Size` is set to 100 and a value is 150 characters, it will be truncated to 100 characters before insertion. Columns without a `Size` specified (or with `Size: null`) will not be truncated.

### Clearing and Replacing Mappings

```csharp
config.Mappings.Clear(); // Remove all default mappings
config.Mappings.Add(MappingDefaults.TimestampMapping);
config.Mappings.Add(MappingDefaults.LevelMapping);
config.Mappings.Add(MappingDefaults.MessageMapping);
// Add only the mappings you need
```

## Integration with ASP.NET Core

### Program.cs Configuration

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(loggerConfiguration =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.SqlServer(config =>
        {
            config.ConnectionString = builder.Configuration.GetConnectionString("Serilog");
            config.TableName = "LogEvent";
        });
});

var app = builder.Build();
app.UseSerilogRequestLogging();
app.Run();
```

### appsettings.json Configuration

```json
{
  "ConnectionStrings": {
    "Serilog": "Data Source=(local);Initial Catalog=Serilog;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## Performance Tuning

### Batching Configuration

Adjust batch size and timeout based on your logging volume:

```csharp
config.BatchSizeLimit = 1000;              // Larger batches for high-volume scenarios
config.BufferingTimeLimit = TimeSpan.FromSeconds(10); // Longer timeout for efficiency
```

### SQL Bulk Copy Options

Configure bulk copy behavior:

```csharp
using Microsoft.Data.SqlClient;

config.BulkCopyOptions = SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers;
```

### Index Strategy

Create appropriate indexes for your query patterns:

```sql
-- Optimize for time-based queries
CREATE INDEX [IX_LogEvent_TimeStamp] ON [dbo].[LogEvent] ([Timestamp] DESC);

-- Optimize for filtering by level
CREATE INDEX [IX_LogEvent_Level] ON [dbo].[LogEvent] ([Level] ASC);

-- Optimize for distributed tracing queries
CREATE INDEX [IX_LogEvent_TraceId] ON [dbo].[LogEvent] ([TraceId] ASC);
```

## Best Practices

1. **Use Batching**: The sink uses batching by default for optimal performance
2. **Add Indexes**: Create indexes on columns you frequently query (Timestamp, Level, TraceId)
3. **Data Compression**: Use page compression to reduce storage costs
4. **Partition Large Tables**: Consider table partitioning for very large log volumes
5. **Implement Archival**: Archive or delete old log data regularly
6. **Monitor Performance**: Monitor SQL Server performance and adjust batch sizes accordingly
7. **Use Connection Pooling**: Connection pooling is enabled by default in SqlClient

## Table Creation

**This library does not automatically create database tables.** You are responsible for creating the target log table before using the sink.

### Why No Auto-Creation?

The sink prioritizes **performance and efficiency** by design. Automatic table creation would introduce several challenges:

1. **Complexity**: Managing different SQL Server versions, permissions, and configuration options would add significant complexity
2. **Performance Overhead**: Schema checks and potential table creation on startup would add latency to application initialization
3. **Security Concerns**: Many production environments don't grant `CREATE TABLE` permissions to application accounts
4. **Flexibility Loss**: Users have different requirements for:
   - Partitioning strategies
   - Index configurations
   - Data compression settings
   - Filegroup placement
   - Custom column types and constraints

### Creating Your Table

You have full control over your table schema. At minimum, create columns that match your configured mappings:

```sql
CREATE TABLE [dbo].[LogEvent]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NULL,
    [TraceId] NVARCHAR(100) NULL,
    [SpanId] NVARCHAR(100) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [SourceContext] NVARCHAR(1000) NULL,
    CONSTRAINT [PK_LogEvent] PRIMARY KEY CLUSTERED ([Id] ASC)
)
WITH (DATA_COMPRESSION = PAGE);
```

You can add indexes, partitioning, and other optimizations based on your specific requirements. See the [Quick Start](#quick-start) section for complete table examples.

## Log Management and Archival

As your application generates logs over time, the log table will grow continuously. It's important to implement a log retention strategy to manage storage costs and maintain query performance.

### Automated Log Purging

The following stored procedure provides an efficient way to delete old log entries in batches, preventing transaction log bloat and minimizing system impact:

```sql
CREATE PROCEDURE dbo.PurgeLogs
    @RetentionDays INT = 90,
    @BatchSize INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate the threshold date
    DECLARE @Threshold DATETIMEOFFSET = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
    DECLARE @RowsDeleted INT = 0;
    DECLARE @TotalDeleted INT = 0;

    -- Validate batch size
    IF @BatchSize <= 0
        SET @BatchSize = 5000;
   
    -- Delete in batches to prevent transaction log bloat and reduce lock contention
    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize)
        FROM dbo.LogEvent WITH (ROWLOCK)
        WHERE [Timestamp] < @Threshold;

        SET @RowsDeleted = @@ROWCOUNT;
        SET @TotalDeleted = @TotalDeleted + @RowsDeleted;

        -- Exit if no more rows to delete
        IF @RowsDeleted = 0
            BREAK;

        -- Brief delay between batches to reduce contention
        WAITFOR DELAY '00:00:00.100';  -- 100ms
    END
   
    -- Return summary
    SELECT @TotalDeleted AS TotalRowsDeleted;
END
```

**How the procedure works:**

1. **Batch Processing**: Deletes records in configurable batches (default 10,000 rows) rather than all at once
   - Prevents transaction log from filling up
   - Reduces lock duration and blocking
   - Allows other queries to access the table between batches
   - Makes the operation recoverable if interrupted

2. **Row-Level Locking**: Uses `WITH (ROWLOCK)` hint to minimize lock escalation and reduce contention with concurrent operations

3. **Threshold-Based Deletion**: Removes logs older than the specified retention period (default 90 days)

4. **Progress Tracking**: Returns total deleted rows and provides feedback during execution

### Alternative: Table Partitioning

For very high-volume logging scenarios, consider implementing table partitioning by date:

- **Benefits**: Near-instant deletion of old partitions, better query performance, easier maintenance
- **Trade-off**: More complex initial setup and management
- **Recommended for**: Systems generating millions of log entries per day

See [SQL Server table partitioning documentation](https://learn.microsoft.com/en-us/sql/relational-databases/partitions/partitioned-tables-and-indexes) for implementation details.

## Troubleshooting

### Connection Issues

Enable Serilog self-logging to diagnose connection problems:

```csharp
using Serilog.Debugging;

SelfLog.Enable(Console.Error);
```

### Schema Mismatches

Ensure your database table schema matches your column mappings. Missing columns will cause bulk insert to fail.

### Performance Issues

- Increase `BatchSizeLimit` for high-volume scenarios
- Increase `BufferingTimeLimit` to allow more batching
- Ensure proper indexing on the database table
- Monitor SQL Server performance counters

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
