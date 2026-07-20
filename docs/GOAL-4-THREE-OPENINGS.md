# Goal 4 — Three Openings, One Chronicle

## Status

Authorized on 2026-07-19. Slice 4A implementation, automated proof, player UAT,
and tracker reconciliation completed on 2026-07-19. Slice 4B implementation and
full automated proof completed on 2026-07-19. Its functional player journey,
Clock/Codex correction, focused recheck, and tracker reconciliation completed
on 2026-07-20. Slice 4B is accepted. Slice 4C is the next candidate but remains
forbidden until separately authorized. The complete retained verifier passed
again after the correction on 2026-07-20.

The player confirmed the public test seams and exact first catalogue/source
fixture on 2026-07-19.

```text
Slice 3 complete
  → 4A A Choice of Words
  → UAT
  → 4B A Place Called Home
  → UAT
  → 4C A Consequential Fight
  → UAT
  → Goal 5
```

## Goal outcome

A new Chronicle will eventually offer Combat, Explore, and Build as three
different First Horizons into one persistent world. They are Starting Vectors,
not permanent classes, separate campaigns, or content locks.

This contract authorizes one child slice at a time. Completion of 4A is not
permission to build Home or combat before the corresponding slice is
separately authorized.

## Slice 4A — A Choice of Words

### Player-visible hypothesis

A Study Source should make the player interpret something strange and choose
what it means, rather than reveal one hidden reward or pay generic experience.

### Fixed first fixture

The smallest real authored Word Catalogue contains:

| Word | Kind | Current meaning |
| --- | --- | --- |
| `Fly` | Verb | The accepted Explore First Verb and intrinsic/fitted traversal capability |
| `Stone` | Noun | The accepted material Noun compatible with `Fly[Stone]` |
| `Bell` | Noun | The identity embodied by The Bell That Fell Up; it has no active Expression in 4A |

The generated Study Source is the sky-stone clapper within The Bell That Fell
Up. It is regenerated from the pinned World Grammar, durable Landmark
identity, and World Address. It offers, in order:

1. `Stone` — the dark clapper is stone veined with open sky and rises against
   the curve that contains it;
2. `Bell` — the gold vessel, clapper, and impossible fall make its identity
   legible as a Bell.

The source qualities are `Rare`, `Lethal`, and `Landmark`. Together
they let the source contribute at most `16` Understanding to either offered
word, preserving the accepted sixteen-Chronicle-tick Study scale. Source
quality determines its possible contribution; one Chronicle tick performs one
point of Study while the selected pursuit remains valid.

`Fly` is catalogue-authored but is granted by the Explore Starting Vector, so
it is not a fake second offer after the player already knows it.

The player confirmed these exact fixture choices on 2026-07-19.

### Module seam

Keep `ChronicleSimulation.Apply(ChronicleCommand)` as the single command
interface. Add one read-only `CurrentStudySource` query for the same Core state
Godot and checks already consume.

The Core Study module hides:

- catalogue storage and normalization;
- source generation from World Grammar semantics;
- catalogue kind and compatibility validation;
- source quality and word-specific yield calculation;
- canonical Codex membership and Understanding storage;
- active-pursuit validity, tick advancement, completion, and rejection.

Do not add a catalogue interface, generator interface, registry, factory, or
Godot-facing rule abstraction. One concrete implementation does not justify a
hypothetical adapter.

The confirmed public seams for tests and callers are:

- `ChronicleSimulation.Apply(...)` plus public state/query results;
- one read-only catalogue snapshot/lookup exposing stable authored Word
  definitions for Codex presentation and black-box conformance;
- `ChronicleSaveCodec` for literal migration and round-trip persistence;
- the real Godot controls and visible labels for the player journey.

No test may reach through these seams to private generation or calculation
helpers.

### Domain model

- A stable authored Word identifier is the common identity used by the Word
  Catalogue, Codex, Understanding, Study offers, and Loadout.
- `CodexState` is a canonical ordered set of learned Word identifiers, not one
  Boolean per word.
- `StudyState` contains canonical word-specific Understanding entries and at
  most one active `(Study Source, Word)` pursuit.
- A generated Study Source snapshot contains its stable identity, World
  Address, contextual text, qualities, and ordered offers.
