namespace pORM.Mapping.Models;

public class DynamicParameters
{
    private readonly Dictionary<string, object?> _parameters = new();

    /// <summary>
    /// Returns all parameter names.
    /// </summary>
    public IEnumerable<string> ParameterNames => _parameters.Keys;

    /// <summary>
    /// Adds or updates a parameter.
    /// </summary>
    public void Add(string name, object? value)
    {
        _parameters[name] = value;
    }

    /// <summary>
    /// Retrieves a parameter value cast to T.
    /// </summary>
    public T Get<T>(string name)
    {
        if (_parameters.TryGetValue(name, out object? value) && value is T result)
            return result;
        
        throw new KeyNotFoundException($"Parameter '{name}' not found.");
    }

    /// <summary>
    /// Returns the underlying dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetParameters() => _parameters;
}