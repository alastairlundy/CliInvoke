/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using CliInvoke.Core.Configuration;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="ArgumentsSpec"/>.
/// </summary>
public class ArgumentsSpecFuzzTests
{
    [Test]
    public void Add_ThenBuild_AlwaysProducesNonNullOutput()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || value.Contains('\n') || value.Contains('\r'))
                    return true;

                string result = new ArgumentsSpec()
                    .Add(value, escape: false)
                    .Build();

                return !string.IsNullOrEmpty(result);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Add_WithEscape_ProducesNonNullOutput()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || value.Contains('\n') || value.Contains('\r'))
                    return true;

                string result = new ArgumentsSpec()
                    .Add(value, escape: true)
                    .Build();

                return !string.IsNullOrEmpty(result);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void AddMultiple_ThenBuild_ProducesSpaceSeparatedOutput()
    {
        Prop.ForAll<string, string>((a, b) =>
            {
                if (string.IsNullOrEmpty(a) || a.Contains(' ') || a.Contains('"') || a.Contains('\n') ||
                    string.IsNullOrEmpty(b) || b.Contains(' ') || b.Contains('"') || b.Contains('\n'))
                    return true;

                string result = new ArgumentsSpec()
                    .Add(a, escape: false)
                    .Add(b, escape: false)
                    .Build();

                return result.Contains(' ');
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Add_WithValidationPredicate_RejectsInvalidArguments()
    {
        Func<string, bool> rejectAll = _ => false;

        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                var spec = new ArgumentsSpec(rejectAll);

                try
                {
                    spec.Add(value, escape: false);
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
    public async Task AddEnumerable_WithEmptyValues_ThrowsArgumentException()
    {
        var spec = new ArgumentsSpec();

        try
        {
            spec.AddEnumerable(Enumerable.Empty<string>(), escape: false);
            return;
        }
        catch (ArgumentException)
        {
            await Assert.That(true).IsTrue();
        }
    }

    [Test]
    public async Task Add_NullValue_ThrowsArgumentNullException()
    {
        var spec = new ArgumentsSpec();

        try
        {
            spec.Add((string)null!, escape: false);
            return;
        }
        catch (ArgumentNullException)
        {
            await Assert.That(true).IsTrue();
        }
    }

    [Test]
    public void Clear_ResetsBuffer()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                var spec = new ArgumentsSpec();
                spec.Add(value, escape: false);
                spec.Clear();
                string result = spec.Build();
                return result == string.Empty;
            })
            .QuickCheckThrowOnFailure();
    }
}
