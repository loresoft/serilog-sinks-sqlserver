using System.Runtime.InteropServices;
using System.Text.Json;

using AwesomeAssertions;

using Serilog.Events;

namespace Serilog.Sinks.SqlServer.Tests;

public class JsonWriterTests
{
    #region WriteException Tests

    [Fact]
    public void WriteException_WithNull_ShouldReturnNull()
    {
        // Act
        var result = JsonWriter.WriteException(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WriteException_WithSimpleException_ShouldReturnValidJson()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error message");

        // Act
        var result = JsonWriter.WriteException(exception);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.GetProperty("Message").GetString().Should().Be("Test error message");
        root.GetProperty("Type").GetString().Should().Be("System.InvalidOperationException");
        root.GetProperty("Text").GetString().Should().Contain("Test error message");
        root.TryGetProperty("HResult", out var hResult).Should().BeTrue();
    }

    [Fact]
    public void WriteException_WithInnerException_ShouldIncludeBaseMessage()
    {
        // Arrange
        var innerException = new ArgumentException("Inner error");
        var exception = new InvalidOperationException("Outer error", innerException);

        // Act
        var result = JsonWriter.WriteException(exception);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.GetProperty("Message").GetString().Should().Be("Outer error");
        root.GetProperty("BaseMessage").GetString().Should().Be("Inner error");
        root.GetProperty("Type").GetString().Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public void WriteException_WithExceptionSource_ShouldIncludeSource()
    {
        // Arrange
        var exception = new Exception("Test error")
        {
            Source = "TestSource"
        };

        // Act
        var result = JsonWriter.WriteException(exception);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.GetProperty("Source").GetString().Should().Be("TestSource");
    }

    [Fact]
    public void WriteException_WithExternalException_ShouldIncludeErrorCode()
    {
        // Arrange
        var exception = new ExternalException("External error", unchecked((int)0x80004005));

        // Act
        var result = JsonWriter.WriteException(exception);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.GetProperty("Message").GetString().Should().Be("External error");
        root.GetProperty("ErrorCode").GetInt32().Should().Be(unchecked((int)0x80004005));
    }

    [Fact]
    public void WriteException_WithAggregateExceptionSingleInner_ShouldFlatten()
    {
        // Arrange
        var innerException = new ArgumentException("Single inner error");
        var aggregateException = new AggregateException(innerException);

        // Act
        var result = JsonWriter.WriteException(aggregateException);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        // Should be flattened to the inner exception
        root.GetProperty("Type").GetString().Should().Be("System.ArgumentException");
        root.GetProperty("Message").GetString().Should().Be("Single inner error");
    }

    [Fact]
    public void WriteException_WithAggregateExceptionMultipleInner_ShouldNotFlatten()
    {
        // Arrange
        var inner1 = new ArgumentException("First error");
        var inner2 = new InvalidOperationException("Second error");
        var aggregateException = new AggregateException(inner1, inner2);

        // Act
        var result = JsonWriter.WriteException(aggregateException);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        // Should remain as AggregateException
        root.GetProperty("Type").GetString().Should().Be("System.AggregateException");
    }

    [Fact]
    public void WriteException_WithTargetSite_ShouldIncludeMethodInfo()
    {
        // Arrange
        Exception? capturedException = null;
        try
        {
            ThrowTestException();
        }
        catch (Exception ex)
        {
            capturedException = ex;
        }

        // Act
        var result = JsonWriter.WriteException(capturedException);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.GetProperty("MethodName").GetString().Should().Be("ThrowTestException");
        root.TryGetProperty("ModuleName", out _).Should().BeTrue();
    }

    #endregion

    #region WriteProperties Tests

    [Fact]
    public void WriteProperties_WithNull_ShouldReturnNull()
    {
        // Act
        var result = JsonWriter.WriteProperties(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WriteProperties_WithEmptyDictionary_ShouldReturnNull()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Act
        var result = JsonWriter.WriteProperties(properties);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WriteProperties_WithSimpleProperties_ShouldReturnValidJson()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["UserId"] = new ScalarValue(123),
            ["UserName"] = new ScalarValue("John Doe"),
            ["IsActive"] = new ScalarValue(true)
        };

        // Act
        var result = JsonWriter.WriteProperties(properties);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.GetProperty("UserId").GetInt32().Should().Be(123);
        root.GetProperty("UserName").GetString().Should().Be("John Doe");
        root.GetProperty("IsActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void WriteProperties_WithIgnoredProperties_ShouldExcludeThem()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["UserId"] = new ScalarValue(123),
            ["Password"] = new ScalarValue("secret"),
            ["UserName"] = new ScalarValue("John Doe")
        };
        var ignored = new HashSet<string> { "Password" };

        // Act
        var result = JsonWriter.WriteProperties(properties, ignored);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("UserId", out _).Should().BeTrue();
        root.TryGetProperty("UserName", out _).Should().BeTrue();
        root.TryGetProperty("Password", out _).Should().BeFalse();
    }

    [Fact]
    public void WriteProperties_WithAllPropertiesIgnored_ShouldReturnNull()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["UserId"] = new ScalarValue(123),
            ["UserName"] = new ScalarValue("John Doe")
        };
        var ignored = new HashSet<string> { "UserId", "UserName" };

        // Act
        var result = JsonWriter.WriteProperties(properties, ignored);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region WritePropertyValue Tests

    [Fact]
    public void WritePropertyValue_WithNull_ShouldReturnNull()
    {
        // Act
        var result = JsonWriter.WritePropertyValue(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WritePropertyValue_WithScalarValue_ShouldReturnJson()
    {
        // Arrange
        var value = new ScalarValue(42);

        // Act
        var result = JsonWriter.WritePropertyValue(value);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("42");
    }

    [Fact]
    public void WritePropertyValue_WithSequenceValue_ShouldReturnJsonArray()
    {
        // Arrange
        var value = new SequenceValue([
            new ScalarValue(1),
            new ScalarValue(2),
            new ScalarValue(3)
        ]);

        // Act
        var result = JsonWriter.WritePropertyValue(value);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().Be(3);
        root[0].GetInt32().Should().Be(1);
        root[1].GetInt32().Should().Be(2);
        root[2].GetInt32().Should().Be(3);
    }

    [Fact]
    public void WritePropertyValue_WithStructureValue_ShouldReturnJsonObject()
    {
        // Arrange
        var value = new StructureValue([
            new LogEventProperty("Name", new ScalarValue("John")),
            new LogEventProperty("Age", new ScalarValue(30))
        ]);

        // Act
        var result = JsonWriter.WritePropertyValue(value);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("Name").GetString().Should().Be("John");
        root.GetProperty("Age").GetInt32().Should().Be(30);
    }

    [Fact]
    public void WritePropertyValue_WithDictionaryValue_ShouldReturnJsonObject()
    {
        // Arrange
        var value = new DictionaryValue([
            new(new ScalarValue("key1"), new ScalarValue("value1")),
            new(new ScalarValue("key2"), new ScalarValue(42))
        ]);

        // Act
        var result = JsonWriter.WritePropertyValue(value);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("key1").GetString().Should().Be("value1");
        root.GetProperty("key2").GetInt32().Should().Be(42);
    }

    #endregion

    #region WriteScalarValue Tests

    [Fact]
    public void WriteScalarValue_WithNull_ShouldWriteNullValue()
    {
        // Arrange
        var scalarValue = new ScalarValue(null);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("null");
    }

    [Fact]
    public void WriteScalarValue_WithString_ShouldWriteStringValue()
    {
        // Arrange
        var scalarValue = new ScalarValue("Hello World");

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetString().Should().Be("Hello World");
    }

    [Fact]
    public void WriteScalarValue_WithBoolean_ShouldWriteBooleanValue()
    {
        // Arrange
        var trueValue = new ScalarValue(true);
        var falseValue = new ScalarValue(false);

        // Act
        var trueResult = JsonWriter.WritePropertyValue(trueValue);
        var falseResult = JsonWriter.WritePropertyValue(falseValue);

        // Assert
        trueResult.Should().Be("true");
        falseResult.Should().Be("false");
    }

    [Theory]
    [InlineData(42)]
    [InlineData(-123)]
    [InlineData(0)]
    public void WriteScalarValue_WithInt_ShouldWriteNumberValue(int value)
    {
        // Arrange
        var scalarValue = new ScalarValue(value);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetInt32().Should().Be(value);
    }

    [Theory]
    [InlineData(42L)]
    [InlineData(-9223372036854775808L)]
    [InlineData(9223372036854775807L)]
    public void WriteScalarValue_WithLong_ShouldWriteNumberValue(long value)
    {
        // Arrange
        var scalarValue = new ScalarValue(value);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetInt64().Should().Be(value);
    }

    [Theory]
    [InlineData(3.14)]
    [InlineData(-2.718)]
    [InlineData(0.0)]
    public void WriteScalarValue_WithDouble_ShouldWriteNumberValue(double value)
    {
        // Arrange
        var scalarValue = new ScalarValue(value);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetDouble().Should().Be(value);
    }

    [Fact]
    public void WriteScalarValue_WithFloat_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue(3.14f);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        Math.Abs(jsonDoc.RootElement.GetSingle() - 3.14f).Should().BeLessThan(0.001f);
    }

    [Fact]
    public void WriteScalarValue_WithDecimal_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue(123.45m);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetDecimal().Should().Be(123.45m);
    }

    [Theory]
    [InlineData((byte)255)]
    [InlineData((byte)0)]
    public void WriteScalarValue_WithByte_ShouldWriteNumberValue(byte value)
    {
        // Arrange
        var scalarValue = new ScalarValue(value);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetByte().Should().Be(value);
    }

    [Theory]
    [InlineData((short)32767)]
    [InlineData((short)-32768)]
    public void WriteScalarValue_WithShort_ShouldWriteNumberValue(short value)
    {
        // Arrange
        var scalarValue = new ScalarValue(value);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetInt16().Should().Be(value);
    }

    [Fact]
    public void WriteScalarValue_WithUInt_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue(4294967295u);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetUInt32().Should().Be(4294967295u);
    }

