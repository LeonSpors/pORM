using System.Data;
using MySqlConnector;
using pORM.Core.Interfaces;

namespace pORM.Mysql;

public sealed class MySqlDatabaseTransaction : IDatabaseTransaction
{
    private readonly MySqlConnection _connection;
    private readonly MySqlTransaction _transaction;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _completed;
    private bool _disposed;

    public MySqlDatabaseTransaction(MySqlConnection connection, MySqlTransaction transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public IDbConnection Connection => _connection;
    public IDbTransaction Transaction => _transaction;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureActive();
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureActive();
            await _transaction.RollbackAsync(cancellationToken);
            _completed = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
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
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_completed)
            throw new InvalidOperationException("The database transaction has already completed.");
    }
}
