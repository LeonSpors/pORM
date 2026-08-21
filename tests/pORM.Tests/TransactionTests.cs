using System.Data;
using NSubstitute;
using NUnit.Framework;
using pORM.Core.Interfaces;
using pORM.Mapping;
using pORM.Tests.Models;

namespace pORM.Tests;

[TestFixture]
public class TransactionTests
{
    [Test]
    public void WithTransaction_WithNullTransaction_ThrowsArgumentNullException()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        Table<TestEntity> table = new(factory, cache);

        Assert.Throws<ArgumentNullException>(() => table.WithTransaction(null!));
    }

    [Test]
    public void WithTransaction_ReturnsTransactionBoundTable()
    {
        IDatabaseConnectionFactory factory = Substitute.For<IDatabaseConnectionFactory>();
        ITableCache cache = Substitute.For<ITableCache>();
        IDatabaseTransaction transaction = Substitute.For<IDatabaseTransaction>();
        transaction.Connection.Returns(Substitute.For<IDbConnection>());
        transaction.Transaction.Returns(Substitute.For<IDbTransaction>());
        Table<TestEntity> table = new(factory, cache);

        ITable<TestEntity> transactionTable = table.WithTransaction(transaction);

        Assert.That(transactionTable, Is.Not.Null);
        Assert.That(transactionTable, Is.TypeOf<Table<TestEntity>>());
    }
}
