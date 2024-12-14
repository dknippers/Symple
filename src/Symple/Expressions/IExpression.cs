using System.Collections.Generic;

namespace Symple.Expressions
{
    public interface IExpression
    {
        /// <summary>
        /// Renders this expression as a string.
        /// </summary>
        /// <param name="variables">Variables of the template this expression is used in.</param>
        /// <returns></returns>
        string Render(Dictionary<string, object> variables);

        /// <summary>
        /// Evaluates this expression as a boolean.
        /// </summary>
        /// <param name="variables">Variables of the template this expression is used in.</param>
        /// <returns></returns>
        bool AsBool(Dictionary<string, object> variables);
    }
}
