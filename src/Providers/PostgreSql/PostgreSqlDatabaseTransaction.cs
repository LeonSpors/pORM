using System.Data;
using Npgsql;
using pORM.Core.Interfaces;

namespace pORM.PostgreSql;

public sealed class PostgreSqlDatabaseTransaction : IDatabaseTransaction
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private bool _completed;
    private bool _disposed;

    public PostgreSqlDatabaseTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public IDbConnection Connection => _connection;
    public IDbTransaction Transaction => _transaction;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();
        await _transaction.CommitAsync(cancellationToken);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();
        await _transaction.RollbackAsync(cancellationToken);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            if (!_completed)
                await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
            _completed = true;
        }
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
            throw new InvalidOperationException("The database transaction has already completed.");
    }
}
