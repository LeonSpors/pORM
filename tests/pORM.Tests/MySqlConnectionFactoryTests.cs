using NUnit.Framework;
using pORM.Mysql;

namespace pORM.Tests;

[TestFixture]
public class MySqlConnectionFactoryTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_WithMissingConnectionString_ThrowsArgumentException(string? connectionString)
    {
        Assert.Throws<ArgumentException>(() => new MySqlConnectionFactory(connectionString!));
    }
}
