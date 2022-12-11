using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;

namespace Authok.AspNetCore.Authentication
{
    /// <summary>
    /// Contains <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">AuthenticationBuilder</see> extension(s) for registering Authok.
    /// </summary>
    public static class AuthenticationBuilderExtensions
    {
        private static readonly IList<string> CodeResponseTypes = new List<string>() {
            OpenIdConnectResponseType.Code,
            OpenIdConnectResponseType.CodeIdToken
        };

        /// <summary>
        /// Add Authok configuration using Open ID Connect
        /// </summary>
        /// <param name="builder">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">AuthenticationBuilder</see> instance</param>
        /// <param name="configureOptions">A delegate used to configure the <see cref="AuthokWebAppOptions"/></param>
        /// <returns>The <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">AuthenticationBuilder</see> instance that has been configured.</returns>

        public static AuthokWebAppAuthenticationBuilder AddAuthokWebAppAuthentication(this AuthenticationBuilder builder, Action<AuthokWebAppOptions> configureOptions)
        {
            return AddAuthokWebAppAuthentication(builder, AuthokConstants.AuthenticationScheme, configureOptions);
        }

        /// <summary>
        /// Add Authok configuration using Open ID Connect
        /// </summary>
        /// <param name="builder">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">AuthenticationBuilder</see> instance</param>
        /// <param name="authenticationScheme">The authentication scheme to use.</param>
        /// <param name="configureOptions">A delegate used to configure the <see cref="AuthokWebAppOptions"/></param>
        /// <returns>The <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">AuthenticationBuilder</see> instance that has been configured.</returns>

        public static AuthokWebAppAuthenticationBuilder AddAuthokWebAppAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<AuthokWebAppOptions> configureOptions)
        {
            var authokOptions = new AuthokWebAppOptions();

            configureOptions(authokOptions);
            ValidateOptions(authokOptions);

            builder.AddOpenIdConnect(authenticationScheme, options => ConfigureOpenIdConnect(options, authokOptions));

            if (!authokOptions.SkipCookieMiddleware)
            {
                builder.AddCookie();
            }

            builder.Services.Configure(authenticationScheme, configureOptions);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenIdConnectOptions>, AuthokOpenIdConnectPostConfigureOptions>());

            return new AuthokWebAppAuthenticationBuilder(builder.Services, authenticationScheme, authokOptions);
        }

        /// <summary>
        /// Configure Open ID Connect based on the provided <see cref="AuthokWebAppOptions"/>.
        /// </summary>
        /// <param name="oidcOptions">A reference to the <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.openidconnect.openidconnectoptions">OpenIdConnectOptions</see> that needs to be configured.</param>
        /// <param name="authokOptions">The provided <see cref="AuthokWebAppOptions"/>.</param>
        private static void ConfigureOpenIdConnect(OpenIdConnectOptions oidcOptions, AuthokWebAppOptions authokOptions)
        {
            oidcOptions.Authority = $"https://{authokOptions.Domain}";
            oidcOptions.ClientId = authokOptions.ClientId;
            oidcOptions.ClientSecret = authokOptions.ClientSecret;
            oidcOptions.Scope.Clear();
            oidcOptions.Scope.AddRange(authokOptions.Scope.Split(" "));
            oidcOptions.CallbackPath = new PathString(authokOptions.CallbackPath ?? AuthokConstants.DefaultCallbackPath);
            oidcOptions.SaveTokens = true;
            oidcOptions.ResponseType = authokOptions.ResponseType ?? oidcOptions.ResponseType;
            oidcOptions.Backchannel = authokOptions.Backchannel!;
            oidcOptions.MaxAge = authokOptions.MaxAge;

            if (!oidcOptions.Scope.Contains("openid"))
            {
                oidcOptions.Scope.Add("openid");
            }

            oidcOptions.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                ValidateAudience = true,
                ValidAudience = authokOptions.ClientId,
                ValidateIssuer = true,
                ValidIssuer = $"https://{authokOptions.Domain}/",
                ValidateLifetime = true,
                RequireExpirationTime = true,
            };

            oidcOptions.Events = OpenIdConnectEventsFactory.Create(authokOptions);
        }

        private static void ValidateOptions(AuthokWebAppOptions authokOptions)
        {
            if (CodeResponseTypes.Contains(authokOptions.ResponseType!) && string.IsNullOrWhiteSpace(authokOptions.ClientSecret))
            {
                throw new ArgumentNullException(nameof(authokOptions.ClientSecret), "Client Secret can not be null when using `code` or `code id_token` as the response_type.");
            }
        }
    }
}
