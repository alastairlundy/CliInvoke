using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Specializations.Tests;

internal class TestFixture
{
    internal TestFixture()
    {
        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection = serviceCollection
            .AddCliInvoke();

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    internal IServiceProvider ServiceProvider { get; private set; }
}