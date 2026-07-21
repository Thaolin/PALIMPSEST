# Active Handoff Contract

Last reconciled: 2026-07-21

This is the short-lived execution contract for the current gate. It may narrow
the active slice but may never expand or contradict [AGENTS.md](../AGENTS.md),
the [Vision](VISION.md), [glossary](../CONTEXT.md),
[Architecture](ARCHITECTURE.md), [Roadmap](ROADMAP.md), or an explicitly
authorized future slice contract.

## Current gate

**P-GEN E5 implemented — player visual UAT pending**

- Goal 2, Slice 3, and Goal 4 are complete and accepted.
- Slice 4C's implementation and complete retained automated gate passed on
  2026-07-20.
- The player passed focused 4C UAT on 2026-07-20 with: “All pass.”
- The completed [Goal 4 contract](archive/contracts/GOAL-4-THREE-OPENINGS.md)
  and [Goal 4C UAT evidence](archive/uat/GOAL-4C-UAT.md) are archived.
- The Roadmap now inserts
  [Slice 5 — A Word Multiplies](archive/contracts/SLICE-5-A-WORD-MULTIPLIES.md)
  before Agents.
- The player authorized Slice 5 production on 2026-07-20.
- Shared fitted-Fly resolution, `Fly[Bell]`, durable Bell/source movement,
  strict save v5, literal v4 migration, and the complete retained automated
  gate passed on 2026-07-20.
- The player reported “Full UAT accept” on 2026-07-20. The completed contract
  and [accepted UAT evidence](archive/uat/SLICE-5-UAT.md) are archived.
- The player's later post-UAT assessment was that collectible `Verb[Noun]`
  composition changed the selected subject without creating enough fun. Slice
  5 remains accepted engineering and migration evidence, not the future depth
  axis.
- The player settled the successor direction: authored Verbs plus linked
  Modifiers act on contextual Chronicle Targets; Verb slots, link capacity, and
  shared Load constrain the active build.
- [ADR 0003](adr/0003-use-verbs-linked-modifiers-and-world-targets.md), the
  glossary, Vision, Architecture, and Roadmap now record that direction.
- The accepted v5 predecessor, Slice 5 evidence, archives, and reconciled
  direction were verified through the complete retained gate and committed on
  `main` as `bd023c6` before prototype work began.
- The player authorized the isolated
  [Combat Grammar Pressure Test](COMBAT-GRAMMAR-PRESSURE-TEST.md) on
  2026-07-21. It may create only a throwaway C# logic model and terminal shell
  outside production projects and saves.
- The first player pressure-test session reported that the system “feels pretty
  great” and “feels like a proper RPG combat cycle.” This passes the combat
  model hypothesis. Before closing the prototype, the player requested one
  interaction refinement: Engagement applies selected opening Weapon and
  Companion behaviors, then pauses before a visible Slow heartbeat begins. The
  refinement was implemented in the isolated prototype.
- The player passed the refined interaction on 2026-07-21 with: “Yeah this
  feels great too. Like Baldur's Gate!” The pressure test is closed. Heartbeats,
  the Engagement Plan, opening pause, and pause-first tactical input are now
  settled successor direction, not authorization to port fixture rules.
- The player then chose to retain the accepted Slice 0–3 map generation,
  graphics, palette, Clock, and runtime seams, and redesign the RPG gameplay
  from that foundation rather than restart the project or preserve predecessor
  gameplay by inertia.
- The [RPG Successor Rebuild Direction](RPG-SUCCESSOR-REBUILD-DIRECTION.md)
  sequences Goal 6 as `A Real Fight` followed by `Power Comes Home`; neither
  child slice is authorized. `Home Has People` is now Goal 7 and the first Raid
  is Slice 8.
- The player corrected P-GEN's status: it is built, ready, and required, not an
  optional candidate. [ADR 0004](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md)
  records P-GEN as the canonical authoring-time visual pipeline. The next gate
  is Palimpsest-owned reader integration; Goal 6A follows only after it passes.
