# Goal 6B — Power Comes Home

**Status:** complete and player-accepted on 2026-07-22 after four bounded
corrections; the neutral testing start, physical Burn Primer acquisition, and
complete retained automated gate are green

This contract connects the accepted Goal 6A RPG spine to Home through one
generated piece of matter, one physical return journey, and one vulnerable Load
Source. It defines and now authorizes only the smallest complete production
slice that can prove the loop.

Canonical authority remains [AGENTS.md](../AGENTS.md), the
[glossary](../CONTEXT.md), [Vision](VISION.md),
[Architecture](ARCHITECTURE.md), [Roadmap](ROADMAP.md), and active
[Handoff](HANDOFF.md). The accepted
[Goal 6A contract](GOAL-6A-A-REAL-FIGHT.md) supplies the current successor
runtime, combat fixture, and explicit prototype debt. This contract deepens
that one runtime through `ChronicleSimulation`; it does not create a second
simulation, migration path, construction framework, or economy.

## Product question

Can one identifiable piece of generated matter travel physically from a
persistent expedition origin to Home, become vulnerable infrastructure, and
make a previously impossible Loadout possible—while the player can always tell
what will happen, when, what can interrupt it, and what the choice prevents?

## Player promise

The player finds a **Resonant Lode** embedded in a generated **Singing Seam**.
The Seam remains at its World Address after extraction, and the Lode retains
that origin through carrying, construction, destruction, death, save/load, and
rebuilding. The Incarnation can carry exactly that one large object in the
world, visibly and with explicit action limits; Goal 6B does not introduce the
future robust inventory Module.

At Home, the Lode becomes the core of one **Hearth Resonator**. The structure
adds four Load to the next Attunement and makes `Burn + Quickly + Lasting`
possible under a twelve-Load ceiling. Damaging or dismantling the Resonator
removes no current Expression. Destruction changes only the capacity offered
at the next Attunement, including the mandatory fresh Attunement of a
replacement Incarnation. Rebuilding the same physical Lode restores the future
capacity.

The complete loop is:

```text
find the Singing Seam -> extract one Resonant Lode -> carry it home
-> build one Hearth Resonator -> Attune a 12-Load Expression
-> damage and destroy the Resonator -> keep the current Expression
-> lose or change the body -> face the 8-Load limit
-> rebuild -> regain 12 Load at the next Attunement
```

The fixture names and numbers in this document are approved contract decisions
for this slice, not final balance values or permission for a broad taxonomy.

## Non-negotiable presentation contract

Mechanically correct but unclear behavior fails Goal 6B. Before every
meaningful commitment, the map and supporting text must answer:

1. What happens next?
2. When will it happen?
3. What can interrupt it?
4. What will this action prevent me from doing?

The answer may not live only in a transient Message Log, tooltip, raw number,
debug label, or Inspector. A decision card must show the four answers before
commitment; the pending state must remain visible on the map and in text while
paused; and the result must connect the same subject identity, World Address,
cause, cost, and persistent consequence.

### Required map truth

At native gameplay scale, without opening another screen, the map must visibly
distinguish:

- the intact Singing Seam and its extracted, empty origin;
- the loose Resonant Lode at any current World Address;
- the Incarnation carrying the Lode, using a dedicated carrier overlay that
  cannot be confused with a floor object or status ring;
- the singular Home Hearthstone and the one eligible Resonator site;
- the Lode committed to a foundation and each construction step;
- the intact, damaged, destroyed, and rebuilding Hearth Resonator;
- the physical Lode remaining in the destroyed Source; and
- the exact cell affected by extraction, construction, dismantling, damage,
  destruction, dropping, or rebuilding.

The carried Lode has one physical state, not a duplicate map object plus an
inventory count. Home and the Source remain ordinary Chronicle subjects in the
same character-scale grid as the expedition and fight.

### Required supporting text

The persistent HUD surface must explain, in plain language:

- the Lode's generated origin and current holder or Address;
- extraction, construction, dismantling, and rebuilding progress in remaining
  Heartbeats, including that paused Heartbeats do not advance it;
- the one-object carrying limit and every action disabled by carrying;
- the action currently pending, its interruption conditions, its committed
  matter, and the explicit Cancel route;
- construction and rebuilding costs without implying a recipe catalogue;
- the current Loadout's used Load and capacity at its last Attunement;
- the next Attunement capacity as a named breakdown, such as
  `8 inherent + 4 intact Hearth Resonator = 12`;
- why Attunement is disabled and the exact condition that will enable it;
- why a requested Loadout does not fit, without changing the current one;
- whether Source damage still contributes capacity and when destruction takes
  effect; and
- what persists through departure, save/load, death, replacement, destruction,
  and rebuilding.

The Source may show bounded integrity or progress values, but text must name
their causal meaning. `DAMAGED — still contributes +4; one more dismantling
Heartbeat destroys it` passes. An unexplained `1/2`, red flash, or `+4` does
not.

### Decision feedback matrix

