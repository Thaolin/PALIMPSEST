# Decision

Status: `Pending`

No adopt, narrow, defer, or reject decision is authorized before human visual
review. E4 implementation and automated evidence are complete.

## Evidence

- Final normalized catalogue:
  [`catalogues/e3.json`](../catalogues/e3.json)
- Canonical manifest and hashes:
  [`manifest.json`](../artifacts/e4/build-a/manifest.json),
  [`hashes.json`](../artifacts/e4/build-a/hashes.json)
- Validation and provenance:
  [`validation.json`](../artifacts/e4/build-a/validation.json),
  [`provenance.json`](../artifacts/e4/build-a/provenance.json)
- Native and enlarged sheets:
  [`native-16.png`](../artifacts/e4/build-a/review/native-16.png),
  [`nearest-16.png`](../artifacts/e4/build-a/review/nearest-16.png),
  [`native-20.png`](../artifacts/e4/build-a/review/native-20.png),
  [`nearest-20.png`](../artifacts/e4/build-a/review/nearest-20.png)
- Specialized sheets:
  [`adjacency-16.png`](../artifacts/e4/build-a/review/adjacency-16.png),
  [`shifted-overlap-16.png`](../artifacts/e4/build-a/review/shifted-overlap-16.png),
  [`variants-16.png`](../artifacts/e4/build-a/review/variants-16.png),
  [`palette-surface-16.png`](../artifacts/e4/build-a/review/palette-surface-16.png),
  [`palette-sky-16.png`](../artifacts/e4/build-a/review/palette-sky-16.png),
  [`manual-baseline-16.png`](../artifacts/e4/build-a/review/manual-baseline-16.png)
- Authoring-cost evidence:
  [`authoring-evidence.json`](../artifacts/e4/build-a/review/authoring-evidence.json)
- Godot viewport capture, CPU oracle, and metadata:
  [`capture.png`](../artifacts/e4/godot-a/capture.png),
  [`oracle.png`](../artifacts/e4/godot-a/oracle.png),
  [`capture.json`](../artifacts/e4/godot-a/capture.json)

## Review observations

- Stronger than baseline: the generated Bell has the clearest lineage and gains
  variants/palette portability without additional pixel masks; connected forms
  provide the strongest authoring leverage.
- Comparable: generated Incarnation and loose Stone remain readable and require
  no authored pixel rows, but their silhouettes are deliberately simple.
- Weaker: native glyphs are abstract; three material objects depend more on
  outline than internal material language; palette differences are conservative.
- Technical limitation: real viewport capture is off-screen OpenGL, while a
  separate dummy-renderer launch supplies the strict headless scene check.

The next decision owner should inspect native sheets and the interactive preview,
then record `Adopt`, `Narrow`, `Defer`, or `Reject` with rationale. Until then,
the status remains `Pending`.
