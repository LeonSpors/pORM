using Microsoft.Extensions.DependencyInjection;
using pORM.ProviderCore;

namespace pORM.Mysql;

public static class MySqlServiceCollectionExtensions
{
    public static IServiceCollection AddMySql(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDatabaseCore(new MySqlConnectionFactory(connectionString));
    }
}