| Decision | What happens next and when | What interrupts it | What it prevents | Required visible result |
| --- | --- | --- | --- | --- |
| Extract | Two active Heartbeats unseat the Lode; while paused it remains `0/2` | explicit Cancel, hostile damage, or Incarnation death | movement, Weapon actions, Invocation, Attunement, and another commitment | Seam and Lode pulse together; progress and affected cell remain named |
| Lift | Immediate while on the Lode's cell or cardinally adjacent and unoccupied | invalid carrier or Lode state rejects without mutation | carrying a second object; while carried, Weapon stance, Burn Preparation, Fly, and Attunement are unavailable | loose Lode disappears from its cell and appears on the Incarnation carrier overlay in the same frame |
| Carry/move | The existing pending movement resolves on the next active Heartbeat with the Lode attached | normal movement rejection or Incarnation death; death drops the Lode | Weapon stance, Burn Preparation, Fly, and Attunement until Set Down | forecast names carrier movement; map moves actor and Lode together |
| Set Down | Immediate onto the Incarnation's current valid cell | occupied or invalid destination rejects without mutation | nothing after success | carrier overlay clears and one loose Lode appears at that cell |
| Build | Three active Heartbeats turn the Lode into one Resonator at the highlighted Home site | Cancel, hostile damage, or death; progress remains materially present | movement, combat actions, Invocation, Attunement, and other Home actions while active | foundation progresses `0/3` to `3/3`; completion names future capacity, not an immediate Loadout change |
| Attune | Immediate while safe; a successful command replaces the current Loadout under the capacity available now | threat, carrying, or another commitment makes it unavailable with a reason | the previous Loadout is replaced only on success | map highlights the Source contribution and text records used Load, capacity, and Attunement tick |
| Dismantle | First of two Heartbeats leaves the Source damaged; the second destroys it and exposes the same Lode | Cancel, hostile damage, or death | movement, combat actions, Invocation, Attunement, and rebuilding while active | exact Source cell changes intact → damaged → destroyed; next capacity changes only on destruction |
| Rebuild | Three active Heartbeats raise the same Resonator around the exposed Lode | Cancel, hostile damage, or death | movement, combat actions, Invocation, Attunement, and other Home actions while active | destroyed → rebuilding `0/3..3/3` → intact; completion restores only next-Attunement capacity |

No paused command may vanish into an invisible queue. If a command requires a
Heartbeat, its map emphasis and decision card must say `PAUSED — progress waits
for SPACE` and show the exact next resolution tick.

## Proposed fixed authored fixture

These are the approved bounded Goal 6B fixture values, frozen for this slice
only.

### Generated matter and origin

- Acceptance seed remains `41_337`.
- New Chronicles use World Grammar `v5`.
- WG5 generates one Singing Seam on the Surface. For the acceptance seed its
  exact origin is `surface:8,3`, with stable identity
  `place.singing-seam.41337`.
- The embedded Lode has stable identity `resource.resonant-lode.41337` and
  immutable `OriginAddress = surface:8,3`.
- Every bounded Goal 6B interaction accepts the subject's own cell or one
  cardinally adjacent cell. A non-blocking book, loose Lode, foundation, or
  destroyed Source does not become uninteractable when the Incarnation stands
  directly over it.
- Extraction takes `2` Heartbeats while on the Seam's cell or cardinally
  adjacent. The embedded Seam currently blocks entry, so the accepted fixture
  normally begins from an adjacent cell.
- After extraction, the empty Seam remains generated and inspectable. The Lode
  becomes one durable movable subject whose state is exactly one of embedded,
  loose at an Address, carried by one Incarnation, committed to construction,
  or installed in the Resonator.
- The acceptance Home remains The First Hearth at `surface:0,3`. The sole
  eligible fixture site is the supported cell east of its Hearthstone at
  `surface:1,3`. No general placement or building-site search is introduced.

### Physical carrying

- The Incarnation may carry exactly one Resonant Lode and no other Goal 6B
  carryable subject exists.
- Lift and Set Down are immediate deliberate commands and spend no Heartbeat.
- Ordinary movement keeps its existing pending-Heartbeat behavior. The Lode
  shares the carrier's Address and never moves independently while carried.
- Carrying occupies both hands and focused Attunement: Iron Cleaver stance and
  strikes, Burn Preparation, Fly, and Attunement are disabled. Inspection,
  pause/speed controls, movement, Set Down, and the eligible Build action remain
  available.
- An immediate threat still pauses as in Goal 6A. The interface says to Set
  Down the Lode before fighting; it does not silently clear the carrier state.
- If the carrier dies, the Lode becomes loose at the death Address before the
  replacement is created. The replacement receives no remote copy and must
  retrieve it physically.

This is one large-object state, not the project's eventual inventory,
encumbrance, equipment weight, loot, a carry-stat system, or permission for
additional carryable types. A robust inventory is planned future product work;
this fixture neither implements nor constrains its eventual interface.

### Hearth Resonator

- Building requires an existing Home, the carried Lode, the exact eligible
  site under or beside the Incarnation, safety, and no other pending
  commitment.
- Initial construction takes `3` Heartbeats. Cancel or interruption preserves
  completed construction steps and the Lode at the site; resuming continues the
  same structure rather than refunding an abstract material count.
- The intact Resonator contributes `+4` Load to the next Attunement.
- Controlled Dismantle is the sole Goal 6B vulnerability proof. It takes `2`
  Heartbeats. The first removes the outer brace and leaves a visibly damaged,
  still-functional Source. The second destroys the Source, contributes `0`,
  and leaves the same Lode loose at the Source Address.
- A damaged Resonator still contributes `+4`. This prevents a hidden threshold:
  capacity changes only when the map and text report `DESTROYED`.
