# P-GEN E5.1 — Materials Read as Materials

**Authorized:** 2026-07-21  
**Status:** Accepted and closed 2026-07-21  
**Predecessor result:** E5 automation passed; player visual UAT rejected the
candidate because terrain read as connected walls and important sprites were
too small inside their cells.

## Question

Can P-GEN produce a readable authored world in which water looks like water,
trees look like trees, mountains look like mountains, and important subjects
occupy their cells—while also giving the player a practical authoring surface
for reviewing assets and briefing the next biome?

This is a visual-authoring correction inside the required P-GEN pipeline. It
does not reopen the proven Palimpsest reader or permit compiler code in the
shipped game.

## Reference scale

The accepted Palimpsest pack remains native `20 × 20` during this spike. The
comparison target supplied by the player is Caves of Qud's `16 × 24` sprite
cell and `80 × 25` visible zone. That comparison indicates that logical tiles
do not need to become larger merely to improve readability: Qud fits more
columns than Palimpsest. E5.1 must first correct silhouette occupancy,
rectangular character proportion, material treatment, and repetition.

The authoring workbench may preview a Qud-like `2:3` display aspect beside the
native square cell. That preview cannot change Chronicle coordinates, the pack
contract, or gameplay camera density by implication.

## Permitted scope

E5.1 may:

- replace generic center-and-arm rastering with material-specific treatments;
- keep true connectivity for walls, roads, fences, pipes, and similar authored
  structures, while water, clouds, groves, ridges, and crossings use their own
  visual grammar;
- enlarge the authored masks for the current Incarnation, landmarks, stones,
  Cairns, Hearthstone, Bell, and other current subjects;
- add visual-only variants and authoring specimens needed to judge a small
  surface biome kit, without inventing new Chronicle semantics;
- add a separate authoring-only Godot workbench in P-GEN that may reference the
  compiler, recompile a catalogue in memory, browse/filter assets, compare
  masks and variants, preview native and tall-cell presentation, and render a
  representative biome board;
- add an explicit biome-brief export for collaboration on future authored
  biome kits; and
- refresh the compiled four-file bundle, pinned vocabulary fixture, hashes,
  review evidence, packaged Palimpsest data, and checks required by the visual
  correction.

The existing pack-only preview remains pack-only. The new workbench is a
separate Adapter at the authoring seam and is never a Palimpsest dependency.

## Player-visible acceptance

At native scale and enlarged nearest-neighbour review:

1. continuous water is a field with shoreline only at material boundaries,
   not a pipe or wall lattice;
2. grove cells read as individual trees or small clusters rather than green
   connectors;
3. ridge cells read as peaks, crags, or rock faces rather than wall segments;
4. only genuinely connected structures use generic cardinal connectors;
5. the Incarnation and principal landmarks occupy roughly 70–90% of cell
   height unless an authored exception explains the smaller silhouette;
6. Bell, Incarnation, loose Stone, Hearthstone, intact Cairn, and shattered
   Cairn remain distinguishable without labels;
7. one mixed biome board reads as ground, shore, water, trees, and mountains
   before metadata is consulted; and
8. the workbench can recompile the catalogue, inspect assets by mouse, compare
   adjacency and aspect, and save a bounded biome brief through one documented
   launch command.

The player must review the corrected world and workbench before E5.1 closes.

Implementation produced 249 definitions and canonical aggregate
`sha256:85418f3025f2944d2f58a0a981febb00903bf67edcc23cb84054b3fd9f91eae0`.
Both complete repository verifiers pass. The player accepted the corrected
assets and workbench with one deferred non-blocking note that the actor art
looks terrible and should be iterated later. The verdict is recorded in
[P-GEN-E5-1-UAT.md](P-GEN-E5-1-UAT.md).

## Required automated proof

- P-GEN compiles twice to byte-identical canonical and review output.
- Material conformance proves that water, grove, ridge, cloud, crossing, and
  transition treatments do not call the generic connector primitive.
- Occupancy checks cover the current principal actor and landmark silhouettes.
- The canonical reader, exact vocabulary, package contents, player and
  Inspector paths, deterministic composition, saves, World Grammar, and every
  retained Palimpsest journey still pass.
- Published Palimpsest builds contain the four compiled pack files and no
  P-GEN compiler, workbench, catalogue, concept sheet, or biome brief.

## Explicitly forbidden

E5.1 does not authorize gameplay, World Grammar, save migration, camera or map
coordinate redesign, successor combat, Goal 6A vocabulary, semantic biome
generation, runtime catalogue parsing, live pack editing in Palimpsest, or a
general-purpose pixel editor. A future biome remains an authored visual kit
for already-settled Chronicle facts.

## Stop condition

Stop after automated proof and a player review launch. Goal 6A remains
downstream and separately authorized.
