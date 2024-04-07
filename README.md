# Symple

A simple templating engine targeting .NET 6.0.

It has the following features:

- Variables
  - `$var` or `$[var]`
  - `$var.a.b.c` for nested objects.
- Conditionals  
  - `?[ condition ] { if } { else (optional) }`
- Loops
  - `@[ $x : $xs ] { use $x here }`
  - Variable `$xs` must implement `IEnumerable`, i.e. pretty much any .NET collection class.
- Logical operators
  - `&&`
  - `||`
  - `==`
  - `!=`

## Basic usage
```csharp
var template = "Hello $user.Name, say hi to $user.Parent.Name for me!";

var variables = new Dictionary<string, object?>
{
    ["user"] = new 
    { 
        Name = "John", 
        Parent = new {  Name = "Jim" } 
    }
};

var output = Symple.Parser
    .Parse(template)
    .Render(variables);

// Output: "Hello John, say hi to Jim for me!"
```

## Complete example

The sample below uses all available functionality of Symple. A few things to note:
- You can use collections like `$user.Family` in a conditional `?[ $user.Family ]` which evaluates to `true` if the collection has at least 1 element.
- Similarly, a `string` can also be used in a condition and will evaluate to `true` if it has a length of at least 1.

```cs
var template = @"
<h2>$user.Name</h2>
Family members:
<ul>@[ $member : $user.Family ] {
    <li>
        <span>$member.Name</span>?[ $member.Hobbies ] {
        <ul>@[ $hobby : $member.Hobbies ] {
            <li>$hobby</li>}
        </ul>}
    </li>}
</ul>";

var variables = new Dictionary<string, object?>
{   
    ["user"] = new
    {
        Name = "Rafa",
        Family = new[]
        {
            new { Name = "Sophia", Hobbies = new[] { "Guitar", "Swimming" } },
            new { Name = "Andres", Hobbies = new string[0] { } },
            new { Name = "Pablo", Hobbies = new[] { "Football", "Video games" } },
        },
    }
};

var output = Symple.Parser
    .Parse(template)
    .Render(variables);

// Output:
// <h2>Rafa</h2>
// Family members:
// <ul>
//     <li>
//         <span>Sophia</span>
//         <ul>
//             <li>Guitar</li>
//             <li>Swimming</li>
//         </ul>
//     </li>
//     <li>
//         <span>Andres</span>
//     </li>
//     <li>
//         <span>Pablo</span>
//         <ul>
//             <li>Football</li>
//             <li>Video games</li>
//         </ul>
//     </li>
// </ul>
```

