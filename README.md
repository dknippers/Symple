# Symple

A templating engine focused on simplicity.

It has the following features:

- Variables
  - `$var` or `$[var]`
  - `$var.a.b.c` for nested objects.
- Conditionals
  - `?[condition] {if} {else}`
  - or without the else
  - `?[condition] {if}`
- Loops
  - `@[$x:$xs] {use $x here}`
  - Variable `$xs` must implement `IEnumerable`, i.e. pretty much every .NET collection class will work.
- Logical operators - operands are evaluated as `bool`.
  - `!`&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Logical NOT
  - `&&`&nbsp;&nbsp;&nbsp;Logical AND
  - `||`&nbsp;&nbsp;&nbsp;Logical OR
- Comparison operators - operands are evaluated as `decimal`
  - `<`&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Less than
  - `>`&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Greater than
  - `<=`&nbsp;&nbsp;&nbsp;Less than or equal
  - `>=`&nbsp;&nbsp;&nbsp;Greater than or equal
  - Note if any expression that is not a numeric is part of the comparision it will yield `false`.
- Equality operators - operands are evaluated as `string`.
  - `==`&nbsp;&nbsp;&nbsp;Equal
  - `!=`&nbsp;&nbsp;&nbsp;Not Equal
- Grouping in a condition to override default operator precedence
  - e.g. `?[$a && ($b || $c)]`
- Collection count
  - `?[#$items == 1]{exactly 1 item}`
    - Evaluates to the length of the `IEnumerable` variable as an integer.
    - If the variable is not an `IEnumerable` will evaluate to `""` / `false`.

## Types

The core types used in Symple are `string` for rendering, `bool` for conditionals and `decimal` for any numeric comparisons.
You can pass any .NET type as a variable to the template but keep in mind when rendering a variable Symple will call `.ToString()` on it so using anything other than a `string` as the final expression can lead to unwanted output. For example, an `int[]` variable would be rendered as `System.Int32[]` which is unlikely what you want.

## Implicit bool conversion

Any variable can be used directly in the conditional expression, i.e. `?[$var] { ... }` is valid syntax for any variable. For any type other than `bool` we will convert the variable's value to a `bool` by these rules:

- `null`: `false`
- `string` s: `s.Length > 0`
- `IEnumerable` e: `e.Any()`
- Any other value v with type T: `!v.Equals(default(T))`.
  - Any numeric type (`int` / `float` etc) is `true` when not `0`.
  - `char` is true when not `\0`.
  - Any reference type is `true` when not `null`.

## Conditional expression

The conditional expression `?[condition] {if} {else}` allows the following expressions as `condition`, where any expression other than `bool` will be implicitly converted to `bool`.

- Variables
  - `?[$x]`
  - See in "Implicit bool conversion" how variables are converted to `bool`.
- Strings
  - `?["str"]`
  - `?[$a == "str with $var"]`
  - Note unlike in the template itself it requires `"` delimiters.
  - A string evaluates to `true` unless empty (`""`).
- Numbers
  - `?[1]`
  - `?[2.5 > 2]`
  - Numbers evaluate to `true` unless equal to `0`.
- Logical expressions (`!`, `&&`, `||` and parentheses)
  - `?[$a && $b]`
  - `?[!$a]`
  - `?[!$a && ($b || $c)]`
  - Note, this evaluates operands as `bool`
- Comparison (`<`, `>`, `<=`, `>=`)
  - `?[$a > $b && $a < 2]`
  - `?[$a <= 1.5 || $a >= 4]`
  - Note, this evaluates operands as `decimal`
- Equality (`==`, `!=`)
  - `?[$a == "value"]`
  - `?[$b != "other"]`
  - Note, this evaluates operands as `string`
- Collection count operator
  - `?[#$items > 1]`
- Nested conditionals
  - `?[?[$a]{$b}{$c}]`
    - This condition evaluates `$b` if `$a` is true, otherwise `$c`.
    - Based on `$b` or `$c` the `if` or `else` branch of the original conditional is executed.
  - Possible, though potentially confusing

All the above expressions can be infinitely combined using the various operators.

## Basic usage

```csharp
var template = "$person.Name's father was $person.Father.Name.";

var variables = new Dictionary<string, object?>
{
    ["person"] = new
    {
        Name = "Stephen",
        Father = new {  Name = "Frank" }
    }
};

var output = Symple.Parser
    .Parse(template)
    .Render(variables);

// Output: "Stephen's father was Frank."
```

## Complete example

The sample below uses many Symple features such as conditionals / loops / variables /

```cs
var template = @"
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

var variables = new Dictionary<string, object?>
{
    ["planets"] = new[]
    {
        new { Name = "Earth", Moons = new[] { "Moon" } },
        new { Name = "Mars", Moons = new[] { "Phobos", "Deimos" } },
        new { Name = "Venus", Moons = new string[0] { /* Venus has no moons */ } }
    },
};

var output = Symple.Parser
    .Parse(template)
    .Render(variables);

// Output:
// <h1>Planets</h1>
// <ul>
//     <li>
//         <h2>Earth</h2>
//         <strong>Moons of Earth:</strong>
//         <ul>
//             <li>Moon</li>
//         </ul>
//     </li>
//     <li>
//         <h2>Mars</h2>
//         <strong>Moons of Mars:</strong>
//         <ul>
//             <li>Phobos</li>
//             <li>Deimos</li>
//         </ul>
//     </li>
//     <li>
//         <h2>Venus</h2>
//         Venus has no moons
//     </li>
// </ul>
```
