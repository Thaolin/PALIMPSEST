[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$dotnetCandidates = @(
    $env:CHRONICLE_DOTNET,
    (Join-Path $root '.tools\dotnet\dotnet.exe'),
    'C:\DEV\PALIMPSEST\.tools\dotnet\dotnet.exe',
    (Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source)
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
$dotnet = $dotnetCandidates | Select-Object -First 1
if (-not $dotnet) {
    throw 'Missing .NET SDK 8.0.423. Set CHRONICLE_DOTNET to its dotnet executable.'
}
if ((& $dotnet --version) -ne '8.0.423') {
    throw "Expected .NET SDK 8.0.423 at '$dotnet'."
}

$env:DOTNET_ROOT = Split-Path -Parent $dotnet
$env:DOTNET_CLI_HOME = Join-Path $root '.cache\dotnet-cli'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
if (-not $env:NUGET_PACKAGES) {
    $localPackages = Join-Path $root '.tools\nuget-packages'
    $sharedPackages = 'C:\DEV\PALIMPSEST\.tools\nuget-packages'
    $env:NUGET_PACKAGES = if (Test-Path -LiteralPath $localPackages) {
        $localPackages
    } elseif (Test-Path -LiteralPath $sharedPackages) {
        $sharedPackages
    } else {
        Join-Path $root '.cache\nuget-packages'
    }
}

$conformance = Join-Path $root 'src\Chronicle.Visuals.Conformance\Chronicle.Visuals.Conformance.csproj'
& $dotnet restore $conformance --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
& $dotnet build $conformance --configuration Release --no-restore -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
& $dotnet run --project $conformance --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Conformance failed.' }

$cli = Join-Path $root 'src\Chronicle.VisualCompiler.Cli\Chronicle.VisualCompiler.Cli.csproj'
$catalogue = Join-Path $root 'catalogues\e3.json'
$outputA = Join-Path $root 'artifacts\e4\build-a'
$outputB = Join-Path $root 'artifacts\e4\build-b'
$proofRoot = [System.IO.Path]::GetFullPath((Join-Path $root 'artifacts\e4'))
foreach ($output in @($outputA, $outputB)) {
    $resolved = [System.IO.Path]::GetFullPath($output)
    if (-not $resolved.StartsWith(
            $proofRoot + [System.IO.Path]::DirectorySeparatorChar,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe proof output path '$resolved'."
    }
    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
& $dotnet build $cli --configuration Release -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'CLI build failed.' }
$compileTimer = [System.Diagnostics.Stopwatch]::StartNew()
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --catalogue $catalogue --output $outputA
$compileTimer.Stop()
if ($LASTEXITCODE -ne 0) { throw 'First CLI compile failed.' }
if ($compileTimer.Elapsed.TotalSeconds -ge 10) {
    throw "E4 compile exceeded ten seconds: $($compileTimer.Elapsed.TotalSeconds)."
}
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --catalogue $catalogue --output $outputB
if ($LASTEXITCODE -ne 0) { throw 'Second CLI compile failed.' }

$filesA = Get-ChildItem -LiteralPath $outputA -File -Recurse |
    ForEach-Object { $_.FullName.Substring($outputA.Length + 1) } |
    Sort-Object
$filesB = Get-ChildItem -LiteralPath $outputB -File -Recurse |
    ForEach-Object { $_.FullName.Substring($outputB.Length + 1) } |
    Sort-Object
if (Compare-Object $filesA $filesB) {
    throw 'CLI builds emitted different file sets.'
}
foreach ($relativePath in $filesA) {
    $hashA = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $outputA $relativePath)).Hash
    $hashB = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $outputB $relativePath)).Hash
    if ($hashA -ne $hashB) {
        throw "CLI builds differ at '$relativePath'."
    }
}

$unowned = Join-Path $root 'artifacts\e4\unowned'
if (Test-Path -LiteralPath $unowned) {
    Remove-Item -LiteralPath $unowned -Recurse -Force
}
New-Item -ItemType Directory -Path $unowned | Out-Null
$sentinel = Join-Path $unowned 'keep.txt'
Set-Content -LiteralPath $sentinel -Value 'keep' -NoNewline
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --catalogue $catalogue --output $unowned
if ($LASTEXITCODE -eq 0 -or -not (Test-Path -LiteralPath $sentinel)) {
    throw 'CLI replaced an unowned output directory.'
}

$stagingTarget = Join-Path $root 'artifacts\e4\staging-target'
$unownedStaging = $stagingTarget + '.staging'
foreach ($path in @($stagingTarget, $unownedStaging)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}
New-Item -ItemType Directory -Path $unownedStaging | Out-Null
$stagingSentinel = Join-Path $unownedStaging 'keep.txt'
Set-Content -LiteralPath $stagingSentinel -Value 'keep' -NoNewline
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --catalogue $catalogue --output $stagingTarget
if ($LASTEXITCODE -eq 0 -or -not (Test-Path -LiteralPath $stagingSentinel)) {
    throw 'CLI replaced an unowned staging directory.'
}

