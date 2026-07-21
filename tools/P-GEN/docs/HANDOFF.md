# Handoff

- Status: **E5.1 accepted and closed in Palimpsest on 2026-07-21.**
- Active stage: none; later work requires a new Palimpsest contract
- Public artifact: `artifacts/e45/build-a/pack` after
  `tools/verify.ps1`
- Review artifact: `artifacts/e45/build-a/review`
- Catalogue: `catalogues/e45-palimpsest20.json`
- Contract fixture: `fixtures/palimpsest20/contract.json` (249 definitions)
- Hash fixture: `fixtures/palimpsest20/expected-hashes.json`
- Canonical aggregate:
  `sha256:85418f3025f2944d2f58a0a981febb00903bf67edcc23cb84054b3fd9f91eae0`
- Godot viewport/CPU-oracle PNG:
  `sha256:8b987ebe3e65f0e0421d648e4945972f2a253f797c40d35ed7765f5addc92b20`
- Verification entry point: `tools/verify.ps1`

The proof restores offline, builds all C# projects with zero warnings, runs
strict bundle/compiler/motif conformance, compiles two complete outputs and
compares every byte, checks committed hashes, asserts review-only baseline
separation, validates output ownership guards, proves the Godot project remains
pack-only, launches its scene, captures both exports through Godot, compares
viewport output to the CPU oracle, checks logs, and confirms bounded process
shutdown. Its isolated Godot app-data directories are removed in a `finally`
block.

The 2026-07-20 hardening proof completed in 32.1 seconds with zero warnings and
errors and measured the first CLI compile at 1.196 seconds. Both canonical
builds retained aggregate
`sha256:f41d1e4e4f76b5e6e57921cda35050582368486e87e932d5f1273ff4c2be9bd8`.
The unchanged viewport plan remains byte-identical to its CPU oracle; its
recorded captured pixel digest is
`sha256:aa544d2e426f382cb77f43e3b246fa01212ce329a7bb02f37d08a8b43b52f5e4`.

The invalid-bundle inventory now contains 50 public-reader cases: the original
16 plus 34 focused cases for the remaining applicable JSON, format, hash,
compatibility, palette, atlas, definition, metadata, occupancy, and canonical
serialization rules. Legacy Pack-v1 envelope fields and constructor-only
`PackFile` path grammar are recorded as not applicable to this narrowed reader.

The supplied 249-definition contract includes the required centered anchor and
the accepted Hearthstone, Riven Cairn, shattered Cairn, and River Ward danger
visuals.

The first integrated candidate failed player visual UAT on 2026-07-21. Generic
center-and-arm rastering made water, grove, ridge, and related materials read
as one wall lattice; the 6–11-pixel actor and landmark masks also read too
small inside native 20-pixel cells. Palimpsest's authorized
`P-GEN-E5-1-VISUAL-AUTHORING-SPIKE.md` now governs the correction.
Its existing conformance path emits deterministic exact-ID reports for missing,
unexpected, family, layer, mask, variant, anchor, overview, and palette-role
mismatches without reading the Palimpsest repository.

E5.1 now replaces generic visual connectivity with material treatments: water
uses shore boundaries and ripples, groves use tree silhouettes, ridges use
mountain silhouettes, ridge-water intersections use nonconnecting rocky fords,
and cloud cells form continuous banks with scalloped boundaries. Only genuine
walls and paths retain generic connector continuity. Principal actors and
landmarks now occupy 10–18 rows, and grove/ridge families expose four authored
variants. The separate `Chronicle.VisualWorkbench.Godot` project provides an
Asset Lab, Material Matrix, Biome Board, native/tall comparison, live
recompile, filtering, and explicit non-overwriting biome-brief export.

The complete verifier passed on 2026-07-21 with zero compiler warnings or
errors. It produced 249 definitions and canonical aggregate
`sha256:85418f3025f2944d2f58a0a981febb00903bf67edcc23cb84054b3fd9f91eae0`;
the final wrapper marker was `PGEN_E51_VERIFY_EXIT=0`. The player accepted the
corrected assets and workbench with the note that the actor looks terrible and
can be iterated later. Actor-art quality is explicit non-blocking debt.

Review output now adds `manual-baseline-20.png` and
`authoring-evidence.json`. Its 64 accepted/candidate rows cover the six
individual specimens plus all pinned connected-water, cloud, grove, ridge, and
crossing captures. Comparison construction resolves the exact family, mask,
and local variant and rejects missing or ambiguous candidates; the evidence
records all 64 mappings. Native and 4× nearest sheets plus adjacency,
shifted-overlap, motif, layer, and variant sheets cover broader family lineage.
Baseline definitions remain outside the four canonical files and the 249
exported definitions. These images are evidence for human review, not
Palimpsest visual UAT.

Catalogue motif footprint, anchor, ordered-mark, occupancy, variant, and
clipping data remains authoring-only; the export is flat. E5 must choose
composer-owned placement or immutable compiled-pack motif records. The
selection algorithm is now internal authoring/conformance evidence with pinned
vectors, not a runtime API; P-GEN makes no decision about Palimpsest's eventual
typed selector.

## Changed files and proof

- Status and ownership documentation: `README.md`,
  `docs/ENGINE-SPEC.md`, `docs/ARCHITECTURE.md`, `docs/CODEMAP.md`,
  `docs/DECISION.md`, `docs/DEVELOPMENT.md`, `docs/HANDOFF.md`,
  `docs/ROADMAP.md`.
- Review-only inputs and selection boundary:
  `catalogues/e45-palimpsest20.json`,
  `docs/reference/e5-1-sprite-language-concept.png`, and
  `src/Chronicle.VisualCompiler/VisualCompiler.cs`.
- Public-seam conformance and supplied fixtures:
  `src/Chronicle.Visuals.Conformance/Palimpsest20CompilerConformance.cs`,
  `fixtures/palimpsest20/contract.json`, and
  `fixtures/palimpsest20/expected-hashes.json`.
- Authoring Adapter: `src/Chronicle.VisualWorkbench.Godot` and
  `workbench.ps1`.
- One-command evidence assertions: `tools/verify.ps1`, which now builds and
  headlessly launches both Godot projects while preserving the pack-only
  Preview boundary.

## Accepted Palimpsest result

- Reader, shared loader, packaging, deterministic composition, retained saves,
  player/Inspector automation, corrected runtime world, and authoring workbench
  are accepted. A later authorized asset pass may replace the actor art without
  reopening this gate.

The next forbidden P-GEN work is runtime catalogue parsing, compiler linkage
from Palimpsest, compiled motif-record export, live swapping, camera-coordinate
changes, semantic biome generation, or Goal 6A visual vocabulary without a
later authorized contract.

P-GEN is now co-located at `tools/P-GEN` with its prior Git history preserved.
The repository-root verifier runs this verifier before the Palimpsest runtime
gate. E5 and E5.1 are accepted; later work requires a new authorized contract.
