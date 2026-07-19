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
$visualChecksProject = Join-Path $repoRoot "checks\Chronicle.Visuals.Checks\Chronicle.Visuals.Checks.csproj"
$godotProject = Join-Path $repoRoot "src\Chronicle.Godot\Chronicle.Godot.csproj"
$godotProjectDirectory = Join-Path $repoRoot "src\Chronicle.Godot"
$atlasRuntimeName = "godot-atlas-verify-$([Guid]::NewGuid().ToString("N"))"
$atlasRuntimeRoot = Join-Path $repoRoot ".tools\$atlasRuntimeName"
$gate3bAtlasRuntimeName = "godot-gate3b-atlas-verify-$([Guid]::NewGuid().ToString("N"))"
$gate3bAtlasRuntimeRoot = Join-Path $repoRoot ".tools\$gate3bAtlasRuntimeName"
$gate3bPlayerRuntimeName = "godot-gate3b-player-verify-$([Guid]::NewGuid().ToString("N"))"
$gate3bPlayerRuntimeRoot = Join-Path $repoRoot ".tools\$gate3bPlayerRuntimeName"
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

function Assert-PlayerSaveAbsent {
    param(
        [Parameter(Mandatory)]
        [string] $Label
    )

    $saves = @(Get-ChildItem -LiteralPath $env:APPDATA, $env:LOCALAPPDATA -Filter "slice0_chronicle.json" -File -Recurse -ErrorAction SilentlyContinue)
    if ($saves.Count -ne 0)
    {
        throw "$Label unexpectedly found a player save at '$($saves[0].FullName)'."
    }
}

function Set-IsolatedGodotRuntime {
    param(
        [Parameter(Mandatory)]
        [string] $RuntimeRoot,

        [Parameter(Mandatory)]
        [string] $Scenario
    )

    $scenarioRoot = Join-Path $RuntimeRoot $Scenario
    $env:APPDATA = (New-Item -ItemType Directory -Force (Join-Path $scenarioRoot "Roaming")).FullName
    $env:LOCALAPPDATA = (New-Item -ItemType Directory -Force (Join-Path $scenarioRoot "Local")).FullName
}

function New-PlayerSaveSentinel {
    param(
        [Parameter(Mandatory)]
        [string] $Contents
    )

    $playerSave = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    New-Item -ItemType Directory -Force (Split-Path -Parent $playerSave) | Out-Null
    [IO.File]::WriteAllText($playerSave, $Contents, [Text.UTF8Encoding]::new($false))

    return [pscustomobject]@{
        Path = $playerSave
        Hash = (Get-FileHash -LiteralPath $playerSave -Algorithm SHA256).Hash
        WriteTimeUtc = (Get-Item -LiteralPath $playerSave).LastWriteTimeUtc
    }
}

function Assert-PlayerSaveUnchanged {
    param(
        [Parameter(Mandatory)]
        [psobject] $Fingerprint,

        [Parameter(Mandatory)]
        [string] $Label
    )

    if (-not (Test-Path -LiteralPath $Fingerprint.Path -PathType Leaf) -or
        (Get-FileHash -LiteralPath $Fingerprint.Path -Algorithm SHA256).Hash -ne $Fingerprint.Hash -or
        (Get-Item -LiteralPath $Fingerprint.Path).LastWriteTimeUtc -ne $Fingerprint.WriteTimeUtc)
    {
        throw "$Label changed the existing player save."
    }
}

function Invoke-GodotAtlasRun {
    param(
        [Parameter(Mandatory)]
        [string] $Label,

        [Parameter(Mandatory)]
        [bool] $ExpectedExistingSave,

        [ValidateSet(0, 16, 20)]
        [int] $VisualCellSize = 0
    )

    Write-Host "`n==> $Label"
    $commandArguments = @(
        "--headless",
        "--path", $godotProjectDirectory,
        "--scene", "res://WorldAtlasInspector.tscn",
        "--"
    )
    if ($VisualCellSize -eq 0)
    {
        $commandArguments += "--verify-world-atlas"
    }
    else
    {
        $commandArguments += @("--verify-gate3b-atlas", "--visual-cell-size=$VisualCellSize")
    }

    $output = @(& $godot @commandArguments 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "$Label failed with exit code $exitCode."
    }

    if ($text -notmatch "GATE3A ATLAS ACCEPTANCE PASS")
    {
        throw "$Label did not complete the World Atlas Inspector acceptance."
    }

    if ($text -notmatch "GATE3A PLAYER SAVE PRESERVED existing=$ExpectedExistingSave")
    {
        throw "$Label did not observe and preserve the expected player-save state."
    }

    if ($VisualCellSize -ne 0)
    {
        $expectedComposerMarker =
            "GATE3B SHARED COMPOSER PLAN PASS pack=chronicle.gate3b.manual style=1 size=$VisualCellSize"
        if (-not $text.Contains($expectedComposerMarker, [StringComparison]::Ordinal))
        {
            throw "$Label did not use the expected Gate 3B shared-composer pack and cell size."
        }

        $expectedPreviewMarker = "GATE3B ATLAS VISUAL PREVIEW PASS size=$VisualCellSize"
        if (-not $text.Contains($expectedPreviewMarker, [StringComparison]::Ordinal))
        {
            throw "$Label did not complete the expected Gate 3B Atlas visual preview."
        }
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "$Label reported a Godot error."
    }
}

