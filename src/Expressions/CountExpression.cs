using System.Collections;
using System.Globalization;
using System.Text;

namespace Symple.Expressions;

public class CountExpression : IExpression
{
    public CountExpression(VariableExpression collection)
    {
        Collection = collection;
    }

    public VariableExpression Collection { get; }

    public int? GetCount(Dictionary<string, object?> variables)
    {
        if (Collection.GetValue(variables) is not IEnumerable enumerable)
        {
            return null;
        }

        return enumerable.Cast<object?>().Count();
    }

    public string Render(Dictionary<string, object?> variables)
    {
        return GetCount(variables)?.ToString(CultureInfo.InvariantCulture) ?? "";
    }

    public bool AsBool(Dictionary<string, object?> variables)
    {
        return GetCount(variables) is int count && count != 0;
    }

    public override string ToString()
    {
        return new StringBuilder().Append('#').Append(Collection).ToString();
    }
}
