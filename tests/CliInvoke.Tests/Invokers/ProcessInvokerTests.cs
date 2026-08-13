using CliInvoke.Core.Factories;
using CliInvoke.Core.Middleware;
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
    public async Task Invoker_ResultPropagatesFromConfigurationChangingMiddleware()
    {
        IExternalProcessFactory externalProcessFactory =
            _testFixture.ServiceProvider.GetRequiredService<IExternalProcessFactory>();

        // Middleware that rewrites the configuration via WithConfiguration before calling next.
        IProcessMiddleware configMiddleware = new ConfigRewritingMiddleware();

        ProcessInvoker invoker = new ProcessInvoker(externalProcessFactory, new[] { configMiddleware });

        using ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");

        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
            config,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    private sealed class ConfigRewritingMiddleware : IProcessMiddleware
    {
        public async Task InvokeAsync(
            InvocationContext context,
            Func<InvocationContext, CancellationToken, Task> next)
        {
            InvocationContext rewritten = context.WithConfiguration(
                new ProcessConfiguration(
                    context.Configuration.TargetFilePath,
                    context.Configuration.Arguments));
            await next(rewritten, context.CancellationToken);
        }
    }

    [Test]
    public async Task Invoker_WhiteSpaceFilePath_ShouldThrow()
    {
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        // A real file path is required here to avoid throwing FileNotFoundException.
        using ProcessConfiguration config = ProcessConfigurationFactory.Create(ProcessTestHelper.GetTargetFilePath());

        config.TargetFilePath = " ";

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.CreateGraceful(), cancellationToken: CancellationToken.None)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Invoker_EmptyFilePath_ShouldThrow()
    {
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config = ProcessConfigurationFactory.Create("FAKE/PATH");

        config.TargetFilePath = string.Empty;

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.CreateGraceful(), cancellationToken: CancellationToken.None)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Invoker_InvalidFilePath_ShouldThrow()
    {
        IProcessInvoker processInvoker = _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config = ProcessConfigurationFactory.Create("FAKE.FILE");

        await Assert.That(async () => await processInvoker.ExecuteBufferedAsync(config,
            ProcessExitConfiguration.CreateGraceful(), cancellationToken: CancellationToken.None)).Throws<FileNotFoundException>();
    }
}