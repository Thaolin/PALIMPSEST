# Caves of Qud Generation Alignment

**Researched:** 2026-07-21  
**Status:** source-backed design note, not a production contract or canonical
decision

## Finding

Caves of Qud does not have one world-generation algorithm. Its useful pattern is
a staged dependency chain: authored data describes available things, world facts
choose a sequence of builders, builders establish and refine spatial structure,
optional specialized algorithms generate local motifs, connectivity is repaired,
and contextual population systems place authored content.

That pattern aligns strongly with Palimpsest's World Grammar. The implementation
details do not all align. In particular, Palimpsest should not copy Qud's
[static symbolic world map and parasang topology](https://wiki.cavesofqud.com/wiki/Modding%3AIntro_-_Zones_and_Worlds),
[monolithic legacy builders](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Builders),
or exact zone dimensions. These are Palimpsest recommendations, not claims that
Qud's choices are defective for Qud.

## Corrections to the initial premise

### WFC is a tool inside the pipeline, not the world generator

Qud uses Wave Function Collapse for selected local structures such as huts,
crypts, lairs, historic-site structures, village structures, and parts of the
Tomb of the Eaters. Authored colour-map images act as examples; WFC produces a
locally similar colour map that a builder interprets as game objects. It is not
the system that generates all geography, history, creatures, or content. The
[official procedural-generation guide](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Procedural_Generation)
documents those uses and the template-to-colour-map interface.

Brian Bucklew's GDC session describes the ruin generator as a multi-pass system:
early passes establish coarse architecture, middle passes use WFC for detail,
and final passes establish connectivity and population. The result maps cultural
attributes into playable spaces rather than asking WFC to solve the whole
problem. See the
[GDC session overview](https://www.gdcvault.com/play/1025913/Math-for-Game-Developers-Tile).

### Qud's sultan history is not a full causal historical simulation

The developers explicitly distinguish their system from the granular simulation
of Dwarf Fortress. Qud generates curated events, updates shared historical
entity state, and then rationalizes causes using that state. A replacement
grammar turns the result into accounts called gospels. The shared state and
recurring cultural domains create coherence, but the system does not simulate
all underlying historical mechanics.

This is described directly in Grinblat and Bucklew's developer paper,
[“Subverting Historical Cause & Effect”](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf).
The same paper verifies that the generated history has material consequences:
visited places become historic sites, and named historical items are instantiated
at relevant sites with properties derived from their history.

### “Blueprints” are authored contracts, not another magic algorithm

Qud uses the term in more than one layer. `ObjectBlueprints` define creatures,
items, and other objects through inheritance, mixins, stats, tags, and composable
parts. World, cell, and zone blueprints select maps, ordered builders,
post-builders, and population tables. The blueprint is therefore a declarative
input to generation, not generation by itself. See the official modding pages
for [objects](https://wiki.cavesofqud.com/wiki/Modding%3AObjects) and
[worlds](https://wiki.cavesofqud.com/wiki/Modding%3AWorlds).

### Qud does not demonstrate unconstrained infinite monster invention

Qud's documented content model begins with authored creature blueprints and
authored anatomies. Population systems select those objects by explicit table,
inheritance, semantic tags, context, and tier. Parts add behavior, inventory,
names, followers, mutations, and other variation. This produces many contextual
instances, but it is not evidence for inventing arbitrary bodies, statistics,
or semantics at runtime. See the official documentation for
[object parts](https://wiki.cavesofqud.com/wiki/Modding%3AObjects),
[bodies](https://wiki.cavesofqud.com/wiki/Modding%3ABodies), and
[population tables](https://wiki.cavesofqud.com/wiki/Modding%3APopulations).

This distinction supports Palimpsest's settled Creature Grammar: authored body
plans, materials, traits, capabilities, ecologies, behaviors, and visual kits
can create arbitrarily many instances without becoming random-stat soup.

## Verified Qud pattern

1. **World generation establishes high-level facts.** `Worlds.xml` combines
   static maps and dynamic generation. World-building hooks place generated
   villages, lairs, and historic sites and can attach builders to their eventual
   zones. See [Worlds](https://wiki.cavesofqud.com/wiki/Modding%3AWorlds).
2. **Zone blueprints define an ordered recipe.** A zone may use several builders
   in succession, then post-builders and populations. The official zone-builder
   documentation also warns that many old builders are monolithic and recommends
   a more modular architecture for new work. See
   [Zone Builders](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Builders).
3. **Different spatial algorithms solve different jobs.** Qud documents noise
   for underground walls, crystals, rivers, and surface paint, while WFC handles
   selected architectural motifs. See
   [Zone Procedural Generation](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Procedural_Generation).
4. **Connectivity and population are explicit later responsibilities.** They
   are not assumed to emerge from decorative generation. The WFC talk's staged
   ruin pipeline ends with connectivity and intelligent population rather than
   treating locally plausible pixels as a finished level. See the
   [GDC WFC session](https://www.gdcvault.com/play/1025913/Math-for-Game-Developers-Tile).
5. **Population is contextual and authored.** Modern zone templates and
   population tables can draw from explicit object lists, inheritance families,
   semantic-tag intersections, and tier-weighted pools. See
   [Populations](https://wiki.cavesofqud.com/wiki/Modding%3APopulations).
6. **Generation crosses abstraction levels.** Qud's village pipeline links
   history, culture, architectural style, storytelling, NPCs, and quests rather
   than running isolated generators whose outputs merely coexist. See the
   developers' [end-to-end GDC session](https://www.gdcvault.com/play/1026313/Math-for-Game-Developers-End).
7. **Randomness can be world-seeded.** Qud exposes distinct seeded random
   streams and helpers derived from the world seed. This verifies seeded
   generation, though it does not prescribe Palimpsest's exact deterministic
   stream design. See
   [Modding: Randomness](https://wiki.cavesofqud.com/wiki/Modding%3ARandom_Functions).

## Alignment with Palimpsest

The Qud observations below are linked to the primary evidence in the preceding
section. Every “Adaptation required here” entry is a Palimpsest design inference
or a restatement of the settled north star, not a claim about Qud.

| Palimpsest concern | Qud lesson worth adopting | Adaptation required here |
|---|---|---|
| World Grammar | Make generation a dependency-ordered pipeline whose inputs are authored facts, not a bag of unrelated random calls. | Keep the pipeline engine-independent, deterministic, addressable, and capable of generating unbounded territory on demand. |
| Areas and Passages | Generate bounded working regions and make entrances, exits, stairs, and connectivity explicit constraints. | Use the same character-scale rules in every Area. A Passage changes topology, not game mode or spatial scale. |
| Large geography | Establish coarse spatial structure before local detail. | A mountain is a generated region with foothills, slopes, cliffs, passes, caves, and peaks across many cells, never an actor-sized icon. |
| WFC-like assembly | Use an example-driven solver for authored local architectural character after required anchors are known. | Treat WFC as one optional Area-builder backend. Validate reachability and Passage continuity afterward. Do not use it for world truth, creature semantics, or unconstrained geography. |
| History | Let compact historical state influence sites, relics, factions, text, and discoverable evidence. | Chronicle history continues during play and must record real material events. Ex-post narrative rationalization may add voice, but must never contradict settled world state. |
| Blueprints | Separate declarative authored content from the code that chooses and assembles it. | Candidate future contracts may define authored Area, structure, and creature-kit inputs, but this note does not settle their names or schemas. |
| Creature Grammar | Select and combine authored, semantically compatible components through ecology, location, role, and danger context. | Generate instances on demand; promote consequential creatures to durable Agents with stable identity and history. Never use arbitrary stat rolls as the primary source of novelty. |
| Determinism | Derive procedural choices from a world seed. | Prefer address- and stage-specific random streams so generation remains stable when unrelated builders change. This is a Palimpsest design inference, not a claim about Qud's internal guarantees. |
| Durable deltas | Separate initial generation from later runtime state. | Generate an Area's baseline from its stable address and grammar version, then retain material changes as durable deltas. The cited sources do not establish that Qud uses this exact persistence architecture. |
| P-GEN | Keep authored visual vocabulary separate from runtime spatial generation. | World Grammar decides what exists and its scale; P-GEN compiles how those facts render. WFC output must resolve to semantic cells before visual selection. |

## Topology: copy the seam, not Qud's map

Qud defines a zone as an `80 × 25` character-scale screen, a parasang as a
`3 × 3` group of zones, and a separate symbolic world map above surface zones.
Those facts are useful evidence that large worlds can be divided into bounded
generation units, but the strategic map conflicts with Palimpsest's settled
same-scale north star. See
[Intro — Zones and Worlds](https://wiki.cavesofqud.com/wiki/Modding%3AIntro_-_Zones_and_Worlds).

Palimpsest should retain bounded Areas for generation, activation, saving, and
rendering while connecting them through persistent Passages. Walking across
open territory, entering Home, descending into a temple, or crossing a portal
must retain the same character scale and the same Combat and Chronicle-time
rules. The World Atlas may summarize known geography, but it must not become a
second movement game.

## What not to copy

- **Do not make WFC mandatory or universal.** Qud itself documents both
  [noise and specialized WFC uses](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Procedural_Generation).
  Authored anchors, graph
  grammars, constructive carving, erosion-like fields, and other builders may
  better serve different Area families. The choice among them is a Palimpsest
  design inference.
- **Do not accept local plausibility as playability.** Every spatial builder
  needs explicit Passage, reachability, reserved-space, and traversal checks;
  Qud's [staged WFC pipeline](https://www.gdcvault.com/play/1025913/Math-for-Game-Developers-Tile)
  likewise handles connectivity after local generation.
- **Do not copy Qud's symbolic overworld.** Its documented
  [world map and parasangs](https://wiki.cavesofqud.com/wiki/Modding%3AIntro_-_Zones_and_Worlds)
  are evidence about Qud, not requirements for this game.
- **Do not standardize on `80 × 25` merely because Qud does.** It is a useful
  viewport and density comparison, not a settled Palimpsest Area size. The
  dimensions are documented in
  [Zones and Worlds](https://wiki.cavesofqud.com/wiki/Modding%3AIntro_-_Zones_and_Worlds).
- **Do not build monolithic biome generators.** Qud's current
  [zone-builder guide](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Builders)
  identifies monolithic legacy zone builders as a weakness and recommends
  modular builders for new zones.
- **Do not copy the legacy/current split between encounter systems.** The
  [population guide](https://wiki.cavesofqud.com/wiki/Modding%3APopulations)
  identifies a legacy encounter-table system alongside the preferred modern
  zone-template and population-table system. One
  composable population contract is preferable to parallel generations of
  encounter infrastructure; that preference is a Palimpsest inference.
- **Do not use generated prose as a substitute for material history.** Generated
  history earns its place when it changes sites, objects, relationships, and
  discoverable evidence. Qud's developer paper documents that connection from
  generated history to
  [sites and artifacts](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf).
- **Do not attempt an eager simulation of infinite territory or every possible
  creature.** Generate stable possibility on demand and preserve only the
  identities and deltas that become consequential. This is a Palimpsest design
  requirement, not a documented Qud technique.

## Research-informed shape for a future proof

This is a candidate experiment, not authorization:

1. Resolve one stable Area address into authored world facts and required
   Passage anchors.
2. Run a coarse builder for terrain regions and large-scale structure.
3. Run one specialized local builder—possibly WFC—for a ruin or temple motif.
4. Repair and verify connectivity without erasing the motif's character.
5. Populate the result from authored semantic tables and one small Creature
   Grammar.
6. Materialize one historical fact as a place, object, or relationship.
7. Save a player-made material delta, leave through a Passage, return, and prove
   deterministic regeneration plus delta replay.

That proof would test the Qud-aligned dependency chain while preserving
Palimpsest's different topology, persistent Chronicle, same-scale Areas, and
stronger identity rules.

## Primary sources

- Freehold Games developers Jason Grinblat and C. Brian Bucklew,
  [“Subverting Historical Cause & Effect: Generation of Mythic Biographies in Caves of Qud”](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf).
- Brian Bucklew, Freehold Games,
  [“Tile-Based Map Generation using Wave Function Collapse in Caves of Qud”](https://www.gdcvault.com/play/1025913/Math-for-Game-Developers-Tile),
  GDC 2019.
- Brian Bucklew and Jason Grinblat, Freehold Games,
  [“End-to-End Procedural Generation in Caves of Qud”](https://www.gdcvault.com/play/1026313/Math-for-Game-Developers-End),
  GDC 2019.
- Official Caves of Qud modding documentation:
  [Zones and Worlds](https://wiki.cavesofqud.com/wiki/Modding%3AIntro_-_Zones_and_Worlds),
  [Worlds](https://wiki.cavesofqud.com/wiki/Modding%3AWorlds),
  [Zone Builders](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Builders),
  [Zone Procedural Generation](https://wiki.cavesofqud.com/wiki/Modding%3AZone_Procedural_Generation),
  [Objects](https://wiki.cavesofqud.com/wiki/Modding%3AObjects),
  [Bodies](https://wiki.cavesofqud.com/wiki/Modding%3ABodies),
  [Populations](https://wiki.cavesofqud.com/wiki/Modding%3APopulations), and
  [Randomness](https://wiki.cavesofqud.com/wiki/Modding%3ARandom_Functions).
