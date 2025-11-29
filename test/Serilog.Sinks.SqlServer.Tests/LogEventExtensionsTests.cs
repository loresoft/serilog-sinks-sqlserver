using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.SqlServer.Tests;

public class LogEventExtensionsTests
{
    #region GetPropertyValue - Null and Empty Tests

    [Fact]
    public void GetPropertyValue_WithNullLogEvent_ShouldReturnNull()
    {
        // Arrange
        LogEvent logEvent = null!;

        // Act
        var result = logEvent.GetPropertyValue("PropertyName");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithNullPropertyName_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = logEvent.GetPropertyValue(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithEmptyPropertyName_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = logEvent.GetPropertyValue("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithNonExistentProperty_ShouldReturnNull()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["ExistingProperty"] = new ScalarValue("value")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("NonExistentProperty");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPropertyValue - Scalar Value Tests

    [Fact]
    public void GetPropertyValue_WithStringScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Message"] = new ScalarValue("Hello World")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("Message");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
        result.Should().Be("Hello World");
    }

    [Fact]
    public void GetPropertyValue_WithIntScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["UserId"] = new ScalarValue(42)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("UserId");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<int>();
        result.Should().Be(42);
    }

    [Fact]
    public void GetPropertyValue_WithBooleanScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["IsActive"] = new ScalarValue(true)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("IsActive");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<bool>();
        result.Should().Be(true);
    }

