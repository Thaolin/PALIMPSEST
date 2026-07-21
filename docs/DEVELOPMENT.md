# Development

## Toolchain

- .NET SDK 8.0.423, pinned by `global.json`
- Godot 4.7.1 stable .NET/Mono
- PowerShell 7 or Windows PowerShell 5.1

Set `CHRONICLE_DOTNET` and `CHRONICLE_GODOT` when those exact executables are not
discoverable. The verifier also checks the repository-local tools and the
read-only PALIMPSEST tool cache.

## Full proof

```powershell
./tools/verify.ps1
```

This is the exact E4.5 acceptance command. It restores without requiring the
network, builds with zero warnings, runs conformance, performs two independent
CLI builds, verifies pinned hashes and all emitted bytes, asserts the E4.5
review bundle, and exercises the pack-only Godot proof. It also scans the
preview source boundary for compiler, catalogue, and motif-placement
references. On Windows, real viewport capture briefly uses hidden off-screen
OpenGL windows; a separate headless launch proves scene startup.

## Compile manually

```powershell
dotnet run --project src/Chronicle.VisualCompiler.Cli -c Release -- `
  build --profile Palimpsest20 `
  --catalogue catalogues/e45-palimpsest20.json `
  --output artifacts/manual-pal20
```

`artifacts/manual-pal20/pack` contains exactly four canonical files.
`artifacts/manual-pal20/review` contains noncanonical 20px review evidence.
The CLI refuses to replace directories without its ownership marker.

## Negative bundle and contract maintenance

The public reader proof is
`src/Chronicle.Visuals.Conformance/Palimpsest20BundleConformance.cs`; its 50
version-controlled cases are inventoried in
`fixtures/palimpsest20/invalid/cases.json`. They exercise exact `PAL20-*`
diagnostics for file sets, JSON shape, canonical atlas path and serialization,
hashes, compatibility, dimensions and buffers, palettes, identifiers,
families, variants, layers, rectangles, anchors, adjacency, overview indexes,
palette-role indexes, overlap, and empty occupancy through
`Palimpsest20Codec.ReadCanonical`.

The older Pack-v1 envelope fields are not part of the narrowed four-file
format. `PackFile` constructor-only path grammar is also not a reader fixture
because invalid `PackFile` values cannot reach `ReadCanonical`; neither
category should be mimicked with invented Palimpsest20 fields.

The frozen pre-Goal-4B mapping contract is
`fixtures/palimpsest20/contract.json`. Update that supplied,
version-controlled fixture only when an authorized adoption review freezes a
new accepted vocabulary; never scan the Palimpsest repository. The existing
conformance path reports the exact ID with stable diagnostics for missing and
unexpected IDs and family, layer, mask, variant, centered-anchor, overview, or
palette-role mismatches.

## E4.5 visual review

The full proof emits `review/manual-baseline-20.png` and
`review/authoring-evidence.json` beside, never inside, the canonical pack.
Its 64 rows compare every pinned accepted-reference capture with the exact
candidate family, adjacency mask, and local variant: six individual specimens,
all water and cloud masks, and the selected grove, ridge, and crossing
variants. `authoring-evidence.json` records the complete deterministic mapping.
`native-20.png` and `nearest-20.png` provide native and 4× nearest-neighbour
evidence; adjacency, shifted-overlap, motif, layer, and variant sheets provide
additional connected-terrain and feature context.

These artifacts are deterministic review inputs, not visual approval.
Silhouette recognition, family lineage, contrast, and preference remain
subjective and require Palimpsest player UAT.

## Interactive preview

Run the full proof once so `artifacts/e45/build-a/pack` exists, then:

```powershell
$root = (Resolve-Path '.').Path
$godot = 'C:\DEV\PALIMPSEST\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --path "$root\src\Chronicle.VisualPreview.Godot" -- `
  --pack "$root\artifacts\e45\build-a\pack" `
  --plan "$root\fixtures\preview-plans\e45-palimpsest20.json"
```

Controls:

- `F`: family
- `V`: variant
- `M`: concrete mask
- `L`: layer
- `N`: next definition
- `S`: integer inspection scale

The preview calls only exact `Resolve(string)` IDs and never edits the pack.

## E5.1 authoring workbench

```powershell
.\workbench.ps1
```

The workbench is a separate authoring-only Godot project. It reads the source
catalogue and invokes the compiler in memory; it does not weaken the pack-only
Preview. Use its three views to inspect one silhouette, compare family
variants/masks, or review a mixed surface-biome board. The aspect selector
compares native `20 × 20` with a stretched `20 × 30` evidence view based on
Qud's `16 × 24` cell; it does not change exported pixels.

The Biome Board's form writes only after **Save biome brief** is pressed. It
creates a new `catalogues/briefs/<id>.visual-brief.json` and refuses to
overwrite an existing brief. The brief records visual intent for a later
authored P-GEN pass; it is not compiler input and cannot invent Chronicle
semantics.

## Boundaries

Production code is C# only. Do not add a P-GEN-to-PALIMPSEST runtime adapter, a
PALIMPSEST filesystem reader, live swapping, semantic gameplay authoring, or
changes under `C:\DEV\PALIMPSEST`. Those are outside E4.5.
