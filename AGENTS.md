# Untitled Chronicle RPG — Agent Guide

## North star

Build a persistent-world RPG where the player’s first half-hour produces
“What the fuck was that? I can do that?” A body may die; its Chronicle and
Codex do not. Never rebuild the removed PALIMPSEST prototype or carry its
semantic-authoring gameplay forward by inertia.

Feel: Caves of Qud meets Dwarf Fortress with a healthy dose of power fantasy.
Find power words, combine them into Expressions, and use them to leave changes
that outlive the current body.

## Technology boundary

- Godot 4 .NET is the presentation, input, UI, audio, and authoring shell.
- C# is the only production language. Do not add shipped GDScript.
- Keep Chronicle rules in engine-independent C# projects. Godot Nodes adapt
  inputs and render state; they do not own simulation rules.
- The Chronicle advances on deterministic fixed ticks with pause and speed
  controls. Never require reflex-speed input.

## Product rules

- Preserve the player’s Codex of Verbs and Nouns across Incarnations.
- A Loadout is bounded and consequential; it is not the entire Codex turned on.
- Discovery, Study, the Codex, Loadouts, and Expressions are the primary power
  loop, not supporting terminology around a conventional class system.
- The Word Catalogue is large and authored. World Grammar generates contextual
  Study Sources, offered words, and Understanding yield; it does not invent new
  word semantics.
- The initial Starting Vectors are Combat, Explore, and Build. They are
  nonbinding emphases, never permanent classes or exclusive content tracks.
- Agents have identity and agency. Command communicates intent; it is not unit
  control. Command may be required for dangerous intent, but never guarantees
  obedience.
- Home is the singular primary Holding and the Chronicle's emotional anchor,
  even for a player who mostly explores ruins and dungeons. Holdings, raids,
  routes, crafted objects, and world changes are material and persistent.
- Procedural generation uses World Grammar plus memorable Landmarks; do not
  build featureless random content, hand-authored levels, or a fixed world
  edge. Generate unbounded territory on demand rather than simulating infinity.
- Palimpsests are extraordinarily rare apex artifacts, with a hard cap of two
  per World. Their exact affordances are not settled; Decrees are one direction,
  not permission to invent arbitrary semantic authoring or a required ending.

## Working rules

- Read `CONTEXT.md`, `docs/VISION.md`, `docs/CODEMAP.md`,
  `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, and `docs/HANDOFF.md` before
  proposing a major feature or changing production code.
- Update `CONTEXT.md` immediately when a domain term becomes settled. Keep it
  free of implementation details.
- Update `docs/CODEMAP.md` when module ownership, file locations, verification
  entry points, or canonical documentation changes. Never put progress status
  there.
- Treat `docs/HANDOFF.md` as the active execution contract. Update it whenever
  the current gate, permitted scope, known proof, blocker, UAT result, or next
  forbidden work changes. It may narrow an active slice but never expand it.
- At every UAT boundary, reconcile the active contract's Status, the Roadmap,
  and the Handoff before beginning more implementation.
- Add an ADR only for a hard-to-reverse, non-obvious trade-off.
- Work one vertical slice at a time; each slice needs explicit player-visible
  proof and automated core tests.
- Prefer small deterministic models, test fixtures, and saved deltas over broad
  systems that only look impressive in a diagram.
