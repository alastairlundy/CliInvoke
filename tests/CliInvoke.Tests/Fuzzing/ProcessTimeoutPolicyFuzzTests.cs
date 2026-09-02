/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="ProcessTimeoutPolicy"/>.
/// </summary>
public class ProcessTimeoutPolicyFuzzTests
{
    [Test]
    public void Equals_IsReflexive()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds < 0) return true;

                    var policy = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds),
                        enabled: true,
                        ProcessExitBehaviour.GracefulExit);

                    return policy.Equals(policy);
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        Prop.ForAll<int, bool>((milliseconds, enabled) =>
                {
                    if (milliseconds < 0) return true;

                    var ts = TimeSpan.FromMilliseconds(milliseconds);
                    var a = new ProcessTimeoutPolicy(ts, enabled, ProcessExitBehaviour.GracefulExit);
                    var b = new ProcessTimeoutPolicy(ts, enabled, ProcessExitBehaviour.GracefulExit);

                    return a.Equals(b) && b.Equals(a);
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds < 1) return true;

                    var a = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds),
                        enabled: true,
                        ProcessExitBehaviour.GracefulExit);

                    var b = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds + 1),
                        enabled: true,
                        ProcessExitBehaviour.GracefulExit);

                    return !a.Equals(b);
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds < 0) return true;

                    var policy = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds),
                        enabled: true);

                    return !policy.Equals(null);
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GetHashCode_ConsistentWithEquals()
    {
        Prop.ForAll<int, bool>((milliseconds, enabled) =>
                {
                    if (milliseconds < 0) return true;

                    var ts = TimeSpan.FromMilliseconds(milliseconds);
                    var a = new ProcessTimeoutPolicy(ts, enabled, ProcessExitBehaviour.GracefulExit);
                    var b = new ProcessTimeoutPolicy(ts, enabled, ProcessExitBehaviour.GracefulExit);

                    return a.Equals(b) && a.GetHashCode() == b.GetHashCode();
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Constructor_NegativeTimeSpan_ThrowsArgumentOutOfRangeException()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds >= 0) return true;

                    try
                    {
                        new ProcessTimeoutPolicy(
                            TimeSpan.FromMilliseconds(milliseconds),
                            enabled: true);
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
    public void FromTimeSpan_PreservesThreshold()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds < 0) return true;

                    var policy = ProcessTimeoutPolicy.FromTimeSpan(
                        TimeSpan.FromMilliseconds(milliseconds));

                    return policy.TimeoutThreshold == TimeSpan.FromMilliseconds(milliseconds) &&
                           policy.Enabled;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void OperatorEquals_Symmetric()
    {
        Prop.ForAll<int, bool>((milliseconds, enabled) =>
                {
                    if (milliseconds < 0) return true;

                    var ts = TimeSpan.FromMilliseconds(milliseconds);
                    var a = new ProcessTimeoutPolicy(ts, enabled, ProcessExitBehaviour.GracefulExit);
                    var b = new ProcessTimeoutPolicy(ts, enabled, ProcessExitBehaviour.GracefulExit);

                    return (a == b) && (b == a);
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void OperatorNotEquals_DifferentValues_ReturnsTrue()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds < 1) return true;

                    var a = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds),
                        enabled: true);
                    var b = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds + 1),
                        enabled: true);

                    return a != b;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void OperatorGreaterThan_NonNullVsNull_ReturnsTrue()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds < 0) return true;

                    var policy = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds),
                        enabled: true);

                    return policy > null;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void OperatorLessThan_NullVsNonNull_ReturnsTrue()
    {
        Prop.ForAll<int>(milliseconds =>
                {
                    if (milliseconds < 0) return true;

                    var policy = new ProcessTimeoutPolicy(
                        TimeSpan.FromMilliseconds(milliseconds),
                        enabled: true);

                    return null < policy;
                })
            .QuickCheckThrowOnFailure();
    }
}