- Rebuilding a destroyed Source at the same eligible site takes `3`
  Heartbeats and the same Lode. No additional material, repair resource, worker,
  recipe, stockpile, or production step exists.
- Rebuilding a merely damaged Source is outside the slice. The player may
  complete dismantling and rebuild; this contract does not define a general
  repair command or damage arithmetic.

### Load and the previously impossible Loadout

- Inherent Load capacity remains `8`.
- Active Verb slots remain `1`.
- The 6B acceptance Loadout has a fixture Link capacity of `3`: one Verb and
  two unique Modifiers. This is a fixed prerequisite, not capacity granted by
  the Source and not a general Link-progression system.
- Existing costs remain unchanged: `Burn = 1`, `Quickly = 6`, and
  `Lasting = 5`.
- `Burn + Quickly + Lasting` therefore uses `12` Load. It retains one-Heartbeat
  Preparation from Quickly, six-Heartbeat consequence from Lasting, and
  eight-Heartbeat Recovery from Burn. Modifier order remains irrelevant and
  canonical presentation order remains catalogue order.
- At base capacity the request is rejected without mutation:
  `Needs 12 Load; next Attunement capacity is 8. An intact Hearth Resonator at
  Home would add 4.`
- An intact Source makes the same request valid at the next Attunement. Source
  completion alone does not change the active Expression.

This one combined Expression is the only new Loadout outcome. Goal 6B adds no
Word, Study Source, Verb slot, extra Modifier, additional combat Target, or
general high-Link rule family.

## Current versus next-Attunement capacity

Core must expose, and Godot must present, three distinct facts:

1. **Current Loadout:** the exact active Expressions and used Load.
2. **Capacity at last Attunement:** the ceiling under which that current
   Loadout was successfully created.
3. **Next Attunement capacity:** capacity derived now from inherent Load plus
   the intact Source.

Attunement means creating or changing any part of the Loadout. A successful
Attunement validates the whole proposed Loadout against the current derived
capacity, replaces the old Loadout atomically, and records its capacity and
tick. A rejected Attunement changes nothing.

The required transitions are:

| Chronicle state | Current Loadout display | Next Attunement display |
| --- | --- | --- |
| Before Source | existing expression under its recorded ceiling | `8 inherent` |
| Source completed, before Attunement | unchanged | `12 = 8 inherent + 4 Hearth Resonator` |
| Combined Expression Attuned | `12 / 12 at Heartbeat N` | `12 = 8 + 4` |
| Source damaged | current `12 / 12` remains | `12`; damaged Source still contributes |
| Source destroyed/dismantled | current `12 / 12` remains active | `8`; next change or replacement cannot retain 12 |
| Source rebuilt, before another Attunement | current Loadout unchanged | `12 = 8 + 4 restored` |

Source loss never triggers remote cancellation, removal of a Modifier, a
Recovery change, or an invalid saved state. The already-attuned combined Burn
remains usable until the Incarnation successfully Attunes again or dies.

Replacement is a mandatory fresh Attunement. If the Resonator is destroyed,
the replacement may select only a Loadout of at most `8`; the `12`-Load
Expression remains visible but disabled with the named Source-loss reason. If
the Resonator is intact or rebuilt, the replacement may Attune it at `12`.

## Chronicle-time commitments

Extraction, construction, dismantling, and rebuilding are one bounded family of
physical commitments owned by Core. At most one may be active. Each snapshot
must expose actor, subject, affected Address, completed and remaining
Heartbeats, next transition, interruption conditions, disabled alternatives,
and whether paused time is preventing progress.

While one is active, inspection and pause/speed controls remain responsive.
The player may Cancel explicitly. Movement, Weapon actions, Invocations,
Attunement, Lift/Set Down, and another commitment are disabled with a Core-owned
reason. Hostile damage or Incarnation death interrupts the commitment; material
progress already represented on the map persists. Safe waiting may skip only
to the next construction-state change and must still present that change.

This is not a universal job, crafting-duration, or construction scheduler.

## Core Module and seam

`ChronicleSimulation` remains the sole external rule seam used by Godot and
Core checks. Goal 6B may deepen its implementation with one internal Home-power
Module, but may not expose a parallel inventory, crafting, building, Load, or
resource simulation.

Through existing commands plus the smallest Goal 6B command values, Core owns:

- generated identity, origin, current physical state, adjacency, carrying, and
  dropping of the Lode;
- Home/site eligibility and precise rejection;
- commitment planning, timing, cancellation, interruption, and material
  progress;
- Source construction, damage, destruction, dismantling, contribution, and
  rebuilding;
- current and next-Attunement capacity, atomic Loadout validation, and
  replacement-body Attunement;
- durable results and deterministic forecast/result snapshots; and
- all persistence and migration invariants.

The preferred public query is one read-only `PowerComesHome` context composed
from Core-owned subject, commitment, Source, and Attunement facts. Its
structured rejection and availability facts drive both keyboard and mouse
presentation. Tests cross the same seam. Godot may select and emphasize a
subject, but it never infers origin, carry limits, build eligibility, progress,
capacity, damage thresholds, or persistence from labels.

World Grammar exposes the Seam, Lode, empty origin, construction, and Source
states through the same bounded semantic area snapshots used by player and
Inspector. Visual Grammar maps those facts; neither Godot consumer receives a
second rule path.

## Persistence, death, replay, and migration

