using AlastairLundy.CliInvoke.Builders;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class ProcessExitInfoBuilderTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var builder = new ProcessExitInfoBuilder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithExitCode_ShouldSetExitCode()
    {
        // TODO: Test WithExitCode method
    }

    [Fact]
    public void WithExitTime_ShouldSetExitTime()
    {
        // TODO: Test WithExitTime method
    }

    [Fact]
    public void Build_ShouldReturnExitInfo()
    {
        // TODO: Test Build method
    }
}