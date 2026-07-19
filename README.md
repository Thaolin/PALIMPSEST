# P-GEN

P-GEN is a deterministic, offline C# authoring compiler for PALIMPSEST's
accepted 20px visual contract. It compiles a typed catalogue into one canonical
`Palimpsest20` pack: one indexed atlas, one palette, concrete string-resolvable
definitions, overview indexes, compatibility metadata, and pinned hashes.

Godot 4.7.1 .NET is presentation and verification only. It reads the exported
pack; it does not contain compiler rules, variant selection, adjacency fallback,
or catalogue parsing.

## Verify

```powershell
./tools/verify.ps1
```

The proof builds with zero warnings, runs public-contract conformance, compiles
twice, checks the committed expected hashes, and captures the same exported pack
twice through Godot.

For a manual build:

```powershell
dotnet run --project src/Chronicle.VisualCompiler.Cli -c Release -- `
  build --profile Palimpsest20 `
  --catalogue catalogues/e45-palimpsest20.json `
  --output artifacts/manual-pal20
```

The canonical four-file artifact is under `artifacts/manual-pal20/pack`; review
sheets are separate under `artifacts/manual-pal20/review`.

E4.5 ends at authoring and preview. PALIMPSEST runtime loading, pack swapping,
and production integration are E5 and require separate authorization.
