#:package CliInvoke.Core@2.5.4
#:package CliInvoke@2.5.4
#:package CliInvoke.Extensions@2.5.4
#:package Microsoft.Extensions.DependencyInjection@10.0.7

using CliInvoke;
using CliInvoke.Core.Builders;
using CliInvoke.Builders;
using Microsoft.Extensions.DependencyInjection;
using CliInvoke.Core;
using CliInvoke.Extensions;

IServiceCollection services = new ServiceCollection();
services.AddCliInvoke();

IServiceProvider serviceProvider = services.BuildServiceProvider();
IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

// Fluently configure your Command.
IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                          .SetArguments(["arg1", "arg2"])
                          .SetWorkingDirectory("/Path/To/Directory");

// Build it as a CliCommandConfiguration object when you're ready to use it.
using ProcessConfiguration configuration = builder.Build();

// Execute the CliCommand through CommandRunner and get the results.
BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(configuration);
