/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Tests.Primitives;

/// <summary>
///     Tests the static <see cref="BufferedProcessResult.Equals(BufferedProcessResult?, BufferedProcessResult?)"/>
///     helper for null-safety, mirroring the sibling <c>ProcessResult.Equals(left, right)</c> contract.
/// </summary>
public class BufferedProcessResultStaticEqualsTests
{
    private static DateTime FixedTime { get; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BufferedProcessResult Create()
        => new BufferedProcessResult(
            "tool",
            exitCode: 0,
            processId: 1,
            standardOutput: "out",
            standardError: "err",
            startTime: FixedTime,
            exitTime: FixedTime,
            canceled: false,
            signal: null);

    [Test]
    public async Task StaticEquals_NullLeft_NullRight_IsTrue()
    {
        BufferedProcessResult? left = null;
        BufferedProcessResult? right = null;

        await Assert.That(BufferedProcessResult.Equals(left, right)).IsTrue();
    }

    [Test]
    public async Task StaticEquals_NullLeft_NonNullRight_IsFalse()
    {
        BufferedProcessResult? left = null;
        BufferedProcessResult right = Create();

        await Assert.That(BufferedProcessResult.Equals(left, right)).IsFalse();
    }

    [Test]
    public async Task StaticEquals_NonNullLeft_NullRight_IsFalse()
    {
        BufferedProcessResult left = Create();
        BufferedProcessResult? right = null;

        await Assert.That(BufferedProcessResult.Equals(left, right)).IsFalse();
    }

    [Test]
    public async Task StaticEquals_EquivalentResults_IsTrue()
    {
        BufferedProcessResult left = Create();
        BufferedProcessResult right = Create();

        await Assert.That(BufferedProcessResult.Equals(left, right)).IsTrue();
    }
}
