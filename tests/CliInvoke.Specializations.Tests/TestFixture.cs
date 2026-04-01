using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Specializations.Tests;

public class TestFixture
{
    public TestFixture()
    {
        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection = serviceCollection
            .AddCliInvoke();

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider { get; private set; }
}