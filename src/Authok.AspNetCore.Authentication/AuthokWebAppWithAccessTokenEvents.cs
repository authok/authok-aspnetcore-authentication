using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Authok.AspNetCore.Authentication
{
    /// <summary>
    /// Events allowing you to hook into specific moments in the Authok middleware.
    /// </summary>
    public class AuthokWebAppWithAccessTokenEvents
    {
        /// <summary>
        /// Executed when an Access Token is missing where one was expected, allowing you to react accordingly.
        /// </summary>
        /// <example>
        /// <code>
        /// services
        ///   .AddAuthokWebAppAuthentication(options => {})
        ///   .WithAccessToken(options =>
        ///   {
        ///       options.Events = new AuthokWebAppWithAccessTokenEvents
        ///       {
        ///           OnMissingAccessToken = async (context) =>
        ///           {
        ///               await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        ///               var authenticationProperties = new AuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
        ///               await context.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
        ///           }
        ///       };
        ///   });
        /// </code>
        /// </example>
        public Func<HttpContext, Task>? OnMissingAccessToken { get; set; }

        /// <summary>
        /// Executed when a Refresh Token is missing where one was expected, allowing you to react accordingly.
        /// </summary>
        /// <example>
        /// <code>
        /// services
        ///   .AddAuthokWebAppAuthentication(options => {})
        ///   .WithAccessToken(options =>
        ///   {
        ///       options.Events = new AuthokWebAppWithAccessTokenEvents
        ///       {
        ///           OnMissingRefreshToken = async (context) =>
        ///           {
        ///               await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        ///               var authenticationProperties = new AuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
        ///               await context.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
        ///           }
        ///       };
        ///   });
        /// </code>
        /// </example>
        public Func<HttpContext, Task>? OnMissingRefreshToken { get; set; }
    }
}
