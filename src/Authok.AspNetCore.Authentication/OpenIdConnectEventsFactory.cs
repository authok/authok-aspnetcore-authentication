using Authok.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Authok.AspNetCore.Authentication
{
    internal class OpenIdConnectEventsFactory
    {
        internal static OpenIdConnectEvents Create(AuthokWebAppOptions authokOptions)
        {
            return new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnRedirectToIdentityProvider, CreateOnRedirectToIdentityProvider(authokOptions)),
                OnRedirectToIdentityProviderForSignOut = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnRedirectToIdentityProviderForSignOut, CreateOnRedirectToIdentityProviderForSignOut(authokOptions)),
                OnTokenValidated = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnTokenValidated, CreateOnTokenValidated(authokOptions)),

                OnAccessDenied = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnAccessDenied),
                OnAuthenticationFailed = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnAuthenticationFailed),
                OnAuthorizationCodeReceived = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnAuthorizationCodeReceived),
                OnMessageReceived = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnMessageReceived),
                OnRemoteFailure = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnRemoteFailure),
                OnRemoteSignOut = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnRemoteSignOut),
                OnSignedOutCallbackRedirect = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnSignedOutCallbackRedirect),
                OnTicketReceived = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnTicketReceived),
                OnTokenResponseReceived = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnTokenResponseReceived),
                OnUserInformationReceived = ProxyEvent(authokOptions.OpenIdConnectEvents?.OnUserInformationReceived),
            };
        }

        private static Func<T, Task> ProxyEvent<T>(Func<T, Task>? originalHandler, Func<T, Task>? newHandler = null)
        {
            return async (context) =>
            {
                if (newHandler != null)
                {
                   await newHandler(context);
                }

                if (originalHandler != null)
                {
                    await originalHandler(context);
                }
            };
        }

        private static Func<RedirectContext, Task> CreateOnRedirectToIdentityProvider(AuthokWebAppOptions authokOptions)
        {
            return (context) =>
            {
                // Set authokClient querystring parameter for /authorize
                context.ProtocolMessage.SetParameter("authokClient", Utils.CreateAgentString());

                foreach (var extraParam in GetAuthorizeParameters(authokOptions, context.Properties.Items))
                {
                    context.ProtocolMessage.SetParameter(extraParam.Key, extraParam.Value);
                }

                if (!string.IsNullOrWhiteSpace(authokOptions.Organization) && !context.Properties.Items.ContainsKey(AuthokAuthenticationParameters.Organization))
                {
                    context.Properties.Items[AuthokAuthenticationParameters.Organization] = authokOptions.Organization;
                }

                return Task.CompletedTask;
            };
        }

        private static Func<RedirectContext, Task> CreateOnRedirectToIdentityProviderForSignOut(AuthokWebAppOptions authokOptions)
        {
            return (context) =>
            {
                var logoutUri = $"https://{authokOptions.Domain}/v1/logout?client_id={authokOptions.ClientId}";
                var postLogoutUri = context.Properties.RedirectUri;
                var parameters = GetExtraParameters(context.Properties.Items);

                if (!string.IsNullOrEmpty(postLogoutUri))
                {
                    if (postLogoutUri.StartsWith("/"))
                    {
                        // transform to absolute
                        var request = context.Request;
                        postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                    }

                    logoutUri += $"&return_to={ Uri.EscapeDataString(postLogoutUri)}";
                }

                foreach (var (key, value) in parameters)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        logoutUri += $"&{key}={ Uri.EscapeDataString(value)}";
                    }
                    else
                    {
                        logoutUri += $"&{key}";
                    }
                }

                context.Response.Redirect(logoutUri);
                context.HandleResponse();

                return Task.CompletedTask;
            };
        }

        private static Func<TokenValidatedContext, Task> CreateOnTokenValidated(AuthokWebAppOptions authokOptions)
        {
            return (context) =>
            {
                try
                {
                    IdTokenValidator.Validate(authokOptions, context.SecurityToken, context.Properties.Items);
                }
                catch (IdTokenValidationException ex)
                {
                    context.Fail(ex.Message);
                }

                return Task.CompletedTask;
            };
        }

        private static IDictionary<string, string?> GetAuthorizeParameters(AuthokWebAppOptions authokOptions, IDictionary<string, string?> authSessionItems)
        {
            var parameters = new Dictionary<string, string?>();

            if (!string.IsNullOrEmpty(authokOptions.Organization))
            {
                parameters["organization"] = authokOptions.Organization;
            }

            // Extra Parameters
            if (authokOptions.LoginParameters != null)
            {
                foreach (var (key, value) in authokOptions.LoginParameters)
                {
                    parameters[key] = value;
                }
            }

            // Any Authok specific parameter
            foreach (var item in GetExtraParameters(authSessionItems))
            {
                var value = item.Value;
                if (item.Key == "scope")
                {
                    // Openid is a required scope, meaning that when omitted we need to ensure it gets added.
                    if (value == null)
                    {
                        value = "openid";
                    }
                    else if (!value.Contains("openid", StringComparison.CurrentCultureIgnoreCase))
                    {
                        value += " openid";
                    }
                }

                parameters[item.Key] = value;
            }

            return parameters;
        }

        private static IDictionary<string, string?> GetExtraParameters(IDictionary<string, string?> authSessionItems)
        {
            var parameters = new Dictionary<string, string?>();

            foreach (var (key, value) in authSessionItems.Where(item => item.Key.StartsWith($"{AuthokAuthenticationParameters.Prefix}:")))
            {
                parameters[key.Replace($"{AuthokAuthenticationParameters.Prefix}:", "")] = value;
            }

            return parameters;
        }
    }
}
