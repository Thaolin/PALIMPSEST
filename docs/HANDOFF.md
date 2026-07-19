# Active Handoff Contract

Last reconciled: 2026-07-19

This document is the short-lived execution contract for the current gate. It
may narrow the active slice but may never expand or contradict
[AGENTS.md](../AGENTS.md), the [Vision](VISION.md), the
[glossary](../CONTEXT.md), the [Architecture](ARCHITECTURE.md), the
[Roadmap](ROADMAP.md), or the active implementation contract.

Update this file whenever the active gate, permitted scope, known proof,
blocker, UAT result, or next forbidden work changes.

## Current gate

**Slice 3 complete — awaiting explicit Goal 4 authorization**

- Slice 2A passed player UAT.
- Slice 2B passed player UAT.
- Slice 2C implementation and automated proof pass.
- Slice 2C's functional UAT journey passed on 2026-07-19.
- UAT exposed a Codex/Loadout text overlap. The layout and headless overlap
  regression check were corrected and the player accepted the visual recheck.
- Goal 2 is complete.
- Gate 3A implementation and automated proof pass.
- The player accepted Gate 3A on 2026-07-19 as the deliberately semantic/debug
  alpha described by its contract.
- UAT noted that the green circles read as trees or groves, that the water and
  ridge forms visibly cross them, and that the overview remains periodic,
  symmetrical, and more like a painting than a world.
- Those observations do not block the narrow Gate 3A coherence proof, but they
  are requirements evidence for later work and must not be dismissed as final
  art polish.
- Gate 3A is complete.
- The player confirmed that no procedural asset compiler prototype currently
  exists in this repository and will explore that engine separately.
- The candidate engine is optional, parallel, and non-blocking.
- Its E0–E4 copy-paste build handoff is complete and explicitly stops before
  E5 Palimpsest integration.
- Gate 3B now has one manually authored, versioned pack seam; one pure C#
  composer; and one Godot drawing path shared by the player and Inspector.
- Both native candidates pass automated proof: `20 px / 33 × 23` and
  `16 px / 41 × 29`.
- The player accepted Gate 3B on 2026-07-19 and selected
  `20 px / 33 × 23` as the local-view baseline.
- The 16 px candidate remains deterministic comparison evidence and a
  diagnostic launch option, not the player-view baseline.
- The Inspector's default 1024 × 1024 overview remains semantic and protected;
  Visual Grammar preview is available only after zooming to a bounded 64 × 64
  or 32 × 32 request.
- Gate 3B and Slice 3 are complete.
- A separately developed procedural generator prototype now exists for
  read-only conformance review. Its existence does not authorize integration.
- Goal 4 and all later production work remain forbidden.

Detailed authority:
[Slice 3 — A World With Shape](SLICE-3-WORLD-VISUAL-GRAMMAR.md).
The non-blocking parallel engine contract is
[Chronicle Visual Engine — Drop-in Specification](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md).
Its separate-workspace implementation prompt is
[Build the Chronicle Visual Engine](CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md).

## Objective now

Preserve the accepted Slice 3 state and stop before Goal 4. The accepted local
baseline is `20 px / 33 × 23`; a later authorized camera treatment may zoom
that view without casually reopening the native compiled-pack scale. The UAT
record and exact launch commands remain in
[Gate 3B Visual UAT](GATE-3B-VISUAL-UAT.md).

The external procedural generator prototype may be inspected against the
documented drop-in contract. That review is read-only evidence gathering, not
E5 integration authority and not permission to change PALIMPSEST production
code.

A later candidate Visual Asset Compiler may drop in only as an authoring
adapter that emits the accepted compiled pack. Runtime composition and Godot
drawing must not depend on how that pack was authored.

The candidate compiler is specified as pure C#/.NET 8 without Godot. Godot is
limited to preview and drawing adapters. The compiler is never shipped or
invoked at runtime.

The implemented seams are:

- the concrete Chronicle.Core bounded-area snapshot operation over
  Chronicle-owned generation inputs, Stratum, and an absolute-address
  rectangle;
- the immutable `Chronicle.VisualPack` compiled-pack contract and manual
  reference pack;
- the engine-independent `Chronicle.Visuals` composition input and render plan;
- the shared Chronicle.Godot player/Inspector adapter and their isolated
  headless presentation/control acceptance paths.

The completed Gate 3B implementation is limited to:

- a small versioned compiled visual pack with stable visual identifiers,
  palette roles, atlas coordinates, anchors, and validation metadata;
- deterministic render plans derived only from Core semantic snapshots,
  Chronicle seed, absolute address or durable identity, visual style version,
  and temporary presentation emphasis;
