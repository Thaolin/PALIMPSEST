# Site Blueprint generation guidelines

This is a design contract for future spatial generation. Site Blueprints are
not implemented in The First Patch.

A Site Blueprint is a bounded deterministic recipe for a place such as a cave,
village, lake, or ruin. It is not an Entity Prototype and not a frozen tile
stamp.

## Contract

1. **Explicit identity.** Every materialized site receives a stable ID derived
   from the root seed, blueprint kind, and deterministic placement key.
2. **Independent randomness.** Each site owns a named RNG stream. Layout,
   inhabitants, and decoration use derived substreams so adding a flower cannot
   reroll a cave entrance.
3. **Constraints before decoration.** A blueprint declares footprint, terrain
   requirements, clearance, required connectivity, and exclusion rules before
   placing content.
4. **Anchors and sockets.** Entrances, roads, waterways, doors, and nesting
   points are explicit connection anchors. Blueprints compose only through
   compatible anchors and bounded spatial budgets.
5. **Bounded work.** Placement and validation have fixed attempt and memory
   budgets. Failure selects a deterministic fallback or omits the site; it
   never loops indefinitely or grows unbounded scratch state.
6. **Stable ordering.** Candidate sites and internal parts are processed in a
   canonical order independent of unrelated generation systems.
7. **Validate invariants.** A generated cave must keep required chambers
   reachable; a village must connect required lots to a road; a lake must keep
   its shoreline and inlet/outlet rules coherent.
8. **Save recipe and scars.** Saves retain blueprint identity/version plus
   sparse materialized changes. Unchanged geometry regenerates from Genesis.
9. **Keep provenance.** Generated tiles and Entities can report which Site
   Blueprint and anchor produced them, preserving the promise that places can
   eventually be opened and edited.
10. **Honor C memory contracts.** Generation receives explicit fixed-capacity
    scratch storage, reports overflow, and performs no unbounded host allocation
    during simulation.

## Scarred Zones are not Site Blueprints

A Site Blueprint answers where a place and its material parts come from. A
Scarred Zone answers why meaning resolves differently within a bounded place.
A ruin may host a Scarred Zone, but either may exist without the other. Site
connection anchors and sockets remain spatial-generation vocabulary; an
in-fiction holder of a semantic change is always called a Patch Anchor.

Future Scarred Zone generation must obey these additional constraints:

1. Give the zone a stable identity, deterministic boundary, Patch Anchor, and
   Provenance.
2. Express the changed meaning through bounded typed Patches and Clauses rather
   than a bespoke biome callback.
3. Begin with one dominant violated assumption so symptoms remain learnable.
4. Apply the rule consistently inside its boundary and leave the ordinary
   baseline legible outside it.
5. Let Knowledge reveal symptoms, concept, author, and Clause progressively;
   valid altered behavior is not an Anomaly.
6. Store the recipe and sparse Scars rather than materializing a second map.
7. Define spatial binding, overlapping-zone precedence, and Patch conflicts in
   an acceptance milestone before adding runtime records. Current Reach does
   not yet contain a regional form.

## First proofs when implemented

- **Cave:** one guaranteed entrance and one connected objective chamber, with
  optional branches derived from a separate stream.
- **Village:** one road spine with compatible building-lot sockets and a
  deterministic fallback hamlet when full placement fails.
- **Lake:** one validated basin footprint with explicit shoreline and optional
  inlet/outlet anchors.

The first runtime slice should implement only one of these and test same-seed
identity, different-seed variation, connectivity, bounded failure, provenance,
and save regeneration before a second blueprint kind is added.

The first later Scarred Zone proof should likewise contain only one altered
rule, one inspectable Patch Anchor, and one deterministic boundary. Its complete
loop is to witness the symptoms, survive them, reveal the rule through
Observation, and extract or repair a compatible piece of its grammar.