Goal 6B uses strict save envelope `v7` and World Grammar `v5` for new
Chronicles.

Save v7 contains only the minimum additional durable state:

- the Lode's stable identity, immutable origin, and exclusive physical state;
- its loose Address, carrier Incarnation identity, construction commitment, or
  installed Source identity as applicable;
- the Resonator's stable identity, Home-relative Address, exact construction or
  dismantling step, intact/damaged/destroyed/rebuilding state, and linked Lode;
- the one active physical commitment and its deterministic remaining ticks;
- the active Loadout, used Load, capacity and tick at last successful
  Attunement; and
- no derived presentation label, map overlay, next-capacity total, generated
  catalogue, or visual selection.

Strict invariants forbid a Lode from being simultaneously embedded, loose,
carried, committed, or installed; a Source without the same Lode; a carrier
that is not the living current Incarnation; progress outside authored bounds;
and an attuned Loadout that exceeded its recorded capacity when created.
Current saves remain valid when the currently derived next capacity is lower.

Death applies in deterministic order:

1. interrupt the body's pending physical commitment;
2. if it carried the Lode, place the Lode loose at the death Address;
3. preserve the Seam, construction progress, Resonator, damage, destruction,
   Lode origin, Home, Brute, scorch, and all other Chronicle state;
4. end the body's current Attunement; and
5. require the replacement to Attune under the Source state that exists then.

The same seed plus command and Heartbeat stream must reproduce identical
origin, identities, movement, commitment transitions, capacity snapshots,
results, death drop, Source state, and final serialized state regardless of
viewport, Inspector query order, pause duration, or save/load boundaries.

Literal v6, v5, v4, v3, v2, v1, and pre-envelope inputs remain accepted. The
v6-to-v7 migration preserves every Goal 6A durable and records its existing
Loadout under the inherent capacity of `8`; it creates no Lode or Source in a
World Grammar v4 Chronicle. Older pins likewise gain no retroactive Seam.
Existing migration shapes remain private inputs to the one current v7 runtime;
there is no parallel v6 gameplay path. New WG5 Chronicles alone generate the
Goal 6B resource.

## P-GEN, Visual Grammar, and packaging

Goal 6B deliberately advances the compatible compiled-pack contract after Goal
6A. P-GEN must author, at minimum:

- embedded and loose Resonant Lode;
- intact and empty Singing Seam;
- the Incarnation's carried-Lode overlay;
- eligible Home site emphasis;
- three readable construction steps;
- intact, damaged, destroyed, and three rebuilding Resonator states; and
- pending, interrupted, capacity-gained, and capacity-lost emphasis using
  existing semantic palette roles where possible.

The map states must remain legible at the accepted `2×` player presentation and
native Inspector preview. Required busy-state captures must prove that the
carrier, Home, Source, progress, pause/location rail, decision text, and Message
Log neither clip nor overlap.

P-GEN decides appearance only. It never owns resource generation, Source
state, Load, Attunement, or commitment rules. Production projects consume the
compiled artifact through Palimpsest's existing reader. Root verification must
compile P-GEN first, prove player/Inspector semantic and render-plan parity,
and prove that the packaged game still contains only the four canonical runtime
pack files—never compiler assemblies, catalogues, workbench code, review
captures, or authoring briefs.

## Exact automated acceptance fixtures

`checks/verify.ps1` must retain the complete Goal 6A and predecessor gate and
add the following through the real `ChronicleSimulation` interface.

### F1 — Generated identity and origin

- Seed `41_337`, WG5, Seam `surface:8,3`, Home `surface:0,3`, and eligible site
  `surface:1,3` match exactly.
- Overlapping and differently ordered bounded queries return the same Seam and
  Lode identities.
- Extraction produces the empty persistent Seam and one loose Lode whose
  origin never changes.
- WG4 and every older grammar pin return neither subject.

### F2 — Physical carry state

- Lift requires adjacency, moves no Clock, and makes the state exclusively
  carried.
- A carried Lode follows each resolved movement and is visible through the
  semantic snapshot; query or viewport order changes nothing.
- a second Lift, Fly, Weapon stance/strike, Burn Preparation, and Attunement
  reject with precise non-mutating reasons.
- Set Down creates exactly one loose subject at the carrier Address.
- carrier death drops exactly one loose Lode at the death Address before
  replacement.

### F3 — Commitments and construction

- Extraction is `2`, Build is `3`, Dismantle is `2`, and Rebuild is `3`
  Heartbeats.
- Paused pulses make no progress; active pulses make one deterministic step.
- every snapshot names next tick, interruption, disabled actions, and committed
  matter.
- cancellation, hostile interruption, death, and
  save/load at every intermediate step preserve the specified material state.
- build eligibility rejects no Home, wrong site, missing/not-carried Lode,
  danger, and another commitment without spending time or consuming matter.

### F4 — Load and Attunement

- `Burn + Quickly + Lasting` is order-independent, unique, three Links, Load
  `12`, Preparation `1`, consequence `6`, and Recovery `8`.
- at base capacity `8`, the request rejects atomically with current Loadout,
  Clock, Recovery, and Source unchanged.
- completed intact Source changes next capacity to `12` but does not change the
  current Loadout or its recorded Attunement ceiling.
- successful Attunement records the combined Expression, used Load `12`,
  capacity `12`, and exact tick.

### F5 — Damage, destruction, and rebuilding

