/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;
using CliInvoke.Extensions.Middleware;
using CliInvoke.Validation;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests verifying that the post-exit validation middleware (reached via the
///     public <c>UsePostExitValidation</c> builder extension) folds the exit-configuration validation rules
///     ahead of the validator's rules, preserving order, that the merged set is evaluated config-first, and
///     that <c>WithValidationRules</c> copies rules without mutating the source configuration.
/// </summary>
public class ValidationRuleMergeFuzzTests
{
    [Test]
    public void UsePostExitValidation_MergesConfigAndValidatorRules_InOrder()
    {
        Prop.ForAll<int, int>((k, j) =>
            {
                if (k < 0 || k > 8 || j < 0 || j > 8) return true;

                ValidationRule<ProcessResult>[] configRules = new ValidationRule<ProcessResult>[k];
                for (int i = 0; i < k; i++)
                    configRules[i] = new ValidationRule<ProcessResult>(r => true, $"cfg-{i}");

                ValidationRule<ProcessResult>[] validatorRules = new ValidationRule<ProcessResult>[j];
                for (int i = 0; i < j; i++)
                    validatorRules[i] = new ValidationRule<ProcessResult>(r => true, $"val-{i}");

                IProcessResultValidator<ProcessResult> validator = new ProcessResultValidator<ProcessResult>(validatorRules);

                ProcessExitConfiguration exit = ProcessExitConfigurationCreationExtensions.WithValidationRules(
                    ProcessExitConfiguration.CreateGraceful(), configRules);

                ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
                builder.UsePostExitValidation(validator);
                IProcessMiddleware middleware = builder.Build()[0];

                InvocationContext context = new InvocationContext(
                    ProcessConfigurationFactory.Create("dotnet", "--version"),
                    exit,
                    InvocationMode.Buffered,
                    CancellationToken.None);

                ValidationRule<ProcessResult>[]? captured = null;
                Func<InvocationContext, Task> next = c =>
                {
                    captured = c.ExitConfiguration.ValidationRules;
                    return Task.CompletedTask;
                };

                middleware.InvokeAsync(context, next).GetAwaiter().GetResult();

                if (captured is null) return false;
                if (captured.Length != k + j) return false;

                for (int i = 0; i < k; i++)
                    if (captured[i].Name != $"cfg-{i}") return false;

                for (int i = 0; i < j; i++)
                    if (captured[k + i].Name != $"val-{i}") return false;

                return true;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void UsePostExitValidation_FirstFailingRule_IsEvaluatedInMergeOrder()
    {
        // f is the index of the single failing rule in the merged set; -1 means no rule fails. The rules
        // carry real predicates (rule i fails iff i == f), so the pipeline's first-failure walk is exercised
        // against actual predicate evaluations, not a synthetic stand-in.
        Prop.ForAll<int, int, int>((k, j, f) =>
            {
                if (k < 0 || k > 8 || j < 0 || j > 8) return true;
                if (f < -1 || f > k + j - 1) return true;

                ValidationRule<ProcessResult>[] configRules = new ValidationRule<ProcessResult>[k];
                for (int i = 0; i < k; i++)
                {
                    int index = i;
                    configRules[i] = new ValidationRule<ProcessResult>(r => index != f, $"cfg-{i}");
                }

                ValidationRule<ProcessResult>[] validatorRules = new ValidationRule<ProcessResult>[j];
                for (int i = 0; i < j; i++)
                {
                    int index = i;
                    validatorRules[i] = new ValidationRule<ProcessResult>(r => (k + index) != f, $"val-{i}");
                }

                IProcessResultValidator<ProcessResult> validator = new ProcessResultValidator<ProcessResult>(validatorRules);

                ProcessExitConfiguration exit = ProcessExitConfigurationCreationExtensions.WithValidationRules(
                    ProcessExitConfiguration.CreateGraceful(), configRules);

                ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
                builder.UsePostExitValidation(validator);
                IProcessMiddleware middleware = builder.Build()[0];

                InvocationContext context = new InvocationContext(
                    ProcessConfigurationFactory.Create("dotnet", "--version"),
                    exit,
                    InvocationMode.Buffered,
                    CancellationToken.None);

                ValidationRule<ProcessResult>[]? captured = null;
                Func<InvocationContext, Task> next = c =>
                {
                    captured = c.ExitConfiguration.ValidationRules;
                    return Task.CompletedTask;
                };

                middleware.InvokeAsync(context, next).GetAwaiter().GetResult();

                if (captured is null || captured.Length != k + j) return false;

                // Walk the merged set in order, invoking each rule's real predicate; the first that returns
                // false is the failing rule.
                ProcessResult result = new ProcessResult("dummy", 0, 1, DateTime.UtcNow, DateTime.UtcNow, false, null);
                int firstFailing = -1;
                for (int i = 0; i < captured.Length; i++)
                {
                    if (!captured[i].Predicate(result))
                    {
                        firstFailing = i;
                        break;
                    }
                }

                if (f == -1)
                    return firstFailing == -1;

                if (firstFailing != f) return false;

                string expectedName = f < k ? $"cfg-{f}" : $"val-{f - k}";
                return captured[firstFailing].Name == expectedName;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void WithValidationRules_ProducesSuppliedRulesInOrder_WithoutMutatingSource()
    {
        Prop.ForAll<int>((ruleCount) =>
            {
                if (ruleCount < 0 || ruleCount > 8) return true;

                ValidationRule<ProcessResult>[] supplied = new ValidationRule<ProcessResult>[ruleCount];
                for (int i = 0; i < ruleCount; i++)
                    supplied[i] = new ValidationRule<ProcessResult>(r => true, $"sup-{i}");

                // Source starts from a graceful config (its ValidationRules is the default empty array).
                ProcessExitConfiguration source = ProcessExitConfiguration.CreateGraceful();
                ProcessExitConfiguration result = ProcessExitConfigurationCreationExtensions.WithValidationRules(source, supplied);

                if (result.ValidationRules.Length != ruleCount) return false;

                for (int i = 0; i < ruleCount; i++)
                {
                    // Exact supplied rules, in order, by reference and by name.
                    if (!ReferenceEquals(result.ValidationRules[i], supplied[i])) return false;
                    if (result.ValidationRules[i].Name != $"sup-{i}") return false;
                }

                // The source configuration's own rules array is untouched (still the default empty array).
                if (source.ValidationRules.Length != 0) return false;

                return true;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void WithValidationRules_PreservesMaxBufferedOutputBytes()
    {
        Prop.ForAll<long>(capLong =>
            {
                long? cap = capLong;
                if (cap is < 0) return true;

                ValidationRule<ProcessResult>[] rules =
                [
                    new ValidationRule<ProcessResult>(r => true, "rule-0")
                ];

                ProcessExitConfiguration source = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
                    ProcessExitConfiguration.CreateGraceful(), cap);
                ProcessExitConfiguration result = ProcessExitConfigurationCreationExtensions.WithValidationRules(source, rules);

                return result.MaxBufferedOutputBytes == cap
                       && result.ValidationRules.Length == 1;
            })
            .QuickCheckThrowOnFailure();
    }
}
