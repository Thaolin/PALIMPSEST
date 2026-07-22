# Goal 6A — A Real Fight

**Status:** accepted and closed 2026-07-21 as a prototype-quality vertical
slice; complete automated gate passed; player-reported usability and
presentation debt retained; Goal 6B unauthorized

This contract moves the passed combat-grammar pressure test into the real
Chronicle through one dangerous generated-world encounter. It also replaces
the current fixture-oriented play screen with the smallest interface capable
of producing trustworthy combat UAT.

Canonical authority remains [AGENTS.md](../AGENTS.md), the
[glossary](../CONTEXT.md), [Vision](VISION.md),
[Architecture](ARCHITECTURE.md), [Roadmap](ROADMAP.md), and active
[Handoff](HANDOFF.md). The passed
[Combat Grammar Pressure Test](COMBAT-GRAMMAR-PRESSURE-TEST.md) supplies player
evidence, not production types or balance authority.

## Product question

Can the production game deliver one legible, dangerous, real-time-with-pause
fight in which physical equipment and two competing `Burn` Expressions create
different good plans, without becoming a separate combat mode or a generic RPG
framework?

## Player promise

The player meets one Mire Brute as an actual subject on the generated surface.
Contact applies the Engagement Plan and pauses before the first hostile
Heartbeat. The player can inspect the threat, choose a Target, read what will
happen next, move, rely on a Weapon, prepare or abandon an Invocation, retreat,
and resume at Slow speed without any reflex-speed requirement.

The fight must prove all of the following together:

- the map remains the battlefield;
- Heartbeat timing is readable enough to support deliberate decisions;
- physical and language actions buy openings for one another;
- `Burn + Quickly` and `Burn + Lasting` support defensibly different plans;
- invalid Targets explain world facts rather than missing Word recipes; and
- harm, death, fire, and material change belong to the persistent Chronicle.

## Qud-inspired interface direction

The player-provided *Caves of Qud* combat screenshot is inspiration for
information hierarchy, not a template for rules, art, or exact dimensions.
Retain these useful properties:

- the map owns most of the screen;
- identity, health, time, and place fit into thin top rails;
- repeated actions and abilities remain reachable in one bottom rail;
- a narrow right rail keeps recent events readable without covering the map;
- dense information uses restrained colour and strong alignment instead of
  large panels; and
- status is visible continuously rather than hidden behind combat dialogs.

Do not copy Qud's turn sequence, UI art, glyphs, `16 × 24` cell, `80 × 25`
zone, message wording, or palette. Palimpsest retains its accepted native
`20 × 20` pack, Chronicle Heartbeats, and its own restrained blue-black, gold,
cyan, fire, and danger roles.

The first, rejected `1600 × 900` HUD used this composition:

```text
┌────────────────────────────────────────────────────────────────────────────┐
│ Incarnation / HP / Load       PAUSED · Heartbeat 184       place / light  │
├──────────────────────────────────────────────────────────┬─────────────────┤
│                                                          │ MIRE BRUTE      │
│                                                          │ HP / facts      │
│                                                          │ next action     │
│                     CHARACTER-SCALE MAP                  ├─────────────────┤
│                                                          │ FORECAST        │
│                                                          │ release / swing │
│                                                          ├─────────────────┤
│                                                          │ MESSAGE LOG     │
│                                                          │ recent results  │
├──────────────────────────────────────────────────────────┴─────────────────┤
│ effects / equipment │ Weapon │ Burn—Quickly │ other Verb slots │ controls │
└────────────────────────────────────────────────────────────────────────────┘
```

That fixed three-rail geometry is retired by the authorized correction. The
right rail remains `320 px`, but a `64 px` status/pause treatment and compact
action palette float over the map instead of subtracting permanent rows. The
map owns the complete `1280 × 900` left surface and displays the native `20 px`
P-GEN pack at crisp `2×` scale through a centered `32 × 23` query. A strong
pause veil/badge and selected-Target treatment sit above the map without owning
simulation state.

### Required visual hierarchy

1. The Incarnation and immediate threat must be legible on the map.
2. Incarnation HP, Target HP, Clock pace, and pause state must be readable
   without moving focus.
3. The selected action, its Target, Preparation, interruption risk, release,
   and Recovery must be visible before time resumes.