- dismantling tick one yields Damaged and still contributes `+4`.
- dismantling tick two yields Destroyed, exposes the same Lode at the Source
  Address, and changes only next capacity from `12` to `8`.
- the already-attuned twelve-Load Expression previews and releases normally
  after Source destruction.
- any new Attunement over `8` rejects without mutating that current Expression.
- rebuilding the same Lode takes three ticks and restores next capacity to
  `12` without automatically changing the current Loadout.

### F6 — Save/load, death, and replacement

- strict v7 round-trips embedded, loose, carried, dropped, each construction
  step, intact, damaged, destroyed, and each rebuilding step.
- save/load during a commitment preserves its exact next transition and does
  not double-advance, duplicate, refund, or consume the Lode.
- after Source destruction, death ends the twelve-Load Attunement; replacement
  sees capacity `8`, cannot Attune the combined Expression, and retains every
  world durable.
- after rebuilding, replacement sees capacity `12` and may Attune the combined
  Expression explicitly.

### F7 — Deterministic replay and migration

- uninterrupted and save-split command streams end in byte-equivalent or
  canonical-state-equivalent v7 state and identical result/forecast order.
- literal strict v6 plus every older supported fixture migrates through one
  current runtime, preserves all accepted durables, and gains neither resource
  nor Source under old grammar pins.
- malformed dual-location, missing-Lode Source, invalid carrier, invalid
  progress, and impossible recorded-Attunement saves fail precisely.

### F8 — Player, Inspector, and package proof

- player and Inspector semantic snapshots and render-plan digests agree for
  every resource/Source state and overlapping request.
- keyboard and mouse paths expose identical Core-owned decision text and
  results.
- deterministic native captures cover embedded, carried, construction,
  intact/capacity-ready, damaged, destroyed/current-versus-next, and rebuilding
  states at `1600 × 900`.
- layout assertions prove no pause/location, progress, capacity, Target,
  forecast, or log clipping in the busiest required state.
- P-GEN compile, required mapping, pack digest, shared reader, and exact
  four-file packaged isolation all pass.

A Core marker, raw JSON assertion, or Inspector-only capture cannot substitute
for the rendered Godot acceptance paths.

## Exact player UAT

Use isolated named profiles through `play.ps1`. Each journey begins from a
fresh WG5 acceptance Chronicle and uses no verifier-mutated save.

### Journey A — Bring power Home

Profile: `goal6b-power-home-uat`

1. Begin directly at The First Hearth with no opening-path chooser. Confirm the
   map and checklist identify the unread Burn Primer one tile north. Press `P`
   while on its cell or adjacent and confirm reading spends no Heartbeat, leaves the book
   visibly read, and adds `Burn`, `Quickly`, and `Lasting` to the Codex. Open
   Attunement while safe and inspect `Burn + Quickly + Lasting`. Confirm the
   interface says it needs `12`, only `8` is available, and one intact Hearth
   Resonator would add `4`.
2. Follow the physical map to the visible Singing Seam at `surface:8,3`.
   Inspect the Lode and verify its origin, extraction time, interruption rules,
   and disabled actions before committing.
3. Begin extraction while paused. Confirm `0/2` remains visible and says that
   Heartbeats must resume. Advance one Heartbeat, save/close/reload, and confirm
   `1/2` remains on the same map cell. Finish extraction and see the empty Seam
   plus one loose Lode.
4. Lift the Lode. Confirm the carrier overlay, one-object limit, and explicit
   reasons that Cleaver, Burn, Fly, and Attunement are unavailable. The direct
   westward line enters the living Brute's visible threat range; take the safe
   one-cell detour while it remains alive. Walk the full route back, then drop
   and lift the Lode once to prove it is physical, not a count.
5. At Home, inspect the highlighted eastern site. Begin Build while paused and
   answer the four decision questions from the screen. Cancel after one active
   Heartbeat, move only after cancellation, return, and resume the same material
   foundation to completion.
6. Confirm the map shows an intact Resonator while the current Loadout remains
   unchanged. Read `NEXT ATTUNEMENT: 12 = 8 inherent + 4 Hearth Resonator`.
7. Attune `Burn + Quickly + Lasting`. Confirm Load `12/12`, Preparation `1`,
   consequence `6`, Recovery `8`, and the recorded Attunement tick. Use it
   against the one existing Mire Brute and confirm both Modifier effects are
   legible in the forecast and result text.
8. Save, close, and reload. Confirm the empty Seam, Lode origin, Home,
   Resonator, current combined Expression, Brute/scorch state, and capacity
   breakdown all remain connected on the map and in text.

### Journey B — Lose it without a remote shutoff, then rebuild

Profile: `goal6b-loss-rebuild-uat`

1. Begin from the same neutral testing start, read the Burn Primer, and complete
   the same Lode acquisition/build/Attunement loop, leaving the Mire Brute alive
   and the twelve-Load Expression current.
2. At the Resonator, inspect Dismantle before committing. Advance exactly one
   Heartbeat. Confirm the map shows Damaged, text says it still contributes
   `+4`, one more Heartbeat destroys it, and current/next capacity both remain
   understandable. Save/reload in this state.
3. Finish Dismantle. Confirm the map shows Destroyed with the same Lode exposed.
   Read `CURRENT: 12/12 remains until Attunement or death` and
   `NEXT ATTUNEMENT: 8 inherent`.
