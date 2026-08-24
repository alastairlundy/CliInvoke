/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     A typed dictionary wrapper for sharing data between middleware steps.
///     Backed by <see cref="IDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class MiddlewareItems
{
    private readonly Dictionary<string, object?> _items = new();

    /// <summary>
    ///     Gets a value by key, cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the value cannot be cast to <typeparamref name="T"/>.
    /// </exception>
    public T? Get<T>(string key)
    {
        if (!_items.TryGetValue(key, out object? value))
            throw new KeyNotFoundException($"Key '{key}' not found in middleware items.");

        if (value is null)
        {
            // default(T) is null for reference types and nullable value types,
            // but non-null for non-nullable value types like int.
            if (default(T) is not null)
                throw new InvalidOperationException(
                    $"Value for key '{key}' is null, but '{typeof(T).Name}' does not accept null.");

            return default;
        }

        if (value is not T typedValue)
            throw new InvalidOperationException(
                $"Value for key '{key}' is of type '{value.GetType().Name}', not '{typeof(T).Name}'.");

        return typedValue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGet<T>(string key, out T? value)
    {
        try
        {
            value = Get<T>(key);

            return true;
        }
        catch(KeyNotFoundException)
        {
            value = default;
            return false;
        }
    }
    
    /// <summary>
    ///     Sets a value by key.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string key, T value)
    {
        _items[key] = value;
    }
}