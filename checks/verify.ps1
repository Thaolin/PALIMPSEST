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
$runtimeName = "godot-verify-$([Guid]::NewGuid().ToString("N"))"
$runtimeRoot = Join-Path $repoRoot ".tools\$runtimeName"
$goal6AQuicklyRuntimeName = "godot-goal6a-quickly-verify-$([Guid]::NewGuid().ToString("N"))"
$goal6AQuicklyRuntimeRoot = Join-Path $repoRoot ".tools\$goal6AQuicklyRuntimeName"
$goal6ALastingRuntimeName = "godot-goal6a-lasting-verify-$([Guid]::NewGuid().ToString("N"))"
$goal6ALastingRuntimeRoot = Join-Path $repoRoot ".tools\$goal6ALastingRuntimeName"
$goal6BRuntimeName = "godot-goal6b-verify-$([Guid]::NewGuid().ToString("N"))"
$goal6BRuntimeRoot = Join-Path $repoRoot ".tools\$goal6BRuntimeName"
$goal7ARuntimeName = "godot-goal7a-verify-$([Guid]::NewGuid().ToString("N"))"
$goal7ARuntimeRoot = Join-Path $repoRoot ".tools\$goal7ARuntimeName"
$goal7BRuntimeName = "godot-goal7b-verify-$([Guid]::NewGuid().ToString("N"))"
$goal7BRuntimeRoot = Join-Path $repoRoot ".tools\$goal7BRuntimeName"

$originalEnvironment = @{
    AppData = $env:APPDATA
    DotnetCliHome = $env:DOTNET_CLI_HOME
    DotnetMultilevelLookup = $env:DOTNET_MULTILEVEL_LOOKUP
    DotnetRoot = $env:DOTNET_ROOT
    LocalAppData = $env:LOCALAPPDATA
    NugetPackages = $env:NUGET_PACKAGES
    NugetAudit = $env:NuGetAudit
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

    if ($text -notmatch "GOAL6B READY")
    {
        throw "$Label did not reach the Goal 6B ready state."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "$Label reported a Godot error."
    }

    if ($RequireLoad -and $text -notmatch "GOAL6B READY")
    {
        throw "$Label did not restore the Goal 6B Chronicle."
    }
}

function Invoke-GodotGoal6ARun {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Quickly", "QuicklyRestart", "Lasting", "LastingRestart")]
        [string] $Phase
    )

    $argument = switch ($Phase)
    {
        "Quickly" { "--verify-goal6a-quickly" }
        "QuicklyRestart" { "--verify-goal6a-quickly-restart" }
        "Lasting" { "--verify-goal6a-lasting" }
        "LastingRestart" { "--verify-goal6a-lasting-restart" }
    }
    $marker = switch ($Phase)
    {
        "Quickly" { "GOAL6A QUICKLY SAVE READY hud=map-first target=basalt-rejected scorch=present brute=dead save=9" }
        "QuicklyRestart" { "GOAL6A QUICKLY RESTART PASS scorch=present brute=dead hud=restored" }
        "Lasting" { "GOAL6A LASTING DEATH READY" }
        "LastingRestart" { "GOAL6A LASTING RESTART PASS incarnation=2 equipment=fresh scorch=present brute=dead save=9" }
    }

    Write-Host "`n==> Drive Goal 6A $Phase journey through the actual Godot HUD"
    $output = @(& $godot --path $godotProjectDirectory --position 32000,32000 --resolution 1600x900 -- $argument 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Goal 6A $Phase journey failed with exit code $exitCode."
    }

    if (-not $text.Contains($marker))
    {
        throw "Godot did not complete the expected Goal 6A $Phase journey."
    }

    $hudMarker = switch ($Phase)
    {
        "Quickly" { "GOAL6A HUD CAPTURE PASS stage=quickly-preparation size=1600x900 icons=4" }
        "Lasting" { "GOAL6A HUD CAPTURE PASS stage=lasting-interruption size=1600x900 icons=4" }
        default { $null }
    }
    if ($null -ne $hudMarker -and -not $text.Contains($hudMarker))
    {
        throw "Godot did not render and capture the required Goal 6A $Phase HUD state."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during Goal 6A $Phase."
    }
}

