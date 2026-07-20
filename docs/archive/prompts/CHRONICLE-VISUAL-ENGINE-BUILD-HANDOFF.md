# Handoff Prompt — Build the Chronicle Visual Engine

Use this prompt in a separate Codex task whose workspace is the dedicated
engine repository. Attach or link the governing specification at
`C:\DEV\PALIMPSEST\docs\PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md`.

Do not run this build inside the Palimpsest repository.

---

$gpt-hyper $ponytail $cavecrew $tdd

/goal Build the Chronicle Visual Engine from scratch through specification
Stages E0–E4, with deterministic conformance proof and a Godot 4 .NET preview,
then stop for review before any Palimpsest integration.

## Workspace and authority

You are building a dedicated authoring engine in its own repository or
workspace.

The governing source specification is:

`C:\DEV\PALIMPSEST\docs\PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md`

Read that file completely before planning or changing code. Treat it as
read-only source authority. If the active workspace is
`C:\DEV\PALIMPSEST`, stop and ask the user for the separate engine workspace;
do not create the engine inside Palimpsest.

This goal authorizes:

- Stage E0 — compiled-pack contract and conformance;
- Stage E1 — small deterministic compiler;
- Stage E2 — connected forms and motifs;
- Stage E3 — Palimpsest-shaped vocabulary breadth;
- Stage E4 — Godot preview adapter;
- documentation, automation, fixtures, validation, and review artifacts
  required to prove those stages.

This goal does not authorize:

- Stage E5 Palimpsest integration;
- edits anywhere under `C:\DEV\PALIMPSEST`;
- a runtime compiler dependency;
- changes to `Chronicle.Core`, Chronicle saves, World Grammar, or gameplay;
- a package publish, external deployment, commit, push, or pull request unless
  separately requested.

## Product question

Build enough real evidence to answer:

> Can a compact deterministic C# pixel compiler produce a coherent,
> Palimpsest-specific visual vocabulary with more authoring leverage than
> equivalent manual sprite work, while emitting a compiled pack that
> Palimpsest can consume without knowing how it was authored?

Technical success alone is insufficient. The output must be inspectable at
native resolution and preserve deliberate silhouettes and family lineage.

## Technology boundary

- Use C# on .NET 8 for every engine, pack, compiler, CLI, and conformance
  module.
- Use Godot 4.7.1 .NET only for the Stage E4 preview and drawing adapter.
- Do not put compiler rules in Godot scenes or Nodes.
- Do not add shipped GDScript.
- The compiler and conformance suite must build and run without Godot.
- Do not require a browser, TypeScript, Python runtime, service, hosted
  gallery, database, runtime LLM, or network connection.
- Prefer the .NET standard library. Any added package must have a concrete
  local need, pinned version, compatible licence, deterministic behavior, and
  a short decision record.
- Add native code only after profiling proves a measured C# bottleneck.

## Required repository documents

Create and keep current:

- `README.md` — purpose and one-command quick start;
- `docs/ENGINE-SPEC.md` — local copy or precise pointer to the governing
  Palimpsest specification, without silently changing its contract;
- `docs/ARCHITECTURE.md` — actual module/dependency shape;
- `docs/CODEMAP.md` — ownership, files, interfaces, and verification entry
  points, never progress status;
- `docs/ROADMAP.md` — E0–E5 sequence and acceptance;
- `docs/HANDOFF.md` — active stage, permitted scope, proof, blockers,
  limitations, and next forbidden work;
- `docs/DEVELOPMENT.md` — exact supported build, test, compile, conformance,
  preview, and capture commands;
- `docs/DECISION.md` — initially `Pending`; at E4 record the evidence needed
  for later adopt, narrow, defer, or reject review.

Update `docs/HANDOFF.md` before each stage begins and whenever its proof or
stop condition changes. Stop at every stage boundary long enough to reconcile
Roadmap and Handoff before continuing.

## Required module shape

Implement these deep modules unless concrete evidence demands a smaller shape:

