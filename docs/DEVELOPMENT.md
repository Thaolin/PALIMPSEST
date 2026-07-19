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
CLI builds, verifies pinned hashes and all emitted bytes, and exercises the
pack-only Godot proof. On Windows, real viewport capture briefly uses hidden
off-screen OpenGL windows; a separate headless launch proves scene startup.

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

## Boundaries

Production code is C# only. Do not add a P-GEN-to-PALIMPSEST runtime adapter, a
PALIMPSEST filesystem reader, live swapping, semantic gameplay authoring, or
changes under `C:\DEV\PALIMPSEST`. Those are outside E4.5.
