# Sawyer's WebApi Template

This repo is a template for the .NET SDK to make a slightly more batteries
included web API csproj, subject to my preferences. Also
[stickfigure](https://github.com/stickfigure) has an amazing article on this
subject [here](https://github.com/stickfigure/blog/wiki/How-to-%28and-how-not-to%29-design-REST-APIs).

## Installation

Here are the steps in bash:

```sh
template_dir=~/.dotnetTemplates/SawyerCSharpWebApi
git clone https://github.com/sawyerwatts/SawyerCSharpWebApi.git $template_dir
rm -rf $template_dir/.git
rm $template_dir/README.md
dotnet new install $template_dir
```

Here are the steps in PowerShell:

```ps1
$templateDir="$env:USERPROFILE\.dotnetTemplates\SawyerCSharpWebApi"
git clone https://github.com/sawyerwatts/SawyerCSharpWebApi.git $templateDir
rm $templateDir\.git -r -force
rm $templateDir\README.md
dotnet new install $templateDir
```

## Uninstallation

Here are the steps in bash:

```sh
template_dir=~/.dotnetTemplates/SawyerCSharpWebApi
dotnet new uninstall $template_dir
rm -rf $template_dir/.git
```

Here are the steps in PowerShell:

```ps1
$templateDir="$env:USERPROFILE\.dotnetTemplates\SawyerCSharpWebApi"
dotnet new uninstall $templateDir
rm $templateDir -r -force
```

## Features

1. This uses the `Worker` SDK, so it has access to the `HostApplicationBuilder`,
   allowing easy dependency injection and access to the `appsettings*.json` files.
2. This comes pre-baked with Serilog configured as the implementation to the
   built in `ILogger<T>`, and these settings are controlled via the
   `appsettings*.json` files. Serilog is configured to buffer file writes and to
   write file and console logs via a background thread.
3. `Program` supplies a `CancellationToken` to `Driver.RunAsync`, and it will
   mimic the functionality that the various web/app hosts supply when managing
   long-running applications: interrupt signals are captured by the host, the host
   will update the `CancellationToken` so the app can gracefully shut down, and if
   the app doesn't close in five seconds, then the app will be forcefully closed.
4. `Driver` has `Settings` and `RegisterTo`, an easy reminder how to best
   register settings POCOs with validation on startup.
