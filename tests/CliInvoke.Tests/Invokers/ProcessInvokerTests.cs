using Xunit;

namespace AlastairLundy.CliInvoke.Tests;

public class ProcessInvokerTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        // Use mocks for dependencies if needed
        var invoker = new ProcessConfigurationInvoker(null, null);
        Assert.NotNull(invoker);
    }
    // Add more tests for Invoke, RunAsync, etc.
}