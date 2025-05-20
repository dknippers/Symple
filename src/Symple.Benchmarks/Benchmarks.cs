using BenchmarkDotNet.Attributes;
using Symple.Expressions;

namespace Symple.Benchmarks;

[MemoryDiagnoser]
public class Benchmarks
{
    private string _template = null!;
    private Dictionary<string, object> _variables = null!;
    private IExpression _parsed = null!;

    [GlobalSetup]
    public void Setup()
    {
        _template = @"
<h1>Planets</h1>
<ul>@[$planet:$planets] {
    <li>
        <h2>$planet.Name</h2>?[$planet.Moons] {
        <strong>$planet.Name has #$planet.Moons moon?[#$planet.Moons!=1]{s}</strong>
        <ul>@[$moon:$planet.Moons] {
            <li>$moon</li>}
        </ul>} {
        <strong>$planet.Name has no moons</strong>}
    </li>}
</ul>";

        _variables = new Dictionary<string, object>
        {
            ["planets"] = new[]
            {
                new { Name = "Earth", Moons = new[] { "Moon" } },
                new { Name = "Mars", Moons = new[] { "Phobos", "Deimos" } },
                new { Name = "Venus", Moons = Array.Empty<string>() }
            }
        };

        _parsed = Parser.Parse(_template);
    }

    [Benchmark]
    public IExpression Parse()
    {
        return Parser.Parse(_template);
    }

    [Benchmark]
    public string Render()
    {
        return _parsed.Render(_variables);
    }
}
