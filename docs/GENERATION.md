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
