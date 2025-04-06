using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace pORM.Extensions;

public static class IDBCommandExtensions
{
    /// <summary>
    /// Executes a SQL command and returns the number of affected rows.
    /// </summary>
    public static async Task<int> ExecuteAsync(this IDbConnection connection, string sql, object? param = null)
    {
        await EnsureOpenAsync(connection);
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        AddParametersToCommand(command, param);
        if (command is DbCommand dbCommand)
            return await dbCommand.ExecuteNonQueryAsync();
        else
            return command.ExecuteNonQuery();
    }

    /// <summary>
    /// Executes a SQL query and maps the result to an enumerable of T.
    /// </summary>
    public static async Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection connection, string sql,
        object? param = null) where T : new()
    {
        await EnsureOpenAsync(connection);
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        AddParametersToCommand(command, param);
        List<T> list = new List<T>();
        if (command is DbCommand dbCommand)
        {
            await using DbDataReader reader = await dbCommand.ExecuteReaderAsync();
            while (await ((DbDataReader)reader).ReadAsync())
            {
                list.Add(MapReaderToEntity<T>(reader));
            }
        }
        else
        {
            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapReaderToEntity<T>(reader));
            }
        }

        return list;
    }
    
    /// <summary>
    /// Executes a SQL scalar query asynchronously and returns a value of type T.
    /// </summary>
    public static async Task<T> ExecuteScalarAsync<T>(this IDbConnection connection, string sql, object? param = null)
    {
        await EnsureOpenAsync(connection);
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        AddParametersToCommand(command, param);
        object? result = null;
        if (command is DbCommand dbCommand)
            result = await dbCommand.ExecuteScalarAsync();
        else
            result = command.ExecuteScalar();

        if (result == null || result == DBNull.Value)
            return default!;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    #region Helper Methods

    private static async Task EnsureOpenAsync(IDbConnection connection)
    {
        if (connection.State == ConnectionState.Open)
            return;

        if (connection is DbConnection dbConn)
            await dbConn.OpenAsync();
        else
            connection.Open();
    }

    /// <summary>
    /// Adds parameters to the IDbCommand from either an anonymous object or a dictionary.
    /// </summary>
    private static void AddParametersToCommand(IDbCommand command, object? param)
    {
        if (param == null) return;

        if (param is IDictionary<string, object?> dictionary)
        {
            foreach (KeyValuePair<string, object?> kv in dictionary)
            {
                IDbDataParameter parameter = command.CreateParameter();
                parameter.ParameterName = kv.Key;
                parameter.Value = kv.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }
        else
        {
            // Reflect over public properties.
            foreach (PropertyInfo prop in param.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                IDbDataParameter parameter = command.CreateParameter();
                // Parameter names are prefixed with "@".
                parameter.ParameterName = "@" + prop.Name;
                object? value = prop.GetValue(param);
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }
    }

    /// <summary>
    /// Maps a data reader row to an instance of T using reflection.
    /// Honors [Column("...")] attribute if present.
    /// </summary>
    private static T MapReaderToEntity<T>(IDataReader reader) where T : new()
    {
        T entity = new T();
        Type type = typeof(T);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            string columnName = reader.GetName(i);
            object value = reader.GetValue(i);
            if (value == DBNull.Value)
                continue;

            // Find property by matching [Column] attribute or property name.
            PropertyInfo? property = properties.FirstOrDefault(p =>
            {
                ColumnAttribute? colAttr = p.GetCustomAttribute<ColumnAttribute>();
                return colAttr != null
                    ? string.Equals(colAttr.Name, columnName, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase);
            });
            if (property != null)
            {
                try
                {
                    // Handle Guid and Nullable<Guid> properties separately.
                    if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                    {
                        Guid guidValue;
                        if (value is string s)
                        {
                            guidValue = Guid.Parse(s);
                        }
                        else if (value is Guid g)
                        {
                            guidValue = g;
                        }
                        else
                        {
                            // Fallback: try converting the value to string and parsing.
                            guidValue = Guid.Parse(value.ToString()!);
                        }

                        property.SetValue(entity, guidValue);
                    }
                    else
                    {
                        // For other types, use Convert.ChangeType.
                        property.SetValue(entity, Convert.ChangeType(value, property.PropertyType));
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error mapping column '{columnName}' to property '{property.Name}'.", ex);
                }
            }
        }

        return entity;
    }

    #endregion
}