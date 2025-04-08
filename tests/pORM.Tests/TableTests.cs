using System.Data;
using Moq;
using pORM.Core.Interfaces;
using pORM.Core.Caching;
using pORM.Mapping;
using pORM.Tests.Helpers;
using pORM.Tests.Models;

namespace pORM.Tests;


public class TableTests
{
    [Fact]
    public void TableConstructor_Throws_WhenEntityLacksTableAttribute()
    {
        // Arrange: Create a mock connection factory (not needed further because constructor should fail before connection is used).
        Mock<IDatabaseConnectionFactory> mockFactory = new Mock<IDatabaseConnectionFactory>();
        TableCache tableCache = new TableCache(); // Using real implementation here.

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            Table<NoTableEntity> table = new Table<NoTableEntity>(mockFactory.Object, tableCache);
        });
    }

    [Fact]
    public async Task AddAsync_ReturnsTrue_ForTestEntity()
    {
        // Arrange

        // Create a fake data parameter collection that will be returned by the command.
        FakeDataParameterCollection fakeParameters = new FakeDataParameterCollection();

        // Create a mock for IDbCommand.
        Mock<IDbCommand> mockCommand = new Mock<IDbCommand>();
        // When ExecuteNonQuery is called, simulate a successful insert by returning 1.
        mockCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(1);
        // Ensure the Parameters property is not null.
        mockCommand.Setup(cmd => cmd.Parameters).Returns(fakeParameters);
        
        mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(() =>
        {
            Mock<IDbDataParameter> paramMock = new Mock<IDbDataParameter>();
            return paramMock.Object;
        });

        // Create a mock for IDbConnection.
        Mock<IDbConnection> mockConnection = new Mock<IDbConnection>();
        // When CreateCommand is called, return our mocked command.
        mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
        // Setup Open() as a no-op.
        mockConnection.Setup(conn => conn.Open());

        // Create a mock for the connection factory that returns our mocked connection.
        Mock<IDatabaseConnectionFactory> mockFactory = new Mock<IDatabaseConnectionFactory>();
        mockFactory.Setup(factory => factory.CreateConnectionAsync())
            .ReturnsAsync(mockConnection.Object);

        // Create a real instance of TableCache.
        TableCache tableCache = new TableCache();

        // Instantiate Table<TestEntity> using the mocked factory and real cache.
        Table<TestEntity> table = new Table<TestEntity>(mockFactory.Object, tableCache);
        TestEntity entity = new TestEntity { Id = 1, Name = "Test" };

        // Act: Call AddAsync.
        bool result = await table.AddAsync(entity);

        // Assert: Verify that AddAsync returned true.
        Assert.True(result);
        // Verify that ExecuteNonQuery was called once.
        mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
    }
}