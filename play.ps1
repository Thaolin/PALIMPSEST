<#
.SYNOPSIS
Builds and launches Untitled Chronicle RPG with the packaged Godot and .NET tools.

.EXAMPLE
.\play.ps1

.EXAMPLE
.\play.ps1 -Fresh

.EXAMPLE
.\play.ps1 -Profile goal4b-uat
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $Profile,

    [switch] $Fresh,

    [ValidateSet(16, 20)]
    [int] $CellSize = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Fresh -and -not [string]::IsNullOrWhiteSpace($Profile))
{
    throw "Choose either -Fresh or -Profile, not both. -Fresh creates a new named profile automatically."
}

if (-not [string]::IsNullOrWhiteSpace($Profile) -and
    $Profile -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')
{
    throw "Profile names must start with a letter or number and contain at most 64 letters, numbers, dots, underscores, or hyphens."
}

$repoRoot = $PSScriptRoot
$dotnetRoot = Join-Path $repoRoot ".tools\dotnet"
$dotnet = Join-Path $dotnetRoot "dotnet.exe"
$godot = Join-Path $repoRoot ".tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe"
$projectDirectory = Join-Path $repoRoot "src\Chronicle.Godot"
$projectFile = Join-Path $projectDirectory "Chronicle.Godot.csproj"
$nugetConfig = Join-Path $repoRoot "NuGet.Config"
$assetsFile = Join-Path $projectDirectory "obj\project.assets.json"
$profilesDirectory = Join-Path $repoRoot ".tools\play-profiles"

if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf))
{
    throw "Packaged .NET SDK not found at '$dotnet'."
}

if (-not (Test-Path -LiteralPath $godot -PathType Leaf))
{
    throw "Packaged Godot executable not found at '$godot'."
}

if ($Fresh)
{
    $baseName = "fresh-{0:yyyyMMdd-HHmmss-fff}" -f (Get-Date)
    $Profile = $baseName
    $suffix = 1

    while (Test-Path -LiteralPath (Join-Path $profilesDirectory $Profile))
    {
        $Profile = "$baseName-$suffix"
        $suffix++
    }
}

$profileRoot = $null
$profileAppData = $null
$profileLocalAppData = $null
$profileSave = $null

if (-not [string]::IsNullOrWhiteSpace($Profile))
{
    $profileRoot = Join-Path $profilesDirectory $Profile
    $profileAppData = Join-Path $profileRoot "Roaming"
    $profileLocalAppData = Join-Path $profileRoot "Local"
    $profileSave = Join-Path $profileAppData "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"

    $profileState = if (Test-Path -LiteralPath $profileSave -PathType Leaf)
    {
        "existing Chronicle"
    }
    else
    {
        "new Chronicle"
    }

    Write-Host "Profile: $Profile ($profileState)"
    Write-Host "Save root: $profileRoot"

    if ($Fresh)
    {
        Write-Host "Resume later with: .\play.ps1 -Profile $Profile"
    }
}
else
{
    Write-Host "Profile: normal Godot user save"
}

Write-Host "Visual pack: $CellSize px"

$launchDescription = if ($null -eq $profileRoot)
{
    "Build and launch with the normal Godot user save"
}
else
{
    "Build and launch profile '$Profile'"
}

if (-not $PSCmdlet.ShouldProcess($projectDirectory, $launchDescription))
{
    return
}

$originalEnvironment = @{
    AppData = $env:APPDATA
    DotnetCliHome = $env:DOTNET_CLI_HOME
    DotnetMultilevelLookup = $env:DOTNET_MULTILEVEL_LOOKUP
    DotnetRoot = $env:DOTNET_ROOT
    LocalAppData = $env:LOCALAPPDATA
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

    $needsRestore = -not (Test-Path -LiteralPath $assetsFile -PathType Leaf)
    if (-not $needsRestore)
    {
        $needsRestore =
            (Get-Item -LiteralPath $projectFile).LastWriteTimeUtc -gt
            (Get-Item -LiteralPath $assetsFile).LastWriteTimeUtc
    }

    if ($needsRestore)
    {
        Write-Host "Restoring the Godot C# project..."
        & $dotnet restore --configfile $nugetConfig $projectFile
        if ($LASTEXITCODE -ne 0)
        {
            throw "Project restore failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host "Building the Godot C# project..."
    & $dotnet build --no-restore --nologo $projectFile
    if ($LASTEXITCODE -ne 0)
    {
        throw "Project build failed with exit code $LASTEXITCODE."
    }

    if ($null -ne $profileRoot)
    {
        $env:APPDATA = (New-Item -ItemType Directory -Force -Path $profileAppData).FullName
        $env:LOCALAPPDATA = (New-Item -ItemType Directory -Force -Path $profileLocalAppData).FullName
    }

    Write-Host "Launching Untitled Chronicle RPG..."
    & $godot --path $projectDirectory -- "--visual-cell-size=$CellSize"
    if ($LASTEXITCODE -ne 0)
    {
        throw "Godot exited with code $LASTEXITCODE."
    }
}
finally
{
    $env:APPDATA = $originalEnvironment.AppData
    $env:DOTNET_CLI_HOME = $originalEnvironment.DotnetCliHome
    $env:DOTNET_MULTILEVEL_LOOKUP = $originalEnvironment.DotnetMultilevelLookup
    $env:DOTNET_ROOT = $originalEnvironment.DotnetRoot
    $env:LOCALAPPDATA = $originalEnvironment.LocalAppData
    $env:NUGET_PACKAGES = $originalEnvironment.NugetPackages
    $env:PATH = $originalEnvironment.Path
}
