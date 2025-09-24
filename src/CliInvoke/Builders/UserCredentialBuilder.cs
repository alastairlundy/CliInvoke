/*
    AlastairLundy.CliInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Diagnostics.Contracts;
using System.Security;

using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable PossibleInvalidOperationException
// ReSharper disable PossibleNullReferenceException
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace AlastairLundy.CliInvoke.Builders;

#nullable enable

/// <summary>
/// A class that provides builder methods for constructing UserCredentials.
/// </summary>
public class UserCredentialBuilder : IUserCredentialBuilder
{
    private readonly UserCredential? _userCredential;

    /// <summary>
    /// Instantiates the UserCredentialBuilder class.
    /// </summary>
    public UserCredentialBuilder()
    {
        _userCredential = new UserCredential();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential"></param>
    protected UserCredentialBuilder(UserCredential credential)
    {
        _userCredential = credential;
    }
    
    /// <summary>
    /// Sets the domain for the credential to be created.
    /// </summary>
    /// <param name="domain">The domain to set.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated domain.</returns>
    [Pure]
    public IUserCredentialBuilder SetDomain(string? domain) =>
        new UserCredentialBuilder(
            new UserCredential(domain,
                    _userCredential.UserName,
#pragma warning disable CA1416
                    _userCredential.Password,
                    _userCredential.LoadUserProfile)
#pragma warning restore CA1416
            );

    /// <summary>
    /// Sets the username for the credential to be created.
    /// </summary>
    /// <param name="username">The username to set.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated username.</returns>
    [Pure]
    public IUserCredentialBuilder SetUsername(string? username) =>
        new UserCredentialBuilder
        (
#pragma warning disable CA1416
            new UserCredential(_userCredential.Domain,
                username,
                _userCredential.Password,
                _userCredential.LoadUserProfile)
#pragma warning restore CA1416
        );

    /// <summary>
    /// Sets the password for the credential to be created.
    /// </summary>
    /// <param name="password">The password to set, as a SecureString.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated password.</returns>
    [Pure]
    public IUserCredentialBuilder SetPassword(SecureString? password) =>
        new UserCredentialBuilder
        (
#pragma warning disable CA1416
            new UserCredential(_userCredential.Domain,
                _userCredential.UserName,
                password,
                _userCredential.LoadUserProfile)
#pragma warning restore CA1416
        );
        
    /// <summary>
    /// Specifies whether to load the user profile.
    /// </summary>
    /// <param name="loadUserProfile">True to load the user profile, false otherwise.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated load user profile setting.</returns>
    [Pure]
    public IUserCredentialBuilder LoadUserProfile(bool loadUserProfile) =>
        new UserCredentialBuilder
        (
#pragma warning disable CA1416
            new UserCredential(_userCredential.Domain,
                _userCredential.UserName,
                _userCredential.Password,
                loadUserProfile)
#pragma warning restore CA1416
        );

    /// <summary>
    /// Builds a new instance of UserCredentials using the current settings.
    /// </summary>
    /// <returns>The built UserCredentials.</returns>
    [Pure]
    public UserCredential Build() =>
#pragma warning disable CA1416
        new UserCredential(_userCredential.Domain,
            _userCredential.UserName,
            _userCredential.Password,
            _userCredential.LoadUserProfile);
#pragma warning restore CA1416
        
    /// <summary>
    /// Disposes of the provided settings.
    /// </summary>
    public void Dispose()
    {
        _userCredential.Dispose();
    }
}