# Chronicle Visual Engine — Drop-in Specification

**Status:** Design contract for a parallel candidate engine and its future
Palimpsest integration. No implementation currently exists in this repository.
This specification grants no authority to change production code, does not
block Slice 3 Gate 3B, and does not make the candidate engine a runtime
dependency.

The copy-paste implementation handoff is
[Build the Chronicle Visual Engine](CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md).
It authorizes a separate workspace through Stage E4 and forbids Stage E5
Palimpsest integration.

## Decision

Build the deterministic engine in **C# on .NET 8 without a Godot dependency**.
Use **Godot 4 .NET only as a preview and rendering adapter**.

This split is required:

- C# shares Palimpsest's integer, hashing, serialization, test, packaging, and
  build environment.
- A pure .NET compiler can run headlessly and reproducibly without a scene
  tree, renderer, frame loop, importer cache, or display.
- Godot remains the correct place to preview native pixels, exercise the real
  atlas texture path, inspect composition, and draw the final render plan.
- `Chronicle.Core` remains unaware of visual assets, palettes, atlas
  coordinates, and cosmetic variants.

Godot must not be used as the compiler implementation. A Godot preview project
may call the pure C# modules and display their output.

## Purpose

The candidate engine should help a small team author and maintain
Palimpsest's complete symbolic pixel vocabulary:

- ground, floors, walls, paths, water, clouds, and connected fields;
- adjacency pieces, transitions, borders, corners, caps, and joins;
- motifs such as groves, ridges, ruins, fungal growth, and masonry;
- structures, passages, Landmarks, and durable world subjects;
- tools, weapons, resources, crafted objects, and artifacts;
- actors, equipment overlays, poses, static states, and corpses;
- map glyphs, Codex and Loadout marks, status and action symbols;
- temporary emphasis such as targets, selection, danger, discovery, and
  Expression effects.

The engine is valuable only when compact authored definitions create real
leverage without sacrificing silhouette, lineage, native-scale readability,
or deliberate art direction.

It is customized for Palimpsest. It is not a general-purpose graphics product,
creature-only generator, runtime prompt system, mod platform, or second World
Grammar.

## Product ownership

World Grammar decides what exists. `Chronicle.Core` owns:

- semantic terrain and features;
- mechanically meaningful materials;
- identities and World Addresses;
- semantic adjacency truth;
- subjects, actors, Landmarks, and durable state;
- all simulation and persistence decisions.

Visual Grammar decides how that truth appears. It may select:

- palettes and palette roles;
- masks and atlas marks;
- adjacency pieces and transitions;
- controlled cosmetic variants;
- texture, silhouette, anchors, offsets, and draw order;
- temporary presentation emphasis.

Visual Grammar may never invent terrain, identity, passability, collision,
materials with gameplay meaning, entity state, or history. No visual output is
written to the Chronicle save.

An LLM may help a developer draft catalogue definitions. Those definitions are
reviewed, version-controlled project data. Runtime prompts, generated prose,
and player-authored prompts are forbidden.

## System shape

```text
Authored visual sources
        │
        ▼
Chronicle.VisualCompiler             pure C# / authoring time
        │
        ▼
CompiledVisualPack                   stable drop-in seam
        │
        ├──────────────► review sheets and diagnostics
        │
        ▼
Chronicle.Visuals                    pure C# / Palimpsest-specific composer
        ▲
        │ Chronicle.Core snapshots
        │
        ▼
VisualRenderPlan                     deterministic, transient
        │
        ▼
Chronicle.Godot adapter              Godot drawing and preview only
```

The compiler is absent from the shipped runtime. Palimpsest packages a
previously compiled pack.

## Modules and dependencies

### `Chronicle.VisualPack`

Pure .NET 8 class library. It owns:

- compiled-pack value types;
- manifest and indexed-pixel codecs;
- pack compatibility checks;
- stable visual identifiers and resolved visual handles;
- pack-level validation shared by compiler and consumer.

It references neither `Chronicle.Core` nor Godot.

This small runtime library is justified because two real producers must be
possible: Palimpsest's initial manual packager and the candidate compiler.
They meet at the pack format, not at a broad plugin interface.

### `Chronicle.VisualCompiler`

Pure .NET 8 class library. It owns:

- normalization of compiler-owned catalogue sources;
- deterministic primitives, masks, materials, motif assembly, and variants;
- indexed atlas packing;
- compiler diagnostics and review-artifact generation;
- reproducible compilation hashes.

