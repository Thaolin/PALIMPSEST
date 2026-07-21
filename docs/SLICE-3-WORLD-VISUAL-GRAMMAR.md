# Slice 3 — A World With Shape

## Status

Gate 3A passed its automated Core, inspector, Godot, and Goal 2 regression
proof plus explicit player UAT on 2026-07-19. The player accepted it as the
deliberately semantic/debug alpha while recording that its grove circles,
crossing forms, periodicity, and symmetry are not a sufficiently world-like
final grammar.

Gate 3B's compact manually authored visual pack, compiler-neutral
pack/composition seam, shared player/Inspector rendering, dual-density proof,
and player visual UAT passed on 2026-07-19. The player selected
`20 px / 33 × 23` as the local-view baseline because its subject, Landmark,
crossing, and feature hierarchy read more clearly. The 16 px candidate remains
comparison evidence. Gate 3B and Slice 3 are complete. A procedural Visual
Asset Compiler may be developed separately and evaluated later; it was not a
Gate 3B dependency.

Visual Grammar may improve legibility and presentation, but it must not be
claimed to solve periodic or shallow semantic composition without an
explicitly authorized World Grammar refinement. Slice 3 precedes Goal 4 —
Three Openings, One Chronicle.

## Outcome

Replace independent coloured-tile noise with a small deterministic World
Grammar whose large-scale forms can be interrogated through a developer-only
interactive World Atlas Inspector. After its semantic shape passes UAT, map
the same generated truth through a coherent Visual Grammar in a denser local
player view.

The developer should be able to pan and zoom over a large bounded request,
switch fixture seeds and Strata, inspect absolute addresses and semantic forms,
and find generator seams before those rules become the player's world. A
player should then read terrain, relationships, the Incarnation, a changed
world subject, and The Bell That Fell Up without the world looking
hand-authored or visually arbitrary.

This slice changes how generated places are structured and presented. It does
not add settlement or social simulation, and it does not ship a player-facing
World Atlas.

## The paired grammars

World Grammar decides what exists:

- semantic terrain and features;
- spatial relationships and adjacency;
- durable Landmark identity and address;
- generated subjects plus saved world deltas;
- a pinned World Grammar version for the Chronicle.

Visual Grammar decides how that state appears:

- palette;
- glyph or tile selection;
- adjacency-aware composition;
- controlled cosmetic variation;
- draw order, emphasis, and UI framing.

Godot may map semantic state to art. It may not invent terrain, Landmark,
passability, subjects, or world history.

## One generation seam, two presentation adapters

Chronicle.Core exposes one concrete bounded-area generation interface. The
local player viewport and the developer World Atlas Inspector are two
Chronicle.Godot presentation adapters over the same ordered semantic
snapshots. Core checks cross that same interface.

The inspector is the first tool shaped so that a future player-facing World
Atlas can reuse the generation and visual-projection seams. Slice 3 does not
add player knowledge, fog, labels, map travel, or discovery persistence.

A request rectangle is a window into an unbounded Stratum, never the Stratum's
size. The inspector may show a 1024 × 1024-address overview and then pan beyond
it; that does not create a 1024 × 1024 World or require eager generation around
it.

## Reference direction

The supplied visual references point toward a shared direction rather than one
game to copy:

- orthographic, grid-first top-down space;
- dense symbolic information at a readable scale;
- limited palettes with a few strong accents;
- recognizable silhouettes before detailed illustration;
- layered ground, feature, structure, subject, and actor marks;
- connected water, walls, paths, and material fields;
- dark, restrained UI panels that frame rather than overpower the map;
- variation produced by repeated visual rules, not arbitrary per-cell colour.

The target is a crisp symbolic pixel grammar between pure ASCII and fully
illustrated tiles. Do not copy another game's assets, UI, palette, or exact
glyph vocabulary.

## Fixed slice choices

