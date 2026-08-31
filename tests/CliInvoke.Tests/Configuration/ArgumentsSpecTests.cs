/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Globalization;

using CliInvoke.Core.Configuration;
using CliInvoke.Tests.Helpers;

namespace CliInvoke.Tests.Configuration;

public class ArgumentsSpecTests
{
    [Test]
    public async Task DefaultConstructor_AppendsWithoutExtraSpace_WhenBufferEmpty()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.Add((string)"first", escape: false);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("first");
    }

    [Test]
    public async Task Add_InsertsSingleSpaceBetweenValues()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.Add("first", escape: false).Add("second", escape: false);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("first second");
    }

    [Test]
    public async Task Add_RejectsNullValue()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Assert
        await Assert.That(() => spec.Add((string)null!, false)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Add_RejectsInvalidArgument()
    {
        // Arrange
        ArgumentsSpec spec = new(arg => false);

        // Assert
        await Assert.That(() => spec.Add("nope", false))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Add_WithCustomValidation_AllowsValidArgument()
    {
        // Arrange
        ArgumentsSpec spec = new(arg => !arg.StartsWith("-"));

        // Act
        spec.Add("allowed", escape: false);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("allowed");
    }

    [Test]
    public async Task Add_WithoutEscape_KeepsRawValue()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.Add("value with space", escape: false);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("value with space");
    }

    [Test]
    public async Task Add_WithEscape_WrapsValueContainingSpaceInQuotes()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.Add("value with space", escape: true);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("\"value with space\"");
    }

    [Test]
    public async Task Add_WithEscape_PlainValueNotWrapped()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.Add("plainvalue", escape: true);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("plainvalue");
    }

    [Test]
    public async Task Add_Formattable_AppendsFormattedValue()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.Add(42, escape: false);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("42");
    }

    [Test]
    public async Task Add_Formattable_NullToString_Throws()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // A formattable whose ToString() returns null is rejected by the validation
        // logic, which surfaces as ArgumentException ("not permitted").
        await Assert.That(() => spec.Add(new NullReturningFormattable(), escape: false))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task AddEnumerable_WrapsJoinedValuesInQuotes()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.AddEnumerable(new[] { "one", "two", "three" }, escape: false);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("\"one two three\"");
    }

    [Test]
    public async Task AddEnumerable_RejectsNullItem()
    {
        // Arrange
        ArgumentsSpec spec = new(arg => !arg.Contains("x"));

        // Assert - a null item in the collection is rejected.
        await Assert.That(() => spec.AddEnumerable(new[] { "keep", null!, "hasx" }, escape: false))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task AddEnumerable_AllInvalid_Throws()
    {
        // Arrange
        ArgumentsSpec spec = new(_ => false);

        // Assert
        await Assert.That(() => spec.AddEnumerable(new[] { "a", "b" }, escape: false))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task AddEnumerable_RejectsNullCollection()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Assert
        await Assert.That(() => spec.AddEnumerable((IEnumerable<string>)null!, false))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task AddEnumerable_Formattable_WrapsJoinedValues()
    {
        // Arrange
        ArgumentsSpec spec = new();

        // Act
        spec.AddEnumerable(new IFormattable[] { 1, 2, 3 }, escape: false);

        // Assert
        await Assert.That(spec.Build()).IsEqualTo("\"1 2 3\"");
    }

    [Test]
    public async Task Clear_RemovesAllArguments()
    {
        // Arrange
        ArgumentsSpec spec = new();
        spec.Add("first", escape: false).Add("second", escape: false);

        // Act
        spec.Clear();

        // Assert
        await Assert.That(spec.Build()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Build_ReturnsAccumulatedArguments()
    {
        // Arrange
        ArgumentsSpec spec = new();
        spec.Add("alpha", escape: false).Add("beta", escape: false);

        // Act / Assert
        await Assert.That(spec.Build()).IsEqualTo("alpha beta");
    }
}
