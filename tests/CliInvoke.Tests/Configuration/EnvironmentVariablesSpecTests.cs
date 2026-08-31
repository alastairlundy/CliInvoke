/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;

using CliInvoke.Core.Configuration;

namespace CliInvoke.Tests.Configuration;

public class EnvironmentVariablesSpecTests
{
    [Test]
    public async Task DefaultConstructor_StartsEmpty()
    {
        // Act
        EnvironmentVariablesSpec spec = new();

        // Assert
        IReadOnlyDictionary<string, string> built = spec.Build();
        await Assert.That(built).IsEmpty();
    }

    [Test]
    public async Task SetPair_AddsVariable()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();

        // Act
        spec.SetPair("KEY", "value");

        // Assert
        IReadOnlyDictionary<string, string> built = spec.Build();
        await Assert.That(built).ContainsKey("KEY");
        await Assert.That(built["KEY"]).IsEqualTo("value");
    }

    [Test]
    public async Task SetPair_RejectsNullOrEmptyNameOrValue()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();

        // Assert
        await Assert.That(() => spec.SetPair(null!, "v")).Throws<ArgumentException>();
        await Assert.That(() => spec.SetPair("", "v")).Throws<ArgumentException>();
        await Assert.That(() => spec.SetPair("k", null!)).Throws<ArgumentException>();
        await Assert.That(() => spec.SetPair("k", "")).Throws<ArgumentException>();
    }

    [Test]
    public async Task SetPair_DuplicateKey_ThrowsWhenConfigured()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();

        // Act
        spec.SetPair("KEY", "one");

        // Assert
        await Assert.That(() => spec.SetPair("KEY", "two"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task SetPair_DuplicateKey_OverridesWhenDisabled()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new(StringComparer.Ordinal, throwExceptionIfDuplicateKeyFound: false);

        // Act
        spec.SetPair("KEY", "one").SetPair("KEY", "two");

        // Assert
        await Assert.That(spec.Build()["KEY"]).IsEqualTo("two");
    }

    [Test]
    public async Task SetEnumerable_AddsAllVariables()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();
        List<KeyValuePair<string, string>> vars = new()
        {
            new("A", "1"),
            new("B", "2"),
        };

        // Act
        spec.SetEnumerable(vars);

        // Assert
        IReadOnlyDictionary<string, string> built = spec.Build();
        await Assert.That(built["A"]).IsEqualTo("1");
        await Assert.That(built["B"]).IsEqualTo("2");
    }

    [Test]
    public async Task SetEnumerable_RejectsNull()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();

        // Assert
        await Assert.That(() => spec.SetEnumerable((IEnumerable<KeyValuePair<string, string>>)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task SetEnumerable_DuplicateKey_ThrowsWhenConfigured()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();
        List<KeyValuePair<string, string>> vars = new()
        {
            new("K", "1"),
            new("K", "2"),
        };

        // Assert
        await Assert.That(() => spec.SetEnumerable(vars)).Throws<ArgumentException>();
    }

    [Test]
    public async Task SetDictionary_AddsVariables()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();
        Dictionary<string, string> vars = new() { ["X"] = "9" };

        // Act
        spec.SetDictionary(vars);

        // Assert
        await Assert.That(spec.Build()["X"]).IsEqualTo("9");
    }

    [Test]
    public async Task SetReadOnlyDictionary_AddsVariables()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();
        IReadOnlyDictionary<string, string> vars = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string> { ["R"] = "7" });

        // Act
        spec.SetReadOnlyDictionary(vars);

        // Assert
        await Assert.That(spec.Build()["R"]).IsEqualTo("7");
    }

    [Test]
    public async Task CustomComparer_RespectsCaseSensitivity()
    {
        // Arrange
        EnvironmentVariablesSpec spec =
            new(StringComparer.OrdinalIgnoreCase, throwExceptionIfDuplicateKeyFound: false);

        // Act
        spec.SetPair("key", "lower").SetPair("KEY", "upper");

        // Assert - case-insensitive comparer treats them as the same key, overridden
        await Assert.That(spec.Build().Count).IsEqualTo(1);
        await Assert.That(spec.Build()["key"]).IsEqualTo("upper");
    }

    [Test]
    public async Task Build_ReturnsCopy_NotBackingStore()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();
        spec.SetPair("K", "v");
        IReadOnlyDictionary<string, string> first = spec.Build();

        // Act
        spec.Clear();

        // Assert - the previously built dictionary is unaffected by later mutation
        await Assert.That(first).ContainsKey("K");
    }

    [Test]
    public async Task Clear_RemovesAllVariables()
    {
        // Arrange
        EnvironmentVariablesSpec spec = new();
        spec.SetPair("K", "v");

        // Act
        spec.Clear();

        // Assert
        await Assert.That(spec.Build()).IsEmpty();
    }
}
