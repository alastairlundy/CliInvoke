/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Validation;
using CliInvoke.Extensions.Middleware.Validation;

namespace CliInvoke.Tests.Extensions.Middleware.Validation;

/// <summary>
///     Tests for the buffered-to-raw validation-rule adapter: the adapted rule must mirror the
///     inner rule's own null/non-buffered semantics instead of failing every non-buffered result.
/// </summary>
public class PostExitValidationAdapterTests
{
    private static ProcessResult MakeRawResult()
        => new("dummy", 0, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);

    private static BufferedProcessResult MakeBufferedResult(string standardError)
        => new("dummy", 0, 1, "output", standardError, DateTime.UtcNow, DateTime.UtcNow,
            canceled: false, signal: null, wasTruncated: false);

    [Test]
    public async Task StderrIsEmpty_RawResult_Passes()
    {
        IProcessResultValidator<ProcessResult> validator = PostExitValidation.StderrIsEmpty();

        bool isValid = validator.Validate(MakeRawResult());

        await Assert.That(isValid).IsTrue();
        await Assert.That(validator.GetValidationFailures(MakeRawResult())).IsEmpty();
    }

    [Test]
    public async Task StderrIsEmpty_BufferedEmptyStderr_Passes()
    {
        IProcessResultValidator<ProcessResult> validator = PostExitValidation.StderrIsEmpty();

        await Assert.That(validator.Validate(MakeBufferedResult(standardError: ""))).IsTrue();
    }

    [Test]
    public async Task StderrIsEmpty_BufferedNonEmptyStderr_Fails()
    {
        IProcessResultValidator<ProcessResult> validator = PostExitValidation.StderrIsEmpty();

        await Assert.That(validator.Validate(MakeBufferedResult(standardError: "boom"))).IsFalse();
    }

    [Test]
    public async Task StdoutMatches_RawResult_Fails()
    {
        IProcessResultValidator<ProcessResult> validator = PostExitValidation.StdoutMatches(@"\d+\.\d+");

        await Assert.That(validator.Validate(MakeRawResult())).IsFalse();
    }

    [Test]
    public async Task StdoutMatches_BufferedMatchingOutput_Passes()
    {
        IProcessResultValidator<ProcessResult> validator = PostExitValidation.StdoutMatches(@"\d+\.\d+");
        BufferedProcessResult result = new("dummy", 0, 1, "version 1.2", "", DateTime.UtcNow,
            DateTime.UtcNow, canceled: false, signal: null, wasTruncated: false);

        await Assert.That(validator.Validate(result)).IsTrue();
    }
}
