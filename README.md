# P-GEN

P-GEN is a deterministic, offline C# authoring compiler for a candidate
PALIMPSEST 20px visual contract. It compiles a typed catalogue into one canonical
`Palimpsest20` pack: one indexed atlas, one palette, concrete string-resolvable
definitions, overview indexes, compatibility metadata, and pinned hashes.

Status: **E5 integration active in Palimpsest.** The authorized vocabulary
baseline includes the accepted Home and first-conflict visuals.

Godot 4.7.1 .NET is presentation and verification only. It reads the exported
pack; it does not contain compiler rules, motif definitions, variant selection,
adjacency fallback, or catalogue parsing.

## Verify

```powershell
./tools/verify.ps1
```

The proof builds with zero warnings, runs public-contract and invalid-bundle
conformance, checks the frozen required-ID fixture, compiles twice, checks the
committed expected hashes, emits native/manual review evidence, and captures the
same exported pack twice through Godot.

For a manual build:

```powershell
dotnet run --project src/Chronicle.VisualCompiler.Cli -c Release -- `
  build --profile Palimpsest20 `
  --catalogue catalogues/e45-palimpsest20.json `
  --output artifacts/manual-pal20
```

The canonical four-file artifact is under `artifacts/manual-pal20/pack`;
baseline definitions and review sheets remain separate under
`artifacts/manual-pal20/review`.

E4.5 proves reproducible technical authoring evidence. Human visual review and
Palimpsest player UAT remain pending; runtime loading, pack swapping, and any
adoption decision are E5 work requiring separate authorization.
