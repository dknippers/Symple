namespace Symple.Expressions;

public class IntegerExpression : IExpression
{
    public IntegerExpression(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public string Render(Dictionary<string, object?> variables)
    {
        return Value.ToString();
    }

    public bool AsBool(Dictionary<string, object?> variables)
    {
        return Value != 0;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
