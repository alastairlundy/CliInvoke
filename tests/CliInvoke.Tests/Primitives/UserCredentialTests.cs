/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace CliInvoke.Tests.Primitives;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class UserCredentialTests
{
    private static SecureString MakeSecureString(string value)
    {
        SecureString secure = new();
        foreach (char c in value)
            secure.AppendChar(c);
        secure.MakeReadOnly();
        return secure;
    }

    [Test]
    public async Task DefaultConstructor_SetsAllFieldsNull()
    {
        // Act
        UserCredential credential = new();

        // Assert
        await Assert.That(credential.Domain).IsNull();
        await Assert.That(credential.UserName).IsNull();
        await Assert.That(credential.Password).IsNull();
        await Assert.That(credential.LoadUserProfile).IsNull();
    }

    [Test]
    public async Task ParameterizedConstructor_SetsProvidedFields()
    {
        // Arrange
        using SecureString password = MakeSecureString("hunter2");

        // Act
        UserCredential credential = new("CONTOSO", "alice", password, loadUserProfile: true);

        // Assert
        await Assert.That(credential.Domain).IsEqualTo("CONTOSO");
        await Assert.That(credential.UserName).IsEqualTo("alice");
        await Assert.That(credential.Password).IsSameReferenceAs(password);
        await Assert.That(credential.LoadUserProfile).IsTrue();
    }

    [Test]
    public async Task Null_Sentinel_HasAllNullFields()
    {
        // Assert
        await Assert.That(UserCredential.Null.Domain).IsNull();
        await Assert.That(UserCredential.Null.UserName).IsNull();
        await Assert.That(UserCredential.Null.Password).IsNull();
        await Assert.That(UserCredential.Null.LoadUserProfile).IsNull();
    }

    [Test]
    public async Task Equals_EqualCredentials_AreEqual()
    {
        // Arrange
        // UserCredential compares SecureString by reference, so equal credentials must share
        // the same SecureString instance.
        using SecureString password = MakeSecureString("secret");
        UserCredential a = new("DOM", "user", password, loadUserProfile: false);
        UserCredential b = new("DOM", "user", password, loadUserProfile: false);

        // Assert
        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
    }

    [Test]
    public async Task Equals_DifferentUserNames_AreNotEqual()
    {
        // Arrange
        UserCredential a = new("DOM", "userA", password: null, loadUserProfile: null);
        UserCredential b = new("DOM", "userB", password: null, loadUserProfile: null);

        // Assert
        await Assert.That(a.Equals(b)).IsFalse();
        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task Equals_DifferentDomains_AreNotEqual()
    {
        // Arrange
        UserCredential a = new("DOM1", "user", password: null, loadUserProfile: null);
        UserCredential b = new("DOM2", "user", password: null, loadUserProfile: null);

        // Assert
        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    public async Task Equals_DifferentPasswords_AreNotEqual()
    {
        // Arrange
        using SecureString passwordA = MakeSecureString("secret");
        using SecureString passwordB = MakeSecureString("different");
        UserCredential a = new(domain: null, "user", passwordA, loadUserProfile: null);
        UserCredential b = new(domain: null, "user", passwordB, loadUserProfile: null);

        // Assert
        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    public async Task Equals_DifferentLoadUserProfile_AreNotEqual()
    {
        // Arrange
        UserCredential a = new(domain: null, "user", password: null, loadUserProfile: true);
        UserCredential b = new(domain: null, "user", password: null, loadUserProfile: false);

        // Assert
        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    public async Task Equals_NullFieldVsNonNullField_AreNotEqual()
    {
        // Arrange
        UserCredential a = new(domain: null, "user", password: null, loadUserProfile: null);
        UserCredential b = new("DOM", "user", password: null, loadUserProfile: null);

        // Assert
        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    public async Task Equals_NullOther_ReturnsFalse()
    {
        // Arrange
        UserCredential a = new("DOM", "user", password: null, loadUserProfile: null);

        // Assert
        await Assert.That(a.Equals((UserCredential?)null)).IsFalse();
        await Assert.That(a == null).IsFalse();
        await Assert.That(a != null).IsTrue();
    }

    [Test]
    public async Task StaticEquals_BothNull_ReturnsTrue()
    {
        // Assert
        await Assert.That(UserCredential.Equals(left: null, right: null)).IsTrue();
    }

    [Test]
    public async Task StaticEquals_OneNull_ReturnsFalse()
    {
        // Arrange
        UserCredential a = new("DOM", "user", password: null, loadUserProfile: null);

        // Assert
        await Assert.That(UserCredential.Equals(a, right: null)).IsFalse();
        await Assert.That(UserCredential.Equals(left: null, a)).IsFalse();
    }

    [Test]
    public async Task Equals_NonUserCredentialObject_ReturnsFalse()
    {
        // Arrange
        UserCredential a = new("DOM", "user", password: null, loadUserProfile: null);

        // Assert
        await Assert.That(a.Equals((object)"not a credential")).IsFalse();
    }

    [Test]
    public async Task GetHashCode_TwoEqualCredentials_AreEqual()
    {
        // Arrange
        using SecureString password = MakeSecureString("secret");
        UserCredential a = new("DOM", "user", password, loadUserProfile: true);
        UserCredential b = new("DOM", "user", password, loadUserProfile: true);

        // Assert
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task GetHashCode_AllNullFields_ReturnsZero()
    {
        // Arrange
        UserCredential credential = new();

        // Assert
        await Assert.That(credential.GetHashCode()).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_DisposesHeldSecureString()
    {
        // Arrange
        using SecureString password = MakeSecureString("secret");
        UserCredential credential = new(domain: null, "user", password, loadUserProfile: null);

        // Assert - the credential holds the same SecureString reference (no copy is made)
        await Assert.That(credential.Password).IsSameReferenceAs(password);

        // Act - Dispose is idempotent and does not throw
        credential.Dispose();
        credential.Dispose();

        // Assert - the credential still exposes the (now disposed) reference
        await Assert.That(credential.Password).IsSameReferenceAs(password);
    }

    [Test]
    public async Task GetHashCode_DifferentPasswordsSameUserDomain_AreEqual()
    {
        // Password is excluded from GetHashCode — distinct passwords with the same
        // user/domain/loadProfile should produce the same hash code.
        using SecureString passwordA = MakeSecureString("secret");
        using SecureString passwordB = MakeSecureString("different");

        UserCredential a = new("DOM", "user", passwordA, loadUserProfile: true);
        UserCredential b = new("DOM", "user", passwordB, loadUserProfile: true);

        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task GetHashCode_DifferentDomains_Differ()
    {
        UserCredential a = new("DOM1", "user", password: null, loadUserProfile: null);
        UserCredential b = new("DOM2", "user", password: null, loadUserProfile: null);

        await Assert.That(a.GetHashCode()).IsNotEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task GetHashCode_DifferentUserNames_Differ()
    {
        UserCredential a = new("DOM", "userA", password: null, loadUserProfile: null);
        UserCredential b = new("DOM", "userB", password: null, loadUserProfile: null);

        await Assert.That(a.GetHashCode()).IsNotEqualTo(b.GetHashCode());
    }
}