It references `Chronicle.VisualPack`. It references neither `Chronicle.Core`
nor Godot.

### `Chronicle.VisualCompiler.Cli`

Thin .NET 8 executable adapter. It owns file-system input/output and one-command
developer use. It delegates all decisions to `Chronicle.VisualCompiler`.

The required command shape is:

```text
chronicle-visuals build --catalogue <path> --output <directory>
```

The command fails nonzero on validation or reproducibility failure and prints
the pack digest on success.

### `Chronicle.Visuals`

Future Palimpsest-side pure .NET 8 module. It owns the game-specific Visual
Grammar:

- mapping Core semantic snapshots to stable visual identifiers;
- deterministic adjacency and controlled variant selection;
- Palimpsest layer order;
- construction of packed transient render plans;
- coverage diagnostics for Palimpsest semantic types.

It references `Chronicle.Core` and `Chronicle.VisualPack`. It references no
Godot types. This module does not exist until an authorized Palimpsest gate
creates it.

### `Chronicle.Godot`

Rendering adapter. It owns:

- loading the packaged atlas into Godot textures;
- nearest-neighbour texture configuration;
- drawing render-plan marks in order;
- viewport clipping, camera placement, and UI layout;
- native-resolution preview and capture.

It does not compile catalogues, choose cosmetic variants, infer adjacency, or
map Core semantics independently.

### Optional `Chronicle.VisualPreview.Godot`

A separate Godot 4 .NET preview application may accompany the candidate engine.
It consumes `CompiledVisualPack` plus fixture render plans and offers native
scale, enlarged nearest-neighbour scale, palette, adjacency, variant, and
layer inspection.

It is an adapter, not an engine module. The compiler and its checks must still
build and run without Godot.

## Deep interfaces

Do not expose public primitive registries, generator graphs, asset-class
plugins, painter objects, or one interface per visual family. Class-specific
rules remain internal until real independent implementations demand a seam.

### Compiler interface

The principal in-process operation is:

```csharp
CompilationResult Compile(
    VisualCatalogue catalogue,
    CompilationOptions options);
```

`CompilationResult` returns:

- a complete `CompiledVisualPack`;
- ordered validation diagnostics;
- normalized-source and pack digests;
- optional review artifacts represented as in-memory files.

The operation performs no file-system writes and reads no environment state.
The CLI adapter handles paths.

`VisualCatalogue` is the normalized in-memory compiler input. The source-file
syntax used to produce it is compiler-owned and may evolve. Palimpsest must
never parse source catalogues at runtime.

Do not add `IVisualCompiler` merely to make one implementation look swappable.
The real drop-in seam is the compiled-pack format.

### Composer interface

The future Palimpsest operation is:

```csharp
VisualRenderPlan Compose(VisualCompositionInput input);
```

`VisualCompositionInput` contains:

- the exact read-only Core semantic area snapshot;
- the relevant Core actor, durable-subject, and Landmark snapshots;
- the loaded `CompiledVisualPack`;
- Chronicle seed;
- active visual style version;
- accepted native cell size;
- temporary presentation state such as selected addresses, targets, danger,
  discovery, or Expression emphasis.

It does not contain camera movement history, frame time, Godot objects, mutable
RNG state, or saved visual selections.

`VisualRenderPlan` returns ordered packed marks. Each mark contains only what
Godot needs to draw:

- resolved atlas and visual handle;
- destination cell or pixel position;
- integer offset and anchor;
- palette selection;
- layer;
- permitted integer transform flags;
- optional clipping group or emphasis parameters.

The plan is transient and never serialized into Chronicle state.

## Compiled-pack seam

The compiled pack—not the catalogue schema—is the compatibility contract.

### Directory shape

Version 1 is an ordinary directory, not a custom archive:

```text
<pack>/
  manifest.json
  hashes.json
  validation.json
  atlases/
    world-16.indices
    world-20.indices
    symbols-16.indices
  previews/
    world-16.png
    world-20.png
    native-review.png
    nearest-review.png
  provenance.json
```

Canonical runtime files are `manifest.json` and the indexed atlas buffers.
PNG files are derived review artifacts and are not used when computing
canonical geometry hashes.

### Indexed atlas format

- Each `.indices` file is an uncompressed row-major array of unsigned 8-bit
  palette indexes.
- Width, height, palette identifier, and SHA-256 digest live in the manifest.
- Index `0` is transparent in every palette.
- A palette contains at most 256 RGBA8 entries.
- Geometry uses integer pixel coordinates only.
- Native cell sizes are compiled independently. A 20-pixel asset is not
  silently derived by scaling a 16-pixel asset.
