using System;
using System.Collections.Generic;
using System.Globalization;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Tests.Helpers;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

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
        string input = "\\\n\t\r\"'"; // backslash, newline, tab, carriage return, double quote, single quote
        string expected = "\\\\\\n\\t\\r\\\"\\'";

        string result = builder.EscapeCharacters(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clear_EmptiesBuffer_SharedAcrossInstances()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        IArgumentsBuilder returned = builder.Add("hello");

        // buffer is shared with returned instance
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
        string[] values = new[] { "a\nb", "c\"d" };

        IArgumentsBuilder result = builder.AddEnumerable(values, true);

        // a\nb becomes a\\nb and c"d becomes c\"d, joined with a space
        Assert.Equal("a\\nb c\\\"d", result.ToString());
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
        Assert.Throws<NullReferenceException>(() => builder.Add(nullFormattable, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AddEnumerable_IFormattable_JoinsValues()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        IFormattable[] values = { 1, 2 };

        IArgumentsBuilder result = builder.AddEnumerable(values);

        Assert.Equal("1 2", result.ToString());
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
