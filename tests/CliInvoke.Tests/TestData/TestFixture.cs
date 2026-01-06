using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CliInvoke.Tests.TestData;

public class TestFixture
{
    public IServiceProvider ServiceProvider { get; private set; }

    public TestFixture()
    {
        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection = serviceCollection.AddMemoryCache()
            .AddCliInvoke();

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }
}