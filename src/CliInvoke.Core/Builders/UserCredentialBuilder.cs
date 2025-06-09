/*
    AlastairLundy.CliInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Security;
using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;


// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable PossibleInvalidOperationException
// ReSharper disable PossibleNullReferenceException
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace AlastairLundy.CliInvoke.Core.Builders;

#nullable enable

/// <summary>
/// A class that provides builder methods for constructing UserCredentials.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class UserCredentialBuilder : IUserCredentialBuilder
{
    private UserCredential? _userCredential;

    /// <summary>
    /// Instantiates the UserCredentialBuilder class.
    /// </summary>
    public UserCredentialBuilder()
    {
        _userCredential = new UserCredential();
    }
        
    /// <summary>
    /// Sets the domain for the credential to be created.
    /// </summary>
    /// <param name="domain">The domain to set.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated domain.</returns>
    [Pure]
    public IUserCredentialBuilder SetDomain(string? domain) =>
        new UserCredentialBuilder
        {
           _userCredential = new UserCredential(domain, _userCredential.UserName, _userCredential.Password,
                   _userCredential.LoadUserProfile)
        };

    /// <summary>
    /// Sets the username for the credential to be created.
    /// </summary>
    /// <param name="username">The username to set.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated username.</returns>
    [Pure]
    public IUserCredentialBuilder SetUsername(string? username) =>
        new UserCredentialBuilder
        {
            _userCredential = new UserCredential(_userCredential.Domain, username, _userCredential.Password,
                _userCredential.LoadUserProfile)
        };

    /// <summary>
    /// Sets the password for the credential to be created.
    /// </summary>
    /// <param name="password">The password to set, as a SecureString.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated password.</returns>
    [Pure]
    public IUserCredentialBuilder SetPassword(SecureString? password) =>
        new UserCredentialBuilder
        {
            _userCredential = new UserCredential(_userCredential.Domain, _userCredential.UserName, password,
                _userCredential.LoadUserProfile)
        };
        
    /// <summary>
    /// Specifies whether to load the user profile.
    /// </summary>
    /// <param name="loadUserProfile">True to load the user profile, false otherwise.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated load user profile setting.</returns>
    [Pure]
    public IUserCredentialBuilder LoadUserProfile(bool loadUserProfile) =>
        new UserCredentialBuilder
        {
            _userCredential = new UserCredential(_userCredential.Domain, _userCredential.UserName, _userCredential.Password,
                loadUserProfile)
        };

    /// <summary>
    /// Builds a new instance of UserCredentials using the current settings.
    /// </summary>
    /// <returns>The built UserCredentials.</returns>
    [Pure]
    public UserCredential Build() =>
        new UserCredential(_userCredential.Domain, _userCredential.UserName, _userCredential.Password,
            _userCredential.LoadUserProfile);
        
    /// <summary>
    /// Disposes of the provided settings.
    /// </summary>
    public void Dispose()
    {
        _userCredential.Dispose();
    }
}