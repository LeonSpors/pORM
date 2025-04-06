using System.Linq.Expressions;

namespace pORM.Core.Interfaces;

public interface ITable<T>
    where T : class
{
    public Task<bool> AddAsync(T item);
    public Task<bool> UpdateAsync(T item);
    public Task<bool> RemoveAsync(T item);
    public Task<bool> ExistsAsync(T item);
    public Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate);
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
}