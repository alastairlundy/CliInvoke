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
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests verifying that the buffered-output cap (<see cref="ProcessExitConfiguration.MaxBufferedOutputBytes"/>)
///     round-trips through the configuration extensions, is published onto the exit configuration by the
///     output-truncation middleware without disturbing the other configuration properties, and participates
///     in <see cref="ProcessExitConfiguration"/> equality.
/// </summary>
public class TruncationCapFuzzTests
{
    [Test]
    public void WithMaxBufferedOutputBytes_RoundTripsCap_AndPreservesOtherProperties()
    {
        // FsCheck generates long; we map to long? inside and guard negatives. The null case is
        // covered explicitly by WithMaxBufferedOutputBytes_NullCap_RoundTripsNull below.
        Prop.ForAll<long>(capLong =>
            {
                long? cap = capLong;
                if (cap is < 0) return true;

                ProcessExitConfiguration original = ProcessExitConfiguration.CreateGraceful();
                ProcessExitConfiguration updated = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(original, cap);

                return updated.MaxBufferedOutputBytes == cap
                       && ReferenceEquals(updated.ValidationRules, original.ValidationRules)
                       && updated.TimeoutPolicy == original.TimeoutPolicy
                       && updated.ExceptionBehaviour == original.ExceptionBehaviour
                       && updated.RequestedCancellationExitBehaviour == original.RequestedCancellationExitBehaviour
                       && updated.CancellationThrowsException == original.CancellationThrowsException;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void WithMaxBufferedOutputBytes_NullCap_RoundTripsNull()
    {
        ProcessExitConfiguration original = ProcessExitConfiguration.CreateGraceful();
        ProcessExitConfiguration updated = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(original, null);

        if (updated.MaxBufferedOutputBytes is not null)
            throw new InvalidOperationException("WithMaxBufferedOutputBytes did not preserve a null cap.");
    }

    [Test]
    public void WithValidationRules_And_WithMaxBufferedOutputBytes_CrossPreserve()
    {
        Prop.ForAll<long, int>((capLong, ruleCount) =>
            {
                long? cap = capLong;
                if (cap is < 0) return true;
                if (ruleCount < 0 || ruleCount > 5) return true;

                ValidationRule<ProcessResult>[] rules = new ValidationRule<ProcessResult>[ruleCount];
                for (int i = 0; i < ruleCount; i++)
                    rules[i] = new ValidationRule<ProcessResult>(r => true, $"cfg-{i}");

                ProcessExitConfiguration baseConfig = ProcessExitConfigurationCreationExtensions.WithValidationRules(
                    ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(ProcessExitConfiguration.CreateGraceful(), cap),
                    rules);

                // WithValidationRules replaces the rules but must preserve the cap.
                ProcessExitConfiguration afterRules = ProcessExitConfigurationCreationExtensions.WithValidationRules(baseConfig, rules);
                bool rulesPreserveCap = afterRules.MaxBufferedOutputBytes == cap
                                         && afterRules.ValidationRules.Length == ruleCount;

                // WithMaxBufferedOutputBytes overwrites the cap but must preserve the rules (same array reference).
                ProcessExitConfiguration afterCap = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(baseConfig, cap);
                bool capPreservesRules = afterCap.ValidationRules.Length == ruleCount
                                         && ReferenceEquals(afterCap.ValidationRules, baseConfig.ValidationRules);

                return rulesPreserveCap && capPreservesRules;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void UseOutputTruncation_PublishesCapToExitConfiguration_OverwriteSemantics()
    {
        // FsCheck 3.4 exposes a Func<T, Task<bool>> ForAll overload, so the middleware is awaited
        // directly inside the property. The terminal next completes synchronously.
        Prop.ForAll<long>(async maxSize =>
            {
                if (maxSize < 1 || maxSize > 8_000_000) return true;

                ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
                builder.UseOutputTruncation(new TruncationOptions { MaxBytes = maxSize });
                IProcessMiddleware middleware = builder.Build()[0];

                // Pre-set a different cap to prove the middleware overwrites rather than merges.
                ProcessExitConfiguration exit = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
                    ProcessExitConfiguration.CreateGraceful(), 1);
                InvocationContext context = new InvocationContext(
                    new ProcessConfiguration("dotnet", "--version"),
                    exit,
                    InvocationMode.Buffered,
                    CancellationToken.None);

                ProcessExitConfiguration? captured = null;
                Func<InvocationContext, Task> next = c =>
                {
                    captured = c.ExitConfiguration;
                    return Task.CompletedTask;
                };

                await middleware.InvokeAsync(context, next);

                return captured is not null && captured.MaxBufferedOutputBytes == maxSize;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void WithMaxBufferedOutputBytes_Equality_ReflectsCap()
    {
        // ProcessExitConfiguration overrides Equals and includes MaxBufferedOutputBytes, so two configs
        // built from CreateGraceful with caps a and b are equal iff a == b.
        Prop.ForAll<long, long>((a, b) =>
            {
                if (a < 0 || b < 0) return true;

                ProcessExitConfiguration configA = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
                    ProcessExitConfiguration.CreateGraceful(), a);
                ProcessExitConfiguration configB = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
                    ProcessExitConfiguration.CreateGraceful(), b);

                return configA.Equals(configB) == (a == b);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void WithMaxBufferedOutputBytes_NullCap_Equality()
    {
        ProcessExitConfiguration nullCapA = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), null);
        ProcessExitConfiguration nullCapB = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), null);
        ProcessExitConfiguration capped = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), 1000);

        if (!nullCapA.Equals(nullCapB))
            throw new InvalidOperationException("Two null-cap configurations were not equal.");

        if (nullCapA.Equals(capped))
            throw new InvalidOperationException("A null-cap configuration was equal to a capped configuration.");
    }

    [Test]
    public void WithMaxBufferedOutputBytes_NegativeCap_IsStoredCorrectly()
    {
        // Negative caps are stored on the configuration and treated as no-cap at runtime
        // (in ReadStreamCappedAsync). The configuration equality compares raw values,
        // so a negative cap is distinct from null.
        ProcessExitConfiguration negativeCap = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), -1);

        if (negativeCap.MaxBufferedOutputBytes != -1)
            throw new InvalidOperationException("Negative cap value was not preserved on the configuration.");
    }

    [Test]
    public void WithMaxBufferedOutputBytes_ZeroCap_IsDistinctFromNullCap()
    {
        // 0 is a valid zero-byte cap and must be distinct from null (no cap).
        ProcessExitConfiguration nullCap = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), null);
        ProcessExitConfiguration zeroCap = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), 0);

        if (zeroCap.MaxBufferedOutputBytes != 0)
            throw new InvalidOperationException("Zero cap value was not preserved on the configuration.");

        if (nullCap.Equals(zeroCap))
            throw new InvalidOperationException("A null-cap and zero-cap configuration should not be equal.");
    }

    [Test]
    public void WithMaxBufferedOutputBytes_CapBoundaryValues_RoundTrip()
    {
        // Verify that common cap boundary values (0, 1, 8192, etc.) round-trip correctly.
        long[] boundaryValues = [0, 1, 8191, 8192, 8193, 65536, 1_000_000];

        foreach (long cap in boundaryValues)
        {
            ProcessExitConfiguration original = ProcessExitConfiguration.CreateGraceful();
            ProcessExitConfiguration updated = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(original, cap);

            if (updated.MaxBufferedOutputBytes != cap)
                throw new InvalidOperationException($"Cap value {cap} did not round-trip correctly.");
        }
    }

    [Test]
    public void UseOutputTruncation_WithZeroCap_PublishesZeroToExitConfiguration()
    {
        ProcessMiddlewareBuilder builder = new(_ => throw new InvalidOperationException("Not expected"));
        builder.UseOutputTruncation(new TruncationOptions { MaxBytes = 0 });
        IProcessMiddleware middleware = builder.Build()[0];

        ProcessExitConfiguration exit = ProcessExitConfiguration.CreateGraceful();
        InvocationContext context = new InvocationContext(
            new ProcessConfiguration("dotnet", "--version"),
            exit,
            InvocationMode.Buffered,
            CancellationToken.None);

        ProcessExitConfiguration? captured = null;
        Func<InvocationContext, Task> next = c =>
        {
            captured = c.ExitConfiguration;
            return Task.CompletedTask;
        };

        middleware.InvokeAsync(context, next).GetAwaiter().GetResult();

        if (captured is null)
            throw new InvalidOperationException("The next was not invoked.");

        if (captured.MaxBufferedOutputBytes != 0)
            throw new InvalidOperationException($"Expected zero cap but got {captured.MaxBufferedOutputBytes}.");
    }
}
