using System.Linq.Expressions;
using NSubstitute;
using NUnit.Framework;
using pORM.Core.Interfaces;
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

    private static IQuery<TestEntity> CreateQuery()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        return new Query<TestEntity>(factory, cache, "sample_table");
    }
}
