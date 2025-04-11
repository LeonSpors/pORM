using Microsoft.Extensions.DependencyInjection;
using pORM.Core.Interfaces;
using pORM.Extensions;
using pORM.ProviderCore;

namespace pORM.Mysql;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MySQL-specific pORM services into the DI container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="connectionString">The connection string for the MySQL database.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDatabaseConnectionFactory>(new MySqlConnectionFactory(connectionString));
        services.AddDatabaseCore();
        return services;
    }
}