using CliInvoke.Core.Factories;
using CliInvoke.Factories;
using CliInvoke.Tests.TestData;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.Invokers;

[ClassDataSource<TestFixture>(Shared = SharedType.PerClass)]
public class ProcessInvokerTests
{
    private readonly TestFixture _testFixture;

    public ProcessInvokerTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }

    [Test]
    public async Task Invoker_WhiteSpaceFilePath_ShouldThrow()
    {
        IProcessConfigurationFactory configFactory = _testFixture.ServiceProvider.GetRequiredService<IProcessConfigurationFactory>();
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        // A real file path is required here to avoid throwing FileNotFoundException.
        using ProcessConfiguration config = configFactory.Create(ProcessTestHelper.GetTargetFilePath());

        config.TargetFilePath = " ";

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config, 
            ProcessExitConfiguration.Default, cancellationToken: CancellationToken.None)).Throws<FileNotFoundException>();
    }

    [Test]
    public async Task Invoker_EmptyFilePath_ShouldThrow()
    {
        IProcessConfigurationFactory configFactory = new ProcessConfigurationFactory();
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config = configFactory.Create("FAKE/PATH");

        config.TargetFilePath = string.Empty;

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException, cancellationToken: CancellationToken.None)).Throws<FileNotFoundException>();
    }

    [Test]
    public async Task Invoker_InvalidFilePath_ShouldThrow()
    {
        IProcessConfigurationFactory configFactory = _testFixture.ServiceProvider.GetRequiredService<IProcessConfigurationFactory>();
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config = configFactory.Create("FAKE.FILE");

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException, cancellationToken: CancellationToken.None)).Throws<FileNotFoundException>();
    }
}