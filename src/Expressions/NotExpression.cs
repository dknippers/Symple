using System.Text;

namespace Symple.Expressions;

public class NotExpression : IExpression
{
    public IExpression Operand { get; }

    public NotExpression(IExpression operand)
    {
        Operand = operand;
    }

    public bool AsBool(Dictionary<string, object?> variables)
    {
        return !Operand.AsBool(variables);
    }

    public string Render(Dictionary<string, object?> variables)
    {
        return AsBool(variables).ToString();
    }

    public override string ToString()
    {
        return new StringBuilder().Append('!').Append(Operand).ToString();
    }
}
