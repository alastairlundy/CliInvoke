using System;
using Microsoft.Extensions.Hosting;
using CliInvoke.Extensions;

Console.WriteLine("AoTTest starting");

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Ensure the extension method is registered and compatible with AOT publishing
        services.AddCliInvoke();
    })
    .Build();

Console.WriteLine("ServiceProvider built: " + (host.Services != null));
return 0;