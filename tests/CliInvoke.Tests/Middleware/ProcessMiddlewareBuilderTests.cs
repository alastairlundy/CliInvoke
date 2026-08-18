using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class ProcessMiddlewareBuilderTests
{
    [Test]
    public async Task UseMiddleware_AddsInstanceToPipeline()
    {
        List<string> callLog = [];
        FakeMiddleware middleware = new FakeMiddleware("A", callLog);
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => middleware);

        builder.UseMiddleware(middleware);

        IReadOnlyList<IProcessMiddleware> result = builder.Build();
        await Assert.That(result).HasCount(1);
        await Assert.That(result[0]).IsEqualTo(middleware);
    }

    [Test]
    public async Task UseMiddleware_ReturnsBuilder_ForFluentChaining()
    {
        List<string> callLog = [];
        FakeMiddleware middleware = new FakeMiddleware("A", callLog);
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => middleware);

        IProcessMiddlewareBuilder returned = builder.UseMiddleware(middleware);

        await Assert.That(returned).IsEqualTo(builder);
    }

    [Test]
    public async Task UseMiddleware_ThrowsArgumentNullException_ForNullMiddleware()
    {
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => null!);

        await Assert.That(() => builder.UseMiddleware(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task UseMiddlewareTyped_ResolvesTypeViaResolver()
    {
        List<string> callLog = [];
        FakeMiddleware expected = new FakeMiddleware("Resolved", callLog);
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(type =>
        {
            if (type == typeof(FakeMiddleware))
                return expected;
            throw new InvalidOperationException($"Unexpected type: {type}");
        });

        builder.UseMiddleware<FakeMiddleware>();

        IReadOnlyList<IProcessMiddleware> result = builder.Build();
        await Assert.That(result).HasCount(1);
        await Assert.That(result[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task UseMiddlewareTyped_ThrowsOnBuild_WhenResolverReturnsNull()
    {
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => null!);
        builder.UseMiddleware<FakeMiddleware>();

        await Assert.That(() => builder.Build())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Build_ReturnsEmptyList_WhenNoMiddlewareRegistered()
    {
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(_ => null!);

        IReadOnlyList<IProcessMiddleware> result = builder.Build();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Build_CombinesInstanceAndTypedMiddleware()
    {
        List<string> callLog = [];
        FakeMiddleware instance = new FakeMiddleware("Instance", callLog);
        FakeMiddleware resolved = new FakeMiddleware("Resolved", callLog);
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(type =>
        {
            if (type == typeof(FakeMiddleware))
                return resolved;
            throw new InvalidOperationException($"Unexpected type: {type}");
        });

        builder.UseMiddleware(instance);
        builder.UseMiddleware<FakeMiddleware>();

        IReadOnlyList<IProcessMiddleware> result = builder.Build();
        await Assert.That(result).HasCount(2);
        await Assert.That(result[0]).IsEqualTo(instance);
        await Assert.That(result[1]).IsEqualTo(resolved);
    }

    [Test]
    public async Task Constructor_ThrowsArgumentNullException_ForNullProvider()
    {
        await Assert.That(() => new ProcessMiddlewareBuilder((IServiceProvider)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_ThrowsArgumentNullException_ForNullResolver()
    {
        await Assert.That(() => new ProcessMiddlewareBuilder((Func<Type, IProcessMiddleware>)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithProvider_ResolvesViaServiceProvider()
    {
        List<string> callLog = [];
        FakeMiddleware expected = new FakeMiddleware("SP", callLog);
        Dictionary<Type, object> services = new Dictionary<Type, object>
        {
            [typeof(FakeMiddleware)] = expected
        };
        IServiceProvider provider = new SimpleServiceProvider(services);
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(provider);

        builder.UseMiddleware<FakeMiddleware>();

        IReadOnlyList<IProcessMiddleware> result = builder.Build();
        await Assert.That(result).HasCount(1);
        await Assert.That(result[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Constructor_WithProvider_ThrowsOnBuild_WhenServiceNotRegistered()
    {
        IServiceProvider provider = new SimpleServiceProvider(new Dictionary<Type, object>());
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(provider);
        builder.UseMiddleware<FakeMiddleware>();

        InvalidOperationException? thrown = await Assert.That(() => Task.FromResult(builder.Build()))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TypedMiddleware_IsReusedAcrossInvocations()
    {
        int constructionCount = 0;
        ProcessMiddlewareBuilder builder = new ProcessMiddlewareBuilder(type =>
        {
            constructionCount++;
            List<string> log = [];
            return new FakeMiddleware("Singleton", log);
        });

        builder.UseMiddleware<FakeMiddleware>();

        IReadOnlyList<IProcessMiddleware> first = builder.Build();
        IReadOnlyList<IProcessMiddleware> second = builder.Build();

        await Assert.That(first[0]).IsEqualTo(second[0]);
        await Assert.That(constructionCount).IsEqualTo(1);
    }

    private sealed class SimpleServiceProvider(Dictionary<Type, object> services) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return services.TryGetValue(serviceType, out object? service) ? service : null;
        }
    }
}
