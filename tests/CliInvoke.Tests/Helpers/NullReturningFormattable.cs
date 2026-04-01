namespace CliInvoke.Tests.Helpers;

// Helper test class that simulates an IFormattable that returns null from ToString
internal class NullReturningFormattable : IFormattable
{
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return null!;
    }

    public override string ToString()
    {
        return ToString(null, null);
    }
}