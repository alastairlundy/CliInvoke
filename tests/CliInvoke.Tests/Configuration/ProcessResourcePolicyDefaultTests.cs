/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics.CodeAnalysis;

namespace CliInvoke.Tests.Configuration;

/// <summary>
///     Verifies the default affinity mask semantics of <see cref="ProcessResourcePolicy"/>:
///     the mask must select every logical processor, not a shrinking subset of cores.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ProcessResourcePolicyDefaultTests
{
    [Test]
    public async Task Default_ProcessorAffinity_SelectsEveryLogicalProcessor()
    {
        // The previous default, 2 * ProcessorCount - 1, selected only 4 cores on an
        // 8-core machine; the mask must instead be (1 << ProcessorCount) - 1.
        nint mask = ProcessResourcePolicy.Default.ProcessorAffinity ?? 0;

        if (Environment.ProcessorCount >= (nint.Size * 8) - 1)
        {
            // Processor count exhausts the native integer width; the mask saturates.
            await Assert.That(mask).IsEqualTo(nint.MaxValue);
            return;
        }

        int selectedProcessorCount = 0;
        for (int bit = 0; bit < Environment.ProcessorCount; bit++)
        {
            if ((mask & ((nint)1 << bit)) != 0)
                selectedProcessorCount++;
        }

        await Assert.That(selectedProcessorCount).IsEqualTo(Environment.ProcessorCount);
        await Assert.That(mask).IsEqualTo(((nint)1 << Environment.ProcessorCount) - 1);
    }

    [Test]
    public async Task Constructor_NullProcessorAffinity_FallsBackToAllLogicalProcessors()
    {
        ProcessResourcePolicy policy = new ProcessResourcePolicy();

        await Assert.That(policy.ProcessorAffinity).IsEqualTo(ProcessResourcePolicy.Default.ProcessorAffinity);
    }
}
