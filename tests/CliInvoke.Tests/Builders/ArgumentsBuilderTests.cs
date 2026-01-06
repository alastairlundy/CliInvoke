using System.Collections.Generic;
using System.Globalization;
using CliInvoke.Builders;

namespace CliInvoke.Tests.Builders;

public partial class ArgumentsBuilderTests
{
    [Fact]
    public void Add_AppendsValue_WithSingleSpaceBetween()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        IArgumentsBuilder afterFirst = builder.Add("first");
        IArgumentsBuilder afterSecond = afterFirst.Add("second");

        Assert.False(object.ReferenceEquals(builder, afterFirst)); // new instance returned when no validation logic provided
        Assert.Equal("first second", afterSecond.ToString());
    }

    [Fact]
    public void EscapeCharacters_ReplacesSpecialCharacters()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        string input = "\\\n\t\r\""; // backslash, newline, tab, carriage return, double quote, single quote
        string expected = "\"\\\\\\n\\t\\r\\\"";

        string result = builder.EscapeCharacters(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clear_EmptiesBuffer_SharedAcrossInstances()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        IArgumentsBuilder returned = builder.Add("hello");

        // buffer is shared with the returned instance
        Assert.Equal("hello", builder.ToString());
        Assert.Equal("hello", returned.ToString());

        builder.Clear();

        Assert.Equal(string.Empty, builder.ToString());
        Assert.Equal(string.Empty, returned.ToString());
    }

    [Fact]
    public void Add_WithValidationLogic_InvalidReturnsSameInstanceAndDoesNotAppend()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder(s => s != "bad");

        IArgumentsBuilder result = builder.Add("bad");

        Assert.True(object.ReferenceEquals(builder, result));
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void Add_WithValidationLogic_ValidReturnsDifferentInstance_BufferIsShared()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder(s => true);

        IArgumentsBuilder result = builder.Add("ok");

        Assert.False(object.ReferenceEquals(builder, result));
        Assert.Equal("ok", builder.ToString());
        Assert.Equal("ok", result.ToString());
    }

    [Fact]
    public void AddEnumerable_Strings_EscapesAndJoinsValues()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        string[] values = ["a\nb", "c\"d"];

        // Expect the two escaped values to be joined with a space and wrapped in quotes:
        const string expected = "\"a\\nb c\\\"d\"";
        
        IArgumentsBuilder result = builder.AddEnumerable(values, true);

        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void AddEnumerable_Strings_ThrowsOnNullArgument()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddEnumerable((IEnumerable<string>)null!, true));
    }

    [Fact]
    public void Add_IFormattable_ThrowsWhenFormattableProducesNullString()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        NullReturningFormattable nullFormattable = new NullReturningFormattable();

        // When IFormattable.ToString returns null or whitespace, Add should throw NullReferenceException
        Assert.Throws<ArgumentNullException>(() => builder.Add(nullFormattable, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AddEnumerable_IFormattable_JoinsValues()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        IFormattable[] values = { 1, 2 };

        IArgumentsBuilder result = builder.AddEnumerable(values);

        string expected = @"""1 2""";
        
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void AddEnumerable_IFormattable_ThrowsOnNullArgument()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddEnumerable((IEnumerable<IFormattable>)null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Add_StringWithoutEscape_AppendsRawValue()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        IArgumentsBuilder result = builder.Add("x y"); // contains a space inside the value

        Assert.Equal("x y", result.ToString());
    }

}
