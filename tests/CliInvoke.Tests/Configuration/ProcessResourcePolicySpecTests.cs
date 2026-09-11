/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

using CliInvoke.Core;
using CliInvoke.Core.Configuration;

namespace CliInvoke.Tests.Configuration;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ProcessResourcePolicySpecTests
{
    [Test]
    public async Task DefaultConstructor_ProducesDefaultPolicy()
    {
        // Act
        ProcessResourcePolicy policy = new ProcessResourcePolicySpec().Build();

        // Assert
        await Assert.That(policy.PriorityClass).IsEqualTo(ProcessPriorityClass.Normal);
        await Assert.That(policy.EnablePriorityBoost).IsFalse();
        await Assert.That(policy.MinWorkingSet).IsNull();
        await Assert.That(policy.MaxWorkingSet).IsNull();
    }

    [Test]
    public async Task FluentSetters_ReturnSameInstance()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // Act
        ProcessResourcePolicySpec afterAffinity = spec.SetProcessorAffinity(1);
        ProcessResourcePolicySpec afterMin = spec.SetMinWorkingSet((nint)1024);
        ProcessResourcePolicySpec afterMax = spec.SetMaxWorkingSet((nint)8192);
        ProcessResourcePolicySpec afterPriority = spec.SetPriorityClass(ProcessPriorityClass.High);
        ProcessResourcePolicySpec afterBoost = spec.ConfigurePriorityBoost(true);

        // Assert
        await Assert.That(afterAffinity).IsSameReferenceAs(spec);
        await Assert.That(afterMin).IsSameReferenceAs(spec);
        await Assert.That(afterMax).IsSameReferenceAs(spec);
        await Assert.That(afterPriority).IsSameReferenceAs(spec);
        await Assert.That(afterBoost).IsSameReferenceAs(spec);
    }

    [Test]
    public async Task SetProcessorAffinity_Zero_Throws()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // Assert
        await Assert.That(() => spec.SetProcessorAffinity(0)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetProcessorAffinity_ExceedsMaxMask_Throws()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // The policy rejects any affinity that uses a bit beyond the available processor count.
        // When the machine has the maximum representable processor count there is no value above
        // the ceiling, so only assert the valid-value path in that case.
        if (Environment.ProcessorCount >= (nint.Size * 8) - 1)
        {
            ProcessResourcePolicy ceilingPolicy = spec.SetProcessorAffinity(1).Build();
            await Assert.That(ceilingPolicy).IsNotNull();
            return;
        }

        nint tooLarge = (nint)1 << Environment.ProcessorCount;

        // Assert
        await Assert.That(() => spec.SetProcessorAffinity(tooLarge))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetProcessorAffinity_ValidValue_BuildsPolicy()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // Act
        ProcessResourcePolicy policy = spec.SetProcessorAffinity(1).Build();

        // Assert
        await Assert.That(policy.ProcessorAffinity).IsEqualTo((nint)1);
    }

    [Test]
    public async Task SetMinWorkingSet_Negative_Throws()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // Assert
        await Assert.That(() => spec.SetMinWorkingSet((nint)(-1))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetMaxWorkingSet_Zero_Throws()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // Assert
        await Assert.That(() => spec.SetMaxWorkingSet((nint)0)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetMaxWorkingSet_GreaterThanMin_Succeeds()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // Act
        ProcessResourcePolicy policy = spec
            .SetMinWorkingSet((nint)1024)
            .SetMaxWorkingSet((nint)8192)
            .Build();

        // Assert
        await Assert.That(policy.MinWorkingSet).IsEqualTo((nint)1024);
        await Assert.That(policy.MaxWorkingSet).IsEqualTo((nint)8192);
    }

    [Test]
    public async Task SetMaxWorkingSet_LessThanMin_Throws()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec().SetMinWorkingSet((nint)8192);

        // Assert
        await Assert.That(() => spec.SetMaxWorkingSet((nint)1024))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetMinWorkingSet_GreaterThanMax_Throws()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec().SetMaxWorkingSet((nint)1024);

        // Assert
        await Assert.That(() => spec.SetMinWorkingSet((nint)8192))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetPriorityClass_AndBoost_BuildsPolicy()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec();

        // Act
        ProcessResourcePolicy policy = spec
            .SetPriorityClass(ProcessPriorityClass.High)
            .ConfigurePriorityBoost(true)
            .Build();

        // Assert
        await Assert.That(policy.PriorityClass).IsEqualTo(ProcessPriorityClass.High);
        await Assert.That(policy.EnablePriorityBoost).IsTrue();
    }

    [Test]
    public async Task Build_ReflectsAllConfiguredValues()
    {
        // Arrange
        ProcessResourcePolicySpec spec = new ProcessResourcePolicySpec()
            .SetProcessorAffinity(1)
            .SetMinWorkingSet((nint)1024)
            .SetMaxWorkingSet((nint)8192)
            .SetPriorityClass(ProcessPriorityClass.BelowNormal)
            .ConfigurePriorityBoost(true);

        // Act
        ProcessResourcePolicy policy = spec.Build();

        // Assert
        await Assert.That(policy.ProcessorAffinity).IsEqualTo((nint)1);
        await Assert.That(policy.MinWorkingSet).IsEqualTo((nint)1024);
        await Assert.That(policy.MaxWorkingSet).IsEqualTo((nint)8192);
        await Assert.That(policy.PriorityClass).IsEqualTo(ProcessPriorityClass.BelowNormal);
        await Assert.That(policy.EnablePriorityBoost).IsTrue();
    }
}
