/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Security;
using System.Runtime.Versioning;
using CliInvoke.Exceptions;

using Assert = Xunit.Assert;

namespace CliInvoke.Tests.Primitives;

public class ProcessExceptionInfoTests
{
    [SupportedOSPlatform("windows")]
    [Fact]
    public void Dispose_ShouldNotDisposeCallerOwnedCredential()
    {
        // Arrange
        SecureString password = new();
        password.AppendChar('p');
        password.MakeReadOnly();

        UserCredential credential = new("domain", "username", password, true);

        ProcessConfiguration configuration = new("cmd", "/c exit",
            credential: credential);
        ProcessResult result = new("cmd", 0, 1234, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(1));

        ProcessExceptionInfo processExceptionInfo = new(result, configuration);

        // Act
        processExceptionInfo.Dispose();

        // Assert — the credential is caller-owned: ProcessExceptionInfo.Dispose()
        // must leave it fully usable (a disposed SecureString throws on read).
        Assert.Equal(1, credential.Password!.Length);
    }
}
