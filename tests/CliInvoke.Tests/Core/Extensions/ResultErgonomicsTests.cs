/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;

using CliInvoke.Core.Exceptions;

namespace CliInvoke.Tests.Core.Extensions;

/// <summary>
///     Unit tests covering every new result-ergonomics member:
///     <see cref="ProcessResultHelperExtensions.IsExitCodeZero"/>,
///     <see cref="ProcessResultHelperExtensions.EnsureExitCodeZero"/>,
///     <see cref="ProcessResultHelperExtensions.EnumerateOutputLines"/>,
///     <see cref="ProcessResultHelperExtensions.EnumerateErrorLines"/>,
///     <see cref="ProcessResult.ToString"/>,
///     <see cref="BufferedProcessResult.ToString"/>,
///     and <see cref="BufferedProcessResult.Deconstruct"/>.
/// </summary>
public class ResultErgonomicsTests
{
    private static DateTime FixedTime { get; } =
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ProcessResult MakeProcessResult(int exitCode, bool canceled = false) =>
        new("tool.exe", exitCode, 42, FixedTime, FixedTime, canceled, signal: null);

    private static BufferedProcessResult MakeBuffered(
        string stdout,
        string stderr,
        int exitCode = 0,
        bool wasTruncated = false,
        bool canceled = false) =>
        new("tool.exe", exitCode, 42, stdout, stderr, FixedTime, FixedTime, canceled,
            signal: null, wasTruncated: wasTruncated);

    // -------------------------------------------------------------------
    //  IsExitCodeZero truth table
    // -------------------------------------------------------------------

    [Test]
    public async Task IsExitCodeZero_Zero_ReturnsTrue()
    {
        ProcessResult result = MakeProcessResult(0);

        await Assert.That(result.IsExitCodeZero()).IsTrue();
    }

    [Test]
    public async Task IsExitCodeZero_NonZero_ReturnsFalse()
    {
        ProcessResult result = MakeProcessResult(1);

        await Assert.That(result.IsExitCodeZero()).IsFalse();
    }

    [Test]
    public async Task IsExitCodeZero_CanceledZero_ReturnsTrue()
    {
        // Cancellation does not flip the exit-code fact: exit code 0 is still zero.
        ProcessResult result = MakeProcessResult(0, canceled: true);

        await Assert.That(result.IsExitCodeZero()).IsTrue();
    }

    [Test]
    public async Task IsExitCodeZero_CanceledNonZero_ReturnsFalse()
    {
        ProcessResult result = MakeProcessResult(137, canceled: true);

        await Assert.That(result.IsExitCodeZero()).IsFalse();
    }

    [Test]
    public async Task IsExitCodeZero_NegativeExitCode_ReturnsFalse()
    {
        ProcessResult result = MakeProcessResult(-1);

        await Assert.That(result.IsExitCodeZero()).IsFalse();
    }

    // -------------------------------------------------------------------
    //  EnsureExitCodeZero no-throw and throw paths
    // -------------------------------------------------------------------

