using System.Data;
using MySqlConnector;
using pORM.Core.Interfaces;

namespace pORM.Mysql;

public sealed class MySqlDatabaseTransaction : IDatabaseTransaction
{
    private readonly MySqlConnection _connection;
    private readonly MySqlTransaction _transaction;

    public MySqlDatabaseTransaction(MySqlConnection connection, MySqlTransaction transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public IDbConnection Connection => _connection;
    public IDbTransaction Transaction => _transaction;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _transaction.RollbackAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
