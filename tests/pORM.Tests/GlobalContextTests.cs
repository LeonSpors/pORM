using System.Linq.Expressions;
using NSubstitute;
using NUnit.Framework;
using pORM.Core.Interfaces;
using pORm.Data;
using pORM.Mapping;
using pORM.Tests.Models;

namespace pORM.Tests;

[TestFixture]
public class GlobalContextTests
{
    [Test]
    public void Constructor_WithValidEntity_ReturnsTable()
    {
        IDatabaseConnectionFactory? connectionFactory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache? cache = Substitute.For<ITableCache>();
        GlobalContext globalContext = new GlobalContext(connectionFactory, cache);

        ITable<TestEntity> table = globalContext.GetTable<TestEntity>();

        Assert.That(table, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithInvalidEntity_ThrowsInvalidOperationException()
    {
        IDatabaseConnectionFactory? connectionFactory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache? cache = Substitute.For<ITableCache>();
        
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(
            () => new Table<InvalidEntity>(connectionFactory, cache)
        );
        
        Assert.That(ex?.Message, Is.EqualTo($"No table definition found for type {nameof(InvalidEntity)}"));
    }
    
        // ----- AddAsync Tests -----
    [Test]
    public async Task AddAsync_ValidItem_ReturnsTrueAndPersistsItem()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity entity = new() { Id = 1, Name = "Test" };

        table.AddAsync(entity).Returns(Task.FromResult(true));

        bool result = await table.AddAsync(entity);

        Assert.That(result, Is.True);
        await table.Received().AddAsync(entity);
    }

    [Test]
    public void AddAsync_NullItem_ThrowsArgumentNullException()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity? nullEntity = null;

        table.When(x => x.AddAsync(Arg.Any<TestEntity>()))
            .Do(callInfo => { throw new ArgumentNullException(); });

        Assert.ThrowsAsync<ArgumentNullException>(async () => await table.AddAsync(nullEntity!));
    }

    // ----- UpdateAsync Tests -----
    [Test]
    public async Task UpdateAsync_ExistingItem_ReturnsTrueAndUpdatesItem()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity entity = new() { Id = 1, Name = "Test" };

        table.UpdateAsync(entity).Returns(Task.FromResult(true));

        bool result = await table.UpdateAsync(entity);

        Assert.That(result, Is.True);
        await table.Received().UpdateAsync(entity);
    }

    [Test]
    public void UpdateAsync_NullItem_ThrowsArgumentNullException()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity? nullEntity = null;
        table.When(x => x.UpdateAsync(Arg.Any<TestEntity>()))
            .Do(callInfo => { throw new ArgumentNullException(); });

        Assert.ThrowsAsync<ArgumentNullException>(async () => await table.UpdateAsync(nullEntity!));
    }

    // ----- RemoveAsync Tests -----
    [Test]
    public async Task RemoveAsync_ExistingItem_ReturnsTrueAndRemovesItem()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity entity = new() { Id = 1, Name = "Test" };

        table.RemoveAsync(entity).Returns(Task.FromResult(true));

        bool result = await table.RemoveAsync(entity);

        Assert.That(result, Is.True);
        await table.Received().RemoveAsync(entity);
    }

    [Test]
    public void RemoveAsync_NullItem_ThrowsArgumentNullException()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity? nullEntity = null;
        table.When(x => x.RemoveAsync(Arg.Any<TestEntity>()))
            .Do(callInfo => { throw new ArgumentNullException(); });

        Assert.ThrowsAsync<ArgumentNullException>(async () => await table.RemoveAsync(nullEntity!));
    }

    // ----- ExistsAsync Tests -----
    [Test]
    public async Task ExistsAsync_ItemExists_ReturnsTrue()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity entity = new() { Id = 1, Name = "Test" };

        table.ExistsAsync(entity).Returns(Task.FromResult(true));

        bool result = await table.ExistsAsync(entity);

        Assert.That(result, Is.True);
        await table.Received().ExistsAsync(entity);
    }

    // ----- WhereAsync Tests -----
    [Test]
    public async Task WhereAsync_MatchingPredicate_ReturnsCorrectSubset()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity entity = new() { Id = 1, Name = "Test" };

        table.WhereAsync(Arg.Any<Expression<Func<TestEntity, bool>>>())
            .Returns(Task.FromResult<IEnumerable<TestEntity>>(new List<TestEntity> { entity }));

        Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1;
        IEnumerable<TestEntity> result = await table.WhereAsync(predicate);

        Assert.That(result, Contains.Item(entity));
        await table.Received().WhereAsync(predicate);
    }

    [Test]
    public void WhereAsync_NullPredicate_ThrowsArgumentNullException()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        Expression<Func<TestEntity, bool>>? nullPredicate = null;
        table.When(x => x.WhereAsync(Arg.Any<Expression<Func<TestEntity, bool>>>()))
            .Do(callInfo => { throw new ArgumentNullException(); });

        Assert.ThrowsAsync<ArgumentNullException>(async () => await table.WhereAsync(nullPredicate!));
    }

    // ----- FirstOrDefaultAsync Tests -----
    [Test]
    public async Task FirstOrDefaultAsync_MatchingPredicate_ReturnsFirstItem()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        TestEntity entity = new() { Id = 1, Name = "Test" };

        table.FirstOrDefaultAsync(Arg.Any<Expression<Func<TestEntity, bool>>>())
            .Returns(Task.FromResult<TestEntity?>(entity));

        Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1;
        TestEntity? result = await table.FirstOrDefaultAsync(predicate);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(entity.Id));
        await table.Received().FirstOrDefaultAsync(predicate);
    }

    [Test]
    public void FirstOrDefaultAsync_NullPredicate_ThrowsArgumentNullException()
    {
        ITable<TestEntity>? table = Substitute.For<ITable<TestEntity>>();
        Expression<Func<TestEntity, bool>>? nullPredicate = null;
        table.When(x => x.FirstOrDefaultAsync(Arg.Any<Expression<Func<TestEntity, bool>>>()))
            .Do(callInfo => { throw new ArgumentNullException(); });

        Assert.ThrowsAsync<ArgumentNullException>(async () => await table.FirstOrDefaultAsync(nullPredicate!));
    }
}