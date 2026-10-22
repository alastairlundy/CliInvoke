/*
    CliInvoke

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Builders;

namespace CliInvoke.Internal;

/// <summary>
///     Internal derivation hook that produces a new validated
///     <see cref="ProcessConfiguration" /> by seeding a
///     <see cref="ProcessConfigurationBuilder" /> from a source configuration,
///     applying a caller-supplied delta, and invoking
///     <see cref="IProcessConfigurationBuilder.Build" />. Also hosts the pipeline's
///     Raw-mode redirection normalization, which is a faithful member-wise copy rather
///     than a builder round-trip (builder validation is stricter than direct
///     construction and would reject configurations the pipeline must accept).
/// </summary>
internal static class ProcessConfigurationDerivation
{
    /// <summary>
    ///     Produces a faithful member-wise copy of <paramref name="source" /> with
    ///     <see cref="ProcessConfiguration.OutputRedirection" /> forced off, without
    ///     re-running builder validation.
    /// </summary>
    /// <remarks>
    ///     Used by the invocation pipeline to normalize Raw-mode invocations against the
    ///     raw-mode pipe deadlock (Raw waits for exit without draining redirected output,
    ///     so a redirected pipe nobody reads blocks the child). This deliberately bypasses
    ///     the builder-based <see cref="Derive" /> path: builder seeding re-runs
    ///     <c>EnvironmentVariablesSpec</c> validation, which rejects the null values that
    ///     <see cref="ProcessConfiguration.EnvironmentVariables" /> documents as
    ///     variable-removal semantics, and it normalizes
    ///     <see cref="ProcessConfiguration.StandardInput" /> — a builder round-trip would
    ///     therefore narrow which configurations the pipeline accepts. Resource-owning
    ///     members (e.g. <see cref="ProcessConfiguration.StandardInput" />) are
    ///     reference-copied, matching <see cref="Derive" />'s own copying semantics.
    /// </remarks>
    /// <param name="source">The source configuration to copy from.</param>
    /// <returns>A copy of <paramref name="source" /> with output redirection disabled.</returns>
    internal static ProcessConfiguration WithOutputRedirectionDisabled(
        ProcessConfiguration source)
    {
        return new ProcessConfiguration
        {
            TargetFilePath = source.TargetFilePath,
            WorkingDirectoryPath = source.WorkingDirectoryPath,
            Arguments = source.Arguments,
            ArgumentList = source.ArgumentList,
            WindowCreation = source.WindowCreation,
            EnvironmentVariables = source.EnvironmentVariables,
            RequiresAdministrator = source.RequiresAdministrator,
            Credential = source.Credential,
            UseShellExecution = source.UseShellExecution,
            StandardInput = source.StandardInput,
            RedirectStandardInput = source.RedirectStandardInput,
            OutputRedirection = false,
            ResourcePolicy = source.ResourcePolicy,
            StandardInputEncoding = source.StandardInputEncoding,
            StandardOutputEncoding = source.StandardOutputEncoding,
            StandardErrorEncoding = source.StandardErrorEncoding,
        };
    }

    /// <summary>
    ///     Derives a new <see cref="ProcessConfiguration" /> from
    ///     <paramref name="source" /> with the overrides specified by
    ///     <paramref name="configure" />.
    /// </summary>
    /// <remarks>
    ///     Resource-owning members (e.g. <see cref="ProcessConfiguration.StandardInput" />)
    ///     are reference-copied; the derived configuration shares the same
    ///     instance as the source. Construction validation re-runs via
    ///     <see cref="IProcessConfigurationBuilder.Build" />.
    /// </remarks>
    /// <param name="source">The source configuration to copy from.</param>
    /// <param name="configure">
    ///     An action that applies delta overrides to the seeded builder.
    /// </param>
    /// <returns>A new, validated <see cref="ProcessConfiguration" />.</returns>
    internal static ProcessConfiguration Derive(
        ProcessConfiguration source,
        Action<IProcessConfigurationBuilder> configure)
    {
        ProcessConfigurationBuilder builder = new(source);
        try
        {
            configure(builder);
            return builder.Build();
        }
        finally
        {
            builder.Dispose();
        }
    }
}
