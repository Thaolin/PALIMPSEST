# Active Handoff Contract

Last reconciled: 2026-07-21

This is the short-lived execution contract for the current gate. It may narrow
the active slice but may never expand or contradict [AGENTS.md](../AGENTS.md),
the [Vision](VISION.md), [glossary](../CONTEXT.md),
[Architecture](ARCHITECTURE.md), [Roadmap](ROADMAP.md), or an explicitly
authorized future slice contract.

## Current gate

**Combat grammar pressure test authorized — isolated prototype only**

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
- Goal 6 and Slice 7 remain later gates.

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
- whether Invocation Recovery simply advances on Chronicle ticks everywhere or
  needs any derived danger state for presentation and auto-pause; a resettable
  combat-mode boundary is not settled;
- how Companions join, leave, interpret combat Directives, and differ under
  taming or leadership without requiring a broad Agent framework first;
- strict migration of the accepted v5 Codex and `Fly[Stone]`/`Fly[Bell]`
  Loadouts if the successor reaches production.

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

No production feature or polish work is authorized. The active scope is the
bounded [Combat Grammar Pressure Test](COMBAT-GRAMMAR-PRESSURE-TEST.md): its
contract documentation, one in-memory pure C# combat model, one thin terminal
shell, and one-command runner. It must remain outside production projects,
saves, Godot, and the retained verification gate.

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
- P-GEN remains an optional external authoring compiler. E5 adoption is a
  separate future decision.

## Other retained non-blocking notes

- Water traversal still needs an appropriate future capability.
- A directional Return Route arrow may improve later UI.

## Do not drift into

- Goal 6 Agents, residents, relationships, Directives, Pressure, or off-camera
  history before a Goal 6 child-slice contract is approved;
- Slice 7 raid work before Goal 6 provides its accepted Pressure;
- successor grammar production code, save v6, old-save migration, a broad
  EffectPlan framework, or a large Verb/Modifier catalogue before the pressure
  test passes and a production contract is separately approved;
- a universal tick surcharge per Link, a generic mana bar, or Home progression
  that makes ordinary combat and exploration wait for settlement development;
- settled Palimpsest Awakening rules or a power-resource economy by inference;
- camera zoom, Cairn art, broad visual polish, water traversal, route arrows,
  P-GEN integration, or E5 without a separately approved gate;
- health, damage, initiative, combat statistics, bestiary, loot, inventory, or
  a generic production combat framework by extending the accepted 4C fixture;
- moving prototype HP, equipment, tick attacks, `Burn`, Companion, Target, or
  Recovery rules into production, the solution, saves, Godot, or the retained
  gate before player pressure-test acceptance and a separate contract;
- weakening strict saves, changing old grammar pins, or hiding Core decisions
  in Godot.

## Stop and hand off

When the isolated prototype runs, stop and hand the one-command journey to the
player. Automated execution is not UAT acceptance. Do not begin a production
successor, Goal 6, P-GEN E5, a save migration, or resource-system implementation
without a separately authorized contract.
