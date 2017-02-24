// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up authentication services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthentication(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<ISystemClock, SystemClock>();
            services.AddDataProtection();
            services.AddWebEncoders();
            services.TryAddScoped<IAuthenticationService, DefaultAuthenticationService>();
            services.TryAddSingleton<IClaimsTransformation, DefaultClaimsTransformation>(); // Can be replaced with scoped ones that use DbContext
            services.TryAddScoped<IAuthenticationHandlerResolver, DefaultAuthenticationHandlerResolver>();
            services.TryAddSingleton<IAuthenticationSchemeProvider, DefaultAuthenticationSchemeProvider>();
            return services;
        }

        public static IServiceCollection AddAuthentication(this IServiceCollection services, Action<AuthenticationOptions> configureOptions) {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.AddAuthentication();
            services.Configure(configureOptions);
            return services;
        }

        public static IServiceCollection AddScheme<TOptions, THandler>(this IServiceCollection services, string authenticationScheme, Action<TOptions> configureOptions, bool canHandleRequests)
            where TOptions : AuthenticationSchemeOptions, new()
            where THandler : AuthenticationHandler<TOptions>
        {
            services.AddAuthentication(o =>
            {
                o.AddScheme(authenticationScheme, b =>
                {
                    b.HandlerType = typeof(THandler);
                    b.CanHandleRequests = canHandleRequests;
                    var options = new TOptions();

                    // REVIEW: is there a better place for this default?
                    options.DisplayName = authenticationScheme;
                    options.ClaimsIssuer = authenticationScheme;

                    configureOptions?.Invoke(options);
                    options.Validate();

                    // revisit the settings typing
                    b.Settings["Options"] = options;
                });
            });
            services.AddTransient<THandler>();
            return services;
        }

        public static IServiceCollection AddScheme<TOptions, THandler>(this IServiceCollection services, string authenticationScheme, TOptions options, bool canHandleRequests)
            where TOptions : AuthenticationSchemeOptions, new()
            where THandler : AuthenticationHandler<TOptions>
        {
            services.AddAuthentication(o =>
            {
                o.AddScheme(authenticationScheme, b =>
                {
                    b.HandlerType = typeof(THandler);
                    b.CanHandleRequests = canHandleRequests;
                    b.Settings["Options"] = options;
                });
            });
            services.AddTransient<THandler>();
            return services;
        }

        // REVIEW: rename to just ConfigureScheme?
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="services"></param>
        /// <param name="authenticationScheme"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigureSchemeHandler<TOptions>(this IServiceCollection services, string authenticationScheme, Action<TOptions> configureOptions)
            where TOptions : AuthenticationSchemeOptions, new()
        {
            services.Configure<AuthenticationOptions>(o =>
            {
                if (o.SchemeMap.ContainsKey(authenticationScheme))
                {
                    var options = o.SchemeMap[authenticationScheme].Settings["Options"] as TOptions;
                    if (options == null)
                    {
                        throw new InvalidOperationException("Unable to find options in authenticationScheme settings for: " + authenticationScheme);
                    }
                    configureOptions?.Invoke(options);
                }
                else
                {
                    throw new InvalidOperationException("No scheme registered for " + authenticationScheme);
                }

            });
            return services;
        }
    }
}
