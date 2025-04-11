using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using pORM.Core.Interfaces;
using pORM.Core.Models;
using pORM.Extensions;
using pORM.Mapping.Utilities;

namespace pORM.Mapping
{
    public class Table<T> : ITable<T>
        where T : class, new()
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly string _tableName;
        private readonly ITableCache _cache;

        public Table(IDatabaseConnectionFactory connectionFactory, ITableCache cache)
        {
            _connectionFactory = connectionFactory;
            _cache = cache;
            
            TableAttribute? tableAttribute = typeof(T).GetCustomAttribute<TableAttribute>();
            if (tableAttribute is null)
                throw new InvalidOperationException($"No table definition found for type {typeof(T).Name}");
            
            _tableName = tableAttribute.Name;
        }

        public async Task<bool> AddAsync(T item)
        {
            using IDbConnection connection = await _connectionFactory.CreateConnectionAsync();
            
            IReadOnlyList<TableCacheItem> mappings = _cache.GetItems<T>();
            IEnumerable<string> columnNames = mappings.Select(m => m.ColumnName);
            IEnumerable<string> parameterNames = mappings.Select(m => "@" + m.Metadata.Name);

            string sql = $"INSERT INTO {_tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)})";

            // Our extension method from SimpleOrmExtensions handles an anonymous object as parameters.
            int rowsAffected = await connection.ExecuteAsync(sql, item);
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateAsync(T item)
        {
            using IDbConnection connection = await _connectionFactory.CreateConnectionAsync();

            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            List<TableCacheItem> mappings = _cache.GetItems<T>().Where(m => !m.IsKey).ToList();

            string setClause = string.Join(", ", mappings.Select(m => $"{m.ColumnName} = @{m.Metadata.Name}"));
            string sql = $"UPDATE {_tableName} SET {setClause} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";

            int rowsAffected = await connection.ExecuteAsync(sql, item);
            return rowsAffected > 0;
        }

        public async Task<bool> RemoveAsync(T item)
        {
            using IDbConnection connection = await _connectionFactory.CreateConnectionAsync();
            
            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            string sql = $"DELETE FROM {_tableName} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";

            // Build a dictionary of parameters using our own parameter container.
            Dictionary<string, object?> parameters = new Dictionary<string, object?>();
            parameters.Add("@" + keyMapping.Metadata.Name, keyMapping.Metadata.GetValue(item));

            int rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsAsync(T item)
        {
            using IDbConnection connection = await _connectionFactory.CreateConnectionAsync();

            TableCacheItem keyMapping = _cache.GetKeyItem<T>();
            string sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {keyMapping.ColumnName} = @{keyMapping.Metadata.Name}";

            Dictionary<string, object?> parameters = new Dictionary<string, object?>();
            parameters.Add("@" + keyMapping.Metadata.Name, keyMapping.Metadata.GetValue(item));

            int count = await connection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
        
        public async Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            using IDbConnection connection = await _connectionFactory.CreateConnectionAsync();

            // Use our custom translator that now uses our SimpleDynamicParameters container.
            ExpressionToSqlTranslator translator = new(_cache);
            string whereClause = translator.Translate(predicate.Body);
            string sql = $"SELECT * FROM {_tableName} WHERE {whereClause}";

            // Pass our parameters as a dictionary using GetParameters().
            return await connection.QueryAsync<T>(sql, translator.Parameters.GetParameters());
        }
        
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            using IDbConnection connection = await _connectionFactory.CreateConnectionAsync();

            ExpressionToSqlTranslator translator = new(_cache);
            string whereClause = translator.Translate(predicate.Body);
            string sql = $"SELECT * FROM {_tableName} WHERE {whereClause} LIMIT 1";

            IEnumerable<T> result = await connection.QueryAsync<T>(sql, translator.Parameters.GetParameters());
            return result.ElementAtOrDefault(0);
        }
    }
}
