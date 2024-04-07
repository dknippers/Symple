namespace Symple.Exceptions;

public partial class ParseException : Exception
{
    public string Input { get; }

    public int Index { get; }

    public ParseException(string input, int index, string message) : base(message)
    {
        Input = input;
        Index = index;
    }
}
