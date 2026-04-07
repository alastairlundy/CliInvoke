using System.Collections.Generic;

namespace CliInvoke.Tests.Builders;

public class ArgumentsBuilderTests
{
    [Test]
    public async Task Add_AppendsValue_WithSingleSpaceBetween()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        IArgumentsBuilder afterFirst = builder.Add("first");
        IArgumentsBuilder afterSecond = afterFirst.Add("second");

        await Assert.That(ReferenceEquals(builder, afterFirst)).IsFalse(); // new instance returned when no validation logic provided
        await Assert.That(afterSecond.ToString()).IsEqualTo("first second");
    }

    [Test]
    public async Task EscapeCharacters_ReplacesSpecialCharacters()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        string input = "\\\n\t\r\""; // backslash, newline, tab, carriage return, double quote, single quote
        string expected = "\"\\\\\\n\\t\\r\\\"";

        string result = builder.EscapeCharacters(input);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Clear_EmptiesBuffer_SharedAcrossInstances()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        IArgumentsBuilder returned = builder.Add("hello");

        // buffer is shared with the returned instance
        await Assert.That(builder.ToString()).IsEqualTo("hello");
        await Assert.That(returned.ToString()).IsEqualTo("hello");

        builder.Clear();

        await Assert.That(builder.ToString()).IsEqualTo(string.Empty);
        await Assert.That(returned.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Add_WithValidationLogic_InvalidReturnsSameInstanceAndDoesNotAppend()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder(s => s != "bad");

        IArgumentsBuilder result = builder.Add("bad");

        await Assert.That(ReferenceEquals(builder, result)).IsTrue();
        await Assert.That(builder.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Add_WithValidationLogic_ValidReturnsDifferentInstance_BufferIsShared()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder(_ => true);

        IArgumentsBuilder result = builder.Add("ok");

        await Assert.That(ReferenceEquals(builder, result)).IsFalse();
        await Assert.That(builder.ToString()).IsEqualTo("ok");
        await Assert.That(result.ToString()).IsEqualTo("ok");
    }

    [Test]
    public async Task AddEnumerable_Strings_EscapesAndJoinsValues()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        string[] values = ["a\nb", "c\"d"];

        // Expect the two escaped values to be joined with a space and wrapped in quotes:
        const string expected = "\"a\\nb c\\\"d\"";

        IArgumentsBuilder result = builder.AddRange(values);

        await Assert.That(result.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task AddEnumerable_Strings_ThrowsOnNullArgument()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        await Assert.That(() => builder.AddRange((IEnumerable<string>)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Add_IFormattable_ThrowsWhenFormattableProducesNullString()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        NullReturningFormattable nullFormattable = new NullReturningFormattable();

        // When IFormattable.ToString returns null or whitespace, Add should throw NullReferenceException
        await Assert.That(() => builder.Add(nullFormattable)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task AddEnumerable_IFormattable_JoinsValues()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();
        IFormattable[] values = { 1, 2 };

        IArgumentsBuilder result = builder.AddRange(values);

        string expected = @"""1 2""";

        await Assert.That(result.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task AddEnumerable_IFormattable_ThrowsOnNullArgument()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        await Assert.That(() => builder.AddRange((IEnumerable<string>)null)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Add_StringWithoutEscape_AppendsRawValue()
    {
        IArgumentsBuilder builder = new ArgumentsBuilder();

        IArgumentsBuilder result = builder.Add("x y"); // contains a space inside the value

        await Assert.That(result.ToString()).IsEqualTo("x y");
    }
}