# Decision

Status: `E4.5 authoring boundary accepted`

P-GEN is narrowed to an engine-independent C# authoring compiler. Its only
public compiled-pack integration contract is `Palimpsest20`; the richer historic
pack aggregate is assembly-internal. The accepted native scale is 20px.

This decision does not adopt generated art into PALIMPSEST and does not
authorize runtime loading, translation adapters, or live pack swapping.

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
- Godot viewport, CPU oracle, and metadata:
  [`capture.png`](../artifacts/e45/godot-a/capture.png),
  [`oracle.png`](../artifacts/e45/godot-a/oracle.png),
  [`capture.json`](../artifacts/e45/godot-a/capture.json)

The next permissible decision is a separately scoped E5 proposal in the
PALIMPSEST repository. Until authorized, the exported directory remains an
authoring artifact and preview input only.
