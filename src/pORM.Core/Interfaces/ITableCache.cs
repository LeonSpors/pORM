using System.Reflection;
using pORM.Core.Models;

namespace pORM.Core.Interfaces;

public interface ITableCache
{
    IReadOnlyList<TableCacheItem> GetItems<T>();
    TableCacheItem GetItem(PropertyInfo propertyInfo);
    TableCacheItem GetKeyItem<T>();
}