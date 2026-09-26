/*
    CliInvoke

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Builders;

namespace CliInvoke.Internal;

/// <summary>
///     Internal derivation hook that produces a new validated
///     <see cref="ProcessConfiguration" /> by seeding a
///     <see cref="ProcessConfigurationBuilder" /> from a source configuration,
///     applying a caller-supplied delta, and invoking
///     <see cref="IProcessConfigurationBuilder.Build" />.
/// </summary>
internal static class ProcessConfigurationDerivation
{
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