```text
Chronicle.VisualPack
    pure .NET 8 class library
    manifest/indexed-buffer codecs, immutable pack values, compatibility,
    stable handles, shared pack validation

Chronicle.VisualCompiler
    pure .NET 8 class library
    normalization, deterministic raster rules, variants, packing,
    diagnostics, review artifacts

Chronicle.VisualCompiler.Cli
    thin .NET 8 executable adapter
    file-system I/O and one-command developer workflow

Chronicle.Visuals.Conformance
    dependency-free executable or test project
    public-seam conformance, invalid fixtures, hashes, reproducibility

Chronicle.VisualPreview.Godot
    Godot 4.7.1 .NET adapter
    pack loading, native preview, layer/adjacency/palette inspection, capture
```

Dependencies flow:

```text
CLI ───────────────► Compiler ─────────► VisualPack
Conformance ───────► Compiler + VisualPack
Godot Preview ─────────────────────────► VisualPack
```

The Godot preview must not reference compiler internals. It consumes only
compiled packs and fixture render plans.

Do not add public primitive registries, plugin systems, asset-class interfaces,
or generator graphs. Keep class-specific rules internal.

## Public interfaces

The compiler's principal in-process operation is:

```csharp
CompilationResult Compile(
    VisualCatalogue catalogue,
    CompilationOptions options);
```

It returns a complete in-memory pack, ordered diagnostics, canonical hashes,
and optional review files. It performs no file-system I/O and reads no
environment state.

The CLI's principal command is:

```text
chronicle-visuals build --catalogue <path> --output <directory>
```

It exits nonzero on invalid or nondeterministic output and prints the aggregate
pack digest on success.

Do not add `IVisualCompiler` for hypothetical interchangeability. The real
drop-in seam is the compiled-pack format.

## Stage E0 — Pack contract

Implement and prove:

- `PackFormatVersion = 1`;
- normalized UTF-8 `manifest.json`;
- `hashes.json`, `validation.json`, and `provenance.json`;
- raw row-major unsigned 8-bit indexed atlas buffers;
- palettes of at most 256 RGBA8 entries with index `0` transparent;
- stable namespaced visual identifiers;
- atlas, palette, visual, compatibility, anchor, layer, family, variant,
  adjacency, transform, and digest records;
- immutable loaded-pack values and resolved visual handles;
- ordinal deterministic serialization;
- pack validation and explicit diagnostic codes;
- invalid fixtures for every required validation category;
- a manually constructed reference pack that round-trips exactly;
- two clean builds producing byte-identical canonical files and hashes.

Canonical hashes exclude timestamps, absolute paths, machine names, build
directories, locale, and PNG metadata.

Stop and reconcile the stage before E1. Do not invent raster grammar merely to
make E0 look impressive.

## Stage E1 — Small compiler

Implement and prove:

- normalized compiler-owned catalogue input;
- palettes and named palette roles;
- integer masks, anchors, layers, and simple authored silhouette parts;
- a deliberately small set of integer pixel primitives;
- explicit family seeds and variant counts;
- deterministic indexed atlas packing;
- native 16-pixel and 20-pixel targets compiled independently;
- stable geometry and aggregate hashes;
- native-size and integer nearest-neighbour review sheets;
- palette swaps whose geometry digests remain unchanged;
- local authored overrides when a generalized rule weakens recognition;
- meaningful local definition edits producing predictable local output changes.

Do not make the source catalogue a runtime or Palimpsest integration contract.

Stop and reconcile the stage before E2.

## Stage E2 — Connected forms and motifs

Implement and prove:

- the fixed four-cardinal adjacency mask:
  `North=1`, `East=2`, `South=4`, `West=8`;
- caps, straights, corners, tees, crosses, and isolated pieces;
- all declared masks or a declared deterministic fallback;
- edge-pixel continuity validation;
- connected water and cloud families;
- one wall family and one path or border family;
- multi-cell grove and stone-ridge motifs;
- motif footprints, anchor cells, ordered marks, and clipping behavior;
- deterministic variants selected from explicit family seeds;
- adjacency, shifted-overlap, motif, and layer-isolation review sheets.

Visual rules may never invent semantic connectivity or motif placement.

Stop and reconcile the stage before E3.

## Stage E3 — Vocabulary breadth

Build a small, coherent Palimpsest-shaped specimen set:

- Surface and Sky palettes;
- the Incarnation;
- the loose Stone;
- The Bell That Fell Up;
- three distinct material objects;
- several Codex, Loadout, map, or status glyphs;
- target and selection emphasis;
- static actor state or corpse treatment sufficient to test family extension;
- manually authored baseline equivalents for the same important silhouettes.

