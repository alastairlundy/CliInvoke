using System;
using CliInvoke.Core;
using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable LocalizableElement

Console.WriteLine("TrimmingTest starting");

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // This call ensures the AddCliInvoke extension is registered (survives trimming)
        services.AddCliInvoke();
    })
    .Build();

using IServiceScope scopes = host.Services.CreateScope();

// Resolve factory/invoker, run "echo <randomNumber>", and print the random number.
IProcessInvoker invoker = scopes.ServiceProvider.GetRequiredService<IProcessInvoker>();

int randomNumber = Random.Shared.Next();

Console.WriteLine($"Random number is {randomNumber}");

using ProcessConfiguration procConfig = ProcessConfiguration.Create("echo", [randomNumber.ToString()]);

BufferedProcessResult processResult = await invoker.ExecuteBufferedAsync(procConfig);

Console.WriteLine($"Standard Output was: {processResult.StandardOutput}");

return processResult.ExitCode;