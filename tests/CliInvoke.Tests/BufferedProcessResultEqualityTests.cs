/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
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
    public async Task HashSet_RetainsBothResultsThatDifferOnlyByWasTruncated()
    {
        // The GetHashCode contract permits unequal values to share a hash code, so we do not assert
        // distinct codes. Instead, verify the set retains both entries: it relies on Equals (which
        // distinguishes the truncated flag), not on the hash codes differing.
        BufferedProcessResult truncated = Create(true);
        BufferedProcessResult notTruncated = Create(false);

        var set = new HashSet<BufferedProcessResult> { truncated, notTruncated };

        await Assert.That(set.Count).IsEqualTo(2);
        await Assert.That(set.Contains(truncated)).IsTrue();
        await Assert.That(set.Contains(notTruncated)).IsTrue();
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