4. The next four meaningful events under the current state must be readable in
   a compact Heartbeat forecast. This is not a universal initiative queue.
5. Recent results remain in a transient Message Log. It is presentation
   history, not a saved Chronicle Record.
6. The complete Codex and Loadout editor are secondary surfaces. The active
   Expression, its Links, shared Load, and immediate actions remain visible in
   the compact map-overlay palette.

### Interaction rules

- Mouse and keyboard actions have equivalent visible affordances.
- Issuing a tactical command while the Clock is Slow pauses before that command
  resolves. The pending command is named in the HUD.
- While immediate danger is active, at most one Incarnation action may be
  pending. The player may replace or cancel it while paused.
- Attunement and Engagement Plan changes are available only while safe. Combat
  cannot be trivialized by swapping `Quickly` and `Lasting` between hostile
  Heartbeats.
- Preparation and Recovery use fixed tick marks and numbers, not an ambiguous
  animation-only bar.
- Target rejection stays inline in the Target rail and spends no Heartbeat.
- No modal combat screen, full-screen ability menu, floating damage-number
  cloud, decorative animation, or hidden hover-only rule may be required.

## Fixed authored fixture

These numbers are 6A fixture values. Passing UAT does not make them the final
balance curve.

### Incarnation and equipment

- Base HP: `30`.
- Weapon: **Iron Cleaver**, `5` physical damage every `2` Heartbeats while the
  Weapon stance is active, the Brute is adjacent, and the Incarnation is not
  occupied by Preparation.
- Armor: **Quilted Jack**, reducing each physical hit by `2`, never below zero.
- Accessory: **Copper Ward**, adding `4` maximum HP.
- There is exactly one Weapon, one Armor, and one Accessory. They are authored
  equipped state, not inventory items, loot, or a swappable equipment system.
- Equipment consumes no Load.

### Mire Brute

- Maximum HP: `45`.
- Material facts: living, organic, flammable, massive relative to the
  Incarnation, and not anchored.
- It becomes an immediate threat at cardinal distance `3`.
- Contact applies the configured Engagement Plan, changes the Clock to Paused,
  and exposes its Target facts before the first hostile Heartbeat.
- While threatening and not adjacent, it takes one deterministic cardinal step
  toward the Incarnation on each Heartbeat, resolving X before Y when both
  reduce distance. The generated acceptance clearing guarantees these steps
  use currently traversable open ground; 6A does not add general pathfinding.
- While adjacent, it swings for `7` physical damage every `3` Heartbeats.
- A swing that lands while Preparation still has time remaining interrupts
  that Preparation after dealing damage.
- On the Heartbeat when Preparation reaches zero, release resolves before the
  Brute's hostile action. This makes a `3`-Heartbeat preparation started
  immediately after a swing a valid but narrow window.
- It stops being an immediate threat when dead or beyond cardinal distance
  `3`. Retreat does not clear Invocation Recovery.
- Its identity, current Address, HP, and living/dead state are durable.

The Mire Brute is one authored opponent, not the first entry in a generic
bestiary. Its deterministic pursuit is the minimum proof that combat occupies
the map; it does not authorize navigation graphs, senses, factions, ecologies,
loot, spawning tables, or generalized AI.

## Successor language fixture

### Authored Words

- `Burn` is a Verb with fixed Load `1`.
- `Quickly` is a Modifier compatible with `Burn`, with fixed attachment Load
  `6`.
- `Lasting` is a Modifier compatible with `Burn`, with fixed attachment Load
  `5`.
- Available shared Load is `8`.
- 6A link capacity is `2`: one Verb and at most one Modifier. Unlocking further
  Links is deferred.
- Modifier order remains semantically irrelevant and presentation uses
  canonical catalogue order.
- A Modifier remains unique within one Expression.

Both Modifiers are available in the canonical 6A acceptance Chronicle. Their
eventual discovery pacing is outside this contract; 6A must not grow a new
Study Source merely to disguise test setup as progression. The Combat Starting
Vector grants `Burn`; the focused acceptance profile supplies both accepted
Modifiers without settling the final opening economy.

### Burn plans