- On 2026-07-21 the player said “make it so,” authorizing the bounded
  [P-GEN E5 integration contract](P-GEN-E5-INTEGRATION.md). P-GEN's E4.5
  verifier passed and its clean authoring baseline is pinned at
  `6d9c749e52e2a5bef99b6f27f23d2163592e37f8`.
- P-GEN then added the exact four accepted v5 mappings missing from that
  baseline, passed its complete verifier with zero warnings or errors, and was
  repinned cleanly at `812bb75c70fc22d78219759b70a9338a26b3ee8b` with canonical
  aggregate `sha256:245cb53df47d7f9866071d75359d272cbd53c56010e3d3f4921d12cf72eaf707`.
- Palimpsest now owns the strict canonical reader and shared packaged loader.
  P-GEN is the default 20-pixel pack in both player and Inspector; manual 20 px
  is explicit comparison only and manual 16 px remains the retained reference.
- The complete retained gate passed on 2026-07-21 with zero build warnings or
  errors. It proved exact vocabulary, precise negative reader failures,
  P-GEN/manual semantic composer parity, deterministic native captures, four
  packaged files with compiler/catalogue code absent, all player/Inspector
  paths, and every retained v5 journey. The final marker was:

  ```text
  PASS: P-GEN E5 reader/default packaging plus Slice 5, Goal 4, Goal 2, Gate 3A, and Gate 3B verified.
  ```
- The [E5 UAT sheet](P-GEN-E5-UAT.md) presents native P-GEN/manual comparison
  captures and the focused interactive journey. A player visual result is the
  only remaining E5 acceptance condition.

## Settled successor direction

- A Verb says what magic occurs; a Modifier changes how it acts; a Target is an
  actual subject or place in the Chronicle rather than a collectible Word.
- Target facts such as matter, mass, scale, resistance, identity, and agency
  constrain what an Expression can affect.
- One active Verb plus five linked Modifiers is a six-link Expression.
- Shared Load creates the breadth-versus-depth build decision across the whole
  Loadout.
- Modifiers are reusable across different Verbs, unique within one Expression,
  and order-independent. Every attachment pays fixed authored Load independent
  of the Target.
- Targets expose constraints rather than exact Word recipes. Invalid Targets
  remain inspectable with factual preview before an Invocation commits.
- Link count does not automatically add waiting. Ordinary fighting and
  exploration remain responsive; genuinely massive or durable effects may
  consume substantial Chronicle time, and `Quickly`-like Modifiers may transfer
  that cost into material power, risk, notice, instability, or collateral.
- Combat is broader than Invocation: positioning, physical actions, equipment,
  terrain, autonomous Companions, and Expressions create openings for one
  another in the same Chronicle state and time. Actions may use Preparation,
  delayed resolution, Recovery, or ritual commitment, but every delay must
  create decisions rather than idle waiting.
- The Chronicle Clock advances through fixed Heartbeats. Immediate danger is
  readable at Slow or paused. Engagement applies the selected opening Weapon
  and Companion behaviors, then pauses before the first hostile Heartbeat;
  tactical input during Slow pauses before it resolves.
- Companions remain autonomous Agents. Taming and leadership may improve bond,
  learned behavior, coordination, and willingness without creating per-tick
  unit control. Companions and equipment do not consume Load merely by being
  present.
- The first candidate combat shell has one Weapon, one Armor, and one Accessory
  slot, visible HP bars, tick-timed weapon attacks, and `Burn` as its first
  combat Invocation. These are pressure-test inputs, not implemented v5 rules.
- Some discoveries may grant one Load, but most long-term capacity comes from
  vulnerable Load Sources built at Home. For the first pass, losing one affects
  the next Attunement, including a replacement Incarnation, rather than the
  current expedition. Destroyed Sources can be rebuilt; lesser damage and repair
  thresholds remain unsettled.
- Awakening a Power Word is a candidate Palimpsest offer, not a settled
  affordance or required ending.

