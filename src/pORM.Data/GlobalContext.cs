using pORM.Core.Interfaces;
using pORM.Mapping;

namespace pORm.Data;

public class GlobalContext : IGlobalContext
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ITableCache _tableCache;

    public GlobalContext(IDatabaseConnectionFactory connectionFactory, ITableCache tableCache)
    {
        _connectionFactory = connectionFactory;
        _tableCache = tableCache;
    }

    public ITable<T> GetTable<T>() 
        where T : class, new()
    {
        return new Table<T>(_connectionFactory, _tableCache);
    }
}