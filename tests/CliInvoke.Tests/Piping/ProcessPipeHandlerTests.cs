using CliInvoke.Piping;

namespace CliInvoke.Tests.Piping;

public class ProcessPipeHandlerTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        ProcessPipeHandler handler = new ProcessPipeHandler();
        Assert.NotNull(handler);
    }
    // Add more tests for PipeStandardInputAsync, PipeStandardOutputAsync, PipeStandardErrorAsync, etc.
}