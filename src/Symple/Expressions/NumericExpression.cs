using System.Collections.Generic;

namespace Symple.Expressions
{
    public class NumericExpression : INumericExpression
    {
        public NumericExpression(decimal value)
        {
            Value = value;
        }

        public decimal Value { get; }

        public string Render(Dictionary<string, object> variables)
        {
            return Value.ToString();
        }

        public bool AsBool(Dictionary<string, object> variables)
        {
            return Value != 0;
        }

        public decimal? AsNumber(Dictionary<string, object> variables)
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