function Assert-Goal6AHudCapture {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("quickly-preparation", "lasting-interruption")]
        [string] $Stage
    )

    $capturePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\goal6a-$Stage-hud.png"
    if (-not (Test-Path -LiteralPath $capturePath -PathType Leaf) -or
        (Get-Item -LiteralPath $capturePath).Length -lt 4096)
    {
        throw "Goal 6A $Stage did not produce a substantive isolated player HUD capture."
    }
}

function Assert-Goal6ASave {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("Quickly", "LastingDeath", "LastingResolved")]
        [string] $Phase
    )

    $savePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    if (-not (Test-Path -LiteralPath $savePath -PathType Leaf))
    {
        throw "Goal 6A $Phase did not create its isolated Chronicle save."
    }

    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
    if ([int]$save.Version -ne 9 -or
        [int]$save.Chronicle.WorldGrammarVersion -ne 4 -or
        $null -eq $save.Chronicle.Combat -or
        $null -eq $save.Chronicle.Combat.Scorch)
    {
        throw "Goal 6A $Phase did not retain strict v9/WG4 combat and scorch state."
    }

    $bruteHp = [int]$save.Chronicle.Combat.MireBrute.HitPoints
    if ($Phase -eq "Quickly" -or $Phase -eq "LastingResolved")
    {
        if ($bruteHp -ne 0)
        {
            throw "Goal 6A $Phase did not retain the defeated Mire Brute."
        }
    }
    elseif ($save.Chronicle.IncarnationLife -ne 1 -or $bruteHp -le 0 -or $bruteHp -ge 45)
    {
        throw "Goal 6A LastingDeath did not retain awaiting replacement plus a wounded living Mire Brute."
    }
}

function Invoke-GodotGoal6BRun {
    Write-Host "`n==> Drive the complete Goal 6B journey through the actual Godot HUD"
    $output = @(& $godot --path $godotProjectDirectory --position 32000,32000 --resolution 1600x900 -- --verify-goal6b-visuals 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Goal 6B journey failed with exit code $exitCode."
    }

    $marker = "GOAL6B VISUAL ACCEPTANCE PASS captures=8 save=9 keyboard=mouse map=physical capacity=next-attunement"
    if (-not $text.Contains($marker))
    {
        throw "Godot did not complete the exact Goal 6B rendered journey."
    }

    $stages = @(
        "burn-primer",
        "embedded",
        "carried",
        "construction",
        "intact-capacity-ready",
        "damaged",
        "destroyed-current-next",
        "rebuilding"
    )
    foreach ($stage in $stages)
    {
        $captureMarker = "GOAL6B HUD CAPTURE PASS stage=$stage size=1600x900"
        if (-not $text.Contains($captureMarker))
        {
            throw "Godot did not report the required Goal 6B '$stage' HUD capture."
        }

        $capturePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\goal6b-$stage-hud.png"
        if (-not (Test-Path -LiteralPath $capturePath -PathType Leaf) -or
            (Get-Item -LiteralPath $capturePath).Length -lt 4096)
        {
            throw "Goal 6B '$stage' did not produce a substantive isolated 1600x900 HUD capture."
        }
    }

    $savePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    if (-not (Test-Path -LiteralPath $savePath -PathType Leaf))
    {
        throw "Goal 6B did not create its isolated strict v9 Chronicle save."
    }

    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
    if ([int]$save.Version -ne 9 -or
        [int]$save.Chronicle.WorldGrammarVersion -ne 5 -or
        $null -eq $save.Chronicle.PowerHome -or
        [int]$save.Chronicle.PowerHome.Resonator.Phase -ne 2 -or
        [int]$save.Chronicle.Attunement.Capacity -ne 12)
    {
        throw "Goal 6B did not retain strict v9/WG5 rebuilt Source and twelve-Load Attunement state."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during Goal 6B rendered acceptance."
    }
}