Produce controlled variants for several explicit seeds. Verify that variants
preserve family lineage and do not collapse into recolours of one silhouette.

Do not grow broad creature, building, biome, item, weapon, or animation
catalogues. This stage tests leverage, not asset count.

Stop and reconcile the stage before E4.

## Stage E4 — Godot preview adapter

Using Godot 4.7.1 .NET:

- load the compiled pack without referencing compiler internals;
- expand indexed atlases through selected palettes;
- create nearest-neighbour textures;
- draw fixture render plans at native 16-pixel and 20-pixel sizes;
- offer integer-scale nearest-neighbour inspection;
- switch palette, family, variant, adjacency mask, layer, and specimen;
- show atlas rectangle, anchor, stable identifier, family, variant, hashes,
  and validation diagnostics;
- capture deterministic review images from fixed plans;
- exercise the same pack twice and reproduce the same plan/capture metadata;
- use a bounded Node tree rather than one Node per pixel or specimen cell;
- build and launch headlessly without scene, script, or resource errors.

The preview is a read-only adapter. It does not edit catalogue sources, run the
compiler, choose semantic meaning, or create a second pack format.

## Determinism requirements

Do not use:

- `System.Random`;
- `GetHashCode`;
- Godot RNG;
- wall-clock time;
- ambient culture;
- file enumeration order;
- absolute paths;
- process history;
- floating-point raster decisions;
- thread scheduling as an ordering source.

Fix and version the hash and pseudorandom algorithms. Supply cross-platform
test vectors, including negative and large coordinates.

Integer masks, anchors, rectangles, offsets, and raster operations are
required. Any parallel compilation gathers results into stable ordinal order
before packing or serialization.

## Validation and native review

Mechanical checks must cover:

- identifiers and required mappings;
- palettes and transparency;
- atlas bounds and overlap;
- anchors, transforms, layers, and motif footprints;
- adjacency coverage and declared edge continuity;
- disconnected fragments where forbidden;
- missing native sizes;
- duplicate variants;
- geometry stability across palette swaps;
- serialization and digest stability;
- pack-reader compatibility.

Warnings may flag weak contrast, dense texture, suspicious variant similarity,
unused visuals, or converging palette roles.

Automated checks cannot approve visual quality. At E4, provide:

1. native-size sheets;
2. 4× or 8× sheets;
3. adjacency sheets;
4. shifted-overlap sheets;
5. controlled-variant sheets;
6. palette comparisons;
7. layer-isolation sheets;
8. manual-baseline comparisons;
9. Godot captures;
10. normalized source, manifest, hashes, validation, and provenance.

## Performance and packaging

- Compilation of the complete E4 specimen set must remain under ten seconds on
  the reference Palimpsest desktop.
- Pack validation and preview loading must be bounded and reported.
- The preview must not compile at runtime.
- Review sheets and authoring caches remain outside runtime pack contents.
- No background process may remain after automated verification.
- All verification must run without network access after the supported
  toolchain is installed.

## Required one-command proof

Provide one fail-fast command from the engine repository root that:

1. restores from the repository's pinned configuration;
2. builds all pure C# modules;
3. runs public-seam tests and conformance fixtures;
4. compiles the E4 catalogue twice in isolated directories;
5. compares every canonical byte and digest;
6. validates the compiled pack;
7. builds the Godot C# preview;
8. launches its headless acceptance;
9. exports deterministic review evidence;
10. verifies no errors or background Godot process remain.

Document the exact command and packaged tool versions.

## Completion and handoff

The goal is complete only when E0–E4 pass automated proof and the complete
review bundle is ready for human inspection.

At completion:

- set the engine Handoff status to `E4 implemented — awaiting visual review`;
- leave `docs/DECISION.md` as `Pending` with evidence links;
- report files and modules created;
- report exact commands and results;
- report reproducibility hashes and timings;
- report known visual and technical limitations;
- report which specimens appear stronger or weaker than the manual baseline;
- provide exact interactive preview instructions;
- stop before E5.

Do not call the engine adopted, modify Palimpsest, publish packages, commit, or
push unless the user separately authorizes those actions.