4. Attempt to Attune the same combined Expression. Confirm rejection names the
   destroyed Resonator, changes nothing, and leaves the current Expression
   active. Return to the Mire Brute and release that current combined
   Expression once, proving there was no remote shutoff.
5. Deliberately allow the Incarnation to die. Create the replacement and
   confirm the Source remains destroyed, the Lode remains at Home, the prior
   world consequences remain, and mandatory fresh Attunement cannot select the
   twelve-Load Expression under capacity `8`.
6. Return to the Source cell, inspect Rebuild, and confirm its same-Lode cost,
   three-Heartbeat timing, interruptions, and disabled actions. Advance one
   Heartbeat, save/close/reload, then complete rebuilding.
7. Confirm `NEXT ATTUNEMENT` returns to `12` but no Loadout changes
   automatically. Explicitly Attune the combined Expression, return to the
   Mire Brute, and finish the existing encounter.
8. Save/reload once more and explain aloud: where the Lode came from, who
   carried it, what Home changed, when capacity applied, why destruction did
   not disable the current body, why death did, and how rebuilding restored the
   future.

## Goal 6A debt as negative acceptance examples

Goal 6B fails UAT if it reproduces any of these prototype failures:

- **Invisible paused queues:** a pending move or commitment appears inert or
  catches up without a visible next tick and resume instruction.
- **Unexplained disabled Attunement:** a disabled Expression or control does not
  name safety, carrying, pending commitment, current capacity, Source state,
  and the route back to availability as applicable.
- **Opaque mitigation or raw values:** construction, damage, or capacity shows
  only numbers without the named material or rule that caused them.
- **Ambiguous physical action feedback:** extraction, lifting, melee-range
  interaction, dismantling, or rebuilding reads as occurring at range or on an
  unclear cell.
- **Clipping:** pause/place, progress, capacity, consequence, forecast, and log
  text overlap, truncate essential reasons, or leave the `1600 × 900` frame.
- **Map/log disconnects:** the log claims acquisition, carrying, construction,
  damage, destruction, capacity loss, or rebuilding that the map does not show
  at the same identity and Address.

The accepted Goal 6A Incarnation ring remains temporary art, but Goal 6B may
add only the carrier and Source readability needed for this slice. It is not a
graphics 1.0 or unrelated HUD redesign authorization.

## Acceptance gate

Goal 6B passes only after the complete automated gate succeeds with zero build
warnings or errors and the player completes both UAT journeys, then reports all
of the following:

- the Seam, Lode, carrier, Home, construction, Source, damage, destruction, and
  rebuilding were immediately identifiable on the map;
- every commitment answered what happens next, when, interruption, and locked
  alternatives before the player committed;
- acquisition and carrying felt physical rather than like loot or inventory;
- the capacity gain made the combined Expression feel desirable and previously
  impossible;
- current Loadout, capacity at last Attunement, and next-Attunement capacity
  were never confused;
- Source loss did not read as a remote shutoff, while replacement-body loss of
  the old Attunement made sense;
- the Lode's origin and material continuity survived save/load, death,
  destruction, and rebuilding;
- the full loop remained one expedition, Home, and Chronicle rather than a
  crafting or base-management mode; and
- the player wants to seek another material or protect Home after the slice.

Passing the automated gate is engineering readiness, not player acceptance.
Mechanically correct but unclear behavior must be corrected within this bounded
presentation surface and resubmitted to UAT.

### Engineering readiness result — 2026-07-21

The complete `checks/verify.ps1` gate passes with zero build warnings or
errors. It proves:

- strict v7/WG5 Core state, literal v6-through-pre-envelope migration,
  malformed-state rejection, deterministic replay, physical carrying, death
  drop, replacement, work interruption, Source loss, and rebuilding;
- all required P-GEN mappings and deterministic Goal 6B semantic/render plans;
- packaged and manual-comparison Inspector parity across embedded, loose,
  carried, construction, intact, damaged, destroyed, and rebuilding states;
- eight deterministic native `1600 × 900` player captures covering the unread
  Burn Primer, embedded Lode, carried Lode, construction,
  intact/capacity-ready, damaged,
  destroyed/current-versus-next, and rebuilding states, including keyboard and
  mouse parity and non-clipping decision/capacity assertions;
- both retained Goal 6A journeys and every predecessor migration fixture; and
- the exact four-file shipped visual pack with P-GEN compiler, catalogue,
  workbench, and review material absent.

The final marker is:

```text
PASS: Goal 6B Power Comes Home implementation, strict v7 migration, eight rendered HUD proofs, Inspector parity, exact four-file packaging, and the complete retained Goal 6A/predecessor gate verified; player UAT remains pending.
```

At this engineering checkpoint, Journeys A and B below remained the active
acceptance gate. Final player acceptance is recorded after the correction
history below.

### First player UAT result and bounded correction — 2026-07-21

The first player UAT rejected the candidate. After combat, a single movement
command paused, remained queued, and moved one extra cell on resume. The player
also could not infer the loop from the interface: the animated Seam and Source
appeared inert, the Source asked to be damaged without exposing a plainly named
action, its mountain-like foundation obscured it, and neither construction nor
dismantling made the Load consequence evident.

The bounded correction:

- resolves one safe post-combat movement command exactly once without entering
  the Slow danger queue;
