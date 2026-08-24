using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using pORM.Core.Interfaces;
using pORM.Mysql;
using pORM.ProviderCore;

namespace pORM.Tests;

[TestFixture]
public class ProviderRegistrationTests
{
    [Test]
    public void AddMySql_RegistersCoreServicesAndFactory()
    {
        ServiceCollection services = new();

        services.AddMySql("Server=localhost;Database=test;");
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.That(provider.GetService<IGlobalContext>(), Is.Not.Null);
        Assert.That(provider.GetService<ITableCache>(), Is.Not.Null);
        Assert.That(provider.GetService<IDatabaseConnectionFactory>(), Is.TypeOf<MySqlConnectionFactory>());
    }

    [Test]
    public void AddDatabaseCore_WithNullFactory_ThrowsArgumentNullException()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentNullException>(() => services.AddDatabaseCore(null!));
    }
}