- Each offer identifies one catalogue word, its contextual rationale, and the
  maximum Understanding this source can contribute to that word.
- Source snapshots are regenerated facts and are never serialized.
- Codex membership, word-specific Understanding, and the active pursuit are
  Chronicle state and survive save/load.

### Rules

- The Explore opening remains the diegetic `UP` Intent and grants catalogue
  word `Fly`; presentation must identify it as the Explore Starting Vector.
- A Study Source exists only when the generated semantic cell and durable
  identity satisfy its authored World Grammar rule.
- The current source query is deterministic, read-only, and independent of
  query order, UI state, frame timing, or prior inspection.
- A version `2` source exposes at least two distinct plausible catalogue words
  before the player chooses.
- Choosing requires the source identity and Word identity. Core re-resolves
  the current source and rejects stale, absent, unknown, learned, or unoffered
  choices without mutation.
- Exactly one word may be actively pursued at a time. Choosing a different
  offered word changes the active pursuit but preserves all accumulated
  word-specific Understanding.
- Only the selected word advances, and only on Chronicle ticks while a living
  Incarnation remains at the source.
- Pause stops Study because it stops Chronicle ticks.
- Leaving the source, dying, or awaiting replacement clears the active pursuit
  but retains all Understanding.
- A replacement Incarnation inherits the Chronicle's Codex and Understanding
  with a fresh empty Loadout. Returning to the source and continuing Study
  requires another deliberate selection.
- Understanding never exceeds the catalogue threshold or the source's
  contribution for that word.
- Reaching the threshold adds the word to the Codex exactly once and clears the
  active pursuit.
- Learning a word does not invent an Expression. `Bell` has no compatible
  active Expression in 4A.

### Player journey

1. Begin a new Chronicle and choose the `UP` Explore opening.
2. See `Fly` in the Codex and use it to reach The Bell That Fell Up.
3. Examine the generated sky-stone Study Source.
4. See its situation, `Rare / Lethal / Landmark` qualities, `16`
   Understanding contribution, and the ordered `Stone` and `Bell` offers with
   distinct reasons.
5. Deliberately choose `Stone`.
6. See only Stone Understanding begin; Bell remains at zero and neither word
   enters the Codex early.
7. Pause and prove Study stops.
8. Save partway, perturb the Chronicle, and load the exact selected word and
   partial Understanding.
9. Deliberately end the Incarnation at the Bell and create its replacement.
10. See the partial Stone Understanding and complete Codex survive while the
    new Incarnation receives eight empty Loadout slots.
11. Re-equip `Fly`, return to the source, and deliberately choose `Stone`
    again.
12. Complete the remaining Study and see `Stone` enter the Codex exactly once.
13. Preserve the accepted `Fly[Stone]`, moved-Stone, death, replacement, and
    restart journey as regression proof.

### Persistence and migration

- Bump the minimal save envelope to version `2`.
- Deserialize version `1` and pre-envelope saves through explicit predecessor
  values rather than the redesigned current state type.
- Map legacy `Intent == Up` or `HasFly` to catalogue word `Fly`.
- Map legacy `HasStone` or completed Stone Understanding to learned `Stone`
  with full Understanding.
- Map partial legacy Stone Understanding exactly.
- Map a valid legacy active Bell Study to the generated Bell source plus
  selected word `Stone`; clear activity when away, complete, dead, or awaiting
  replacement.
- Map the old independent Verb value `1` to `Fly` and old independent Noun
  value `1` to `Stone`; never pass those colliding numeric values through as a
  unified Word identity.
- Preserve seed, tick, address, speed, Intent, Incarnation identity and life,
  eight ordered Loadout slots, and the loose-Stone delta. Version `1` World
  Grammar explicitly becomes version `2`; legacy version `0` remains version
  `0`.
- Keep literal Slice 0, Slice 1, 2A, 2B, and version-1 envelope fixtures in the
  Core checks.
- Do not serialize generated sources, offers, catalogue definitions, World
  cells, or presentation state.

4A introduces pinned World Grammar version `2`. Version `2` delegates physical
Surface/Sky generation exactly to accepted version `1` and adds the generated
two-offer Bell Study Source. New Chronicles use version `2`; version `1`
Chronicles receive an explicit save migration to version `2`, with automated
proof that every physical semantic cell remains unchanged.

