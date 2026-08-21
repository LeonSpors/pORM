using NSubstitute;
using NUnit.Framework;
using pORM.Core.Interfaces;
using pORM.Mapping;
using pORM.Tests.Models;

namespace pORM.Tests;

[TestFixture]
public class TableValidationTests
{
    private static ITableCache CreateCache() => Substitute.For<ITableCache>();

    [Test]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Table<TestEntity>(null!, CreateCache()));
    }

    [Test]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();

        Assert.Throws<ArgumentNullException>(() => new Table<TestEntity>(factory, null!));
    }

    [Test]
    public void AddAsync_WithNullItem_ThrowsArgumentNullException()
    {
        Table<TestEntity> table = CreateTable();

        Assert.ThrowsAsync<ArgumentNullException>(() => table.AddAsync(null!));
    }

    [Test]
    public void UpdateAsync_WithNullItem_ThrowsArgumentNullException()
    {
        Table<TestEntity> table = CreateTable();

        Assert.ThrowsAsync<ArgumentNullException>(() => table.UpdateAsync(null!));
    }

    [Test]
    public void RemoveAsync_WithNullItem_ThrowsArgumentNullException()
    {
        Table<TestEntity> table = CreateTable();

        Assert.ThrowsAsync<ArgumentNullException>(() => table.RemoveAsync(null!));
    }

    [Test]
    public void ExistsAsync_WithNullItem_ThrowsArgumentNullException()
    {
        Table<TestEntity> table = CreateTable();

        Assert.ThrowsAsync<ArgumentNullException>(() => table.ExistsAsync(null!));
    }

    [Test]
    public void WhereAsync_WithNullPredicate_ThrowsArgumentNullException()
    {
        Table<TestEntity> table = CreateTable();

        Assert.ThrowsAsync<ArgumentNullException>(() => table.WhereAsync(null!));
    }

    [Test]
    public void FirstOrDefaultAsync_WithNullPredicate_ThrowsArgumentNullException()
    {
        Table<TestEntity> table = CreateTable();

        Assert.ThrowsAsync<ArgumentNullException>(() => table.FirstOrDefaultAsync(null!));
    }

    private static Table<TestEntity> CreateTable()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        return new Table<TestEntity>(factory, CreateCache());
    }
}