    [Fact]
    public void WriteScalarValue_WithULong_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue(18446744073709551615ul);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetUInt64().Should().Be(18446744073709551615ul);
    }

    [Fact]
    public void WriteScalarValue_WithUShort_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue((ushort)65535);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetUInt16().Should().Be(65535);
    }

    [Fact]
    public void WriteScalarValue_WithSByte_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue((sbyte)-128);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetSByte().Should().Be(-128);
    }

    [Fact]
    public void WriteScalarValue_WithDateTime_ShouldWriteStringValue()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        var scalarValue = new ScalarValue(dateTime);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var parsedDate = jsonDoc.RootElement.GetDateTime();
        parsedDate.Should().Be(dateTime);
    }

    [Fact]
    public void WriteScalarValue_WithDateTimeOffset_ShouldWriteStringValue()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2024, 1, 15, 10, 30, 45, TimeSpan.FromHours(5));
        var scalarValue = new ScalarValue(dateTimeOffset);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var parsedDate = jsonDoc.RootElement.GetDateTimeOffset();
        parsedDate.Should().Be(dateTimeOffset);
    }

    [Fact]
    public void WriteScalarValue_WithTimeSpan_ShouldWriteStringValue()
    {
        // Arrange
        var timeSpan = TimeSpan.FromHours(2.5);
        var scalarValue = new ScalarValue(timeSpan);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetString().Should().Be(timeSpan.ToString());
    }

    [Fact]
    public void WriteScalarValue_WithGuid_ShouldWriteStringValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var scalarValue = new ScalarValue(guid);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetGuid().Should().Be(guid);
    }

    [Fact]
    public void WriteScalarValue_WithEnum_ShouldWriteStringValue()
    {
        // Arrange
        var scalarValue = new ScalarValue(LogEventLevel.Warning);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetString().Should().Be("Warning");
    }

    [Fact]
    public void WriteScalarValue_WithChar_ShouldWriteStringValue()
    {
        // Arrange
        var scalarValue = new ScalarValue('A');

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetString().Should().Be("A");
    }

    [Fact]
    public void WriteScalarValue_WithNativeInt_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue((nint)42);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetInt32().Should().Be(42);
    }

    [Fact]
    public void WriteScalarValue_WithNativeUInt_ShouldWriteNumberValue()
    {
        // Arrange
        var scalarValue = new ScalarValue((nuint)42);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetUInt32().Should().Be(42u);
    }

    [Fact]
    public void WriteScalarValue_WithBigInteger_ShouldWriteStringValue()
    {
        // Arrange
        var bigInt = System.Numerics.BigInteger.Parse("12345678901234567890");
        var scalarValue = new ScalarValue(bigInt);

        // Act
        var result = JsonWriter.WritePropertyValue(scalarValue);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        jsonDoc.RootElement.GetString().Should().Be("12345678901234567890");
    }

    #endregion

    #region Complex Nested Tests

    [Fact]
    public void WriteProperties_WithNestedStructure_ShouldSerializeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["User"] = new StructureValue([
                new LogEventProperty("Id", new ScalarValue(123)),
                new LogEventProperty("Name", new ScalarValue("John")),
                new LogEventProperty("Roles", new SequenceValue([
                    new ScalarValue("Admin"),
                    new ScalarValue("User")
                ]))
            ])
        };

        // Act
        var result = JsonWriter.WriteProperties(properties);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        var user = root.GetProperty("User");
        user.GetProperty("Id").GetInt32().Should().Be(123);
        user.GetProperty("Name").GetString().Should().Be("John");

        var roles = user.GetProperty("Roles");
        roles.GetArrayLength().Should().Be(2);
        roles[0].GetString().Should().Be("Admin");
        roles[1].GetString().Should().Be("User");
    }

    [Fact]
    public void WriteProperties_WithDeeplyNestedObjects_ShouldSerializeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Request"] = new StructureValue([
                new LogEventProperty("Headers", new DictionaryValue([
                    new(new ScalarValue("Content-Type"), new ScalarValue("application/json")),
                    new(new ScalarValue("Authorization"), new ScalarValue("Bearer token"))
                ])),
                new LogEventProperty("Body", new StructureValue([
                    new LogEventProperty("Data", new SequenceValue([
                        new ScalarValue(1),
                        new ScalarValue(2)
                    ]))
                ]))
            ])
        };

        // Act
        var result = JsonWriter.WriteProperties(properties);

        // Assert
        result.Should().NotBeNull();

        var jsonDoc = JsonDocument.Parse(result!);
        var root = jsonDoc.RootElement;

        var request = root.GetProperty("Request");
        var headers = request.GetProperty("Headers");
        headers.GetProperty("Content-Type").GetString().Should().Be("application/json");

        var body = request.GetProperty("Body");
        var data = body.GetProperty("Data");
        data.GetArrayLength().Should().Be(2);
    }

    #endregion

    #region Helper Methods

    private static void ThrowTestException()
    {
        throw new InvalidOperationException("Test exception for method tracking");
    }

    #endregion
}
