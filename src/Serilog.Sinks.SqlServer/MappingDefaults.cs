using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Provides default column mappings and naming conventions for logging Serilog events to SQL Server.
/// </summary>
/// <remarks>
/// This class defines standard column names, mappings, and a complete set of default mappings for common log event properties.
/// These defaults can be used as-is or customized for specific logging requirements.
/// </remarks>
public static class MappingDefaults
{
    /// <summary>
    /// The default database schema name for the log table.
    /// </summary>
    public const string TableSchema = "dbo";

    /// <summary>
    /// The default table name for storing log events.
    /// </summary>
    public const string TableName = "LogEvent";

    /// <summary>
    /// The default column name for the log event timestamp.
    /// </summary>
    public const string TimestampName = "Timestamp";

    /// <summary>
    /// The default column name for the log event level.
    /// </summary>
    public const string LevelName = "Level";

    /// <summary>
    /// The default column name for the log event message.
    /// </summary>
    public const string MessageName = "Message";

    /// <summary>
    /// The default column name for the distributed tracing trace ID.
    /// </summary>
    public const string TraceIdName = "TraceId";

    /// <summary>
    /// The default column name for the distributed tracing span ID.
    /// </summary>
    public const string SpanIdName = "SpanId";

    /// <summary>
    /// The default column name for exception information.
    /// </summary>
    public const string ExceptionName = "Exception";

    /// <summary>
    /// The default column name for additional log event properties.
    /// </summary>
    public const string PropertiesName = "Properties";

    /// <summary>
    /// The default column name for the source context property.
    /// </summary>
    public const string SourceContextName = "SourceContext";

    /// <summary>
    /// Gets the default column mapping for the log event timestamp.
    /// </summary>
    /// <remarks>
    /// Maps to a <see cref="DateTimeOffset"/> column. Timestamp is converted to UTC. This column is not nullable.
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> TimestampMapping = new(
        ColumnName: TimestampName,
        ColumnType: typeof(DateTimeOffset),
        GetValue: static logEvent => logEvent.Timestamp.ToUniversalTime(),
        Nullable: false
    );

    /// <summary>
    /// Gets the default column mapping for the log event level.
    /// </summary>
    /// <remarks>
    /// Maps to a string column with a maximum size of 50 characters. This column is not nullable.
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> LevelMapping = new(
        ColumnName: LevelName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.Level.ToString(),
        Nullable: false,
        Size: 50
    );

    /// <summary>
    /// Gets the default column mapping for the rendered log event message.
    /// </summary>
    /// <remarks>
    /// Maps to a nullable string column containing the fully rendered message template.
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> MessageMapping = new(
        ColumnName: MessageName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.RenderMessage()
    );

    /// <summary>
    /// Gets the default column mapping for the distributed tracing trace ID.
    /// </summary>
    /// <remarks>
    /// Maps to a nullable string column with a maximum size of 100 characters. Contains the trace ID in hexadecimal format.
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> TraceIdMapping = new(
        ColumnName: TraceIdName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.TraceId?.ToHexString(),
        Size: 100
    );

    /// <summary>
    /// Gets the default column mapping for the distributed tracing span ID.
    /// </summary>
    /// <remarks>
    /// Maps to a nullable string column with a maximum size of 100 characters. Contains the span ID in hexadecimal format.
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> SpanIdMapping = new(
        ColumnName: SpanIdName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.SpanId?.ToHexString(),
        Size: 100
    );

    /// <summary>
    /// Gets the default column mapping for exception information.
    /// </summary>
    /// <remarks>
    /// Maps to a nullable string column containing exception details serialized as JSON.
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> ExceptionMapping = new(
        ColumnName: ExceptionName,
        ColumnType: typeof(string),
        GetValue: static logEvent => JsonWriter.WriteException(logEvent.Exception)
    );

    /// <summary>
    /// Gets the default column mapping for additional log event properties.
    /// </summary>
    /// <remarks>
    /// Maps to a nullable string column containing all log event properties serialized as JSON.
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> PropertiesMapping = new(
        ColumnName: PropertiesName,
        ColumnType: typeof(string),
        GetValue: static logEvent => JsonWriter.WriteProperties(logEvent.Properties)
    );

    /// <summary>
    /// Gets the default column mapping for the source context property.
    /// </summary>
    /// <remarks>
    /// Maps to a nullable string column with a maximum size of 1000 characters. 
    /// Contains the source context (typically the full type name of the class creating the log event).
    /// </remarks>
    public static readonly ColumnMapping<LogEvent> SourceContextMapping = new(
        ColumnName: SourceContextName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.GetPropertyValue(SourceContextName),
        Size: 1000
    );

    /// <summary>
    /// Gets the complete set of standard column mappings for log events.
    /// </summary>
    /// <remarks>
    /// This list includes mappings for: Timestamp, Level, Message, TraceId, SpanId, Exception, Properties, and SourceContext.
    /// These mappings provide a comprehensive default configuration for logging to SQL Server.
    /// </remarks>
    public static readonly List<ColumnMapping<LogEvent>> StandardMappings =
    [
        TimestampMapping,
        LevelMapping,
        MessageMapping,
        TraceIdMapping,
        SpanIdMapping,
        ExceptionMapping,
        PropertiesMapping,
        SourceContextMapping
    ];
}
