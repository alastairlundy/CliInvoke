using System.Diagnostics;
using System.Threading.Tasks;
using CliInvoke.Extensions;
using CliInvoke.Specializations.Configurations;

namespace CliInvoke.Specializations.Tests.Configurations;

/// <summary>
///     Verifies that a real <see cref="CmdProcessConfiguration" /> — the documented
///     path for callers who own their quoting — carries its raw command line through
///     the process start sink unchanged: the configuration's tokenised argument path
///     is never populated, so token rules apply only to pre-tokenised arguments and
///     the raw line stays caller-owned.
/// </summary>
public class CmdProcessConfigurationTests
{
    [Test, WindowsOnly]
    public async Task ToProcessStartInfo_RawCmdArgumentsWithSeparator_ArePreserved()
    {
        // CmdProcessConfiguration composes the raw /c line itself; reaching the
        // assertions below without an exception is what shows the raw path is
        // untouched by the tokenised-argument rules.
        CmdProcessConfiguration configuration = new("echo a & echo b",
            redirectStandardInput: false,
            redirectStandardOutput: true,
            redirectStandardError: true);

        ProcessStartInfo processStartInfo = configuration.ToProcessStartInfo();

        await Assert.That(configuration.ArgumentsList.Count).IsEqualTo(0);
        await Assert.That(processStartInfo.Arguments).IsEqualTo("/c echo a & echo b");
        await Assert.That(processStartInfo.ArgumentList.Count).IsEqualTo(0);
        await Assert.That(processStartInfo.FileName).IsEqualTo(configuration.TargetFilePath);
    }

    [Test, WindowsOnly]
    public async Task ToProcessStartInfo_RawCmdBenignArguments_ArePreserved()
    {
        CmdProcessConfiguration configuration = new("echo hello",
            redirectStandardInput: false,
            redirectStandardOutput: true,
            redirectStandardError: true);

        ProcessStartInfo processStartInfo = configuration.ToProcessStartInfo();

        await Assert.That(configuration.ArgumentsList.Count).IsEqualTo(0);
        await Assert.That(processStartInfo.Arguments).IsEqualTo("/c echo hello");
        await Assert.That(processStartInfo.ArgumentList.Count).IsEqualTo(0);
    }
}
