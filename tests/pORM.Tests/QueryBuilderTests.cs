using System.Reflection;
using System.Linq.Expressions;
using NSubstitute;
using NUnit.Framework;
using pORM.Core.Interfaces;
using pORM.Core.Models;
using pORM.Mapping;
using pORM.Tests.Models;

namespace pORM.Tests;

[TestFixture]
public class QueryBuilderTests
{
    [Test]
    public void Skip_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        IQuery<TestEntity> query = CreateQuery();

        Assert.Throws<ArgumentOutOfRangeException>(() => query.Skip(-1));
    }

    [Test]
    public void Take_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        IQuery<TestEntity> query = CreateQuery();

        Assert.Throws<ArgumentOutOfRangeException>(() => query.Take(-1));
    }

    [Test]
    public void Where_WithNullPredicate_ThrowsArgumentNullException()
    {
        IQuery<TestEntity> query = CreateQuery();

        Assert.Throws<ArgumentNullException>(() => query.Where(null!));
    }

    [Test]
    public void OrderBy_WithNullSelector_ThrowsArgumentNullException()
    {
        IQuery<TestEntity> query = CreateQuery();

        Assert.Throws<ArgumentNullException>(() => query.OrderBy<int>(null!));
    }

    [Test]
    public void Query_WithMultipleOrderings_BuildsAllOrderClauses()
    {
        ITableCache cache = Substitute.For<ITableCache>();
        cache.GetItem(Arg.Any<PropertyInfo>())
            .Returns(callInfo => new TableCacheItem(callInfo.Arg<PropertyInfo>()));
        Query<TestEntity> query = CreateQuery(cache);

        query.OrderBy(entity => entity.Name)
            .ThenByDescending(entity => entity.Id);

        string sql = GetSql(query);

        Assert.That(sql, Does.Contain("ORDER BY Name ASC, Id DESC"));
    }

    [Test]
    public void Query_WithSkipOnly_UsesUnboundedLimit()
    {
        Query<TestEntity> query = CreateQuery();

        query.Skip(10);

        string sql = GetSql(query);

        Assert.That(sql, Does.Contain("LIMIT 18446744073709551615 OFFSET 10"));
    }

    [Test]
    public void Query_WithProjection_SelectsMappedColumns()
    {
        ITableCache cache = Substitute.For<ITableCache>();
        cache.GetItem(Arg.Any<PropertyInfo>())
            .Returns(callInfo => new TableCacheItem(callInfo.Arg<PropertyInfo>()));
        Query<TestEntity> query = CreateQuery(cache);

        MethodInfo buildProjection = typeof(Query<TestEntity>).GetMethod(
            "BuildProjectionQuery",
            BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(TestEntityProjection));
        object queryParts = buildProjection.Invoke(query, new object[]
        {
            (Expression<Func<TestEntity, TestEntityProjection>>)(entity => new TestEntityProjection { DisplayName = entity.Name })
        })!;
        string sql = (string)queryParts.GetType().GetProperty("Sql")!.GetValue(queryParts)!;

        Assert.That(sql, Is.EqualTo("SELECT Name AS DisplayName FROM sample_table"));
    }

    [Test]
    public void Query_WithJoin_BuildsAliasedInnerJoin()
    {
        ITableCache cache = Substitute.For<ITableCache>();
        cache.GetItem(Arg.Any<PropertyInfo>())
            .Returns(callInfo => new TableCacheItem(callInfo.Arg<PropertyInfo>()));
        Query<TestEntity> query = CreateQuery(cache);

        MethodInfo buildJoin = typeof(Query<TestEntity>).GetMethod(
            "BuildJoinQuery",
            BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(RelatedEntity), typeof(JoinProjection));
        object queryParts = buildJoin.Invoke(query, new object[]
        {
            "related_entities",
            (Expression<Func<TestEntity, object>>)(entity => entity.Id),
            (Expression<Func<RelatedEntity, object>>)(entity => entity.Id),
            (Expression<Func<TestEntity, RelatedEntity, JoinProjection>>)((entity, related) => new JoinProjection
            {
                Name = entity.Name,
                Description = related.Description
            })
        })!;
        string sql = (string)queryParts.GetType().GetProperty("Sql")!.GetValue(queryParts)!;

        Assert.That(sql, Is.EqualTo("SELECT t.Name AS Name, j.Description AS Description FROM sample_table t INNER JOIN related_entities j ON t.Id = j.Id"));
    }

    private static Query<TestEntity> CreateQuery()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        return new Query<TestEntity>(factory, cache, "sample_table");
    }

    private static Query<TestEntity> CreateQuery(ITableCache cache)
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        return new Query<TestEntity>(factory, cache, "sample_table");
    }

    private static string GetSql(Query<TestEntity> query)
    {
        MethodInfo buildQuery = typeof(Query<TestEntity>).GetMethod(
            "BuildQuery",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        object queryParts = buildQuery.Invoke(query, new object[] { false })!;
        return (string)queryParts.GetType().GetProperty("Sql")!.GetValue(queryParts)!;
    }
}
