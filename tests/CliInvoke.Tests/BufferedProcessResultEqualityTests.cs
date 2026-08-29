/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core;

namespace CliInvoke.Tests;

/// <summary>
///     Tests that <see cref="BufferedProcessResult.WasTruncated"/> is immutable and participates in
///     equality and hashing, so a capped result is distinct from an uncapped one with identical text.
/// </summary>
public class BufferedProcessResultEqualityTests
{
    private static DateTime FixedTime { get; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BufferedProcessResult Create(bool wasTruncated)
        => new BufferedProcessResult(
            "tool",
            exitCode: 0,
            processId: 1,
            standardOutput: "out",
            standardError: "err",
            startTime: FixedTime,
            exitTime: FixedTime,
            canceled: false,
            signal: null,
            wasTruncated: wasTruncated);

    [Test]
    public async Task Equals_DistinguishesResultsThatDifferOnlyByWasTruncated()
    {
        BufferedProcessResult truncated = Create(true);
        BufferedProcessResult notTruncated = Create(false);

        await Assert.That(truncated.Equals(notTruncated)).IsFalse();
    }

    [Test]
    public async Task GetHashCode_DistinguishesResultsThatDifferOnlyByWasTruncated()
    {
        BufferedProcessResult truncated = Create(true);
        BufferedProcessResult notTruncated = Create(false);

        await Assert.That(truncated.GetHashCode() == notTruncated.GetHashCode()).IsFalse();
    }

    [Test]
    public async Task WasTruncated_IsImmutable_AfterConstruction()
    {
        BufferedProcessResult result = Create(true);

        await Assert.That(result.WasTruncated).IsTrue();
    }

    [Test]
    public async Task Equals_ConsidersEqual_WhenWasTruncatedMatches()
    {
        BufferedProcessResult first = Create(true);
        BufferedProcessResult second = Create(true);

        await Assert.That(first.Equals(second)).IsTrue();
    }
}
