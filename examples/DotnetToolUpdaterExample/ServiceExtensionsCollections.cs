using CliInvoke;
using CliInvoke.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetToolUpdaterExample;

internal static class ServiceExtensionsCollections
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection RegisterServices()
        {
            services.AddSingleton<IFilePathResolver, FilePathResolver>();
            services.AddSingleton<IProcessInvoker, ProcessInvoker>();

            return services;
        }
    }
}