| Concern | Slice 3 choice |
| --- | --- |
| Inspector overview target | At least one bounded 1024 × 1024-address semantic overview, with pan beyond its request bounds |
| Inspector rendering | One or a few raster/batched draw surfaces; never one Godot Node per World Address |
| Inspector modes | Semantic overview and diagnostic overlays in Gate 3A; Gate 3B Visual Grammar preview only at bounded local requests of 64 × 64 or less |
| Local player density | Accepted baseline: 20 px at 33 × 23 cells at the 1280 × 720 validation size; 16 px at 41 × 29 remains comparison evidence |
| Surface motifs | clearings, vegetation clusters, stone ridges, and connected water |
| Sky motifs | open lanes and connected cloud banks |
| Existing Landmark | Preserve The Bell That Fell Up and its durable address |
| Traversal | All generated terrain remains traversable in Slice 3 |
| Fixture seeds | `41337`, `41338`, and `90421` |
| Visual mode | layered symbolic pixel tiles |
| UAT artifacts | annotated captures for all three fixture seeds |

Gate 3B presented the strongest 16-pixel and 20-pixel local-view candidates
rather than adding permanent runtime zoom controls. UAT selected
`20 px / 33 × 23`; later slices treat that native scale as the baseline rather
than reopening it casually. A separately authorized camera treatment may zoom
the view without changing the accepted compiled-pack cell size.

## World Grammar contract

### Small interface

Core exposes one bounded area-generation operation over Chronicle state, a
Stratum, and an absolute-address rectangle. It returns an inspectable ordered
semantic snapshot. Do not introduce an `IWorldGenerator`, plugin registry,
rule graph, authoring language, or public chunk abstraction while there is one
generation implementation.

The rectangle is the only presentation-shaped input. Chunk size, caching,
parallelism, and query tiling remain hidden implementation choices and may not
alter semantics. The same large area generated once or as overlapping smaller
requests must agree at every World Address.

The generated snapshot contains only what callers need:

- World Address;
- semantic ground;
- optional semantic feature;
- optional Landmark or durable subject identity;
- enough neighboring semantic context for Visual Grammar to compose edges.

The inspector and future player Atlas may project this snapshot differently,
but they do not receive a second semantic generator. Slice 3 stays at one
semantic cell per World Address; it does not build continental simulation,
region-level history, or a general multi-resolution world framework.

### First surface rules

The surface grammar combines a few deterministic fields:

- connected water rather than isolated blue cells;
- clustered vegetation with readable boundaries;
- stone ridges or fields with direction and continuity;
- grass or soil clearings that separate denser motifs.

At least two motifs must interact in every fixture seed. For example, a
watercourse may cut a ridge or a clearing may interrupt vegetation. The result
must be explainable from a handful of rules; do not add dozens of terrain
types to manufacture variety.

At least one named form in each fixture seed must remain traceable across
several local viewport widths and remain recognizable in the inspector
overview. Large-scale readability may come from deterministic low-frequency
fields or constructed forms, but it may never depend on request bounds or
generation order.

### First sky rules

The sky grammar creates:

- connected cloud banks;
- open lanes large enough to read movement;
- a distinct approach around The Bell That Fell Up.

The Bell keeps its established address through Slice 3 so Goal 2 journeys and
predecessor saves remain valid. Later Grammar may vary Landmark placement only
after durable generated Landmark addresses have an explicit migration policy.

## Developer World Atlas Inspector contract

The inspector is a separate developer launch mode in the existing Godot
project. It is not a Godot editor plugin and is not reachable as a shipped
player feature in Slice 3.

Its deliberately small interface provides:

- pan and zoom over bounded Core area requests;
- fixture-seed selection, with optional arbitrary numeric seed entry;
- surface/sky Stratum selection;
- recentering on the origin, the Incarnation, or the Bell when applicable;
- toggles for semantic classes, absolute addresses, request bounds, motif
  identity, and durable Landmark/subject identity;
- a deterministic capture/export action carrying seed, grammar version,
  Stratum, bounds, and zoom metadata;
- in Gate 3B, a toggle between semantic debug presentation and the Visual
  Grammar projection, enabled only for a 64 × 64 or smaller local
  request.

The inspector is read-only. Its camera, overlays, selected seed, and capture
settings are developer preferences, not Chronicle state. It must not create,
load, overwrite, or advance the player's save. Rebuilding after a grammar
change is sufficient; Slice 3 does not add live grammar editing, painting,
rule-authoring UI, or hot reload.

At overview scale, semantic cells are rasterized or drawn in batches. The
default 1024 × 1024 overview keeps Visual Grammar preview disabled; zoom to a
64 × 64 or 32 × 32 local request before enabling it. This protects the large
semantic overview while local visual presentation changes detail over the same
addresses; it does not instantiate a million Nodes or materialize a permanent
finite layer.

