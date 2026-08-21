namespace pORM.Core.Interfaces;

public interface IGlobalContext
{
    public ITable<T> GetTable<T>()
        where T : class, new();
    public Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
