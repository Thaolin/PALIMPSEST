# Codemap

| Area | Ownership | Stable seam | Verification |
| --- | --- | --- | --- |
| `src/Chronicle.VisualPack/Palimpsest20Pack.cs` | Accepted 20px pack and direct resolver | `Palimpsest20Pack.Resolve(string)` | Pack/compiler conformance |
| `src/Chronicle.VisualPack/Palimpsest20Codec.cs` | Four-file canonical codec and compatibility checks | `WriteCanonical` / `ReadCanonical` | Invalid-bundle fixtures |
| `src/Chronicle.VisualPack/PixelConnectivity.cs` | Shared four-neighbour occupancy | `IsFourConnected` | Legacy and E4.5 conformance |
| `src/Chronicle.VisualPack/MotifPlacement.cs` | Ordered clip/reject placement | `Resolve` | Motif vectors |
| `src/Chronicle.VisualCompiler/VisualCatalogue.cs` | Strict typed schema-v2 catalogue | `ParseJson` | Catalogue conformance |
| `src/Chronicle.VisualCompiler/VisualCompiler.cs` | Deterministic authoring raster compiler | Internal rich compile | Compiler conformance |
| `src/Chronicle.VisualCompiler/Palimpsest20Exporter.cs` | Exact PALIMPSEST vocabulary/profile projection | `CompilePalimpsest20` | 181-definition fixture |
| `src/Chronicle.VisualCompiler/DeterministicSelection.cs` | Stable coordinate selection | `SelectVariant` | Literal boundary vectors |
| `src/Chronicle.VisualCompiler/ReviewRenderer.cs` | Review layout | Internal review files | Visual inspection |
| `src/Chronicle.VisualCompiler/PngEncoder.cs` | Deterministic PNG serialization | Internal encoder | Compile-twice proof |
| `src/Chronicle.VisualCompiler.Cli` | Safe filesystem adapter | `build --profile Palimpsest20` | CLI acceptance |
| `src/Chronicle.VisualPreview.Godot` | Pack-only 20px presentation | `--pack` and `--plan` | Godot acceptance |
| `catalogues/e45-palimpsest20.json` | Canonical authoring source | Schema-v2 JSON | Compile-twice proof |
| `fixtures/palimpsest20` | Exact contract, invalid cases, pinned hashes | Committed fixtures | Conformance runner |
| `tools/verify.ps1` | End-to-end fail-fast proof | Successful exit | Clean-checkout entry point |

The assembly-internal rich authoring pack is not an integration seam.
