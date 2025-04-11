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
        try
        {
            Console.WriteLine("[DEBUG] Starting translation...");
            Visit(expression);
            string sql = _builder.ToString();
            Console.WriteLine("[DEBUG] Generated SQL: " + sql);
            Console.WriteLine("[DEBUG] Parameters:");
            foreach (var name in Parameters.ParameterNames)
            {
                Console.WriteLine($"  {name}: {Parameters.Get<object>(name)}");
            }
            return sql;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Console.WriteLine($"[DEBUG] VisitBinary: {node.NodeType}");
        _builder.Append('(');

        bool leftIsNull = IsNullConstant(node.Left);
        bool rightIsNull = IsNullConstant(node.Right);

        if ((leftIsNull || rightIsNull) &&
            (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual))
        {
            Console.WriteLine("[DEBUG] Detected null constant in binary expression.");
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
        Console.WriteLine($"[DEBUG] VisitUnary: {node.NodeType}, From {node.Operand.Type} To {node.Type}");
        if (node.NodeType == ExpressionType.Convert)
        {
            Type sourceType = node.Operand.Type;
            Type targetType = node.Type;

            // Bypass conversion from string to Guid/Guid?
            if (sourceType == typeof(string) &&
                (targetType == typeof(Guid) || targetType == typeof(Guid?)))
            {
                Console.WriteLine("[DEBUG] Bypassing conversion from string to Guid/Guid?");
                return Visit(node.Operand);
            }

            // Bypass conversion from Guid/Guid? to string
            if ((sourceType == typeof(Guid) || sourceType == typeof(Guid?)) &&
                targetType == typeof(string))
            {
                Console.WriteLine("[DEBUG] Bypassing conversion from Guid/Guid? to string");
                return Visit(node.Operand);
            }

            // Bypass conversion from Guid to Guid? (implicit conversion by the compiler)
            if (sourceType == typeof(Guid) && targetType == typeof(Guid?))
            {
                Console.WriteLine("[DEBUG] Bypassing conversion from Guid to Guid?");
                return Visit(node.Operand);
            }
        }
        return base.VisitUnary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Console.WriteLine($"[DEBUG] VisitMethodCall: {node.Method.Name}");
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
        Console.WriteLine($"[DEBUG] VisitConstant: {node.Value} (Type: {node.Type})");
        return AddParameter(node.Value, node.Type);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        Console.WriteLine($"[DEBUG] VisitMember: {node.Member.Name} (Type: {node.Type})");
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
                Console.WriteLine($"[DEBUG] Output CAST for column: {mapping.ColumnName}");
            }
            else
            {
                _builder.Append(mapping.ColumnName);
                Console.WriteLine($"[DEBUG] Output column: {mapping.ColumnName}");
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
            Console.WriteLine($"[DEBUG] Captured variable {node.Member.Name} value: {value}");
            return AddParameter(value, node.Type);
        }

        // Fallback: compile and evaluate the member expression.
        object? fallbackValue = Expression.Lambda(node).Compile().DynamicInvoke();
        Console.WriteLine($"[DEBUG] Fallback member {node.Member.Name} evaluation: {fallbackValue}");
        return AddParameter(fallbackValue, node.Type);
    }

    private Expression AddParameter(object? value, Type? expectedType = null)
    {
        string paramName = $"@p{_paramIndex++}";
        Console.WriteLine($"[DEBUG] AddParameter: Value = {value}, ExpectedType = {expectedType}");

        // If the expected type is Guid or Guid?, force the constant type to string.
        if (expectedType == typeof(Guid) || expectedType == typeof(Guid?))
        {
            if (value is Guid guid)
            {
                value = guid.ToString();
                Console.WriteLine($"[DEBUG] Converted Guid to string: {value}");
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