using AlastairLundy.CliInvoke.Piping;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Piping;

public class ProcessPipeHandlerTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var handler = new ProcessPipeHandler();
        Assert.NotNull(handler);
    }
    // Add more tests for PipeStandardInputAsync, PipeStandardOutputAsync, PipeStandardErrorAsync, etc.
}