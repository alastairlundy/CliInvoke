namespace CliInvoke.Tests.Helpers;
    // Helper test class that simulates an IFormattable that returns null from ToString
    internal class NullReturningFormattable : IFormattable
    {
#pragma warning disable CS8603 // Possible null reference return.
        public string ToString(string? format, IFormatProvider? formatProvider) => null;
#pragma warning restore CS8603 // Possible null reference return.
        public override string ToString() => ToString(null, null);
    }