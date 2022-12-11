using Microsoft.Extensions.DependencyInjection;
using System;

namespace Authok.AspNetCore.Authentication
{
    /// <summary>
    /// Builder to add functionality on top of OpenId Connect authentication. 
    /// </summary>
    public class AuthokWebAppAuthenticationBuilder
    {
        private readonly IServiceCollection _services;
        private readonly AuthokWebAppOptions _options;
        private readonly string _authenticationScheme;

        /// <summary>
        /// Constructs an instance of <see cref="AuthokWebAppAuthenticationBuilder"/>
        /// </summary>
        /// <param name="services">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see> instance</param>
        /// <param name="options">The <see cref="AuthokWebAppOptions"/> used when calling AddAuthokWebAppAuthentication.</param>
        public AuthokWebAppAuthenticationBuilder(IServiceCollection services, AuthokWebAppOptions options) : this(services, AuthokConstants.AuthenticationScheme, options)
        {
        }

        /// <summary>
        /// Constructs an instance of <see cref="AuthokWebAppAuthenticationBuilder"/>
        /// </summary>
        /// <param name="services">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see> instance</param>
        /// <param name="authenticationScheme">The authentication scheme to use.</param>
        /// <param name="options">The <see cref="AuthokWebAppOptions"/> used when calling AddAuthokWebAppAuthentication.</param>
        public AuthokWebAppAuthenticationBuilder(IServiceCollection services, string authenticationScheme, AuthokWebAppOptions options)
        {
            _services = services;
            _options = options;
            _authenticationScheme = authenticationScheme;
        }

        /// <summary>
        /// Configures the use of Access Tokens
        /// </summary>
        /// <param name="configureOptions">A delegate used to configure the <see cref="AuthokWebAppWithAccessTokenOptions"/></param>
        /// <returns>An instance of <see cref="AuthokWebAppWithAccessTokenAuthenticationBuilder"/></returns>
        public AuthokWebAppWithAccessTokenAuthenticationBuilder WithAccessToken(Action<AuthokWebAppWithAccessTokenOptions> configureOptions)
        {
            return new AuthokWebAppWithAccessTokenAuthenticationBuilder(_services, configureOptions, _options, _authenticationScheme);
        }
    }
}
