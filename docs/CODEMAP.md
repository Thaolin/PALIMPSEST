# Codemap

| Area | Ownership | Public seam | Verification |
| --- | --- | --- | --- |
| `src/Chronicle.VisualPack` | Pack model, codecs, validation | Pack write/load/resolve | Conformance runner |
| `src/Chronicle.VisualCompiler` | Catalogue compile and review files | `Compile(...)` | Conformance runner |
| `src/Chronicle.VisualCompiler.Cli` | File adapter | `chronicle-visuals build` | CLI acceptance |
| `src/Chronicle.Visuals.Conformance` | Fixtures and proof orchestration | Process exit and report | `tools/verify.ps1` |
| `src/Chronicle.VisualPreview.Godot` | Pack-only preview | Headless acceptance | `tools/verify.ps1` |
| `catalogues` | Compiler-owned source definitions | Catalogue JSON | Compile-twice proof |
| `fixtures` | Independent known-good/invalid inputs | Pack directory shape | Conformance runner |
| `tools` | Fail-fast local automation | `verify.ps1` | Successful exit |

This map records stable ownership and entry points, not implementation progress.