- compact manually authored Gate 3B assets and adjacency-aware composition for
  the existing Surface, Sky, Incarnation, loose Stone, Bell, target, and
  Loadout/Codex presentation;
- the shared player-view and Inspector visual-preview adapter;
- 16-pixel and 20-pixel native-resolution candidates and annotated review
  sheets for the three fixture seeds;
- automated mapping, overlap, stable-variant, save-omission, build, and Goal 2
  regression proof required by the Slice 3 contract.

The accepted 20 px candidate is now the baseline. The 16 px implementation and
captures remain comparison evidence; do not make it a second supported product
mode by inertia. Do not begin another PALIMPSEST feature under this completed
gate.

## Player-visible proof

The accepted journey is:

1. Move the loose Stone to `sky (1, 0)` and save.
2. Reach the Bell with the first Incarnation and configure `Fly[Stone]`.
3. Deliberately ring the Bell and confirm death.
4. See a timeless awaiting-replacement state rather than a checkpoint reload.
5. Create a replacement at `surface (0, 0)`.
6. See the same `Fly` and `Stone` Codex, Understanding, Chronicle state, and
   moved Stone, but a new Incarnation identity and eight empty Loadout slots.
7. Equip intrinsic `Fly`, return to the sky, and observe the moved Stone.
8. Save, quit, relaunch, and see the replacement Chronicle restored exactly.

This journey remains the Goal 2 regression proof throughout Slice 3.

## Automated proof at hand

- Gate 3A Core checks now prove the concrete bounded-area operation, ordered
  determinism, grammar version 0 migration and legacy regeneration, grammar
  version 1 fixture composition, whole-versus-tiled and overlapping requests,
  reversed query order, Surface/Sky adjacency context, named long-form motifs,
  Bell and moved-Stone durable identity, read-only generation, save omission,
  and replay independence.
- `Chronicle.Visuals.Checks` proves both compiled pack sizes, unique stable
  identifiers, atlas/palette bounds, every connected-feature mask, exact
  dry-ridge-to-water-crossing edge compatibility, controlled variants,
  canonical layers, visible crop, overlap agreement, and repeatable plan
  digests without Godot. It also proves clamped viewports and composition at
  both numeric absolute-address limits without neighbor wrapping.
- The player and Inspector both compose the same Core snapshot plus one-cell
  halo through `VisualGrammar.Compose`; at the numeric storage limits that halo
  contains every representable neighbor. Godot expands and draws the resulting
  plan without a second semantic mapping.
- The player acceptance passes at `20 px / 33 × 23` and
  `16 px / 41 × 29`, including Bell, actor, loose Stone, target emphasis,
  save/load visual digest, UI layout, and the entire Slice 2C journey.
- The Inspector Gate 3B acceptance passes at both sizes, proves the exact
  compiled pack and shared plan, retains every Gate 3A diagnostic and
  save-preservation assertion, and regenerates the fixture review pairs.
- The default 1024 × 1024 Inspector overview keeps Visual Grammar preview
  disabled; the shared native plan is composed only for local 64 × 64 or
  32 × 32 requests.
- The separate inspector headless acceptance starts the direct scene, exercises
  the 1024 × 1024 overview, pan, zoom, fixture and numeric seed selection,
  Surface/Sky, recentering, all five diagnostics, and deterministic capture,
  then reports `GATE3A ATLAS ACCEPTANCE PASS`.
- The absolute-address diagnostic uses stable absolute-coordinate guides and a
  hovered-cell readout with World Address, ground, feature, motif, and durable
  identity.
- Equivalent capture state reproduces byte-identical PNG and JSON after
  unrelated query and overlay ordering.
- Inspector verification passes both with no player save and beside a sentinel
  player save whose bytes and timestamp remain unchanged.
- The 1024 × 1024 overview contains 1,048,576 semantic cells but uses 38 Godot
  Nodes; the acceptance ceiling is 48.
- The dependency-free Core harness passes through deterministic death,
  awaiting-replacement restrictions, replacement identity, fresh Loadout,
  replay, literal predecessor migration, and save/load before and after
  replacement.
- Every earlier Core check remains in the harness and passes.
- Godot's headless acceptance drives the complete 2C journey through visible
  controls and reports `SLICE2C ACCEPTANCE PASS`.
- The Godot C# build, editor callback, clean startup, controlled save creation,
  and next-launch restoration pass.
- The restored automated Chronicle is Incarnation `2`, alive at `sky (0, 0)`,
  with intrinsic `Fly`, the complete Codex and Understanding, tick `16`, normal
  speed, and the loose Stone still at `sky (1, 0)`.
- `Chronicle.Core`, `Chronicle.VisualPack`, and `Chronicle.Visuals` build and
  their checks run without Godot.

Exact commands and packaged executable paths remain in the
[Development guide](DEVELOPMENT.md).

