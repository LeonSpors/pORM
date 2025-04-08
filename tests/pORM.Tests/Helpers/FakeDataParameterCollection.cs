using System.Collections;
using System.Data;

namespace pORM.Tests.Helpers;

public class FakeDataParameterCollection : IDataParameterCollection
{
    private readonly List<object> _items = new List<object>();

    public object this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public object this[string parameterName]
    {
        get => _items.Find(item => item?.ToString() == parameterName);
        set { /* leave unimplemented for the test */ }
    }

    public bool IsFixedSize => false;
    public bool IsReadOnly => false;
    public int Count => _items.Count;
    public bool IsSynchronized => false;
    public object SyncRoot => new object();

    public int Add(object value)
    {
        _items.Add(value);
        return _items.Count - 1;
    }

    public void Clear() => _items.Clear();

    public bool Contains(object value) => _items.Contains(value);

    public bool Contains(string parameterName) => _items.Exists(item => item?.ToString() == parameterName);

    public void CopyTo(Array array, int index) => _items.ToArray().CopyTo(array, index);

    public IEnumerator GetEnumerator() => _items.GetEnumerator();

    public int IndexOf(object value) => _items.IndexOf(value);

    public int IndexOf(string parameterName) =>
        _items.FindIndex(item => item?.ToString() == parameterName);

    public void Insert(int index, object value) => _items.Insert(index, value);

    public void Remove(object value) => _items.Remove(value);

    public void RemoveAt(int index) => _items.RemoveAt(index);

    public void RemoveAt(string parameterName)
    {
        int index = IndexOf(parameterName);
        if (index >= 0)
            RemoveAt(index);
    }
}