## Retained foundation and redesign surface

Retain the accepted foundation through Slice 3:

- the engine-independent C# Chronicle and thin Godot adapter;
- deterministic Heartbeats, pause and speed, seed, World Address, Strata,
  bounded versioned World Grammar, generated Landmarks, and persistent deltas;
- the developer World Atlas Inspector and shared semantic snapshot path; and
- the accepted 20-pixel map scale, restrained palette, compiled-pack seam,
  deterministic Visual Grammar, and Godot rendering adapters.

Redesign predecessor gameplay on top of that foundation through one migration,
not a parallel runtime. Collectible Nouns, fitted `Fly[Stone]` / `Fly[Bell]`,
the tiny Bell Study fixture, the one-exchange Cairn fight, current onboarding,
and fixture-specific UI are evidence rather than successor constraints. Home's
identity and role remain settled, but its production mechanics may be replaced.

P-GEN is the required authoring-time visual compiler. The accepted manual pack
remains a golden comparison fixture until E5 makes the P-GEN artifact the
default authored pack. Current required mappings form a versioned baseline;
Goal 6A and 6B add their new visual subjects through P-GEN rather than waiting
for one final vocabulary freeze.

## Open decisions before production

- the smallest Verbs, Modifiers, Targets, and changing situation that can prove
  the grammar fun;
- Modifier applicability and conflict rules without a freeform programming
  language;
- exact Verb-slot, link-capacity, Word-cost, and Load-progression curves;
- the materials, structures, repair rules, and limits for built Load Sources;
- whether death temporarily locks otherwise surviving Load and how that capacity
  is recovered;
- whether any runtime power resource is needed before world-scale play;
- tactical, committed, and ritual timing bands and interruption rules;
- the smallest physical action set, positioning rules, hostile intent,
  condition or injury model, and exact Preparation and Recovery behavior;
- exact weapon cadence and commands, damage and Armor arithmetic, Accessory
  effects, healing, HP persistence, and `Burn` behavior;
- exact additional auto-pause triggers and available Chronicle Clock speeds;
- whether Invocation Recovery simply advances on Heartbeats everywhere or
  needs any derived danger state for presentation;
- how Companions join, leave, interpret combat Directives, and differ under
  taming or leadership without requiring a broad Agent framework first;
- strict migration of the accepted v5 Codex and `Fly[Stone]`/`Fly[Bell]`
  Loadouts if the successor reaches production.

After P-GEN E5 passes, Goal 6A's contract must narrow these decisions to one
dangerous opponent, two competing `Burn` builds, one Weapon, Armor, and
Accessory, the accepted Heartbeat interaction, one persistent material result,
and the smallest strict successor migration. Goal 6B separately owns one
resource and one Load Source; it may not be pulled into 6A as infrastructure
work.

## Accepted Goal 4 result

The accepted Chronicle now offers three nonbinding Starting Vectors:

- `AGAINST — COMBAT` grants authored intrinsic Verb `Smash` and leads to the
  deterministic Riven Cairn/River-Ward confrontation;
- `UP — EXPLORE` grants `Fly` and leads to the sky Landmark and generated Study
  Source;
- `HERE — BUILD` grants `Found` and can establish the singular persistent
  Home and its physical Return Route.

All three use the same Codex, Loadout, fixed Chronicle Clock, World Grammar,
strict save, and eventual Incarnations. The 4C success path leaves the durable
Shattered Cairn; the no-action path ends one body while preserving the
Chronicle.

## UAT reconciliation

The player accepted the alpha and retained two non-blocking visual notes:

- the local playspace still needs a separately scoped closer-zoom comparison;
- the Riven Cairn was extremely difficult to discern and needs stronger visual
  hierarchy in a later visual pass.

The player also observed that movement remains available while paused. This is
intentional and accepted: pause freezes Chronicle ticks, autonomous object
state, and time-driven presentation. Deliberate commands remain responsive so
the player can inspect, configure, prepare an action, or retreat.

