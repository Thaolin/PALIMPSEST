# Development

## Toolchain

- .NET SDK 8.0.423, pinned by `global.json`
- Godot 4.7.1 stable .NET/Mono
- PowerShell 7 or Windows PowerShell 5.1

Set `CHRONICLE_DOTNET` and `CHRONICLE_GODOT` to exact executable paths when the
tools are not on `PATH`. The proof script also recognizes the repository-local
paths `.tools/dotnet/dotnet.exe` and
`.tools/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe`.

## Commands

```powershell
./tools/verify.ps1
```

The command performs the full E0–E4 fail-fast proof. On Windows it briefly runs
two hidden off-screen OpenGL windows for real viewport capture; the separate
headless launch still proves scene, script, resource, and draw startup.

### E0 pack contract

```powershell
$env:CHRONICLE_DOTNET = 'C:\path\to\dotnet.exe'
./tools/verify.ps1
```

The current proof restores, builds Release, and runs the dependency-free
public-seam conformance executable, then compiles `catalogues/e3.json` twice,
compares every emitted file, and exercises compiler output ownership guards.

```powershell
dotnet run --project src/Chronicle.VisualCompiler.Cli -c Release -- `
  build --catalogue catalogues/e3.json --output artifacts/manual-e3
```

### Interactive Godot preview

Run the proof once so `artifacts/e4/build-a` exists, then:

```powershell
$root = (Resolve-Path '.').Path
$godot = 'C:\DEV\PALIMPSEST\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --path "$root\src\Chronicle.VisualPreview.Godot" -- `
  --pack "$root\artifacts\e4\build-a" `
  --plan "$root\fixtures\preview-plans\e4-acceptance.json"
```

Controls:

- `P`: palette
- `F`: family
- `V`: variant
- `M`: adjacency mask
- `L`: layer
- `N`: specimen
- `S`: inspection scale (1×, 4×, 8×)

The metadata panel shows stable ID, family, size, variant, mask, layer, atlas
rectangle, anchor, geometry/atlas/pack hashes, and validation count. Close the
window normally; the preview never edits catalogue or pack files.
