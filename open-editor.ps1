[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$dotnetRoot = Join-Path $repoRoot ".tools\dotnet"
$dotnet = Join-Path $dotnetRoot "dotnet.exe"
$godot = Join-Path $repoRoot ".tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe"
$projectDirectory = Join-Path $repoRoot "src\Chronicle.Godot"

if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf))
{
    throw "Packaged .NET SDK not found at '$dotnet'."
}

if (-not (Test-Path -LiteralPath $godot -PathType Leaf))
{
    throw "Packaged Godot editor not found at '$godot'."
}

$originalEnvironment = @{
    DotnetCliHome = $env:DOTNET_CLI_HOME
    DotnetMultilevelLookup = $env:DOTNET_MULTILEVEL_LOOKUP
    DotnetRoot = $env:DOTNET_ROOT
    NugetPackages = $env:NUGET_PACKAGES
    Path = $env:PATH
}

try
{
    $env:DOTNET_ROOT = $dotnetRoot
    $env:DOTNET_MULTILEVEL_LOOKUP = "0"
    $env:PATH = "$dotnetRoot$([IO.Path]::PathSeparator)$($originalEnvironment.Path)"
    $env:DOTNET_CLI_HOME = Join-Path $repoRoot ".tools\dotnet-cli"
    $env:NUGET_PACKAGES = Join-Path $env:DOTNET_CLI_HOME ".nuget\packages"

    Write-Host "Opening Godot 4.7.1 with the packaged .NET SDK..."
    & $godot --editor --path $projectDirectory
}
finally
{
    $env:DOTNET_CLI_HOME = $originalEnvironment.DotnetCliHome
    $env:DOTNET_MULTILEVEL_LOOKUP = $originalEnvironment.DotnetMultilevelLookup
    $env:DOTNET_ROOT = $originalEnvironment.DotnetRoot
    $env:NUGET_PACKAGES = $originalEnvironment.NugetPackages
    $env:PATH = $originalEnvironment.Path
}
