/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Security;

namespace CliInvoke.Builders;

/// <summary>
///     A class that provides builder methods for constructing UserCredentials.
/// </summary>
public class UserCredentialBuilder : IUserCredentialBuilder
{
    private string  _userName;
    private string _domain;
    private SecureString _userPassword;
    private bool  _loadUserProfile;
    
    /// <summary>
    ///     Instantiates the UserCredentialBuilder class.
    /// </summary>
    public UserCredentialBuilder()
    {
        _userName = string.Empty;
        _domain = string.Empty;
        _userPassword = new SecureString();
        _loadUserProfile = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="domain"></param>
    /// <param name="userPassword"></param>
    /// <param name="loadUserProfile"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public UserCredentialBuilder(string name, string domain, SecureString userPassword,
        bool loadUserProfile)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(domain);
        
        _userName = name;
        _domain = domain;
        _userPassword = userPassword;
        _loadUserProfile = loadUserProfile;
    }

    /// <summary>
    ///     Sets the domain for the credential to be created.
    /// </summary>
    /// <param name="domain">The domain to set.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated domain.</returns>
    public IUserCredentialBuilder SetDomain(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);

        _domain = domain;
        return this;
    }

    /// <summary>
    ///     Sets the username for the credential to be created.
    /// </summary>
    /// <param name="username">The username to set.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated username.</returns>
    public IUserCredentialBuilder SetUsername(string username)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        _userName = username;
        return this;
    }

    /// <summary>
    ///     Sets the password for the credential to be created.
    /// </summary>
    /// <param name="password">The password to set, as a SecureString.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated password.</returns>
    public IUserCredentialBuilder SetPassword(SecureString password)
    {
        ArgumentNullException.ThrowIfNull(password);
        
        if(password.Length == 0)
            throw new ArgumentException(Resources.Exceptions_Credentials_EmptyPassword,  nameof(password));
        
        _userPassword = password;
        return this;
    }

    /// <summary>
    ///     Specifies whether to load the user profile.
    /// </summary>
    /// <param name="loadUserProfile">True to load the user profile, false otherwise.</param>
    /// <returns>A new instance of the CredentialsBuilder with the updated load user profile setting.</returns>
    public IUserCredentialBuilder LoadUserProfile(bool loadUserProfile)
    { 
        _loadUserProfile = loadUserProfile;
        return this;
    }
        
    /// <summary>
    ///     Builds a new instance of UserCredentials using the current settings.
    /// </summary>
    /// <returns>The built UserCredentials.</returns>
    [Pure]
    public UserCredential Build() =>
        new(_domain, _userName, _userPassword, _loadUserProfile);

    public void Dispose()
    {
        _userPassword.Dispose();
        GC.SuppressFinalize(this);
    }
}