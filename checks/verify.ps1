[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$powershell = (Get-Process -Id $PID).Path
$dotnetRoot = Join-Path $repoRoot ".tools\dotnet"
$dotnet = Join-Path $dotnetRoot "dotnet.exe"
$godot = Join-Path $repoRoot ".tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
$nugetConfig = Join-Path $repoRoot "NuGet.Config"
$coreChecksProject = Join-Path $repoRoot "checks\Chronicle.Core.Checks\Chronicle.Core.Checks.csproj"
$visualChecksProject = Join-Path $repoRoot "checks\Chronicle.Visuals.Checks\Chronicle.Visuals.Checks.csproj"
$godotProject = Join-Path $repoRoot "src\Chronicle.Godot\Chronicle.Godot.csproj"
$godotProjectDirectory = Join-Path $repoRoot "src\Chronicle.Godot"
$pgenVerify = Join-Path $repoRoot "tools\P-GEN\tools\verify.ps1"
$atlasRuntimeName = "godot-atlas-verify-$([Guid]::NewGuid().ToString("N"))"
$atlasRuntimeRoot = Join-Path $repoRoot ".tools\$atlasRuntimeName"
$gate3bAtlasRuntimeName = "godot-gate3b-atlas-verify-$([Guid]::NewGuid().ToString("N"))"
$gate3bAtlasRuntimeRoot = Join-Path $repoRoot ".tools\$gate3bAtlasRuntimeName"
$gate3bPlayerRuntimeName = "godot-gate3b-player-verify-$([Guid]::NewGuid().ToString("N"))"
$gate3bPlayerRuntimeRoot = Join-Path $repoRoot ".tools\$gate3bPlayerRuntimeName"
$runtimeName = "godot-verify-$([Guid]::NewGuid().ToString("N"))"
$runtimeRoot = Join-Path $repoRoot ".tools\$runtimeName"
$goal4ARuntimeName = "godot-goal4a-verify-$([Guid]::NewGuid().ToString("N"))"
$goal4ARuntimeRoot = Join-Path $repoRoot ".tools\$goal4ARuntimeName"
$goal4BRuntimeName = "godot-goal4b-verify-$([Guid]::NewGuid().ToString("N"))"
$goal4BRuntimeRoot = Join-Path $repoRoot ".tools\$goal4BRuntimeName"
$goal4CRuntimeName = "godot-goal4c-verify-$([Guid]::NewGuid().ToString("N"))"
$goal4CRuntimeRoot = Join-Path $repoRoot ".tools\$goal4CRuntimeName"
$slice5RuntimeName = "godot-slice5-verify-$([Guid]::NewGuid().ToString("N"))"
$slice5RuntimeRoot = Join-Path $repoRoot ".tools\$slice5RuntimeName"

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

function Get-FileSha256 {
    param(
        [Parameter(Mandatory)]
        [string] $LiteralPath
    )

    $stream = [IO.File]::OpenRead($LiteralPath)
    $sha = [Security.Cryptography.SHA256]::Create()
    try
    {
        return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace("-", "")
    }
    finally
    {
        $sha.Dispose()
        $stream.Dispose()
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

function Invoke-GodotGoal4ARun {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Partial", "Complete")]
        [string] $Phase
    )

    $argument = if ($Phase -eq "Partial") { "--verify-4a-partial" } else { "--verify-4a" }
    $marker = if ($Phase -eq "Partial")
    {
        "GOAL4A PARTIAL SAVE READY stone=5 bell=0 active=Stone"
    }
    else
    {
        "GOAL4A ACCEPTANCE PASS stoneBranch=16 bellBranch=16 finalStone=0 finalBell=16"
    }

    Write-Host "`n==> Drive Goal 4A $Phase journey through Godot controls"
    $output = @(& $godot --headless --path $godotProjectDirectory -- $argument 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Goal 4A $Phase journey failed with exit code $exitCode."
    }

    if (-not $text.Contains($marker))
    {
        throw "Godot did not complete the expected Goal 4A $Phase journey."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during the Goal 4A $Phase journey."
    }
}

function Invoke-GodotGoal4ARestart {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Partial", "Complete")]
        [string] $Phase
    )

    Write-Host "`n==> Restart Godot and restore Goal 4A $Phase Study"
    $output = @(& $godot --headless --path $godotProjectDirectory --quit-after 2 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Goal 4A $Phase restart failed with exit code $exitCode."
    }

    $expectedFragments = if ($Phase -eq "Partial")
    {
        @(
            "SLICE2C LOAD",
            "activeStudy=Stone",
            "stoneUnderstanding=5/16",
            "bellUnderstanding=0/16",
            "codexWords=Fly"
        )
    }
    else
    {
        @(
            "SLICE2C LOAD",
            "activeStudy=none",
            "stoneUnderstanding=0/16",
            "bellUnderstanding=16/16",
            "codexWords=Fly, Bell"
        )
    }

    foreach ($fragment in $expectedFragments)
    {
        if (-not $text.Contains($fragment))
        {
            throw "Goal 4A $Phase restart did not restore expected fragment '$fragment'."
        }
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during the Goal 4A $Phase restart."
    }
}

function Invoke-GodotGoal4BRun {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Initial", "Restart")]
        [string] $Phase
    )

    $argument = if ($Phase -eq "Initial") { "--verify-4b" } else { "--verify-4b-restart" }

    Write-Host "`n==> Drive Goal 4B $Phase Home journey through Godot controls"
    $output = @(& $godot --headless --path $godotProjectDirectory -- $argument "--visual-cell-size=20" 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Goal 4B $Phase journey failed with exit code $exitCode."
    }

    if ($Phase -eq "Restart")
    {
        $marker = "GOAL4B ACCEPTANCE PASS home=surface:0,3 material=hearthstone route=physical view=50x36 save=5"
        if (-not $text.Contains($marker))
        {
            throw "Godot did not complete the exact Goal 4B acceptance journey."
        }
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during the Goal 4B $Phase journey."
    }
}

