namespace Authok.AspNetCore.Authentication
{
    /// <summary>
    /// Class containing Authok specific constants used throughout the SDK
    /// </summary>
    public class AuthokConstants
    {
        /// <summary>
        /// The Authentication Scheme, used when configuring OpenIdConnect
        /// </summary>
        public static string AuthenticationScheme = "Authok";

        /// <summary>
        /// The callback path to which Authok should redirect back, used when configuring OpenIdConnect
        /// </summary>
        internal static string DefaultCallbackPath = "/callback";
    }
}
