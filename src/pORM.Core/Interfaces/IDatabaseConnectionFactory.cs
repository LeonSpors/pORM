using System.Data;

namespace pORM.Core.Interfaces;

public interface IDatabaseConnectionFactory
{
    public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    public Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