function Assert-Goal4BSave {
    $savePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    if (-not (Test-Path -LiteralPath $savePath -PathType Leaf))
    {
        throw "Goal 4B acceptance did not create its isolated Chronicle save."
    }

    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
    if ([int]$save.Version -ne 5)
    {
        throw "Goal 4B acceptance did not save envelope version 5."
    }

    $savedHome = $save.Chronicle.PSObject.Properties["Home"]
    if ($null -eq $savedHome -or $null -eq $savedHome.Value)
    {
        throw "Goal 4B acceptance did not save its explicit Home."
    }
}

function Invoke-GodotGoal4CRun {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Initial", "Restart", "Resolved", "Failure")]
        [string] $Phase
    )

    $argument = switch ($Phase)
    {
        "Initial" { "--verify-4c" }
        "Restart" { "--verify-4c-restart" }
        "Resolved" { "--verify-4c-resolved" }
        "Failure" { "--verify-4c-failure" }
    }
    $marker = switch ($Phase)
    {
        "Initial" { "GOAL4C THREATENED SAVE READY address=surface:1,3 pending=Smash save=5" }
        "Restart" { "GOAL4C SUCCESS RESOLVED address=surface:1,3 result=shattered save=5" }
        "Resolved" { "GOAL4C SUCCESS RESTART PASS address=surface:1,3 result=shattered save=5" }
        "Failure" { "GOAL4C FAILURE ACCEPTANCE PASS tick=1 replacement=2 smash=retained loadout=empty ward=intact" }
    }

    Write-Host "`n==> Drive Goal 4C $Phase conflict journey through Godot controls"
    $output = @(& $godot --headless --path $godotProjectDirectory -- $argument "--visual-cell-size=20" 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Goal 4C $Phase journey failed with exit code $exitCode."
    }

    if (-not $text.Contains($marker))
    {
        throw "Godot did not complete the exact Goal 4C $Phase journey."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during the Goal 4C $Phase journey."
    }
}

function Assert-Goal4CSave {
    $savePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    if (-not (Test-Path -LiteralPath $savePath -PathType Leaf))
    {
        throw "Goal 4C acceptance did not create its isolated Chronicle save."
    }

    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
    if ([int]$save.Version -ne 5)
    {
        throw "Goal 4C acceptance did not save envelope version 5."
    }
}

