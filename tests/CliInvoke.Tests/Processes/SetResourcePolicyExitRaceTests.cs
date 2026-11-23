using CliInvoke.Processes.Internal;
using CliInvoke.Processes.Internal.ControlAdapters;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Regression tests for the fast-exiting-child resource-policy race in
///     <see cref="BaseProcessControlAdapter.SetResourcePolicy"/>.
/// </summary>
public class SetResourcePolicyExitRaceTests
{
    [Test]
    public async Task SetResourcePolicy_NonStartedProcess_ThrowsInvalidOperationException()
    {
        BaseProcessControlAdapter adapter = ProcessControlAdapterFactory.Create();

        using ProcessWrapper process = ProcessTestHelper.CreateProcess(
            ProcessTestHelper.GetTargetFilePath(), ProcessTestHelper.GetExitingArguments());

        await Assert.That(process.HasStarted).IsFalse();

        await Assert.That(() => adapter.SetResourcePolicy(process, ProcessResourcePolicy.Default))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SetResourcePolicy_ExitedChild_DoesNotThrow()
    {
        BaseProcessControlAdapter adapter = ProcessControlAdapterFactory.Create();

        using ProcessWrapper process = ProcessTestHelper.CreateProcess(
            ProcessTestHelper.GetTargetFilePath(), ProcessTestHelper.GetExitingArguments());

        process.Start();
        process.WaitForExit();
        await Assert.That(process.HasExited).IsTrue();

        // Policy on a terminated child must be a no-op.
        await Assert.That(() => adapter.SetResourcePolicy(process, ProcessResourcePolicy.Default))
            .ThrowsNothing();
    }

    [Test]
    public async Task SetResourcePolicy_RunningChild_AppliesPolicyWithoutThrowing()
    {
        BaseProcessControlAdapter adapter = ProcessControlAdapterFactory.Create();

        // A long-lived child so every setter runs against a live process.
        ProcessWrapper process = ProcessTestHelper.CreateProcess(
            OperatingSystem.IsWindows() ? "cmd" : "sleep",
            OperatingSystem.IsWindows() ? "/c ping -n 30 127.0.0.1 > nul" : "30");

        try
        {
            process.Start();
            await Assert.That(process.HasStarted).IsTrue();

            await Assert.That(() => adapter.SetResourcePolicy(process, ProcessResourcePolicy.Default))
                .ThrowsNothing();

            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            {
                // The default policy's full affinity mask must equal the child's actual mask.
                await Assert.That(process.ProcessorAffinity)
                    .IsEqualTo((IntPtr)ProcessResourcePolicy.Default.ProcessorAffinity!);
            }

            await Assert.That(process.PriorityClass).IsEqualTo(ProcessPriorityClass.Normal);
        }
        finally
        {
            process.ForcefulExit();
            process.WaitForExit();
            process.Dispose();
        }
    }
}