function Invoke-GodotGoal7ARun {
    Write-Host "`n==> Drive both bounded Goal 7A journeys through the actual Godot HUD"
    $output = @(& $godot --path $godotProjectDirectory --position 32000,32000 --resolution 1600x900 -- --verify-goal7a-visuals 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Goal 7A journey failed with exit code $exitCode."
    }

    $marker = "GOAL7A VISUAL ACCEPTANCE PASS captures=6 save=9 keyboard=mouse agent=consequential relationship=guest"
    if (-not $text.Contains($marker))
    {
        throw "Godot did not complete the exact Goal 7A rendered journeys."
    }

    $stages = @(
        "approaching",
        "waiting",
        "open-offer",
        "accepted-guest",
        "restored-guest",
        "replacement-return"
    )
    foreach ($stage in $stages)
    {
        $captureMarker = "GOAL7A HUD CAPTURE PASS stage=$stage size=1600x900"
        if (-not $text.Contains($captureMarker))
        {
            throw "Godot did not report the required Goal 7A '$stage' HUD capture."
        }

        $capturePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\goal7a-$stage-hud.png"
        if (-not (Test-Path -LiteralPath $capturePath -PathType Leaf) -or
            (Get-Item -LiteralPath $capturePath).Length -lt 4096)
        {
            throw "Goal 7A '$stage' did not produce a substantive isolated 1600x900 HUD capture."
        }
    }

    $savePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    if (-not (Test-Path -LiteralPath $savePath -PathType Leaf))
    {
        throw "Goal 7A did not create its isolated strict v9 Chronicle save."
    }

    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
    $agents = @($save.Chronicle.Agents)
    if ([int]$save.Version -ne 9 -or
        [int]$save.Chronicle.WorldGrammarVersion -ne 6 -or
        [int]$save.Chronicle.IncarnationId -ne 2 -or
        $agents.Count -ne 1 -or
        $agents[0].Profile.DisplayName -ne "Tamar Venn" -or
        [int]$agents[0].Presence -ne 3 -or
        [int]$agents[0].Need.Status -ne 3 -or
        [int]$agents[0].HomeRelationship.Kind -ne 3 -or
        [int]$agents[0].HomeRelationship.WelcomingIncarnationId -ne 1 -or
        $null -eq $agents[0].RoadRollAddress)
    {
        throw "Goal 7A did not retain strict-v9 WG6 identity, Guest history, prior-Incarnation cause, and road-roll."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during Goal 7A rendered acceptance."
    }
}

