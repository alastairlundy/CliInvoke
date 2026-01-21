using CliInvoke.Benchmarking.MockDataSimTool.Commands;
using Spectre.Console.Cli;

CommandApp app = new CommandApp();

app.Configure(config =>
{
    config
        .AddCommand<GenerateFakeTextCommand>("generate-fake-text")
        .WithAlias("gen-fake-text")
        .WithDescription("Generates fake text output to the console.");
});

app.SetDefaultCommand<GenerateFakeTextCommand>();

return await app.RunAsync(args);