Pre-versioned Chronicles remain on World Grammar version `0`; their existing
single-offer Stone-at-the-Bell behavior is retained only as a migration
compatibility path so unfinished accepted Study is not stranded. That path
must reproduce the old source, threshold, and physical generation exactly; it
does not silently grant the new Bell choice. Do not rewrite legacy terrain or
pretend the compatibility path is the 4A experience.

At the legacy Bell, `CurrentStudySource` exposes that one Stone offer and the
same source/word command may select or resume it. This remains publicly usable
for a version `0` Chronicle so an old Chronicle is not stranded, but it never
offers Bell and is exercised only as predecessor compatibility proof.

### Automated Core proof

Use red → green tracer bullets through the confirmed public seams:

1. the catalogue contains unique stable authored `Fly`, `Stone`, and `Bell`
   definitions with correct kinds and compatibility;
2. the fixture Bell cell deterministically exposes the exact ordered source
   offers and contextual reasons, while another address exposes none;
3. selecting Stone advances only Stone and switching pursuits preserves both
   word-specific amounts;
4. pause, leaving, invalid selections, and repeated selections obey the rules
   without hidden mutation;
5. save/load restores the exact selected word and partial Understanding;
6. death clears activity but preserves progress, and replacement preserves it
   with a fresh Loadout;
7. returning and finishing adds the selected word exactly once;
8. an independent choice of `Bell` advances, saves, restores, and completes
   Bell exactly once without advancing or learning Stone;
9. source queries are replay-neutral and generated source data is absent from
   JSON;
10. every literal predecessor save migrates without losing accepted state or
    changing its pinned physical World semantics;
11. every earlier Core and Visual check remains green.

Completed on 2026-07-19. The full verifier passed with:

```text
GOAL4A CORE ACCEPTANCE PASS catalogue=Fly,Stone,Bell source=Stone>Bell stone=16 bell=16 save=2
GOAL4A ACCEPTANCE PASS stoneBranch=16 bellBranch=16 finalStone=0 finalBell=16 evidence=stone-replacement,bell-save-load
PASS: Goal 4A Core and Godot Study choice, partial/completed restarts, every Goal 2 regression, Gate 3A Inspector, and Gate 3B visual acceptance verified.
```

This is automated evidence only. It does not satisfy the player UAT item in
the definition of done.

### Godot proof

- Present the Explore Starting Vector without turning it into a permanent
  class.
- At the Bell, expose one Study Source action that opens its contextual choice.
- Show both offered words, why each is plausible, source qualities, possible
  contribution, current Understanding, and learned status.
- Send the chosen source and word identifiers through a Core command.
- Show selected-word progress distinctly from the Codex.
- Preserve pause, save/load, movement, Loadout, death, replacement, and the
  accepted 20-pixel player view.
- Add a focused 4A headless journey while retaining the complete Goal 2 and
  Gate 3 regressions.
- Drive the alternate `Bell` offer through the real choice control, partial
  save/load, and completion in an independent headless fixture so both
  selectable outcomes cross the presentation seam.

Do not add a general Codex browser, catalogue search, tooltips for a large
vocabulary, drag-and-drop, a second UI-owned offer list, or presentation-owned
Study eligibility.

### UAT gate

Stop before 4B unless the player can answer without documentation:

- What am I studying?
- Why did this source offer Stone and Bell?
- Which word did I choose?
- Why did only that word advance?
- What survived save/load and the body's death?
- Where did the completed word go?

Reject 4A if the choice is cosmetic, if progress behaves like generic
experience, if the source invents semantics, if Godot decides the offers, or
if partial Understanding dies with the Incarnation.

## Forbidden until the next slice is authorized

Do not add:

- the Build or Combat Starting Vectors;
- Home, Holdings, Return Routes, residents, production, crafting, or jobs;
- health, damage, enemies, combat AI, loot, or a bestiary;
- more Study Sources, generated word meanings, random catalogue words, source
  depletion, irreversible allocation, or a general authoring system;
- new World Grammar terrain, visual art expansion, P-GEN integration, or E5;
- factions, Agents, Pressures, raids, Chronicle Records, Palimpsests, Decrees,
  or World Claims.

## Definition of 4A done

Slice 4A is complete only when:

