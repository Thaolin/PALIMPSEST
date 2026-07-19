# Roadmap

| Stage | Scope | Acceptance |
| --- | --- | --- |
| E0 | Compiled-pack contract | Reference pack round-trips; invalid categories fail; two canonical writes match |
| E1 | Small deterministic compiler | 16/20 targets, palettes, variants, packing, review sheets, local edits |
| E2 | Connected forms and motifs | Cardinal masks, continuity, connected families, grove/ridge motifs |
| E3 | Palimpsest-shaped specimen breadth | Required specimens, seeds, family lineage, manual baselines |
| E4 | Godot 4.7.1 .NET preview | Pack-only native preview, deterministic captures, headless acceptance |
| E5 | Palimpsest integration | Forbidden until separately authorized |

Every implemented stage stops for Handoff reconciliation before work on the next
stage begins.

## Reconciled gates

- E0: `tools/verify.ps1` passed with zero warnings and aggregate
  `sha256:42ec768423d3318b5c7c6ccea0b02c77ea2b429ec8cad569823db6730b12179a`;
  independent E0 verification and review passed.
- E1: the same command passed two isolated byte-identical CLI builds with
  aggregate
  `sha256:261d0cf0d21b094347ba817faf8ecd09aadffe5a6480be12d612dcc757c22f26`;
  independent E1 review passed.
- E2: the same command passed complete connected-family, transition fallback,
  motif, topology, continuity, per-size/per-variant coverage, and specialized
  review-evidence conformance. Two isolated CLI builds matched at
  `sha256:3ba831eb7f7ee468e8ac85cce59b68674af7978a51d93afbd087e7a36b116347`;
  independent E2 review passed.
- E3: the same command passed the bounded Palimpsest-shaped vocabulary,
  two-palette geometry stability, controlled-variant lineage, independent
  manual-baseline comparison, and authoring-cost provenance. Two isolated CLI
  builds matched at
  `sha256:1fff3cb03aad5bae0013eef507ec5fb7346efe74b600cf771f9cc311fddb0987`;
  independent E3 review passed.
- E4: the same command passed the pack-only Godot dependency audit, headless
  scene launch, native/integer-scale fixture plan, real viewport/CPU-oracle
  equality, reproducible capture metadata, clean logs, bounded load/compile, and
  background-process shutdown. Raw capture digest:
  `sha256:c3bfabe9ca9e40f64e05b739fa7ec2c0784142150831c238c62ae769bbe2a7a6`;
  independent E4 review passed. Work is stopped before E5.
