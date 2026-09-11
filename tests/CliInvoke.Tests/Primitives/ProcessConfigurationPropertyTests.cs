using System.Collections.Generic;

using CliInvoke.Core.Configuration;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Primitives;

/// <summary>
///     Property-based tests for <see cref="ProcessConfiguration"/> construction semantics.
/// </summary>
public class ProcessConfigurationPropertyTests
{
    [Test]
    public void SnapshotIsolation_MutatingCallerDictionary_DoesNotChangeConfiguration()
    {
        Prop.ForAll<int>(seed =>
            {
                var rng = new Random(seed);
                Dictionary<string, string> original = new();

                int count = Math.Abs(rng.Next() % 10) + 1;
                for (int i = 0; i < count; i++)
                    original[$"KEY_{i}"] = $"VALUE_{i}";

                ProcessConfiguration config = new()
                {
                    TargetFilePath = "foo.exe",
                    EnvironmentVariables = original
                };

                // Snapshot the configuration's env vars
                int snapshotCount = config.EnvironmentVariables.Count;

                // Mutate the caller's dictionary
                original["EXTRA_KEY"] = "EXTRA_VALUE";

                // Configuration should be unaffected
                return config.EnvironmentVariables.Count == snapshotCount &&
                       !config.EnvironmentVariables.ContainsKey("EXTRA_KEY");
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Equality_SameValues_ReturnsEqual()
    {
        Prop.ForAll<string>(target =>
            {
                if (string.IsNullOrEmpty(target)) return true;

                ProcessConfiguration a = new() { TargetFilePath = target };
                ProcessConfiguration b = new() { TargetFilePath = target };

                return a.Equals(b) && b.Equals(a);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        Prop.ForAll<string>(target =>
            {
                if (string.IsNullOrEmpty(target)) return true;

                ProcessConfiguration a = new() { TargetFilePath = target };
                ProcessConfiguration b = new() { TargetFilePath = target };

                return a.GetHashCode() == b.GetHashCode();
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Equality_DifferentTargets_ReturnsNotEqual()
    {
        Prop.ForAll<string, string>((a, b) =>
            {
                if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b)
                    return true;

                ProcessConfiguration config1 = new() { TargetFilePath = a };
                ProcessConfiguration config2 = new() { TargetFilePath = b };

                return !config1.Equals(config2);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public async Task EnvironmentVariables_EqualAcrossInsertionOrder()
    {
        // Arrange - same entries inserted in different order
        Dictionary<string, string> dict1 = new()
        {
            ["KEY_0"] = "VALUE_A",
            ["KEY_1"] = "VALUE_B",
            ["KEY_2"] = "VALUE_C"
        };

        Dictionary<string, string> dict2 = new()
        {
            ["KEY_2"] = "VALUE_C",
            ["KEY_1"] = "VALUE_B",
            ["KEY_0"] = "VALUE_A"
        };

        ProcessConfiguration config1 = new()
        {
            TargetFilePath = "foo.exe",
            EnvironmentVariables = dict1
        };

        ProcessConfiguration config2 = new()
        {
            TargetFilePath = "foo.exe",
            EnvironmentVariables = dict2
        };

        // Assert - the ImmutableSortedDictionary is sorted by key, so both
        // should have the same entries regardless of insertion order.
        await Assert.That(config1.EnvironmentVariables.Count).IsEqualTo(config2.EnvironmentVariables.Count);
        await Assert.That(config1.EnvironmentVariables["KEY_0"]).IsEqualTo(config2.EnvironmentVariables["KEY_0"]);
        await Assert.That(config1.EnvironmentVariables["KEY_1"]).IsEqualTo(config2.EnvironmentVariables["KEY_1"]);
        await Assert.That(config1.EnvironmentVariables["KEY_2"]).IsEqualTo(config2.EnvironmentVariables["KEY_2"]);
    }
}
