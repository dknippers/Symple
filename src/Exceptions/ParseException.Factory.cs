using System;
using System.Text;

namespace Symple.Exceptions
{
    public partial class ParseException : Exception
    {
        /// <summary>
        /// Number of characters to include in a parsing error
        /// message, both before and after the failing character.
        /// </summary>
        private const int CONTEXT_SIZE = 8;

        public static ParseException Create(string input, int index, string details)
        {
            var exceptionMsg = GetParseExceptionMessage(input, index);

            if (!string.IsNullOrWhiteSpace(details))
            {
                exceptionMsg += "\n" + details + "\n";
            }

            return new ParseException(input, index, exceptionMsg);
        }

        private static string GetContext(string input, int index)
        {
            var from = Math.Max(0, index - CONTEXT_SIZE);
            var to = Math.Min(input.Length - 1, index + CONTEXT_SIZE);

            var errorIdx = index >= CONTEXT_SIZE ? CONTEXT_SIZE : index;

            var context = input.Substring(from, to - from + 1);

            var sb = new StringBuilder(context);

            _ = sb.Append('\n');

            var lastLF = context.LastIndexOf('\n', errorIdx >= context.Length ? context.Length - 1 : errorIdx);
            var lastLineStart = lastLF == -1 ? 0 : lastLF + 1;

            for (int i = lastLineStart; i < errorIdx; i++)
            {
                _ = sb.Append(' ');
            }

            _ = sb.Append('^');

            return sb.ToString();
        }

        private static string GetParseExceptionMessage(string input, int index)
        {
            var lineNumber = GetLineNumber(input, index);
            var context = GetContext(input, index);
            var character = index < input.Length ? $"'{input[index]}'" : "EOF";
            return $"Error parsing {character} on line {lineNumber}\n\n{context}";
        }

        private static int GetLineNumber(string input, int index)
        {
            int line = 1;

            for (int i = 0; i < index && i < input.Length; i++)
            {
                if (input[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }
    }
}