    [Fact]
    public void GetPropertyValue_WithDoubleScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Temperature"] = new ScalarValue(98.6)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("Temperature");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<double>();
        result.Should().Be(98.6);
    }

    [Fact]
    public void GetPropertyValue_WithDecimalScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Price"] = new ScalarValue(19.99m)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("Price");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<decimal>();
        result.Should().Be(19.99m);
    }

    [Fact]
    public void GetPropertyValue_WithDateTimeScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["CreatedAt"] = new ScalarValue(dateTime)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("CreatedAt");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<DateTime>();
        result.Should().Be(dateTime);
    }

    [Fact]
    public void GetPropertyValue_WithGuidScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["RequestId"] = new ScalarValue(guid)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("RequestId");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Guid>();
        result.Should().Be(guid);
    }

    [Fact]
    public void GetPropertyValue_WithNullScalarValue_ShouldReturnNull()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["NullProperty"] = new ScalarValue(null)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("NullProperty");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithEnumScalarValue_ShouldReturnUnderlyingValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["LogLevel"] = new ScalarValue(LogEventLevel.Warning)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("LogLevel");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<LogEventLevel>();
        result.Should().Be(LogEventLevel.Warning);
    }

    #endregion

    #region GetPropertyValue - Complex Value Tests

    [Fact]
    public void GetPropertyValue_WithSequenceValue_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Roles"] = new SequenceValue([
                new ScalarValue("Admin"),
                new ScalarValue("User"),
                new ScalarValue("Guest")
            ])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("Roles");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();

        var jsonDoc = JsonDocument.Parse((string)result!);
        var array = jsonDoc.RootElement;

        array.ValueKind.Should().Be(JsonValueKind.Array);
        array.GetArrayLength().Should().Be(3);
        array[0].GetString().Should().Be("Admin");
        array[1].GetString().Should().Be("User");
        array[2].GetString().Should().Be("Guest");
    }

    [Fact]
    public void GetPropertyValue_WithStructureValue_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["User"] = new StructureValue([
                new LogEventProperty("Id", new ScalarValue(123)),
                new LogEventProperty("Name", new ScalarValue("John Doe")),
                new LogEventProperty("IsActive", new ScalarValue(true))
            ])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("User");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();

        var jsonDoc = JsonDocument.Parse((string)result!);
        var obj = jsonDoc.RootElement;

        obj.ValueKind.Should().Be(JsonValueKind.Object);
        obj.GetProperty("Id").GetInt32().Should().Be(123);
        obj.GetProperty("Name").GetString().Should().Be("John Doe");
        obj.GetProperty("IsActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void GetPropertyValue_WithDictionaryValue_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Headers"] = new DictionaryValue([
                new(new ScalarValue("Content-Type"), new ScalarValue("application/json")),
                new(new ScalarValue("Authorization"), new ScalarValue("Bearer token")),
                new(new ScalarValue("X-Request-Id"), new ScalarValue("abc123"))
            ])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("Headers");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();

        var jsonDoc = JsonDocument.Parse((string)result!);
        var obj = jsonDoc.RootElement;

        obj.ValueKind.Should().Be(JsonValueKind.Object);
        obj.GetProperty("Content-Type").GetString().Should().Be("application/json");
        obj.GetProperty("Authorization").GetString().Should().Be("Bearer token");
        obj.GetProperty("X-Request-Id").GetString().Should().Be("abc123");
    }

    [Fact]
    public void GetPropertyValue_WithNestedStructure_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Request"] = new StructureValue([
                new LogEventProperty("Method", new ScalarValue("POST")),
                new LogEventProperty("Path", new ScalarValue("/api/users")),
                new LogEventProperty("Headers", new DictionaryValue([
                    new(new ScalarValue("Content-Type"), new ScalarValue("application/json"))
                ])),
                new LogEventProperty("Body", new StructureValue([
                    new LogEventProperty("Name", new ScalarValue("John")),
                    new LogEventProperty("Tags", new SequenceValue([
                        new ScalarValue("tag1"),
                        new ScalarValue("tag2")
                    ]))
                ]))
            ])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("Request");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();

        var jsonDoc = JsonDocument.Parse((string)result!);
        var obj = jsonDoc.RootElement;

        obj.ValueKind.Should().Be(JsonValueKind.Object);
        obj.GetProperty("Method").GetString().Should().Be("POST");
        obj.GetProperty("Path").GetString().Should().Be("/api/users");

        var headers = obj.GetProperty("Headers");
        headers.GetProperty("Content-Type").GetString().Should().Be("application/json");

        var body = obj.GetProperty("Body");
        body.GetProperty("Name").GetString().Should().Be("John");

        var tags = body.GetProperty("Tags");
        tags.GetArrayLength().Should().Be(2);
        tags[0].GetString().Should().Be("tag1");
        tags[1].GetString().Should().Be("tag2");
    }

    #endregion

    #region Property Name Case Sensitivity Tests

    [Fact]
    public void GetPropertyValue_IsCaseSensitive()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["UserId"] = new ScalarValue(42)
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var resultCorrectCase = logEvent.GetPropertyValue("UserId");
        var resultWrongCase = logEvent.GetPropertyValue("userid");

        // Assert
        resultCorrectCase.Should().NotBeNull();
        resultCorrectCase.Should().Be(42);
        resultWrongCase.Should().BeNull();
    }

    #endregion

    #region Multiple Properties Tests

    [Fact]
    public void GetPropertyValue_WithMultipleProperties_ShouldReturnCorrectValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Property1"] = new ScalarValue("Value1"),
            ["Property2"] = new ScalarValue(42),
            ["Property3"] = new ScalarValue(true),
            ["Property4"] = new SequenceValue([new ScalarValue(1), new ScalarValue(2)])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act & Assert
        logEvent.GetPropertyValue("Property1").Should().Be("Value1");
        logEvent.GetPropertyValue("Property2").Should().Be(42);
        logEvent.GetPropertyValue("Property3").Should().Be(true);
        logEvent.GetPropertyValue("Property4").Should().BeOfType<string>();
    }

    #endregion

    #region Special Characters in Property Names Tests

    [Fact]
    public void GetPropertyValue_WithSpecialCharactersInName_ShouldWork()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["Property-With-Dashes"] = new ScalarValue("value1"),
            ["Property_With_Underscores"] = new ScalarValue("value2"),
            ["Property.With.Dots"] = new ScalarValue("value3")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act & Assert
        logEvent.GetPropertyValue("Property-With-Dashes").Should().Be("value1");
        logEvent.GetPropertyValue("Property_With_Underscores").Should().Be("value2");
        logEvent.GetPropertyValue("Property.With.Dots").Should().Be("value3");
    }

    #endregion

    #region Integration with Common Serilog Properties

    [Fact]
    public void GetPropertyValue_WithSourceContext_ShouldReturnValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["SourceContext"] = new ScalarValue("MyApp.Services.UserService")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("SourceContext");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("MyApp.Services.UserService");
    }

    [Fact]
    public void GetPropertyValue_WithMachineName_ShouldReturnValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["MachineName"] = new ScalarValue("SERVER-01")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("MachineName");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("SERVER-01");
    }

    [Fact]
    public void GetPropertyValue_WithApplicationName_ShouldReturnValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["ApplicationName"] = new ScalarValue("MyWebApp")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("ApplicationName");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("MyWebApp");
    }

    #endregion

    #region Edge Cases and Whitespace Tests

    [Fact]
    public void GetPropertyValue_WithWhitespacePropertyName_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = logEvent.GetPropertyValue("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPropertyValue_WithEmptySequence_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["EmptyArray"] = new SequenceValue([])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("EmptyArray");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();

        var jsonDoc = JsonDocument.Parse((string)result!);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        jsonDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void GetPropertyValue_WithEmptyStructure_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["EmptyObject"] = new StructureValue([])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("EmptyObject");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();

        var jsonDoc = JsonDocument.Parse((string)result!);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void GetPropertyValue_WithEmptyDictionary_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["EmptyDict"] = new DictionaryValue([])
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = logEvent.GetPropertyValue("EmptyDict");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();

        var jsonDoc = JsonDocument.Parse((string)result!);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    #endregion

    #region Helper Methods

    private static LogEvent CreateLogEvent(
        DateTimeOffset? timestamp = null,
        LogEventLevel level = LogEventLevel.Information,
        string messageTemplate = "Test message",
        Exception? exception = null,
        Dictionary<string, LogEventPropertyValue>? properties = null)
    {
        var template = new MessageTemplateParser().Parse(messageTemplate);
        var logProperties = new List<LogEventProperty>();

        if (properties != null)
        {
            foreach (var prop in properties)
                logProperties.Add(new LogEventProperty(prop.Key, prop.Value));
        }

        return new LogEvent(
            timestamp ?? DateTimeOffset.UtcNow,
            level,
            exception,
            template,
            logProperties
        );
    }

    #endregion
}
