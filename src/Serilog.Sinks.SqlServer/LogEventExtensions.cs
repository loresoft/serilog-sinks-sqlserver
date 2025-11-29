using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Provides extension methods for <see cref="LogEvent"/> to simplify property value extraction.
/// </summary>
public static class LogEventExtensions
{
    /// <summary>
    /// Gets the value of a specified property from the log event.
    /// </summary>
    /// <param name="logEvent">The log event to extract the property from.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>
    /// The underlying value if the property is a <see cref="ScalarValue"/>, 
    /// a JSON string representation for complex types (sequences, structures, dictionaries),
    /// or <c>null</c> if the log event is null, the property name is empty, or the property does not exist.
    /// </returns>
    /// <remarks>
    /// Scalar values are returned directly as their underlying type. 
    /// Complex property types (sequences, structures, dictionaries) are serialized to JSON strings.
    /// </remarks>
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
