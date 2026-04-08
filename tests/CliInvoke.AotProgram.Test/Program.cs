using System;
using System.Diagnostics;
using System.Threading;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;
using CliInvoke.Extensions;
using CliInvoke.Processes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable LocalizableElement

Console.WriteLine("CliInvoke.AotProgram.Test starting");

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Ensure the extension method is registered and compatible with AOT publishing
        services.AddCliInvoke();
    })
    .Build();

using IServiceScope scopes = host.Services.CreateScope();

// Resolve factory/invoker, run "echo <randomNumber>", and print the random number.
IProcessInvoker invoker = scopes.ServiceProvider.GetRequiredService<IProcessInvoker>();

using Process process = new Process()
{
    StartInfo = new ProcessStartInfo()
    {
        FileName = "\"\\\"C:\\\\Users\\\\alast\\\\Desktop\\\\To build a unified Blazor Hybrid fr.txt\\\"",
    }
};

Console.WriteLine("Starting process");

process.Start();

Console.WriteLine("CliInvoke.AotProgram.Test finished");

int randomNumber = Random.Shared.Next();

Console.WriteLine($"Random number is {randomNumber}");

using ProcessConfiguration procConfig = ProcessConfiguration.Create("echo", randomNumber.ToString());

BufferedProcessResult processResult = await invoker.ExecuteBufferedAsync(procConfig);

Console.WriteLine($"Standard Output was: {processResult.StandardOutput}");

return processResult.ExitCode;