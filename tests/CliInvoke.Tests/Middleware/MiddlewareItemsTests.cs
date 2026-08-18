using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class MiddlewareItemsTests
{
    [Test]
    public async Task Get_ReturnsValueSetBySet_ForMatchingType()
    {
        MiddlewareItems items = new MiddlewareItems();
        items.Set("key", 42);

        int result = items.Get<int>("key");

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Get_ReturnsStringValue_ForStringKey()
    {
        MiddlewareItems items = new MiddlewareItems();
        items.Set("name", "hello");

        string? result = items.Get<string>("name");

        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task Get_RoundTrips_BoolValue()
    {
        MiddlewareItems items = new MiddlewareItems();
        items.Set("flag", true);

        bool result = items.Get<bool>("flag");

        await Assert.That(result).IsEqualTo(true);
    }

    [Test]
    public async Task Get_ThrowsKeyNotFoundException_ForMissingKey()
    {
        MiddlewareItems items = new MiddlewareItems();

        await Assert.That(() => items.Get<int>("missing"))
            .Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task Get_ThrowsInvalidOperationException_OnTypeMismatch()
    {
        MiddlewareItems items = new MiddlewareItems();
        items.Set("key", "string value");

        await Assert.That(() => items.Get<int>("key"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Get_OverwritesPreviousValue_ForSameKey()
    {
        MiddlewareItems items = new MiddlewareItems();
        items.Set("key", 1);
        items.Set("key", 2);

        int result = items.Get<int>("key");

        await Assert.That(result).IsEqualTo(2);
    }

}