1. its exact fixture and test seams are player-confirmed;
2. each automated Core tracer bullet passes through the public seam;
3. the focused Godot journey and every earlier regression pass;
4. literal predecessor saves migrate and a real application restart restores
   partial and completed Study;
5. the player passes the 4A UAT journey;
6. the Roadmap, this contract, and the active Handoff are reconciled;
7. work stops before 4B until separately authorized by the completed UAT.

All seven conditions were satisfied on 2026-07-19. Slice 4A is complete; that
completion did not by itself authorize Slice 4B.

## Slice 4B — A Place Called Home

### Status

**Complete and accepted on 2026-07-20.** Implementation and full automated
proof completed on 2026-07-19. Every functional UAT item passed on 2026-07-20.
A subsequently noticed Clock/Codex overlap was corrected by placing Tick and
Clock on one compact line and adding a headless four-line layout assertion.
The player confirmed the focused visual recheck looks good. This completion
does not authorize 4C.

### Player-visible hypothesis

Home should feel like a singular place the player chose and changed, not a
respawn setting, management screen, or free-travel destination. A Build
opening should make that transformation immediate, while a player who mostly
explores can leave the resulting Home as one modest expedition anchor.

### Confirmed fixed first fixture

| Concern | 4B choice |
| --- | --- |
| Build Intent | `HERE`, presented as the Build Starting Vector |
| First Verb | `Found` |
| First Horizon | A valid place at the Incarnation's current World Address |
| Invalid start | `surface (0, 0)` for seed `41337`, where the generated Stone ridge is still under water |
| Fixture site | `surface (0, 3)`, reached by three deliberate south steps, where the same generated ridge has soil support |
| Home identity | Stable `holding.home`, displayed as **THE FIRST HEARTH**, with founding Address, Chronicle tick, and Incarnation identity |
| Material change | Found gives the existing ridge Stone the durable identity **The First Hearthstone** |
| Return Route | Core-owned next physical step toward Home; the player still walks or uses an equipped traversal Verb |
| Player view | `1600 × 900` canvas with a `1020 × 740` map: actual `20 px` view `51 × 37`, retained `16 px` view `63 × 45`, and required acceptance minimum `view=50x36` |

`Found` is one new authored Verb in the Word Catalogue, with stable identity
`word.found`. Its 4B meaning is: “Establish one place as Home by giving its
matter a durable mark.” It takes no Noun in 4B and requires zero Understanding
because it is the Build First Verb. Choosing `HERE` learns `Found` and equips
intrinsic `Found` in slot one; it does not create a class or restrict later
exploration. Starting Vectors remain nonbinding: a mixed Codex independently
equips `Fly` or `Found` through the existing Loadout controls.

Using the equipped Verb through the existing hotbar is the only way to
establish Home. There is no parallel establishment command or menu bypass.
Because `Found` remains in the Codex, any later living Incarnation in that
Chronicle may re-equip it and establish Home if no earlier body did. A player
may then leave the single Hearthstone modest and spend the Chronicle exploring
without entering a Home-management mode.

### Valid site rule

Core evaluates only the current generated cell and durable Chronicle deltas:

- the cell must be in the current `surface` Stratum;
- it may not be a Landmark;
- it must contain a generated Stone feature;
- its ground may not be water;
- it may not already have a durable identity;
- the Incarnation must be alive and no Home may already exist.

The seed-`41337` start is deliberately invalid: the generated Stone ridge at
`surface (0, 0)` is under water. Three south steps reach the supported soil
ridge at `surface (0, 3)`, which is the exact valid fixture. A loose Stone
is not itself a foundation. Plain water, every sky cell, generic cloud, and the
Bell are explicit invalid fixtures. This intentionally proves one surface Home;
later support for other Strata requires its own material and route proof. The
rule reads existing World Grammar; it does not change terrain, add blockers, or
special-case either fixture address.

### Singular Home state

Save exactly one optional Home value:

- stable Holding identity `holding.home`;
- display name `The First Hearth`, holding the singular role of Home;
- founding World Address;
- founding Chronicle tick;
- founding Incarnation identity;
- material state `HearthstoneRaised`.

The first successful establishment fixes that identity and Address. Repeated
or attempted second establishments reject without mutation. Home survives
absence, save/load, application restart, Incarnation death, and replacement.
4B does not rename, relocate, abandon, damage, repair, or found another
Holding.

