using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CliInvoke.Tests.TestData;

public class TestFixture
{
    public IServiceProvider ServiceProvider { get; private set; }

    public TestFixture()
    {
        IHost hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(serviceCollection=>
            {
                serviceCollection = serviceCollection.AddCliInvoke()
                    .AddMemoryCache();
            })
            .Build();
            
        ServiceProvider = hostBuilder.Services; 
    }
}