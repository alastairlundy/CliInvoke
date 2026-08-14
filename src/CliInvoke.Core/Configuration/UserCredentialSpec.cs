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

namespace CliInvoke.Core.Configuration;

/// <summary>
///     A sealed configuration seam for constructing user credentials,
///     replacing the former UserCredentialBuilder / IUserCredentialBuilder pair.
/// </summary>
public sealed class UserCredentialSpec : IDisposable
{
    private string? _domain;
    private string? _userName;
    private SecureString? _password;
    private bool? _loadUserProfile;
    private bool _disposed;

    /// <summary>
    ///     Instantiates the <see cref="UserCredentialSpec" /> class with null defaults.
    /// </summary>
    public UserCredentialSpec()
    {
        _domain = null;
        _userName = null;
        _password = null;
        _loadUserProfile = null;
    }

    /// <summary>
    ///     Sets the domain for the credential to be created.
    /// </summary>
    /// <param name="domain">The domain to set.</param>
    /// <returns>The current <see cref="UserCredentialSpec" /> instance.</returns>
    public UserCredentialSpec SetDomain(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);

        _domain = domain;
        return this;
    }

    /// <summary>
    ///     Sets the username for the credential to be created.
    /// </summary>
    /// <param name="username">The username to set.</param>
    /// <returns>The current <see cref="UserCredentialSpec" /> instance.</returns>
    public UserCredentialSpec SetUsername(string username)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        _userName = username;
        return this;
    }

    /// <summary>
    ///     Sets the password for the credential to be created.
    /// </summary>
    /// <param name="password">The password to set, as a SecureString.</param>
    /// <returns>The current <see cref="UserCredentialSpec" /> instance.</returns>
    public UserCredentialSpec SetPassword(SecureString password)
    {
        ArgumentNullException.ThrowIfNull(password);

        _password = password;
        return this;
    }

    /// <summary>
    ///     Specifies whether to load the user profile.
    /// </summary>
    /// <param name="loadUserProfile">True to load the user profile, false otherwise.</param>
    /// <returns>The current <see cref="UserCredentialSpec" /> instance.</returns>
    public UserCredentialSpec SetUserProfileLoading(bool loadUserProfile)
    {
        _loadUserProfile = loadUserProfile;
        return this;
    }

    /// <summary>
    ///     Builds a new instance of <see cref="UserCredential" /> using the current settings.
    /// </summary>
    /// <returns>The built <see cref="UserCredential" />.</returns>
    public UserCredential Build() =>
        new(_domain, _userName, _password, _loadUserProfile);

    /// <summary>
    ///     Disposes of the held <see cref="SecureString" /> and suppresses finalization.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _password?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
