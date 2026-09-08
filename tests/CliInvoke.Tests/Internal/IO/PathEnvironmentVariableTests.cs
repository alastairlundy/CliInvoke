/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CliInvoke.Internal.IO;

namespace CliInvoke.Tests.Internal.IO;

[NotInParallel]
public class PathEnvironmentVariableTests
{
    private static string GetUserProfile() =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [Test]
    public async Task EnumerateDirectories_TildeEntry_ExpandsToUserProfile()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        string originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        try
        {
            string testEntry = "~/bin";
            Environment.SetEnvironmentVariable("PATH", testEntry);

            IEnumerable<string>? dirs = PathEnvironmentVariable.EnumerateDirectories();
            string[] result = dirs?.ToArray() ?? [];

            string expected = $"{GetUserProfile()}/bin";
            await Assert.That(result).Contains(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    [Test]
    public async Task EnumerateDirectories_DollarHomeEntry_ExpandsToUserProfile()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        string originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        try
        {
            string testEntry = "$HOME/bin";
            Environment.SetEnvironmentVariable("PATH", testEntry);

            IEnumerable<string>? dirs = PathEnvironmentVariable.EnumerateDirectories();
            string[] result = dirs?.ToArray() ?? [];

            string expected = $"{GetUserProfile()}/bin";
            await Assert.That(result).Contains(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    [Test]
    public async Task EnumerateDirectories_MixedTildeAndDollarHome_ExpandsBoth()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        string originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        try
        {
            string testEntry = "~$HOME/bin";
            Environment.SetEnvironmentVariable("PATH", testEntry);

            IEnumerable<string>? dirs = PathEnvironmentVariable.EnumerateDirectories();
            string[] result = dirs?.ToArray() ?? [];

            string expected = $"{GetUserProfile()}{GetUserProfile()}/bin";
            await Assert.That(result).Contains(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    [Test]
    public async Task EnumerateDirectories_PlainPath_IsUnchanged()
    {
        string originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        try
        {
            string testEntry = "/usr/local/bin";
            Environment.SetEnvironmentVariable("PATH", testEntry);

            IEnumerable<string>? dirs = PathEnvironmentVariable.EnumerateDirectories();
            string[] result = dirs?.ToArray() ?? [];

            await Assert.That(result).Contains("/usr/local/bin");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }
}
