using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public class MiddlewareItemsTests
{
    [Test]
    public async Task Get_ReturnsValueSetBySet_ForMatchingType()
    {
        var items = new MiddlewareItems();
        items.Set("key", 42);

        var result = items.Get<int>("key");

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Get_ReturnsStringValue_ForStringKey()
    {
        var items = new MiddlewareItems();
        items.Set("name", "hello");

        var result = items.Get<string>("name");

        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task Get_RoundTrips_BoolValue()
    {
        var items = new MiddlewareItems();
        items.Set("flag", true);

        var result = items.Get<bool>("flag");

        await Assert.That(result).IsEqualTo(true);
    }

    [Test]
    public async Task Get_ThrowsKeyNotFoundException_ForMissingKey()
    {
        var items = new MiddlewareItems();

        await Assert.That(() => items.Get<int>("missing"))
            .Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task Get_ThrowsInvalidOperationException_OnTypeMismatch()
    {
        var items = new MiddlewareItems();
        items.Set("key", "string value");

        await Assert.That(() => items.Get<int>("key"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Get_OverwritesPreviousValue_ForSameKey()
    {
        var items = new MiddlewareItems();
        items.Set("key", 1);
        items.Set("key", 2);

        var result = items.Get<int>("key");

        await Assert.That(result).IsEqualTo(2);
    }
}
