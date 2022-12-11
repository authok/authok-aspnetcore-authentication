![Authok SDK for ASP.NET Core applications](https://cdn.authok.cn/website/sdks/banners/authok-aspnetcore-authentication-banner.png)

A library based on `Microsoft.AspNetCore.Authentication.OpenIdConnect` to make integrating Authok in your ASP.NET Core application as seamlessly as possible.

![Release](https://img.shields.io/github/v/release/authok/authok-aspnetcore-authentication)
![Downloads](https://img.shields.io/nuget/dt/authok.aspnetcore.authentication)
[![License](https://img.shields.io/:license-MIT-blue.svg?style=flat)](https://opensource.org/licenses/MIT)
![AzureDevOps](https://img.shields.io/azure-devops/build/AuthokSDK/Authok.AspNetCore.Authentication/8)

:books: [Documentation](#documentation) - :rocket: [Getting Started](#getting-started) - :computer: [API Reference](#api-reference) - :speech_balloon: [Feedback](#feedback)

## Documentation

- [Quickstart](https://authok.cn/docs/quickstart/webapp/aspnet-core) - our interactive guide for quickly adding login, logout and user information to an ASP.NET MVC application using Authok.
- [Sample App](https://github.com/authok-samples/authok-aspnetcore-mvc-samples/tree/master/Quickstart/Sample) - a full-fledged ASP.NET MVC application integrated with Authok.
- [Examples](https://github.com/authok/authok-aspnetcore-authentication/blob/main/EXAMPLES.md) - code samples for common ASP.NET MVC authentication scenario's.
- [Docs site](https://www.authok.cn/docs) - explore our docs site and learn more about 

## 快速开始
### 要求

This library supports .NET Core 3.1 and .NET 6.

### 安装

The SDK is available on [Nuget](https://www.nuget.org/packages/Authok.AspNetCore.Authentication) and can be installed through the UI or using the Package Manager Console:

```
Install-Package Authok.AspNetCore.Authentication
```

### 配置 Authok

Create a **Regular Web Application** in the [Authok Dashboard](https://mgmt.authok.cn/#/applications).

> **If you're using an existing application**, verify that you have configured the following settings in your Regular Web Application:
>
> - Click on the "Settings" tab of your application's page.
> - Scroll down and click on "Advanced Settings".
> - Under "Advanced Settings", click on the "OAuth" tab.
> - Ensure that "JSON Web Token (JWT) Signature Algorithm" is set to `RS256` and that "OIDC Conformant" is enabled.

Next, configure the following URLs for your application under the "Application URIs" section of the "Settings" page:

- **Allowed Callback URLs**: `https://YOUR_APP_DOMAIN:YOUR_APP_PORT/callback`
- **Allowed Logout URLs**: `https://YOUR_APP_DOMAIN:YOUR_APP_PORT/`

Take note of the **Client ID**, **Client Secret**, and **Domain** values under the "Basic Information" section. You'll need these values to configure your ASP.NET web application.

> :information_source: You need the **Client Secret** only when you have to get an access token to [call an API](#calling-an-api).

### Configure the SDK

To make your ASP.NET web application communicate properly with Authok, you need to add the following configuration section to your `appsettings.json` file:

```json
  "Authok": {
    "Domain": "YOUR_AUTHOK_DOMAIN",
    "ClientId": "YOUR_AUTHOK_CLIENT_ID"
  }
```

Replace the placeholders with the proper values from the Authok Dashboard.

Make sure you have enabled authentication and authorization in your `Startup.Configure` method:

```csharp
...
app.UseAuthentication();
app.UseAuthorization();
...
```

Integrate the SDK in your ASP.NET Core application by calling `AddAuthokWebAppAuthentication` in your `Startup.ConfigureServices` method:

```csharp
services.AddAuthokWebAppAuthentication(options =>
{
    options.Domain = Configuration["Authok:Domain"];
    options.ClientId = Configuration["Authok:ClientId"];
});
```

### 登录 和 退登
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

For more code samples on how to integrate the **authok-aspnetcore-authentication** SDK in your **ASP.NET MVC** application, have a look at our [examples](https://github.com/authok/authok-aspnetcore-authentication/blob/main/EXAMPLES.md).

## API reference
Explore public API's available in authok-aspnetcore-authentication.

- [AuthokWebAppOptions](https://authok.github.io/authok-aspnetcore-authentication/api/Authok.AspNetCore.Authentication.AuthokWebAppOptions.html)
- [AuthokWebAppWithAccessTokenOptions](https://authok.github.io/authok-aspnetcore-authentication/api/Authok.AspNetCore.Authentication.AuthokWebAppWithAccessTokenOptions.html)
- [LoginAuthenticationPropertiesBuilder](https://authok.github.io/authok-aspnetcore-authentication/api/Authok.AspNetCore.Authentication.LoginAuthenticationPropertiesBuilder.html)
- [LogoutAuthenticationPropertiesBuilder](https://authok.github.io/authok-aspnetcore-authentication/api/Authok.AspNetCore.Authentication.LogoutAuthenticationPropertiesBuilder.html)
- [AuthokWebAppAuthenticationBuilder](https://authok.github.io/authok-aspnetcore-authentication/api/Authok.AspNetCore.Authentication.AuthokWebAppAuthenticationBuilder.html)
- [AuthokWebAppWithAccessTokenAuthenticationBuilder](https://authok.github.io/authok-aspnetcore-authentication/api/Authok.AspNetCore.Authentication.AuthokWebAppWithAccessTokenAuthenticationBuilder.html)

## Feedback
### Contributing

We appreciate feedback and contribution to this repo! Before you get started, please see the following:

- [Authok's general contribution guidelines](https://github.com/authok/open-source-template/blob/master/GENERAL-CONTRIBUTING.md)
- [Authok's code of conduct guidelines](https://github.com/authok/open-source-template/blob/master/CODE-OF-CONDUCT.md)
- [This repo's contribution guide](https://github.com/authok/authok-aspnetcore-authentication/blob/main/CONTRIBUTING.md)

### Raise an issue

To provide feedback or report a bug, please [raise an issue on our issue tracker](https://github.com/authok/authok-aspnetcore-authentication/issues).

### Vulnerability Reporting

Please do not report security vulnerabilities on the public GitHub issue tracker. The [Responsible Disclosure Program](https://authok.cn/responsible-disclosure-policy) details the procedure for disclosing security issues.

---

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: light)" srcset="https://cdn.authok.cn/website/sdks/logos/authok_light_mode.png"   width="150">
    <source media="(prefers-color-scheme: dark)" srcset="https://cdn.authok.cn/website/sdks/logos/authok_dark_mode.png" width="150">
    <img alt="Authok Logo" src="https://cdn.authok.cn/website/sdks/logos/authok_light_mode.png" width="150">
  </picture>
</p>
<p align="center">Authok is an easy to implement, adaptable authentication and authorization platform. To learn more checkout <a href="https://authok.com/why-authok">Why Authok?</a></p>
<p align="center">
This project is licensed under the MIT license. See the <a href="https://github.com/authok/authok-aspnetcore-authentication/blob/main/LICENSE"> LICENSE</a> file for more info.</p>
