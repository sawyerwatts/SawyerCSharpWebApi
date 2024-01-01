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

todo: this
