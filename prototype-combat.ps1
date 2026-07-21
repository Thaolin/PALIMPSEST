$ErrorActionPreference = "Stop"

$prototypeDotnetRoot = Join-Path $PSScriptRoot ".tools\dotnet"
$prototypeDotnet = Join-Path $prototypeDotnetRoot "dotnet.exe"
$prototypeCliHome = Join-Path $PSScriptRoot ".tools\dotnet-cli"
$prototypeProject = Join-Path $PSScriptRoot "prototypes\Chronicle.CombatGrammar\Chronicle.CombatGrammar.csproj"
$prototypeNugetConfig = Join-Path $PSScriptRoot "NuGet.Config"

if (-not (Test-Path -LiteralPath $prototypeDotnet)) {
    throw "Bundled dotnet runtime not found at $prototypeDotnet"
}

New-Item -ItemType Directory -Force -Path $prototypeCliHome | Out-Null
$env:DOTNET_ROOT = $prototypeDotnetRoot
$env:DOTNET_CLI_HOME = $prototypeCliHome
$env:NUGET_PACKAGES = Join-Path $prototypeCliHome ".nuget\packages"

& $prototypeDotnet restore --configfile $prototypeNugetConfig $prototypeProject
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $prototypeDotnet run --no-restore --project $prototypeProject
exit $LASTEXITCODE
