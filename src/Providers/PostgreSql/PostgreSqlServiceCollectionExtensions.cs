using Microsoft.Extensions.DependencyInjection;
using pORM.ProviderCore;

namespace pORM.PostgreSql;

public static class PostgreSqlServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSql(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddDatabaseCore(new PostgreSqlConnectionFactory(connectionString));
    }
}
