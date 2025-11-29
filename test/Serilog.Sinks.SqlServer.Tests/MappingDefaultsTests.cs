using System;
using System.Collections.Generic;
using System.Diagnostics;
using AwesomeAssertions;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.SqlServer.Tests;

public class MappingDefaultsTests
{
    #region Constant Tests

    [Fact]
    public void Constants_ShouldHaveCorrectValues()
    {
        // Assert
        MappingDefaults.TimestampName.Should().Be("Timestamp");
        MappingDefaults.LevelName.Should().Be("Level");
        MappingDefaults.MessageName.Should().Be("Message");
        MappingDefaults.TraceIdName.Should().Be("TraceId");
        MappingDefaults.SpanIdName.Should().Be("SpanId");
        MappingDefaults.ExceptionName.Should().Be("Exception");
        MappingDefaults.PropertiesName.Should().Be("Properties");
        MappingDefaults.SourceContextName.Should().Be("SourceContext");
    }

    #endregion

    #region TimestampMapping Tests

    [Fact]
    public void TimestampMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.TimestampMapping.ColumnName.Should().Be("Timestamp");
    }

    [Fact]
    public void TimestampMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.TimestampMapping.ColumnType.Should().Be(typeof(DateTimeOffset));
    }

    [Fact]
    public void TimestampMapping_ShouldNotBeNullable()
    {
        // Assert
        MappingDefaults.TimestampMapping.Nullable.Should().BeFalse();
    }

    [Fact]
    public void TimestampMapping_GetValue_ShouldReturnUtcDateTime()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 45, TimeSpan.FromHours(5));
        var logEvent = CreateLogEvent(timestamp);

        // Act
        var result = MappingDefaults.TimestampMapping.GetValue(logEvent);

        // Assert
        result.Should().BeOfType<DateTimeOffset>();
        ((DateTimeOffset)result!).Should().Be(timestamp);
    }

    #endregion

    #region LevelMapping Tests

    [Fact]
    public void LevelMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.LevelMapping.ColumnName.Should().Be("Level");
    }

    [Fact]
    public void LevelMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.LevelMapping.ColumnType.Should().Be(typeof(string));
    }

    [Fact]
    public void LevelMapping_ShouldNotBeNullable()
    {
        // Assert
        MappingDefaults.LevelMapping.Nullable.Should().BeFalse();
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, "Verbose")]
    [InlineData(LogEventLevel.Debug, "Debug")]
    [InlineData(LogEventLevel.Information, "Information")]
    [InlineData(LogEventLevel.Warning, "Warning")]
    [InlineData(LogEventLevel.Error, "Error")]
    [InlineData(LogEventLevel.Fatal, "Fatal")]
    public void LevelMapping_GetValue_ShouldReturnLevelAsString(LogEventLevel level, string expected)
    {
        // Arrange
        var logEvent = CreateLogEvent(level: level);

        // Act
        var result = MappingDefaults.LevelMapping.GetValue(logEvent);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region MessageMapping Tests

    [Fact]
    public void MessageMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.MessageMapping.ColumnName.Should().Be("Message");
    }

    [Fact]
    public void MessageMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.MessageMapping.ColumnType.Should().Be(typeof(string));
    }

    [Fact]
    public void MessageMapping_ShouldBeNullable()
    {
        // Assert
        MappingDefaults.MessageMapping.Nullable.Should().BeTrue();
    }

    [Fact]
    public void MessageMapping_GetValue_ShouldReturnRenderedMessage()
    {
        // Arrange
        var template = new MessageTemplateParser().Parse("User {UserId} logged in");
        var properties = new List<LogEventProperty>
        {
            new("UserId", new ScalarValue(42))
        };

        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            template,
            properties);

        // Act
        var result = MappingDefaults.MessageMapping.GetValue(logEvent);

        // Assert
        result.Should().BeOfType<string>();

        var resultString = (string)result;
        resultString.Should().Contain("User");
        resultString.Should().Contain("42");
        resultString.Should().Contain("logged in");
    }

    #endregion

    #region TraceIdMapping Tests

    [Fact]
    public void TraceIdMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.TraceIdMapping.ColumnName.Should().Be("TraceId");
    }

    [Fact]
    public void TraceIdMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.TraceIdMapping.ColumnType.Should().Be(typeof(string));
    }

    [Fact]
    public void TraceIdMapping_ShouldBeNullable()
    {
        // Assert
        MappingDefaults.TraceIdMapping.Nullable.Should().BeTrue();
    }

    [Fact]
    public void TraceIdMapping_GetValue_WithTraceId_ShouldReturnHexString()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var logEvent = CreateLogEvent(traceId: traceId);

        // Act
        var result = MappingDefaults.TraceIdMapping.GetValue(logEvent);

        // Assert
        result.Should().BeOfType<string>();
        ((string)result!).Should().Be(traceId.ToHexString());
    }

    [Fact]
    public void TraceIdMapping_GetValue_WithoutTraceId_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = MappingDefaults.TraceIdMapping.GetValue(logEvent);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SpanIdMapping Tests

    [Fact]
    public void SpanIdMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.SpanIdMapping.ColumnName.Should().Be("SpanId");
    }

    [Fact]
    public void SpanIdMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.SpanIdMapping.ColumnType.Should().Be(typeof(string));
    }

    [Fact]
    public void SpanIdMapping_ShouldBeNullable()
    {
        // Assert
        MappingDefaults.SpanIdMapping.Nullable.Should().BeTrue();
    }

    [Fact]
    public void SpanIdMapping_GetValue_WithSpanId_ShouldReturnHexString()
    {
        // Arrange
        var spanId = ActivitySpanId.CreateRandom();
        var logEvent = CreateLogEvent(spanId: spanId);

        // Act
        var result = MappingDefaults.SpanIdMapping.GetValue(logEvent);

        // Assert
        result.Should().BeOfType<string>();
        ((string)result!).Should().Be(spanId.ToHexString());
    }

    [Fact]
    public void SpanIdMapping_GetValue_WithoutSpanId_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = MappingDefaults.SpanIdMapping.GetValue(logEvent);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ExceptionMapping Tests

    [Fact]
    public void ExceptionMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.ExceptionMapping.ColumnName.Should().Be("Exception");
    }

    [Fact]
    public void ExceptionMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.ExceptionMapping.ColumnType.Should().Be(typeof(string));
    }

    [Fact]
    public void ExceptionMapping_ShouldBeNullable()
    {
        // Assert
        MappingDefaults.ExceptionMapping.Nullable.Should().BeTrue();
    }

    [Fact]
    public void ExceptionMapping_GetValue_WithException_ShouldReturnJsonString()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var logEvent = CreateLogEvent(exception: exception);

        // Act
        var result = MappingDefaults.ExceptionMapping.GetValue(logEvent);

        // Assert
        result.Should().BeOfType<string>();

        var jsonString = (string)result;
        jsonString.Should().Contain("InvalidOperationException");
        jsonString.Should().Contain("Test exception");
    }

    [Fact]
    public void ExceptionMapping_GetValue_WithoutException_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = MappingDefaults.ExceptionMapping.GetValue(logEvent);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PropertiesMapping Tests

    [Fact]
    public void PropertiesMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.PropertiesMapping.ColumnName.Should().Be("Properties");
    }

    [Fact]
    public void PropertiesMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.PropertiesMapping.ColumnType.Should().Be(typeof(string));
    }

    [Fact]
    public void PropertiesMapping_ShouldBeNullable()
    {
        // Assert
        MappingDefaults.PropertiesMapping.Nullable.Should().BeTrue();
    }

    [Fact]
    public void PropertiesMapping_GetValue_WithProperties_ShouldReturnJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["UserId"] = new ScalarValue(123),
            ["UserName"] = new ScalarValue("John Doe")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = MappingDefaults.PropertiesMapping.GetValue(logEvent);

        // Assert
        result.Should().BeOfType<string>();

        var jsonString = (string)result;
        jsonString.Should().Contain("UserId");
        jsonString.Should().Contain("123");
        jsonString.Should().Contain("UserName");
        jsonString.Should().Contain("John Doe");
    }

    [Fact]
    public void PropertiesMapping_GetValue_WithEmptyProperties_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = MappingDefaults.PropertiesMapping.GetValue(logEvent);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SourceContextMapping Tests

    [Fact]
    public void SourceContextMapping_ShouldHaveCorrectColumnName()
    {
        // Assert
        MappingDefaults.SourceContextMapping.ColumnName.Should().Be("SourceContext");
    }

    [Fact]
    public void SourceContextMapping_ShouldHaveCorrectColumnType()
    {
        // Assert
        MappingDefaults.SourceContextMapping.ColumnType.Should().Be(typeof(string));
    }

    [Fact]
    public void SourceContextMapping_ShouldBeNullable()
    {
        // Assert
        MappingDefaults.SourceContextMapping.Nullable.Should().BeTrue();
    }

    [Fact]
    public void SourceContextMapping_GetValue_WithSourceContext_ShouldReturnValue()
    {
        // Arrange
        var properties = new Dictionary<string, LogEventPropertyValue>
        {
            ["SourceContext"] = new ScalarValue("MyApp.Services.UserService")
        };
        var logEvent = CreateLogEvent(properties: properties);

        // Act
        var result = MappingDefaults.SourceContextMapping.GetValue(logEvent);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("MyApp.Services.UserService");
    }

    [Fact]
    public void SourceContextMapping_GetValue_WithoutSourceContext_ShouldReturnNull()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var result = MappingDefaults.SourceContextMapping.GetValue(logEvent);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static LogEvent CreateLogEvent(
        DateTimeOffset? timestamp = null,
        LogEventLevel level = LogEventLevel.Information,
        string messageTemplate = "Test message",
        Exception? exception = null,
        ActivityTraceId? traceId = null,
        ActivitySpanId? spanId = null,
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
            logProperties,
            traceId ?? default,
            spanId ?? default
        );
    }

    #endregion
}
