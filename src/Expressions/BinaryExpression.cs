using System.Text;

namespace Symple.Expressions;

public class BinaryExpression : IExpression
{
    public BinaryOperator Operator { get; }

    public IExpression Left { get; }

    public IExpression Right { get; }

    public BinaryExpression(BinaryOperator @operator, IExpression left, IExpression right)
    {
        Operator = @operator;
        Left = left;
        Right = right;
    }

    public bool AsBool(Dictionary<string, object?> variables)
    {
        return Operator switch
        {
            BinaryOperator.And => Left.AsBool(variables) && Right.AsBool(variables),
            BinaryOperator.Or => Left.AsBool(variables) || Right.AsBool(variables),

            BinaryOperator.Equal => Left.Render(variables) == Right.Render(variables),
            BinaryOperator.NotEqual => Left.Render(variables) != Right.Render(variables),

            _ => throw new NotSupportedException($"Unsupported binary operator: {Operator}"),
        };
    }

    public string Render(Dictionary<string, object?> variables)
    {
        return AsBool(variables).ToString();
    }

    public override string ToString()
    {
        var op = Operator switch
        {
            BinaryOperator.And => "&&",
            BinaryOperator.Or => "||",
            BinaryOperator.Equal => "==",
            BinaryOperator.NotEqual => "!=",
            _ => throw new NotImplementedException($"Unknown binary operator {Operator}"),
        };

        return new StringBuilder().Append('(').Append(Left).Append(' ').Append(op).Append(' ').Append(Right).Append(')').ToString();
    }
}

public enum BinaryOperator
{
    Or,
    And,
    Equal,
    NotEqual,
}
