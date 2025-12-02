using System;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using Microsoft.Extensions.Hosting;
using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable LocalizableElement

Console.WriteLine("CliInvoke.Aot.Test starting");

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Ensure the extension method is registered and compatible with AOT publishing
        services.AddCliInvoke();
    })
    .Build();

using var scopes = host.Services.CreateScope();

// Resolve factory/invoker, run "echo <randomNumber>", and print the random number.
IProcessConfigurationFactory factory = scopes.ServiceProvider.GetRequiredService<IProcessConfigurationFactory>();
IProcessInvoker invoker = scopes.ServiceProvider.GetRequiredService<IProcessInvoker>();

int randomNumber = Random.Shared.Next();

Console.WriteLine($"Random number is {randomNumber}");

using ProcessConfiguration procConfig = factory.Create("echo", randomNumber.ToString());

BufferedProcessResult processResult = await invoker.ExecuteBufferedAsync(procConfig);

Console.WriteLine($"Standard Output was: {processResult.StandardOutput}");

return processResult.ExitCode;