### Determinism and overlap

- A Chronicle saves its World Grammar version when it is created.
- The same seed, durable state, and visible area return the same ordered
  semantic tiles.
- Overlapping patches agree on every shared World Address.
- Patch generation does not depend on camera movement order.
- Generated tiles are reproduced, not serialized.
- Saved world deltas overlay generated state by durable identity or World
  Address.
- Godot randomness never enters Chronicle state.

Changing the default World Grammar affects new Chronicles only. Existing
Chronicles retain their pinned generator until an explicit migration exists.
See [ADR 0002](adr/0002-pin-world-grammar-version-per-chronicle.md).

Saves created before this field existed migrate to `WorldGrammarVersion = 0`,
the retained Slice 0–2 surface/sky generators. New Chronicles created after
Slice 3 use version 1. Migration must never assign an old save to whatever
version happens to be the current default.

## Visual Grammar contract

### Composition stack

Compose each visible cell in this order:

1. ground field;
2. adjacency edge or transition;
3. material or environmental feature;
4. Landmark or durable subject;
5. Incarnation;
6. temporary action emphasis;
7. UI selection and target indicators.

Higher layers must not erase the semantic read of lower layers unless hiding it
is the intended visual meaning.

### Palette

- Give surface and sky distinct limited palettes.
- Reserve the brightest gold for Landmarks and consequential discoveries.
- Reserve a consistent high-contrast actor treatment for the Incarnation.
- Use cyan, white, or pale values sparingly for sky structure and active UI.
- Use colour plus silhouette; never make colour the only identifier.
- Avoid unrelated random hue changes between neighboring cells.

### Tile and glyph vocabulary

- Use a small version-controlled pixel atlas for recognizable silhouettes and
  a deterministic C# composition layer for adjacency, palette, controlled
  variants, and draw order. Gate 3A semantic debug colours may remain
  programmatic.
- Ground uses broad fields and restrained texture.
- Water, ridges, cloud banks, walls, and paths use adjacency-aware pieces.
- Vegetation and debris use a small family of deterministic variants.
- Landmarks and subjects use recognizable silhouettes larger in visual weight
  than ground detail.
- The engine-independent Visual Grammar derives cosmetic variants from visual
  style/version, Chronicle seed, World Address, family, and semantic layer. It
  uses a stable hash, not Godot RNG.
- Cosmetic variants are never serialized and never alter gameplay meaning.

This hybrid art path is intentional: do not require one authored image for
every generated cell, and do not ask runtime procedural geometry to carry all
recognition and character. The atlas contains reusable marks; the Visual
Grammar decides how they compose.

### UI baseline

Keep the existing world/readout/hotbar layout recognizable while establishing:

- dark neutral panels;
- thin restrained borders;
- one consistent selected-slot treatment;
- readable Codex, Study, and Loadout states from Goal 2;
- map priority over decorative UI chrome.

The local map must show materially more territory than Slice 2 while preserving
the contextual side panel and avoiding text clipping. This is not a final HUD
or a Dwarf Fortress-style management interface.

## Two implementation gates

### Gate 3A — Inspect the world's shape

Implement and verify the semantic surface/sky World Grammar and expose it
through the developer World Atlas Inspector. Temporary fixed colours and debug
glyphs are the correct presentation at this gate.

Gate 3A tests one experiential rule: large generated forms remain coherent
while the inspector pans across request bounds and zooms between overview and
local scales.

The UAT journey is:

1. Open the inspector at surface seed `41337`, centered on the origin.
2. Show a bounded 1024 × 1024-address overview and identify connected water,
   vegetation, stone, and clearings.
3. Choose one recognizable form, zoom into it, and confirm its addresses and
   local semantic cells describe the same shape.
4. Pan until the original request edge crosses the middle of the view; confirm
   the form continues without a seam.
5. Enable address, request-bound, motif, and durable-identity overlays; confirm
   overlays explain the result without changing it.
6. Switch to seeds `41338` and `90421`; confirm spatial compositions differ
   rather than merely changing colour.
7. Switch seed `41337` to sky, trace a cloud bank across a request edge, and
   recenter on the Bell.
