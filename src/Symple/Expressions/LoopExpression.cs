using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Symple.Expressions
{
    public class LoopExpression : IExpression
    {
        public LoopExpression(string identifier, VariableExpression collection, IExpression template)
        {
            Identifier = identifier;
            Collection = collection;
            Template = template;
        }

        public string Identifier { get; }

        public VariableExpression Collection { get; }

        public IExpression Template { get; }

        public string Render(Dictionary<string, object> variables)
        {
            if (!(Collection.GetValue(variables) is IEnumerable enumerable))
            {
                // If the Collection cannot be enumerated we render nothing.
                return "";
            }

            var sb = new StringBuilder();

            var loopVariables = new Dictionary<string, object>(variables);

            foreach (var identifier in enumerable)
            {
                loopVariables[Identifier] = identifier;
                var value = Template.Render(loopVariables);
                _ = sb.Append(value);
            }

            return sb.ToString();
        }

        public bool AsBool(Dictionary<string, object> variables)
        {
            var str = Render(variables);
            return !string.IsNullOrEmpty(str);
        }

        public override string ToString()
        {
            return new StringBuilder().Append("@[$").Append(Identifier).Append(':').Append(Collection).Append("]{").Append(Template).Append('}').ToString();
        }
    }
}
