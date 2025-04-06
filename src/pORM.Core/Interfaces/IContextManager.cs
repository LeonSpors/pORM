namespace pORM.Core.Interfaces;

public interface IContextManager
{
    public ITable<T> GetTable<T>()
        where T : class, new();
}