- Atlas padding and extrusion rules are explicit in the manifest.
- Atlas rectangles may not overlap and may not extend outside the buffer.

Godot expands indexed buffers through the selected palette into an RGBA8
`Image`, then creates an `ImageTexture` with nearest-neighbour filtering.

### Manifest requirements

The normalized UTF-8 JSON manifest contains:

```json
{
  "packFormatVersion": 1,
  "packId": "chronicle.default",
  "visualStyleVersion": 1,
  "composerContractVersion": 1,
  "catalogueSchemaVersion": 1,
  "compiler": {
    "id": "chronicle.visual-compiler",
    "version": "0.1.0"
  },
  "sourceDigest": "sha256:...",
  "palettes": [],
  "atlases": [],
  "visuals": [],
  "requiredMappings": []
}
```

The exact record definitions belong in `Chronicle.VisualPack`. The following
facts are mandatory.

#### Palette record

- stable palette identifier;
- ordered RGBA8 entries;
- named role-to-index mapping;
- transparent index;
- palette digest.

Roles such as `surface.ground`, `water.deep`, `landmark.gold`, and
`actor.primary` are presentation vocabulary. They do not become Core material
types.

#### Atlas record

- stable atlas identifier;
- native cell or sprite size;
- width and height;
- indexed-buffer relative path;
- palette compatibility;
- padding and extrusion;
- canonical indexed-pixel digest.

#### Visual record

- stable namespaced visual identifier;
- atlas identifier and integer rectangle;
- logical size and integer anchor;
- layer class;
- family identifier and variant ordinal;
- supported native size;
- optional adjacency mask;
- allowed integer transforms;
- palette roles used;
- geometry digest;
- authoring tags used only for inspection.

Runtime code uses stable visual identifiers, never atlas coordinates as
identity.

#### Compatibility record

- pack-format version;
- composer-contract version;
- visual-style version;
- required Palimpsest mapping identifiers;
- minimum reader version when needed.

### Hashes

`hashes.json` contains:

- normalized catalogue digest;
- normalized manifest digest;
- digest for every canonical indexed buffer;
- digest for every palette;
- aggregate pack digest computed from the ordered preceding digests.

Timestamps, absolute paths, machine names, build directories, and PNG encoder
metadata are excluded.

### Provenance

`provenance.json` records authoring-only provenance per family:

- authored, procedural, or developer-assisted origin;
- source file or definition identifier;
- licence or project ownership;
- optional review notes.

Prompts and working prose are not runtime inputs and need not ship with the
game.

## Catalogue and authoring model

The candidate compiler may choose its own author-friendly source syntax. JSON
is the recommended first adapter because .NET supplies deterministic parsing
without another production dependency. Comments or convenience syntax may be
accepted before normalization.

The normalized `VisualCatalogue` supports:

- named palettes and palette roles;
- integer masks and authored silhouette parts;
- a small set of pixel primitives;
- material treatments;
- adjacency families;
- multi-cell motifs;
- glyphs and symbols;
- explicit family seeds and variant counts;
- target native sizes;
- anchors, layers, and permitted transforms;
- authored exceptions where procedural rules fight recognition.

An author must be able to override or replace one visual family locally. The
compiler must not force every silhouette through one generalized grammar.

The compiler may share indexed buffers, masks, palettes, validation, hashing,
and packing internally while keeping terrain, motif, object, actor, and glyph
rules private and class-specific.

## Visual identifiers and families

Identifiers are lowercase namespaced strings with dot-separated segments:

```text
terrain.surface.grass
terrain.surface.water.edge
feature.surface.grove
feature.surface.ridge
terrain.sky.cloud
subject.loose-stone
landmark.bell-that-fell-up
actor.incarnation
glyph.codex.verb
glyph.loadout.slot
emphasis.target.valid
```

Rules:

- an identifier's meaning is stable after release;
- removing or repurposing an identifier is a compatibility change;
- variants belong to a stable family and use integer ordinals;
- atlas movement does not change identity;
- palette swaps do not change geometry identity;
- aliases are migration data, not duplicate runtime identities.

## Adjacency and transitions

Version 1 uses an explicit four-cardinal bit mask:

```text
North = 1
East  = 2
South = 4
West  = 8
```

Core supplies semantic neighbor truth. The composer converts that truth into a
mask and selects the corresponding visual record.

The compiler must:

