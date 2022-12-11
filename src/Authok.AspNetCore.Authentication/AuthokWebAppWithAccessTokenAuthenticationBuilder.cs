using Authok.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Authok.AspNetCore.Authentication
{
    /// <summary>
    /// Builder to add extra functionality when using Access Tokens. 
    /// </summary>
    public class AuthokWebAppWithAccessTokenAuthenticationBuilder
    {
        private readonly IServiceCollection _services;
        private readonly Action<AuthokWebAppWithAccessTokenOptions> _configureOptions;
        private readonly AuthokWebAppOptions _options;
        private readonly string _authenticationScheme;

        private static readonly IList<string> CodeResponseTypes = new List<string>() {
            OpenIdConnectResponseType.Code,
            OpenIdConnectResponseType.CodeIdToken
        };

        /// <summary>
        /// Constructs an instance of <see cref="AuthokWebAppWithAccessTokenAuthenticationBuilder"/>
        /// </summary>
        /// <param name="services">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see> instance</param>
        /// <param name="configureOptions">A delegate used to configure the <see cref="AuthokWebAppWithAccessTokenOptions"/></param>
        /// <param name="options">The <see cref="AuthokWebAppOptions"/> used when calling AddAuthokWebAppAuthentication.</param>
        public AuthokWebAppWithAccessTokenAuthenticationBuilder(IServiceCollection services, Action<AuthokWebAppWithAccessTokenOptions> configureOptions, AuthokWebAppOptions options) 
            : this(services, configureOptions, options, AuthokConstants.AuthenticationScheme)
        {
        }

        /// <summary>
        /// Constructs an instance of <see cref="AuthokWebAppWithAccessTokenAuthenticationBuilder"/>
        /// </summary>
        /// <param name="services">The original <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see> instance</param>
        /// <param name="configureOptions">A delegate used to configure the <see cref="AuthokWebAppWithAccessTokenOptions"/></param>
        /// <param name="options">The <see cref="AuthokWebAppOptions"/> used when calling AddAuthokWebAppAuthentication.</param>
        /// <param name="authenticationScheme">The authentication scheme to use.</param>
        public AuthokWebAppWithAccessTokenAuthenticationBuilder(IServiceCollection services, Action<AuthokWebAppWithAccessTokenOptions> configureOptions, AuthokWebAppOptions options, string authenticationScheme)
        {
            _services = services;
            _configureOptions = configureOptions;
            _options = options;
            _authenticationScheme = authenticationScheme;

            EnableWithAccessToken();
        }

        private void EnableWithAccessToken()
        {
            var authokWithAccessTokensOptions = new AuthokWebAppWithAccessTokenOptions();

            _configureOptions(authokWithAccessTokensOptions);

            ValidateOptions(_options);

            _services.Configure(_authenticationScheme, _configureOptions);
            _services.AddOptions<OpenIdConnectOptions>(_authenticationScheme)
                .Configure(options =>
                {
                    options.ResponseType = OpenIdConnectResponseType.Code;

                    if (!string.IsNullOrEmpty(authokWithAccessTokensOptions.Scope))
                    {
                        options.Scope.AddRange(authokWithAccessTokensOptions.Scope.Split(" "));
                    }

                    if (authokWithAccessTokensOptions.UseRefreshTokens)
                    {
                        options.Scope.AddSafe("offline_access");
                    }

                    options.Events.OnRedirectToIdentityProvider = Utils.ProxyEvent(CreateOnRedirectToIdentityProvider(_authenticationScheme), options.Events.OnRedirectToIdentityProvider);
                });

            _services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
                .Configure(options =>
                {
                    options.Events.OnValidatePrincipal = Utils.ProxyEvent(CreateOnValidatePrincipal(_authenticationScheme), options.Events.OnValidatePrincipal);
                });
        }

        private static Func<RedirectContext, Task> CreateOnRedirectToIdentityProvider(string authenticationScheme)
        {
            return (context) =>
            {
                var optionsWithAccessToken = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<AuthokWebAppWithAccessTokenOptions>>().Get(authenticationScheme);

                if (!string.IsNullOrWhiteSpace(optionsWithAccessToken.Audience))
                {
                    context.ProtocolMessage.SetParameter("audience", optionsWithAccessToken.Audience);
                }

                if (context.Properties.Items.ContainsKey(AuthokAuthenticationParameters.Audience))
                {
                    context.ProtocolMessage.SetParameter("audience", context.Properties.Items[AuthokAuthenticationParameters.Audience]);
                }

                return Task.CompletedTask;
            };
        }

        private static Func<CookieValidatePrincipalContext, Task> CreateOnValidatePrincipal(string authenticationScheme)
        {
            return async (context) =>
            {
                var options = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<AuthokWebAppOptions>>().Get(authenticationScheme);
                var optionsWithAccessToken = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<AuthokWebAppWithAccessTokenOptions>>().Get(authenticationScheme);
                var oidcOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<OpenIdConnectOptions>>().Get(authenticationScheme);

                if (context.Properties.Items.TryGetValue(".AuthScheme", out var authScheme))
                {
                    if (!string.IsNullOrEmpty(authScheme) && authScheme != authenticationScheme)
                    {
                        return;
                    }
                }

                var accessToken = context.Properties.GetTokenValue("access_token");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    if (optionsWithAccessToken.UseRefreshTokens)
                    {
                        var refreshToken = context.Properties.GetTokenValue("refresh_token");
                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            var now = DateTimeOffset.Now;
                            var expiresAt = DateTimeOffset.Parse(context.Properties.GetTokenValue("expires_at")!);
                            var leeway = 60;
                            var difference = DateTimeOffset.Compare(expiresAt, now.AddSeconds(leeway));
                            var isExpired = difference <= 0;

                            if (isExpired && !string.IsNullOrWhiteSpace(refreshToken))
                            {
                                var result = await RefreshTokens(options, refreshToken, oidcOptions.Backchannel);

                                if (result != null)
                                {
                                    context.Properties.UpdateTokenValue("access_token", result.AccessToken);
                                    if (!string.IsNullOrEmpty(result.RefreshToken))
                                    {
                                        context.Properties.UpdateTokenValue("refresh_token", result.RefreshToken);
                                    }
                                    context.Properties.UpdateTokenValue("id_token", result.IdToken);
                                    context.Properties.UpdateTokenValue("expires_at", DateTimeOffset.Now.AddSeconds(result.ExpiresIn).ToString("o"));
                                }
                                else
                                {
                                    context.Properties.UpdateTokenValue("refresh_token", null!);
                                }

                                context.ShouldRenew = true;

                            }
                        }
                        else
                        {
                            if (optionsWithAccessToken.Events?.OnMissingRefreshToken != null)
                            {
                                await optionsWithAccessToken.Events.OnMissingRefreshToken(context.HttpContext);
                            }
                        }
                    }
                }
                else
                {
                    if (CodeResponseTypes.Contains(options.ResponseType!))
                    {
                        if (optionsWithAccessToken.Events?.OnMissingAccessToken != null)
                        {
                            await optionsWithAccessToken.Events.OnMissingAccessToken(context.HttpContext);
                        }
                    }
                }
            };
        }

        private static async Task<AccessTokenResponse?> RefreshTokens(AuthokWebAppOptions options, string refreshToken, HttpClient? httpClient = null)
        {
            using (var tokenClient = new TokenClient(httpClient))
            {
                return await tokenClient.Refresh(options, refreshToken);
            }
        }

        private static void ValidateOptions(AuthokWebAppOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                throw new ArgumentNullException(nameof(options.ClientSecret), "Client Secret can not be null when requesting an access token.");
            }
        }

    }
}
