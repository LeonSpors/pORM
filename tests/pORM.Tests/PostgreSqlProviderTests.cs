using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using pORM.Core.Interfaces;
using pORM.PostgreSql;

namespace pORM.Tests;

[TestFixture]
public class PostgreSqlProviderTests
{
    [Test]
    public void Constructor_WithMissingConnectionString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new PostgreSqlConnectionFactory(" "));
    }

    [Test]
    public void AddPostgreSql_RegistersConnectionFactory()
    {
        ServiceCollection services = new();
        services.AddPostgreSql("Host=localhost;Database=test;");

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.That(provider.GetService<IDatabaseConnectionFactory>(), Is.TypeOf<PostgreSqlConnectionFactory>());
    }
}