Founding does not move the Incarnation or change generated ground or feature.
It records Home and adds the durable identity **The First Hearthstone** to the
existing generated Stone at that address. The shared World-area and visual
paths regenerate exactly one Hearthstone subject over the unchanged underlying
terrain. The separate loose Stone remains where prior Chronicle actions left
it.

### Physical Return Route

Home's saved Address is the durable route knowledge. One read-only
`HomeContext` query owns current-site eligibility, Home presentation facts, and
the currently usable next route step:

- at Home: arrived, zero steps;
- in the same Stratum: one cardinal step toward Home, resolving X before Y;
- outside Home's surface Stratum: Home remains known but the 4B Return Route is
  currently untraversable.

The query exposes Home's destination, whether the route is currently
traversable, the next World Address when one exists, and the exact remaining
step count. It never mutates state. There is no “return” command, teleport,
automatic movement, or Godot path rule.

### Confirmed public seams

Tests and Godot use only:

- `ChronicleSimulation.Apply(...)` for choosing `HERE`, configuring/using
  `Found`, movement, death, and replacement;
- public Chronicle state for the singular saved Home;
- one read-only `ChronicleSimulation.HomeContext` snapshot;
- the public read-only Word Catalogue definition lookup for authored `Found`;
- `WorldArea.Generate(...)` for the durable Hearthstone overlay;
- `VisualGrammar.Compose(...)` and the compiled visual pack for the one shared
  Hearthstone mark;
- `ChronicleSaveCodec` for strict current version-`3` canonical persistence and
  literal version-`2`, version-`1`, and pre-envelope predecessor migration;
- the real Godot controls, render plan, visible readouts, and separate-process
  headless journeys.

Do not add a Holding repository, route service, material system interface,
generic effect registry, navigation abstraction, or Godot-owned validity rule
for one concrete fixture.

### Core rules and automated proof

Automated proof completed through the confirmed seams:

1. `HERE` grants catalogue-authored `Found`, equips it once, leaves the
   accepted `UP`/`Fly` opening unchanged, and preserves independent mixed-Codex
   `Fly`/`Found` Loadout fitting.
2. The generated start rejects as water; three south moves reach the exact
   valid supported-Stone fixture. Unsupported sky, the Bell, a dead
   Incarnation, a Chronicle without equipped `Found`, and a Chronicle with
   Home reject exactly.
3. Using intrinsic `Found` with no target establishes the one Home at the
   current valid Address without moving; passing a remote target rejects.
4. The successful action gives the existing Stone exactly one durable
   Hearthstone identity while preserving generated ground and feature. The
   separate loose Stone remains unchanged.
5. A replacement Incarnation retains `Found` in the Codex and may re-equip it
   to establish Home when an earlier body did not; an existing Home remains
   singular.
6. The Return Route reports every exact ordinary cardinal step back to Home
   and never changes the Incarnation's Address itself.
7. Home, its Hearthstone, and route knowledge survive save/load, real
   restart, death, and replacement; a replacement Loadout remains empty.
8. Strict save version `3` round-trips current state, and literal version `2`,
   version `1`, and pre-envelope fixtures migrate without gaining Home or
   losing accepted state.
9. Core area queries, route queries, visual composition, and replay remain
   deterministic and query-order neutral.
10. Every Goal 2, Slice 3, and 4A regression remains green.

The complete verifier ended with:

```text
GOAL4B ACCEPTANCE PASS home=surface:0,3 material=hearthstone route=physical view=50x36 save=3
PASS: Goal 4B Home and restart acceptance, Goal 4A Study choice, every Goal 2 regression, Gate 3A Inspector, and Gate 3B visual acceptance verified.
```

These automated markers are retained alongside the completed player UAT and
focused Clock/Codex correction recheck.

### Godot proof and playspace constraint

Enlarge the actual playspace before adding the one Home action:

- default canvas `1600 × 900`;
- `1020 × 740` map with an actual 20-pixel player view of `51 × 37` and a
  retained 16-pixel view of `63 × 45`; the required marker remains
  `view=50x36`;
- keep the control region narrower than the map and do not add a Home
  management panel;
- show Home identity/material and next route step through compact existing
  readout/guidance regions;
