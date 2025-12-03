using System.Buffers;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

/// <summary>
/// Provides utility methods for writing Serilog log event data to JSON format.
/// </summary>
public static class JsonWriter
{
    // Pre-encoded JSON property names for exception details
    private static readonly JsonEncodedText MessageProperty = JsonEncodedText.Encode("Message");
    private static readonly JsonEncodedText BaseMessageProperty = JsonEncodedText.Encode("BaseMessage");
    private static readonly JsonEncodedText TypeProperty = JsonEncodedText.Encode("Type");
    private static readonly JsonEncodedText TextProperty = JsonEncodedText.Encode("Text");
    private static readonly JsonEncodedText ErrorCodeProperty = JsonEncodedText.Encode("ErrorCode");
    private static readonly JsonEncodedText HResultProperty = JsonEncodedText.Encode("HResult");
    private static readonly JsonEncodedText SourceProperty = JsonEncodedText.Encode("Source");
    private static readonly JsonEncodedText MethodNameProperty = JsonEncodedText.Encode("MethodName");
    private static readonly JsonEncodedText ModuleNameProperty = JsonEncodedText.Encode("ModuleName");
    private static readonly JsonEncodedText ModuleVersionProperty = JsonEncodedText.Encode("ModuleVersion");

    /// <summary>
    /// Writes an exception to a JSON string representation.
    /// </summary>
    /// <param name="exception">The exception to serialize. Can be null.</param>
    /// <returns>A JSON string containing exception details, or null if the exception is null.</returns>
    /// <remarks>
    /// The JSON output includes the exception message, type, full text, error codes, source, and method information.
    /// Aggregate exceptions with a single inner exception are automatically flattened.
    /// </remarks>
    public static string? WriteException(Exception? exception)
    {
        if (exception == null)
            return null;

        // flatten aggregate exceptions with a single inner exception
        if (exception is AggregateException aggregateException)
        {
            aggregateException = aggregateException.Flatten();
            if (aggregateException.InnerExceptions?.Count == 1)
                exception = aggregateException.InnerExceptions[0];
            else
                exception = aggregateException;
        }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
#else
        using MemoryStream stream = new();
        using (var writer = new Utf8JsonWriter(stream))
#endif
        {
            writer.WriteStartObject();

            writer.WriteString(MessageProperty, exception.Message);

            // include base exception message
            if (exception.InnerException != null)
                writer.WriteString(BaseMessageProperty, exception.GetBaseException().Message);

            writer.WriteString(TypeProperty, exception.GetType().FullName);
            writer.WriteString(TextProperty, exception.ToString());

            if (exception is ExternalException external)
                writer.WriteNumber(ErrorCodeProperty, external.ErrorCode);

            writer.WriteNumber(HResultProperty, exception.HResult);

            if (!string.IsNullOrEmpty(exception.Source))
                writer.WriteString(SourceProperty, exception.Source);

            var method = exception.TargetSite;
            if (method != null)
            {
                writer.WriteString(MethodNameProperty, method.Name);

                var assembly = method.Module?.Assembly?.GetName();
                if (assembly != null)
                {
                    writer.WriteString(ModuleNameProperty, assembly.Name);
                    writer.WriteString(ModuleVersionProperty, assembly.Version?.ToString());
                }
            }

            writer.WriteEndObject();
            writer.Flush();
        }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
#else
        return Encoding.UTF8.GetString(stream.ToArray());
#endif

    }

