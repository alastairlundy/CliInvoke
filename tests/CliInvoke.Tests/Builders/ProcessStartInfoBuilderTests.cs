using Xunit;
using AlastairLundy.CliInvoke.Builders;

namespace CliInvoke.Tests.Builders;

public class ProcessStartInfoBuilderTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var builder = new ProcessStartInfoBuilder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithFileName_ShouldSetFileName()
    {
        // TODO: Test WithFileName method
    }

    [Fact]
    public void WithArguments_ShouldSetArguments()
    {
        // TODO: Test WithArguments method
    }

    [Fact]
    public void Build_ShouldReturnStartInfo()
    {
        // TODO: Test Build method
    }
}