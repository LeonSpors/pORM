using System.Data;

namespace pORM.Core.Interfaces;

public interface IDatabaseConnectionFactory
{
    public Task<IDbConnection> CreateConnectionAsync();
}