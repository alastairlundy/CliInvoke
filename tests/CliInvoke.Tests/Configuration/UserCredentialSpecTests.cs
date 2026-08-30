/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security;

using CliInvoke.Core;
using CliInvoke.Core.Configuration;

namespace CliInvoke.Tests.Configuration;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class UserCredentialSpecTests
{
    private static SecureString MakeSecureString(string value)
    {
        SecureString secure = new SecureString();
        foreach (char c in value)
            secure.AppendChar(c);
        secure.MakeReadOnly();
        return secure;
    }

    [Test]
    public async Task DefaultConstructor_LeavesFieldsNull()
    {
        // Act
        UserCredentialSpec spec = new UserCredentialSpec();

        // Assert
        UserCredential built = spec.Build();
        await Assert.That(built.Domain).IsNull();
        await Assert.That(built.UserName).IsNull();
        await Assert.That(built.Password).IsNull();
        await Assert.That(built.LoadUserProfile).IsNull();
    }

    [Test]
    public async Task FluentSetters_ReturnSameInstance()
    {
        // Arrange
        UserCredentialSpec spec = new UserCredentialSpec();

        // Act
        UserCredentialSpec afterDomain = spec.SetDomain("DOM");
        UserCredentialSpec afterUser = afterDomain.SetUsername("alice");
        UserCredentialSpec afterProfile = afterUser.SetUserProfileLoading(true);

        // Assert
        await Assert.That(afterDomain).IsSameReferenceAs(spec);
        await Assert.That(afterUser).IsSameReferenceAs(spec);
        await Assert.That(afterProfile).IsSameReferenceAs(spec);
    }

    [Test]
    public async Task SetDomain_RejectsNullOrEmpty()
    {
        // Arrange
        UserCredentialSpec spec = new UserCredentialSpec();

        // Assert
        await Assert.That(() => spec.SetDomain(null!)).Throws<ArgumentException>();
        await Assert.That(() => spec.SetDomain(string.Empty)).Throws<ArgumentException>();
    }

    [Test]
    public async Task SetUsername_RejectsNullOrEmpty()
    {
        // Arrange
        UserCredentialSpec spec = new UserCredentialSpec();

        // Assert
        await Assert.That(() => spec.SetUsername(null!)).Throws<ArgumentException>();
        await Assert.That(() => spec.SetUsername(string.Empty)).Throws<ArgumentException>();
    }

    [Test]
    public async Task SetPassword_RejectsNull()
    {
        // Arrange
        UserCredentialSpec spec = new UserCredentialSpec();

        // Assert
        await Assert.That(() => spec.SetPassword(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Build_ProducesCredentialWithConfiguredValues()
    {
        // Arrange
        using SecureString password = MakeSecureString("hunter2");
        UserCredentialSpec spec = new UserCredentialSpec()
            .SetDomain("CONTOSO")
            .SetUsername("alice")
            .SetPassword(password)
            .SetUserProfileLoading(true);

        // Act
        UserCredential built = spec.Build();

        // Assert
        await Assert.That(built.Domain).IsEqualTo("CONTOSO");
        await Assert.That(built.UserName).IsEqualTo("alice");
        await Assert.That(built.Password).IsNotSameReferenceAs(password); // a copy is held
        await Assert.That(built.Password!.Length).IsEqualTo(password.Length);
        await Assert.That(built.LoadUserProfile).IsTrue();
    }

    [Test]
    public async Task SetPassword_OverwritesPreviousAndBuildReflectsLatest()
    {
        // Arrange
        using SecureString first = MakeSecureString("first");
        using SecureString second = MakeSecureString("second");
        UserCredentialSpec spec = new UserCredentialSpec().SetPassword(first);

        // Act
        spec.SetPassword(second);

        // Assert - Build reflects the latest password, not the first
        UserCredential built = spec.Build();
        await Assert.That(built.Password!.Length).IsEqualTo(second.Length);
        await Assert.That(built.Password).IsNotSameReferenceAs(second); // a copy is held
        // The caller-owned SecureStrings remain usable; the spec only disposes its own copies.
        await Assert.That(first.Length).IsEqualTo(5);
        await Assert.That(second.Length).IsEqualTo(6);
    }

    [Test]
    public async Task Dispose_InvalidatesSubsequentBuild()
    {
        // Arrange
        using SecureString password = MakeSecureString("secret");
        UserCredentialSpec spec = new UserCredentialSpec().SetPassword(password);

        // Act
        spec.Dispose();

        // Assert - the spec disposes its held copy, so a subsequent Build throws
        await Assert.That(() => spec.Build()).Throws<ObjectDisposedException>();
        // The caller-owned SecureString is NOT disposed by the spec.
        await Assert.That(password.Length).IsEqualTo(6);
    }

    [Test]
    public async Task Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        UserCredentialSpec spec = new UserCredentialSpec().SetPassword(MakeSecureString("secret"));

        // Act / Assert - should not throw on repeated dispose
        spec.Dispose();
        spec.Dispose();
        await Assert.That(true).IsTrue();
    }
}
