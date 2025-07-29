/*
    AlastairLundy.DotPrimitives
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

    Based on Tyrrrz's CliWrap Credentials.cs
    https://github.com/Tyrrrz/CliWrap/blob/master/CliWrap/Credentials.c

     Constructor signature and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

// ReSharper disable NonReadonlyMemberInGetHashCode

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

using System;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using System.Security;


namespace AlastairLundy.CliInvoke.Core.Primitives;

/// <summary>
/// A class to represent a User Credential to be used with ProcessPrimitives.
/// </summary>
public class UserCredential : IEquatable<UserCredential>, IDisposable
{
    /// <summary>
    /// Instantiates the user credential with null values.
    /// </summary>
    public UserCredential()
    {
#pragma warning disable CA1416
        Domain = null;
        UserName = null;
        Password = null;
        LoadUserProfile = false;
#pragma warning restore CA1416
    }

    /// <summary>
    /// Instantiates user credential with the specified values.
    /// </summary>
    /// <param name="domain">The domain to be used.</param>
    /// <param name="username">The username to be used.</param>
    /// <param name="password">The password to be used.</param>
    /// <param name="loadUserProfile">Whether to load the user profile during Process creation.</param>
    public UserCredential(string? domain, string? username, SecureString? password, bool? loadUserProfile)
    {
#pragma warning disable CA1416
        Domain = domain;
        UserName = username;
        Password = password;
        LoadUserProfile = loadUserProfile;
#pragma warning restore CA1416
    }
        
    /// <summary>
    /// The domain to be used.
    /// </summary>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public string? Domain { get; private set; }
        
    /// <summary>
    /// The username to be used.
    /// </summary>
    public string? UserName { get; private set; }
        
    /// <summary>
    /// The password to be used.
    /// </summary>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public SecureString? Password { get; private set; }
        
    /// <summary>
    /// Whether to load the UserCredential information and user profile.
    /// </summary>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public bool? LoadUserProfile { get; private set; }
        
    /// <summary>
    /// A null UserCredential instance.
    /// </summary>
    public static UserCredential Null { get; } = new UserCredential();

    /// <summary>
    /// Disposes of the Password SecureString and other UserCredential values.
    /// </summary>
    public void Dispose()
    {
#pragma warning disable CA1416
        Domain = string.Empty;
        UserName = string.Empty;
        LoadUserProfile = false;
        Password?.Dispose();
#pragma warning restore CA1416
    }

    /// <summary>
    /// Determines whether the specified user credential is equal to the current user credential.
    /// </summary>
    /// <param name="other">The user credential to compare with the current user credential.</param>
    /// <returns>True if the specified user credential is equal to the current user credential; false otherwise.</returns>
    public bool Equals(UserCredential? other)
    {
#pragma warning disable CA1416
        if (other is null)
        {
            return false;
        }

        if (other.UserName is null || other.Domain is null || other.Password is null ||
            other.LoadUserProfile is null)
        {
            return false;
        }
            
        return Domain == other.Domain &&
               UserName == other.UserName &&
#pragma warning disable CS8602 // Dereference of a possibly null reference.
               Password.Equals(other.Password)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
               && LoadUserProfile == other.LoadUserProfile;
#pragma warning restore CA1416

    }

    /// <summary>
    /// Determines whether one User Credential is equal to another.
    /// </summary>
    /// <param name="left">The first user credential to compare.</param>
    /// <param name="right">The second user credential to compare.</param>
    /// <returns>True if the two user credential objects are equal; false otherwise.</returns>
    public static bool Equals(UserCredential? left, UserCredential? right)
    {
        if (left is null || right is null)
        {
            return false;
        }
            
        return left.Equals(right);
    }
        
    /// <summary>
    /// Determines whether the specified object is equal to the current user credential.
    /// </summary>
    /// <param name="obj">The object to compare with the current user credential.</param>
    /// <returns>True if the specified object is equal to the current user credential; false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is UserCredential other)
        {
            return Equals(other);
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the hash code for the current user credential.
    /// </summary>
    /// <returns>The hash code for the current user credential.</returns>
    public override int GetHashCode()
    {
#pragma warning disable CA1416
        return HashCode.Combine(Domain,
            UserName,
            Password,
            LoadUserProfile);
#pragma warning restore CA1416
    }

    /// <summary>
    /// Determines if a UserCredential is equal to another UserCredential.
    /// </summary>
    /// <param name="left">A UserCredential to be compared.</param>
    /// <param name="right">The other UserCredential to be compared.</param>
    /// <returns>True if both UserCredentials are equal to each other, false otherwise.</returns>
    public static bool operator ==(UserCredential? left, UserCredential? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines if a UserCredential is not equal to another UserCredential.
    /// </summary>
    /// <param name="left">A UserCredential to be compared.</param>
    /// <param name="right">The other UserCredential to be compared.</param>
    /// <returns>True if both UserCredentials are not equal to each other; false otherwise.</returns>
    public static bool operator !=(UserCredential? left, UserCredential? right)
    {
        return Equals(left, right) == false;
    }
}