- gives the generated Resonator site a deterministic clear-soil foundation and
  enlarges every Goal 6B map mark within the existing 20-pixel vocabulary;
- replaces indirect fixture language with visible `GOAL`, `NOW`, `DO`, and
  `LOCKED` guidance, explicit `PRESS P` actions, and a named `[G] ATTUNE 12/12`
  control; and
- distinguishes `+4 LOAD READY` from the current Loadout and says directly that
  building changes capacity only when the player next Attunes.

Focused regressions prove exact-once safe movement after combat, the WG5
clear-soil Source foundation, required wayfinding/action/capacity language, and
minimum visual occupancy. The complete retained `checks/verify.ps1` gate then
passed again with zero build warnings or errors. The regenerated exact four-file
pack has aggregate digest
`sha256:93ae41731f3f04a2824156752bf29fa7f0cbd4f5fff29b5a9185ca64dce88e68`.

Journeys A and B must be repeated from fresh profiles:

```powershell
.\play.ps1 -Profile goal6b-power-home-uat-r2
.\play.ps1 -Profile goal6b-loss-rebuild-uat-r2
```

This correction is a new engineering-ready candidate, not player acceptance.

### Second player UAT result and bounded correction — 2026-07-22

The next player attempt rejected the candidate during extraction. Pressing `P`,
resuming Heartbeats to finish harvesting, and then moving caused every safe
movement command to pause and queue again.

The commitment was clearing correctly: extraction was complete, no commitment
or tactical command remained, Engagement was inactive, and the Incarnation was
outside immediate danger. The actual latch was the movement rule's use of a
living Mire Brute anywhere in the World as a proxy for tactical context. Since
Space resumes physical work at Slow, every later Slow movement was incorrectly
treated as a combat command even while the Brute was distant.

Core now queues movement only when the Incarnation is already in immediate
danger, or when a Slow movement would cross into immediate danger. Safe Slow
movement elsewhere resolves once immediately. This retains the accepted
pause-before-contact behavior without letting a distant living opponent turn
ordinary travel into a permanent auto-pause loop.

A deterministic Core regression drives the exact paused extraction, two active
Heartbeats, cleared commitment, and first safe movement. The actual Godot Goal
6B keyboard journey repeats a safe south/north step after extraction and proves
that neither step pauses or leaves a queued action. The complete retained
`checks/verify.ps1` gate passes with zero build warnings or errors and exit code
`0`.

Repeat Journeys A and B from fresh profiles:

```powershell
.\play.ps1 -Profile goal6b-power-home-uat-r3
.\play.ps1 -Profile goal6b-loss-rebuild-uat-r3
```

This second correction is engineering-ready only; player acceptance remains
pending.

### Third player UAT feedback and bounded presentation correction — 2026-07-22

Before repeating UAT, the player reported that the instruction popup remained
verbose and confusing and requested one simple checklist that changes when the
material loop enters a new state.

The bounded correction removes the three stacked instruction surfaces. Core now
owns one checklist of at most five non-empty lines for each meaningful state:
embedded, active extraction, loose, carried, active construction, resumable
construction, intact before Attunement, intact after the twelve-Load
Attunement, active dismantling, destroyed, active rebuilding, and resumable
rebuilding. Each checklist names only the next input or Heartbeat, the next
state, interruption behavior, and disabled actions relevant at that moment.
Godot renders that checklist once, retains the existing action controls, and
shows capacity in one compact line: `CURRENT … · NEXT ATTUNE …`. The duplicate
`NEXT / WHEN / INTERRUPTS / PREVENTS` paragraph is removed.

Core acceptance now asserts the checklist shape and required facts across the
complete physical loop. The actual Godot journey asserts no more than five
lines, state changes, keyboard/mouse parity, compact current/next capacity, no
duplicate decision panel, and no clipping in all seven native `1600 × 900`
captures. Visual inspection confirmed the smaller panel leaves the map dominant.
The complete retained `checks/verify.ps1` gate passes with zero build warnings
or errors and exit code `0`, including strict migration/replay, Inspector
parity, P-GEN isolation, exact packaging, and both Goal 6A journeys.

Repeat Journeys A and B from fresh profiles:

```powershell
.\play.ps1 -Profile goal6b-power-home-uat-r4
.\play.ps1 -Profile goal6b-loss-rebuild-uat-r4
```

This third correction is engineering-ready only; player acceptance remains
pending.

### Fourth player UAT result and bounded testing-start correction — 2026-07-22

The next player attempt correctly rejected the candidate because the checklist
eventually required `Burn + Quickly + Lasting`, but the chosen Build opening had
provided only `Found`. The automated Goal 6B fixtures had selected the Combat
opening and silently received all three required Words, so they did not exercise
the real player start.

The player directed that opening paths be removed from the current testing flow
and that Burn become a nearby physical acquisition. Fresh WG5 player profiles
now begin directly at The First Hearth under the neutral Home start; the opening
chooser is never shown. One deterministic **Burn Primer** remains at
`surface:0,2`, one tile north of Home. Its book glyph and unread brackets are
visible on the map. The first checklist is `LEARN BURN`, and the contextual `P`
action reads the Primer immediately with no Heartbeat cost. Reading adds
`Burn`, `Quickly`, and `Lasting` to the persistent Codex, removes the unread
brackets while leaving the book visible, and switches to `GET THE GOLD LODE`.
This is one bounded acquisition fixture, not inventory, loot, or a generic
Study system.