| Expression | Load | Preparation | Consequence | Recovery |
| --- | ---: | ---: | --- | ---: |
| `Burn` | 1 | 3 | 4 fire damage for 3 Heartbeats | 8 |
| `Burn + Quickly` | 7 | 1 | 4 fire damage for 3 Heartbeats | 8 |
| `Burn + Lasting` | 6 | 3 | 4 fire damage for 6 Heartbeats | 8 |

`Quickly` buys a flexible release window with most of the early Load budget.
`Lasting` leaves more Load and causes a longer consequence, but its exposed
Preparation demands distance or the post-swing window. Neither may dominate
the other in the accepted fixture.

Burning damage, Weapon damage, and hostile damage resolve deterministically on
Chronicle Heartbeats. Recovery advances everywhere the Clock advances,
including after retreat, and may skip to completion only when no meaningful
interruption can occur.

## Contextual Targets

The Mire Brute and a nearby basalt World cell are both selectable actual
Targets.

- The Mire Brute preview reports its matter, scale, flammability, distance,
  current state, Preparation, consequence, and Recovery. A valid preview
  commits nothing.
- The basalt Target reports mineral, nonflammable, and anchored facts. `Burn`
  is rejected without spending time, changing Recovery, or saying which Word
  is missing.
- Target selection belongs to presentation. Eligibility, preview, rejection,
  revalidation, and resolution belong to Core.
- If actor, Target, range, or state changes during Preparation, Core revalidates
  before release and reports the factual reason for interruption or rejection.

## Engagement and Heartbeat order

The Engagement Plan contains one 6A choice: whether the Iron Cleaver Weapon
stance becomes active when immediate danger begins. Companion behavior is
absent until Goal 7 supplies an Agent with identity and agency.

Each threatening Heartbeat uses one documented stable order:

1. resolve the Incarnation's one pending movement or stance command;
2. advance and release prepared Invocations whose timer reaches zero;
3. apply ongoing consequences such as burning;
4. resolve a ready automatic Weapon strike;
5. advance or resolve the Mire Brute's movement or swing;
6. advance Recovery and derive pause, victory, death, and danger transitions.

The exact implementation may use a different internal organization, but
observable outcomes and ordering must match this contract.

Automatic pause is required on:

- first contact after the Engagement Plan is applied;
- any tactical command issued while Slow, before it resolves;
- Preparation interruption;
- Invocation release;
- Mire Brute death; and
- Incarnation death.

Do not pause on every ordinary damage tick or Weapon strike. The forecast and
Message Log must make those events readable while Slow continues.

## Persistent consequence

The first successful Burn release creates a scorched-ground delta at its
release Address. The scorch remains if the Brute moves, dies, the current body
dies, the player leaves, the Chronicle is saved and loaded, or a replacement
Incarnation returns.

Only the bounded 6A scorch delta is required. Do not build a general fire,
temperature, surface-coating, corpse, condition, or environmental simulation.

## Core Module and seam

`ChronicleSimulation` remains the external rule seam for Godot and checks.
Goal 6A may deepen its implementation around one Core-owned action-planning
Module, but must not expose a parallel `CombatSimulation` or port prototype
types into production.

One rule must own:

- Expression and shared-Load validation;
- Target facts and preview;
- eligibility and precise rejection;
- pending action, Preparation, interruption, and revalidation;
- release, ongoing consequence, and Recovery;
- Weapon, Armor, Accessory, HP, pursuit, hostile timing, and death; and
- deterministic upcoming-event snapshots used by the HUD.

The interface exposed through `ChronicleSimulation` is limited to commands and
read-only snapshots. Tests use that same interface. Godot never computes
damage, range, target validity, forecast order, Load, or timing.

The Godot presentation should likewise deepen behind one `ChronicleHud` Module
whose interface accepts the current presentation snapshot and emits existing
or new `ChronicleCommand` values. `ChronicleApp` retains lifecycle, save I/O,
input routing, and simulation coordination. Internal HUD regions do not each
need a public interface merely to make the file tree look modular.

## Persistence and migration

Goal 6A introduces strict save envelope `v6` and World Grammar `v4` for new
Chronicles.

Save v6 contains the minimum durable successor state:

- Verb-and-Modifier Codex and Loadout state;
- shared Load, fixed link capacity, and Engagement Plan;
- Incarnation HP, equipment, pending action, Preparation, and Recovery;
- Mire Brute identity, origin/current Address, HP, readiness, and outcome;
- ongoing Burn state; and
- the scorched-ground delta.

