using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Symple.Exceptions;
using Symple.Expressions;

namespace Symple;

public class Parser
{
    private readonly string _input;
    private int _index;
    private readonly int _length;

    private const char OPEN = '{';
    private const char CLOSE = '}';

    internal Parser(string template)
    {
        _input = template;
        _length = _input.Length;
        _index = 0;
    }

    /// <summary>
    /// Parses the given <paramref name="template"/> into an <see cref="IExpression"/> that can be rendered to a string.
    /// </summary>
    /// <param name="template">Template to parse</param>
    /// <returns><see cref="IExpression"/> that represents the input template.</returns>
    public static IExpression Parse(string template)
    {
        return new Parser(template).ParseTemplate();
    }

    private IExpression ParseTemplate(bool nested = false)
    {
        var expressions = new List<IExpression>();

        var terminator = nested ? CLOSE : (char?)null;
        var specialChars = nested ? SpecialChars.Nested : SpecialChars.Default;

        while (_index < _length)
        {
            var c = _input[_index];

            if (nested && c == CLOSE)
            {
                break;
            }

            char? next = CharAt(_index + 1);
            IExpression expression = c switch
            {
                char ch when IsStartOfVariable(ch, next) => ParseVariable(),
                char ch when IsStartOfConditional(ch, next) => ParseConditional(),
                char ch when IsStartOfLoop(ch, next) => ParseLoop(),
                char ch when IsStartOfCount(ch, next, CharAt(_index + 2)) => ParseCount(),
                _ => ParseString(terminator, specialChars),
            };

            expressions.Add(expression);
        }

        return expressions.Count == 1
            ? expressions[0] // Unbox single expressions
            : new CompositeExpression(expressions.ToArray());
    }

    private char? CharAt(int index)
    {
        return index < _length ? _input[index] : null;
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

            IExpression expression = c switch
            {
                '$' => ParseVariable(),
                _ => ParseString('"', SpecialChars.Interpolated),
            };

            expressions.Add(expression);
        }

        Read('"');

        return expressions.Count == 1
            ? expressions[0] // Unbox single expressions
            : new CompositeExpression(expressions.ToArray());
    }

    private static class SpecialChars
    {
        public static readonly char[] Default = new[] { '$', '?', '@', '#', '\\' };
        public static readonly char[] Nested = Default.Concat(new[] { CLOSE }).ToArray();
        public static readonly char[] Interpolated = Default.Concat(new[] { '"' }).ToArray();
    }

    private StringExpression ParseString(char? terminator, char[] specialChars)
    {
        var sb = new StringBuilder();

        var start = _index;
        while (true)
        {
            if (_index >= _length)
            {
                _ = sb.Append(_input[start.._length]);
                break;
            }

            var idx = _input.IndexOfAny(specialChars, _index);
            if (idx == -1)
            {
                var substr = _input[start.._length];
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

            if (c == '\\')
            {
                _ = sb.Append(_input[start..idx]);

                if (idx + 1 >= _length)
                {
                    _index = idx + 1;
                    throw ParseException("Expected escaped character");
                }

                // Append escaped char
                _ = sb.Append(_input[idx + 1]);

                _index = idx + 2;
                start = _index;
                continue;
            }

            char? next = CharAt(idx + 1);
            if (c == terminator ||
                IsStartOfConditional(c, next) ||
                IsStartOfLoop(c, next) ||
                IsStartOfVariable(c, next) ||
                IsStartOfCount(c, next, CharAt(idx + 2)))
            {
                _ = sb.Append(_input[start..idx]);
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

        List<string>? propertyNames = null;
        if (allowProperties)
        {
            propertyNames = new List<string>();
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
        }

        if (wrapped)
        {
            // If a variable is wrapped it must be closed.
            Read(']');
        }

        return new VariableExpression(name, propertyNames?.ToArray());
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

        return start == _index
            ? throw ParseException("Expecting identifier")
            : _input[start.._index];
    }

    private static bool IsStartOfIdentifier(char? c)
    {
        return c is not null && (char.IsLetter(c.Value) || c == '_');
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
        return next is not null && c == '#' && IsStartOfVariable(next.Value, nextNext);
    }

    private LoopExpression ParseLoop()
    {
        Read('@');
        Read('[', IgnoreWhiteSpace.After);
        var identifier = ParseVariable(false).Name;
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
        Read("?[", IgnoreWhiteSpace.After);
        var boolean = ParseBoolean();
        Read("]", IgnoreWhiteSpace.Around);
        return boolean;
    }

    private IExpression ParseThen()
    {
        Read(OPEN);
        var expression = ParseTemplate(true);
        Read(CLOSE);

        return expression;
    }

    private IExpression? ParseElse()
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
        return ParseBinary(ParsePrimary, BinaryOperator.Equal, BinaryOperator.NotEqual);
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

        return c switch
        {
            '!' => ParseNot(),
            '(' => ParseGroup(),
            '$' => ParseVariable(),
            '?' => ParseConditional(),
            '#' => ParseCount(),
            '"' => ParseInterpolatedString(),
            _ => throw ParseException("Expected '!', '(', '$', '?', '#' or '\"'"),
        };
    }

    private bool TryReadBinaryOperator([NotNullWhen(true)] out BinaryOperator? @operator, BinaryOperator[]? allowedOperators = null)
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

                _index++;

                var next = c switch
                {
                    '!' => '=',
                    _ => c,
                };

                if (_index >= _length || _input[_index] != next)
                {
                    throw ParseException($"Expected '{next}'");
                }

                @operator = c switch
                {
                    '&' => BinaryOperator.And,
                    '|' => BinaryOperator.Or,
                    '=' => BinaryOperator.Equal,
                    '!' => BinaryOperator.NotEqual,
                    _ => throw new NotImplementedException($"Unknown binary operator: '{c}{_input[_index]}'"),
                };

                if (allowedOperators is not null && !allowedOperators.Contains(@operator.Value))
                {
                    goto rewind;
                }

                _index++;

                SkipWhiteSpace();
                return true;

            default:
                goto rewind;
        }

    rewind:
        _index = checkpoint;
        return false;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    private void Read(string str, IgnoreWhiteSpace whiteSpace = IgnoreWhiteSpace.None)
    {
        if (whiteSpace.HasFlag(IgnoreWhiteSpace.Before))
        {
            SkipWhiteSpace();
        }

        var len = str.Length;
        if (_index > _length - len)
        {
            throw ParseException($"Expected \"{str}\"");
        }

        var next = _input[_index..(_index + len)];
        if (next != str)
        {
            throw ParseException($"Unexpected string \"{next}\", expected \"{str}\"");
        }

        _index += len;

        if (whiteSpace.HasFlag(IgnoreWhiteSpace.After))
        {
            SkipWhiteSpace();
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

    private ParseException ParseException(string? details = null)
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