- mark Home on the map and render the Hearthstone through the shared
  composer/compiled-pack path;
- expose `HERE — BUILD` and intrinsic `FOUND` through the real opening and
  hotbar controls; do not add a separate Home action panel.

The headless 4B journey must drive the Build fixture through invalid-site
feedback, three physical steps to the supported ridge, Found, physical
departure/return, save, and a separate-process restore. A separate Core
replacement fixture proves a later Incarnation can retain/re-equip `Found` and
that an existing Home survives death. The Godot journey must prove the player
can leave the one Hearthstone modest and continue exploring without a
management mode.

### Player UAT gate

Stop before 4C and ask the player:

- Did `HERE` feel like a Build opening rather than a class?
- Did you choose a particular place as Home?
- Did `Found` make the existing ridge Stone read as a persistent Hearthstone
  rather than conjuring a menu-owned base?
- Could you leave and physically navigate back without teleportation?
- Did Home remain through save/restart?
- Could an explorer understand Home as an optional anchor rather than a
  management obligation?
- Is the actual 20-pixel `51 × 37` playspace, satisfying the required
  `50 × 36` minimum, materially better balanced against the menus?

Reject 4B if Home is only a saved coordinate, if the route moves the player,
if Found bypasses Chronicle matter, if Home is a Godot mode, or if the new UI
again dominates the playspace.

The exact interactive journey is recorded in the
[Goal 4B UAT sheet](GOAL-4B-UAT.md). Its functional items and focused
Clock/Codex correction recheck passed on 2026-07-20.

### UAT findings and correction

The 2026-07-20 player review found:

- the enlarged playspace looks good for the alpha, though individual detail
  feels less prominent; a later comparison may test an optional roughly 30%
  closer camera while retaining the accepted 20-pixel source pack;
- the flooded starting cell communicates the intentional invalid Home site,
  but unrestricted water traversal without an appropriate capability is a
  future terrain/traversal limitation, not permission to add that rule in 4B;
- the textual Return Route works, while a directional arrow may communicate
  the next step more directly in a later UI-bearing slice; and
- the fifth-line Clock speed was covered by the later-drawn Codex panel.

Only the concrete layout rejection was corrected in 4B. The Chronicle header
now renders `Tick: … · Clock: …` on one line, retaining every fact in four
lines above the Codex. Headless 4B and retained acceptance paths assert that
Clock remains in that compact readout. The full retained verifier passed again
on 2026-07-20. The player confirmed the corrected line looks good.

### Forbidden while 4B is active

Do not add:

- another Holding, Home relocation/naming/abandonment, storage, inventory,
  resources, recipes, crafting, construction menus, or production;
- residents, Agents, jobs, Command, factions, Pressure, raids, damage, repair,
  ruin, or Chronicle Records;
- pathfinding, blockers, roads, route history, automatic movement, respawning
  at Home, or fast travel;
- sky Home or a cross-Stratum Return Route in this first fixture;
- more Build verbs, more material transformations, a general effect system,
  or a general Holding framework;
- 4C health, enemies, combat, loot, or bestiary work;
- new World Grammar terrain, P-GEN integration, runtime compilation, E5, or
  broad visual-art expansion.

### Definition of 4B done

Slice 4B is complete only when:

1. this exact fixture and its public test seams are player-confirmed;
2. each Core and visual tracer bullet passes through those seams;
3. the Build Godot journey, replacement Core journey, and every earlier
   regression pass;
4. a real application restart restores Home, its material state, and route
   knowledge;
5. the player passes the 4B UAT;
6. the Roadmap, this contract, UAT sheet, Codemap, Development guide, and
   active Handoff are reconciled;
7. work stops before 4C until separately authorized after the completed UAT.

All seven conditions were satisfied on 2026-07-20. Slice 4B is complete; that
completion does not itself authorize Slice 4C.

## Next authorization boundary — Slice 4C

Slice 4C — A Consequential Fight is the next Roadmap candidate. No production
work is authorized yet. Before implementation begins, the active Handoff must
record separate player authorization and narrow 4C to one exact deterministic,
pausable conflict fixture, its public seams, persistent consequence, automated
proof, and player UAT. The retained camera-zoom, water-traversal, and route-arrow
notes do not automatically become 4C scope.
