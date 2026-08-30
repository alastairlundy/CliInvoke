/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core;
using CliInvoke.Core.Validation;

namespace CliInvoke.Tests.Validation;

public class CommonValidationRulesTests
{
    private static ProcessResult MakeResult(int exitCode) =>
        new("app.exe", exitCode, 1, DateTime.UtcNow, DateTime.UtcNow, canceled: false, signal: null);

    private static BufferedProcessResult MakeBuffered(int exitCode, string stdout, string stderr) =>
        new("app.exe", exitCode, 1, stdout, stderr, DateTime.UtcNow, DateTime.UtcNow, canceled: false, signal: null);

    [Test]
    public async Task RequiresExitCodeZero_MatchesZeroOnly()
    {
        await Assert.That(CommonValidationRules<ProcessResult>.RequiresExitCodeZero(MakeResult(0))).IsTrue();
        await Assert.That(CommonValidationRules<ProcessResult>.RequiresExitCodeZero(MakeResult(1))).IsFalse();
    }

    [Test]
    public async Task RequiresExitCode_MatchesSpecifiedCode()
    {
        Func<ProcessResult, bool> rule = CommonValidationRules<ProcessResult>.RequiresExitCode(42);
        await Assert.That(rule(MakeResult(42))).IsTrue();
        await Assert.That(rule(MakeResult(0))).IsFalse();
    }

    [Test]
    public async Task RequiresAllowedExitCode_MatchesAnyAllowed()
    {
        Func<ProcessResult, bool> rule =
            CommonValidationRules<ProcessResult>.RequiresAllowedExitCode(0, 2, 4);
        await Assert.That(rule(MakeResult(2))).IsTrue();
        await Assert.That(rule(MakeResult(4))).IsTrue();
        await Assert.That(rule(MakeResult(1))).IsFalse();
    }

    [Test]
    public async Task NoValidation_AlwaysTrue()
    {
        await Assert.That(CommonValidationRules<ProcessResult>.NoValidation()(MakeResult(99))).IsTrue();
    }

    [Test]
    public async Task RequiresStandardOutputMatches_NullOrEmptyPattern_Throws()
    {
        await Assert.That(() => CommonValidationRules<BufferedProcessResult>.RequiresStandardOutputMatches(null!))
            .Throws<ArgumentException>();
        await Assert.That(() => CommonValidationRules<BufferedProcessResult>.RequiresStandardOutputMatches(string.Empty))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task RequiresStandardOutputMatches_MatchingOutput_Passes()
    {
        Func<BufferedProcessResult, bool> rule =
            CommonValidationRules<BufferedProcessResult>.RequiresStandardOutputMatches("hello");
        await Assert.That(rule(MakeBuffered(0, "say hello world", ""))).IsTrue();
    }

    [Test]
    public async Task RequiresStandardOutputMatches_NonMatchingOutput_Fails()
    {
        Func<BufferedProcessResult, bool> rule =
            CommonValidationRules<BufferedProcessResult>.RequiresStandardOutputMatches("goodbye");
        await Assert.That(rule(MakeBuffered(0, "say hello world", ""))).IsFalse();
    }

    [Test]
    public async Task RequiresStandardOutputMatches_NonBufferedResult_Fails()
    {
        Func<BufferedProcessResult, bool> rule =
            CommonValidationRules<BufferedProcessResult>.RequiresStandardOutputMatches("hello");
        await Assert.That(rule(MakeResult(0) as BufferedProcessResult)).IsFalse();
    }

    [Test]
    public async Task RequiresStandardErrorIsEmpty_EmptyError_Passes()
    {
        Func<BufferedProcessResult, bool> rule =
            CommonValidationRules<BufferedProcessResult>.RequiresStandardErrorIsEmpty();
        await Assert.That(rule(MakeBuffered(0, "out", ""))).IsTrue();
        await Assert.That(rule(MakeBuffered(0, "out", "   "))).IsTrue();
    }

    [Test]
    public async Task RequiresStandardErrorIsEmpty_NonEmptyError_Fails()
    {
        Func<BufferedProcessResult, bool> rule =
            CommonValidationRules<BufferedProcessResult>.RequiresStandardErrorIsEmpty();
        await Assert.That(rule(MakeBuffered(0, "out", "boom"))).IsFalse();
    }

    [Test]
    public async Task RequiresStandardErrorIsEmpty_NonBufferedResult_Passes()
    {
        Func<BufferedProcessResult, bool> rule =
            CommonValidationRules<BufferedProcessResult>.RequiresStandardErrorIsEmpty();
        await Assert.That(rule(MakeResult(0) as BufferedProcessResult)).IsTrue();
    }

    [Test]
    public async Task ExitCodeZeroRule_HasNameAndMessage()
    {
        // Arrange
        ValidationRule<ProcessResult> rule =
            CommonValidationRules<ProcessResult>.ExitCodeZeroRule();

        // Assert
        await Assert.That(rule.Name).IsEqualTo(nameof(CommonValidationRules<ProcessResult>.RequiresExitCodeZero));
        await Assert.That(rule.FailureMessage).IsNotNull();
        await Assert.That(rule.Predicate(MakeResult(0))).IsTrue();
        await Assert.That(rule.Predicate(MakeResult(1))).IsFalse();
    }

    [Test]
    public async Task StandardOutputMatchesRule_NullOrEmptyPattern_Throws()
    {
        await Assert.That(() => CommonValidationRules<BufferedProcessResult>.StandardOutputMatchesRule(null!))
            .Throws<ArgumentException>();
        await Assert.That(() => CommonValidationRules<BufferedProcessResult>.StandardOutputMatchesRule(string.Empty))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task StandardOutputMatchesRule_ValidatesAgainstPattern()
    {
        // Arrange
        ValidationRule<BufferedProcessResult> rule =
            CommonValidationRules<BufferedProcessResult>.StandardOutputMatchesRule("done");

        // Assert
        await Assert.That(rule.Predicate(MakeBuffered(0, "job done", ""))).IsTrue();
        await Assert.That(rule.Predicate(MakeBuffered(0, "still running", ""))).IsFalse();
        await Assert.That(rule.FailureMessage).Contains("done");
    }

    [Test]
    public async Task StandardErrorIsEmptyRule_Validates()
    {
        // Arrange
        ValidationRule<BufferedProcessResult> rule =
            CommonValidationRules<BufferedProcessResult>.StandardErrorIsEmptyRule();

        // Assert
        await Assert.That(rule.Predicate(MakeBuffered(0, "out", ""))).IsTrue();
        await Assert.That(rule.Predicate(MakeBuffered(0, "out", "err"))).IsFalse();
    }
}
