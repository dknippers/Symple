# Symple

A templating engine focused on simplicity.

It has the following features:

- Variables
  - `$var` or `$[var]`
  - `$var.a.b.c` for nested objects.
- Conditionals
  - `?[ condition ] { if } { else }`
  - or without the else
  - `?[ condition ] { if }`
- Loops
  - `@[ $x : $xs ] { use $x here }`
  - Variable `$xs` must implement `IEnumerable`, i.e. pretty much every .NET collection class will work.
- Logical operators - operands are evaluated as `bool`.
  - `!`&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Logical NOT
  - `&&`&nbsp;&nbsp;&nbsp;Logical AND
  - `||`&nbsp;&nbsp;&nbsp;Logical OR
- Comparison operators - operands are evaluated as `string`.
  - `==`&nbsp;&nbsp;&nbsp;Equal
  - `!=`&nbsp;&nbsp;&nbsp;Not Equal
- Grouping in a condition to override default operator precedence
  - e.g. `?[ $a && ($b || $c) ]`

## Types

You can pass any .NET type as a variable to the template but keep in mind when rendering a variable Symple will call `.ToString()` on it so using anything other than a `string` as the final expression can lead to unwanted output. For example, an `int[]` variable would be rendered as `System.Int32[]` which is unlikely what you want.

## Implicit bool conversion

Any variable can be used directly in the conditional expression, i.e. `?[ $var ] { ... }` is valid syntax for any variable. For any type other than `bool` we will convert the variable's value to a `bool` by these rules:

- `null`: `false`
- `string` s: `s.Length > 0`
- `IEnumerable` e: `e.Any()`
- Any other value v with type T: `!v.Equals(default(T))`.
  - Any numeric type (`int` / `float` etc) is `true` when not `0`.
  - `char` is true when not `\0`.
  - Any reference type is `true` when not `null`.

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

The sample below uses all available functionality of Symple. A few things to note:

- You can use collections like `$planet.Moons` in a conditional `?[ $planet.Moons ]` which evaluates to `true` if the collection has at least 1 element.
- Similarly, a `string` can also be used in a condition and will evaluate to `true` if it has a length of at least 1.

```cs
var template = @"
<h1>Planets</h1>
<ul>@[ $planet : $planets ] {
    <li>
        <h2>$planet.Name</h2>?[ $planet.Moons ] {
        <strong>Moons of $planet.Name:</strong>
        <ul>@[ $moon : $planet.Moons ] {
            <li>$moon</li>}
        </ul>} {
        $planet.Name has no moons}
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
