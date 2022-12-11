using System;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;

namespace Authok.AspNetCore.Authentication.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Helper class to create an instance of the TestServer to use for Integration Tests.
    /// </summary>
    internal class TestServerBuilder
    {
        public static readonly string Host = @"https://localhost";
        public static readonly string Login = "Account/Login";
        public static readonly string Protected = "Account/Claims";
        public static readonly string Process = "Process";
        public static readonly string Logout = "Account/Logout";
        public static readonly string Callback = "Callback";
        public static readonly string ExtraProviderScheme = "ExtraProviderScheme";

        /// <summary>
        /// Create an instance of the TestServer to use for Integration Tests.
        /// </summary>
        /// <param name="configureOptions">Action used to provide custom configuration for the Authok middleware.</param>
        /// <param name="mockAuthentication">Indicated whether or not the authenitcation should be mocked, useful because some tests require an authenticated user while others require no user to exist.</param>
        /// <returns>The created TestServer instance.</returns>
        public static TestServer CreateServer(Action<AuthokWebAppOptions> configureOptions = null, Action<AuthokWebAppWithAccessTokenOptions> configureWithAccessTokensOptions = null, bool mockAuthentication = false, bool useServiceCollectionExtension = false, bool addExtraProvider = false, Action<AuthokWebAppOptions> configureAdditionalOptions = null)
        {
            var configuration = TestConfiguration.GetConfiguration();
            var host = new HostBuilder()
                .ConfigureWebHost(builder =>
                    builder.UseTestServer()
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseAuthentication();
                            app.UseAuthorization();
                            app.Use(async (context, next) =>
                            {
                                var req = context.Request;
                                var res = context.Response;

                               if (req.Path == new PathString("/process"))
                                {
                                    var ticket = await context.AuthenticateAsync("Cookies");
                                    await res.WriteAsync(JsonSerializer.Serialize(new
                                    {
                                        RefreshToken = await context.GetTokenAsync("refresh_token")
                                    }));
                                }
                                else
                                {
                                    await next();
                                }
                            });
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapControllerRoute(
                                    name: "default",
                                    pattern: "{controller=Home}/{action=Index}/{id?}");
                            });
                        })
                        .ConfigureServices(services =>
                        {
                            AuthokWebAppAuthenticationBuilder builder;
                            if (useServiceCollectionExtension)
                            {
                                builder = services.AddAuthokWebAppAuthentication(options =>
                                {
                                    options.Domain = configuration["Authok:Domain"];
                                    options.ClientId = configuration["Authok:ClientId"];

                                    if (configureOptions != null) configureOptions(options);
                                });
                            }
                            else
                            {
                                var authenticationBuilder = services.AddAuthentication(options =>
                                {
                                    if (!mockAuthentication)
                                    {
                                        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                                        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                                        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                                    }
                                });

                                builder = authenticationBuilder.AddAuthokWebAppAuthentication(options =>
                                {
                                    options.Domain = configuration["Authok:Domain"];
                                    options.ClientId = configuration["Authok:ClientId"];

                                    if (configureOptions != null) configureOptions(options);
                                });

                                if (addExtraProvider)
                                {
                                    authenticationBuilder.AddAuthokWebAppAuthentication(ExtraProviderScheme, options =>
                                    {
                                        options.Domain = configuration["Authok:ExtraProvider:Domain"];
                                        options.ClientId = configuration["Authok:ExtraProvider:ClientId"];
                                        options.SkipCookieMiddleware = true;

                                        if (configureAdditionalOptions != null) configureAdditionalOptions(options);
                                    });
                                }
                            }

                            if (configureWithAccessTokensOptions != null)
                            {
                                builder.WithAccessToken(configureWithAccessTokensOptions);
                            }

                            services.AddControllersWithViews();
                        })
                        .ConfigureTestServices(services =>
                        {
                            if (mockAuthentication)
                            {
                                services.AddAuthentication("Test")
                                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                                        "Test", options => { });
                            }
                        })
                        .UseConfiguration(configuration))

                .Build();

            host.Start();
            return host.GetTestServer();
        }
    }
}