8. Export deterministic captures with their generation metadata and annotate
   the forms that pass or fail.

The inspector itself is the independent UAT instrument. Its review artifact
contains one overview capture per fixture seed, one zoomed detail and shifted
overlap for seed `41337`, and one sky capture with the Bell. Gate 3A passes
when the player can trace coherent forms between overview, detail, and shifted
requests without cosmetic variants or final art.

Stop for UAT if:

- terrain still reads as a checkerboard;
- patch edges reveal discontinuities;
- different seeds share the same spatial composition;
- overview forms dissolve into unstructured pixel noise;
- zoomed details contradict the overview at the same addresses;
- inspector actions mutate, create, or advance a player Chronicle;
- a large overview creates one Godot Node per address or freezes the desktop;
- the Bell or moved Stone loses durable identity;
- generated data is serialized to make tests pass.

Gate 3A passed automated proof and player UAT on 2026-07-19.

### Gate 3B — Give the world a visual language

**Status:** accepted on 2026-07-19 at `20 px / 33 × 23`; automated proof also
retains the 16 px comparison candidate. See
[Gate 3B Visual UAT](GATE-3B-VISUAL-UAT.md).

Map the accepted semantic grammar through the Visual Grammar in both the local
player view and the inspector's visual-preview mode. Compare the strongest
16-pixel and 20-pixel local cell treatments, accept one exact density, and
produce the three fixture captures. The Inspector preserves its large semantic
overview; UAT zooms to a 64 × 64 or 32 × 32 request before enabling the visual
preview.

The Gate 3B player journey is:

1. Launch fixture seed `41337` at the surface origin.
2. Identify water, stone, vegetation, the Incarnation, and the loose Stone
   without reading raw terrain names.
3. Move far enough for the visible patch to regenerate.
4. Confirm newly revealed cells continue existing shapes rather than changing
   at the viewport edge.
5. Use Goal 2's Fly and moved-Stone proof; confirm both remain legible in the
   new presentation.
6. Reach The Bell That Fell Up and identify it before reading its label.
7. Save, quit, and reload; confirm semantic placement and controlled visual
   variants return.
8. Repeat the visual inspection with seeds `41338` and `90421`.

The three Chronicles should feel related by one visual language but different
in spatial composition. A seed difference that only recolours the same layout
does not pass.

Stop for UAT if:

- the player or Landmark is lost in texture;
- every cell competes for attention;
- terrain types require labels to distinguish;
- the local view shows fewer than 33 × 23 logical cells at 1280 × 720;
- the contextual panel or Goal 2 controls clip at the accepted density;
- decorative variation changes after reload;
- a visual choice changes any Core decision or replay result;
- the result imitates one reference rather than synthesizing the shared
  direction.

The initial authored pack and the required P-GEN compiler meet at one versioned
compiled-pack seam. Replacing the authoring adapter must not change Core
semantics, Chronicle saves, runtime composition inputs, or Godot drawing. The
exact P-GEN and drop-in requirements are in the
[Chronicle Visual Engine specification](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md).

Both Slice 3 gates pass. This completed contract does not itself authorize
Goal 4 production work.

## Automated Core proof

Prove:

1. Each fixture seed generates the same ordered semantic patch on every run.
2. Different fixture seeds change spatial composition, not merely cosmetic
   variants.
3. A large area generated in one request agrees with the same area assembled
   from smaller requests.
4. Overlapping surface requests, including negative coordinates, agree on
   shared addresses.
5. Overlapping sky requests agree on shared addresses.
6. Query order, pan direction, zoom, overlays, and capture order cannot alter
   generated semantics.
7. Water and cloud adjacency is internally consistent.
8. Each fixture contains the required interacting motifs and at least one form
   traceable across several local viewport widths.
9. The Bell remains at its established address and keeps its identity.
10. The moved Stone delta overrides generated state after save/load.
11. A predecessor save without a grammar version migrates to version 0 and
   reproduces its legacy surface and sky.
12. A new Chronicle pins version 1 even after the application's default later
    changes.
13. Generated tiles, inspector state, and visual choices are absent from saved
    JSON.
14. A read-only area request leaves Chronicle state byte-for-byte unchanged.
15. Replay results do not depend on render or camera order.
16. Every Goal 2 and earlier check still passes.

