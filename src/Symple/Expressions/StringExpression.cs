using System.Collections.Generic;
using System.Text;

namespace Symple.Expressions
{
    public class StringExpression : IExpression
    {
        public StringExpression(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public string Render(Dictionary<string, object> variables)
        {
            return Value;
        }

        public bool AsBool(Dictionary<string, object> variables)
        {
            return !string.IsNullOrEmpty(Value);
        }

        public override string ToString()
        {
            return new StringBuilder().Append(Value.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n")).ToString();
        }
    }
}
