using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using CliInvoke.Benchmarking.MockDataSimTool.Commands;

RootCommand rootCommand = new("Generates fake text output to the console.");

Option<int> lineLengthOption = new("--line-length")
{
    Description = "The length of each generated fake text line.",
    Arity = ArgumentArity.ExactlyOne,
    DefaultValueFactory = _ => GenerateFakeTextCommand.DefaultLineLength
};
lineLengthOption.Aliases.Add("-l");

Option<int> linesOption = new("--lines")
{
    Description = "The number of fake text lines to generate.",
    Arity = ArgumentArity.ExactlyOne,
    DefaultValueFactory = _ => GenerateFakeTextCommand.DefaultLineCount
};
linesOption.Aliases.Add("-n");

rootCommand.Options.Add(lineLengthOption);
rootCommand.Options.Add(linesOption);

// Preserve backward compatibility with callers (e.g. BufferedTestHelper) that invoke "gen-fake-text".
rootCommand.Aliases.Add("gen-fake-text");

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    int lineLength = parseResult.GetRequiredValue(lineLengthOption);
    int lineCount = parseResult.GetRequiredValue(linesOption);

    if (
        lineLength < GenerateFakeTextCommand.MinLineLength
        || lineLength > GenerateFakeTextCommand.MaxLineLength
    )
    {
        Console.Error.WriteLine(
            $"--line-length must be between {GenerateFakeTextCommand.MinLineLength} and {GenerateFakeTextCommand.MaxLineLength}.");
        return 1;
    }

    if (
        lineCount < GenerateFakeTextCommand.MinLineCount
        || lineCount > GenerateFakeTextCommand.MaxLineCount
    )
    {
        Console.Error.WriteLine(
            $"--lines must be between {GenerateFakeTextCommand.MinLineCount} and {GenerateFakeTextCommand.MaxLineCount}.");
        return 1;
    }

    return await Task.Run(() => GenerateFakeTextCommand.Execute(lineLength, lineCount), cancellationToken);
});

return await rootCommand.Parse(args).InvokeAsync();