$godotCandidates = @(
    $env:CHRONICLE_GODOT,
    (Join-Path $root '.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'),
    'C:\DEV\PALIMPSEST\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
$godot = $godotCandidates | Select-Object -First 1
if (-not $godot -or (& $godot --version) -notlike '4.7.1.stable.mono*') {
    throw 'Missing Godot 4.7.1 stable .NET console executable.'
}

$previewRoot = Join-Path $root 'src\Chronicle.VisualPreview.Godot'
$previewProject = Join-Path $previewRoot 'Chronicle.VisualPreview.Godot.csproj'
$plan = Join-Path $root 'fixtures\preview-plans\e4-acceptance.json'
$captureA = Join-Path $proofRoot 'godot-a'
$captureB = Join-Path $proofRoot 'godot-b'
foreach ($capture in @($captureA, $captureB)) {
    if (Test-Path -LiteralPath $capture) {
        Remove-Item -LiteralPath $capture -Recurse -Force
    }
}
$env:APPDATA = Join-Path $root '.cache\godot-appdata'
$env:LOCALAPPDATA = Join-Path $root '.cache\godot-localappdata'
New-Item -ItemType Directory -Force -Path $env:APPDATA,$env:LOCALAPPDATA |
    Out-Null
& $dotnet build $previewProject --configuration Debug -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Godot preview build failed.' }

[xml]$previewXml = Get-Content -Raw -LiteralPath $previewProject
$references = @($previewXml.Project.ItemGroup.ProjectReference.Include)
if ($references.Count -ne 1 -or
    $references[0] -notlike '*Chronicle.VisualPack.csproj') {
    throw 'Godot preview must reference only Chronicle.VisualPack.'
}

$godotBefore = @(Get-Process -ErrorAction SilentlyContinue |
    Where-Object ProcessName -Like 'Godot*' |
    Select-Object -ExpandProperty Id)
$logA = (Join-Path $proofRoot 'godot-a.log').Replace('\', '/')
$logB = (Join-Path $proofRoot 'godot-b.log').Replace('\', '/')
$interactiveLog = (Join-Path $proofRoot 'godot-interactive.log').Replace('\', '/')
$godotPreviewRoot = $previewRoot.Replace('\', '/')
$godotOutputA = $outputA.Replace('\', '/')
$godotOutputB = $outputB.Replace('\', '/')
$godotPlan = $plan.Replace('\', '/')
$godotCaptureA = $captureA.Replace('\', '/')
$godotCaptureB = $captureB.Replace('\', '/')
& $godot --headless --quit-after 3 --log-file $interactiveLog `
    --path $godotPreviewRoot -- `
    --pack $godotOutputA --plan $godotPlan
if ($LASTEXITCODE -ne 0) { throw 'Interactive preview headless launch failed.' }
function Invoke-GodotCapture(
    [string]$log,
    [string]$pack,
    [string]$capture
) {
    $arguments = @(
        '--rendering-driver', 'opengl3',
        '--position', '10000,10000',
        '--log-file', $log,
        '--path', $godotPreviewRoot,
        '--',
        '--pack', $pack,
        '--plan', $godotPlan,
        '--acceptance',
        '--output', $capture
    )
    $process = Start-Process `
        -FilePath $godot `
        -ArgumentList $arguments `
        -Wait `
        -PassThru `
        -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "Godot viewport acceptance failed with exit $($process.ExitCode)."
    }
}
Invoke-GodotCapture $logA $godotOutputA $godotCaptureA
Invoke-GodotCapture $logB $godotOutputB $godotCaptureB

$captureFilesA = Get-ChildItem -LiteralPath $captureA -File |
    Select-Object -ExpandProperty Name |
    Sort-Object
$captureFilesB = Get-ChildItem -LiteralPath $captureB -File |
    Select-Object -ExpandProperty Name |
    Sort-Object
if (Compare-Object $captureFilesA $captureFilesB) {
    throw 'Godot acceptances emitted different file sets.'
}
foreach ($name in $captureFilesA) {
    $captureHashA = (Get-FileHash -Algorithm SHA256 -LiteralPath (
        Join-Path $captureA $name)).Hash
    $captureHashB = (Get-FileHash -Algorithm SHA256 -LiteralPath (
        Join-Path $captureB $name)).Hash
    if ($captureHashA -ne $captureHashB) {
        throw "Godot acceptances differ at '$name'."
    }
}

$newGodotProcesses = @(Get-Process -ErrorAction SilentlyContinue |
    Where-Object ProcessName -Like 'Godot*' |
    Where-Object { $_.Id -notin $godotBefore })
if ($newGodotProcesses.Count -ne 0) {
    $newGodotProcesses | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
}
$remainingGodotProcesses = @(Get-Process -ErrorAction SilentlyContinue |
    Where-Object ProcessName -Like 'Godot*' |
    Where-Object { $_.Id -notin $godotBefore })
if ($remainingGodotProcesses.Count -ne 0) {
    throw 'Godot acceptance did not shut down within ten seconds.'
}
foreach ($godotLog in @($interactiveLog, $logA, $logB)) {
    if (Select-String -LiteralPath $godotLog -Pattern 'ERROR:' -Quiet) {
        throw "Godot reported an error in '$godotLog'."
    }
}
Write-Host (
    "E4 compile milliseconds: {0}; pack: {1}" -f
    [Math]::Round($compileTimer.Elapsed.TotalMilliseconds),
    (Get-Content -Raw (Join-Path $outputA 'hashes.json') |
        ConvertFrom-Json).aggregatePackDigest)
