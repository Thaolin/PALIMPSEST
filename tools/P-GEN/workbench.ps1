[CmdletBinding()]
param(
    [string]$Catalogue = (Join-Path $PSScriptRoot 'catalogues\e45-palimpsest20.json'),
    [string]$BriefDirectory = (Join-Path $PSScriptRoot 'catalogues\briefs')
)

$ErrorActionPreference = 'Stop'
$palimpsestRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$godot = if ($env:CHRONICLE_GODOT) {
    $env:CHRONICLE_GODOT
} else {
    Join-Path $palimpsestRoot '.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
}

if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) {
    throw "Godot 4.7.1 .NET was not found at '$godot'. Set CHRONICLE_GODOT."
}

$project = Join-Path $PSScriptRoot 'src\Chronicle.VisualWorkbench.Godot'
& $godot --path $project -- --catalogue $Catalogue --brief-directory $BriefDirectory
exit $LASTEXITCODE
