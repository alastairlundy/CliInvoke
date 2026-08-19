/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Middleware;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Extension methods for adding platform shell middleware to a <see cref="IProcessMiddlewareBuilder"/>.
/// </summary>
public static class PlatformMiddlewareExtensions
{
      /// <param name="builder">The middleware builder.</param>
      extension(IProcessMiddlewareBuilder builder)
      {
          /// <summary>
          ///     Adds <see cref="PowerShellMiddleware"/> to the process invocation pipeline.
          /// </summary>
          /// <remarks>
          ///     Configure <see cref="PowerShellMiddlewareOptions"/> via the dependency injection
          ///     container to customise window-creation and shell-execution behaviour.
          ///     When no options are registered, <see cref="PowerShellMiddlewareOptions.Default"/> is used.
          /// </remarks>
          /// <returns>The builder for fluent chaining.</returns>
          /// <exception cref="ArgumentNullException">
          ///     Thrown when <paramref name="builder"/> is <c>null</c>.
          /// </exception>
          [SupportedOSPlatform("windows")]
          [SupportedOSPlatform("macos")]
          [SupportedOSPlatform("maccatalyst")]
          [SupportedOSPlatform("linux")]
          [SupportedOSPlatform("freebsd")]
          [UnsupportedOSPlatform("browser")]
          [UnsupportedOSPlatform("android")]
          [UnsupportedOSPlatform("ios")]
          [UnsupportedOSPlatform("tvos")]
          [UnsupportedOSPlatform("watchos")]
          public IProcessMiddlewareBuilder UsePowerShell()
          {
              ArgumentNullException.ThrowIfNull(builder);

              builder.UseMiddleware<PowerShellMiddleware>();

              return builder;
          }

          /// <summary>
          ///     Adds <see cref="CmdMiddleware"/> to the process invocation pipeline.
          /// </summary>
          /// <returns>The builder for fluent chaining.</returns>
          /// <exception cref="ArgumentNullException">
          ///     Thrown when <paramref name="builder"/> is <c>null</c>.
          /// </exception>
          [SupportedOSPlatform("windows")]
          [UnsupportedOSPlatform("macos")]
          [UnsupportedOSPlatform("linux")]
          [UnsupportedOSPlatform("freebsd")]
          [UnsupportedOSPlatform("browser")]
          [UnsupportedOSPlatform("android")]
          [UnsupportedOSPlatform("ios")]
          [UnsupportedOSPlatform("tvos")]
          [UnsupportedOSPlatform("watchos")]
          public IProcessMiddlewareBuilder UseCmd()
          {
              ArgumentNullException.ThrowIfNull(builder);

              builder.UseMiddleware<CmdMiddleware>();

              return builder;
          }
      }
}
