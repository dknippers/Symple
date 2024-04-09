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
        switch (Operator)
        {
            case BinaryOperator.And: return Left.AsBool(variables) && Right.AsBool(variables);
            case BinaryOperator.Or: return Left.AsBool(variables) || Right.AsBool(variables);

            case BinaryOperator.Equal: return Left.Render(variables) == Right.Render(variables);
            case BinaryOperator.NotEqual: return Left.Render(variables) != Right.Render(variables);

            case BinaryOperator.LessThan:
            case BinaryOperator.GreaterThan:
            case BinaryOperator.LessThanOrEqual:
            case BinaryOperator.GreaterThanOrEqual:
                if ((Left as INumericExpression)?.AsNumber(variables) is not decimal lhs ||
                    (Right as INumericExpression)?.AsNumber(variables) is not decimal rhs)
                {
                    // Instead of a runtime exception a comparison with invalid operands will just return false.
                    return false;
                }

                if (Operator == BinaryOperator.LessThan) return lhs < rhs;
                if (Operator == BinaryOperator.GreaterThan) return lhs > rhs;
                if (Operator == BinaryOperator.LessThanOrEqual) return lhs <= rhs;
                if (Operator == BinaryOperator.GreaterThanOrEqual) return lhs >= rhs;

                break;
        }

        throw new NotSupportedException($"Unsupported binary operator: {Operator}");
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
            BinaryOperator.LessThan => "<",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.LessThanOrEqual => "<=",
            BinaryOperator.GreaterThanOrEqual => ">=",
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
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,
}
