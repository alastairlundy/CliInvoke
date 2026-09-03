/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using CliInvoke.Internal.Versions;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="VersionParseExtensions"/>.
/// </summary>
public class VersionParseExtensionsFuzzTests
{
    [Test]
    public void GracefulParse_StandardDigitDotFormat_ReturnsValidVersion()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    !Enumerable.All(value, c => char.IsDigit(c) || c == '.') ||
                    value.Split('.', StringSplitOptions.RemoveEmptyEntries).Length < 1 ||
                    Enumerable.Any(value.Split('.', StringSplitOptions.RemoveEmptyEntries), seg => seg.Length == 0))
                    return true;

                Version result = Version.GracefulParse(value);
                return result.Major >= 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GracefulParse_WithDigitContent_ReturnsNonNegativeMajor()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value) || !Enumerable.Any(value, char.IsDigit))
                    return true;

                Version result = Version.GracefulParse(value);
                return result.Major >= 0 && result.Minor >= 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GracefulParse_NoDigits_ThrowsArgumentException()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value) || Enumerable.Any(value, char.IsDigit))
                    return true;

                try
                {
                    Version.GracefulParse(value);
                    return false;
                }
                catch (ArgumentException)
                {
                    return true;
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GracefulParse_NullOrEmpty_ThrowsArgumentException()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (value is not null and { Length: > 0 })
                        return true;

                    try
                    {
                        Version.GracefulParse(value!);
                        return false;
                    }
                    catch (ArgumentException)
                    {
                        return true;
                    }
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GracefulParse_WhitespaceOnly_ThrowsArgumentException()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || !string.IsNullOrWhiteSpace(value))
                    return true;

                try
                {
                    Version.GracefulParse(value);
                    return false;
                }
                catch (ArgumentException)
                {
                    return true;
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GracefulParse_SingleDigit_ReturnsVersionWithZeroMinor()
    {
        Prop.ForAll<char>(c =>
                {
                    if (!char.IsDigit(c))
                        return true;

                    Version result = Version.GracefulParse(c.ToString());
                    return result.Major == c - '0' && result.Minor == 0;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void GracefulParse_DotSeparatedDigits_ProducesCorrectComponentCount()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value) || value.Length > 20 ||
                    !Enumerable.All(value, c => char.IsDigit(c) || c == '.'))
                    return true;

                string[] segments = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2 || segments.Length > 4 || Enumerable.Any(segments, seg => seg.Length == 0))
                    return true;

                Version result = Version.GracefulParse(value);

                // Parse each segment and compare with parsed components
                if (!int.TryParse(segments[0], out int expectedMajor))
                    return true;

                if (result.Major != expectedMajor)
                    return false;

                if (segments.Length >= 2)
                {
                    if (!int.TryParse(segments[1], out int expectedMinor))
                        return true;
                    if (result.Minor != expectedMinor)
                        return false;
                }

                if (segments.Length >= 3)
                {
                    if (!int.TryParse(segments[2], out int expectedBuild))
                        return true;
                    if (result.Build != expectedBuild)
                        return false;
                }

                if (segments.Length >= 4)
                {
                    if (!int.TryParse(segments[3], out int expectedRevision))
                        return true;
                    if (result.Revision != expectedRevision)
                        return false;
                }

                return true;
            })
            .QuickCheckThrowOnFailure();
    }
}
