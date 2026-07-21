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

function Get-RelativeFiles {
    param([Parameter(Mandatory)][string]$Directory)

    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        throw "Expected directory '$Directory' was not produced."
    }

    return @(
        Get-ChildItem -LiteralPath $Directory -File -Recurse |
            ForEach-Object {
                $_.FullName.Substring($Directory.Length + 1).Replace('\', '/')
            } |
            Sort-Object
    )
}

function Test-ByteEquality {
    param(
        [Parameter(Mandatory)][string]$Left,
        [Parameter(Mandatory)][string]$Right
    )

    [byte[]]$leftBytes = [System.IO.File]::ReadAllBytes($Left)
    [byte[]]$rightBytes = [System.IO.File]::ReadAllBytes($Right)
    if ($leftBytes.Length -ne $rightBytes.Length) {
        return $false
    }

    for ($index = 0; $index -lt $leftBytes.Length; $index++) {
        if ($leftBytes[$index] -ne $rightBytes[$index]) {
            return $false
        }
    }
    return $true
}

function Get-Sha256Digest {
    param([Parameter(Mandatory)][string]$Path)

    return 'sha256:' + ((Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant())
}

$canonicalPackPaths = @(
    'atlases/palimpsest20.indices',
    'hashes.json',
    'manifest.json',
    'validation.json'
)
$hashedPackPaths = @(
    'atlases/palimpsest20.indices',
    'manifest.json',
    'validation.json'
)
$requiredReviewPaths = @(
    'review/adjacency-20.png',
    'review/authoring-evidence.json',
    'review/layers-20.png',
    'review/manual-baseline-20.png',
    'review/motifs-20.png',
    'review/native-20.png',
    'review/nearest-20.png',
    'review/shifted-overlap-20.png',
    'review/variants-20.png'
)
$requiredManualBaselineFamilies = @(
    'baseline.actor.incarnation',
    'baseline.emphasis.selection',
    'baseline.emphasis.target.valid',
    'baseline.glyph.codex',
    'baseline.landmark.bell-that-fell-up',
    'baseline.subject.loose-stone'
)

$acceptedReferenceFixture = Join-Path $root 'fixtures\palimpsest20\accepted-reference.json'
$requiredReferenceCommit = '15917b3'
$requiredReferenceVisualIdSetDigest =
    'sha256:7f2f0c09ddc9e84f483c580513a11ad79a62188c861a973cc01044a9e4e88729'

function Assert-CanonicalPackFiles {
    param(
        [Parameter(Mandatory)][string]$PackDirectory,
        [Parameter(Mandatory)][string]$Label
    )

    $actualFiles = @(Get-RelativeFiles $PackDirectory)
    if ($actualFiles.Count -ne $canonicalPackPaths.Count -or
        (Compare-Object $canonicalPackPaths $actualFiles)) {
        throw "$Label contains an incomplete or unexpected Palimpsest20 pack file set."
    }

    $actualDirectories = @(
        Get-ChildItem -LiteralPath $PackDirectory -Directory -Recurse |
            ForEach-Object {
                $_.FullName.Substring($PackDirectory.Length + 1).Replace('\', '/')
            } |
            Sort-Object
    )
    if ($actualDirectories.Count -ne 1 -or $actualDirectories[0] -cne 'atlases') {
        throw "$Label contains an unexpected Palimpsest20 pack directory layout."
    }
}

function Assert-PinnedPalimpsest20Pack {
    param(
        [Parameter(Mandatory)][string]$PackDirectory,
        [Parameter(Mandatory)][hashtable]$ExpectedByPath,
        [Parameter(Mandatory)]$Expected,
        [Parameter(Mandatory)][string]$Label
    )

    Assert-CanonicalPackFiles $PackDirectory $Label
    foreach ($relativePath in $canonicalPackPaths) {
        $actualDigest = Get-Sha256Digest (Join-Path $PackDirectory $relativePath)
        if ($actualDigest -cne $ExpectedByPath[$relativePath]) {
            throw "$Label digest drift at '$relativePath': expected '$($ExpectedByPath[$relativePath])', got '$actualDigest'."
        }
    }

    $manifest = Get-Content -Raw -LiteralPath (Join-Path $PackDirectory 'manifest.json') |
        ConvertFrom-Json
    if ($manifest.palimpsestDigest -cne $Expected.palimpsestDigest) {
        throw "$Label manifest palimpsestDigest does not match the committed E4.5 pin."
    }

    $hashDocument = Get-Content -Raw -LiteralPath (Join-Path $PackDirectory 'hashes.json') |
        ConvertFrom-Json
    if ($hashDocument.algorithm -cne 'sha256' -or
        $hashDocument.palimpsestDigest -cne $Expected.palimpsestDigest -or
        $hashDocument.aggregateDigest -cne $Expected.aggregateDigest) {
        throw "$Label hashes.json does not match the committed E4.5 Palimpsest20 digests."
    }

    $actualHashEntries = @($hashDocument.files)
    if ($actualHashEntries.Count -ne $hashedPackPaths.Count) {
        throw "$Label hashes.json has an unexpected canonical file digest count."
    }

    $actualHashByPath = @{}
    foreach ($entry in $actualHashEntries) {
        $path = [string]$entry.path
        if ([string]::IsNullOrWhiteSpace($path) -or
            $actualHashByPath.ContainsKey($path)) {
            throw "$Label hashes.json contains an invalid or duplicate canonical digest path."
        }
        $actualHashByPath[$path] = [string]$entry.digest
    }
    if ($actualHashByPath.Count -ne $hashedPackPaths.Count -or
        (Compare-Object $hashedPackPaths @($actualHashByPath.Keys | Sort-Object))) {
        throw "$Label hashes.json has an incomplete or unexpected canonical digest set."
    }
    foreach ($relativePath in $hashedPackPaths) {
        $actualDigest = Get-Sha256Digest (Join-Path $PackDirectory $relativePath)
        if ($actualHashByPath[$relativePath] -cne $actualDigest -or
            $actualHashByPath[$relativePath] -cne $ExpectedByPath[$relativePath]) {
            throw "$Label hashes.json digest mismatch at '$relativePath'."
        }
    }
}

function Assert-E45ReviewArtifacts {
    param(
        [Parameter(Mandatory)][string]$OutputDirectory,
        [Parameter(Mandatory)][string]$Label
    )

    $packDirectory = Join-Path $OutputDirectory 'pack'
    $packedReviewDirectory = Join-Path $packDirectory 'review'
    if (Test-Path -LiteralPath $packedReviewDirectory) {
        throw "$Label incorrectly contains review artifacts inside the canonical pack."
    }

    foreach ($relativePath in $requiredReviewPaths) {
        $artifact = Join-Path $OutputDirectory $relativePath
        if (-not (Test-Path -LiteralPath $artifact -PathType Leaf)) {
            throw "$Label is missing required review artifact '$relativePath'."
        }
    }

    $manifest = Get-Content -Raw -LiteralPath (Join-Path $packDirectory 'manifest.json') |
        ConvertFrom-Json
    $definitions = @($manifest.definitions)
    if ($definitions.Count -ne 181) {
        throw "$Label must export exactly 181 visual definitions, got $($definitions.Count)."
    }
    if (@($definitions | Where-Object {
                $_.visualId -like 'baseline.*' -or $_.familyId -like 'baseline.*'
            }).Count -ne 0) {
        throw "$Label incorrectly exports review-only baseline definitions."
    }

    $authoringEvidence = Get-Content -Raw -LiteralPath (
        Join-Path $OutputDirectory 'review/authoring-evidence.json') |
        ConvertFrom-Json
    $comparisonLayout = $authoringEvidence.comparisonLayout
    if (-not $comparisonLayout -or
        $comparisonLayout.columns[0] -cne 'accepted-reference' -or
        $comparisonLayout.columns[1] -cne 'candidate') {
        throw "$Label authoring evidence is missing or has incorrect comparison layout."
    }
    $refProv = $comparisonLayout.referenceProvenance
    if ($refProv.sourceCommit -cne $requiredReferenceCommit) {
        throw "$Label authoring evidence has wrong reference provenance commit."
    }
    $acceptedRef = $authoringEvidence.acceptedReference
    if (-not $acceptedRef -or
        [string]::IsNullOrWhiteSpace($acceptedRef.aggregateDigest) -or
        $acceptedRef.visualCount -ne 64 -or
        $acceptedRef.visualIdSetDigest -cne $requiredReferenceVisualIdSetDigest -or
        $acceptedRef.comparedVisualCount -ne 64) {
        throw "$Label authoring evidence is missing accepted reference identity."
    }

    $comparedVisuals = @($acceptedRef.comparedVisuals)
    if ($comparedVisuals.Count -ne 64) {
        throw "$Label authoring evidence does not list all 64 accepted/candidate comparisons."
    }
    $comparedIds = [string[]]@(
        $comparedVisuals | ForEach-Object { [string]$_.referenceVisualId }
    )
    [Array]::Sort($comparedIds, [StringComparer]::Ordinal)
    $comparedIdBytes = [Text.Encoding]::UTF8.GetBytes($comparedIds -join "`n")
    $comparedIdDigest = 'sha256:' + [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($comparedIdBytes)
    ).ToLowerInvariant()
    if ($comparedIdDigest -cne $requiredReferenceVisualIdSetDigest) {
        throw "$Label authoring evidence comparison IDs do not match the accepted reference set."
    }

    $comparisonPng = [IO.File]::ReadAllBytes(
        (Join-Path $OutputDirectory 'review/manual-baseline-20.png'))
    if ($comparisonPng.Length -lt 24) {
        throw "$Label manual comparison PNG is truncated."
    }
    $comparisonWidth =
        ([int]$comparisonPng[16] -shl 24) -bor
        ([int]$comparisonPng[17] -shl 16) -bor
        ([int]$comparisonPng[18] -shl 8) -bor
        [int]$comparisonPng[19]
    $comparisonHeight =
        ([int]$comparisonPng[20] -shl 24) -bor
        ([int]$comparisonPng[21] -shl 16) -bor
        ([int]$comparisonPng[22] -shl 8) -bor
        [int]$comparisonPng[23]
    if ($comparisonWidth -ne 40 -or $comparisonHeight -ne 1280) {
        throw "$Label manual comparison PNG must be 40x1280, got ${comparisonWidth}x${comparisonHeight}."
    }
}

function Assert-AcceptedReferenceFixture {
    if (-not (Test-Path -LiteralPath $acceptedReferenceFixture -PathType Leaf)) {
        throw "Accepted reference fixture is missing."
    }

    $fixture = Get-Content -Raw -LiteralPath $acceptedReferenceFixture | ConvertFrom-Json

    if ($fixture.provenance.repository -cne 'Palimpsest') {
        throw "Accepted reference fixture provenance.repository must be 'Palimpsest'."
    }
    if ($fixture.provenance.sourceCommit -cne '15917b3') {
        throw "Accepted reference fixture provenance.sourceCommit must be '15917b3'."
    }
    if ($fixture.provenance.sourceFile -cne 'src/Chronicle.VisualPack/ManualVisualPack.cs') {
        throw "Accepted reference fixture provenance.sourceFile must be 'src/Chronicle.VisualPack/ManualVisualPack.cs'."
    }
    if ($fixture.provenance.nativeSize -ne 20) {
        throw "Accepted reference fixture provenance.nativeSize must be 20."
    }

    if ($fixture.aggregateDigest -cne 'sha256:16878ef64d5daef65680ccd2a0b3ae335fd725fb76c908a91e30970d6561ff07') {
        throw "Accepted reference fixture aggregate digest is incorrect."
    }

    $palette = $fixture.palette
    if ($palette.Count -ne 28) {
        throw "Accepted reference fixture must have exactly 28 palette entries, got $($palette.Count)."
    }
    $paletteIndices = @($palette | ForEach-Object { $_.index } | Sort-Object)
    for ($i = 0; $i -lt 28; $i++) {
        if ($paletteIndices[$i] -ne $i) {
            throw "Accepted reference fixture palette indices must be 0..27 contiguous."
        }
    }
    $paletteByIndex = @{}
    foreach ($entry in $palette) {
        $paletteByIndex["$($entry.index)"] = $entry.rgba
    }

    $visuals = $fixture.visuals
    if ($visuals.Count -ne 64) {
        throw "Accepted reference fixture must have exactly 64 visual entries, got $($visuals.Count)."
    }

    $visualIds = @($visuals | ForEach-Object { $_.visualId })
    $uniqueIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($id in $visualIds) {
        if (-not $uniqueIds.Add($id)) {
            throw "Accepted reference fixture has duplicate visual ID '$id'."
        }
    }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $utf8 = [System.Text.Encoding]::UTF8

    $sortedVisualIds = [string[]]$visualIds.Clone()
    [Array]::Sort($sortedVisualIds, [StringComparer]::Ordinal)
    $computedVisualIdSetDigest = 'sha256:' + [System.Convert]::ToHexString(
        $sha256.ComputeHash($utf8.GetBytes($sortedVisualIds -join "`n"))
    ).ToLowerInvariant()
    if ($computedVisualIdSetDigest -cne $requiredReferenceVisualIdSetDigest) {
        throw "Accepted reference fixture visual ID set mismatch.`n  Expected: $requiredReferenceVisualIdSetDigest`n  Computed: $computedVisualIdSetDigest"
    }

    foreach ($visual in $visuals) {
        $id = $visual.visualId
        if ([string]::IsNullOrWhiteSpace($id)) {
            throw "Accepted reference fixture has a visual with a blank ID."
        }

        $indexedDigest = $visual.indexedDigest
        if ([string]::IsNullOrWhiteSpace($indexedDigest)) {
            throw "Accepted reference fixture visual '$id' is missing indexedDigest."
        }

        $rgbaDigest = $visual.rgbaDigest
        if ([string]::IsNullOrWhiteSpace($rgbaDigest)) {
            throw "Accepted reference fixture visual '$id' is missing rgbaDigest."
        }

        $bufferB64 = $visual.indexedBuffer
        if ([string]::IsNullOrWhiteSpace($bufferB64)) {
            throw "Accepted reference fixture visual '$id' is missing indexedBuffer."
        }

        $indexedBuffer = [System.Convert]::FromBase64String($bufferB64)
        if ($indexedBuffer.Length -ne 400) {
            throw "Accepted reference fixture visual '$id' indexedBuffer must be exactly 400 bytes, got $($indexedBuffer.Length)."
        }

        for ($i = 0; $i -lt 400; $i++) {
            if ($indexedBuffer[$i] -ge 28) {
                throw "Accepted reference fixture visual '$id' has palette index $($indexedBuffer[$i]) out of range."
            }
        }

        $computedIndexedDigest = 'sha256:' + [System.Convert]::ToHexString(
            $sha256.ComputeHash($indexedBuffer)).ToLowerInvariant()
        if ($computedIndexedDigest -cne $indexedDigest) {
            throw "Accepted reference fixture visual '$id' indexedDigest mismatch.`n  Committed: $indexedDigest`n  Computed:  $computedIndexedDigest"
        }

        $rgbaBytes = [byte[]]::new(1600)
        for ($i = 0; $i -lt 400; $i++) {
            $hex = $paletteByIndex["$($indexedBuffer[$i])"]
            $offset = $i * 4
            $rgbaBytes[$offset] = [System.Convert]::ToByte($hex.Substring(0, 2), 16)
            $rgbaBytes[$offset + 1] = [System.Convert]::ToByte($hex.Substring(2, 2), 16)
            $rgbaBytes[$offset + 2] = [System.Convert]::ToByte($hex.Substring(4, 2), 16)
            $rgbaBytes[$offset + 3] = [System.Convert]::ToByte($hex.Substring(6, 2), 16)
        }
        $computedRgbaDigest = 'sha256:' + [System.Convert]::ToHexString(
            $sha256.ComputeHash($rgbaBytes)).ToLowerInvariant()
        if ($computedRgbaDigest -cne $rgbaDigest) {
            throw "Accepted reference fixture visual '$id' rgbaDigest mismatch.`n  Committed: $rgbaDigest`n  Computed:  $computedRgbaDigest"
        }
    }

    $sortedVisuals = @($visuals | Sort-Object -Property visualId)
    $concatenatedDigests = ($sortedVisuals | ForEach-Object { $_.indexedDigest }) -join ''
    $computedAggregateDigest = 'sha256:' + [System.Convert]::ToHexString(
        $sha256.ComputeHash($utf8.GetBytes($concatenatedDigests))).ToLowerInvariant()
    if ($computedAggregateDigest -cne $fixture.aggregateDigest) {
        throw "Accepted reference fixture aggregate digest mismatch.`n  Committed: $($fixture.aggregateDigest)`n  Computed:  $computedAggregateDigest"
    }

    $sha256.Dispose()
}

$conformance = Join-Path $root 'src\Chronicle.Visuals.Conformance\Chronicle.Visuals.Conformance.csproj'
& $dotnet restore $conformance --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
& $dotnet build $conformance --configuration Release --no-restore -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
& $dotnet run --project $conformance --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Conformance failed.' }

Assert-AcceptedReferenceFixture

$cli = Join-Path $root 'src\Chronicle.VisualCompiler.Cli\Chronicle.VisualCompiler.Cli.csproj'
$catalogue = Join-Path $root 'catalogues\e45-palimpsest20.json'
$expectedHashesPath = Join-Path $root 'fixtures\palimpsest20\expected-hashes.json'
if (-not (Test-Path -LiteralPath $catalogue -PathType Leaf)) {
    throw "Missing E4.5 catalogue '$catalogue'."
}
if (-not (Test-Path -LiteralPath $expectedHashesPath -PathType Leaf)) {
    throw "Missing E4.5 expected hashes '$expectedHashesPath'."
}

$expectedHashes = Get-Content -Raw -LiteralPath $expectedHashesPath | ConvertFrom-Json
if ($expectedHashes.profile -cne 'Palimpsest20' -or
    [string]::IsNullOrWhiteSpace([string]$expectedHashes.palimpsestDigest) -or
    [string]::IsNullOrWhiteSpace([string]$expectedHashes.aggregateDigest)) {
    throw 'The committed E4.5 expected-hashes fixture is malformed.'
}
$expectedByPath = @{}
foreach ($entry in @($expectedHashes.files)) {
    $path = [string]$entry.path
    $digest = [string]$entry.digest
    if ([string]::IsNullOrWhiteSpace($path) -or
        [string]::IsNullOrWhiteSpace($digest) -or
        $expectedByPath.ContainsKey($path)) {
        throw 'The committed E4.5 expected-hashes fixture has an invalid or duplicate file entry.'
    }
    $expectedByPath[$path] = $digest
}
if ($expectedByPath.Count -ne $canonicalPackPaths.Count -or
    (Compare-Object $canonicalPackPaths @($expectedByPath.Keys | Sort-Object))) {
    throw 'The committed E4.5 expected-hashes fixture has an incomplete or unexpected canonical file set.'
}

$proofRoot = [System.IO.Path]::GetFullPath((Join-Path $root 'artifacts\e45'))
function Resolve-ProofOutputPath {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    if (-not $resolved.StartsWith(
            $proofRoot + [System.IO.Path]::DirectorySeparatorChar,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe proof output path '$resolved'."
    }
    return $resolved
}

function Remove-ProofDirectory {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = Resolve-ProofOutputPath $Path
    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    return $resolved
}

$outputA = Resolve-ProofOutputPath (Join-Path $proofRoot 'build-a')
$outputB = Resolve-ProofOutputPath (Join-Path $proofRoot 'build-b')
foreach ($output in @($outputA, $outputB)) {
    Remove-ProofDirectory $output | Out-Null
}

& $dotnet build $cli --configuration Release -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw 'CLI build failed.' }
$unsupportedProfileOutput = Resolve-ProofOutputPath (
    Join-Path $proofRoot 'unsupported-profile')
Remove-ProofDirectory $unsupportedProfileOutput | Out-Null
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --profile RichPack --catalogue $catalogue --output $unsupportedProfileOutput
if ($LASTEXITCODE -eq 0 -or (Test-Path -LiteralPath $unsupportedProfileOutput)) {
    throw 'CLI accepted an unsupported integration profile.'
}
$compileTimer = [System.Diagnostics.Stopwatch]::StartNew()
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --profile Palimpsest20 --catalogue $catalogue --output $outputA
$compileTimer.Stop()
if ($LASTEXITCODE -ne 0) { throw 'First CLI compile failed.' }
if ($compileTimer.Elapsed.TotalSeconds -ge 10) {
    throw "E4.5 compile exceeded ten seconds: $($compileTimer.Elapsed.TotalSeconds)."
}
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --profile Palimpsest20 --catalogue $catalogue --output $outputB
if ($LASTEXITCODE -ne 0) { throw 'Second CLI compile failed.' }

$filesA = @(Get-RelativeFiles $outputA)
$filesB = @(Get-RelativeFiles $outputB)
if ($filesA.Count -ne $filesB.Count -or (Compare-Object $filesA $filesB)) {
    throw 'E4.5 CLI builds emitted different complete output file sets.'
}
foreach ($relativePath in $filesA) {
    if (-not (Test-ByteEquality (Join-Path $outputA $relativePath) (Join-Path $outputB $relativePath))) {
        throw "E4.5 CLI builds differ byte-for-byte at '$relativePath'."
    }
}

$packA = Join-Path $outputA 'pack'
$packB = Join-Path $outputB 'pack'
Assert-PinnedPalimpsest20Pack $packA $expectedByPath $expectedHashes 'First E4.5 output pack'
Assert-PinnedPalimpsest20Pack $packB $expectedByPath $expectedHashes 'Second E4.5 output pack'
Assert-E45ReviewArtifacts $outputA 'First E4.5 output'
Assert-E45ReviewArtifacts $outputB 'Second E4.5 output'

$replacementBackup = Resolve-ProofOutputPath ($outputA + '.backup')
Remove-ProofDirectory $replacementBackup | Out-Null
New-Item -ItemType Directory -Path $replacementBackup | Out-Null
$backupSentinel = Join-Path $replacementBackup 'keep.txt'
Set-Content -LiteralPath $backupSentinel -Value 'keep' -NoNewline
$manifestBeforeFailedReplacement = Get-Sha256Digest (
    Join-Path $packA 'manifest.json')
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --profile Palimpsest20 --catalogue $catalogue --output $outputA
if ($LASTEXITCODE -eq 0 -or
    -not (Test-Path -LiteralPath $backupSentinel) -or
    (Get-Sha256Digest (Join-Path $packA 'manifest.json')) -cne
        $manifestBeforeFailedReplacement) {
    throw 'CLI failed to preserve the current artifact across a replacement failure.'
}
Remove-ProofDirectory $replacementBackup | Out-Null

$recoveryTarget = Resolve-ProofOutputPath (Join-Path $proofRoot 'recovery-target')
$recoveryBackup = Resolve-ProofOutputPath ($recoveryTarget + '.backup')
foreach ($path in @($recoveryTarget, $recoveryBackup)) {
    Remove-ProofDirectory $path | Out-Null
}
Copy-Item -LiteralPath $outputA -Destination $recoveryBackup -Recurse
$recoveryManifest = Get-Sha256Digest (
    Join-Path $recoveryBackup 'pack/manifest.json')
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --profile Palimpsest20 `
    --catalogue ($catalogue + '.missing') `
    --output $recoveryTarget
if ($LASTEXITCODE -eq 0 -or
    -not (Test-Path -LiteralPath $recoveryTarget) -or
    (Test-Path -LiteralPath $recoveryBackup) -or
    (Get-Sha256Digest (Join-Path $recoveryTarget 'pack/manifest.json')) -cne
        $recoveryManifest) {
    throw 'CLI failed to recover an owned backup before a compile failure.'
}
Remove-ProofDirectory $recoveryTarget | Out-Null

$unowned = Resolve-ProofOutputPath (Join-Path $proofRoot 'unowned')
Remove-ProofDirectory $unowned | Out-Null
New-Item -ItemType Directory -Path $unowned | Out-Null
$sentinel = Join-Path $unowned 'keep.txt'
Set-Content -LiteralPath $sentinel -Value 'keep' -NoNewline
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --profile Palimpsest20 --catalogue $catalogue --output $unowned
if ($LASTEXITCODE -eq 0 -or -not (Test-Path -LiteralPath $sentinel)) {
    throw 'CLI replaced an unowned output directory.'
}
Remove-ProofDirectory $unowned | Out-Null

$stagingTarget = Resolve-ProofOutputPath (Join-Path $proofRoot 'staging-target')
$unownedStaging = Resolve-ProofOutputPath ($stagingTarget + '.staging')
foreach ($path in @($stagingTarget, $unownedStaging)) {
    Remove-ProofDirectory $path | Out-Null
}
New-Item -ItemType Directory -Path $unownedStaging | Out-Null
$stagingSentinel = Join-Path $unownedStaging 'keep.txt'
Set-Content -LiteralPath $stagingSentinel -Value 'keep' -NoNewline
& $dotnet run --project $cli --configuration Release --no-build -- `
    build --profile Palimpsest20 --catalogue $catalogue --output $stagingTarget
if ($LASTEXITCODE -eq 0 -or -not (Test-Path -LiteralPath $stagingSentinel)) {
    throw 'CLI replaced an unowned staging directory.'
}
Remove-ProofDirectory $unownedStaging | Out-Null

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
$plan = Join-Path $root 'fixtures\preview-plans\e45-palimpsest20.json'
if (-not (Test-Path -LiteralPath $plan -PathType Leaf)) {
    throw "Missing E4.5 Godot preview plan '$plan'."
}
$previewSourceFiles = @(
    Get-ChildItem -LiteralPath $previewRoot -File -Recurse |
        Where-Object {
            $_.Extension -in '.cs', '.csproj', '.tscn', '.godot' -and
            $_.FullName -notlike '*\.godot\*'
        }
)
if ($previewSourceFiles.Count -eq 0) {
    throw 'Godot preview source files are missing.'
}
$forbiddenPreviewReferences = @(
    'Chronicle.VisualCompiler',
    'VisualCatalogue',
    'MotifPlacement',
    'catalogue'
)
$previewReferenceMatches = @(
    Select-String -LiteralPath $previewSourceFiles.FullName `
        -Pattern $forbiddenPreviewReferences -SimpleMatch -CaseSensitive:$false
)
if ($previewReferenceMatches.Count -ne 0) {
    $firstPreviewReference = $previewReferenceMatches[0]
    throw (
        'Godot preview must remain pack-only and cannot reference compiler, ' +
        "catalogue, or motif definitions: $($firstPreviewReference.Path):" +
        "$($firstPreviewReference.LineNumber).")
}
$captureA = Resolve-ProofOutputPath (Join-Path $proofRoot 'godot-a')
$captureB = Resolve-ProofOutputPath (Join-Path $proofRoot 'godot-b')
foreach ($capture in @($captureA, $captureB)) {
    Remove-ProofDirectory $capture | Out-Null
}
$previousAppData = $env:APPDATA
$previousLocalAppData = $env:LOCALAPPDATA
$godotAppData = Resolve-ProofOutputPath (
    Join-Path $proofRoot '.godot-appdata')
$godotLocalAppData = Resolve-ProofOutputPath (
    Join-Path $proofRoot '.godot-localappdata')
try {
foreach ($path in @($godotAppData, $godotLocalAppData)) {
    Remove-ProofDirectory $path | Out-Null
}
$env:APPDATA = $godotAppData
$env:LOCALAPPDATA = $godotLocalAppData
New-Item -ItemType Directory -Force -Path $godotAppData,$godotLocalAppData |
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
$godotPackA = $packA.Replace('\', '/')
$godotPackB = $packB.Replace('\', '/')
$godotPlan = $plan.Replace('\', '/')
$godotCaptureA = $captureA.Replace('\', '/')
$godotCaptureB = $captureB.Replace('\', '/')
function Invoke-BoundedGodot(
    [string[]]$Arguments,
    [string]$Description,
    [int]$TimeoutSeconds = 30
) {
    $process = Start-Process `
        -FilePath $godot `
        -ArgumentList $Arguments `
        -PassThru `
        -WindowStyle Hidden
    try {
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            throw "$Description exceeded $TimeoutSeconds seconds."
        }
        if ($process.ExitCode -ne 0) {
            throw "$Description failed with exit $($process.ExitCode)."
        }
    } finally {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            [void]$process.WaitForExit(5000)
        }
    }
}

$headlessArguments = @(
    '--headless',
    '--quit-after', '3',
    '--log-file', $interactiveLog,
    '--path', $godotPreviewRoot,
    '--',
    '--pack', $godotPackA,
    '--plan', $godotPlan
)
Invoke-BoundedGodot $headlessArguments 'Interactive preview headless launch'
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
    Invoke-BoundedGodot $arguments 'Godot viewport acceptance'
}
Invoke-GodotCapture $logA $godotPackA $godotCaptureA
Invoke-GodotCapture $logB $godotPackB $godotCaptureB

$captureFilesA = @(Get-RelativeFiles $captureA)
$captureFilesB = @(Get-RelativeFiles $captureB)
if ($captureFilesA.Count -ne $captureFilesB.Count -or
    (Compare-Object $captureFilesA $captureFilesB)) {
    throw 'Godot acceptances emitted different file sets.'
}
foreach ($relativePath in $captureFilesA) {
    if (-not (Test-ByteEquality (Join-Path $captureA $relativePath) (Join-Path $captureB $relativePath))) {
        throw "Godot acceptances differ byte-for-byte at '$relativePath'."
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
} finally {
    $env:APPDATA = $previousAppData
    $env:LOCALAPPDATA = $previousLocalAppData
    foreach ($path in @($godotAppData, $godotLocalAppData)) {
        Remove-ProofDirectory $path | Out-Null
    }
}

$aggregateDigest = (Get-Content -Raw -LiteralPath (Join-Path $packA 'hashes.json') |
    ConvertFrom-Json).aggregateDigest
Write-Host (
    "E4.5 compile milliseconds: {0}; aggregateDigest: {1}" -f
    [Math]::Round($compileTimer.Elapsed.TotalMilliseconds),
    $aggregateDigest)
