using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Symple.Expressions
{
    public class CompositeExpression : IExpression
    {
        public IExpression[] Expressions { get; } = Array.Empty<IExpression>();

        public CompositeExpression(IExpression[] expressions)
        {
            Expressions = expressions;
        }

        public string Render(Dictionary<string, object> variables)
        {
            var sb = new StringBuilder();

            foreach (var expression in Expressions)
            {
                var value = expression.Render(variables);
                _ = sb.Append(value);
            }

            return sb.ToString();
        }

        public bool AsBool(Dictionary<string, object> variables)
        {
            if (Expressions.Length == 0)
            {
                return false;
            }

            if (Expressions.Length == 1)
            {
                return Expressions[0].AsBool(variables);
            }

            var str = Render(variables);
            return !string.IsNullOrEmpty(str);
        }

        public override string ToString()
        {
            if (Expressions.Length == 0)
            {
                return "";
            }

            if (Expressions.Length == 1)
            {
                return Expressions[0].ToString() ?? "";
            }

            return new StringBuilder()
                .Append('[')
                .Append(string.Join(", ", Expressions.Select(e => e.ToString())))
                .Append(']')
                .ToString();
        }
    }
}
