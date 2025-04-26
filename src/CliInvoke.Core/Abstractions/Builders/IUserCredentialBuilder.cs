/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Security;

using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Core.Abstractions.Builders
{
    public interface IUserCredentialBuilder : IDisposable
    {
        /// <summary>
        /// Sets the domain for the credential to be created.
        /// </summary>
        /// <param name="domain">The domain to set.</param>
        /// <returns>A new instance of the CredentialsBuilder with the updated domain.</returns>
        IUserCredentialBuilder SetDomain(string domain);

        /// <summary>
        /// Sets the username for the credential to be created.
        /// </summary>
        /// <param name="username">The username to set.</param>
        /// <returns>A new instance of the CredentialsBuilder with the updated username.</returns>
        IUserCredentialBuilder SetUsername(string username);

        /// <summary>
        /// Sets the password for the credential to be created.
        /// </summary>
        /// <param name="password">The password to set, as a SecureString.</param>
        /// <returns>A new instance of the CredentialsBuilder with the updated password.</returns>
        IUserCredentialBuilder SetPassword(SecureString? password);

        /// <summary>
        /// Specifies whether to load the user profile.
        /// </summary>
        /// <param name="loadUserProfile">True to load the user profile, false otherwise.</param>
        /// <returns>A new instance of the CredentialsBuilder with the updated load user profile setting.</returns>
        IUserCredentialBuilder LoadUserProfile(bool loadUserProfile);

        /// <summary>
        /// Builds a new instance of UserCredentials using the current settings.
        /// </summary>
        /// <returns>The built UserCredentials.</returns>
        UserCredential Build();
    }
}