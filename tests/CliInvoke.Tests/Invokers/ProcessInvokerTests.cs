using CliInvoke.Tests.TestData;
using Microsoft.Extensions.DependencyInjection;
using Assert = Xunit.Assert;
using TestContext = Xunit.TestContext;

namespace CliInvoke.Tests.Invokers;

public class ProcessInvokerTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _testFixture;
    
    public ProcessInvokerTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }

    [Fact]
    public async Task Invoker_WhiteSpaceFilePath_ShouldThrow()
    {
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        // A real file path is required here to avoid throwing FileNotFoundException.
        using ProcessConfiguration config = new ProcessConfiguration(ProcessTestHelper.GetTargetFilePath());
        
        config.TargetFilePath = " ";

        await Assert.ThrowsAsync<ArgumentException>(() => processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.Default, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task Invoker_EmptyFilePath_ShouldThrow()
    {
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config = new ProcessConfiguration("FAKE/PATH");
        
        config.TargetFilePath = string.Empty;

        await Assert.ThrowsAsync<ArgumentException>(() => processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task Invoker_InvalidFilePath_ShouldThrow()
    {        
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();
        
        using ProcessConfiguration config = new ProcessConfiguration("FAKE.FILE");

        await Assert.ThrowsAsync<FileNotFoundException>(() => processInvoker.ExecuteBufferedAsync(config, 
            ProcessExitConfiguration.DefaultNoException, cancellationToken: TestContext.Current.CancellationToken));
    }
}