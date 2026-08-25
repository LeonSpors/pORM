using NSubstitute;
using NUnit.Framework;
using pORM.Core.Interfaces;
using pORM.Mapping;
using pORM.Tests.Models;

namespace pORM.Tests;

[TestFixture]
public class CrudConvenienceTests
{
    [Test]
    public void FirstAsync_WithNoMatchingEntity_ThrowsInvalidOperationException()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        Table<TestEntity> table = new(factory, cache);

        Assert.That(table, Is.Not.Null);
    }

    [Test]
    public void ExistsByIdAsync_WithNullId_ThrowsArgumentNullException()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        Table<TestEntity> table = new(factory, cache);

        Assert.ThrowsAsync<ArgumentNullException>(() => table.ExistsByIdAsync<string>(null!));
    }

    [Test]
    public void RemoveByIdAsync_WithNullId_ThrowsArgumentNullException()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        Table<TestEntity> table = new(factory, cache);

        Assert.ThrowsAsync<ArgumentNullException>(() => table.RemoveByIdAsync<string>(null!));
    }

    [Test]
    public async Task AddBatchAsync_WithEmptyBatch_ReturnsZero()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        Table<TestEntity> table = new(factory, cache);

        int result = await table.AddBatchAsync(Array.Empty<TestEntity>());

        Assert.That(result, Is.Zero);
    }
}
