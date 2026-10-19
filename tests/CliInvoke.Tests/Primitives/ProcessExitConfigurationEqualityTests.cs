/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Tests.Primitives;

/// <summary>
///     Tests the equality operators and static <see cref="ProcessExitConfiguration.Equals(ProcessExitConfiguration?, ProcessExitConfiguration?)"/>
///     helper for null-safety, mirroring the sibling <c>ProcessConfiguration</c> operator contract.
/// </summary>
public class ProcessExitConfigurationEqualityTests
{
    [Test]
    public async Task NullEqualsNull_IsTrue()
    {
        ProcessExitConfiguration? a = null;
        ProcessExitConfiguration? b = null;

        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
    }

    [Test]
    public async Task NullEqualsNonNull_IsFalse()
    {
        ProcessExitConfiguration? a = null;
        ProcessExitConfiguration b = new();

        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task NonNullEqualsNull_IsFalse()
    {
        ProcessExitConfiguration a = new();
        ProcessExitConfiguration? b = null;

        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task EquivalentConfigurations_AreEqual()
    {
        ProcessExitConfiguration a = new();
        ProcessExitConfiguration b = new();

        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
    }

    [Test]
    public async Task StaticEquals_BothNull_IsFalse()
    {
        // Kept behaviour, intentionally different from the == operator: the static
        // Equals(left, right) returns false when either operand is null (both null included),
        // matching ProcessConfiguration's static Equals.
        ProcessExitConfiguration? left = null;
        ProcessExitConfiguration? right = null;

        await Assert.That(ProcessExitConfiguration.Equals(left, right)).IsFalse();
    }
}
