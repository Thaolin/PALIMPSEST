# Decision

Status: **Superseded for adoption status by Palimpsest ADR 0004; retained as
the E4.5 technical decision record.**

P-GEN is narrowed to an engine-independent C# authoring compiler. Its only
public compiled-pack candidate is `Palimpsest20`; the richer historic pack
aggregate is assembly-internal. The technically reproduced native scale is
20px.

This record accepts the reproduced technical evidence inside P-GEN only. It
does not record human visual acceptance, adopt generated art into Palimpsest,
or authorize runtime loading, translation adapters, live pack swapping, or E5.

## Evidence

- Source catalogue:
  [`e45-palimpsest20.json`](../catalogues/e45-palimpsest20.json)
- Exact required vocabulary:
  [`contract.json`](../fixtures/palimpsest20/contract.json)
- Pinned known-good hashes:
  [`expected-hashes.json`](../fixtures/palimpsest20/expected-hashes.json)
- Canonical manifest and hashes after verification:
  [`manifest.json`](../artifacts/e45/build-a/pack/manifest.json),
  [`hashes.json`](../artifacts/e45/build-a/pack/hashes.json)
- Compatibility metadata:
  [`validation.json`](../artifacts/e45/build-a/pack/validation.json)
- Native and enlarged sheets:
  [`native-20.png`](../artifacts/e45/build-a/review/native-20.png),
  [`nearest-20.png`](../artifacts/e45/build-a/review/nearest-20.png)
- Specialized review:
  [`adjacency-20.png`](../artifacts/e45/build-a/review/adjacency-20.png),
  [`shifted-overlap-20.png`](../artifacts/e45/build-a/review/shifted-overlap-20.png),
  [`motifs-20.png`](../artifacts/e45/build-a/review/motifs-20.png),
  [`variants-20.png`](../artifacts/e45/build-a/review/variants-20.png)
- E4.5 manual comparison and authoring evidence:
  [`manual-baseline-20.png`](../artifacts/e45/build-a/review/manual-baseline-20.png),
  [`authoring-evidence.json`](../artifacts/e45/build-a/review/authoring-evidence.json)
- Godot viewport, CPU oracle, and metadata:
  [`capture.png`](../artifacts/e45/godot-a/capture.png),
  [`oracle.png`](../artifacts/e45/godot-a/oracle.png),
  [`capture.json`](../artifacts/e45/godot-a/capture.json)

The canonical export contains flat concrete definitions. Motif footprint,
anchor, ordered-mark, occupancy, variant, and clipping metadata remains
authoring-only. E5 must decide whether placement remains composer-owned or
becomes immutable compiled-pack data. The internal deterministic-selection
vectors are likewise evidence or an E5 design candidate; P-GEN makes no
runtime-selector decision.

The next permissible decision is a separately scoped E5 proposal in the
Palimpsest repository after explicit authorization and pending visual UAT.
Until then, the exported directory remains an authoring artifact and preview
input only.
