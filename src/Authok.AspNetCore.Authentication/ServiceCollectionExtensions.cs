using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Authok.AspNetCore.Authentication
{
    /// <summary>
    /// Contains <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see> extension(s) for registering Authok.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Authok configuration using Open ID Connect
        /// </summary>
        /// <param name="services">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see> instance</param>
        /// <param name="configureOptions">A delegate used to configure the <see cref="AuthokWebAppOptions"/></param>
        /// <returns>The <see cref="AuthokWebAppAuthenticationBuilder"/> instance that has been created.</returns>
        public static AuthokWebAppAuthenticationBuilder AddAuthokWebAppAuthentication(this IServiceCollection services, Action<AuthokWebAppOptions> configureOptions)
        {
            return services.AddAuthokWebAppAuthentication(AuthokConstants.AuthenticationScheme, configureOptions);
        }

        /// <summary>
        /// Add Authok configuration using Open ID Connect
        /// </summary>
        /// <param name="services">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see> instance</param>
        /// <param name="configureOptions">A delegate used to configure the <see cref="AuthokWebAppOptions"/></param>
        /// <returns>The <see cref="AuthokWebAppAuthenticationBuilder"/> instance that has been created.</returns>
        public static AuthokWebAppAuthenticationBuilder AddAuthokWebAppAuthentication(this IServiceCollection services, string authenticationScheme, Action<AuthokWebAppOptions> configureOptions)
        {
            return services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddAuthokWebAppAuthentication(authenticationScheme, configureOptions);
        }
    }
}
