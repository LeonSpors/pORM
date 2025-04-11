using System.Data;
using MySqlConnector;
using pORM.Core.Interfaces;

namespace pORM.Mysql;

public class MySqlConnectionFactory : IDatabaseConnectionFactory
{
    private readonly string _connectionString;
        
    public MySqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }
        
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}