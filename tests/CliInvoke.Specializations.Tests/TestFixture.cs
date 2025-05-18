using System;
using AlastairLundy.CliInvoke.Extensions;

using Microsoft.Extensions.Hosting;

namespace CliInvoke.Specializations.Tests
{
    public class TestFixture
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public TestFixture()
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(serviceCollection=>
                {
                    serviceCollection.AddCliInvoke();
                })
                .Build();
            
            ServiceProvider = hostBuilder.Services;
        }
    }
}