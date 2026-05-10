// Shamelessly copied from https://stackoverflow.com/a/43498938
// Modified slightly.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ClassicUO.Common;

public class Accessor<T>
{
    private readonly Action<T> _setter;
    private readonly Func<T> _getter;

    public Accessor(Expression<Func<T>> expr)
    {
        ArgumentNullException.ThrowIfNull(expr);

        var memberExpression = (MemberExpression)expr.Body;
        Expression instanceExpression = memberExpression.Expression;
        ParameterExpression parameter = Expression.Parameter(typeof(T));

        switch (memberExpression.Member)
        {
            case PropertyInfo propertyInfo:
                MethodInfo setMethod = propertyInfo.GetSetMethod() ?? throw new InvalidOperationException($"Property '{propertyInfo.Name}' does not have a setter.");
                MethodInfo getMethod = propertyInfo.GetGetMethod() ?? throw new InvalidOperationException($"Property '{propertyInfo.Name}' does not have a setter.");

                _setter = Expression.Lambda<Action<T>>(Expression.Call(instanceExpression, setMethod, parameter), parameter).Compile();
                _getter = Expression.Lambda<Func<T>>(Expression.Call(instanceExpression, getMethod)).Compile();
                break;
            case FieldInfo fieldInfo:
                _setter = Expression.Lambda<Action<T>>(Expression.Assign(memberExpression, parameter), parameter).Compile();
                _getter = Expression.Lambda<Func<T>>(Expression.Field(instanceExpression,fieldInfo)).Compile();
                break;
        }

    }

    public void Set(T value) => _setter(value);

    public T Get() => _getter();
}
