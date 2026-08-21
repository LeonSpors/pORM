using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using pORM.Core.Interfaces;
using pORM.Core.Models;
using pORM.Mapping.Models;

namespace pORM.Mapping.Utilities;

public class ExpressionToSqlTranslator : ExpressionVisitor
{
    private readonly ITableCache _cache;
    private readonly StringBuilder _builder;

    // Use our custom parameter container.
    public DynamicParameters Parameters { get; } = new();
    private int _paramIndex = 0;

    public ExpressionToSqlTranslator(ITableCache cache)
    {
        _cache = cache;
        _builder = new StringBuilder();
    }

    public string Translate(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        try
        {
            Visit(expression);
            string sql = _builder.ToString();
            return sql;
        }
        catch
        {
            throw;
        }
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _builder.Append('(');

        bool leftIsNull = IsNullConstant(node.Left);
        bool rightIsNull = IsNullConstant(node.Right);

        if ((leftIsNull || rightIsNull) &&
            (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual))
        {
            if (leftIsNull)
            {
                _builder.Append("NULL");
            }
            else
            {
                Visit(node.Left);
            }

            _builder.Append(' ');
            _builder.Append(node.NodeType == ExpressionType.Equal ? "IS" : "IS NOT");
            _builder.Append(' ');

            if (rightIsNull)
            {
                _builder.Append("NULL");
            }
            else
            {
                Visit(node.Right);
            }
        }
        else
        {
            Visit(node.Left);
            _builder.Append(' ');
            _builder.Append(GetSqlOperator(node.NodeType));
            _builder.Append(' ');
            Visit(node.Right);
        }

        _builder.Append(')');
        return node;
    }

    // Enhanced VisitUnary to bypass conversions between string and Guid/Guid? types.
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert)
        {
            Type sourceType = node.Operand.Type;
            Type targetType = node.Type;

            // Bypass conversion from string to Guid/Guid?
            if (sourceType == typeof(string) &&
                (targetType == typeof(Guid) || targetType == typeof(Guid?)))
            {
                return Visit(node.Operand);
            }

            // Bypass conversion from Guid/Guid? to string
            if ((sourceType == typeof(Guid) || sourceType == typeof(Guid?)) &&
                targetType == typeof(string))
            {
                return Visit(node.Operand);
            }

            // Bypass conversion from Guid to Guid? (implicit conversion by the compiler)
            if (sourceType == typeof(Guid) && targetType == typeof(Guid?))
            {
                return Visit(node.Operand);
            }
        }
        return base.VisitUnary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(string))
        {
            if (node.Method.Name == "Contains")
            {
                _builder.Append('(');
                Visit(node.Object);
                _builder.Append(" LIKE ");
                object? value = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();
                return AddParameter("%" + value + "%");
            }
            else if (node.Method.Name == "StartsWith")
            {
                _builder.Append('(');
                Visit(node.Object);
                _builder.Append(" LIKE ");
                object? value = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();
                return AddParameter(value + "%");
            }
            else if (node.Method.Name == "EndsWith")
            {
                _builder.Append('(');
                Visit(node.Object);
                _builder.Append(" LIKE ");
                object? value = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();
                return AddParameter("%" + value);
            }
        }

        throw new NotSupportedException(
            $"The method '{node.Method.Name}' is not supported in LINQ-to-SQL translation.");
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        return AddParameter(node.Value, node.Type);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // If the member belongs to a parameter (e.g., x => x.SomeProperty), output its column name.
        if (node.Expression is ParameterExpression)
        {
            PropertyInfo propertyInfo = (PropertyInfo)node.Member;
            TableCacheItem mapping = _cache.GetItem(propertyInfo);
            if (propertyInfo.PropertyType == typeof(Guid) || propertyInfo.PropertyType == typeof(Guid?))
            {
                _builder.Append("CAST(");
                _builder.Append(mapping.ColumnName);
                _builder.Append(" AS CHAR(36))");
            }
            else
            {
                _builder.Append(mapping.ColumnName);
            }
            // Return a dummy constant expression of type string to avoid type mismatches later.
            return Expression.Constant(null, typeof(string));
        }

        // For captured variables (closures), extract the value via reflection.
        if (node.Expression is ConstantExpression constantExpression)
        {
            object? container = constantExpression.Value;
            object? value = null;
            if (node.Member is FieldInfo field)
            {
                value = field.GetValue(container);
            }
            else if (node.Member is PropertyInfo prop)
            {
                value = prop.GetValue(container);
            }
            return AddParameter(value, node.Type);
        }

        // Fallback: compile and evaluate the member expression.
        object? fallbackValue = Expression.Lambda(node).Compile().DynamicInvoke();
        return AddParameter(fallbackValue, node.Type);
    }

    private Expression AddParameter(object? value, Type? expectedType = null)
    {
        string paramName = $"@p{_paramIndex++}";

        // If the expected type is Guid or Guid?, force the constant type to string.
        if (expectedType == typeof(Guid) || expectedType == typeof(Guid?))
        {
            if (value is Guid guid)
            {
                value = guid.ToString();
            }
            expectedType = typeof(string);
        }

        _builder.Append(paramName);
        Parameters.Add(paramName, value);
        return Expression.Constant(value, expectedType ?? value?.GetType() ?? typeof(object));
    }

    private string GetSqlOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Operator '{nodeType}' is not supported")
        };
    }

    private bool IsNullConstant(Expression expr)
    {
        return expr is ConstantExpression ce && ce.Value == null;
    }
}
