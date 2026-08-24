using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using pORM.Core.Interfaces;
using pORM.Core.Models;
using pORM.Extensions;
using pORM.Mapping.Models;
using pORM.Mapping.Utilities;

namespace pORM.Mapping
{
    public class Table<T> : ITable<T>
        where T : class, new()
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly string _tableName;
        private readonly ITableCache _cache;
        private readonly IDatabaseTransaction? _transaction;

        public Table(IDatabaseConnectionFactory connectionFactory, ITableCache cache, IDatabaseTransaction? transaction = null)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _transaction = transaction;
            
            TableAttribute? tableAttribute = typeof(T).GetCustomAttribute<TableAttribute>();
            if (tableAttribute is null)
                throw new InvalidOperationException($"No table definition found for type {typeof(T).Name}");
            
            _tableName = tableAttribute.Name;
        }

        public async Task<bool> AddAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;
            
            IReadOnlyList<TableCacheItem> mappings = _cache.GetItems<T>();
            IEnumerable<string> columnNames = mappings.Select(m => m.ColumnName);
            IEnumerable<string> parameterNames = mappings.Select(m => "@" + m.Metadata.Name);

            string sql = $"INSERT INTO {_tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)})";

            // Our extension method from SimpleOrmExtensions handles an anonymous object as parameters.
            int rowsAffected = await connection.ExecuteAsync(sql, item, cancellationToken, _transaction?.Transaction);
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;

            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            List<TableCacheItem> mappings = _cache.GetItems<T>().Where(m => !m.IsKey).ToList();
            if (mappings.Count == 0)
                throw new InvalidOperationException($"No non-key properties defined for type {typeof(T).Name}");

            string setClause = string.Join(", ", mappings.Select(m => $"{m.ColumnName} = @{m.Metadata.Name}"));
            string sql = $"UPDATE {_tableName} SET {setClause} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";

            int rowsAffected = await connection.ExecuteAsync(sql, item, cancellationToken, _transaction?.Transaction);
            return rowsAffected > 0;
        }

        public async Task<bool> RemoveAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;
            
            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            string sql = $"DELETE FROM {_tableName} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";

            // Build a dictionary of parameters using our own parameter container.
            Dictionary<string, object?> parameters = new Dictionary<string, object?>();
            parameters.Add("@" + keyMapping.Metadata.Name, keyMapping.Metadata.GetValue(item));

            int rowsAffected = await connection.ExecuteAsync(sql, parameters, cancellationToken, _transaction?.Transaction);
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;

            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";

            Dictionary<string, object?> parameters = new Dictionary<string, object?>();
            parameters.Add("@" + keyMapping.Metadata.Name, keyMapping.Metadata.GetValue(item));

            int count = await connection.ExecuteScalarAsync<int>(sql, parameters, cancellationToken, _transaction?.Transaction);
            return count > 0;
        }
        
        public async Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;

            // Use our custom translator that now uses our SimpleDynamicParameters container.
            ExpressionToSqlTranslator translator = new(_cache);
            string whereClause = translator.Translate(predicate.Body);
            string sql = $"SELECT * FROM {_tableName} WHERE {whereClause}";

            // Pass our parameters as a dictionary using GetParameters().
            return await connection.QueryAsync<T>(sql, translator.Parameters.GetParameters(), cancellationToken, _transaction?.Transaction);
        }
        
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;

            ExpressionToSqlTranslator translator = new(_cache);
            string whereClause = translator.Translate(predicate.Body);
            string sql = $"SELECT * FROM {_tableName} WHERE {whereClause} LIMIT 1";

            IEnumerable<T> result = await connection.QueryAsync<T>(sql, translator.Parameters.GetParameters(), cancellationToken, _transaction?.Transaction);
            return result.ElementAtOrDefault(0);
        }

        public async Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            T? result = await FirstOrDefaultAsync(predicate, cancellationToken);
            return result ?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} matched the predicate.");
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;
            DynamicParameters parameters = new();
            string whereClause = BuildWhereClause(predicate, parameters);
            string sql = $"SELECT COUNT(1) FROM {_tableName}{whereClause}";
            return await connection.ExecuteScalarAsync<int>(sql, parameters.GetParameters(), cancellationToken, _transaction?.Transaction);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            return await CountAsync(predicate, cancellationToken) > 0;
        }

        public async Task<bool> ExistsByIdAsync<TKey>(TKey id, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(id);
            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";
            Dictionary<string, object?> parameters = new() { ["@" + keyMapping.Metadata.Name] = id };
            return await connection.ExecuteScalarAsync<int>(sql, parameters, cancellationToken, _transaction?.Transaction) > 0;
        }

        public async Task<bool> RemoveByIdAsync<TKey>(TKey id, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(id);
            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
            IDbConnection connection = lease.Connection;
            string sql = $"DELETE FROM {_tableName} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";
            Dictionary<string, object?> parameters = new() { ["@" + keyMapping.Metadata.Name] = id };
            return await connection.ExecuteAsync(sql, parameters, cancellationToken, _transaction?.Transaction) > 0;
        }

        public IQuery<T> Query() => new Query<T>(_connectionFactory, _cache, _tableName, _transaction);

        public ITable<T> WithTransaction(IDatabaseTransaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            return new Table<T>(_connectionFactory, _cache, transaction);
        }

        private async Task<ConnectionLease> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (_transaction is not null)
            {
                EnsureTransactionConnectionIsOpen(_transaction);
                return new ConnectionLease(_transaction.Connection, ownsConnection: false);
            }

            return new ConnectionLease(
                await _connectionFactory.CreateConnectionAsync(cancellationToken),
                ownsConnection: true);
        }

        private static void EnsureTransactionConnectionIsOpen(IDatabaseTransaction transaction)
        {
            if (transaction.Connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The transaction connection must remain open while the transaction is active.");
        }

        private sealed class ConnectionLease : IAsyncDisposable
        {
            public IDbConnection Connection { get; }
            private readonly bool _ownsConnection;

            public ConnectionLease(IDbConnection connection, bool ownsConnection)
            {
                Connection = connection;
                _ownsConnection = ownsConnection;
            }

            public async ValueTask DisposeAsync()
            {
                if (!_ownsConnection)
                    return;

                if (Connection is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else
                    Connection.Dispose();
            }
        }

        private string BuildWhereClause(Expression<Func<T, bool>>? predicate, DynamicParameters parameters)
        {
            if (predicate is null)
                return string.Empty;

            ExpressionToSqlTranslator translator = new(_cache);
            string whereClause = translator.Translate(predicate.Body);
            foreach (string name in translator.Parameters.ParameterNames)
                parameters.Add(name, translator.Parameters.GetValue(name));

            return $" WHERE {whereClause}";
        }
    }
}
