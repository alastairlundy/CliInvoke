/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Configuration;
using System.Runtime.Versioning;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="ProcessResourcePolicySpec"/>.
/// </summary>
public class ProcessResourcePolicyFuzzTests
{
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    [Test]
    public void SetMinWorkingSet_GreaterThanMax_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(min =>
            {
                if (min <= 1 || min > nint.MaxValue / 2) return true;

                ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();
                spec.SetMaxWorkingSet(min - 1);

                try
                {
                    spec.SetMinWorkingSet(min);
                    return false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return true;
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    [Test]
    public void SetMaxWorkingSet_LessThanMin_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(max =>
            {
                if (max <= 1 || max >= int.MaxValue - 1) return true;

                ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();
                spec.SetMinWorkingSet(max + 1);

                try
                {
                    spec.SetMaxWorkingSet(max);
                    return false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return true;
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    [Test]
    public void SetMinWorkingSet_Negative_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(value =>
            {
                if (value >= 0) return true;

                ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

                try
                {
                    spec.SetMinWorkingSet(value);
                    return false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return true;
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    [Test]
    public void SetMaxWorkingSet_Negative_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(value =>
            {
                if (value >= 0) return true;

                ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

                try
                {
                    spec.SetMaxWorkingSet(value);
                    return false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return true;
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    [Test]
    public async Task SetMaxWorkingSet_Zero_ThrowsArgumentOutOfRangeException()
    {
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        await Assert.That(() => spec.SetMaxWorkingSet(0)).Throws<ArgumentOutOfRangeException>();
    }

    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    [Test]
    public void Build_WithValidMinAndMax_ReturnsPolicyWithCorrectValues()
    {
        Prop.ForAll<int, int>((min, max) =>
            {
                if (min < 1 || max <= min || max > 100_000_000)
                    return true;

                ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();
                spec.SetMinWorkingSet(min);
                spec.SetMaxWorkingSet(max);

                ProcessResourcePolicy policy = spec.Build();

                return policy.MinWorkingSet == min &&
                       policy.MaxWorkingSet == max;
            })
            .QuickCheckThrowOnFailure();
    }

    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    [Test]
    public async Task Build_DefaultSpec_HasExpectedDefaults()
    {
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();
        ProcessResourcePolicy policy = spec.Build();

        await Assert.That(policy.MinWorkingSet).IsNull();
        await Assert.That(policy.MaxWorkingSet).IsNull();
        await Assert.That(policy.PriorityClass).IsEqualTo(ProcessPriorityClass.Normal);
        await Assert.That(policy.EnablePriorityBoost).IsFalse();
    }
}
