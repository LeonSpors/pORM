using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace pORM.Core.Models;

public class TableCacheItem
{
    public PropertyInfo Metadata { get; }
    public bool IsKey { get; }
    public string ColumnName { get; }

    public TableCacheItem(PropertyInfo metadata)
    {
        Metadata = metadata;
        IsKey = metadata.GetCustomAttribute<KeyAttribute>() != null;
        ColumnName = metadata.GetCustomAttribute<ColumnAttribute>()?.Name ?? metadata.Name;
    }
}