    /// <summary>
    /// Writes a collection of log event properties to a JSON string representation.
    /// </summary>
    /// <param name="properties">The dictionary of log event properties to serialize. Can be null or empty.</param>
    /// <param name="ignored">An optional set of property names to exclude from the output.</param>
    /// <returns>A JSON string containing the properties, or null if there are no properties to write or all properties are ignored.</returns>
    public static string? WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue>? properties, HashSet<string>? ignored = null)
    {
        // no properties to write
        if (properties == null || properties.Count == 0)
            return null;

        // properties that are not ignored
        var included = properties
            .Where(p => ignored?.Contains(p.Key) != true)
            .ToList();

        // all properties were ignored
        if (included.Count == 0)
            return null;

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
#else
        using MemoryStream stream = new();
        using (var writer = new Utf8JsonWriter(stream))
#endif
        {
            writer.WriteStartObject();
            foreach (var kvp in included)
            {
                writer.WritePropertyName(kvp.Key);
                WritePropertyValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
            writer.Flush();
        }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
#else
        return Encoding.UTF8.GetString(stream.ToArray());
#endif
    }

    /// <summary>
    /// Writes a single log event property value to a JSON string representation.
    /// </summary>
    /// <param name="value">The log event property value to serialize. Can be null.</param>
    /// <returns>A JSON string containing the property value, or null if the value is null.</returns>
    public static string? WritePropertyValue(LogEventPropertyValue value)
    {
        if (value == null)
            return null;

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
#else
        using MemoryStream stream = new();
        using (var writer = new Utf8JsonWriter(stream))
#endif
        {
            WritePropertyValue(writer, value);
            writer.Flush();
        }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
#else
        return Encoding.UTF8.GetString(stream.ToArray());
#endif
    }

    /// <summary>
    /// Writes a log event property value to the specified JSON writer.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer to write to.</param>
    /// <param name="value">The log event property value to write.</param>
    /// <remarks>
    /// Handles scalar values, sequences, structures, and dictionaries appropriately.
    /// </remarks>
    private static void WritePropertyValue(Utf8JsonWriter writer, LogEventPropertyValue value)
    {
        if (value is ScalarValue scalarValue)
            WriteScalarValue(writer, scalarValue);
        else if (value is SequenceValue sequenceValue)
            WriteSequenceValue(writer, sequenceValue);
        else if (value is StructureValue structureValue)
            WriteStructureValue(writer, structureValue);
        else if (value is DictionaryValue dictionaryValue)
            WriteDictionaryValue(writer, dictionaryValue);
        else
            writer.WriteStringValue(value.ToString());
    }

    /// <summary>
    /// Writes a dictionary value to the specified JSON writer as a JSON object.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer to write to.</param>
    /// <param name="dictionaryValue">The dictionary value to write.</param>
    private static void WriteDictionaryValue(Utf8JsonWriter writer, DictionaryValue dictionaryValue)
    {
        writer.WriteStartObject();
        foreach (var kvp in dictionaryValue.Elements)
        {
            if (kvp.Key is ScalarValue keyScalar && keyScalar.Value != null)
            {
                writer.WritePropertyName(keyScalar.Value.ToString()!);
                WritePropertyValue(writer, kvp.Value);
            }
        }
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a structure value to the specified JSON writer as a JSON object.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer to write to.</param>
    /// <param name="structureValue">The structure value to write.</param>
    private static void WriteStructureValue(Utf8JsonWriter writer, StructureValue structureValue)
    {
        writer.WriteStartObject();
        foreach (var prop in structureValue.Properties)
        {
            writer.WritePropertyName(prop.Name);
            WritePropertyValue(writer, prop.Value);
        }
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a sequence value to the specified JSON writer as a JSON array.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer to write to.</param>
    /// <param name="sequenceValue">The sequence value to write.</param>
    private static void WriteSequenceValue(Utf8JsonWriter writer, SequenceValue sequenceValue)
    {
        writer.WriteStartArray();
        foreach (var item in sequenceValue.Elements)
        {
            WritePropertyValue(writer, item);
        }
        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a scalar value to the specified JSON writer with appropriate JSON typing.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer to write to.</param>
    /// <param name="scalarValue">The scalar value to write.</param>
    /// <remarks>
    /// Handles all primitive types, date/time types, numeric types, and falls back to JSON serialization for unknown types.
    /// </remarks>
    private static void WriteScalarValue(Utf8JsonWriter writer, ScalarValue scalarValue)
    {
        if (scalarValue.Value == null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (scalarValue.Value)
        {
            case string str:
                writer.WriteStringValue(str);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case byte by:
                writer.WriteNumberValue(by);
                break;
            case short s:
                writer.WriteNumberValue(s);
                break;
            case uint ui:
                writer.WriteNumberValue(ui);
                break;
            case ulong ul:
                writer.WriteNumberValue(ul);
                break;
            case ushort us:
                writer.WriteNumberValue(us);
                break;
            case sbyte sb:
                writer.WriteNumberValue(sb);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt);
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto);
                break;
            case TimeSpan ts:
                writer.WriteStringValue(ts.ToString());
                break;
#if NET6_0_OR_GREATER
            case DateOnly dateOnly:
                writer.WriteStringValue(dateOnly.ToString("O"));
                break;
            case TimeOnly timeOnly:
                writer.WriteStringValue(timeOnly.ToString("O"));
                break;
#endif
            case Guid g:
                writer.WriteStringValue(g);
                break;
#if NET5_0_OR_GREATER
            case Half h:
                writer.WriteNumberValue((double)h);
                break;
#endif
#if NET7_0_OR_GREATER
            case Int128 i128:
                writer.WriteStringValue(i128.ToString());
                break;
            case UInt128 ui128:
                writer.WriteStringValue(ui128.ToString());
                break;
#endif
            case nint ni:
                writer.WriteNumberValue(ni);
                break;
            case nuint nui:
                writer.WriteNumberValue(nui);
                break;
            case System.Numerics.BigInteger bi:
                writer.WriteStringValue(bi.ToString());
                break;
            case Enum e:
                writer.WriteStringValue(e.ToString());
                break;
            case char c:
                writer.WriteStringValue(c.ToString());
                break;
            default:
                JsonSerializer.Serialize(writer, scalarValue.Value);
                break;
        }
    }
}