## Godot and visual UAT proof

Automated Godot acceptance must confirm:

- the C# project and editor callback build;
- the developer inspector launch mode starts without scene, script, or resource
  errors;
- pan, zoom, seed selection, Stratum selection, overlays, and capture export
  translate into bounded queries and presentation state only;
- a 1024 × 1024-address overview uses a bounded raster/batched representation,
  not one Node per address;
- Visual Grammar preview remains disabled above 64 × 64 and composes only a
  bounded local 64 × 64 or 32 × 32 request;
- launching and using the inspector does not create, overwrite, load, or
  advance the player save;
- every semantic tile type has a Visual Grammar mapping;
- the Incarnation, Bell, loose Stone, and hotbar render from Core snapshots;
- the same semantic patch plus visual style/version produces the same render
  plan;
- movement and patch regeneration do not create scene or resource errors;
- no GDScript or Godot RNG generates Chronicle state.

Gate 3A manual UAT uses the interactive inspector journey and its annotated
semantic captures. Gate 3B manual UAT uses a four-image visual review sheet:

1. seed `41337` surface;
2. seed `41338` surface;
3. seed `90421` surface;
4. seed `41337` sky with the Bell and moved Stone visible.

The implemented review sheet presents both native candidates for each image,
with deterministic PNG/JSON pairs under `.tools/gate3b-review/`; the tracked
annotations and decision prompt are in
[Gate 3B Visual UAT](GATE-3B-VISUAL-UAT.md).

Annotate directly:

- what reads immediately;
- what becomes visual noise;
- where connected forms break;
- whether landmarks and actors dominate appropriately;
- which motifs should become stronger or quieter.

Pixel-perfect screenshot tests are not required. Deterministic semantic and
variant selection is automated; composition quality is judged by UAT.

## Save compatibility

- Preserve every predecessor save fixture.
- Save the Chronicle's World Grammar version.
- Migrate predecessor saves with no version to retained legacy version 0.
- Existing seed, address, clock, Intent, Codex, Study, Loadout, Incarnation,
  and world-delta state must load unchanged.
- A visual revision may change pixels but may not silently change semantic
  terrain, Landmark identity, or durable subject state.
- Do not serialize rendered tiles, sprites, glyph indices, adjacency masks, or
  captured patches.

## Definition of done

Slice 3 is complete only when:

1. World Grammar Gate 3A and the developer World Atlas Inspector pass Core,
   Godot, and player UAT.
2. Visual Grammar Gate 3B passes Godot checks and annotated visual UAT.
3. The inspector can pan and zoom across a bounded 1024 × 1024-address
   overview without creating a finite World edge or per-address Nodes.
4. Three fixture seeds are structurally distinct and visibly coherent at both
   overview and local scales.
5. The accepted local player view shows at least 33 × 23 logical cells at the
   1280 × 720 validation size without clipping Goal 2 UI.
6. Area overlap, save/load, and replay remain deterministic.
7. Goal 2's complete Study–Expression–death–replacement journey still works.
8. Core owns semantic generation and durable deltas.
9. `Chronicle.Visuals` owns deterministic composition; Godot owns inspection,
   rasterization, and drawing without owning gameplay meaning or player-save
   state.
10. Goal 4 Starting Vector, Study Source, Home, and combat work has not begun.

## Non-goals

Do not add:

- collision, pathfinding, hazards, survival, combat, field of view, fog of war,
  lighting simulation, weather, seasons, or animation systems;
- Agents, factions, Holdings, Return Routes, Pressures, raids, jobs, resources,
  production, or inventory;
- a shipped player World Atlas, discovery/knowledge overlays, map travel,
  fast travel, atlas labels, or a runtime tactical-camera zoom feature;
- eager generation of a whole Stratum, a fixed 1024 × 1024 World, edge-triggered
  map expansion, full-fidelity off-screen simulation, or serialized generated
  terrain;
- terrain painting, a general grammar authoring UI, editor plugin, live
  generator hot reload, mod format, procedural infinity framework, public
  chunk interface, multi-resolution region simulation, or multiple visual
  themes;
- high-resolution illustration, copied reference assets, post-processing, or a
  final UI redesign;
- new Strata, Verbs, Nouns, Expressions, Landmarks, or death causes.
