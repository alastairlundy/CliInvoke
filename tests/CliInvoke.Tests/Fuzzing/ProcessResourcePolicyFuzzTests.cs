/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Configuration;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="ProcessResourcePolicySpec"/>.
/// </summary>
public class ProcessResourcePolicyFuzzTests
{
    [Test]
    public void SetMinWorkingSet_GreaterThanMax_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(min =>
                {
                    if (min <= 1 || min > nint.MaxValue / 2) return true;

                    var spec = new ProcessResourcePolicySpec();
                    spec.SetMaxWorkingSet((nint)(min - 1));

                    try
                    {
                        spec.SetMinWorkingSet((nint)min);
                        return false;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return true;
                    }
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void SetMaxWorkingSet_LessThanMin_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(max =>
                {
                    if (max <= 1 || max >= int.MaxValue - 1) return true;

                    var spec = new ProcessResourcePolicySpec();
                    spec.SetMinWorkingSet((nint)(max + 1));

                    try
                    {
                        spec.SetMaxWorkingSet((nint)max);
                        return false;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return true;
                    }
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void SetMinWorkingSet_Negative_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(value =>
                {
                    if (value >= 0) return true;

                    var spec = new ProcessResourcePolicySpec();

                    try
                    {
                        spec.SetMinWorkingSet((nint)value);
                        return false;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return true;
                    }
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void SetMaxWorkingSet_Negative_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(value =>
                {
                    if (value >= 0) return true;

                    var spec = new ProcessResourcePolicySpec();

                    try
                    {
                        spec.SetMaxWorkingSet((nint)value);
                        return false;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return true;
                    }
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task SetMaxWorkingSet_Zero_ThrowsArgumentOutOfRangeException()
    {
        var spec = new ProcessResourcePolicySpec();

        try
        {
            spec.SetMaxWorkingSet(0);
            return;
        }
        catch (ArgumentOutOfRangeException)
        {
            await Assert.That(true).IsTrue();
        }
    }

    [Test]
    public void Build_WithValidMinAndMax_ReturnsPolicyWithCorrectValues()
    {
        Prop.ForAll<int, int>((min, max) =>
                {
                    if (min < 1 || max <= min || max > 100_000_000)
                        return true;

                    var spec = new ProcessResourcePolicySpec();
                    spec.SetMinWorkingSet((nint)min);
                    spec.SetMaxWorkingSet((nint)max);

                    var policy = spec.Build();

                    return policy.MinWorkingSet == (nint)min &&
                           policy.MaxWorkingSet == (nint)max;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task Build_DefaultSpec_HasExpectedDefaults()
    {
        var spec = new ProcessResourcePolicySpec();
        var policy = spec.Build();

        await Assert.That(policy.MinWorkingSet).IsNull();
        await Assert.That(policy.MaxWorkingSet).IsNull();
        await Assert.That(policy.PriorityClass).IsEqualTo(ProcessPriorityClass.Normal);
        await Assert.That(policy.EnablePriorityBoost).IsFalse();
    }
}
