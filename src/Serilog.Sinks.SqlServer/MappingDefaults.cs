using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

public static class MappingDefaults
{
    public const string TableSchema = "dbo";
    public const string TableName = "LogEvent";

    public const string TimestampName = "Timestamp";
    public const string LevelName = "Level";
    public const string MessageName = "Message";
    public const string TraceIdName = "TraceId";
    public const string SpanIdName = "SpanId";
    public const string ExceptionName = "Exception";
    public const string PropertiesName = "Properties";
    public const string SourceContextName = "SourceContext";

    public static readonly ColumnMapping<LogEvent> TimestampMapping = new(
        ColumnName: TimestampName,
        ColumnType: typeof(DateTimeOffset),
        GetValue: static logEvent => logEvent.Timestamp.ToUniversalTime(),
        Nullable: false
    );

    public static readonly ColumnMapping<LogEvent> LevelMapping = new(
        ColumnName: LevelName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.Level.ToString(),
        Nullable: false,
        Size: 50
    );

    public static readonly ColumnMapping<LogEvent> MessageMapping = new(
        ColumnName: MessageName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.RenderMessage()
    );

    public static readonly ColumnMapping<LogEvent> TraceIdMapping = new(
        ColumnName: TraceIdName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.TraceId?.ToHexString(),
        Size: 100
    );

    public static readonly ColumnMapping<LogEvent> SpanIdMapping = new(
        ColumnName: SpanIdName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.SpanId?.ToHexString(),
        Size: 100
    );

    public static readonly ColumnMapping<LogEvent> ExceptionMapping = new(
        ColumnName: ExceptionName,
        ColumnType: typeof(string),
        GetValue: static logEvent => JsonWriter.WriteException(logEvent.Exception)
    );

    public static readonly ColumnMapping<LogEvent> PropertiesMapping = new(
        ColumnName: PropertiesName,
        ColumnType: typeof(string),
        GetValue: static logEvent => JsonWriter.WriteProperties(logEvent.Properties)
    );

    public static readonly ColumnMapping<LogEvent> SourceContextMapping = new(
        ColumnName: SourceContextName,
        ColumnType: typeof(string),
        GetValue: static logEvent => logEvent.GetPropertyValue(SourceContextName),
        Size: 1000
    );

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
