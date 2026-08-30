using System.Threading.Tasks;
using CliInvoke.Builders;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Extensibility.Factories;

namespace CliInvoke.Specializations.Tests.Invokers;

public class RunnerProcessFactoryTests
{
    private static IRunnerProcessFactory Factory => new RunnerProcessFactory();

    private static ProcessConfiguration PowershellRunner(string arguments) =>
        new ProcessConfigurationBuilder("pwsh").SetArguments(arguments).Build();

    private static ProcessConfiguration CmdRunner(string arguments) =>
        new ProcessConfigurationBuilder("cmd.exe").SetArguments(arguments).Build();

    [Test]
    public async Task CreateRunnerConfiguration_Quotes_Powershell_Target()
    {
        ProcessConfiguration inner = new ProcessConfigurationBuilder("C:\\Program Files\\App\\tool.exe")
            .SetArguments("arg1 arg2")
            .Build();

        ProcessConfiguration result = Factory.CreateRunnerConfiguration(inner, PowershellRunner("-Command"));

        await Assert.That(result.Arguments).Contains("&");
        await Assert.That(result.Arguments).Contains("\"C:\\Program Files\\App\\tool.exe\"");
        await Assert.That(result.Arguments).Contains("\"arg1\"");
        await Assert.That(result.Arguments).Contains("\"arg2\"");
    }

    [Test]
    public async Task CreateRunnerConfiguration_Neutralises_Quote_In_Target_Path()
    {
        ProcessConfiguration inner = new ProcessConfigurationBuilder("C:\\evil\"&calc.exe")
            .Build();

        ProcessConfiguration result = Factory.CreateRunnerConfiguration(inner, PowershellRunner("-Command"));

        // The ampersand must be interior to an escaped quote, not a bare command separator.
        await Assert.That(result.Arguments).Contains("evil\\\"&calc.exe");
        await Assert.That(result.Arguments).Contains("evil");
    }

    [Test]
    public async Task CreateRunnerConfiguration_Neutralises_Quote_In_Arguments()
    {
        ProcessConfiguration inner = new ProcessConfigurationBuilder("echo")
            .SetArguments("/c \"& notepad.exe\"")
            .Build();

        ProcessConfiguration result = Factory.CreateRunnerConfiguration(inner, PowershellRunner("-Command"));

        // The ampersand must be quoted (interior to a quote), not a bare command separator.
        await Assert.That(result.Arguments).Contains("\"&");
        await Assert.That(result.Arguments).Contains("notepad.exe");
    }

    [Test]
    public async Task CreateRunnerConfiguration_Quotes_Cmd_Target()
    {
        ProcessConfiguration inner = new ProcessConfigurationBuilder("C:\\Program Files\\App\\tool.exe")
            .SetArguments("arg1 arg2")
            .Build();

        ProcessConfiguration result = Factory.CreateRunnerConfiguration(inner, CmdRunner("/c"));

        await Assert.That(result.Arguments).Contains("/c");
        await Assert.That(result.Arguments).Contains("\"C:\\Program Files\\App\\tool.exe\"");
        await Assert.That(result.Arguments).Contains("\"arg1\"");
        await Assert.That(result.Arguments).Contains("\"arg2\"");
    }

    [Test]
    public async Task CreateRunnerConfiguration_Neutralises_Quote_In_Cmd_Arguments()
    {
        ProcessConfiguration inner = new ProcessConfigurationBuilder("echo")
            .SetArguments("/c \"& notepad.exe\"")
            .Build();

        ProcessConfiguration result = Factory.CreateRunnerConfiguration(inner, CmdRunner("/c"));

        // The ampersand must be quoted (interior to a quote), not a bare command separator.
        await Assert.That(result.Arguments).Contains("\"&");
        await Assert.That(result.Arguments).Contains("notepad.exe");
    }
}
