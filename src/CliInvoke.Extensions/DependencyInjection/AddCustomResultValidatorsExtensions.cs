/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Validation;
using CliInvoke.Validation;

namespace CliInvoke.Extensions;

/// <summary>
/// Provides extension methods to customize result validators for CliInvoke.
/// </summary>
public static class AddCustomResultValidatorsExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds validation rules to the service collection for a specific process result type.
        /// </summary>
        /// <typeparam name="TProcessResult">
        /// The type of the process result being validated, which must inherit from the 'ProcessResult' class.
        /// </typeparam>
        /// <param name="validationRules">
        /// An array of functions that take a TProcessResult as input and return a boolean indicating whether
        /// the validation passes or fails for that specific rule. Each function represents a single validation rule.
        /// </param>
        /// <param name="serviceLifetime">
        /// The lifetime scope for the validator service in the service collection. Valid values are Scoped,
        /// Singleton, and Transient. Defaults to Scoped if not specified.
        /// </param>
        /// <returns>
        /// The modified IServiceCollection object, which now includes the added validator services for validation rules.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if an invalid value is provided for 'serviceLifetime'. Valid values are Scoped,
        /// Singleton, and Transient. If an unsupported or unknown value is passed, this exception will be thrown.
        /// </exception>
        public IServiceCollection AddValidationRules<TProcessResult>(
            Func<TProcessResult, bool>[] validationRules,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TProcessResult : ProcessResult
        {
            services.RemoveAll<IProcessResultValidator<TProcessResult>>();
            
            switch (serviceLifetime)
            {
                case ServiceLifetime.Scoped:
                    services.TryAddScoped<IProcessResultValidator<TProcessResult>>(_ => 
                        new ProcessResultValidator<TProcessResult>(validationRules));
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<IProcessResultValidator<TProcessResult>>(_ =>
                        new ProcessResultValidator<TProcessResult>(validationRules));
                    break;
                case ServiceLifetime.Transient:
                    services.TryAddTransient<IProcessResultValidator<TProcessResult>>(_ =>
                        new ProcessResultValidator<TProcessResult>(validationRules));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
            }
            
            return services;
        }

        /// <summary>
        /// Adds custom result validators to the service collection.
        /// </summary>
        /// <typeparam name="TProcessResult">
        /// The type of the process result being validated, which must inherit from the 'ProcessResult' class.
        /// </typeparam>
        /// <typeparam name="TProcessResultValidator">
        /// The type of validator implementing the IProcessResultValidator interface for the given process result type.
        /// </typeparam>
        /// <param name="validator">
        /// An instance of the validator to be added to the service collection.
        /// </param>
        /// <param name="serviceLifetime">
        /// The lifetime scope for the validator service.
        /// </param>
        /// <returns>
        /// The modified IServiceCollection object, which now includes the added validator service.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if an invalid value for 'serviceLifetime' is provided. Valid values are Scoped, Singleton, and Transient.
        /// </exception>
        public IServiceCollection
            AddCustomResultValidators<TProcessResult, TProcessResultValidator>(
                TProcessResultValidator validator,
                ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TProcessResultValidator : class, IProcessResultValidator<TProcessResult>
            where TProcessResult : ProcessResult
        {
            services = services.RemoveAll<IProcessResultValidator<TProcessResult>>();

            switch (serviceLifetime)
            {
                case ServiceLifetime.Scoped:
                    services.TryAddScoped<IProcessResultValidator<TProcessResult>, TProcessResultValidator>();
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<IProcessResultValidator<TProcessResult>,
                        TProcessResultValidator>();
                    break;
                case ServiceLifetime.Transient:
                    services.TryAddTransient<IProcessResultValidator<TProcessResult>, TProcessResultValidator>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
            }
            return services;
        }
    }
}