- require all declared masks or an explicit deterministic fallback;
- validate caps, straights, corners, tees, crosses, and isolated pieces;
- emit an adjacency review sheet;
- verify edge pixels match where continuity is required;
- keep diagonal or eight-neighbor rules out of the public contract until an
  accepted visual family needs them.

Visual adjacency cannot create or remove a semantic connection.

Transitions between different ground or material families require explicit
ordered ownership. Version 1 permits one primary ground plus one transition
mark per cell; it does not introduce arbitrary blend graphs.

## Multi-cell motifs

A motif definition contains:

- stable motif family identifier;
- integer footprint;
- anchor cell;
- ordered marks with local cell and pixel offsets;
- required semantic occupancy tags;
- optional deterministic variants;
- clipping and edge behavior.

World Grammar decides that a grove, ridge, ruin, or other motif exists and
where its semantic cells are. The compiler supplies reusable marks; the
composer never creates a semantic motif because an attractive visual template
fits.

Motif variants are selected from Chronicle seed, stable motif identity or
anchor address, visual style version, family identifier, and variant salt.

## Composition and layer order

Palimpsest composes in this order:

1. ground field;
2. adjacency edge or transition;
3. material or environmental feature;
4. structure or passage;
5. Landmark or durable subject;
6. actor;
7. temporary action effect;
8. target or selection emphasis;
9. restrained UI overlay.

Higher layers may cover pixels but may not erase the semantic read of lower
layers unless hiding is the intended meaning.

The Incarnation, dangerous emphasis, and Landmarks require stronger contrast
than ordinary terrain. Bright landmark gold remains reserved. Colour is never
the sole identifier.

## Determinism

Compilation and runtime composition are deterministic across supported
operating systems.

### Compiler determinism

Identical normalized catalogue, compiler version, compilation options, and
explicit family seeds produce identical:

- indexed pixel buffers;
- palettes;
- normalized manifest;
- validation diagnostics and ordering;
- canonical hashes.

The implementation must not use:

- `System.Random`;
- `GetHashCode`;
- Godot RNG;
- wall-clock time;
- file enumeration order;
- current locale;
- floating-point raster decisions;
- thread scheduling as an ordering source;
- absolute paths or environment state.

Any parallel compilation gathers results into a stable ordinal order before
packing or serialization.

### Runtime determinism

Cosmetic selection derives from:

```text
visual style version
+ Chronicle seed
+ absolute World Address or durable identity
+ stable visual family identifier
+ semantic layer
+ explicit variant salt
```

The hash and pseudorandom algorithms are fixed by version and accompanied by
cross-platform test vectors. Camera position, query bounds, query order, frame
timing, process history, and Godot state never participate.

Shared addresses in overlapping requests receive identical visual handles and
integer transforms.

### Integer geometry

Masks, anchors, offsets, rectangles, and raster operations use integers.
Fixed-point arithmetic may be introduced internally only with an explicit
scale and conformance tests. Floating-point values may be used by the final
Godot drawing adapter after the render plan is complete.

## Versioning policy

Four versions remain distinct:

| Version | Meaning |
| --- | --- |
| `PackFormatVersion` | Binary/JSON reader compatibility |
| `CatalogueSchemaVersion` | Compiler-owned authoring input compatibility |
| `VisualStyleVersion` | Pixel families and deterministic cosmetic selection |
| `ComposerContractVersion` | Meaning of render-plan and mapping inputs |

`WorldGrammarVersion` remains a separate Core concern.

For the first integration, the active compiled pack pins one visual style for
the application build. Existing Chronicles may receive visual revisions after
an application update; their semantic state does not change.

Do not add `VisualStyleVersion` to `ChronicleState` merely to preserve pixels.
If the product later promises that each Chronicle retains an exact historic
appearance, design a separate presentation-profile migration owned outside
Core and authorize it explicitly.

Rendered pixels, visual handles, selected variants, atlas positions, and
render plans are never saved.

## Validation

Compilation fails on:

- invalid or duplicate identifiers;
- invalid palette indexes or unexpected colours;
- missing required palette roles;
- out-of-bounds or overlapping atlas rectangles;
- missing native sizes;
- broken required adjacency coverage;
- edge mismatch where continuity is declared;
- missing anchors or invalid motif footprints;
- missing required Palimpsest mappings;
- unsupported transforms or layers;
- nondeterministic normalization or serialization;
- duplicate variants when variation was requested;
- disconnected fragments that violate an asset's occupancy rule;
- invalid transparency or unreadable empty occupancy;
- digest mismatch.