function Invoke-GodotAtlasAcceptance {
    Assert-PlayerSaveAbsent "World Atlas Inspector acceptance before launch"
    Invoke-GodotAtlasRun "Run the World Atlas Inspector without a player save" $false
    Assert-PlayerSaveAbsent "World Atlas Inspector acceptance after absent-save launch"

    $captureStem = "atlas_s41337_g1_sky_xn256_yn260_w512_h512_z512_o11111"
    $captureDirectory = Join-Path $repoRoot ".tools\atlas-captures"
    $capturePng = Join-Path $captureDirectory "$captureStem.png"
    $captureJson = Join-Path $captureDirectory "$captureStem.json"
    if (-not (Test-Path -LiteralPath $capturePng -PathType Leaf) -or
        -not (Test-Path -LiteralPath $captureJson -PathType Leaf))
    {
        throw "World Atlas Inspector did not create its deterministic capture pair."
    }

    $firstPngHash = (Get-FileHash -LiteralPath $capturePng -Algorithm SHA256).Hash
    $firstJsonHash = (Get-FileHash -LiteralPath $captureJson -Algorithm SHA256).Hash

    $sentinel = New-PlayerSaveSentinel '{"sentinel":"Gate 3A Inspector must not read or mutate this player save."}'

    Invoke-GodotAtlasRun "Run the World Atlas Inspector beside an existing player save" $true

    Assert-PlayerSaveUnchanged $sentinel "World Atlas Inspector"

    if ((Get-FileHash -LiteralPath $capturePng -Algorithm SHA256).Hash -ne $firstPngHash -or
        (Get-FileHash -LiteralPath $captureJson -Algorithm SHA256).Hash -ne $firstJsonHash)
    {
        throw "Equivalent World Atlas Inspector runs produced different capture bytes."
    }
}

function Invoke-GodotGate3BPlayerRun {
    param(
        [Parameter(Mandatory)]
        [string] $Label,

        [ValidateSet(16, 20)]
        [int] $VisualCellSize
    )

    $expectedDensity = switch ($VisualCellSize)
    {
        20 { "33x23" }
        16 { "41x29" }
    }
    $expectedMarker =
        "GATE3B PLAYER VISUAL ACCEPTANCE PASS size=$VisualCellSize density=$expectedDensity pack=chronicle.gate3b.manual"

    Write-Host "`n==> $Label"
    $commandArguments = @(
        "--headless",
        "--path", $godotProjectDirectory,
        "--",
        "--verify-gate3b-player",
        "--visual-cell-size=$VisualCellSize"
    )
    $output = @(& $godot @commandArguments 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "$Label failed with exit code $exitCode."
    }

    if (-not $text.Contains($expectedMarker, [StringComparison]::Ordinal))
    {
        throw "$Label did not report the exact Gate 3B player marker '$expectedMarker'."
    }

    if ($text -notmatch "SLICE2C ACCEPTANCE PASS")
    {
        throw "$Label did not retain the complete Slice 2C acceptance journey."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "$Label reported a Godot error."
    }
}

function Get-Gate3BReviewArtifactStems {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Player", "Surface")]
        [string] $Kind,

        [ValidateSet(16, 20)]
        [int] $CellSize
    )

    if ($Kind -eq "Player")
    {
        return @("player_s41337_sky_bell_stone_${CellSize}px")
    }

    return @(
        "surface_s41337_${CellSize}px",
        "surface_s41338_${CellSize}px",
        "surface_s90421_${CellSize}px"
    )
}

