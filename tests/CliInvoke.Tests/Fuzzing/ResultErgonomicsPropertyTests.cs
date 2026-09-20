/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based tests verifying enumeration parity and ToString shape stability
///     for the result-ergonomics members added in tickets 001–004.
/// </summary>
public class ResultErgonomicsPropertyTests
{
    private static DateTime FixedTime { get; } =
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BufferedProcessResult MakeBuffered(string stdout, string stderr) =>
        new("tool.exe", 0, 1, stdout, stderr, FixedTime, FixedTime, canceled: false,
            signal: null);

    /// <summary>
    ///     For arbitrary stdout strings, <c>EnumerateOutputLines()</c> produces the same
    ///     elements as <c>string.Split(Environment.NewLine)</c>.
    /// </summary>
    [Test]
    public void EnumerateOutputLines_ParityWithSplit()
    {
        Prop.ForAll<string>(text =>
            {
                if (text is null) return true;

                BufferedProcessResult result = MakeBuffered(text, string.Empty);
                string[] split = text.Split(Environment.NewLine);
                List<string> enumerated = new(result.EnumerateOutputLines());

                if (enumerated.Count != split.Length) return false;

                for (int i = 0; i < split.Length; i++)
                {
                    if (enumerated[i] != split[i]) return false;
                }

                return true;
            })
            .QuickCheckThrowOnFailure();
    }

    /// <summary>
    ///     For arbitrary stderr strings, <c>EnumerateErrorLines()</c> produces the same
    ///     elements as <c>string.Split(Environment.NewLine)</c>.
    /// </summary>
    [Test]
    public void EnumerateErrorLines_ParityWithSplit()
    {
        Prop.ForAll<string>(text =>
            {
                if (text is null) return true;

                BufferedProcessResult result = MakeBuffered(string.Empty, text);
                string[] split = text.Split(Environment.NewLine);
                List<string> enumerated = new(result.EnumerateErrorLines());

                if (enumerated.Count != split.Length) return false;

                for (int i = 0; i < split.Length; i++)
                {
                    if (enumerated[i] != split[i]) return false;
                }

                return true;
            })
            .QuickCheckThrowOnFailure();
    }

    /// <summary>
    ///     <see cref="ProcessResult.ToString"/> always starts with the bracketed prefix
    ///     <c>[ExitCode=</c> and ends with <c>]</c> across arbitrary field values.
    /// </summary>
    [Test]
    public void ProcessResult_ToString_ShapeStability()
    {
        Prop.ForAll<int, string>((exitCode, path) =>
            {
                if (path is null) path = "unknown";

                ProcessResult result = new ProcessResult(
                    path,
                    exitCode,
                    1,
                    FixedTime,
                    FixedTime,
                    canceled: false,
                    signal: null);

                string text = result.ToString();

                return text.StartsWith("[ExitCode=") && text.EndsWith("]");
            })
            .QuickCheckThrowOnFailure();
    }

    /// <summary>
    ///     <see cref="BufferedProcessResult.ToString"/> always starts with the bracketed prefix
    ///     <c>[ExitCode=</c>, contains <c>StdOutLen=</c> and <c>StdErrLen=</c>,
    ///     and ends with <c>]</c> across arbitrary field values.
    /// </summary>
    [Test]
    public void BufferedProcessResult_ToString_ShapeStability()
    {
        Prop.ForAll<int, string, string>((exitCode, path, stdout) =>
            {
                if (string.IsNullOrEmpty(path)) return true;
                if (stdout is null) stdout = string.Empty;

                BufferedProcessResult result = new BufferedProcessResult(
                    path,
                    exitCode,
                    1,
                    stdout,
                    string.Empty,
                    FixedTime,
                    FixedTime,
                    canceled: false,
                    signal: null);

                string text = result.ToString();

                return text.StartsWith("[ExitCode=")
                       && text.Contains("StdOutLen=")
                       && text.Contains("StdErrLen=")
                       && text.EndsWith("]");
            })
            .QuickCheckThrowOnFailure();
    }

    /// <summary>
    ///     The truncated clause in <see cref="BufferedProcessResult.ToString"/> appears if
    ///     and only if <c>wasTruncated</c> is true, across arbitrary field values.
    /// </summary>
    [Test]
    public void BufferedProcessResult_ToString_TruncatedClause_ClausePresenceMatchesFlag()
    {
        Prop.ForAll<string, string, string>((stdout, stderr, pathSeed) =>
            {
                if (stdout is null) stdout = string.Empty;
                if (stderr is null) stderr = string.Empty;
                if (string.IsNullOrEmpty(pathSeed)) return true;

                BufferedProcessResult notTruncated = new BufferedProcessResult(
                    pathSeed, 0, 1, stdout, stderr,
                    FixedTime, FixedTime, false, null, wasTruncated: false);

                BufferedProcessResult truncated = new BufferedProcessResult(
                    pathSeed, 0, 1, stdout, stderr,
                    FixedTime, FixedTime, false, null, wasTruncated: true);

                string without = notTruncated.ToString();
                string with = truncated.ToString();

                return !without.Contains("Truncated=true")
                       && with.Contains("Truncated=true");
            })
            .QuickCheckThrowOnFailure();
    }
}
