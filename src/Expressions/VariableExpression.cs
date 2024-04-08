using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

namespace Symple.Expressions;

public class VariableExpression : IExpression
{
    public VariableExpression(string name, string[]? propertyNames)
    {
        Name = name;
        PropertyNames = propertyNames;
    }

    public string Name { get; }

    public string[]? PropertyNames { get; }

    public object? GetValue(Dictionary<string, object?> variables)
    {
        var obj = variables.TryGetValue(Name, out var v) ? v : null;
        if (obj is null || !(PropertyNames?.Length > 0))
        {
            return obj;
        }

        foreach (var propertyName in PropertyNames)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                break;
            }

            obj = GetPropertyValue(obj, propertyName);

            if (obj is null)
            {
                break;
            }
        }

        return obj;
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj is IDictionary dict)
        {
            return dict[propertyName];
        }

        if (obj is JsonNode json)
        {
            return json[propertyName];
        }

        // Reflection
        var type = obj.GetType();

        if (type.GetProperty(propertyName) is PropertyInfo pi)
        {
            return pi.GetValue(obj);
        }

        if (type.GetField(propertyName) is FieldInfo fi)
        {
            return fi.GetValue(obj);
        }

        // Property not found
        return null;
    }

    public string Render(Dictionary<string, object?> variables)
    {
        var value = GetValue(variables);
        return value?.ToString() ?? "";
    }

    public bool AsBool(Dictionary<string, object?> variables)
    {
        var value = GetValue(variables);

        if (value is null)
        {
            return false;
        }

        return value switch
        {
            IEnumerable e => e.Cast<object?>().Any(),
            _ => !value.Equals(GetDefaultValue(value.GetType())),
        };

        static object? GetDefaultValue(Type t)
        {
            return t.IsValueType
                ? Activator.CreateInstance(t)
                : null;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder().Append('$').Append(Name);

        if (PropertyNames?.Length > 0)
        {
            _ = sb.Append('.').Append(string.Join('.', PropertyNames));
        }

        return sb.ToString();
    }
}
