using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

public static class LogEventExtensions
{
    public static object? GetPropertyValue(this LogEvent logEvent, string propertyName)
    {
        if (logEvent == null || string.IsNullOrEmpty(propertyName))
            return null;

        if (!logEvent.Properties.TryGetValue(propertyName, out var propertyValue))
            return null;

        // ScalarValue, return the underlying value directly
        if (propertyValue is ScalarValue scalarValue)
            return scalarValue.Value;

        // For other types, serialize to JSON string
        return JsonWriter.WritePropertyValue(propertyValue);
    }
}