function Clear-Gate3BInitialReviewArtifacts {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Player", "Surface")]
        [string] $Kind
    )

    $directory = Join-Path $repoRoot ".tools\gate3b-review"
    $resolvedRepoRoot = [IO.Path]::GetFullPath($repoRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $resolvedDirectory = [IO.Path]::GetFullPath($directory).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $repoPrefix = "$resolvedRepoRoot$([IO.Path]::DirectorySeparatorChar)"
    $directoryPrefix = "$resolvedDirectory$([IO.Path]::DirectorySeparatorChar)"

    if (-not $resolvedDirectory.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase))
    {
        throw "Gate 3B review artifact directory escapes the repository root."
    }

    foreach ($cellSize in @(20, 16))
    {
        foreach ($stem in @(Get-Gate3BReviewArtifactStems $Kind $cellSize))
        {
            foreach ($extension in @(".png", ".json"))
            {
                $artifactPath = [IO.Path]::GetFullPath((Join-Path $resolvedDirectory "$stem$extension"))
                if (-not $artifactPath.StartsWith($directoryPrefix, [StringComparison]::OrdinalIgnoreCase))
                {
                    throw "Gate 3B review artifact '$stem$extension' escapes its expected directory."
                }

                if (Test-Path -LiteralPath $artifactPath -PathType Leaf)
                {
                    Remove-Item -LiteralPath $artifactPath -Force
                }
            }
        }
    }
}

function Get-Gate3BReviewArtifactPairs {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Player", "Surface")]
        [string] $Kind
    )

    $directory = Join-Path $repoRoot ".tools\gate3b-review"
    $pairs = [System.Collections.Generic.List[object]]::new()
    foreach ($cellSize in @(20, 16))
    {
        foreach ($stem in @(Get-Gate3BReviewArtifactStems $Kind $cellSize))
        {
            $pngPath = Join-Path $directory "$stem.png"
            $metadataPath = Join-Path $directory "$stem.json"
            if (-not (Test-Path -LiteralPath $pngPath -PathType Leaf) -or
                -not (Test-Path -LiteralPath $metadataPath -PathType Leaf) -or
                (Get-Item -LiteralPath $pngPath).Length -le 0 -or
                (Get-Item -LiteralPath $metadataPath).Length -le 0)
            {
                throw "Gate 3B $Kind review artifact pair '$stem' is missing or empty."
            }

            $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
            if ($metadata.PackId -ne "chronicle.gate3b.manual" -or [int]$metadata.CellSize -ne $cellSize)
            {
                throw "Gate 3B $Kind review artifact pair '$stem' does not identify its exact pack and native cell size."
            }

            $pairs.Add([pscustomobject]@{
                Stem = $stem
                PngHash = (Get-FileHash -LiteralPath $pngPath -Algorithm SHA256).Hash
                MetadataHash = (Get-FileHash -LiteralPath $metadataPath -Algorithm SHA256).Hash
            })
        }
    }

    return $pairs.ToArray()
}

function Assert-Gate3BReviewPairsDeterministic {
    param(
        [Parameter(Mandatory)]
        [object[]] $First,

        [Parameter(Mandatory)]
        [object[]] $Second,

        [Parameter(Mandatory)]
        [string] $Kind
    )

    if ($First.Count -ne $Second.Count)
    {
        throw "Gate 3B $Kind review artifact runs produced different pair counts."
    }

    for ($index = 0; $index -lt $First.Count; $index++)
    {
        $left = $First[$index]
        $right = $Second[$index]
        if ($left.Stem -ne $right.Stem -or
            $left.PngHash -ne $right.PngHash -or
            $left.MetadataHash -ne $right.MetadataHash)
        {
            throw "Equivalent Gate 3B $Kind review runs produced different bytes for '$($left.Stem)'."
        }
    }
}

function Invoke-GodotGate3BPlayerAcceptance {
    param(
        [Parameter(Mandatory)]
        [string] $RuntimeRoot
    )

    $firstPairs = @()
    Clear-Gate3BInitialReviewArtifacts "Player"
    foreach ($pass in @("initial", "repeat"))
    {
        foreach ($cellSize in @(20, 16))
        {
            Set-IsolatedGodotRuntime $RuntimeRoot "$pass-$cellSize"
            Assert-PlayerSaveAbsent "Gate 3B player acceptance $pass $cellSize-pixel before launch"
            Invoke-GodotGate3BPlayerRun "Run isolated Gate 3B player acceptance at $cellSize pixels ($pass)" $cellSize
        }

        $pairs = @(Get-Gate3BReviewArtifactPairs "Player")
        if ($pass -eq "initial")
        {
            $firstPairs = $pairs
        }
        else
        {
            Assert-Gate3BReviewPairsDeterministic $firstPairs $pairs "player"
        }
    }
}

