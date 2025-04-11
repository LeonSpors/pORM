using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using pORM.Core.Interfaces;
using pORM.Core.Models;

namespace pORM.Core.Caching;

public class TableCache : ITableCache
{
    private readonly ConcurrentDictionary<Type, IReadOnlyList<TableCacheItem>> _propertyMappingCache = new();

    public IReadOnlyList<TableCacheItem> GetItems<T>()
    {
        Type type = typeof(T);
        return _propertyMappingCache.GetOrAdd(type, t =>
            t.GetProperties()
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                .Select(p => new TableCacheItem(p))
                .ToList());
    }

    // Gets the mapping for the provided PropertyInfo.
    public TableCacheItem GetItem(PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        // Use the declaring type of the property as the cache key.
        var items = _propertyMappingCache.GetOrAdd(propertyInfo.DeclaringType, t =>
            t.GetProperties()
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                .Select(p => new TableCacheItem(p))
                .ToList());

        // Return the mapping that matches the provided PropertyInfo.
        var mapping = items.FirstOrDefault(item => item.Metadata.Equals(propertyInfo));
        if (mapping == null)
            throw new InvalidOperationException($"No mapping found for property '{propertyInfo.Name}' on type '{propertyInfo.DeclaringType?.Name}'.");

        return mapping;
    }

    public TableCacheItem GetKeyItem<T>()
    {
        // Find the first property mapping that is marked as key.
        TableCacheItem? mapping = GetItems<T>().FirstOrDefault(pm => pm.IsKey);
        if (mapping == null)
            throw new InvalidOperationException($"No primary key defined on type {typeof(T).Name}");
        return mapping;
    }
}