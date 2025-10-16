using AlastairLundy.CliInvoke.Builders;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class EnvironmentVariablesBuilderTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        EnvironmentVariablesBuilder builder = new EnvironmentVariablesBuilder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void Add_ShouldAddVariable()
    {
        // TODO: Test Add method
    }

    [Fact]
    public void Build_ShouldReturnVariables()
    {
        // TODO: Test Build method
    }
}