/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core;
using CliInvoke.Core.Validation;
using CliInvoke.Validation;

namespace CliInvoke.Tests.Validation;

public class ProcessResultValidatorTests
{
    private static ProcessResult MakeResult(int exitCode) =>
        new("app.exe", exitCode, 1, DateTime.UtcNow, DateTime.UtcNow, canceled: false, signal: null);

    [Test]
    public async Task Constructor_FromPredicates_NullRules_Throws()
    {
        await Assert.That(() => new ProcessResultValidator<ProcessResult>((Func<ProcessResult, bool>[])null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_FromRules_NullRules_Throws()
    {
        await Assert.That(() => new ProcessResultValidator<ProcessResult>((ValidationRule<ProcessResult>[])null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Validate_AllRulesPass_ReturnsTrue()
    {
        // Arrange
        ProcessResultValidator<ProcessResult> validator = new(
            new Func<ProcessResult, bool>[] { r => r.ExitCode == 0, r => r.ProcessId > 0 });

        // Act
        bool result = validator.Validate(MakeResult(0));

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Validate_OneRuleFails_ReturnsFalse()
    {
        // Arrange
        ProcessResultValidator<ProcessResult> validator = new(
            new Func<ProcessResult, bool>[] { r => r.ExitCode == 0, r => r.ProcessId > 0 });

        // Act
        bool result = validator.Validate(MakeResult(1));

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Validate_FromValidationRules_RespectsPredicate()
    {
        // Arrange
        ProcessResultValidator<ProcessResult> validator = new(
            new ValidationRule<ProcessResult>[] { new(r => r.ExitCode == 0, "exit zero") });

        // Act / Assert
        await Assert.That(validator.Validate(MakeResult(0))).IsTrue();
        await Assert.That(validator.Validate(MakeResult(5))).IsFalse();
    }

    [Test]
    public async Task Validate_EmptyRules_AlwaysPasses()
    {
        // Arrange
        ProcessResultValidator<ProcessResult> validator =
            new(Array.Empty<Func<ProcessResult, bool>>());

        // Act / Assert
        await Assert.That(validator.Validate(MakeResult(123))).IsTrue();
    }

    [Test]
    public async Task GetValidationFailures_ReturnsOnlyFailingRules()
    {
        // Arrange
        ValidationRule<ProcessResult> exitRule =
            new(r => r.ExitCode == 0, "exit zero");
        ValidationRule<ProcessResult> pidRule =
            new(r => r.ProcessId > 0, "positive pid");
        ProcessResultValidator<ProcessResult> validator =
            new(new ValidationRule<ProcessResult>[] { exitRule, pidRule });

        // Act
        ValidationFailure<ProcessResult>[] failures = validator.GetValidationFailures(MakeResult(1));

        // Assert
        await Assert.That(failures.Length).IsEqualTo(1);
        await Assert.That(failures[0].Rule).IsSameReferenceAs(exitRule);
    }

    [Test]
    public async Task GetValidationFailures_NoFailures_ReturnsEmpty()
    {
        // Arrange
        ProcessResultValidator<ProcessResult> validator = new(
            new ValidationRule<ProcessResult>[] { new(r => r.ExitCode == 0, "exit zero") });

        // Act
        ValidationFailure<ProcessResult>[] failures = validator.GetValidationFailures(MakeResult(0));

        // Assert
        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task ValidationRules_ExposesConfiguredRules()
    {
        // Arrange
        ValidationRule<ProcessResult> rule =
            new(r => r.ExitCode == 0, "exit zero");
        ProcessResultValidator<ProcessResult> validator =
            new(new ValidationRule<ProcessResult>[] { rule });

        // Assert
        await Assert.That(validator.Rules.Length).IsEqualTo(1);
        await Assert.That(validator.ValidationRules[0]).IsSameReferenceAs(rule);
    }
}
