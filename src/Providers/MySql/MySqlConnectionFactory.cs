using System.Data;
using MySqlConnector;
using pORM.Core.Interfaces;

namespace pORM.Mysql;

public class MySqlConnectionFactory : IDatabaseConnectionFactory
{
    private readonly string _connectionString;
        
    public MySqlConnectionFactory(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("A connection string is required.", nameof(connectionString))
            : connectionString;
    }
        
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        MySqlConnection connection = (MySqlConnection)await CreateConnectionAsync(cancellationToken);
        try
        {
            MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
            return new MySqlDatabaseTransaction(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
