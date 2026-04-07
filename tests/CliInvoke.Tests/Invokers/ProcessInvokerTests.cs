using CliInvoke.Extensions;
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
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        // A real file path is required here to avoid throwing FileNotFoundException.
        using ProcessConfiguration config = ProcessConfiguration.Create(ProcessTestHelper.GetTargetFilePath());

        config.TargetFilePath = " ";

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config, 
            ProcessExitConfiguration.Default, cancellationToken: CancellationToken.None)).Throws<FileNotFoundException>();
    }

    [Test]
    public async Task Invoker_EmptyFilePath_ShouldThrow()
    {
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config = ProcessConfiguration.Create("FAKE/PATH");

        config.TargetFilePath = string.Empty;

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException, cancellationToken: CancellationToken.None)).Throws<FileNotFoundException>();
    }

    [Test]
    public async Task Invoker_InvalidFilePath_ShouldThrow()
    {
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config = ProcessConfiguration.Create("FAKE.FILE");

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.DefaultNoException, cancellationToken: CancellationToken.None)).Throws<FileNotFoundException>();
    }
}