## Do not drift into

- A general-purpose Visual Asset Compiler, procedural creature/item/building
  framework, broad catalogue schema, plugin system, or asset gallery.
- A runtime dependency on the parallel candidate engine, its implementation
  language, a service, prompts, or proprietary scene output.
- A shipped player-facing World Atlas, player knowledge or fog, labels, routes,
  atlas travel, terrain editing, a grammar-authoring UI, or an editor plugin.
- Public chunk interfaces, generator hierarchies, a general procedural
  framework, eager whole-Stratum generation, or a finite World edge.
- Collision, hazards, pathfinding, combat, weather, lighting, animation, new
  Strata, words, Study Sources, Landmarks, factions, Agents, Holdings, or Goal
  4 systems.
- General registries, factories, event sourcing, migration frameworks, or
  interfaces justified only by future systems.

If an appealing idea falls in this list, leave it for its authorized gate.

## Known proof at hand

- `checks/verify.ps1` passed on 2026-07-19 with the packaged .NET SDK 8.0.423
  and official Godot 4.7.1 stable Mono release.
- The exact command `& .\checks\verify.ps1` passed against the final Gate 3B
  pixels and documentation boundary. It reported the Core and Visuals check
  markers, Gate 3A Inspector beside absent and sentinel saves, Gate 3B
  Inspector at 20 px and 16 px beside absent and sentinel saves, repeat
  Gate 3B player journeys at both densities, byte-identical review pairs,
  `SLICE2C ACCEPTANCE PASS`, the editor callback, next-launch restoration, and
  the final integrated Gate 3B pass marker.
- Core checks include connected-component long-form assertions and
  non-wrapping adjacency at the minimum and maximum representable coordinates.
- The inspector's initial 1,048,576-cell Core request measured about 2.1
  seconds on this desktop. The complete headless control-and-capture journey
  remains bounded and exits without a background Godot process.
- The player accepted the Slice 2A and Slice 2B runtime journeys.
- The player reported every Slice 2C functional UAT assertion passed; the only
  failure was Codex/Loadout text clipping.
- Godot reported the Codex label's runtime minimum height as 101 pixels. The
  Codex panel now reserves that height and explicit non-overlapping gaps, and
  the headless journey asserts both Codex-to-Loadout and Loadout-to-button
  separation.
- The complete verification gate passed again after the correction.
- The player confirmed the corrected layout looks good.
- Slice 2C and Goal 2 passed player UAT on 2026-07-19.
- The player inspected Gate 3A Surface and Sky captures and accepted the gate
  as a semantic/debug alpha on 2026-07-19.
- The player identified the Surface's green circles as ambiguous
  trees/groves, noticed water/ridge crossings, and described the broad
  composition as symmetrical and painted rather than world-like.
- The player accepted Gate 3B at `20 px / 33 × 23` on 2026-07-19, citing its
  stronger readability and noting that a later camera treatment can zoom out
  without changing the accepted native baseline.
- Slice 3 passed both gates and is complete.

## Known limitations

- The Gate 3B pack is a deliberately compact authored alpha: two grove/dry
  ridge variants, four water-crossing variants, restrained ground texture, and
  one connected cloud-bank treatment. UAT accepted its current readability and
  hierarchy; later art expansion still requires an authorized gate.
- Version 1 Surface composition is intentionally small and visibly periodic:
  circular grove clusters sit on a jittered grid, soil uses broad repeated
  bands, and the water and ridge use continuous triangular forms.
- Water suppresses vegetation on the cells it occupies. A water/ridge
  intersection now composes as low connected shoals rather than dry crags, but
  richer ecological, route, obstruction, and topology-sensitive interactions
  do not exist.
- Gate 3B can make accepted semantics legible and visually coherent; tile art
  cannot by itself make the underlying semantic layout less symmetrical or
  more world-like. Any such change requires separately authorized World
  Grammar refinement, deterministic Core proof, and inspector re-evaluation.
- No procedural pixel compiler exists in the repository. Gate 3B therefore
  starts with a compact authored pack while keeping the authoring adapter
  replaceable.
- The full candidate-engine pack, composer, conformance, validation,
  versioning, performance, and staged-delivery contract is documented, but
  that document does not authorize an in-repository compiler implementation.
- The parallel handoff authorizes E0–E4 only in a separate engine workspace;
  E5 integration remains forbidden from both tasks.

## Stop and hand off

Slice 3 is complete and reconciled at the accepted 20 px baseline. Stop before
Goal 4 and obtain explicit production authority for its first gate. Reviewing
the external generator prototype may identify conformance work, but do not
integrate it, rebuild it inside this repository, start semantic World Grammar
refinement, or begin Goal 4 without new explicit authorization.
