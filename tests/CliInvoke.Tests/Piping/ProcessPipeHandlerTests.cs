using CliInvoke.Piping;

namespace CliInvoke.Tests.Piping;

public class ProcessPipeHandlerTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        ProcessPipeHandler handler = new();
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.NotNull(handler);
    }
    // Add more tests for PipeStandardInputAsync, PipeStandardOutputAsync, PipeStandardErrorAsync, etc.
}