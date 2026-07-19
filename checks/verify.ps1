[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnetRoot = Join-Path $repoRoot ".tools\dotnet"
$dotnet = Join-Path $dotnetRoot "dotnet.exe"
$godot = Join-Path $repoRoot ".tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
$nugetConfig = Join-Path $repoRoot "NuGet.Config"
$coreChecksProject = Join-Path $repoRoot "checks\Chronicle.Core.Checks\Chronicle.Core.Checks.csproj"
$godotProject = Join-Path $repoRoot "src\Chronicle.Godot\Chronicle.Godot.csproj"
$godotProjectDirectory = Join-Path $repoRoot "src\Chronicle.Godot"
$runtimeName = "godot-verify-$([Guid]::NewGuid().ToString("N"))"
$runtimeRoot = Join-Path $repoRoot ".tools\$runtimeName"

$originalEnvironment = @{
    AppData = $env:APPDATA
    DotnetCliHome = $env:DOTNET_CLI_HOME
    DotnetMultilevelLookup = $env:DOTNET_MULTILEVEL_LOOKUP
    DotnetRoot = $env:DOTNET_ROOT
    LocalAppData = $env:LOCALAPPDATA
    NugetPackages = $env:NUGET_PACKAGES
    Path = $env:PATH
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string] $Label,

        [Parameter(Mandatory)]
        [string] $Executable,

        [Parameter(Mandatory)]
        [string[]] $CommandArguments
    )

    Write-Host "`n==> $Label"
    & $Executable @CommandArguments
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0)
    {
        throw "$Label failed with exit code $exitCode."
    }
}

function Invoke-GodotStartup {
    param(
        [Parameter(Mandatory)]
        [string] $Label,

        [switch] $RequireLoad
    )

    Write-Host "`n==> $Label"
    $output = @(& $godot --headless --path $godotProjectDirectory --quit-after 2 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "$Label failed with exit code $exitCode."
    }

    if ($text -notmatch "SLICE2C READY")
    {
        throw "$Label did not reach the Slice 2C ready state."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "$Label reported a Godot error."
    }

    if ($RequireLoad -and $text -notmatch
        "SLICE2C LOAD seed=41337 tick=16 address=sky:0,0 speed=Normal intent=Up codex=Fly:True,Stone:True study=16/16 incarnation=2:Alive loadout=FLY stone=sky:1,0")
    {
        throw "$Label did not restore the automated Slice 2C journey exactly."
    }
}

function Invoke-GodotAcceptance {
    Write-Host "`n==> Drive the complete Slice 2C journey through Godot controls"
    $output = @(& $godot --headless --path $godotProjectDirectory -- --verify-slice2c 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Slice 2C acceptance failed with exit code $exitCode."
    }

    if ($text -notmatch "SLICE2C ACCEPTANCE PASS")
    {
        throw "Godot did not complete the Slice 2C acceptance journey."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during the Slice 2C acceptance journey."
    }
}

function Invoke-GodotEditorBuild {
    Write-Host "`n==> Build through Godot's C# editor callback"
    $output = @(& $godot --headless --path $godotProjectDirectory --build-solutions --quit-after 2 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot's C# editor callback failed with exit code $exitCode."
    }

    if ($text -match "(?m)(\\.NET Sdk not found|EditorPlugin build callback failed|ERROR:)")
    {
        throw "Godot's C# editor callback reported an error."
    }
}

if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf))
{
    throw "Packaged .NET executable not found at '$dotnet'. See docs/DEVELOPMENT.md."
}

if (-not (Test-Path -LiteralPath $godot -PathType Leaf))
{
    throw "Packaged Godot executable not found at '$godot'. See docs/DEVELOPMENT.md."
}

try
{
    Push-Location $repoRoot

    $env:DOTNET_ROOT = $dotnetRoot
    $env:DOTNET_MULTILEVEL_LOOKUP = "0"
    $env:PATH = "$dotnetRoot$([IO.Path]::PathSeparator)$($originalEnvironment.Path)"
    $env:DOTNET_CLI_HOME = Join-Path $repoRoot ".tools\dotnet-cli"
    $env:NUGET_PACKAGES = Join-Path $env:DOTNET_CLI_HOME ".nuget\packages"

    Invoke-CheckedCommand "Restore Chronicle.Core checks" $dotnet @(
        "restore",
        "--configfile", $nugetConfig,
        $coreChecksProject
    )
    Invoke-CheckedCommand "Build Chronicle.Core checks" $dotnet @(
        "build",
        "--no-restore",
        $coreChecksProject
    )
    Invoke-CheckedCommand "Run Chronicle.Core checks" $dotnet @(
        "run",
        "--no-build",
        "--no-restore",
        "--project", $coreChecksProject
    )

    Invoke-CheckedCommand "Restore Chronicle.Godot" $dotnet @(
        "restore",
        "--configfile", $nugetConfig,
        $godotProject
    )
    Invoke-CheckedCommand "Build Chronicle.Godot" $dotnet @(
        "build",
        "--no-restore",
        $godotProject
    )

    $env:APPDATA = (New-Item -ItemType Directory -Force (Join-Path $runtimeRoot "Roaming")).FullName
    $env:LOCALAPPDATA = (New-Item -ItemType Directory -Force (Join-Path $runtimeRoot "Local")).FullName

    Invoke-GodotEditorBuild
    Invoke-GodotStartup "Start Godot headlessly and create a save"
    Invoke-GodotAcceptance
    Invoke-GodotStartup "Start Godot headlessly and restore the completed journey" -RequireLoad

    Write-Host "`nPASS: Core checks, Godot editor build, full control journey, and next-launch restore verified."
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

    Pop-Location -ErrorAction SilentlyContinue

    $safeRuntimePrefix = Join-Path $repoRoot ".tools\godot-verify-"
    if ($runtimeRoot.StartsWith($safeRuntimePrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $runtimeRoot))
    {
        Remove-Item -LiteralPath $runtimeRoot -Recurse -Force
    }
}
