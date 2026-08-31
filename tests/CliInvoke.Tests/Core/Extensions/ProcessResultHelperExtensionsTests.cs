/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;

using CliInvoke.Core;
using CliInvoke.Core.Exceptions;
using CliInvoke.Core.Validation;
using CliInvoke.Validation;

namespace CliInvoke.Tests.Core.Extensions;

public class ProcessResultHelperExtensionsTests
{
    private static ProcessResult MakeResult(int exitCode) =>
        new("app.exe", exitCode, 1, DateTime.UtcNow, DateTime.UtcNow, canceled: false, signal: null);

    private static BufferedProcessResult MakeBuffered(string stdout, string stderr) =>
        new("app.exe", 0, 1, stdout, stderr, DateTime.UtcNow, DateTime.UtcNow, canceled: false, signal: null);

    private static ProcessResultValidator<ProcessResult> AlwaysPass() =>
        new(new Func<ProcessResult, bool>[] { r => true });

    private static ProcessResultValidator<ProcessResult> AlwaysFail() =>
        new(new Func<ProcessResult, bool>[] { r => false });

    [Test]
    public async Task ThrowIfUnsuccessful_ValidatorPasses_DoesNotThrow()
    {
        // Arrange
        ProcessResult result = MakeResult(0);

        // Act / Assert - a passing validator must not throw
        result.ThrowIfUnsuccessful(AlwaysPass());
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ThrowIfUnsuccessful_ValidatorFails_ThrowsProcessNotSuccessfulException()
    {
        // Arrange
        ProcessResult result = MakeResult(1);

        // Act / Assert
        await Assert.That(() => result.ThrowIfUnsuccessful(AlwaysFail()))
            .Throws<ProcessNotSuccessfulException<ProcessResult>>();
    }

    [Test]
    public async Task ThrowIfUnsuccessful_WithConfiguration_PopulatesExceptionInfo()
    {
        // Arrange
        ProcessResult result = MakeResult(1);
        ProcessConfiguration config = new("app.exe");

        // Act
        ProcessNotSuccessfulException<ProcessResult>? caught = null;
        try
        {
            result.ThrowIfUnsuccessful(AlwaysFail(), config);
        }
        catch (ProcessNotSuccessfulException<ProcessResult> ex)
        {
            caught = ex;
        }

        // Assert
        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.ExecutedProcessInfo).IsNotNull();
        await Assert.That(caught.ExecutedProcessInfo!.Configuration).IsSameReferenceAs(config);
    }

    [Test]
    public async Task GetFirstOutputLine_ReturnsFirstLine()
    {
        // Arrange
        BufferedProcessResult result = MakeBuffered("first line\nsecond line\nthird", "");

        // Act
        string first = result.GetFirstOutputLine();

        // Assert
        await Assert.That(first).IsEqualTo("first line");
    }

    [Test]
    public async Task GetFirstOutputLine_SingleLine_ReturnsWholeLine()
    {
        // Arrange
        BufferedProcessResult result = MakeBuffered("only line", "");

        // Act
        string first = result.GetFirstOutputLine();

        // Assert
        await Assert.That(first).IsEqualTo("only line");
    }

    [Test]
    public async Task GetFirstOutputLine_EmptyOutput_ReturnsEmpty()
    {
        // Arrange
        BufferedProcessResult result = MakeBuffered("", "");

        // Act
        string first = result.GetFirstOutputLine();

        // Assert
        await Assert.That(first).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GetOutputLines_SplitsStdoutAndStderr()
    {
        // Arrange - use the platform's newline so the split is deterministic across OSes.
        string nl = Environment.NewLine;
        BufferedProcessResult result = MakeBuffered($"a{nl}b{nl}", $"x{nl}y{nl}");

        // Act
        (string[] stdout, string[] stderr) = result.GetOutputLines();

        // Assert - String.Split yields a trailing empty entry after the final newline.
        await Assert.That(stdout.Length).IsEqualTo(3);
        await Assert.That(stdout[0]).IsEqualTo("a");
        await Assert.That(stdout[1]).IsEqualTo("b");
        await Assert.That(stderr.Length).IsEqualTo(3);
        await Assert.That(stderr[0]).IsEqualTo("x");
        await Assert.That(stderr[1]).IsEqualTo("y");
    }

    [Test]
    public async Task HasErrors_EmptyError_ReturnsFalse()
    {
        // Arrange
        BufferedProcessResult result = MakeBuffered("out", "");

        // Assert
        await Assert.That(result.HasErrors()).IsFalse();
    }

    [Test]
    public async Task HasErrors_NonEmptyError_ReturnsTrue()
    {
        // Arrange
        BufferedProcessResult result = MakeBuffered("out", "something went wrong");

        // Assert
        await Assert.That(result.HasErrors()).IsTrue();
    }
}