Literal v5, v4, v3, v2, v1, and pre-envelope fixtures remain accepted inputs.
Migration preserves seed, Clock, World Grammar pin, World Addresses, Home,
Bell position, first-conflict material result, Incarnation history, and known
successor Verbs.

`Stone` and `Bell` Noun knowledge, Noun Understanding, and fitted Noun Loadouts
have no honest successor meaning. Migration retires them explicitly rather
than silently remapping them to unrelated Modifiers. A fitted `Fly` slot
collapses to intrinsic `Fly`; an active Noun pursuit clears. Automated proof
must name this intentional alpha migration. Old World Grammar pins do not gain
the Mire Brute retroactively.

On Incarnation death:

- the Clock pauses;
- that body's pending action, Preparation, Recovery, and HP end with it;
- the Brute's Address, HP, and outcome remain;
- ongoing world fire and scorch remain; and
- a replacement Incarnation receives fresh fixed equipment and maximum HP.

## P-GEN and Visual Grammar scope

6A extends the in-repository P-GEN vocabulary and deliberately advances the
compatible pack contract. Required authored visuals are:

- living, wounded/emphasized, burning, and dead Mire Brute states;
- Iron Cleaver, Quilted Jack, and Copper Ward action-rail icons;
- `Burn`, `Quickly`, and `Lasting` action/link icons;
- scorched ground; and
- selected Target, danger, pending action, Preparation, and Recovery emphasis
  using explicit palette roles or overlays.

P-GEN authors appearance only. World Grammar and Chronicle state decide that
the opponent, equipment, Words, Target facts, and scorch exist. Runtime
packaging still contains only the four canonical compiled pack files, never
compiler, catalogue, workbench, or review artifacts.

The existing player actor remains accepted temporary art. Improving it is
allowed only if required for combat readability and must not become a general
character-art pass.

## Implementation sequence

This remains one vertical slice and one UAT gate. The implementation may be
developed in this order:

1. Replace the fixture-oriented play layout with the Qud-inspired HUD frame,
   driven initially by real retained snapshots and explicit empty states.
2. Implement successor Word, Loadout, Target, and action-plan rules in Core
   through `ChronicleSimulation`.
3. Add the generated Mire Brute, deterministic pursuit, physical equipment,
   HP, Preparation, Recovery, scorch, and save-v6 migration.
4. Extend P-GEN and Visual Grammar for the accepted combat vocabulary.
5. Connect the HUD to Core snapshots and commands, then complete automated and
   visual proof.

Do not create fake Godot combat data to finish the HUD first. Empty or pending
states may be presented, but every displayed rule value must ultimately come
from Core.

## Automated acceptance

`checks/verify.ps1` must retain all accepted proof and additionally establish:

Retained predecessor gameplay journeys are represented after the strict v6
migration by literal old-save, neutral-durable, versioned-generation, shared
composer, and Inspector proof. They are not rerun as a parallel v6 player UI.

- deterministic replay of the same encounter from seed and command stream;
- generated placement and stable identity of the Mire Brute and basalt Target;
- Engagement Plan application and pause before the first hostile Heartbeat;
- one pending tactical action while paused and deterministic resolution order;
- Weapon cadence, Armor mitigation, Accessory HP, pursuit, and hostile cadence;
- successful and interrupted Preparation;
- exact `Quickly` and `Lasting` Load, timing, consequence, and Recovery;
- Modifier uniqueness, order independence, link capacity, and shared Load
  rejection;
- factual invalid-Target preview with no state or time cost;
- Burn revalidation when actor, range, Target, or state changes;
- Recovery across retreat and safe skipping;
- scorch and Brute state through save/load, death, replacement, and replay;
- strict v6 round-trip plus literal v5 and every older supported migration;
- player/Inspector semantic and render parity for the new subjects;
- deterministic P-GEN compile, required mappings, pack digest, and native
  capture; and
- packaged-game exclusion of compiler, catalogue, workbench, and review files.

The Godot acceptance path must prove the actual HUD, keyboard and mouse command
paths, pause/Slow transition, Target rail, forecast, Message Log, Loadout
comparison, HP bars, combat visuals, save/load, and replacement journey. A Core
check or headless text marker alone is insufficient for player UAT.