Core acceptance begins from the same neutral Home start as the player, proves
the unread/read map state, atomic acquisition, no-Heartbeat timing, Codex
persistence, save round-trip, and checklist transition, then drives the entire
Goal 6B loop. Visual composition proves the book remains visible after reading.
The actual Godot journey now begins with an eighth native `1600 × 900` Burn
Primer capture and asserts no opening chooser, map/book visibility, the exact
`P` action, all three learned Words, and the next checklist. The complete
retained `checks/verify.ps1` gate passes with zero build warnings or errors and
exit code `0`.

Repeat Journeys A and B from fresh profiles; do not reuse `-r4`, whose save may
retain the obsolete opening selection:

```powershell
.\play.ps1 -Profile goal6b-power-home-uat-r5
.\play.ps1 -Profile goal6b-loss-rebuild-uat-r5
```

This fourth correction produced the accepted player candidate.

### Final player UAT acceptance — 2026-07-22

The player completed the corrected Goal 6B journey and reported a pass. The
accepted bounded behavior requires the Burn Primer acquisition followed by the
three-Word Attunement before `Burn + Quickly + Lasting` can be used in combat.
The player explicitly accepted that limitation for this slice. Improving how
Words are acquired, equipped, and combined remains future work under a separate
contract; it is not a Goal 6B failure and does not authorize a generic Study,
inventory, or equipment framework here.

At the UAT boundary, record the result here, reconcile Roadmap and Handoff, and
stop. This pass closes Goal 6; it does not authorize Goal 7, Slice 8, Goal 6A
polish, another resource, another Source, or a general framework.

## Explicit exclusions

- inventory, loot, item stacks, equipment weight, encumbrance statistics, or a
  generic carry system;
- generic crafting, recipes, tools, repair materials, production chains,
  stockpiles, storage, vendors, trade, or resource currencies;
- broad resource taxonomies, procedural material properties, mining, gathering,
  harvesting, or renewable nodes;
- residents, workers, jobs, Agents, Companions, Directives, automation, or
  construction crews;
- general construction placement, blueprints, rooms, walls, furniture,
  building menus, structural support, or multiple structures;
- more than one Singing Seam, Resonant Lode, Hearth Resonator, build site, or
  carryable type;
- raids, off-camera Source damage, Pressures, factions, defenses, traps, or
  automatic repairs;
- a general damage, integrity, salvage, dismantling, repair, or ruin system;
- another opponent, bestiary growth, drops, reward tables, or combat framework;
- new Verbs, Modifiers, Study Sources beyond the single authorized Burn Primer,
  Targets, Verb slots, Load Sources, or a general Link/Load progression curve;
- runtime mana, fuel, upkeep, Source range, live tethering, brownouts, remote
  deactivation, or settlement-gated ordinary combat;
- Areas, Passages, dungeons, a strategic overworld, pathfinding, water
  traversal, or a World-edge decision;
- camera-density, rectangular-cell, actor-art, settings, onboarding, menu,
  animation-system, or unrelated visual redesign;
- a second runtime, compatibility mode, weakened strict migration, changed old
  grammar pins, or gameplay rules in Godot; and
- any Goal 7, Slice 8, Palimpsest, Decree, World Claim, or raid work.

## Approved bounded fixture decisions

Production authorization approved these five points as written. They may not
expand during implementation.

1. **Fixture language:** approve or rename `Singing Seam`, `Resonant Lode`, and
   `Hearth Resonator`. Recommended: approve; the names communicate place,
   matter, and function without implying a taxonomy.
2. **Desirable Loadout:** approve three-Link `Burn + Quickly + Lasting` at Load
   `12`, with fixture Link capacity `3` already available. Recommended: approve;
   it reuses accepted Words and makes Source capacity the only remaining block.
   Adding a new expensive Modifier would enlarge the slice.
3. **Carry tradeoff:** approve both-hands carrying that disables Cleaver, Burn,
   Fly, and Attunement while preserving movement and Set Down. Recommended:
   approve; it creates one visible physical decision without encumbrance math.
4. **Vulnerability proof:** approve the two-Heartbeat controlled Dismantle whose
   first step is damaged-but-functional and second is destroyed. Recommended:
   approve; it proves damage and loss without raids, a hostile fixture, or a
   general damage system.
5. **Fixture timing and placement:** approve acceptance origin `surface:8,3`,
   site `surface:1,3`, and `2/3/2/3` Heartbeats for extract/build/dismantle/
   rebuild. Recommended: approve provisionally for UAT; they are explicit test
   values, not final balance.

## Consumed production authorization

> Implement Goal 6B — Power Comes Home under
> `docs/GOAL-6B-POWER-COMES-HOME.md` on branch
> `codex/goal-6b-power-comes-home`. The five bounded fixture decisions in the
> contract are approved as written. Production work is limited to that one
> vertical slice, its strict v7/WG5 migration, P-GEN/runtime vocabulary,
> automated fixtures, and two isolated UAT journeys. Preserve the complete
> retained gate and stop with player acceptance pending. Do not begin Goal 7,
> Slice 8, Goal 6A polish, another resource or Source, or any excluded generic
> framework.

## Stop condition

Stop after the accepted player UAT and tracker reconciliation. Do not make
further production changes, implement the future inventory, begin Goal 7 or
another excluded framework, commit, push, or merge. Await separate production
authorization.
