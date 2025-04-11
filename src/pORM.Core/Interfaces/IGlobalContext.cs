namespace pORM.Core.Interfaces;

public interface IGlobalContext
{
    public ITable<T> GetTable<T>()
        where T : class, new();
}