## Player UAT

Use isolated named profiles through `play.ps1`.

### Journey A — Quickly

1. Start a fresh acceptance Chronicle and choose `AGAINST`.
2. While safe, inspect the two Burn builds. Equip `Burn + Quickly` and enable
   the opening Weapon stance.
3. Approach the Mire Brute. Confirm Engagement pauses before the first hostile
   Heartbeat and the HUD answers what happens next, when, and why.
4. Select the basalt Target and confirm factual rejection without cost or a
   missing-Word recipe.
5. Select the Mire Brute, prepare Burn, resume Slow, and use the Cleaver during
   Recovery to survive the encounter.
6. Confirm the ground remains scorched, save, close, reload the same profile,
   and confirm scorch and Brute outcome remain.

### Journey B — Lasting, death, and return

1. Start a second fresh acceptance Chronicle with `Burn + Lasting`.
2. Engage, let the Brute demonstrate its swing cadence, and begin Preparation
   outside the safe post-swing window. Confirm the hit interrupts it visibly.
3. Use physical attacks while waiting, then prepare immediately after a swing.
   Confirm release occurs before the next swing and the longer burn matters.
4. After creating scorch, deliberately allow the Incarnation to die before the
   encounter is resolved.
5. Create a replacement Incarnation and return. Confirm the Brute's wound or
   outcome and the scorched Address survived while the new body's HP and
   equipment reset correctly.
6. Finish the encounter, save/reload, and compare this plan with Journey A.

### Engineering proof ready for player review

- `checks/verify.ps1` passed on 2026-07-21 with zero build warnings or errors.
  It proved strict v6 round trip and every supported literal migration, bounded
  combat replay, player/Inspector parity, the deterministic P-GEN v2 pack, the
  exact four-file runtime artifact with compiler isolation, and actual rendered
  `1600 x 900` HUD captures for both journeys.
- Journey A is isolated under `goal6a-quickly-uat`; Journey B is isolated under
  `goal6a-lasting-return-uat`. Both are prepared as fresh profiles through
  `play.ps1` and remain independent of verifier state.
- The complete gate ends with:

  ```text
  PASS: Goal 6A real fight, strict v6 migration, rendered map-first HUD, P-GEN v2 packaging, Inspector, and retained migration/generation/composer proof verified.
  ```
- At this checkpoint this was engineering completion only; player acceptance
  had not yet been reported.

### First player UAT result — rejected

On 2026-07-21 the player rejected the first production HUD. The map and actors
were too small to read or target; the Mire Brute HP treatment overlapped; pause
feedback was too weak; Space did not toggle pause/resume; the full-width bottom
action rail consumed too much space; and the useful Message Log needed
substantially more room. The player authorized a bounded correction within Goal
6A. The correction must use a closer crisp map presentation, stronger selected
Target and pause states, a compact action overlay, a larger Target/HP block, and
a taller Message Log. This feedback supersedes the earlier `51 x 37` minimum
local-view assumption where necessary for legibility; physical map dominance
remains required. At that checkpoint Goal 6A remained unaccepted and Goal 6B
remained unauthorized.

### Corrected HUD candidate

The bounded correction passed the complete `checks/verify.ps1` gate on
2026-07-21 with zero build warnings or errors. Both actual rendered journeys
now prove the crisp `2×` map, large selected Target/HP block, labeled decision
forecast, pause veil and `SPACE TO RESUME` badge, Space pause/resume toggle,
compact six-action plus WASD overlay, and enlarged Message Log. Fresh second
review profiles were prepared as `goal6a-quickly-uat-r2` and
`goal6a-lasting-return-uat-r2`.

A hands-on combat pass then exposed wrapped forecast entries spilling into the
Message Log during a busy melee. Forecast rows are now compact single-line
event summaries and the enlarged log retains the five newest full results,
preventing cross-panel and bottom-edge overflow while preserving the latest
combat feedback. The complete gate passed again. The fresh visible Quickly
review profile is `goal6a-quickly-uat-r6`; the untouched Lasting return profile
remains `goal6a-lasting-return-uat-r2`. Player acceptance was still pending at
this checkpoint.

### Revised bounded presentation pass

