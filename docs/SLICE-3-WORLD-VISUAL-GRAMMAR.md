# Slice 3 — A World With Shape

## Status

Proposed. Begin only after all three Goal 2 UAT gates pass.

Slice 3 precedes Goal 4 — Three Openings, One Chronicle.

## Outcome

Replace independent coloured-tile noise with a small deterministic World
Grammar and a coherent Visual Grammar. A player should read terrain,
relationships, the Incarnation, a changed world subject, and The Bell That Fell
Up without the world looking hand-authored or visually arbitrary.

This slice changes how generated places are structured and presented. It does
not add settlement or social simulation.

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
| Visible patch target | 21 × 15 logical cells |
| Initial cell target | 24 × 24 pixels at integer scale |
| Surface motifs | clearings, vegetation clusters, stone ridges, and connected water |
| Sky motifs | open lanes and connected cloud banks |
| Existing Landmark | Preserve The Bell That Fell Up and its durable address |
| Traversal | All generated terrain remains traversable in Slice 3 |
| Fixture seeds | `41337`, `41338`, and `90421` |
| Visual mode | layered symbolic pixel tiles |
| UAT artifacts | annotated captures for all three fixture seeds |

Cell size and patch dimensions may change once during the visual prototype if
the three-seed comparison proves them unreadable. After Slice 3 UAT accepts the
scale, later slices treat it as the baseline rather than reopening it casually.

## Player journey

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

## World Grammar contract

### Small interface

Core exposes one patch-generation operation over Chronicle state and a visible
area. It returns an inspectable generated snapshot. Do not introduce generator
interfaces, plugin registries, rule graphs, or an authoring language while
there is one implementation.

The generated snapshot contains only what callers need:

- World Address;
- semantic ground;
- optional semantic feature;
- optional Landmark or durable subject identity;
- enough neighboring semantic context for Visual Grammar to compose edges.

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

### First sky rules

The sky grammar creates:

- connected cloud banks;
- open lanes large enough to read movement;
- a distinct approach around The Bell That Fell Up.

The Bell keeps its established address through Slice 3 so Goal 2 journeys and
predecessor saves remain valid. Later Grammar may vary Landmark placement only
after durable generated Landmark addresses have an explicit migration policy.

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

- Ground uses broad fields and restrained texture.
- Water, ridges, cloud banks, walls, and paths use adjacency-aware pieces.
- Vegetation and debris use a small family of deterministic variants.
- Landmarks and subjects use recognizable silhouettes larger in visual weight
  than ground detail.
- Godot derives cosmetic variants from visual style/version, Chronicle seed,
  World Address, and semantic layer. It uses a stable hash, not Godot RNG.
- Cosmetic variants are never serialized and never alter gameplay meaning.

### UI baseline

Keep the existing world/readout/hotbar layout recognizable while establishing:

- dark neutral panels;
- thin restrained borders;
- one consistent selected-slot treatment;
- readable Codex, Study, and Loadout states from Goal 2;
- map priority over decorative UI chrome.

This is not a final HUD or a Dwarf Fortress-style management interface.

## Two implementation gates

### Gate 3A — World shape

Implement and verify semantic surface/sky grammar using temporary debug
symbols if necessary.

Gate 3A tests one player-visible rule: a generated form remains coherent as the
visible patch moves.

The independent UAT artifact is a semantic debug sheet with:

1. one fixed-colour/glyph surface capture for each fixture seed;
2. a second `41337` capture shifted by half a viewport;
3. address annotations tracing at least one water/stone/vegetation form across
   the overlapping captures;
4. a sky capture tracing one cloud bank across the same kind of overlap.

Gate 3A passes when the player can trace one connected form across the shifted
captures without cosmetic variants or final art. Motif interaction and
different seed compositions remain automated/supporting evidence, not
additional experiential gates.

Stop for UAT if:

- terrain still reads as a checkerboard;
- patch edges reveal discontinuities;
- different seeds share the same spatial composition;
- the Bell or moved Stone loses durable identity;
- generated data is serialized to make tests pass.

### Gate 3B — Visual language

Map the accepted semantic grammar through the Visual Grammar and produce the
three fixture captures.

Stop for UAT if:

- the player or Landmark is lost in texture;
- every cell competes for attention;
- terrain types require labels to distinguish;
- decorative variation changes after reload;
- a visual choice changes any Core decision or replay result;
- the result imitates one reference rather than synthesizing the shared
  direction.

Do not begin Goal 4 until both gates pass.

## Automated Core proof

Prove:

1. Each fixture seed generates the same ordered semantic patch on every run.
2. Different fixture seeds change spatial composition, not merely cosmetic
   variants.
3. Overlapping surface patches agree on shared addresses.
4. Overlapping sky patches agree on shared addresses.
5. Water and cloud adjacency is internally consistent.
6. Each fixture contains the required interacting motifs.
7. The Bell remains at its established address and keeps its identity.
8. The moved Stone delta overrides generated state after save/load.
9. A predecessor save without a grammar version migrates to version 0 and
   reproduces its legacy surface and sky.
10. A new Chronicle pins version 1 even after the application's default later
    changes.
11. Generated tiles and visual choices are absent from saved JSON.
12. Replay results do not depend on render or camera order.
13. Every Goal 2 and earlier check still passes.

## Godot and visual UAT proof

Automated Godot acceptance must confirm:

- the C# project and editor callback build;
- every semantic tile type has a Visual Grammar mapping;
- the Incarnation, Bell, loose Stone, and hotbar render from Core snapshots;
- the same semantic patch plus visual style/version produces the same render
  plan;
- movement and patch regeneration do not create scene or resource errors;
- no GDScript or Godot RNG generates Chronicle state.

Manual UAT uses a four-image review sheet:

1. seed `41337` surface;
2. seed `41338` surface;
3. seed `90421` surface;
4. seed `41337` sky with the Bell and moved Stone visible.

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

1. World Grammar Gate 3A passes Core checks and player UAT.
2. Visual Grammar Gate 3B passes Godot checks and annotated visual UAT.
3. Three fixture seeds are structurally distinct and visibly coherent.
4. Patch overlap, save/load, and replay remain deterministic.
5. Goal 2's complete Study–Expression–death–replacement journey still works.
6. Core owns semantic generation and durable deltas.
7. Godot owns visual composition without owning gameplay meaning.
8. Goal 4 Starting Vector, Study Source, Home, and combat work has not begun.

## Non-goals

Do not add:

- collision, pathfinding, hazards, survival, combat, field of view, fog of war,
  lighting simulation, weather, seasons, or animation systems;
- Agents, factions, Holdings, Return Routes, Pressures, raids, jobs, resources,
  production, or inventory;
- a general-purpose grammar language, editor plugin, mod format, procedural
  infinity framework, or multiple visual themes;
- high-resolution illustration, copied reference assets, post-processing, or a
  final UI redesign;
- new Strata, Verbs, Nouns, Expressions, Landmarks, or death causes.
