/*
    CliInvoke.Tests
    Copyright (C) 2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

namespace CliInvoke.Tests.Extensibility.Factories;

/// <summary>
///     Session-scoped hooks for the
///     <see cref="RunnerConfigurationFactoryTests"/> suite. Kept in a dedicated class so
///     they are not mixed with the test methods themselves (TUnit0042).
/// </summary>
public class RunnerConfigurationFactoryTestsHooks
{
    [Before(TestSession)]
    public static void PrepareMarkerDir()
    {
        Directory.CreateDirectory(RunnerConfigurationFactoryTests.MarkerDirStatic);
    }

    [After(TestSession)]
    public static void CleanupMarkerDir()
    {
        try
        {
            Directory.Delete(RunnerConfigurationFactoryTests.MarkerDirStatic, recursive: true);
        }
        catch
        {
            // best effort
        }
    }
}