A later review correctly noted that `UP` and `HERE` bodies also meet the Cairn
without knowing `Smash`; if they resume time there, the body ends. This is the
accepted shared-world danger and not an Intent-gating defect: all Starting
Vectors encounter the same Chronicle, and retreat while paused is their
available choice. An Intent check would create private Combat content. Clearer
warning presentation remains a possible later UX task, outside Slice 5.

These observations do not reopen Slice 4C and do not authorize production
changes.

Slice 5 player UAT accepted the prediction, choice, fitted Expression, durable
result, and restart journey in full. No blocking or non-blocking issue was
reported.

## Current permitted scope

No further production work is authorized while the player performs the
[P-GEN E5 visual UAT](P-GEN-E5-UAT.md). A narrowly reported E5 visual blocker
may be corrected inside the existing contract; accepting or rejecting the
default requires documentation reconciliation. The completed combat prototype
remains evidence only. No gameplay, save, World Grammar, or successor grammar
work is authorized.

## Retained accepted baseline

- `checks/verify.ps1` passed the complete retained gate on 2026-07-20 with zero
  build warnings or errors. Its final markers were:

  ```text
  SLICE5 CORE ACCEPTANCE PASS expression=Fly[Bell] durable=Bell+source save=5 migration=4
  SLICE5 SAVE READY bell=surface:0,-4 loadout=Fly[Bell] save=5
  SLICE5 RESTART ACCEPTANCE PASS bell=surface:0,-4 source=attached death=confirmed
  PASS: Slice 5 Fly[Bell] composition and restart, Goal 4C conflict, Goal 4B Home, Goal 4A Study choice, Goal 2, Gate 3A, and Gate 3B verified.
  ```
- The same complete retained gate passed again on 2026-07-21 immediately before
  baseline commit `bd023c6`, with zero build warnings or errors and the same
  final acceptance marker.
- The accepted player baseline remains the 20-pixel pack on the `1600 × 900`
  canvas with a `1020 × 740`, `51 × 37` map.
- New Chronicles use strict save envelope v5 and World Grammar v3. Literal
  v4/v3/v2/v1/pre-envelope compatibility remains verified, and old grammar
  pins `0`, `1`, and `2` do not gain the Cairn retroactively.
- P-GEN is the completed required external authoring compiler. Its Palimpsest
  E5 reader/conformance integration is the next separately gated step.

## Other retained non-blocking notes

- Water traversal still needs an appropriate future capability.
- A directional Return Route arrow may improve later UI.

## Do not drift into

- Goal 7 Agents, residents, relationships, Directives, Pressure, or off-camera
  history before Goal 6 passes and a Goal 7 child-slice contract is approved;
- Slice 8 raid work before Goal 7 provides its accepted Pressure;
- successor grammar production code, save v6, old-save migration, a broad
  EffectPlan framework, or a large Verb/Modifier catalogue before a Goal 6A
  production contract is separately approved;
- a universal tick surcharge per Link, a generic mana bar, or Home progression
  that makes ordinary combat and exploration wait for settlement development;
- settled Palimpsest Awakening rules or a power-resource economy by inference;
- camera zoom, Cairn art, broad visual polish, water traversal, route arrows,
  visual work outside the bounded P-GEN E5 contract;
- health, damage, initiative, combat statistics, bestiary, loot, inventory, or
  a generic production combat framework by extending the accepted 4C fixture;
- moving prototype HP, equipment, tick attacks, `Burn`, Companion, Target, or
  Recovery types directly into production, the solution, saves, Godot, or the
  retained gate; Goal 6A must implement its own bounded Core rules through the
  accepted simulation seam;
- weakening strict saves, changing old grammar pins, or hiding Core decisions
  in Godot.

## Stop and hand off

Stop for the player's E5 visual result. Goal 6A remains downstream of accepted
E5 and requires its own later contract; passing 6A will not authorize 6B.
