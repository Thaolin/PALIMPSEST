[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'tools\P-GEN\workbench.ps1')
exit $LASTEXITCODE
