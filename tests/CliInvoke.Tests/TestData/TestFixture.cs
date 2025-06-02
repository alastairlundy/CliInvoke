using System;


using Microsoft.Extensions.Hosting;

namespace AlastairLundy.CliInvoke.Tests.TestData;

public class TestFixture
{
    public IServiceProvider ServiceProvider { get; private set; }

    public TestFixture()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(serviceCollection=>
            {
               serviceCollection = serviceCollection.AddCliInvoke();
            })
            .Build();
            
        ServiceProvider = hostBuilder.Services; 
    }
}