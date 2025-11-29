using System.Data;

using AwesomeAssertions;

namespace Serilog.Sinks.SqlServer.Tests;

public class ListDataReaderTests
{
    #region Test Entity

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid UniqueId { get; set; }
        public double Score { get; set; }
        public decimal Price { get; set; }
        public short Count { get; set; }
        public long BigNumber { get; set; }
        public float Rating { get; set; }
        public byte Status { get; set; }
        public char Code { get; set; }
        public string? NullableString { get; set; }
    }

    #endregion

    #region Helper Methods

    private static List<ColumnMapping<TestEntity>> CreateTestMappings()
    {
        return
        [
            new ColumnMapping<TestEntity>("Id", typeof(int), e => e.Id, false),
            new ColumnMapping<TestEntity>("Name", typeof(string), e => e.Name, false),
            new ColumnMapping<TestEntity>("IsActive", typeof(bool), e => e.IsActive, false),
            new ColumnMapping<TestEntity>("CreatedDate", typeof(DateTime), e => e.CreatedDate, false),
            new ColumnMapping<TestEntity>("NullableString", typeof(string), e => e.NullableString, true)
        ];
    }

    private static List<TestEntity> CreateTestData()
    {
        return
        [
            new TestEntity
            {
                Id = 1,
                Name = "First",
                IsActive = true,
                CreatedDate = new DateTime(2024, 1, 1),
                NullableString = "Value1"
            },
            new TestEntity
            {
                Id = 2,
                Name = "Second",
                IsActive = false,
                CreatedDate = new DateTime(2024, 1, 2),
                NullableString = null
            },
            new TestEntity
            {
                Id = 3,
                Name = "Third",
                IsActive = true,
                CreatedDate = new DateTime(2024, 1, 3),
                NullableString = "Value3"
            }
        ];
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.Should().NotBeNull();
        reader.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullLogEvents_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ListDataReader<TestEntity>(null!, context));
    }

    [Fact]
    public void Constructor_WithNullMappingContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var data = CreateTestData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ListDataReader<TestEntity>(data, null!));
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Depth_ShouldAlwaysReturnZero()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.Depth.Should().Be(0);
    }

    [Fact]
    public void IsClosed_ShouldBeFalseInitially()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void RecordsAffected_ShouldAlwaysReturnZero()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.RecordsAffected.Should().Be(0);
    }

    [Fact]
    public void FieldCount_ShouldReturnMappingCount()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.FieldCount.Should().Be(5);
    }

    #endregion

    #region Read Tests

    [Fact]
    public void Read_WithData_ShouldReturnTrueForEachRow()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.Read().Should().BeTrue();
        reader.Read().Should().BeTrue();
        reader.Read().Should().BeTrue();
        reader.Read().Should().BeFalse();
    }

    [Fact]
    public void Read_WithEmptyData_ShouldReturnFalse()
    {
        // Arrange
        var data = new List<TestEntity>();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.Read().Should().BeFalse();
    }

    #endregion

    #region GetValue Tests

    [Fact]
    public void GetValue_WithValidOrdinal_ShouldReturnValue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader.GetValue(0).Should().Be(1);
        reader.GetValue(1).Should().Be("First");
        reader.GetValue(2).Should().Be(true);
    }

    [Fact]
    public void GetValue_WithNullValue_ShouldReturnDBNull()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        reader.Read(); // Move to second row with null value

        // Assert
        reader.GetValue(4).Should().Be(DBNull.Value);
    }

    [Fact]
    public void GetValue_WithInvalidOrdinal_ShouldThrowIndexOutOfRangeException()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        Assert.Throws<IndexOutOfRangeException>(() => reader.GetValue(100));
    }

    #endregion

    #region GetValues Tests

    [Fact]
    public void GetValues_ShouldFillArrayWithValues()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var values = new object[5];
        var count = reader.GetValues(values);

        // Assert
        count.Should().Be(5);
        values[0].Should().Be(1);
        values[1].Should().Be("First");
        values[2].Should().Be(true);
        values[3].Should().BeOfType<DateTime>();
        values[4].Should().Be("Value1");
    }

    [Fact]
    public void GetValues_WithSmallerArray_ShouldFillPartially()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var values = new object[3];
        var count = reader.GetValues(values);

        // Assert
        count.Should().Be(3);
        values[0].Should().Be(1);
        values[1].Should().Be("First");
        values[2].Should().Be(true);
    }

    [Fact]
    public void GetValues_WithLargerArray_ShouldFillAvailableValues()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var values = new object[10];
        var count = reader.GetValues(values);

        // Assert
        count.Should().Be(5);
        values[0].Should().Be(1);
        values[1].Should().Be("First");
        values[4].Should().Be("Value1");
    }

    #endregion

    #region GetName Tests

    [Fact]
    public void GetName_WithValidOrdinal_ShouldReturnColumnName()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.GetName(0).Should().Be("Id");
        reader.GetName(1).Should().Be("Name");
        reader.GetName(2).Should().Be("IsActive");
    }

    [Fact]
    public void GetName_WithInvalidOrdinal_ShouldThrowIndexOutOfRangeException()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(100));
    }

    #endregion

    #region GetOrdinal Tests

    [Fact]
    public void GetOrdinal_WithValidName_ShouldReturnOrdinal()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.GetOrdinal("Id").Should().Be(0);
        reader.GetOrdinal("Name").Should().Be(1);
        reader.GetOrdinal("IsActive").Should().Be(2);
    }

    [Fact]
    public void GetOrdinal_IsCaseInsensitive()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.GetOrdinal("name").Should().Be(1);
        reader.GetOrdinal("NAME").Should().Be(1);
        reader.GetOrdinal("NaMe").Should().Be(1);
    }

    [Fact]
    public void GetOrdinal_WithInvalidName_ShouldReturnMinusOne()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.GetOrdinal("NonExistent").Should().Be(-1);
    }

    #endregion

    #region GetDataTypeName Tests

    [Fact]
    public void GetDataTypeName_WithValidOrdinal_ShouldReturnTypeName()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.GetDataTypeName(0).Should().Be("Int32");
        reader.GetDataTypeName(1).Should().Be("String");
        reader.GetDataTypeName(2).Should().Be("Boolean");
    }

    [Fact]
    public void GetDataTypeName_WithInvalidOrdinal_ShouldThrowIndexOutOfRangeException()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        Assert.Throws<IndexOutOfRangeException>(() => reader.GetDataTypeName(100));
    }

    #endregion

    #region GetFieldType Tests

    [Fact]
    public void GetFieldType_WithValidOrdinal_ShouldReturnType()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.GetFieldType(0).Should().Be(typeof(int));
        reader.GetFieldType(1).Should().Be(typeof(string));
        reader.GetFieldType(2).Should().Be(typeof(bool));
        reader.GetFieldType(3).Should().Be(typeof(DateTime));
    }

    [Fact]
    public void GetFieldType_WithInvalidOrdinal_ShouldThrowIndexOutOfRangeException()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        Assert.Throws<IndexOutOfRangeException>(() => reader.GetFieldType(100));
    }

    #endregion

    #region Typed Get Methods Tests

    [Fact]
    public void GetBoolean_ShouldReturnBooleanValue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader.GetBoolean(2).Should().BeTrue();
    }

    [Fact]
    public void GetInt32_ShouldReturnIntValue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader.GetInt32(0).Should().Be(1);
    }

    [Fact]
    public void GetString_ShouldReturnStringValue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader.GetString(1).Should().Be("First");
    }

    [Fact]
    public void GetDateTime_ShouldReturnDateTimeValue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader.GetDateTime(3).Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public void TypedGetMethods_WithAllTypes_ShouldReturnCorrectValues()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var data = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 42,
                Name = "Test",
                IsActive = true,
                CreatedDate = new DateTime(2024, 1, 1),
                UniqueId = guid,
                Score = 98.6,
                Price = 19.99m,
                Count = 100,
                BigNumber = 1234567890L,
                Rating = 4.5f,
                Status = 255,
                Code = 'A'
            }
        };

        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Id", typeof(int), e => e.Id),
            new("Name", typeof(string), e => e.Name),
            new("IsActive", typeof(bool), e => e.IsActive),
            new("CreatedDate", typeof(DateTime), e => e.CreatedDate),
            new("UniqueId", typeof(Guid), e => e.UniqueId),
            new("Score", typeof(double), e => e.Score),
            new("Price", typeof(decimal), e => e.Price),
            new("Count", typeof(short), e => e.Count),
            new("BigNumber", typeof(long), e => e.BigNumber),
            new("Rating", typeof(float), e => e.Rating),
            new("Status", typeof(byte), e => e.Status),
            new("Code", typeof(char), e => e.Code)
        };

        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader.GetInt32(0).Should().Be(42);
        reader.GetString(1).Should().Be("Test");
        reader.GetBoolean(2).Should().BeTrue();
        reader.GetDateTime(3).Should().Be(new DateTime(2024, 1, 1));
        reader.GetGuid(4).Should().Be(guid);
        reader.GetDouble(5).Should().Be(98.6);
        reader.GetDecimal(6).Should().Be(19.99m);
        reader.GetInt16(7).Should().Be(100);
        reader.GetInt64(8).Should().Be(1234567890L);
        reader.GetFloat(9).Should().Be(4.5f);
        reader.GetByte(10).Should().Be(255);
        reader.GetChar(11).Should().Be('A');
    }

    #endregion

    #region IsDBNull Tests

    [Fact]
    public void IsDBNull_WithNullValue_ShouldReturnTrue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        reader.Read(); // Move to second row with null

        // Assert
        reader.IsDBNull(4).Should().BeTrue();
    }

    [Fact]
    public void IsDBNull_WithNonNullValue_ShouldReturnFalse()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader.IsDBNull(0).Should().BeFalse();
        reader.IsDBNull(1).Should().BeFalse();
        reader.IsDBNull(4).Should().BeFalse();
    }

    #endregion

    #region GetBytes Tests

    [Fact]
    public void GetBytes_ShouldCopyBytesCorrectly()
    {
        // Arrange
        var testBytes = new byte[] { 1, 2, 3, 4, 5 };
        var data = new List<TestEntity> { new TestEntity() };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Bytes", typeof(byte[]), e => testBytes)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var buffer = new byte[10];
        var count = reader.GetBytes(0, 0, buffer, 0, 3);

        // Assert
        count.Should().Be(3);
        buffer[0].Should().Be(1);
        buffer[1].Should().Be(2);
        buffer[2].Should().Be(3);
    }

    [Fact]
    public void GetBytes_WithOffset_ShouldCopyFromOffset()
    {
        // Arrange
        var testBytes = new byte[] { 1, 2, 3, 4, 5 };
        var data = new List<TestEntity> { new TestEntity() };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Bytes", typeof(byte[]), e => testBytes)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var buffer = new byte[10];
        var count = reader.GetBytes(0, 2, buffer, 0, 3);

        // Assert
        count.Should().Be(3);
        buffer[0].Should().Be(3);
        buffer[1].Should().Be(4);
        buffer[2].Should().Be(5);
    }

    [Fact]
    public void GetBytes_WithNullBuffer_ShouldThrowArgumentNullException()
    {
        // Arrange
        var testBytes = new byte[] { 1, 2, 3 };
        var data = new List<TestEntity> { new TestEntity() };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Bytes", typeof(byte[]), e => testBytes)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        Assert.Throws<ArgumentNullException>(() => reader.GetBytes(0, 0, null, 0, 3));
    }

    #endregion

    #region GetChars Tests

    [Fact]
    public void GetChars_ShouldCopyCharsCorrectly()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "HelloWorld" }
        };
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var buffer = new char[20];
        var count = reader.GetChars(1, 0, buffer, 0, 5);

        // Assert
        count.Should().Be(5);
        new string(buffer, 0, 5).Should().Be("Hello");
    }

    [Fact]
    public void GetChars_WithOffset_ShouldCopyFromOffset()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "HelloWorld" }
        };
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var buffer = new char[20];
        var count = reader.GetChars(1, 5, buffer, 0, 5);

        // Assert
        count.Should().Be(5);
        new string(buffer, 0, 5).Should().Be("World");
    }

    [Fact]
    public void GetChars_WithNullBuffer_ShouldThrowArgumentNullException()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        Assert.Throws<ArgumentNullException>(() => reader.GetChars(1, 0, null, 0, 5));
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void IntIndexer_ShouldReturnValue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader[0].Should().Be(1);
        reader[1].Should().Be("First");
        reader[2].Should().Be(true);
    }

    [Fact]
    public void StringIndexer_ShouldReturnValue()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        reader["Id"].Should().Be(1);
        reader["Name"].Should().Be("First");
        reader["IsActive"].Should().Be(true);
    }

    #endregion

    #region GetSchemaTable Tests

    [Fact]
    public void GetSchemaTable_ShouldReturnSchemaTable()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        var schemaTable = reader.GetSchemaTable();

        // Assert
        schemaTable.Should().NotBeNull();
        schemaTable.Rows.Count.Should().Be(5);
    }

    #endregion

    #region NextResult Tests

    [Fact]
    public void NextResult_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);

        // Assert
        reader.NextResult().Should().BeFalse();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldClosereader()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);
        var reader = new ListDataReader<TestEntity>(data, context);

        // Act
        reader.Dispose();

        // Assert
        reader.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void Close_ShouldCloseReader()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Close();

        // Assert
        reader.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);
        var reader = new ListDataReader<TestEntity>(data, context);

        // Act & Assert
        reader.Dispose();
        reader.Dispose();
        reader.Dispose();

        reader.IsClosed.Should().BeTrue();
    }

    #endregion

    #region GetData Tests

    [Fact]
    public void GetData_ShouldThrowNotImplementedException()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();

        // Assert
        Assert.Throws<NotImplementedException>(() => reader.GetData(0));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void IntegrationTest_ReadAllRows_ShouldWorkCorrectly()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);
        var rowCount = 0;

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        while (reader.Read())
        {
            rowCount++;
            reader.GetInt32(0).Should().Be(rowCount);
        }

        // Assert
        rowCount.Should().Be(3);
    }

    [Fact]
    public void IntegrationTest_LoadIntoDataTable_ShouldWork()
    {
        // Arrange
        var data = CreateTestData();
        var mappings = CreateTestMappings();
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        var dataTable = new DataTable();
        dataTable.Load(reader);

        // Assert
        dataTable.Rows.Count.Should().Be(3);
        dataTable.Columns.Count.Should().Be(5);
        dataTable.Rows[0]["Id"].Should().Be(1);
        dataTable.Rows[0]["Name"].Should().Be("First");
        dataTable.Rows[1]["Id"].Should().Be(2);
        dataTable.Rows[1]["NullableString"].Should().Be(DBNull.Value);
    }

    #endregion

    #region GetValue Auto Truncate Tests

    [Fact]
    public void GetValue_WithStringExceedingSize_ShouldTruncateToSize()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "This is a very long string that exceeds the limit" }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, true, 10)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be("This is a ");
    }

    [Fact]
    public void GetValue_WithStringWithinSize_ShouldNotTruncate()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "Short" }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, true, 10)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be("Short");
    }

    [Fact]
    public void GetValue_WithStringExactlyAtSize_ShouldNotTruncate()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "TenLetters" }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, true, 10)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be("TenLetters");
    }

    [Fact]
    public void GetValue_WithStringAndNoSizeDefined_ShouldNotTruncate()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "This is a very long string without a size limit defined" }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, true, null)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be("This is a very long string without a size limit defined");
    }

    [Fact]
    public void GetValue_WithEmptyStringAndSize_ShouldReturnEmptyString()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "" }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, true, 10)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be("");
    }

    [Fact]
    public void GetValue_WithSizeZeroAndString_ShouldReturnEmptyString()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "SomeValue" }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, true, 0)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be("");
    }

    [Fact]
    public void GetValue_WithNonStringTypeAndSize_ShouldNotTruncate()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 123456789 }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Id", typeof(int), e => e.Id, false, 5)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be(123456789);
    }

    [Fact]
    public void GetValue_WithNullStringAndSize_ShouldReturnDBNull()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { NullableString = null }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("NullableString", typeof(string), e => e.NullableString, true, 10)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void GetValue_WithMultipleStringsExceedingSize_ShouldTruncateEach()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity
            {
                Name = "FirstLongValue",
                NullableString = "SecondLongValue"
            }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, false, 5),
            new("NullableString", typeof(string), e => e.NullableString, true, 6)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value1 = reader.GetValue(0);
        var value2 = reader.GetValue(1);

        // Assert
        value1.Should().Be("First");
        value2.Should().Be("Second");
    }

    [Fact]
    public void GetValue_WithUnicodeStringExceedingSize_ShouldTruncateByCharacterCount()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Name = "Hello 世界 Unicode" }
        };
        var mappings = new List<ColumnMapping<TestEntity>>
        {
            new("Name", typeof(string), e => e.Name, true, 8)
        };
        var context = new MappingContext<TestEntity>(mappings);

        // Act
        using var reader = new ListDataReader<TestEntity>(data, context);
        reader.Read();
        var value = reader.GetValue(0);

        // Assert
        value.Should().Be("Hello 世界");
    }

    #endregion
}