function Invoke-GodotSlice5Run {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Initial", "Restart")]
        [string] $Phase
    )

    $argument = if ($Phase -eq "Initial") { "--verify-slice5" } else { "--verify-slice5-restart" }
    $marker = if ($Phase -eq "Initial")
    {
        "SLICE5 SAVE READY bell=surface:0,-4 loadout=Fly[Bell] save=5"
    }
    else
    {
        "SLICE5 RESTART ACCEPTANCE PASS bell=surface:0,-4 source=attached death=confirmed"
    }

    Write-Host "`n==> Drive Slice 5 $Phase composition journey through Godot controls"
    $output = @(& $godot --headless --path $godotProjectDirectory -- $argument "--visual-cell-size=20" 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Slice 5 $Phase journey failed with exit code $exitCode."
    }

    if (-not $text.Contains($marker))
    {
        throw "Godot did not complete the exact Slice 5 $Phase journey."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during the Slice 5 $Phase journey."
    }
}

function Assert-Slice5Save {
    $savePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    if (-not (Test-Path -LiteralPath $savePath -PathType Leaf))
    {
        throw "Slice 5 acceptance did not create its isolated Chronicle save."
    }

    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
    $bell = $save.Chronicle.BellAddress
    $slot = $save.Chronicle.Loadout.Slot1
    $words = @($save.Chronicle.Codex.Words)
    if ([int]$save.Version -ne 5 -or
        $bell.Stratum -ne "surface" -or
        [long]$bell.X -ne 0 -or
        [long]$bell.Y -ne -4 -or
        $slot.Verb -ne "word.fly" -or
        $slot.Noun -ne "word.bell" -or
        $words -notcontains "word.bell" -or
        $words -contains "word.stone")
    {
        throw "Slice 5 acceptance did not save the exact moved-Bell branch."
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
        Hash = Get-FileSha256 $playerSave
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
        (Get-FileSha256 $Fingerprint.Path) -ne $Fingerprint.Hash -or
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
        [int] $VisualCellSize = 0,

        [switch] $ManualComparison
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
    if ($ManualComparison)
    {
        $commandArguments += "--manual-visual-pack"
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
        $expectedPack = if ($VisualCellSize -eq 20 -and -not $ManualComparison)
        {
            "chronicle.palimpsest20"
        }
        else
        {
            "chronicle.gate3b.manual"
        }
        $expectedComposerMarker =
            "GATE3B SHARED COMPOSER PLAN PASS pack=$expectedPack style=1 size=$VisualCellSize"
        if (-not $text.Contains($expectedComposerMarker))
        {
            throw "$Label did not use the expected Gate 3B shared-composer pack and cell size."
        }

        $expectedPreviewMarker = "GATE3B ATLAS VISUAL PREVIEW PASS size=$VisualCellSize"
        if (-not $text.Contains($expectedPreviewMarker))
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

    $captureStem = "atlas_s41337_g2_sky_xn256_yn260_w512_h512_z512_o11111"
    $captureDirectory = Join-Path $repoRoot ".tools\atlas-captures"
    $capturePng = Join-Path $captureDirectory "$captureStem.png"
    $captureJson = Join-Path $captureDirectory "$captureStem.json"
    if (-not (Test-Path -LiteralPath $capturePng -PathType Leaf) -or
        -not (Test-Path -LiteralPath $captureJson -PathType Leaf))
    {
        throw "World Atlas Inspector did not create its deterministic capture pair."
    }

    $firstPngHash = Get-FileSha256 $capturePng
    $firstJsonHash = Get-FileSha256 $captureJson

    $sentinel = New-PlayerSaveSentinel '{"sentinel":"Gate 3A Inspector must not read or mutate this player save."}'

    Invoke-GodotAtlasRun "Run the World Atlas Inspector beside an existing player save" $true

    Assert-PlayerSaveUnchanged $sentinel "World Atlas Inspector"

    if ((Get-FileSha256 $capturePng) -ne $firstPngHash -or
        (Get-FileSha256 $captureJson) -ne $firstJsonHash)
    {
        throw "Equivalent World Atlas Inspector runs produced different capture bytes."
    }
}

function Invoke-GodotGate3BPlayerRun {
    param(
        [Parameter(Mandatory)]
        [string] $Label,

        [ValidateSet(16, 20)]
        [int] $VisualCellSize,

        [switch] $ManualComparison
    )

    $expectedDensity = switch ($VisualCellSize)
    {
        20 { "51x37" }
        16 { "63x45" }
    }
    $expectedPack = if ($VisualCellSize -eq 20 -and -not $ManualComparison)
    {
        "chronicle.palimpsest20"
    }
    else
    {
        "chronicle.gate3b.manual"
    }
    $expectedMarker =
        "GATE3B PLAYER VISUAL ACCEPTANCE PASS size=$VisualCellSize density=$expectedDensity pack=$expectedPack"

    Write-Host "`n==> $Label"
    $commandArguments = @(
        "--headless",
        "--path", $godotProjectDirectory,
        "--",
        "--verify-gate3b-player",
        "--visual-cell-size=$VisualCellSize"
    )
    if ($ManualComparison)
    {
        $commandArguments += "--manual-visual-pack"
    }
    $output = @(& $godot @commandArguments 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "$Label failed with exit code $exitCode."
    }

    if (-not $text.Contains($expectedMarker))
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
        if ($CellSize -eq 20)
        {
            return @(
                "player_s41337_sky_bell_stone_20px_pgen",
                "player_s41337_sky_bell_stone_20px_manual"
            )
        }

        return @("player_s41337_sky_bell_stone_16px_manual")
    }

    $tags = if ($CellSize -eq 20) { @("pgen", "manual") } else { @("manual") }
    return @($tags | ForEach-Object {
        $tag = $_
        @(
            "surface_s41337_${CellSize}px_$tag",
            "surface_s41338_${CellSize}px_$tag",
            "surface_s90421_${CellSize}px_$tag"
        )
    })
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
            $expectedPack = if ($stem.EndsWith("_pgen", [StringComparison]::Ordinal))
            {
                "chronicle.palimpsest20"
            }
            else
            {
                "chronicle.gate3b.manual"
            }
            if ($metadata.PackId -ne $expectedPack -or [int]$metadata.CellSize -ne $cellSize)
            {
                throw "Gate 3B $Kind review artifact pair '$stem' does not identify its exact pack and native cell size."
            }

            $pairs.Add([pscustomobject]@{
                Stem = $stem
                PngHash = Get-FileSha256 $pngPath
                MetadataHash = Get-FileSha256 $metadataPath
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
        Set-IsolatedGodotRuntime $RuntimeRoot "$pass-20-manual"
        Assert-PlayerSaveAbsent "E5 manual 20-pixel player comparison $pass before launch"
        Invoke-GodotGate3BPlayerRun `
            "Run explicit manual 20-pixel player comparison ($pass)" `
            20 `
            -ManualComparison

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
    Invoke-GodotAtlasRun `
        "Run explicit manual 20-pixel World Atlas Inspector comparison without a player save" `
        $false `
        20 `
        -ManualComparison
    Assert-PlayerSaveAbsent "Gate 3B Atlas acceptance after absent-save launch"
    $firstPairs = @(Get-Gate3BReviewArtifactPairs "Surface")

    $sentinel = New-PlayerSaveSentinel '{"sentinel":"Gate 3B Inspector must not read or mutate this player save."}'
    foreach ($cellSize in @(20, 16))
    {
        Invoke-GodotAtlasRun "Run Gate 3B World Atlas Inspector beside an existing player save at $cellSize pixels" $true $cellSize
    }
    Invoke-GodotAtlasRun `
        "Run explicit manual 20-pixel World Atlas Inspector comparison beside an existing player save" `
        $true `
        20 `
        -ManualComparison
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

function Assert-E5PackagedVisualBundle {
    $outputRoot = Join-Path $godotProjectDirectory ".godot\mono\temp\bin\Debug"
    $bundleRoot = Join-Path $outputRoot "visual-packs\palimpsest20"
    $expectedPaths = @(
        "atlases/palimpsest20.indices",
        "hashes.json",
        "manifest.json",
        "validation.json"
    )
    $actualPaths = @(
        Get-ChildItem -LiteralPath $bundleRoot -File -Recurse |
            ForEach-Object {
                $_.FullName.Substring($bundleRoot.Length + 1).Replace("\", "/")
            } |
            Sort-Object
    )
    if ($actualPaths.Count -ne $expectedPaths.Count -or
        @(Compare-Object $expectedPaths $actualPaths).Count -ne 0)
    {
        throw "E5 packaged output does not contain the exact canonical four-file visual bundle."
    }

    $forbidden = @(Get-ChildItem -LiteralPath $outputRoot -File -Recurse | Where-Object {
        $_.Name -match "VisualCompiler|Visuals.Conformance|catalogue|accepted-reference|contract.json"
    })
    if ($forbidden.Count -ne 0)
    {
        throw "E5 packaged output contains authoring/compiler material '$($forbidden[0].FullName)'."
    }

    $godotProjectText = Get-Content -LiteralPath $godotProject -Raw
    if ($godotProjectText -match "VisualCompiler|Visuals.Conformance|P-GEN")
    {
        throw "Chronicle.Godot references P-GEN authoring/compiler code instead of only the compiled artifact."
    }

    Write-Host "E5 PACKAGING PASS files=4 compiler=absent pack=chronicle.palimpsest20"
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

    Invoke-CheckedCommand "Run in-repository P-GEN authoring verification" $powershell @(
        "-NoProfile",
        "-File", $pgenVerify
    )

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
    Assert-E5PackagedVisualBundle

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

    Set-IsolatedGodotRuntime $goal4ARuntimeRoot "Study"
    Assert-PlayerSaveAbsent "Goal 4A acceptance before launch"
    Invoke-GodotGoal4ARun "Partial"
    Invoke-GodotGoal4ARestart "Partial"

    # A normal restart may legitimately deliver a Slow clock pulse after
    # proving that the partial pursuit restored. Continue the acceptance
    # journey from a second exact Stone=5 fixture instead of depending on
    # frame timing or rewinding the first runtime.
    Set-IsolatedGodotRuntime $goal4ARuntimeRoot "StudyContinuation"
    Assert-PlayerSaveAbsent "Goal 4A continuation before launch"
    Invoke-GodotGoal4ARun "Partial"
    Invoke-GodotGoal4ARun "Complete"
    Invoke-GodotGoal4ARestart "Complete"

    Set-IsolatedGodotRuntime $goal4BRuntimeRoot "Home"
    Assert-PlayerSaveAbsent "Goal 4B acceptance before launch"
    Invoke-GodotGoal4BRun "Initial"
    Assert-Goal4BSave
    Invoke-GodotGoal4BRun "Restart"
    Assert-Goal4BSave

    Set-IsolatedGodotRuntime $goal4CRuntimeRoot "Success"
    Assert-PlayerSaveAbsent "Goal 4C success acceptance before launch"
    Invoke-GodotGoal4CRun "Initial"
    Assert-Goal4CSave
    Invoke-GodotGoal4CRun "Restart"
    Assert-Goal4CSave
    Invoke-GodotGoal4CRun "Resolved"
    Assert-Goal4CSave

    Set-IsolatedGodotRuntime $goal4CRuntimeRoot "Failure"
    Assert-PlayerSaveAbsent "Goal 4C failure acceptance before launch"
    Invoke-GodotGoal4CRun "Failure"

    Set-IsolatedGodotRuntime $slice5RuntimeRoot "WordMultiplies"
    Assert-PlayerSaveAbsent "Slice 5 acceptance before launch"
    Invoke-GodotSlice5Run "Initial"
    Assert-Slice5Save
    Invoke-GodotSlice5Run "Restart"
    Assert-Slice5Save

    Write-Host "`nPASS: P-GEN E5 reader/default packaging plus Slice 5, Goal 4, Goal 2, Gate 3A, and Gate 3B verified."
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
    Remove-VerificationRuntimeRoot $goal4ARuntimeRoot (Join-Path $repoRoot ".tools\godot-goal4a-verify-")
    Remove-VerificationRuntimeRoot $goal4BRuntimeRoot (Join-Path $repoRoot ".tools\godot-goal4b-verify-")
    Remove-VerificationRuntimeRoot $goal4CRuntimeRoot (Join-Path $repoRoot ".tools\godot-goal4c-verify-")
    Remove-VerificationRuntimeRoot $slice5RuntimeRoot (Join-Path $repoRoot ".tools\godot-slice5-verify-")
}