    [Test]
    public async Task EnsureExitCodeZero_Zero_DoesNotThrow()
    {
        ProcessResult result = MakeProcessResult(0);

        // The success criterion is that EnsureExitCodeZero() completes without throwing;
        // reaching the assertion below means the call above did not throw.
        result.EnsureExitCodeZero();

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task EnsureExitCodeZero_CanceledZero_DoesNotThrow()
    {
        ProcessResult result = MakeProcessResult(0, canceled: true);

        // The success criterion is that EnsureExitCodeZero() completes without throwing;
        // reaching the assertion below means the call above did not throw.
        result.EnsureExitCodeZero();

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task EnsureExitCodeZero_NonZero_ThrowsProcessNotSuccessfulException()
    {
        ProcessResult result = MakeProcessResult(1);

        await Assert.That(() => result.EnsureExitCodeZero())
            .Throws<ProcessNotSuccessfulException<ProcessResult>>();
    }

    [Test]
    public async Task EnsureExitCodeZero_NonZero_ExceptionPopulatesProcessExceptionInfo()
    {
        ProcessResult result = MakeProcessResult(42);

        ProcessNotSuccessfulException<ProcessResult>? caught = null;
        try
        {
            result.EnsureExitCodeZero();
        }
        catch (ProcessNotSuccessfulException<ProcessResult> ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.ExecutedProcessInfo).IsNotNull();
        await Assert.That(caught.ExecutedProcessInfo!.Result).IsSameReferenceAs(result);
    }

    [Test]
    public async Task EnsureExitCodeZero_BufferedNonZero_ThrowsProcessNotSuccessfulException()
    {
        BufferedProcessResult result = MakeBuffered("out", "err", exitCode: 1);

        await Assert.That(() => result.EnsureExitCodeZero())
            .Throws<ProcessNotSuccessfulException<BufferedProcessResult>>();
    }

    // -------------------------------------------------------------------
    //  EnumerateOutputLines / EnumerateErrorLines parity with GetOutputLines
    // -------------------------------------------------------------------

    [Test]
    public async Task EnumerateOutputLines_EmptyString_MatchesSplit()
    {
        BufferedProcessResult result = MakeBuffered(string.Empty, string.Empty);

        string[] splitLines = result.StandardOutput.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateOutputLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateOutputLines_SingleLine_MatchesSplit()
    {
        BufferedProcessResult result = MakeBuffered("hello", string.Empty);

        string[] splitLines = result.StandardOutput.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateOutputLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateOutputLines_TrailingNewline_MatchesSplit()
    {
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered($"line1{nl}", string.Empty);

        string[] splitLines = result.StandardOutput.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateOutputLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateOutputLines_MultiLine_MatchesSplit()
    {
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered($"a{nl}b{nl}c", string.Empty);

        string[] splitLines = result.StandardOutput.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateOutputLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateOutputLines_ConsecutiveSeparators_MatchesSplit()
    {
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered($"{nl}{nl}", string.Empty);

        string[] splitLines = result.StandardOutput.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateOutputLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateErrorLines_EmptyString_MatchesSplit()
    {
        BufferedProcessResult result = MakeBuffered(string.Empty, string.Empty);

        string[] splitLines = result.StandardError.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateErrorLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateErrorLines_SingleLine_MatchesSplit()
    {
        BufferedProcessResult result = MakeBuffered(string.Empty, "oops");

        string[] splitLines = result.StandardError.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateErrorLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateErrorLines_TrailingNewline_MatchesSplit()
    {
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered(string.Empty, $"err{nl}");

        string[] splitLines = result.StandardError.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateErrorLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateErrorLines_MultiLine_MatchesSplit()
    {
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered(string.Empty, $"x{nl}y{nl}z");

        string[] splitLines = result.StandardError.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateErrorLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateErrorLines_ConsecutiveSeparators_MatchesSplit()
    {
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered(string.Empty, $"{nl}{nl}");

        string[] splitLines = result.StandardError.Split(Environment.NewLine);
        List<string> enumerated = new(result.EnumerateErrorLines());

        await Assert.That(enumerated.Count).IsEqualTo(splitLines.Length);
        for (int i = 0; i < splitLines.Length; i++)
        {
            await Assert.That(enumerated[i]).IsEqualTo(splitLines[i]);
        }
    }

    [Test]
    public async Task EnumerateOutputLines_MatchesGetOutputLines_WithBothStreamsPopulated()
    {
        // BufferedProcessResult validates non-null streams in its constructor, so the
        // null path in EnumerateLines is defensive-only and unreachable via this API;
        // this test instead verifies consistency with GetOutputLines().
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered($"out1{nl}out2", $"err1{nl}err2");

        (string[] stdoutLines, string[] stderrLines) = result.GetOutputLines();
        List<string> outEnum = new(result.EnumerateOutputLines());
        List<string> errEnum = new(result.EnumerateErrorLines());

        await Assert.That(outEnum.Count).IsEqualTo(stdoutLines.Length);
        await Assert.That(errEnum.Count).IsEqualTo(stderrLines.Length);
    }

    // -------------------------------------------------------------------
    //  ToString format snapshots
    // -------------------------------------------------------------------

    [Test]
    public async Task ProcessResult_ToString_ContainsBracketedPrefix()
    {
        ProcessResult result = MakeProcessResult(0);

        string text = result.ToString();

        await Assert.That(text).StartsWith("[ExitCode=");
        await Assert.That(text).EndsWith("]");
        await Assert.That(text).Contains("Path=tool.exe");
        await Assert.That(text).Contains("Runtime=");
    }

    [Test]
    public async Task ProcessResult_ToString_ReflectsExitCode()
    {
        ProcessResult result = MakeProcessResult(42);

        string text = result.ToString();

        await Assert.That(text).Contains("ExitCode=42");
    }

    [Test]
    public async Task BufferedProcessResult_ToString_ContainsStdOutLenAndStdErrLen()
    {
        BufferedProcessResult result = MakeBuffered("hello", "world");

        string text = result.ToString();

        await Assert.That(text).Contains("StdOutLen=5");
        await Assert.That(text).Contains("StdErrLen=5");
        await Assert.That(text).StartsWith("[ExitCode=");
        await Assert.That(text).EndsWith("]");
    }

    [Test]
    public async Task BufferedProcessResult_ToString_TruncatedTrue_ContainsTruncationIndicator()
    {
        BufferedProcessResult result = MakeBuffered("a", "b", wasTruncated: true);

        string text = result.ToString();

        await Assert.That(text).Contains("Truncated=true");
    }

    [Test]
    public async Task BufferedProcessResult_ToString_NotTruncated_OmitsTruncationIndicator()
    {
        BufferedProcessResult result = MakeBuffered("a", "b", wasTruncated: false);

        string text = result.ToString();

        await Assert.That(text).DoesNotContain("Truncated=true");
    }

    [Test]
    public async Task BufferedProcessResult_ToString_EmptyOutput_ZeroLengths()
    {
        BufferedProcessResult result = MakeBuffered(string.Empty, string.Empty);

        string text = result.ToString();

        await Assert.That(text).Contains("StdOutLen=0");
        await Assert.That(text).Contains("StdErrLen=0");
    }

    [Test]
    public async Task ProcessResult_ToString_NewlinePath_IsEscapedToSingleLine()
    {
        ProcessResult result = new("bad\rpath\ntool.exe", 0, 42, FixedTime, FixedTime,
            canceled: false, signal: null);

        string text = result.ToString();

        // Both separators must be escaped to their literal sequences, never embedded raw.
        await Assert.That(text).Contains(@"Path=bad\rpath\ntool.exe");
        await Assert.That(text.Contains('\r')).IsFalse();
        await Assert.That(text.Contains('\n')).IsFalse();

        // The diagnostic representation must remain a single line.
        int lineCount = 0;
        using StringReader reader = new(text);
        while (await reader.ReadLineAsync() is not null)
            lineCount++;
        await Assert.That(lineCount).IsEqualTo(1);
    }

    // -------------------------------------------------------------------
    //  Deconstruct assignment correctness
    // -------------------------------------------------------------------

    [Test]
    public async Task BufferedProcessResult_Deconstruct_AssignsAllThreeParameters()
    {
        BufferedProcessResult result = MakeBuffered("stdout-text", "stderr-text", exitCode: 7);

        result.Deconstruct(out int exitCode, out string stdout, out string stderr);

        await Assert.That(exitCode).IsEqualTo(7);
        await Assert.That(stdout).IsEqualTo("stdout-text");
        await Assert.That(stderr).IsEqualTo("stderr-text");
    }

    [Test]
    public async Task BufferedProcessResult_Deconstruct_TupleDeconstruction_Works()
    {
        BufferedProcessResult result = MakeBuffered("out", "err", exitCode: 99);

        (int code, string so, string se) = result;

        await Assert.That(code).IsEqualTo(99);
        await Assert.That(so).IsEqualTo("out");
        await Assert.That(se).IsEqualTo("err");
    }

    [Test]
    public async Task BufferedProcessResult_Deconstruct_EmptyStrings()
    {
        BufferedProcessResult result = MakeBuffered(string.Empty, string.Empty, exitCode: 0);

        result.Deconstruct(out int exitCode, out string stdout, out string stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stdout).IsEqualTo(string.Empty);
        await Assert.That(stderr).IsEqualTo(string.Empty);
    }
}
