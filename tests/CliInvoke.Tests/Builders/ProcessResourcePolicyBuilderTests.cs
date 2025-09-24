using AlastairLundy.CliInvoke.Builders;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Builders;

public class ProcessResourcePolicyBuilderTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var builder = new ProcessResourcePolicyBuilder();
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithCpuLimit_ShouldSetCpuLimit()
    {
        // TODO: Test WithCpuLimit method
    }

    [Fact]
    public void WithMemoryLimit_ShouldSetMemoryLimit()
    {
        // TODO: Test WithMemoryLimit method
    }

    [Fact]
    public void Build_ShouldReturnPolicy()
    {
        // TODO: Test Build method
    }
}