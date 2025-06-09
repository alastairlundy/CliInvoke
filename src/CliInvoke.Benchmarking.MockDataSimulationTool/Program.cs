// See https://aka.ms/new-console-template for more information

using CliInvoke.Benchmarking.MockDataSimulationTool.Commands;
using Spectre.Console.Cli;

CommandApp app = new CommandApp();

app.Configure(config =>
{
   config.AddCommand<GenerateFakeTextCommand>("generate-fake-text")
       .WithAlias("gen-fake-text")
       .WithDescription("Generates fake text that is output to the console.");
});

app.SetDefaultCommand<GenerateFakeTextCommand>();


return await app.RunAsync(args);