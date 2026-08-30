using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Extensibility.Factories;
using Xunit;

namespace CliInvoke.Tests.Extensibility.Factories;

public class RunnerProcessFactoryTests
{
    private static ProcessConfiguration MakeRunner(string targetFilePath, string? arguments) =>
        new(targetFilePath, arguments);

    private static ProcessConfiguration MakeInner(string targetFilePath, string? arguments) =>
        new(targetFilePath, arguments);

    [Fact]
    public void CreateRunnerConfiguration_PowerShell_WrapsAndEscapesTargetAndArguments()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = MakeRunner("pwsh", "-Command");
        ProcessConfiguration inner = MakeInner("evil\"&calc", "a\"b");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(inner, runner);

        // The dynamic target+arguments become a single double-quoted literal passed to -Command,
        // with inner double quotes backtick-escaped so a quote in the target cannot break out.
        Assert.Equal("-Command \"& 'evil`\"&calc' a`\"b\"", result.Arguments);
    }

    [Fact]
    public void CreateRunnerConfiguration_Cmd_EscapesMetacharactersAndWrapsAsSingleToken()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = MakeRunner("cmd.exe", "/c");
        ProcessConfiguration inner = MakeInner("evil\"&calc", "a\"b");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(inner, runner);

        // The dynamic target+arguments become a single double-quoted /c token, with shell
        // metacharacters caret-escaped so the OS parser cannot re-tokenize the inner contents.
        Assert.Equal("/c \"evil^\"^&calc a^\"b\"", result.Arguments);
    }

    [Fact]
    public void CreateRunnerConfiguration_MaliciousTargetQuote_CannotTerminateWrapperEarly()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = MakeRunner("cmd.exe", "/c");
        ProcessConfiguration inner = MakeInner("prog.exe\" & notepad.exe", string.Empty);

        ProcessConfiguration result = factory.CreateRunnerConfiguration(inner, runner);

        // The runner prefix "/c " is a separate token, followed by the dynamic body wrapped in a
        // single double-quoted token. Every inner quote must be caret-escaped (^\") so none can
        // close the wrapper and inject a second command.
        Assert.StartsWith("/c \"", result.Arguments);
        Assert.EndsWith("\"", result.Arguments);
        Assert.Contains("prog.exe^\"", result.Arguments);
        Assert.DoesNotContain("/c \" &", result.Arguments);
    }

    [Fact]
    public void CreateRunnerConfiguration_StripsControlCharacters()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = MakeRunner("cmd.exe", "/c");
        string argumentWithControlChars = "arg" + (char)1 + (char)2 + "value";
        ProcessConfiguration inner = MakeInner("prog.exe", argumentWithControlChars);

        ProcessConfiguration result = factory.CreateRunnerConfiguration(inner, runner);

        Assert.DoesNotContain((char)1, result.Arguments);
        Assert.DoesNotContain((char)2, result.Arguments);
    }
}
