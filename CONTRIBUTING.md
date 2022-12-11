# Contribution

Please read [Authok's contribution guidelines](https://github.com/authok/open-source-template/blob/master/GENERAL-CONTRIBUTING.md).

## Environment setup

- Make sure you have the [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0) installed.
- Restore the Nuget dependencies using `dotnet restore` or through the UI.
- Follow the local development steps below to get started

## Local development

- `dotnet restore`: restore dependencies
- `dotnet build`: build the project
- `dotnet test`: run tests

## Testing

### Adding tests

- Tests go inside [Authok.AspNetCore.Authentication.UnitTests](https://github.com/authok/authok-aspnetcore-mvc/tree/main/tests/Authok.AspNetCore.Authentication.UnitTests)

### Running tests

Run the tests before opening a PR:

```bash
dotnet test
```

Also include any information about essential manual tests.
