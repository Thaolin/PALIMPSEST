# Codemap

Current boundary: **E5.1 accepted in Palimpsest on 2026-07-21.**

| Area | Ownership | Stable seam | Verification |
| --- | --- | --- | --- |
| `src/Chronicle.VisualPack/Palimpsest20Pack.cs` | Candidate 20px pack and direct resolver | `Palimpsest20Pack.Resolve(string)` | Pack/compiler conformance |
| `src/Chronicle.VisualPack/Palimpsest20Codec.cs` | Four-file canonical codec and compatibility checks | `WriteCanonical` / `ReadCanonical` | Invalid-bundle fixtures |
| `src/Chronicle.VisualPack/PixelConnectivity.cs` | Shared four-neighbour occupancy | `IsFourConnected` | Legacy and E4.5 conformance |
| `src/Chronicle.VisualPack/MotifPlacement.cs` | Ordered clip/reject placement | `Resolve` | Motif vectors |
| `src/Chronicle.VisualCompiler/VisualCatalogue.cs` | Strict typed schema-v2 catalogue | `ParseJson` | Catalogue conformance |
| `src/Chronicle.VisualCompiler/VisualCompiler.cs` | Deterministic authoring raster compiler | Internal rich compile | Compiler conformance |
| `src/Chronicle.VisualCompiler/Palimpsest20Exporter.cs` | Exact PALIMPSEST vocabulary/profile projection | `CompilePalimpsest20` | 249-definition fixture |
| `src/Chronicle.VisualCompiler/DeterministicSelection.cs` | Authoring/conformance selection evidence; E5 design candidate only | Internal `SelectVariant` | Literal boundary vectors |
| `src/Chronicle.VisualCompiler/ReviewRenderer.cs` | Review layout, including noncanonical manual-baseline comparison | Internal review files | Visual inspection and deterministic output |
| `src/Chronicle.VisualCompiler/PngEncoder.cs` | Deterministic PNG serialization | Internal encoder | Compile-twice proof |
| `src/Chronicle.VisualCompiler.Cli` | Safe filesystem adapter | `build --profile Palimpsest20` | CLI acceptance |
| `src/Chronicle.VisualPreview.Godot` | Pack-only 20px presentation and canonical artifact oracle | `--pack` and `--plan` | Godot acceptance |
| `src/Chronicle.VisualWorkbench.Godot` | Authoring-only catalogue review, aspect comparison, biome board, and biome-brief Adapter | `--catalogue`; never a Palimpsest dependency | Interactive E5.1 UAT |
| `catalogues/e45-palimpsest20.json` | Canonical authoring source | Schema-v2 JSON | Compile-twice proof |
| `fixtures/palimpsest20` | Exact contract, invalid cases, pinned hashes | Committed fixtures | Conformance runner |
| `tools/verify.ps1` | End-to-end fail-fast proof | Successful exit | Clean-checkout entry point |
| `workbench.ps1` | One-command E5.1 authoring workbench launch | Interactive Godot process | Player authoring review |

The assembly-internal rich authoring pack, catalogue motif metadata, and
selection algorithm are not Palimpsest runtime seams.
