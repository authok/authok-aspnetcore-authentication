# Examples using authok-aspnetcore-authentication

- [Login and Logout](#login-and-logout)
- [Scopes](#scopes)
- [Calling an API](#calling-an-api)
- [Organizations](#organizations)
- [Extra parameters](#extra-parameters)
- [Roles](#roles)

## Login and Logout
Triggering login or logout is done using ASP.NET's `HttpContext`:

```csharp
public async Task Login(string returnUrl = "/")
{
    var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
        .WithRedirectUri(returnUrl)
        .Build();

    await HttpContext.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
}

[Authorize]
public async Task Logout()
{
    var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
        // Indicate here where Authok should redirect the user after a logout.
        // Note that the resulting absolute Uri must be added in the
        // **Allowed Logout URLs** settings for the client.
        .WithRedirectUri(Url.Action("Index", "Home"))
        .Build();

    await HttpContext.SignOutAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
```

## Scopes

By default, this SDK requests the `openid profile` scopes, if needed you can configure the SDK to request a different set of scopes.
As `openid` is a [required scope](https://authok.cn/docs/scopes/openid-connect-scopes), the SDK will ensure the `openid` scope is always added, even when explicitly omitted when setting the scope.

```csharp
services.AddAuthokWebAppAuthentication(options =>
{
    options.Domain = Configuration["Authok:Domain"];
    options.ClientId = Configuration["Authok:ClientId"];
    options.Scope = "openid profile scope1 scope2";
});
```

Apart from being able to configure the used scopes globally, the SDK's `LoginAuthenticationPropertiesBuilder` can be used to supply scopes when triggering login through `HttpContext.ChallengeAsync`:

```csharp
var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
    .WithScope("openid profile scope1 scope2")
    .Build();

await HttpContext.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
```

> :information_source: Specifying the scopes when calling `HttpContext.ChallengeAsync` will take precedence over any globally configured scopes.

## Calling an API

If you want to call an API from your ASP.NET MVC application, you need to obtain an access token issued for the API you want to call. 
As the SDK is configured to use OAuth's [Implicit Grant with Form Post](https://authok.cn/docs/flows/implicit-flow-with-form-post), no access token will be returned by default. In order to do so, we should be using the [Authorization Code Grant](https://authok.cn/docs/flows/authorization-code-flow), which requires the use of a `ClientSecret`.
Next, to obtain the token to access an external API, call `WithAccessToken` and set the `audience` to the API Identifier. You can get the API Identifier from the API Settings for the API you want to use.

```csharp
services
    .AddAuthokWebAppAuthentication(options =>
    {
        options.Domain = Configuration["Authok:Domain"];
        options.ClientId = Configuration["Authok:ClientId"];
        options.ClientSecret = Configuration["Authok:ClientSecret"];
    })
    .WithAccessToken(options =>
    {
        options.Audience = Configuration["Authok:Audience"];
    });
```

Apart from being able to configure the audience globally, the SDK's `LoginAuthenticationPropertiesBuilder` can be used to supply the audience when triggering login through `HttpContext.ChallengeAsync`:

```csharp
var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
    .WithRedirectUri("/") // "/" is the default value used for RedirectUri, so this can be omitted.
    .WithAudience("YOUR_AUDIENCE")
    .Build();

await HttpContext.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
```

> :information_source: Specifying the Audience when calling `HttpContext.ChallengeAsync` will take precedence over any globally configured Audience.

### Retrieving the access token

As the SDK uses the OpenId Connect middleware, the ID token is decoded and the corresponding claims are added to the `ClaimsIdentity`, making them available by using `User.Claims`.

The access token can be retrieved by calling `HttpContext.GetTokenAsync("access_token")`.

```csharp
[Authorize]
public async Task<IActionResult> Profile()
{
    var accessToken = await HttpContext.GetTokenAsync("access_token");

    return View(new UserProfileViewModel()
    {
        Name = User.Identity.Name,
        EmailAddress = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
        ProfileImage = User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
    });
}
```

### Refresh tokens

In the case where the application needs to use an access token to access an API, there may be a situation where the access token expires before the application's session does. In order to ensure you have a valid access token at all times, you can configure the SDK to use refresh tokens:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddAuthokWebAppAuthentication(options =>
        {
            options.Domain = Configuration["Authok:Domain"];
            options.ClientId = Configuration["Authok:ClientId"];
            options.ClientSecret = Configuration["Authok:ClientSecret"];
        })
        .WithAccessToken(options =>
        {
            options.Audience = Configuration["Authok:Audience"];
            options.UseRefreshTokens = true;
        });
}
```

#### Detecting the absense of a refresh token

In the event where the API, defined in your Authok dashboard, isn't configured to [allow offline access](https://authok.cn/docs/get-started/dashboard/api-settings), or the user was already logged in before the use of refresh tokens was enabled (e.g. a user logs in a few minutes before the use of refresh tokens is deployed), it might be useful to detect the absense of a refresh token in order to react accordingly (e.g. log the user out locally and force them to re-login).

```
services
    .AddAuthokWebAppAuthentication(options => {})
    .WithAccessToken(options =>
    {
        options.Audience = Configuration["Authok:Audience"];
        options.UseRefreshTokens = true;
        options.Events = new AuthokWebAppWithAccessTokenEvents
        {
            OnMissingRefreshToken = async (context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
                await context.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
            }
        };
    });
```

The above snippet checks whether the SDK is configured to use refresh tokens, if there is an existing ID token (meaning the user is authenticated) as well as the absence of a refresh token. If each of these criteria are met, it logs the user out from the application and initializes a new login flow.

> :information_source: In order for Authok to redirect back to the application's login URL, ensure to add the configured redirect URL to the application's `Allowed Logout URLs` in Authok's dashboard.

## Organizations

[Organizations](https://authok.cn/docs/organizations) is a set of features that provide better support for developers who build and maintain SaaS and Business-to-Business (B2B) applications.

Note that Organizations is currently only available to customers on our Enterprise and Startup subscription plans.

### Log in to an organization

Log in to an organization by specifying the `Organization` when calling `AddAuthokWebAppAuthentication`:

```csharp
services.AddAuthokWebAppAuthentication(options =>
{
    options.Domain = Configuration["Authok:Domain"];
    options.ClientId = Configuration["Authok:ClientId"];
    options.Organization = Configuration["Authok:Organization"];
});
```

Apart from being able to configure the organization globally, the SDK's `LoginAuthenticationPropertiesBuilder` can be used to supply the organization when triggering login through `HttpContext.ChallengeAsync`:

```csharp
var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
    .WithOrganization("YOUR_ORGANIZATION")
    .Build();

await HttpContext.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
```

> :information_source: Specifying the Organization when calling `HttpContext.ChallengeAsync` will take precedence over any globally configured Organization.

### Organization claim validation

If you don't provide an `organization` parameter at login, the SDK can't validate the `org_id` claim you get back in the ID token. In that case, you should validate the `org_id` claim yourself (e.g. by checking it against a list of valid organization ID's or comparing it with the application's URL).

```
services.AddAuthokWebAppAuthentication(options =>
{
    options.Domain = Configuration["Authok:Domain"];
    options.ClientId = Configuration["Authok:ClientId"];
    options.OpenIdConnectEvents = new OpenIdConnectEvents
    {
        OnTokenValidated = (context) =>
        {
            var organizationClaimValue = context.SecurityToken.Claims.SingleOrDefault(claim => claim.Type == "org_id")?.Value;
            var expectedOrganizationIds = new List<string> {"123", "456"};
            if (!string.IsNullOrEmpty(organizationClaimValue) && !expectedOrganizationIds.Contains(organizationClaimValue))
            {
                context.Fail("Unexpected org_id claim detected.");
            }

            return Task.CompletedTask;
        }
    };
}).
```

For more information, please read [Work with Tokens and Organizations](https://authok.cn/docs/organizations/using-tokens) on Authok Docs.

### Accept user invitations
Accept a user invitation through the SDK by creating a route within your application that can handle the user invitation URL, and log the user in by passing the `organization` and `invitation` parameters from this URL.

```csharp
public class InvitationController : Controller {

    public async Task Accept(string organization, string invitation)
    {
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithOrganization(organization)
            .WithInvitation(invitation)
            .Build();
            
        await HttpContext.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
    }
}
```

## Extra Parameters

Authok's `/authorize` and `/v1/logout` endpoint support additional querystring parameters that aren't first-class citizens in this SDK. If you need to support any of those parameters, you can configure the SDK to do so.

### Extra parameters when logging in

In order to send extra parameters to Authok's `/authorize` endpoint upon logging in, set `LoginParameters` when calling `AddAuthokWebAppAuthentication`.

An example is the `screen_hint` parameter, which can be used to show the signup page instead of the login page when redirecting users to Authok:

```csharp
services.AddAuthokWebAppAuthentication(options =>
{
    options.Domain = Configuration["Authok:Domain"];
    options.ClientId = Configuration["Authok:ClientId"];
    options.LoginParameters = new Dictionary<string, string>() { { "screen_hint", "signup" } };
});
```

Apart from being able to configure these globally, the SDK's `LoginAuthenticationPropertiesBuilder` can be used to supply extra parameters when triggering login through `HttpContext.ChallengeAsync`:

```csharp
var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
    .WithParameter("screen_hint", "signup")
    .Build();

await HttpContext.ChallengeAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
```

> :information_source: Specifying any extra parameter when calling `HttpContext.ChallengeAsync` will take precedence over any globally configured parameter.

### Extra parameters when logging out
The same as with the login request, you can send parameters to the `logout` endpoint by calling `WithParameter` on the `LogoutAuthenticationPropertiesBuilder`.

```csharp
var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
    .WithParameter("federated")
    .Build();

await HttpContext.SignOutAsync(AuthokConstants.AuthenticationScheme, authenticationProperties);
await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```
> :information_source: The example above uses a parameter without an actual value, for more information see https://authok.cn/docs/logout/log-users-out-of-idps.

## Roles

Before you can add [Role Based Access Control](https://authok.cn/docs/manage-users/access-control/rbac), you will need to ensure the required roles are created and assigned to the corresponding user(s). Follow the guidance explained in [assign-roles-to-users](https://authok.cn/docs/users/assign-roles-to-users) to ensure your user gets assigned the admin role.

Once the role is created and assigned to the required user(s), you will need to create a [rule](https://authok.cn/docs/rules/current) that adds the role(s) to the ID token so that it is available to your backend. To do so, go to the [new rule page](https://mgmt.authok.cn/#/rules/new) and create an empty rule. Then, use the following code for your rule:

```javascript
function (user, context, callback) {
  const assignedRoles = (context.authorization || {}).roles;
  const idTokenClaims = context.idToken || {};

  idTokenClaims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] = assignedRoles;

  context.idToken = idTokenClaims;

  callback(null, user, context);
}
```

> :information_source: As this SDK uses the OpenId Connect middleware, it expects roles to exist in the `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` claim.

### Integrate roles in your ASP.NET application
You can use the [Role based authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/roles) mechanism to make sure that only the users with specific roles can access certain actions. Add the `[Authorize(Roles = "...")]` attribute to your controller action.

```csharp
[Authorize(Roles = "admin")]
public IActionResult Admin()
{
    return View();
}
```