Warnings may report:

- weak native-scale contrast;
- overly dense texture;
- family variants with suspiciously high similarity;
- silhouettes near occupancy limits;
- unused visuals;
- palette roles that converge visually.

Deterministic repair is allowed only when:

- the repair rule is versioned;
- its change appears in diagnostics;
- repaired output participates in hashes;
- the authored source remains inspectable.

Mechanical validity does not prove visual quality. Native-resolution review and
player UAT remain mandatory.

## Compiled-pack conformance suite

The candidate engine must ship a dependency-free .NET conformance runner with:

- normalized catalogue fixtures;
- expected manifest and indexed-buffer hashes;
- palette-swap fixtures with unchanged geometry hashes;
- every four-cardinal adjacency mask;
- negative and large-coordinate runtime-selection vectors;
- stable-identity and absolute-address variant vectors;
- invalid packs for every required validation class;
- pack-reader compatibility fixtures;
- clean-build reproducibility checks.

The conformance runner crosses the public compiler and pack interfaces. It does
not test private primitives directly.

Palimpsest integration supplies additional fixtures:

- Surface and Sky semantic snapshots for seeds `41337`, `41338`, and `90421`;
- overlapping request rectangles;
- the Incarnation;
- the loose Stone before and after movement;
- The Bell That Fell Up;
- target, selection, Codex, and Loadout emphasis.

## Definition of drop-in

The candidate engine is drop-in compatible only when:

1. It emits an accepted `PackFormatVersion` without Palimpsest parsing its
   source catalogue.
2. The existing `Chronicle.VisualPack` reader accepts the output unchanged.
3. The existing `Chronicle.Visuals` composer resolves every required stable
   visual identifier.
4. `Chronicle.Godot` draws the new pack without code or scene changes.
5. No `Chronicle.Core` type, rule, command, or save migration changes.
6. Clean builds on two supported environments produce identical canonical
   hashes.
7. Equivalent packs from the manual adapter and candidate compiler can be
   swapped by configuration or packaged content alone.
8. Deleting the compiler leaves the game runnable from its committed compiled
   pack.
9. Native-scale review demonstrates recognition and visual lineage.
10. Authoring evidence shows meaningful local changes are materially cheaper
    than editing the equivalent sprites manually.

If integration requires importing the compiler's scene graph, running it at
startup, translating Core semantics into its private vocabulary, or rewriting
Godot drawing, it is not drop-in.

## Godot adapter contract

Godot:

- loads and validates one compiled pack at startup;
- expands indexed atlases into one or a few RGBA8 textures;
- uses nearest-neighbour filtering and integer-aligned destinations;
- draws ordered render-plan marks through bounded raster or batched canvas
  operations;
- clips to the visible area;
- exposes native-size and integer-scale preview;
- captures deterministic review images from a fixed render plan;
- reports missing handles or pack incompatibility loudly in development.

Godot does not:

- read source catalogues;
- run the compiler at game startup;
- select variants;
- infer adjacency;
- create one Node per map cell;
- use imported texture order as identity;
- silently replace missing visuals with gameplay meaning.

The player view and World Atlas Inspector consume the same composer. The
Inspector may use a coarser raster at large overview scale, but it may not
implement a second semantic-to-visual mapping.

## Performance and packaging

Runtime requirements:

- composition complexity is linear in visible semantic cells plus emitted
  marks;
- render plans use packed immutable storage rather than one heap object or
  Godot Node per cell;
- a 33 × 23 local view composes comfortably within one 16 ms frame on the
  project's reference desktop after warmup;
- atlas loading happens once per active pack;
- ordinary camera movement performs no catalogue parsing or atlas compilation;
- large Inspector overviews remain bounded and may reduce presentation detail;
- native code is forbidden until profiling proves a measured C# bottleneck.

Authoring requirements:

- the Gate 3B cross-section compiles in under ten seconds on the reference
  desktop;
- a reproducibility build may compile twice and compare canonical hashes;
- compilation is an explicit developer or build action, never an implicit
  runtime stall;
- the shipped game includes the compiled pack, not compiler source,
  intermediate caches, review sheets, or authoring dependencies.

## Review bundle

The compiler can optionally emit:

1. native-size atlas and specimen sheets;
2. 4× or 8× nearest-neighbour sheets;
3. adjacency sheets for every connected family;
4. shifted-overlap compositions;
5. controlled variants for several family seeds;
6. palette swaps with geometry digests held constant;
7. layer-isolation sheets;
8. actor, object, Landmark, glyph, and emphasis specimens;
9. a comparison against manually authored equivalents;
10. normalized definitions, manifest, hashes, validation, and provenance.

Review sheets are evidence, not canonical runtime files.

## Palimpsest evaluation cross-section

When the candidate engine is ready, evaluate:

- Surface and Sky palettes;
- connected water and cloud;
- one wall family and one path or border family;
- grove and stone-ridge motifs;
- the Incarnation, loose Stone, and The Bell That Fell Up;
- three distinct material objects;
- several Codex, Loadout, map, or status glyphs;
- target and selection emphasis;
- native 16-pixel and 20-pixel candidates.

The comparison must include a small manual baseline using the same pack seam.
The question is authoring leverage and visual quality, not whether the engine
can technically draw pixels.

## Acceptance signals

Adopt or narrow the engine only if:

- all tested classes read as one visual language;
- every required specimen reads at native resolution without labels;
- connected forms remain coherent across request edges;
- controlled variants preserve recognizable lineage;
- local catalogue edits produce predictable local changes;
- definitions are materially simpler than equivalent manual authoring;
- output is reproducible, inspectable, and easy to diff;
- the pack replaces the manual adapter without runtime code changes;
- the module removes complexity that would otherwise spread across views and
  asset families.

## Rejection signals

Reject or reduce the engine if:

- output reads as generic noise or interchangeable procedural art;
- schemas are as laborious as drawing assets directly;
- correcting one silhouette requires fighting generalized grammar;
- families converge on one outline with cosmetic recolouring;
- the engine requires a browser, service, runtime LLM, or second gallery;
- runtime generation creates stalls or packaging complexity;
- visual definitions begin carrying gameplay rules;
- player view and Inspector diverge;
- broad catalogues are required before a playable slice needs them;
- native-size recognition loses to the manual baseline;
- the game must depend on compiler implementation details.

## Delivery stages for the parallel engine

Each stage stops with reviewable evidence.

### Stage E0 — Pack contract

- implement `Chronicle.VisualPack`;
- normalize manifest serialization;
- read/write indexed buffers and palettes;
- establish hashes and invalid-pack fixtures;
- prove a manually constructed pack loads through the conformance runner.

### Stage E1 — Small compiler

- compile palettes, masks, anchors, and simple authored silhouettes;
- pack deterministic atlases;
- emit diagnostics and native review sheets;
- reproduce output across clean builds.

### Stage E2 — Connected forms and motifs

- implement four-cardinal families;
- add explicit transitions and deterministic fallbacks;
- add multi-cell grove and ridge specimens;
- prove overlap and edge continuity.

### Stage E3 — Vocabulary breadth

- add the Incarnation, Stone, Bell, three objects, glyphs, and emphasis;
- compare procedural and manual silhouettes;
- reject generalized rules that weaken recognition.

### Stage E4 — Godot preview adapter

- load the same compiled pack;
- draw fixture render plans at native and enlarged scales;
- capture sheets without introducing compiler logic into Godot.

### Stage E5 — Palimpsest conformance

- run the Gate 3B semantic fixtures through Palimpsest's composer;
- swap the candidate pack for the manual pack without code changes;
- record adopt, narrow, defer, or reject.

No stage authorizes Palimpsest production integration by itself.

## Non-goals

The engine does not initially include:

- runtime catalogue compilation;
- runtime prompts or LLM calls;
- player-generated visual rules;
- gameplay or semantic generation;
- arbitrary vector illustration;
- animation state machines;
- skeletal runtime animation;
- 3D, lighting, shader, or post-processing systems;
- a general plugin framework;
- a public mod format;
- network services or hosted galleries;
- automatic art-quality claims;
- broad creature anatomy, building, biome, or item catalogues without a
  concrete Palimpsest demand;
- live Godot hot reload or an editor plugin;
- Chronicle save fields for pixels or cosmetic choices.

## Future decision record

After a real candidate is evaluated, record:

- **adopt, narrow, defer, or reject**;
- which visual classes showed genuine leverage;
- which classes remain better as authored sprites;
- accepted pack, composer, and versioning contracts;
- evidence from native review and Godot integration;
- authoring-time and runtime performance;
- exact changes, if any, to Palimpsest Architecture and Codemap;
- the playable slice that justifies production integration.

Until that decision, the manual Gate 3B pack remains the reference adapter and
the candidate engine remains optional.
