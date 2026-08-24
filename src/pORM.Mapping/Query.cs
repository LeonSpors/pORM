using System.Linq.Expressions;
using System.Reflection;
using pORM.Core.Interfaces;
using pORM.Core.Models;
using pORM.Extensions;
using pORM.Mapping.Models;
using pORM.Mapping.Utilities;

namespace pORM.Mapping;

public sealed class Query<T> : IQuery<T>
    where T : class, new()
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ITableCache _cache;
    private readonly string _tableName;
    private readonly IDatabaseTransaction? _transaction;
    private Expression<Func<T, bool>>? _predicate;
    private readonly List<OrderClause> _orderBy = new();
    private int? _skip;
    private int? _take;

    public Query(IDatabaseConnectionFactory connectionFactory, ITableCache cache, string tableName, IDatabaseTransaction? transaction = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _tableName = string.IsNullOrWhiteSpace(tableName)
            ? throw new ArgumentException("A table name is required.", nameof(tableName))
            : tableName;
        _transaction = transaction;
    }

    public IQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicate = _predicate is null ? predicate : Combine(_predicate, predicate);
        return this;
    }

    public IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => SetOrder(keySelector, false, reset: true);

    public IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => SetOrder(keySelector, true, reset: true);

    public IQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => SetOrder(keySelector, false, reset: false);

    public IQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => SetOrder(keySelector, true, reset: false);

    public IQuery<T> Skip(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        _skip = count;
        return this;
    }

    public IQuery<T> Take(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        _take = count;
        return this;
    }

    public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
        System.Data.IDbConnection connection = lease.Connection;
        QueryParts parts = BuildQuery();
        return await connection.QueryAsync<T>(parts.Sql, parts.Parameters, cancellationToken, _transaction?.Transaction);
    }

    public async Task<IEnumerable<TResult>> SelectAsync<TResult>(
        Expression<Func<T, TResult>> projection,
        CancellationToken cancellationToken = default)
        where TResult : class, new()
    {
        ArgumentNullException.ThrowIfNull(projection);
        await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
        System.Data.IDbConnection connection = lease.Connection;
        QueryParts parts = BuildProjectionQuery(projection);
        return await connection.QueryAsync<TResult>(parts.Sql, parts.Parameters, cancellationToken, _transaction?.Transaction);
    }

    public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        Take(1);
        return (await ToListAsync(cancellationToken)).FirstOrDefault();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using ConnectionLease lease = await OpenConnectionAsync(cancellationToken);
        System.Data.IDbConnection connection = lease.Connection;
        QueryParts parts = BuildQuery(countOnly: true);
        return await connection.ExecuteScalarAsync<int>(parts.Sql, parts.Parameters, cancellationToken, _transaction?.Transaction);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default) => await CountAsync(cancellationToken) > 0;

    private IQuery<T> SetOrder<TKey>(Expression<Func<T, TKey>> keySelector, bool descending, bool reset)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        MemberExpression member = GetMemberExpression(keySelector.Body);
        if (member.Member is not PropertyInfo property)
            throw new NotSupportedException("Ordering is only supported for entity properties.");

        TableCacheItem mapping = _cache.GetItem(property);
        if (reset)
            _orderBy.Clear();

        _orderBy.Add(new OrderClause(mapping.ColumnName, descending));
        return this;
    }

    private QueryParts BuildQuery(bool countOnly = false)
    {
        DynamicParameters parameters = new();
        string whereClause = string.Empty;
        if (_predicate is not null)
        {
            ExpressionToSqlTranslator translator = new(_cache);
            string translated = translator.Translate(_predicate.Body);
            whereClause = $" WHERE {translated}";
            foreach (string name in translator.Parameters.ParameterNames)
                parameters.Add(name, translator.Parameters.GetValue(name));
        }

        string sql = countOnly
            ? $"SELECT COUNT(1) FROM {_tableName}{whereClause}"
            : $"SELECT * FROM {_tableName}{whereClause}";

        if (!countOnly && _orderBy.Count > 0)
            sql += $" ORDER BY {string.Join(", ", _orderBy.Select(order => $"{order.ColumnName}{(order.Descending ? " DESC" : " ASC")}"))}";
        if (!countOnly && (_take.HasValue || _skip.HasValue))
        {
            sql += $" LIMIT {_take?.ToString() ?? "18446744073709551615"}";
            if (_skip.HasValue)
                sql += $" OFFSET {_skip.Value}";
        }

        return new QueryParts(sql, parameters.GetParameters());
    }

    private QueryParts BuildProjectionQuery<TResult>(Expression<Func<T, TResult>> projection)
    {
        if (projection.Body is not MemberInitExpression memberInit)
            throw new NotSupportedException("Projections must initialize a result object.");

        List<string> columns = new();
        foreach (MemberBinding binding in memberInit.Bindings)
        {
            if (binding is not MemberAssignment assignment || assignment.Expression is not MemberExpression sourceMember)
                throw new NotSupportedException("Projections must map entity properties directly.");

            if (sourceMember.Member is not PropertyInfo sourceProperty)
                throw new NotSupportedException("Projections must map entity properties directly.");

            TableCacheItem mapping = _cache.GetItem(sourceProperty);
            columns.Add($"{mapping.ColumnName} AS {binding.Member.Name}");
        }

        if (columns.Count == 0)
            throw new InvalidOperationException("The projection must select at least one property.");

        string sql = $"SELECT {string.Join(", ", columns)} FROM {_tableName}";
        return new QueryParts(sql, new Dictionary<string, object?>());
    }

    private static Expression<Func<T, bool>> Combine(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "entity");
        Expression leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        Expression rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private static MemberExpression GetMemberExpression(Expression expression)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            expression = unary.Operand;
        return expression as MemberExpression
            ?? throw new NotSupportedException("Ordering requires a property selector.");
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _source ? _target : base.VisitParameter(node);
    }

    private sealed record QueryParts(string Sql, IReadOnlyDictionary<string, object?> Parameters);

    private sealed record OrderClause(string ColumnName, bool Descending);

    private Task<ConnectionLease> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            if (_transaction.Connection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("The transaction connection must remain open while the transaction is active.");

            return Task.FromResult(new ConnectionLease(_transaction.Connection, ownsConnection: false));
        }

        return OpenOwnedConnectionAsync(cancellationToken);
    }

    private async Task<ConnectionLease> OpenOwnedConnectionAsync(CancellationToken cancellationToken)
        => new(await _connectionFactory.CreateConnectionAsync(cancellationToken), ownsConnection: true);

    private sealed class ConnectionLease : IAsyncDisposable
    {
        public System.Data.IDbConnection Connection { get; }
        private readonly bool _ownsConnection;

        public ConnectionLease(System.Data.IDbConnection connection, bool ownsConnection)
        {
            Connection = connection;
            _ownsConnection = ownsConnection;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_ownsConnection)
                return;

            if (Connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                Connection.Dispose();
        }
    }
}