function Invoke-GodotGoal7BRun {
    Write-Host "`n==> Drive both bounded Goal 7B journeys through the actual Godot HUD"
    $output = @(& $godot --path $godotProjectDirectory --position 32000,32000 --resolution 1600x900 -- --verify-goal7b-visuals 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output -join [Environment]::NewLine

    if ($exitCode -ne 0)
    {
        throw "Godot Goal 7B journey failed with exit code $exitCode."
    }

    $marker = "GOAL7B VISUAL ACCEPTANCE PASS captures=8 save=9 inspection=keyboard+mouse directive=agency"
    if (-not $text.Contains($marker))
    {
        throw "Godot did not complete the exact Goal 7B rendered journeys."
    }

    $stages = @(
        "inspected-road-roll",
        "safe-suggest-preview",
        "safe-pending",
        "accepted-movement",
        "dangerous-suggest-rejection",
        "dangerous-command-pending",
        "refusal",
        "restored-refusal"
    )
    foreach ($stage in $stages)
    {
        $captureMarker = "GOAL7B HUD CAPTURE PASS stage=$stage size=1600x900"
        if (-not $text.Contains($captureMarker))
        {
            throw "Godot did not report the required Goal 7B '$stage' HUD capture."
        }

        $capturePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\goal7b-$stage-hud.png"
        if (-not (Test-Path -LiteralPath $capturePath -PathType Leaf) -or
            (Get-Item -LiteralPath $capturePath).Length -lt 4096)
        {
            throw "Goal 7B '$stage' did not produce a substantive isolated 1600x900 HUD capture."
        }
    }

    $savePath = Join-Path $env:APPDATA "Godot\app_userdata\Untitled Chronicle RPG\slice0_chronicle.json"
    if (-not (Test-Path -LiteralPath $savePath -PathType Leaf))
    {
        throw "Goal 7B did not create its isolated strict v9 Chronicle save."
    }

    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
    $agents = @($save.Chronicle.Agents)
    $memories = @($agents[0].DirectiveMemories)
    $codex = @($save.Chronicle.Codex.Words)
    if ([int]$save.Version -ne 9 -or
        [int]$save.Chronicle.WorldGrammarVersion -ne 6 -or
        $agents.Count -ne 1 -or
        $agents[0].Profile.DisplayName -ne "Tamar Venn" -or
        [int]$agents[0].HomeRelationship.Kind -ne 3 -or
        $null -ne $agents[0].PendingDirective -or
        $memories.Count -ne 1 -or
        [int]$memories[0].Response -ne 3 -or
        [int]$memories[0].Reason -ne 3 -or
        $memories[0].Verb -ne "word.command" -or
        -not $codex.Contains("word.suggest") -or
        -not $codex.Contains("word.command"))
    {
        throw "Goal 7B did not retain strict-v9 WG6 social Words, Guest agency, Command refusal, and exact memory."
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "Godot reported an error during Goal 7B rendered acceptance."
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
            "GATE3B SHARED COMPOSER PLAN PASS pack=$expectedPack style=2 size=$VisualCellSize"
        if (-not $text.Contains($expectedComposerMarker))
        {
            throw "$Label did not use the expected Gate 3B shared-composer pack and cell size."
        }

        $expectedHistoricalDigest = if ($VisualCellSize -eq 20 -and -not $ManualComparison)
        {
            "sha256:0c5d7ba3914a594598a95c28a70ce62ed3e2b1c5ebb24ebbb8c043d78d13170a"
        }
        else
        {
            "sha256:87e56029a56dfe042e3355527d96f7f8d433f7471da0042911d9384abc8b7647"
        }
        if (-not $text.Contains("$expectedComposerMarker digest=$expectedHistoricalDigest"))
        {
            throw "$Label changed the accepted pre-6C Inspector render-plan digest."
        }

        $expectedPreviewMarker = "GATE3B ATLAS VISUAL PREVIEW PASS size=$VisualCellSize"
        if (-not $text.Contains($expectedPreviewMarker))
        {
            throw "$Label did not complete the expected Gate 3B Atlas visual preview."
        }

        $expectedGoal6BMarker =
            "GOAL6B INSPECTOR PARITY PASS states=8 pack=$expectedPack size=$VisualCellSize"
        if (-not $text.Contains($expectedGoal6BMarker))
        {
            throw "$Label did not prove Goal 6B Inspector parity for every bounded state."
        }

        $expectedGoal7AMarker =
            "GOAL7A INSPECTOR PARITY PASS states=6 pack=$expectedPack size=$VisualCellSize"
        if (-not $text.Contains($expectedGoal7AMarker))
        {
            throw "$Label did not prove Goal 7A packaged/manual Inspector parity for every bounded Agent state."
        }

        $expectedGoal7BMarker =
            "GOAL7B INSPECTOR PARITY PASS states=9 pack=$expectedPack size=$VisualCellSize"
        if (-not $text.Contains($expectedGoal7BMarker))
        {
            throw "$Label did not prove Goal 7B packaged/manual Inspector parity for every bounded Directive state."
        }
    }

    if ($text -match "(?m)(SCRIPT ERROR|ERROR:)")
    {
        throw "$Label reported a Godot error."
    }
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
    # Verification uses the pinned, repository-local package graph. Disable
    # the network vulnerability feed so the hermetic gate remains warning-free.
    $env:NuGetAudit = "false"

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

    Assert-PlayerSaveAbsent "Goal 6B Inspector acceptance before launch"
    Invoke-GodotAtlasRun "Run retained semantic World Atlas Inspector against current World Grammar" $false
    Invoke-GodotAtlasRun "Run retained native P-GEN World Atlas visual proof" $false -VisualCellSize 20
    Invoke-GodotAtlasRun "Run retained native manual-pack comparison proof" $false -VisualCellSize 20 -ManualComparison
    Assert-PlayerSaveAbsent "Goal 6B Inspector acceptance after launch"

    $env:APPDATA = (New-Item -ItemType Directory -Force (Join-Path $runtimeRoot "Roaming")).FullName
    $env:LOCALAPPDATA = (New-Item -ItemType Directory -Force (Join-Path $runtimeRoot "Local")).FullName

    Invoke-GodotEditorBuild
    Invoke-GodotStartup "Start the current game headlessly and create strict v9 state"
    Invoke-GodotStartup "Restart the current game headlessly and restore strict v9 state" -RequireLoad

    Set-IsolatedGodotRuntime $goal6AQuicklyRuntimeRoot "Journey"
    Assert-PlayerSaveAbsent "Goal 6A Quickly acceptance before launch"
    Invoke-GodotGoal6ARun "Quickly"
    Assert-Goal6AHudCapture "quickly-preparation"
    Assert-Goal6ASave "Quickly"
    Invoke-GodotGoal6ARun "QuicklyRestart"
    Assert-Goal6ASave "Quickly"

    Set-IsolatedGodotRuntime $goal6ALastingRuntimeRoot "Journey"
    Assert-PlayerSaveAbsent "Goal 6A Lasting acceptance before launch"
    Invoke-GodotGoal6ARun "Lasting"
    Assert-Goal6AHudCapture "lasting-interruption"
    Assert-Goal6ASave "LastingDeath"
    Invoke-GodotGoal6ARun "LastingRestart"
    Assert-Goal6ASave "LastingResolved"

    Set-IsolatedGodotRuntime $goal6BRuntimeRoot "Journey"
    Assert-PlayerSaveAbsent "Goal 6B acceptance before launch"
    Invoke-GodotGoal6BRun

    Set-IsolatedGodotRuntime $goal7ARuntimeRoot "Journey"
    Assert-PlayerSaveAbsent "Goal 7A acceptance before launch"
    Invoke-GodotGoal7ARun

    Set-IsolatedGodotRuntime $goal7BRuntimeRoot "Journey"
    Assert-PlayerSaveAbsent "Goal 7B acceptance before launch"
    Invoke-GodotGoal7BRun

    Write-Host "`nPASS: P-GEN authoring verification, Chronicle.Core strict-v9 and literal migration checks, Chronicle.Visuals checks, Godot editor build, exact four-file packaging, World Atlas Inspector packaged/manual parity, both Goal 6A journeys, retained Goal 6B/7A, and Goal 7B with eight rendered HUD proofs."
}
finally
{
    $env:APPDATA = $originalEnvironment.AppData
    $env:DOTNET_CLI_HOME = $originalEnvironment.DotnetCliHome
    $env:DOTNET_MULTILEVEL_LOOKUP = $originalEnvironment.DotnetMultilevelLookup
    $env:DOTNET_ROOT = $originalEnvironment.DotnetRoot
    $env:LOCALAPPDATA = $originalEnvironment.LocalAppData
    $env:NUGET_PACKAGES = $originalEnvironment.NugetPackages
    $env:NuGetAudit = $originalEnvironment.NugetAudit
    $env:PATH = $originalEnvironment.Path

    Pop-Location -ErrorAction SilentlyContinue

    Remove-VerificationRuntimeRoot $runtimeRoot (Join-Path $repoRoot ".tools\godot-verify-")
    Remove-VerificationRuntimeRoot $atlasRuntimeRoot (Join-Path $repoRoot ".tools\godot-atlas-verify-")
    Remove-VerificationRuntimeRoot $goal6AQuicklyRuntimeRoot (Join-Path $repoRoot ".tools\godot-goal6a-quickly-verify-")
    Remove-VerificationRuntimeRoot $goal6ALastingRuntimeRoot (Join-Path $repoRoot ".tools\godot-goal6a-lasting-verify-")
    Remove-VerificationRuntimeRoot $goal6BRuntimeRoot (Join-Path $repoRoot ".tools\godot-goal6b-verify-")
    Remove-VerificationRuntimeRoot $goal7ARuntimeRoot (Join-Path $repoRoot ".tools\godot-goal7a-verify-")
    Remove-VerificationRuntimeRoot $goal7BRuntimeRoot (Join-Path $repoRoot ".tools\godot-goal7b-verify-")
}
