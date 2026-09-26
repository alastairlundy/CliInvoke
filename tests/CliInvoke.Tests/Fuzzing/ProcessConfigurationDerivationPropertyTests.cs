/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;
using System.Text;
using CliInvoke.Core;
using CliInvoke.Internal;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based tests proving that
///     <see cref="ProcessConfigurationDerivation.Derive(ProcessConfiguration, Action{IProcessConfigurationBuilder})"/>
///     with an empty delta reproduces the source configuration exactly.
///     Resource-owning members are matched by reference.
/// </summary>
/// <remarks>
///     The member set is explicitly enumerated to track the T005 init-only
///     property set. When v4 adds PipeSource (D009) or other init-only
///     properties, extend <see cref="ExpectedPropertyCount"/> and add the
///     corresponding assertion in <see cref="DeriveEmptyDelta_PreservesAllProperties"/>.
/// </remarks>
public class ProcessConfigurationDerivationPropertyTests
{
    /// <summary>
    ///     The number of init-only properties on
    ///     <see cref="ProcessConfiguration"/> that this test asserts.
    ///     Must be updated when the T005 member set changes.
    /// </summary>
    private const int ExpectedPropertyCount = 16;

    /// <summary>
    ///     For any generated <see cref="ProcessConfiguration"/>, derivation with
    ///     an empty delta must reproduce every init-only property exactly.
    ///     Resource-owning members (<see cref="ProcessConfiguration.StandardInput"/>)
    ///     are asserted by reference (same instance), not by value.
    /// </summary>
    [Test]
    public void DeriveEmptyDelta_PreservesAllProperties()
    {
        Prop.ForAll<int>(seed =>
            {
                var rng = new Random(seed);

                // Randomise UseShellExecution first; it constrains
                // RedirectStandardInput and StandardInput via the Build() guard.
                bool useShellExec = rng.Next() % 2 == 0;
                bool redirectStdIn = rng.Next() % 2 == 0;

                if (useShellExec)
                    redirectStdIn = false;

                // Resource-owning member: alternate between StreamWriter.Null
                // (singleton reference) and a caller-owned StreamWriter to
                // verify both paths preserve the reference.
                StreamWriter stdIn;
                if (useShellExec || rng.Next() % 2 == 0)
                    stdIn = StreamWriter.Null;
                else
                    stdIn = new StreamWriter(new MemoryStream());

                Dictionary<string, string> envVars = new();
                int envCount = Math.Abs(rng.Next() % 5);
                for (int i = 0; i < envCount; i++)
                    envVars[$"KEY_{rng.Next()}"] = $"VALUE_{rng.Next()}";

                string[] argumentList = [];
                if (rng.Next() % 2 == 0)
                    argumentList = [$"--flag{rng.Next()}", $"--value{rng.Next()}"];

                ProcessConfiguration source = new()
                {
                    TargetFilePath = $"prog_{Math.Abs(rng.Next())}.exe",
                    Arguments = $"--arg{rng.Next()}",
                    WorkingDirectoryPath = Directory.GetCurrentDirectory(),
                    ArgumentList = argumentList,
                    RequiresAdministrator = rng.Next() % 2 == 0,
                    WindowCreation = rng.Next() % 2 == 0,
                    UseShellExecution = useShellExec,
                    RedirectStandardInput = redirectStdIn,
                    OutputRedirection = rng.Next() % 2 == 0,
                    EnvironmentVariables = envVars,
                    StandardInput = stdIn,
                    ResourcePolicy = ProcessResourcePolicy.Default,
                    Credential = UserCredential.Null,
                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                ProcessConfiguration derived = ProcessConfigurationDerivation.Derive(
                    source, _ => { });

                // ---------------------------------------------------------------
                // Explicit member parity assertion.
                //
                // Every init-only property on ProcessConfiguration (the T005 set)
                // is checked individually so that a missing member surfaces as a
                // compile-time error, not a silent runtime gap.
                //
                // If this count diverges from ExpectedPropertyCount, add the new
                // property below and update the constant.
                // ---------------------------------------------------------------
                bool memberParity =
                    // 1. TargetFilePath
                    derived.TargetFilePath == source.TargetFilePath
                    // 2. Arguments
                    && derived.Arguments == source.Arguments
                    // 3. WorkingDirectoryPath
                    && derived.WorkingDirectoryPath == source.WorkingDirectoryPath
                    // 4. ArgumentList
                    && derived.ArgumentList.SequenceEqual(source.ArgumentList)
                    // 5. RequiresAdministrator
                    && derived.RequiresAdministrator == source.RequiresAdministrator
                    // 6. WindowCreation
                    && derived.WindowCreation == source.WindowCreation
                    // 7. UseShellExecution
                    && derived.UseShellExecution == source.UseShellExecution
                    // 8. RedirectStandardInput
                    && derived.RedirectStandardInput == source.RedirectStandardInput
                    // 9. OutputRedirection
                    && derived.OutputRedirection == source.OutputRedirection
                    // 10. EnvironmentVariables (content equality)
                    && derived.EnvironmentVariables.Count
                       == source.EnvironmentVariables.Count
                    && derived.EnvironmentVariables.SequenceEqual(
                        source.EnvironmentVariables)
                    // 11. ResourcePolicy
                    && derived.ResourcePolicy.Equals(source.ResourcePolicy)
                    // 12. Credential
                    && derived.Credential.Equals(source.Credential)
                    // 13. StandardInputEncoding
                    && derived.StandardInputEncoding.Equals(
                        source.StandardInputEncoding)
                    // 14. StandardOutputEncoding
                    && derived.StandardOutputEncoding.Equals(
                        source.StandardOutputEncoding)
                    // 15. StandardErrorEncoding
                    && derived.StandardErrorEncoding.Equals(
                        source.StandardErrorEncoding);

                // 16. StandardInput — resource-owning member, matched by reference.
                bool standardInputRef =
                    ReferenceEquals(derived.StandardInput, source.StandardInput);

                return memberParity && standardInputRef;
            })
            .QuickCheckThrowOnFailure();
    }
}
