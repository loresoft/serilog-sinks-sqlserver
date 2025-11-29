using System.Buffers;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using Serilog.Events;

namespace Serilog.Sinks.SqlServer;

public static class JsonWriter
{
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

            writer.WriteString("Message", exception.Message);

            // include base exception message
            if (exception.InnerException != null)
                writer.WriteString("BaseMessage", exception.GetBaseException().Message);

            writer.WriteString("Type", exception.GetType().FullName);
            writer.WriteString("Text", exception.ToString());

            if (exception is ExternalException external)
                writer.WriteNumber("ErrorCode", external.ErrorCode);

            writer.WriteNumber("HResult", exception.HResult);

            if (!string.IsNullOrEmpty(exception.Source))
                writer.WriteString("Source", exception.Source);

            var method = exception.TargetSite;
            if (method != null)
            {
                writer.WriteString("MethodName", method.Name);

                var assembly = method.Module?.Assembly?.GetName();
                if (assembly != null)
                {
                    writer.WriteString("ModuleName", assembly.Name);
                    writer.WriteString("ModuleVersion", assembly.Version?.ToString());
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


    public static void WritePropertyValue(Utf8JsonWriter writer, LogEventPropertyValue value)
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

    public static void WriteDictionaryValue(Utf8JsonWriter writer, DictionaryValue dictionaryValue)
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

    public static void WriteStructureValue(Utf8JsonWriter writer, StructureValue structureValue)
    {
        writer.WriteStartObject();
        foreach (var prop in structureValue.Properties)
        {
            writer.WritePropertyName(prop.Name);
            WritePropertyValue(writer, prop.Value);
        }
        writer.WriteEndObject();
    }

    public static void WriteSequenceValue(Utf8JsonWriter writer, SequenceValue sequenceValue)
    {
        writer.WriteStartArray();
        foreach (var item in sequenceValue.Elements)
        {
            WritePropertyValue(writer, item);
        }
        writer.WriteEndArray();
    }

    public static void WriteScalarValue(Utf8JsonWriter writer, ScalarValue scalarValue)
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
