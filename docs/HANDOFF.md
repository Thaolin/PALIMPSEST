# Handoff

- Status: **E5 integration active in Palimpsest.**
- Active stage: authorized vocabulary refresh and canonical bundle repin
- Public artifact: `artifacts/e45/build-a/pack` after
  `tools/verify.ps1`
- Review artifact: `artifacts/e45/build-a/review`
- Catalogue: `catalogues/e45-palimpsest20.json`
- Contract fixture: `fixtures/palimpsest20/contract.json` (185 definitions)
- Hash fixture: `fixtures/palimpsest20/expected-hashes.json`
- Canonical aggregate:
  `sha256:245cb53df47d7f9866071d75359d272cbd53c56010e3d3f4921d12cf72eaf707`
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

The supplied 185-definition contract includes the required centered anchor and
the accepted Hearthstone, Riven Cairn, shattered Cairn, and River Ward danger
visuals.
Its existing conformance path emits deterministic exact-ID reports for missing,
unexpected, family, layer, mask, variant, anchor, overview, and palette-role
mismatches without reading the Palimpsest repository.

Review output now adds `manual-baseline-20.png` and
`authoring-evidence.json`. Its 64 accepted/candidate rows cover the six
individual specimens plus all pinned connected-water, cloud, grove, ridge, and
crossing captures. Comparison construction resolves the exact family, mask,
and local variant and rejects missing or ambiguous candidates; the evidence
records all 64 mappings. Native and 4× nearest sheets plus adjacency,
shifted-overlap, motif, layer, and variant sheets cover broader family lineage.
Baseline definitions remain outside the four canonical files and the 185
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
  `src/Chronicle.VisualCompiler/DeterministicSelection.cs`.
- Public-seam conformance and supplied fixtures:
  `src/Chronicle.Visuals.Conformance/Palimpsest20BundleConformance.cs`,
  `src/Chronicle.Visuals.Conformance/Palimpsest20CompilerConformance.cs`,
  `fixtures/palimpsest20/invalid/cases.json`,
  `fixtures/palimpsest20/contract.json`.
- One-command evidence assertions: `tools/verify.ps1`.

## Remaining Palimpsest blockers

- Palimpsest's strict reader, shared player/Inspector loader, packaging checks,
  and player visual UAT must pass before E5 closes.
- Motif placement and typed World/Stratum cosmetic selection remain outside
  this narrowed E5 runtime bundle.

The next forbidden P-GEN work is runtime catalogue parsing, compiler linkage
from Palimpsest, compiled motif-record export, live swapping, or Goal 6A visual
vocabulary without a later authorized contract.

The Palimpsest worktree contains unrelated local modifications and untracked
files. The preceding read-only Palimpsest audit did not edit, stage, commit,
publish, push, or open a pull request in either repository. This current P-GEN
E4.5 hardening task intentionally edits the listed P-GEN files while Palimpsest
remains read-only.
