# Architecture

Dependencies point inward:

```text
Chronicle.VisualCompiler.Cli -> Chronicle.VisualCompiler -> Chronicle.VisualPack
Chronicle.Visuals.Conformance -> Chronicle.VisualCompiler + Chronicle.VisualPack
Chronicle.VisualPreview.Godot -> Chronicle.VisualPack
```

`Chronicle.VisualPack` exposes the narrow `Palimpsest20Pack` and
`Palimpsest20Codec` integration contract. The earlier richer
`CompiledVisualPack` aggregate and its codec/validator are assembly-internal
authoring machinery, visible only to the compiler and conformance assembly.
`Chronicle.VisualCompiler` owns catalogue parsing, reusable raster primitives,
typed material treatments, deterministic selection, packing, diagnostics, and
noncanonical review sheets. The CLI owns filesystem replacement and ownership
guards. Godot reads only a completed canonical pack.

No P-GEN project references PALIMPSEST production assemblies or
`Chronicle.Core`. PALIMPSEST remains the authoritative consumer contract.

## Palimpsest20 boundary

The export profile is intentionally singular and strict:

- native cell size is exactly 20px;
- format, composer-contract, and visual-style versions are exactly 1;
- one palette and one 20px-aligned indexed atlas are emitted;
- every mask and variant has a globally unique concrete ID resolved by
  `Resolve(string)`;
- each definition contains its PALIMPSEST layer, centered anchor, palette-role
  indexes, mask where applicable, and overview palette index;
- the canonical file set is exactly `manifest.json`, `hashes.json`,
  `validation.json`, and `atlases/palimpsest20.indices`;
- JSON is strict, duplicate/unmapped members are rejected, UTF-8 has no BOM and
  one terminal LF, and all input collections are copied before exposure.

The canonical runtime manifest contains no manual comparison baseline,
provenance report, review PNG, path timestamp, or environment data.

## Determinism and grammar

Catalogue schema v2 uses closed enums for generation, identity, material
treatment, transition ownership, and clipping. Authored role masks and reusable
pixel primitives replace ID-specific raster switches. Water, cloud, ridge/wall,
path/border, crossing, and transition treatments have separate branches.

Motifs retain seeds, variant counts, ordered variant-tagged marks, and typed
clip/reject behavior. Coordinate selection is framed SHA-256 over stable
invariant text and accepts signed 64-bit coordinates. `SelectVariant` is a pure
production utility with pinned conformance vectors; the compiler cannot invoke
coordinate selection while emitting a pack because Chronicle semantic
coordinates do not exist at authoring time. The preview enumerates authored
variants for review. Schema v2 requires nonempty, nonblank, unique semantic
occupancy tags and rejects a combined motif review canvas above 16,777,216
pixels; the same allocation ceiling protects all review canvases. Calling
selection from PALIMPSEST runtime composition is an E5 integration decision,
not hidden E4.5 wiring. Four-neighbour occupancy uses one shared utility. PNG
encoding is separate from review-sheet layout.

Canonical file and pack hashes are committed in
`fixtures/palimpsest20/expected-hashes.json`; repeat equality is an additional,
not substitute, proof.

## Presentation boundary

The Godot adapter parses the canonical four-file bundle, calls only direct
`Resolve(string)`, expands the one indexed atlas, and draws at integer scale
with nearest filtering. It has no compiler reference and writes no pack data.

Runtime loading or swapping inside PALIMPSEST is not present. That missing
consumer-side seam is the precise E5 boundary.
