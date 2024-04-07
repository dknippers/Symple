using System.Text;

namespace Symple.Expressions;

public class ConditionalExpression : IExpression
{
    public IExpression Condition { get; }

    public IExpression IfTrue { get; }

    public IExpression? IfFalse { get; }

    public ConditionalExpression(IExpression condition, IExpression ifTrue, IExpression? ifFalse)
    {
        Condition = condition;
        IfTrue = ifTrue;
        IfFalse = ifFalse;
    }

    public bool AsBool(Dictionary<string, object?> variables)
    {
        return Condition.AsBool(variables)
            ? IfTrue.AsBool(variables)
            : IfFalse?.AsBool(variables) ?? false;
    }

    public string Render(Dictionary<string, object?> variables)
    {
        return Condition.AsBool(variables)
            ? IfTrue.Render(variables)
            : IfFalse?.Render(variables) ?? "";
    }

    public override string ToString()
    {
        var sb = new StringBuilder()
            .Append("?[").Append(Condition).Append(']')
            .Append('{').Append(IfTrue).Append('}');

        if (IfFalse is not null)
        {
            _ = sb.Append('{').Append(IfFalse).Append('}');
        }

        return sb.ToString();
    }
}
