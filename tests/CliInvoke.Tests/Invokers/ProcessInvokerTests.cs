using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Factories;
using CliInvoke.Piping;
using Xunit;

namespace CliInvoke.Tests.Invokers;

public class ProcessInvokerTests
{
    private readonly IProcessInvoker processInvoker;
    private readonly IProcessConfigurationFactory configFactory;
    
    public ProcessInvokerTests()
    {
        processInvoker = new ProcessInvoker(new FilePathResolver(), new ProcessPipeHandler());
        configFactory = new ProcessConfigurationFactory();
    }

    [Fact]
    public async Task Invoker_WhiteSpaceFilePath_ShouldThrow()
    {
        using ProcessConfiguration config = configFactory.Create("  ");

        await Assert.ThrowsAsync<ArgumentException>(() => processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException, false, CancellationToken.None));
    }
    
    [Fact]
    public async Task Invoker_EmptyFilePath_ShouldThrow()
    {
        using ProcessConfiguration config = configFactory.Create("FAKE/PATH");
        
        config.TargetFilePath = string.Empty;

        await Assert.ThrowsAsync<ArgumentException>(() => processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException));
    }
    
    [Fact]
    public async Task Invoker_InvalidFilePath_ShouldThrow()
    {
        using ProcessConfiguration config = configFactory.Create("FAKE/PATH/");

        await Assert.ThrowsAsync<FileNotFoundException>(() => processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException));
    }
}