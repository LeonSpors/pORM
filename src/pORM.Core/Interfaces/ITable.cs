using System.Linq.Expressions;

namespace pORM.Core.Interfaces;

public interface ITable<T>
    where T : class, new()
{
    IQuery<T> Query();
    public Task<bool> AddAsync(T item, CancellationToken cancellationToken = default);
    public Task<bool> UpdateAsync(T item, CancellationToken cancellationToken = default);
    public Task<bool> RemoveAsync(T item, CancellationToken cancellationToken = default);
    public Task<bool> ExistsAsync(T item, CancellationToken cancellationToken = default);
    public Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    public Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
}
