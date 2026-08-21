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
    Task<IEnumerable<T>> ToListAsync();
    Task<T?> FirstOrDefaultAsync();
    Task<int> CountAsync();
    Task<bool> AnyAsync();
}
