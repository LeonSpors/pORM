using System.Linq.Expressions;

namespace pORM.Core.Interfaces;

public interface IQuery<T>
    where T : class, new()
{
    IQuery<T> Where(Expression<Func<T, bool>> predicate);
    IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
    IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
    IQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
    IQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
    IQuery<T> Skip(int count);
    IQuery<T> Take(int count);
    Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TResult>> SelectAsync<TResult>(
        Expression<Func<T, TResult>> projection,
        CancellationToken cancellationToken = default)
        where TResult : class, new();
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
