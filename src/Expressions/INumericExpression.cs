using System.Collections.Generic;

namespace Symple.Expressions
{
    public interface INumericExpression : IExpression
    {
        decimal? AsNumber(Dictionary<string, object> variables);
    }
}
