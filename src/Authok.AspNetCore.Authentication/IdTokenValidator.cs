using Authok.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Authok.AspNetCore.Authentication.IntegrationTests")]
namespace Authok.AspNetCore.Authentication
{
    internal static class IdTokenValidator
    {
        public static void Validate(AuthokWebAppOptions authokOptions, JwtSecurityToken token, IDictionary<string, string?>? properties = null)
        {
            var organization = properties != null && properties.ContainsKey(AuthokAuthenticationParameters.Organization) ? properties[AuthokAuthenticationParameters.Organization] : null;

            if (!string.IsNullOrWhiteSpace(organization))
            {
                var organizationClaimValue = token.Claims.SingleOrDefault(claim => claim.Type == "org_id")?.Value;

                if (string.IsNullOrWhiteSpace(organizationClaimValue))
                {
                    throw new IdTokenValidationException("Organization claim must be a string present in the ID token.");
                }
                else if (organizationClaimValue != organization)
                {
                    throw new IdTokenValidationException($"Organization claim mismatch in the ID token; expected \"{organization}\", found \"{organizationClaimValue}\".");
                }
            }

            var sub = token.Claims.SingleOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (sub == null)
            {
                throw new IdTokenValidationException("Subject (sub) claim must be a string present in the ID token.");
            }

            var iat = token.Claims.SingleOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Iat)?.Value;

            if (iat == null)
            {
                throw new IdTokenValidationException("Issued At (iat) claim must be an integer present in the ID token.");
            }

            if (token.Audiences.Count() > 1)
            {
                if (string.IsNullOrWhiteSpace(token.Payload.Azp))
                {
                    throw new IdTokenValidationException("Authorized Party (azp) claim must be a string present in the ID token when Audiences (aud) claim has multiple values.");

                }
                else if (token.Payload.Azp != authokOptions.ClientId)
                {
                    throw new IdTokenValidationException($"Authorized Party (azp) claim mismatch in the ID token; expected \"{authokOptions.ClientId}\", found \"{token.Payload.Azp}\".");
                }
            }

            if (authokOptions.MaxAge.HasValue)
            {
                var authTimeRaw = token.Claims.SingleOrDefault(claim => claim.Type == JwtRegisteredClaimNames.AuthTime)?.Value;
                long? authTime = !string.IsNullOrWhiteSpace(authTimeRaw) ? (long?)Convert.ToDouble(authTimeRaw, CultureInfo.InvariantCulture) : null;

                if (!authTime.HasValue)
                {
                    throw new IdTokenValidationException("Authentication Time (auth_time) claim must be an integer present in the ID token when MaxAge specified.");
                }
                else
                {
                    var authValidUntil = (long)(authTime + authokOptions.MaxAge.Value.TotalSeconds);
                    var epochNow = EpochTime.GetIntDate(DateTime.Now);

                    if (epochNow > authValidUntil)
                    {
                        throw new IdTokenValidationException($"Authentication Time (auth_time) claim in the ID token indicates that too much time has passed since the last end-user authentication. Current time ({epochNow}) is after last auth at {authValidUntil}.");
                    }
                }
            }
        }
    }
}
