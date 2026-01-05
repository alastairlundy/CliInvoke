using System.Linq;
using System.Threading.Tasks;
using CliInvoke.Core.Factories;
using CliInvoke.Extensions.Caching;
using CliInvoke.Factories;
using CliInvoke.Piping;
using CliInvoke.Tests.TestData;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.Resolvers;

public class CachedFilePathResolverTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _testFixture;
    
    public CachedFilePathResolverTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
    }
    
    [Fact]
    public async Task Resolve_Dotnet_PathEnv_Executable_Cache_NotExpired()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        CachedFilePathResolver cachedFilePathResolver = new(_testFixture.ServiceProvider.GetRequiredService<IMemoryCache>());
        
        string beforeActual = cachedFilePathResolver.ResolveFilePath(executable);

        string expected;

        await Task.Delay(TimeSpan.FromMinutes(1));
        
        string cachedActual = cachedFilePathResolver.ResolveFilePath(executable);

        if (OperatingSystem.IsWindows())
        {
            string? winExpected = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            
            if(winExpected is not null)
                expected = winExpected;
            else
            {
                IProcessConfigurationFactory processConfigurationFactory = new ProcessConfigurationFactory();
                using ProcessConfiguration configuration = processConfigurationFactory.Create("where", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(new FilePathResolver(), new ProcessPipeHandler());

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration, cancellationToken: TestContext.Current.CancellationToken);

                expected = task.StandardOutput.Split(Environment.NewLine).First();
            }
        }
        else
        {
            expected = "/usr/bin/dotnet";
        }
       
        Assert.Equal(beforeActual, cachedActual);
        Assert.Equal(expected, cachedActual);
    }
    
    [Fact]
    public async Task Resolve_Dotnet_PathEnv_Executable_Cache_Expired()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        IMemoryCache cache = _testFixture.ServiceProvider.GetRequiredService<IMemoryCache>();
        ;
        CachedFilePathResolver cachedFilePathResolver = new(cache, TimeSpan.FromMinutes(1));
        
        string beforeActual = cachedFilePathResolver.ResolveFilePath(executable);

        string expected;

        await Task.Delay(TimeSpan.FromMinutes(2));
        
        string afterActual = cachedFilePathResolver.ResolveFilePath(executable);

        if (OperatingSystem.IsWindows())
        {
            string? winExpected = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            
            if(winExpected is not null)
                expected = winExpected;
            else
            {
                IProcessConfigurationFactory processConfigurationFactory = new ProcessConfigurationFactory();
                using ProcessConfiguration configuration = processConfigurationFactory.Create("where", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(cachedFilePathResolver, new ProcessPipeHandler());

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration, cancellationToken: TestContext.Current.CancellationToken);

                expected = task.StandardOutput.Split(Environment.NewLine).First();
            }
        }
        else
        {
            expected = "/usr/bin/dotnet";
        }
       
        Assert.Equal(beforeActual, afterActual);
        Assert.Equal(expected, afterActual);
    }

    [Fact]
    public void Resolve_CrossPlatform_PathEnv_Executable_Cache_NotExpired()
    {
        string expected = ProcessTestHelper.GetTargetFilePath();
        
        IMemoryCache cache = _testFixture.ServiceProvider.GetRequiredService<IMemoryCache>();
        CachedFilePathResolver cachedFilePathResolver = new(cache);

        string beforeActual = cachedFilePathResolver.ResolveFilePath(expected);
        
        Task.Delay(TimeSpan.FromMinutes(1));
        
        string cachedActual = cachedFilePathResolver.ResolveFilePath(expected);
        
        Assert.Equal(beforeActual, cachedActual);
        Assert.Equal(expected, cachedActual);
    }
}