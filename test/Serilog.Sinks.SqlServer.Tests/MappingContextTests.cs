using AwesomeAssertions;

using Serilog.Events;

namespace Serilog.Sinks.SqlServer.Tests;

public class MappingContextTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    private static List<ColumnMapping<TestEntity>> CreateTestMappings()
    {
        return
        [
            new ColumnMapping<TestEntity>(
                ColumnName: "Id",
                ColumnType: typeof(int),
                GetValue: e => e.Id,
                Nullable: false
            ),
            new ColumnMapping<TestEntity>(
                ColumnName: "Name",
                ColumnType: typeof(string),
                GetValue: e => e.Name,
                Nullable: true
            ),
            new ColumnMapping<TestEntity>(
                ColumnName: "CreatedDate",
                ColumnType: typeof(DateTime),
                GetValue: e => e.CreatedDate,
                Nullable: false
            )
        ];
    }

    [Fact]
    public void Constructor_ShouldInitializeMappings()
    {
        // Arrange
        var mappings = CreateTestMappings();

        // Act
        var context = new MappingContext<TestEntity>(mappings);

        // Assert
        context.Mappings.Should().BeSameAs(mappings);
        context.Mappings.Should().HaveCount(3);
    }

    [Fact]
    public void Constructor_ShouldInitializeOrdinals()
    {
        // Arrange
        var mappings = CreateTestMappings();

        // Act
        var context = new MappingContext<TestEntity>(mappings);

        // Assert
        context.Ordinals.Should().NotBeNull();
        context.Ordinals.Should().HaveCount(3);
        context.Ordinals["Id"].Should().Be(0);
        context.Ordinals["Name"].Should().Be(1);
        context.Ordinals["CreatedDate"].Should().Be(2);
    }

    [Fact]
    public void Constructor_WithEmptyMappings_ShouldCreateEmptyContext()
    {
        // Arrange
        var mappings = new List<ColumnMapping<TestEntity>>();

        // Act
        var context = new MappingContext<TestEntity>(mappings);

        // Assert
        context.Mappings.Should().HaveCount(0);
        context.Ordinals.Should().HaveCount(0);
    }

    [Fact]
    public void GetMapping_ByName_ShouldReturnCorrectMapping()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var mapping = context.GetMapping("Name");

        // Assert
        mapping.Should().NotBeNull();
        mapping.ColumnName.Should().Be("Name");
        mapping.ColumnType.Should().Be(typeof(string));
        mapping.Nullable.Should().BeTrue();
    }

    [Fact]
    public void GetMapping_ByName_ShouldBeCaseInsensitive()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var lowerCase = context.GetMapping("name");
        var upperCase = context.GetMapping("NAME");
        var mixedCase = context.GetMapping("NaMe");

        // Assert
        lowerCase.Should().NotBeNull();
        upperCase.Should().NotBeNull();
        mixedCase.Should().NotBeNull();
        lowerCase.ColumnName.Should().Be("Name");
        upperCase.ColumnName.Should().Be("Name");
        mixedCase.ColumnName.Should().Be("Name");
    }

    [Fact]
    public void GetMapping_ByName_WithNonExistentName_ShouldReturnNull()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var mapping = context.GetMapping("NonExistent");

        // Assert
        mapping.Should().BeNull();
    }

    [Fact]
    public void GetMapping_ByOrdinal_ShouldReturnCorrectMapping()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var mapping0 = context.GetMapping(0);
        var mapping1 = context.GetMapping(1);
        var mapping2 = context.GetMapping(2);

        // Assert
        mapping0.Should().NotBeNull();
        mapping0.ColumnName.Should().Be("Id");

        mapping1.Should().NotBeNull();
        mapping1.ColumnName.Should().Be("Name");

        mapping2.Should().NotBeNull();
        mapping2.ColumnName.Should().Be("CreatedDate");
    }

    [Fact]
    public void GetMapping_ByOrdinal_WithNegativeOrdinal_ShouldReturnNull()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var mapping = context.GetMapping(-1);

        // Assert
        mapping.Should().BeNull();
    }

    [Fact]
    public void GetMapping_ByOrdinal_WithOrdinalEqualToCount_ShouldReturnNull()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var mapping = context.GetMapping(3);

        // Assert
        mapping.Should().BeNull();
    }

    [Fact]
    public void GetMapping_ByOrdinal_WithOrdinalGreaterThanCount_ShouldReturnNull()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var mapping = context.GetMapping(100);

        // Assert
        mapping.Should().BeNull();
    }

    [Fact]
    public void GetSchemaTable_ShouldCreateCorrectDataTable()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var schemaTable = context.GetSchemaTable();

        // Assert
        schemaTable.Should().NotBeNull();
        schemaTable.Columns.Count.Should().Be(5);
        schemaTable.Columns[0].ColumnName.Should().Be("ColumnOrdinal");
        schemaTable.Columns[1].ColumnName.Should().Be("ColumnName");
        schemaTable.Columns[2].ColumnName.Should().Be("DataType");
        schemaTable.Columns[3].ColumnName.Should().Be("ColumnSize");
        schemaTable.Columns[4].ColumnName.Should().Be("AllowDBNull");
    }

    [Fact]
    public void GetSchemaTable_ShouldCreateCorrectRows()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var schemaTable = context.GetSchemaTable();

        // Assert
        schemaTable.Rows.Count.Should().Be(3);

        // First row
        schemaTable.Rows[0]["ColumnOrdinal"].Should().Be(0);
        schemaTable.Rows[0]["ColumnName"].Should().Be("Id");
        schemaTable.Rows[0]["DataType"].Should().Be(typeof(int));
        schemaTable.Rows[0]["ColumnSize"].Should().Be(-1);
        schemaTable.Rows[0]["AllowDBNull"].Should().Be(false);

        // Second row
        schemaTable.Rows[1]["ColumnOrdinal"].Should().Be(1);
        schemaTable.Rows[1]["ColumnName"].Should().Be("Name");
        schemaTable.Rows[1]["DataType"].Should().Be(typeof(string));
        schemaTable.Rows[1]["ColumnSize"].Should().Be(-1);
        schemaTable.Rows[1]["AllowDBNull"].Should().Be(true);

        // Third row
        schemaTable.Rows[2]["ColumnOrdinal"].Should().Be(2);
        schemaTable.Rows[2]["ColumnName"].Should().Be("CreatedDate");
        schemaTable.Rows[2]["DataType"].Should().Be(typeof(DateTime));
        schemaTable.Rows[2]["ColumnSize"].Should().Be(-1);
        schemaTable.Rows[2]["AllowDBNull"].Should().Be(false);
    }

    [Fact]
    public void GetSchemaTable_ShouldCacheResult()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var schemaTable1 = context.GetSchemaTable();
        var schemaTable2 = context.GetSchemaTable();

        // Assert
        schemaTable1.Should().BeSameAs(schemaTable2);
    }

    [Fact]
    public void GetSchemaTable_WithEmptyMappings_ShouldCreateEmptyDataTable()
    {
        // Arrange
        var mappings = new List<ColumnMapping<TestEntity>>();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        var schemaTable = context.GetSchemaTable();

        // Assert
        schemaTable.Should().NotBeNull();
        schemaTable.Columns.Count.Should().Be(5);
        schemaTable.Rows.Count.Should().Be(0);
    }

    [Fact]
    public void MappingContext_WithLogEventMappings_ShouldWork()
    {
        // Arrange
        var mappings = new List<ColumnMapping<LogEvent>>
        {
            MappingDefaults.TimestampMapping,
            MappingDefaults.LevelMapping,
            MappingDefaults.MessageMapping
        };

        // Act
        var context = new MappingContext<LogEvent>(mappings);

        // Assert
        context.Mappings.Should().HaveCount(3);
        context.GetMapping("Timestamp").Should().NotBeNull();
        context.GetMapping("Level").Should().NotBeNull();
        context.GetMapping("Message").Should().NotBeNull();
    }

    [Fact]
    public void GetMapping_WithGetValue_ShouldExtractCorrectValue()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);
        var testEntity = new TestEntity
        {
            Id = 42,
            Name = "Test",
            CreatedDate = new DateTime(2024, 1, 1)
        };

        // Act
        var idMapping = context.GetMapping("Id");
        var nameMapping = context.GetMapping("Name");
        var dateMapping = context.GetMapping("CreatedDate");

        // Assert
        idMapping.Should().NotBeNull();
        idMapping.GetValue(testEntity).Should().Be(42);

        nameMapping.Should().NotBeNull();
        nameMapping.GetValue(testEntity).Should().Be("Test");

        dateMapping.Should().NotBeNull();
        dateMapping.GetValue(testEntity).Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public void Ordinals_ShouldPreserveMappingOrder()
    {
        // Arrange
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Column3", typeof(string), e => e.Name),
            new("Column1", typeof(int), e => e.Id),
            new("Column2", typeof(DateTime), e => e.CreatedDate)
        };

        // Act
        var context = new MappingContext<TestEntity>(mappings);

        // Assert
        context.Ordinals["Column3"].Should().Be(0);
        context.Ordinals["Column1"].Should().Be(1);
        context.Ordinals["Column2"].Should().Be(2);

        context.GetMapping(0)?.ColumnName.Should().Be("Column3");
        context.GetMapping(1)?.ColumnName.Should().Be("Column1");
        context.GetMapping(2)?.ColumnName.Should().Be("Column2");
    }
}
