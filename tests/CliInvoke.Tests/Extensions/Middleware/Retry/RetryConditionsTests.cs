/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Middleware;
using CliInvoke.Extensions.Middleware.Retry;

namespace CliInvoke.Tests.Extensions.Middleware.Retry;

/// <summary>
///     Tests for the pre-built retry classifiers, including the non-zero exit-code
///     classifier, which was previously and inaccurately named <c>ExitCodeZero</c>.
/// </summary>
public class RetryConditionsTests
{
    private static ProcessResult MakeResult(int exitCode)
        => new("dummy", exitCode, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);

    [Test]
    public async Task NonZeroExitCode_RetriesOnNonZeroExitCode()
    {
        IRetryClassifier classifier = RetryConditions.NonZeroExitCode();

        await Assert.That(classifier.ShouldRetry(MakeResult(1))).IsTrue();
        await Assert.That(classifier.ShouldRetry(MakeResult(-1))).IsTrue();
    }

    [Test]
    public async Task NonZeroExitCode_DoesNotRetryOnZeroExitCode()
    {
        IRetryClassifier classifier = RetryConditions.NonZeroExitCode();

        await Assert.That(classifier.ShouldRetry(MakeResult(0))).IsFalse();
    }
}
