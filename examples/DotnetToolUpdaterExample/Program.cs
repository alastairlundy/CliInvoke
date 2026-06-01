/*
        NOTE:  CliInvoke Example apps are licensed under the MIT license. CliInvoke is licensed under the MPL 2.0 license.

      MIT License

      Copyright (c) 2026 Alastair Lundy

      Permission is hereby granted, free of charge, to any person obtaining a copy
      of this software and associated documentation files (the "Software"), to deal
      in the Software without restriction, including without limitation the rights
      to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
      copies of the Software, and to permit persons to whom the Software is
      furnished to do so, subject to the following conditions:

      The above copyright notice and this permission notice shall be included in all
      copies or substantial portions of the Software.

      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
      FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
      AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
      LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
      OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
      SOFTWARE.
 */

using System.CommandLine;
using System.Text.Json;
using CliInvoke.Core;
using DotnetToolUpdaterExample;
using DotnetToolUpdaterExample.Models;
using Microsoft.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection()
    .RegisterServices();

IServiceProvider serviceProvider = services.BuildServiceProvider();

RootCommand rootCommand = new("A demo app that updates globally installed .NET Tools.");

Option<bool> verbose = new("verbose")
{
    Aliases = { "v" },
    Description = "Determines whether to output update details.",
    Arity = ArgumentArity.Zero,
    DefaultValueFactory = _ => false
};

rootCommand.Options.Add(verbose);

ParseResult parsedResults = rootCommand.Parse(args);

IFilePathResolver filePathResolver = serviceProvider.GetRequiredService<IFilePathResolver>();
IProcessInvoker processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

bool verboseOutput = parsedResults.CommandResult.GetRequiredValue(verbose);

string dotnetExecutable = filePathResolver.ResolveFilePath("dotnet").FullName;

using ProcessConfiguration toolsListConfiguration = new(dotnetExecutable, "tool list -g --format json");

BufferedProcessResult toolListResult = await processInvoker.ExecuteBufferedAsync(toolsListConfiguration);

if (toolListResult.ExitCode != 0 || string.IsNullOrEmpty(toolListResult.StandardOutput))
{
    Console.Error.WriteLine("Could not determine what tools are installed.");
    Console.Error.WriteLine($"Tool detection failed with exit code {toolListResult.ExitCode}");
    return 1;
}

DotnetToolListResponse? response = JsonSerializer.Deserialize(toolListResult.StandardOutput,
    DotnetToolContext.Default.DotnetToolListResponse);

string[] tools = response?.Data.Select(x => x.PackageId).ToArray() ?? [];

if (tools.Length == 0)
{
    Console.WriteLine("No globally installed tools were found.");
    return 0;
}

bool hasErrors = false;

foreach (string tool in tools)
{
    using ProcessConfiguration toolUpdateConfiguration = new(dotnetExecutable, string.Join(' ', "tool update -g", tool));

    BufferedProcessResult toolResult = await processInvoker.ExecuteBufferedAsync(toolUpdateConfiguration,
        new ProcessExitConfiguration(ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromMinutes(5))));

    if (toolResult.HasErrors())
    {
        hasErrors = true;
        Console.WriteLine($"Updating tool '{tool}' failed.");

        if (verboseOutput)
            Console.WriteLine($"Details:{Environment.NewLine} {toolResult.StandardError}");
    }
    else
    {
        Console.WriteLine($"Tool '{tool}' updated successfully.");

        if (verboseOutput)
            Console.WriteLine($"Details:{Environment.NewLine} {toolResult.StandardOutput}");
    }
}

return hasErrors ? 1 : 0;