# Architecture

Dependencies point inward toward the compiled-pack contract:

```text
Chronicle.VisualCompiler.Cli -> Chronicle.VisualCompiler -> Chronicle.VisualPack
Chronicle.Visuals.Conformance -> Chronicle.VisualCompiler + Chronicle.VisualPack
Chronicle.VisualPreview.Godot -> Chronicle.VisualPack
```

- `Chronicle.VisualPack` owns immutable pack values, canonical codecs, stable
  handles, compatibility, and shared validation.
- `Chronicle.VisualCompiler` owns normalized catalogue data, deterministic
  integer raster rules, packing, diagnostics, and in-memory review artifacts.
- `Chronicle.VisualCompiler.Cli` owns filesystem I/O only.
- `Chronicle.Visuals.Conformance` proves behavior through public seams without a
  test-framework dependency.
- `Chronicle.VisualPreview.Godot` reads compiled packs and fixture plans only.

No compiler rule crosses into Godot. No module references Palimpsest or
`Chronicle.Core`.

## Pack invariants

- Pack v1 includes explicit immutable compatibility, palette, atlas, visual,
  motif, mapping, validation, hash, and provenance records.
- Loaded collections and indexed buffers are copied before exposure.
- Stable identifiers use lowercase namespaced ASCII and ordinal comparison;
  atlas locations are never identity.
- Canonical JSON has schema-fixed property order, ordinal set ordering, UTF-8
  without BOM, and one terminal LF.
- SHA-256 digests use versioned domain/length framing. The aggregate covers the
  normalized manifest, ordered palette records, and ordered indexed buffers;
  it excludes hashes, validation, provenance, review images, paths, timestamps,
  and environment data.
- Geometry digests exclude palette values and atlas placement.

## Deliberate algorithms

The compiler uses a fixed-width shelf packer after sorting by stable visual
identity. It trades packing density for predictable diffs and auditable
determinism. Review images use integer RGBA buffers and a deterministic
standard-library PNG writer; images remain noncanonical.

The preview uses one bounded custom-draw root, expands indexed buffers through a
selected palette, and draws fixed fixture plans at integer positions with
nearest filtering. It contains no catalogue parser, adjacency inference,
variant selection, or compiler reference.