function Invoke-GodotGate3BAtlasAcceptance {
    param(
        [Parameter(Mandatory)]
        [string] $RuntimeRoot
    )

    Clear-Gate3BInitialReviewArtifacts "Surface"
    Set-IsolatedGodotRuntime $RuntimeRoot "absent-save"
    Assert-PlayerSaveAbsent "Gate 3B Atlas acceptance before launch"
    foreach ($cellSize in @(20, 16))
    {
        Invoke-GodotAtlasRun "Run Gate 3B World Atlas Inspector without a player save at $cellSize pixels" $false $cellSize
    }
    Assert-PlayerSaveAbsent "Gate 3B Atlas acceptance after absent-save launch"
    $firstPairs = @(Get-Gate3BReviewArtifactPairs "Surface")

    $sentinel = New-PlayerSaveSentinel '{"sentinel":"Gate 3B Inspector must not read or mutate this player save."}'
    foreach ($cellSize in @(20, 16))
    {
        Invoke-GodotAtlasRun "Run Gate 3B World Atlas Inspector beside an existing player save at $cellSize pixels" $true $cellSize
    }
    Assert-PlayerSaveUnchanged $sentinel "Gate 3B World Atlas Inspector"

    $secondPairs = @(Get-Gate3BReviewArtifactPairs "Surface")
    Assert-Gate3BReviewPairsDeterministic $firstPairs $secondPairs "Surface"
}

function Remove-VerificationRuntimeRoot {
    param(
        [Parameter(Mandatory)]
        [string] $RuntimeRoot,

        [Parameter(Mandatory)]
        [string] $ExpectedPrefix
    )

    $resolvedRoot = [IO.Path]::GetFullPath($RuntimeRoot)
    $resolvedPrefix = [IO.Path]::GetFullPath($ExpectedPrefix)
    if ($resolvedRoot.Length -gt $resolvedPrefix.Length -and
        $resolvedRoot.StartsWith($resolvedPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedRoot))
    {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
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

    Invoke-CheckedCommand "Restore Chronicle.Visuals checks" $dotnet @(
        "restore",
        "--configfile", $nugetConfig,
        $visualChecksProject
    )
    Invoke-CheckedCommand "Build Chronicle.Visuals checks" $dotnet @(
        "build",
        "--no-restore",
        $visualChecksProject
    )
    Invoke-CheckedCommand "Run Chronicle.Visuals checks" $dotnet @(
        "run",
        "--no-build",
        "--no-restore",
        "--project", $visualChecksProject
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

    $env:APPDATA = (New-Item -ItemType Directory -Force (Join-Path $atlasRuntimeRoot "Roaming")).FullName
    $env:LOCALAPPDATA = (New-Item -ItemType Directory -Force (Join-Path $atlasRuntimeRoot "Local")).FullName

    Invoke-GodotAtlasAcceptance
    Invoke-GodotGate3BAtlasAcceptance $gate3bAtlasRuntimeRoot
    Invoke-GodotGate3BPlayerAcceptance $gate3bPlayerRuntimeRoot

    $env:APPDATA = (New-Item -ItemType Directory -Force (Join-Path $runtimeRoot "Roaming")).FullName
    $env:LOCALAPPDATA = (New-Item -ItemType Directory -Force (Join-Path $runtimeRoot "Local")).FullName

    Invoke-GodotEditorBuild
    Invoke-GodotStartup "Start Godot headlessly and create a save"
    Invoke-GodotAcceptance
    Invoke-GodotStartup "Start Godot headlessly and restore the completed journey" -RequireLoad

    Write-Host "`nPASS: Gate 3A Core and Atlas Inspector, Gate 3B compiled packs, dual-density player and Atlas visual acceptance, deterministic review artifacts, Godot editor build, full Goal 2 control journey, and next-launch restore verified."
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

    Remove-VerificationRuntimeRoot $runtimeRoot (Join-Path $repoRoot ".tools\godot-verify-")
    Remove-VerificationRuntimeRoot $atlasRuntimeRoot (Join-Path $repoRoot ".tools\godot-atlas-verify-")
    Remove-VerificationRuntimeRoot $gate3bAtlasRuntimeRoot (Join-Path $repoRoot ".tools\godot-gate3b-atlas-verify-")
    Remove-VerificationRuntimeRoot $gate3bPlayerRuntimeRoot (Join-Path $repoRoot ".tools\godot-gate3b-player-verify-")
}
