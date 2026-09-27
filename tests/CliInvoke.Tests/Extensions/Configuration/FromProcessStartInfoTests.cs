/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Security;

using CliInvoke.Extensions.Configuration;

using TUnit.Assertions.Enums;
using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Extensions.Configuration;

/// <summary>
///     Tests for <see cref="ConfigurationExtensions"/>'s <c>FromProcessStartInfo</c> mapping,
///     covering the redirect-standard-input flag and the argument-list/string precedence rules.
/// </summary>
public class FromProcessStartInfoTests
{
    [Test]
    public async Task FromProcessStartInfo_RedirectStandardInputTrue_MapsFlag()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("test.exe")
        {
            RedirectStandardInput = true
        };

        ProcessConfiguration config = ProcessConfiguration.FromProcessStartInfo(startInfo);

        await Assert.That(config.RedirectStandardInput).IsTrue();
    }

    [Test]
    public async Task FromProcessStartInfo_RedirectStandardInputFalse_MapsFlag()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("test.exe")
        {
            RedirectStandardInput = false
        };

        ProcessConfiguration config = ProcessConfiguration.FromProcessStartInfo(startInfo);

        await Assert.That(config.RedirectStandardInput).IsFalse();
    }

    [Test]
    public async Task FromProcessStartInfo_ArgumentListNonEmpty_MapsListAndIgnoresRawString()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("test.exe");
        startInfo.ArgumentList.Add("--flag");
        startInfo.ArgumentList.Add("--value");
        startInfo.Arguments = "--ignored raw string";

        ProcessConfiguration config = ProcessConfiguration.FromProcessStartInfo(startInfo);

        await Assert.That(config.ArgumentList)
            .IsEquivalentTo(new List<string> { "--flag", "--value" }, CollectionOrdering.Matching);
        await Assert.That(config.Arguments).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task FromProcessStartInfo_ArgumentListEmpty_MapsRawArgumentsString()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("test.exe")
        {
            Arguments = "--raw value"
        };

        ProcessConfiguration config = ProcessConfiguration.FromProcessStartInfo(startInfo);

        await Assert.That(config.Arguments).IsEqualTo("--raw value");
        await Assert.That(config.ArgumentList).IsEmpty();
    }

    /// <summary>
    ///     Regression test for the Linux CI failure where reading
    ///     <see cref="ProcessStartInfo.Domain"/> threw
    ///     <see cref="PlatformNotSupportedException"/>: the credential mapping must stay
    ///     guarded to Windows while still mapping the values on Windows.
    /// </summary>
    [Test]
    public async Task FromProcessStartInfo_CredentialProperties_MappedOnWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new SkipTestException("ProcessStartInfo user-credential properties are Windows-only.");

        SecureString password = new SecureString();
        foreach (char character in "s3cret")
        {
            password.AppendChar(character);
        }

        password.MakeReadOnly();

        ProcessStartInfo startInfo = new ProcessStartInfo("test.exe")
        {
            UseShellExecute = false
        };
        startInfo.Domain = "WORKGROUP";
        startInfo.UserName = "service-account";
        startInfo.Password = password;
        startInfo.LoadUserProfile = true;

        ProcessConfiguration config = ProcessConfiguration.FromProcessStartInfo(startInfo);

        await Assert.That(config.Credential.Domain).IsEqualTo("WORKGROUP");
        await Assert.That(config.Credential.UserName).IsEqualTo("service-account");
        await Assert.That(config.Credential.Password).IsNotNull();
        await Assert.That(config.Credential.LoadUserProfile).IsTrue();
    }
}