On 2026-07-21 the player supplied a contract-aware revised HUD review and
directed Codex to act on it. The authorized pass remains presentation-only:
non-occluding pause feedback, compositor-only actor separation, restrained HP
meters with integrated values, stronger consequence hierarchy, semantic color
roles, top-rail rebalance, and one consistent framing motif. The `320 px` rail,
`2×` map, six-action plus WASD overlay, fixed section model, Space behavior,
simulation rules, and existing P-GEN assets remain unchanged.

Collapsible cards, dock paging, Companions, inspection lenses, log filters,
tooltip frameworks, new gameplay information, commissioned fonts, and new
animation systems remain explicitly deferred. This pass does not constitute
player acceptance.

The revised pass now renders pause as a lit top-rail plate, cools the terrain
without dimming living actors, frames the paused map without covering cells,
and gives the Incarnation and Mire Brute compositor keylines, shadows, semantic
rings, and selected-target brackets. The fixed `320 px` rail now uses real
Target, Consequence, Forecast, and Message Log sections; HP values live inside
restrained meters; Consequence is the visual center; and player, hostile, time,
and neutral information have distinct color roles. The complete
`checks/verify.ps1` gate passed again with zero warnings or errors. The fresh
visible profile is `goal6a-quickly-uat-r7`.

### Final player UAT result — qualified pass

On 2026-07-21 the player reported: “Overall, it's a pass but only as a
prototype.” Goal 6A is therefore accepted as proof of the bounded production
vertical slice, not as final combat UX, final equipment taxonomy, or a graphics
1.0 standard. The following observed debt remains explicit:

- movement queued around the engagement pause does not visibly catch up, and
  the need to resume Heartbeats is not clear enough;
- the pause plate clips the Surface/location text;
- `Quickly Burn` and `Lasting Burn` read as unusable because safe-only
  Attunement availability is not explained where the controls are disabled;
- the Iron Cleaver can resolve only while adjacent in Core, but its feedback
  read as a ranged Hatchet strike to the player; later weapon work must preserve
  explicit melee adjacency and introduce range only through an authored weapon
  category;
- the prediction/ESP log is valuable, while damage provenance such as Quilted
  Jack's fixed two-point reduction is too opaque in the Message Log; and
- the Incarnation ring is an acceptable placeholder until a separately scoped
  graphics 1.0 overhaul.

These notes do not authorize follow-up implementation, a ranged-weapon
framework, or Goal 6B.

## Acceptance gate

Goal 6A passes only when the player reports all of the following:

- the production interaction retains the pressure test's deliberate
  *Baldur's Gate*-like real-time-with-pause feel;
- the HUD makes danger, timing, Target facts, Preparation, interruption,
  Recovery, and HP predictable without reading documentation;
- the map remains the place where combat happens;
- Weapon use and Burn both remain relevant;
- `Quickly` and `Lasting` are meaningfully different and neither is an obvious
  universal answer;
- the persistent result and replacement-body return feel like one continuing
  Chronicle; and
- the player wants another opponent or another Expression after finishing the
  slice.

The player's qualified prototype pass closes this gate. It does not promote
the fixture values or presentation to final product standards.

## Explicit exclusions

- Companion or Agent production;
- inventory, loot, equipment comparison, crafting, vendors, or drops;
- healing items, resting economy, damage types, critical hits, levels, XP, or
  a general statistics framework;
- broad bestiary, spawning, ecology, faction, perception, or generalized AI;
- general pathfinding, terrain collision, water traversal, cover, line of
  sight, or ranged-weapon framework;
- more than one hostile opponent;
- broad conditions, fire spread, temperature, corpse, or surface simulation;
- a general EffectPlan hierarchy or freeform Modifier programming;
- more Verbs, Modifiers, Links, or Load progression than the fixture requires;
- Load Sources, power resources, building, raids, or any Goal 6B economy;
- Companions represented as combat slots before Goal 7 Agents;
- Area/Passage production, dungeons, camera-density changes, or rectangular
  runtime cells; and
- actor-art, menu, settings, onboarding, or design-system work not required to
  make this exact fight legible.

## Stop condition

Stop after automated proof and player UAT are ready. Record the result in this
contract and `HANDOFF.md`, reconcile the Roadmap, and do not begin Goal 6B,
Agents, Areas, a second opponent, vocabulary expansion, or balance progression
without a separately authorized contract.
