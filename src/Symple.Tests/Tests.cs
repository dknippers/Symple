using Symple.Exceptions;

namespace Symple.Tests;

public class Tests
{
    [Theory]
    [InlineData("Hello, World!")]
    [InlineData("Is this item $2.99? Yes.")]
    [InlineData("@?$# -> OK")]
    public void Should_Render_Template_Without_Special_Characters_Unchanged(string input)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(input, output);
    }

    [Fact]
    public void Should_Support_Simple_Variables()
    {
        const string INPUT = @"x = $x, y = $y";
        const string EXPECTED = @"x = 1, y = 2";

        var variables = new Dictionary<string, object>
        {
            ["x"] = 1,
            ["y"] = 2
        };

        var output = Parser.Parse(INPUT).Render(variables);

        Assert.Equal(EXPECTED, output);
    }

    [Fact]
    public void Should_Support_Simple_If_Else_With_Variables()
    {
        const string INPUT = @"?[$condition]{$x}{$y} and ?[!$condition]{$x}{$y}";
        const string EXPECTED = @"1 and 0";

        var variables = new Dictionary<string, object>
        {
            ["condition"] = true,
            ["x"] = 1,
            ["y"] = 0,
        };

        var output = Parser.Parse(INPUT).Render(variables);

        Assert.Equal(EXPECTED, output);
    }

    [Fact]
    public void Should_Support_Nested_If_Else_With_Variables()
    {
        const string INPUT = @"?[$condition]{?[$x == ""hi""]{$y}{$x}}{0} and ?[!$condition]{1}{0}";
        const string EXPECTED = @"hello and 0";

        var variables = new Dictionary<string, object>
        {
            ["condition"] = true,
            ["x"] = "hi",
            ["y"] = "hello"
        };

        var output = Parser.Parse(INPUT).Render(variables);

        Assert.Equal(EXPECTED, output);
    }

    [Fact]
    public void Should_Support_Simple_Loop()
    {
        const string INPUT = @"@[$item : $items]{$item,}";
        const string EXPECTED = @"1,2,3,4,5,6,7,8,9,10,";

        var variables = new Dictionary<string, object>
        {
            ["items"] = Enumerable.Range(1, 10),
        };

        var output = Parser.Parse(INPUT).Render(variables);

        Assert.Equal(EXPECTED, output);
    }

    [Theory]
    [InlineData("?[1>0]{1}{0}", "1")]
    [InlineData("?[1>1]{1}{0}", "0")]
    [InlineData("?[0>1]{1}{0}", "0")]
    public void Should_Support_GreaterThan(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[1>=0]{1}{0}", "1")]
    [InlineData("?[1>=1]{1}{0}", "1")]
    [InlineData("?[0>=1]{1}{0}", "0")]
    public void Should_Support_GreaterThanOrEqual(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[1<0]{1}{0}", "0")]
    [InlineData("?[1<1]{1}{0}", "0")]
    [InlineData("?[0<1]{1}{0}", "1")]
    public void Should_Support_LessThan(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[1<=0]{1}{0}", "0")]
    [InlineData("?[1<=1]{1}{0}", "1")]
    [InlineData("?[0<=1]{1}{0}", "1")]
    public void Should_Support_LessThanOrEqual(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[1==0]{1}{0}", "0")]
    [InlineData("?[1==1]{1}{0}", "1")]
    public void Should_Support_Equal(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[1!=0]{1}{0}", "1")]
    [InlineData("?[1!=1]{1}{0}", "0")]
    public void Should_Support_NotEqual(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[1&&1]{1}{0}", "1")]
    [InlineData("?[1&&0]{1}{0}", "0")]
    public void Should_Support_And(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[1||1]{1}{0}", "1")]
    [InlineData("?[1||0]{1}{0}", "1")]
    public void Should_Support_Or(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("?[0&&0||1]{1}{0}", "1")]
    [InlineData("?[1||0&&0]{1}{0}", "1")]
    [InlineData("?[(1||0)&&0]{1}{0}", "0")]
    public void Should_Support_Operator_Precedence(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Theory]
    [InlineData("\\$", "$")]
    [InlineData("\\a", "a")]
    [InlineData("\\\\", "\\")]
    [InlineData("abc\\$x\\y", "abc$xy")]
    public void Should_Escape_Using_Backslash(string input, string expected)
    {
        var output = Parser.Parse(input).Render([]);
        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_Throw_On_Lone_Backslash()
    {
        var ex = Assert.Throws<ParseException>(() => Parser.Parse("\\").Render([]));
        Assert.Contains("Expected escaped character", ex.Message);
    }

    [Theory]
    [InlineData("?[$x == \"@[]Alph$obj.a?[#[$\"]{1}{0}", "1")]
    [InlineData("?[\"$\" == $dollar]{1}{0}", "1")]
    public void Should_Support_InterpolatedString(string input, string expected)
    {
        var variables = new Dictionary<string, object?>
        {
            ["x"] = "@[]Alpha?[#[$",
            ["obj"] = new
            {
                a = "a"
            },
            ["dollar"] = "$"
        };

        var output = Parser.Parse(input).Render(variables);
        Assert.Equal(expected, output);
    }
}
