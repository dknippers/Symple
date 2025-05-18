using Symple.Exceptions;
using Symple.Expressions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Symple
{
    public class Parser
    {
        private readonly string _input;
        private int _index;
        private readonly int _length;

        private const char OPEN = '{';
        private const char CLOSE = '}';

        private static class SpecialChars
        {
            public static readonly char[] Default = new[] { '$', '?', '@', '#', '\\' };
            public static readonly char[] Nested = Default.Concat(new[] { CLOSE }).ToArray();
            public static readonly char[] Interpolated = new[] { '\\', '"', '$' };
            public static readonly Lazy<HashSet<char>> All = new Lazy<HashSet<char>>(() => new HashSet<char>(Default.Concat(Nested).Concat(Interpolated)));
        }

        internal Parser(string template)
        {
            _input = template;
            _length = _input.Length;
            _index = 0;
        }

        /// <summary>
        /// Parses the given <paramref name="template"/> into an <see cref="IExpression"/>.
        /// </summary>
        /// <param name="template">Template to parse</param>
        /// <returns><see cref="IExpression"/> that represents the input template.</returns>
        public static IExpression Parse(string template)
        {
            return new Parser(template).ParseTemplate();
        }

        /// <summary>
        /// Parses the given <paramref name="template"/> into an <see cref="IExpression"/>.
        /// </summary>
        /// <param name="template">Template to parse</param>
        /// <param name="expression">When succesful, the <see cref="IExpression"/> that represents the input template, otherwise <c>null</c>.</param>
        /// <returns><c>true</c> when the template was successfully parsed, otherwise <c>false</c></returns>
        public static bool TryParse(string template, out IExpression expression)
        {
            try
            {
                expression = Parse(template);
                return true;
            }
            catch
            {
                expression = null;
                return false;
            }
        }

        /// <summary>
        /// Escapes any special characters in the input string.
        /// </summary>
        /// <param name="input">Input</param>
        /// <returns>Escaped input</returns>
        public static string Escape(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var sb = new StringBuilder(input.Length * 2);

            foreach (var c in input)
            {
                if (SpecialChars.All.Value.Contains(c))
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private IExpression ParseTemplate(bool nested = false)
        {
            var expressions = new List<IExpression>();

            var terminator = nested ? CLOSE : (char?)null;
            var specialChars = nested ? SpecialChars.Nested : SpecialChars.Default;

            while (_index < _length)
            {
                var c = _input[_index];

                if (c == terminator)
                {
                    break;
                }

                char? next = CharAt(_index + 1);
                IExpression expression = ParseExpression(c, next, terminator, specialChars);
                expressions.Add(expression);
            }

            return expressions.Count == 1
                ? expressions[0] // Unbox single expressions
                : new CompositeExpression(expressions.ToArray());
        }

        private IExpression ParseExpression(char c, char? next, char? terminator, char[] specialChars)
        {
            if (IsStartOfVariable(c, next)) return ParseVariable();
            if (IsStartOfConditional(c, next)) return ParseConditional();
            if (IsStartOfLoop(c, next)) return ParseLoop();
            if (IsStartOfCount(c, next, CharAt(_index + 2))) return ParseCount();

            // Default
            return ParseString(terminator, specialChars);
        }

        private char? CharAt(int index)
        {
            return index < _length ? _input[index] : (char?)null;
        }

        private IExpression ParseInterpolatedString()
        {
            var expressions = new List<IExpression>();

            Read('"');

            while (_index < _length)
            {
                var c = _input[_index];

                if (c == '"')
                {
                    break;
                }

                var next = CharAt(_index + 1);
                IExpression expression = IsStartOfVariable(c, next)
                    ? ParseVariable() as IExpression
                    : ParseString('"', SpecialChars.Interpolated);

                expressions.Add(expression);
            }

            Read('"');

            return expressions.Count == 1
                ? expressions[0] // Unbox single expressions
                : new CompositeExpression(expressions.ToArray());
        }

        private StringExpression ParseString(char? terminator, char[] specialChars)
        {
            var sb = new StringBuilder();

            var start = _index;
            while (true)
            {
                if (_index >= _length)
                {
                    _ = sb.Append(_input.Substring(start, _length - start));
                    break;
                }

                var idx = _input.IndexOfAny(specialChars, _index);
                if (idx == -1)
                {
                    var substr = _input.Substring(start, _length - start);
                    _index = _length;

                    if (sb.Length == 0)
                    {
                        // No special chars in the string at all just return it.
                        return new StringExpression(substr);
                    }

                    _ = sb.Append(substr);
                    break;
                }

                var c = _input[idx];
                char? next = CharAt(idx + 1);

                if (c == '\\')
                {
                    // Append everything before and after backslash but not backslash itself.
                    _ = sb.Append(_input.Substring(start, idx - start)).Append(next);
                    _index = idx + 2;
                    start = _index;
                    continue;
                }

                if (c == terminator ||
                    IsStartOfConditional(c, next) ||
                    IsStartOfLoop(c, next) ||
                    IsStartOfVariable(c, next) ||
                    IsStartOfCount(c, next, CharAt(idx + 2)))
                {
                    _ = sb.Append(_input.Substring(start, idx - start));
                    _index = idx;
                    break;
                }

                // Character is not special; will be part of StringExpression.
                _index = idx + 1;
            }

            return new StringExpression(sb.ToString());
        }

        private VariableExpression ParseVariable(bool allowProperties = true)
        {
            Read('$');

            // Allow optional [ to wrap a variable, i.e. $[var].
            var wrapped = SkipChar('[');

            var name = ParseIdentifier();

            var propertyNames = new List<string>();
            while (TryRead('.'))
            {
                if (_index == _length || !IsStartOfIdentifier(_input[_index]))
                {
                    // This is not a property access operation.
                    _index--;
                    break;
                }
                propertyNames.Add(ParseIdentifier());
            }

            if (wrapped)
            {
                // If a variable is wrapped it must be closed.
                Read(']');
            }

            return new VariableExpression(name, propertyNames?.ToArray());
        }

        private IExpression ParseNumeric()
        {
            var start = _index;

            TryRead('-'); // Leading - for negative numbers
            var isDecimal = TryRead('.'); // Leading . for decimal numbers

            ReadDigits();

            if (!isDecimal && TryRead('.'))
            {
                // Read decimal part
                ReadDigits();
            }

            var value = decimal.Parse(_input.Substring(start, _index - start), CultureInfo.InvariantCulture);
            return new NumericExpression(value);
        }

        private string ParseIdentifier()
        {
            var start = _index;

            while (_index < _length)
            {
                var c = _input[_index];

                if (IsStartOfIdentifier(c) || (_index > start && char.IsDigit(c)))
                {
                    _index++;
                    continue;
                }

                break;
            }

            if (_index == start)
            {
                throw ParseException("Expected identifier");
            }

            return _input.Substring(start, _index - start);
        }

        private static bool IsStartOfIdentifier(char? c)
        {
            return c != null && (char.IsLetter(c.Value) || c == '_');
        }

        private static bool IsStartOfVariable(char c, char? next)
        {
            return c == '$' && (next == '[' || IsStartOfIdentifier(next));
        }

        private static bool IsStartOfConditional(char c, char? next)
        {
            return c == '?' && next == '[';
        }

        private static bool IsStartOfLoop(char c, char? next)
        {
            return c == '@' && next == '[';
        }

        private static bool IsStartOfCount(char c, char? next, char? nextNext)
        {
            return next != null && c == '#' && IsStartOfVariable(next.Value, nextNext);
        }

        private LoopExpression ParseLoop()
        {
            Read('@');
            Read('[', IgnoreWhiteSpace.After);

            Read('$');
            var identifier = ParseIdentifier();
            Read(':', IgnoreWhiteSpace.Around);
            var collection = ParseVariable();
            Read(']', IgnoreWhiteSpace.Around);
            Read(OPEN);
            var template = ParseTemplate(true);
            Read(CLOSE);

            return new LoopExpression(identifier, collection, template);
        }

        private CountExpression ParseCount()
        {
            Read('#');
            var collection = ParseVariable();
            return new CountExpression(collection);
        }

        private ConditionalExpression ParseConditional()
        {
            var condition = ParseCondition();
            var ifTrue = ParseThen();
            var ifFalse = ParseElse();
            return new ConditionalExpression(condition, ifTrue, ifFalse);
        }

        private IExpression ParseCondition()
        {
            Read('?');
            Read('[', IgnoreWhiteSpace.After);
            var boolean = ParseBoolean();
            Read(']', IgnoreWhiteSpace.Around);
            return boolean;
        }

        private IExpression ParseThen()
        {
            Read(OPEN);
            var expression = ParseTemplate(true);
            Read(CLOSE);

            return expression;
        }

        private IExpression ParseElse()
        {
            if (!TryRead(OPEN, IgnoreWhiteSpace.Before))
            {
                // There is no else next in the input.
                return null;
            }

            var expression = ParseTemplate(true);
            Read(CLOSE);

            return expression;
        }

        private IExpression ParseBoolean()
        {
            if (_index >= _length)
            {
                throw ParseException("Expected boolean expression");
            }

            return ParseOr();
        }

        private IExpression ParseOr()
        {
            return ParseBinary(ParseAnd, BinaryOperator.Or);
        }

        private IExpression ParseAnd()
        {
            return ParseBinary(ParseEquality, BinaryOperator.And);
        }

        private IExpression ParseEquality()
        {
            return ParseBinary(ParseComparison, BinaryOperator.Equal, BinaryOperator.NotEqual);
        }

        private IExpression ParseComparison()
        {
            return ParseBinary(
                ParsePrimary,
                BinaryOperator.LessThan, BinaryOperator.GreaterThan, BinaryOperator.LessThanOrEqual, BinaryOperator.GreaterThanOrEqual);
        }

        private IExpression ParseBinary(Func<IExpression> next, params BinaryOperator[] operators)
        {
            var lhs = next();

            while (true)
            {
                if (!TryReadBinaryOperator(out var op, operators))
                {
                    return lhs;
                }

                var rhs = next();
                lhs = new BinaryExpression(op.Value, lhs, rhs);
            }
        }

        private IExpression ParseNot()
        {
            if (!TryRead('!', IgnoreWhiteSpace.After))
            {
                return ParsePrimary();
            }

            var rhs = ParseNot();
            if (rhs is NotExpression not)
            {
                // Simplify !!X -> X.
                return not.Operand;
            }

            return new NotExpression(rhs);
        }

        private IExpression ParsePrimary()
        {
            var c = _input[_index];
            switch (c)
            {
                case '!': return ParseNot();
                case '(': return ParseGroup();
                case '$': return ParseVariable();
                case '?': return ParseConditional();
                case '#': return ParseCount();
                case '"': return ParseInterpolatedString();

                default:
                    if (char.IsDigit(c) || c == '-' || c == '.') return ParseNumeric();

                    throw ParseException("Expected '!', '(', '$', '?', '#', '\"', '-' or a digit");
            }
        }

        private IExpression ParseGroup()
        {
            Read('(', IgnoreWhiteSpace.After);
            var expression = ParseBoolean();
            Read(')', IgnoreWhiteSpace.Before);

            // A group is only for precedence, the final result is
            // simply the contained expression.
            return expression;
        }

        private bool TryReadBinaryOperator(out BinaryOperator? @operator, BinaryOperator[] allowedOperators = null)
        {
            @operator = null;

            if (_index >= _length)
            {
                return false;
            }

            int checkpoint = _index;

            SkipWhiteSpace();

            if (_index >= _length)
            {
                return false;
            }

            var c = _input[_index];
            switch (c)
            {
                case '&':
                case '|':
                case '=':
                case '!':
                case '<':
                case '>':

                    _index++;

                    var next = CharAt(_index);

                    if ((c == '<' || c == '>') && next != '=')
                    {
                        @operator = c == '<' ? BinaryOperator.LessThan : BinaryOperator.GreaterThan;
                        goto done;
                    }

                    var expect = c == '!' || c == '>' || c == '<' ? '=' : c;

                    if (_index >= _length || next != expect)
                    {
                        throw ParseException($"Expected '{expect}'");
                    }

                    switch (c)
                    {
                        case '&': @operator = BinaryOperator.And; break;
                        case '|': @operator = BinaryOperator.Or; break;
                        case '=': @operator = BinaryOperator.Equal; break;
                        case '!': @operator = BinaryOperator.NotEqual; break;
                        case '<': @operator = BinaryOperator.LessThanOrEqual; break;
                        case '>': @operator = BinaryOperator.GreaterThanOrEqual; break;
                        default: throw new NotImplementedException($"Unknown binary operator: '{c}{_input[_index]}'");
                    }

                    _index++;

                done:
                    if (allowedOperators != null && !allowedOperators.Contains(@operator.Value))
                    {
                        goto rewind;
                    }

                    SkipWhiteSpace();
                    return true;

                default:
                    goto rewind;
            }

        rewind:
            _index = checkpoint;
            return false;
        }

        private void Read(char target, IgnoreWhiteSpace whiteSpace = IgnoreWhiteSpace.None)
        {
            if (whiteSpace.HasFlag(IgnoreWhiteSpace.Before))
            {
                SkipWhiteSpace();
            }

            if (_index >= _length)
            {
                throw ParseException($"Expected '{target}'");
            }

            var c = _input[_index];
            if (c != target)
            {
                throw ParseException($"Unexpected character '{c}', expected '{target}'");
            }

            _index++;

            if (whiteSpace.HasFlag(IgnoreWhiteSpace.After))
            {
                SkipWhiteSpace();
            }
        }

        private bool TryRead(char target, IgnoreWhiteSpace whiteSpace = IgnoreWhiteSpace.None)
        {
            var checkpoint = _index;

            if (whiteSpace.HasFlag(IgnoreWhiteSpace.Before))
            {
                SkipWhiteSpace();
            }

            if (_index < _length && _input[_index++] == target)
            {
                if (whiteSpace.HasFlag(IgnoreWhiteSpace.After))
                {
                    SkipWhiteSpace();
                }

                // OK
                return true;
            }

            // Failed: rewind
            _index = checkpoint;
            return false;
        }

        private void ReadDigits()
        {
            var start = _index;

            while (_index < _length && char.IsDigit(_input[_index]))
            {
                _index++;
            }

            if (_index == start)
            {
                throw ParseException("Expected a digit");
            }
        }

        private void SkipWhiteSpace()
        {
            while (_index < _length && char.IsWhiteSpace(_input[_index]))
            {
                _index++;
            }
        }

        private bool SkipChar(char c)
        {
            if (_index >= _length || _input[_index] != c)
            {
                return false;
            }

            _index++;
            return true;
        }

        private ParseException ParseException(string details = null)
            => Exceptions.ParseException.Create(_input, _index, details);

        [Flags]
        private enum IgnoreWhiteSpace
        {
            None = 0,
            Before = 1,
            After = 2,
            Around = Before | After
        }
    }
}
