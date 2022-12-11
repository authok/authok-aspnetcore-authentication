using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Authok.AspNetCore.Authentication
{
    /// <summary>
    /// Used to setup Authok specific defaults for <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.openidconnect.openidconnectoptions">OpenIdConnectOptions</see>.
    /// </summary>
    internal class AuthokOpenIdConnectPostConfigureOptions : IPostConfigureOptions<OpenIdConnectOptions>
    {
        public void PostConfigure(string name, OpenIdConnectOptions options)
        {
            options.Backchannel.DefaultRequestHeaders.Add("Authok-Client", Utils.CreateAgentString());
        }
    }
}
