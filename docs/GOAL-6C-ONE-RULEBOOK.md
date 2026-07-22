# Goal 6C — One Rulebook, One Vocabulary

**Status:** authorized as a bounded consolidation gate on 2026-07-22 and
implemented on `codex/goal-6c-one-rulebook`; first player review accepted the
material loop but rejected a focused-button combat pause loop and top-rail text
overlap. The bounded correction and complete retained gate are green. Player
re-UAT passed on 2026-07-22 with “Yep pass”; Goal 6C is complete and accepted.

This contract closes the ownership gaps the pre-Goal-7 review found in the
accepted Goal 6 build. It adds no new system, content, inventory framework, or
broad Chronicle rule. Its one post-review interaction correction treats a
non-blocking Goal 6B subject's own cell as valid interaction reach, matching an
adjacent cell. Otherwise it is ownership and feedback repair only, so Goal 7
does not harden the Goal 6 fixture shape into the successor architecture.

Canonical authority remains [AGENTS.md](../AGENTS.md), the
[glossary](../CONTEXT.md), [Vision](VISION.md), [Architecture](ARCHITECTURE.md),
[Roadmap](ROADMAP.md), and the active [Handoff](HANDOFF.md). The accepted
[Goal 6A](GOAL-6A-A-REAL-FIGHT.md) and [Goal 6B](GOAL-6B-POWER-COMES-HOME.md)
contracts remain the authority on what the game does. This contract changes only
who owns each fact and where it lives.

## Product question

Can the same accepted Goal 6 game be produced by a rulebook that does not know
what a keyboard is, a Word catalogue that can be extended without editing a
resolver, a world cell that does not grow a field per fixture, and a
verification gate that claims exactly what it runs?

## Player promise

No new gameplay system. Every mechanical behaviour of the accepted Goal 6
build reproduces identically apart from the explicit same-cell interaction
correction. The bounded feedback corrections in R5 and the review-correction
sections make existing material state, timing, interruption, prevention, and
logs readable without changing their Core outcomes.

## What this gate is not

It is not a redesign, a HUD rebuild, a Codex interface, a Goal 7 foundation, or
permission to delete the predecessor command paths. Refactoring pressure that
argues for any of those is out of scope and belongs in a later contract.

## Permitted scope

### R1 — Core owns facts; Godot owns sentences and keys

`Goal6BPowerComesHome.Summary()` and `CapacitySnapshot` currently emit HUD text
containing literal input keys and one screen's nouns: `[ ] P — Read (instant)`,
`[ ] SPACE — run 1 Heartbeat`, `Press G to apply it`, `GOLD SEAM`,
`OUTLINED SITE`. `ChronicleHud` prints that text verbatim and the Core checks
assert the copy back.

Core keeps structured facts in the existing `PowerActionSnapshot` shape — state,
next transition, timing, interruption, prevented actions, and availability
reason — expressed in the settled `CONTEXT.md` vocabulary. Godot composes every
sentence, every key label, and every checklist glyph from those facts. Core
checks assert facts, not prose.

**Acceptance.** A gate assertion over every `src/Chronicle.Core` source file
finds no string literal containing an input key name, a HUD-only label, or a
checklist glyph. The banned lexicon is enforced in code, not by review.

### R2 — Word effects are authored catalogue data

`Goal6AActionPlanning.PreparationFor` and `ConsequenceFor` are per-Word
conditionals inside the fight resolver. Authoring a fourth Modifier today means
editing the resolver.

Preparation, consequence, recovery, and damage move onto `WordDefinition` as
authored data. Exact current values are preserved: Burn 3 preparation, 3
consequence, 8 recovery, 4 damage; Quickly preparation −2 to 1; Lasting
consequence +3 to 6. Resolution reads the catalogue and composes deltas
order-independently.

**Acceptance.** A test-only fourth Modifier, authored as catalogue data in the
gate and never entering the shipped Catalogue, changes resolved timing with
zero changes outside its own definition.

### R3 — One durable-subject model on a cell

`WorldCell` carries `MireBrute`, `Target`, `SingingSeam`, `ResonantLode`,
`HearthResonator`, `IsHearthResonatorSite`, and `BurnPrimer` as one nullable
field per fixture subject, and `VisualGrammar` has a hand-written branch per
field. Goal 7's named Agent and Creature Grammar's arbitrarily many instances
cannot be expressed in that shape.

One `WorldSubject` abstraction replaces those fields: durable identity, kind,
archetype, condition, bounded marks, and one optional bounded current/maximum
progress pair. `Chronicle.Visuals` resolves subject to visual through one table
driven path instead of a branch per fixture. `IsScorched` remains cell material
state, not a subject.

**Acceptance.** The Mire Brute, Singing Seam, Resonant Lode, Hearth Resonator,
Resonator site, Burn Primer, and combat Targets all round-trip through the model
with byte-identical saves and identical render-plan digests.

### R4 — Split `ChronicleState.cs`

2727 lines, roughly 85% save codec, migration, and document validators, is the
largest per-slice tax on Goal 7 velocity and blocks an eighth envelope version.

Split into at least three files: state and behaviour, the current save codec,
and per-version migration plus document validators. No behavioural change, no
save-format change, no envelope version change. No resulting file exceeds
roughly 900 lines.

**Acceptance.** Every existing migration fixture still passes; save bytes are
unchanged.

### R5 — Three presentation defects

Correct rules currently read as broken:

- `ChronicleApp` falls back to the literal `— No meaningful event while the
  Chronicle is paused.` whenever the forecast is empty, which is every
  non-combat state, including while the clock reads SLOW or NORMAL. The panel
  must state its actual reason, or show the real upcoming event.
- `ChronicleHud` Consequence rows use `AutowrapMode.Off` with `TrimEllipsis`,
  truncating the Core-owned rejection reason exactly where the reason lives
  (`REJECTED · The Mire Brute is outsid…`). Reasons must be readable in full.
- `ChronicleApp` appends `_presentationStatus` after `RecentResults`, so every
  command appears twice in a four-line Message Log.

**Acceptance.** No reason text truncates in any of the eight regenerated
captures; no message appears twice; the forecast panel never contradicts the
clock beside it.

### R6 — An honest gate

`checks/verify.ps1` defines `Invoke-GodotAcceptance`, `Invoke-GodotGoal4ARun`,
`Invoke-GodotGoal4ARestart`, `Invoke-GodotGoal4BRun`, `Invoke-GodotGoal4CRun`,
`Assert-Goal4BSave`, `Assert-Goal4CSave`, `Invoke-GodotSlice5Run`,
`Assert-Slice5Save`, `Invoke-GodotGate3BPlayerAcceptance`,
`Invoke-GodotGate3BAtlasAcceptance`, and `Invoke-GodotAtlasAcceptance` — each
defined exactly once and never invoked. Their runtime roots are still created
and torn down. `docs/CODEMAP.md` and the final pass marker assert those
journeys run.

Delete every unreachable function, its unused runtime-root variables, and its
teardown. Correct `docs/CODEMAP.md` and the pass marker to describe exactly what
runs. Predecessor coverage remains as the Core-level literal save migration
fixtures, which do run and do pass.

**Acceptance.** No function in `verify.ps1` is unreachable; the pass marker
names only journeys the script executes.

### R7 — Domain names, one direction

`Goal6AActionPlanning` is the combat system; `Goal6BPowerComesHome` is the
material, Home, Attunement, and capacity system. They call each other in both
directions, and a third module would join that knot rather than a clean seam.

Rename to `CombatRules` and `HoldingRules`. `CombatRules` may not reference
`HoldingRules`. Facts combat needs about carrying, commitments, occupancy, and
capacity become pure predicates in a shared `HoldingFacts` Module both rulebooks
read; the cross-cutting tick, interruption, and death hooks run through one
`IMaterialCommitments` seam supplied by the caller.

**Acceptance.** No `Goal6A`/`Goal6B` type name survives in Core, and
`CombatRules` contains no reference to `HoldingRules`.

### R8 — Codex and Loadout surface direction (decision only)

Written decision, no implementation. Recorded in *Open decisions* below.

## Automated acceptance fixtures

- **C1 — Core vocabulary.** No `src/Chronicle.Core` string literal contains a
  banned key name, HUD-only label, or checklist glyph.
- **C2 — Authored fourth Modifier.** A catalogue-only test Modifier changes
  preparation timing with no resolver edit.
- **C3 — Subject round-trip.** All seven fixture subjects resolve through
  `WorldSubject` with unchanged save bytes and unchanged render-plan digests.
- **C4 — Split with no drift.** Every existing migration fixture and validator
  rejection still passes after the file split.
- **C5 — One-directional modules.** `CombatRules` references no holding rule.
- **C6 — Behaviour preservation.** The eight Goal 6B captures and both Goal 6A
  journeys reproduce identical player-visible behaviour apart from the three
  R5 fixes and the explicit same-cell interaction correction.

## Acceptance gate

`checks/verify.ps1` passes with zero build warnings, zero errors, and exit code
0, and the eight regenerated Goal 6B captures are ready for player review.

## Open decisions

### R8 — Codex and Loadout surface direction

**Problem.** Attunement is three hardcoded hotkey macros — `Q` = Burn+Quickly,
`L` = Burn+Lasting, `G` = Burn+Quickly+Lasting. There is no Word list, no Codex
view, and no Link editor. Goal 6 proved Core can validate a Loadout; it did not
prove the build loop is operable. Goal 7's social Verbs would add Words with
nowhere to go.

**Decision.** The Codex and Loadout become one paused full-screen surface, not a
HUD panel and not a modal per action:

1. **Codex** — the persistent list of known Words, grouped Verb and Modifier,
   each with its authored Load, meaning, and Understanding. It is Chronicle
   state that survives death, so it is never rebuilt from the current Loadout.
2. **Loadout builder** — active Verb slots on the left, the linked Modifier
   chain per slot on the right, with used Load against the capacity available at
   the next Attunement, and the exact reason any link does not fit.
3. **Attune** — one explicit commit action from that surface, subject to the
   existing safety, carrying, and commitment rules. Attunement remains the only
   moment capacity changes.
4. **Hotkeys become presets over that surface**, never the surface itself. The
   current `Q`/`L`/`G` macros retire once the builder exists.

The surface is paused-only, is not a crafting or inventory screen, and does not
introduce Word acquisition. It is authorized here as direction only; it needs
its own contract, fixtures, and player UAT.

## Explicit exclusions

- Goal 7 Agents, residents, relationships, Directives, Pressures, or off-camera
  history; Slice 8 raids.
- Any Codex or Loadout implementation; R8 is a decision only.
- New Words, Verbs, Modifiers, Targets, opponents, Study Sources, or Load
  Sources, beyond the test-only fourth Modifier proving R2.
- Inventory, crafting, economy; Areas, Passages, pathfinding, water traversal.
- Camera density, rectangular cells, actor art, multi-cell geography, or any HUD
  redesign beyond the three R5 feedback fixes.
- Seed-derived world generation (H1), deleting the predecessor Fly/Found/Smash/
  Noun/Study command paths (H3), forecast caching (M10), HUD layout system or
  index-bound action array (M9), assembly-name collision (L1), pack digest byte
  comparison (L2), speed controls beyond Pause/Slow (L3).
- Any change to save v7 semantics, old World Grammar pins, or the P-GEN
  compiler/runtime packaging boundary.
- Commit, push, or merge without separate authorization.

## Engineering result — 2026-07-22

`checks/verify.ps1` passed with zero build warnings, zero errors, and exit code
0. It ran P-GEN authoring verification, the Core checks including the literal
predecessor save migration and the two new gates, the Visuals checks, the Godot
editor build, exact four-file packaging isolation, World Atlas Inspector
semantic and native visual parity, both Goal 6A journeys with their rendered
HUD proofs and saves, and the Goal 6B journey with its eight rendered HUD
proofs. The Inspector render-plan digest is unchanged, which is the proof that
the durable-subject model composes the identical visual plan.

Delivered against scope:

- **R1.** `PowerComesHomeContextSnapshot.Summary`, `PowerActionSnapshot.Label`,
  and the two `AttunementCapacitySnapshot` status strings are gone. Core now
  exposes `HoldingObjectiveSnapshot` — objective, travel subject and offset,
  action, timing, established fact, next outcome, and constraints. Godot's new
  `HoldingPresentation` composes every sentence, key label, and glyph. Core
  checks assert facts through `AssertObjective`.
- **R2.** `WordDefinition` carries `WordEffect`; `WordEffects.Compose` composes
  Verb and Modifier deltas order-independently; the resolver names no Word.
- **R3.** `WorldSubject` replaces all seven per-fixture `WorldCell` fields, and
  `VisualGrammar.AddWorldSubjects` is the one resolution path.
- **R4.** `ChronicleState.cs` is five files: 405, 539, 167, 840, and 798 lines.
- **R5.** All three defects are fixed and visible in the regenerated captures.
- **R6.** Seventeen unreachable functions, six runtime-root variable pairs, and
  six teardown calls are deleted; the pass marker and Codemap now name only
  what runs.
- **R7.** `CombatRules` and `HoldingRules` replace the fixture names.
  `CombatRules` references no Holding rule: shared predicates live in
  `HoldingFacts`, and the three per-Heartbeat hooks run through the
  `IMaterialCommitments` seam that `ChronicleSimulation` and `ChronicleState`
  supply.
- **R8.** Recorded below as a decision only.

One player-visible string outside the three R5 fixes did change, as a required
consequence of R1: the Core-owned pending-commitment message now reads
`PAUSED — progress waits for the next active Heartbeat` instead of naming the
`SPACE` key. Core may not name a key. The HUD checklist still names the key.

## Review correction — 2026-07-22

The first code/gameplay/UX review failed this implementation. The user then
authorized correction of the complete bounded finding set without authorizing
Goal 7 or an excluded framework. The corrected result now additionally proves:

- `WorldSubject` contains semantic archetype/condition, at most four distinct
  semantic marks, and one optional validated current/maximum pair. Core contains
  no pack visual IDs; `Chronicle.Visuals` owns the lookup tables.
- The test-only fourth Modifier declares Burn compatibility in its own
  definition, then passes the production Attunement, Burn preparation, strict
  v7 validation, serialization, and deserialization path.
- frozen pre-6C save bytes and the accepted Goal 6B render-plan digest fail the
  gate if a consolidation changes compatibility;
- Holding snapshots expose structured availability, outcome, timing,
  interruption/prevention constraints, and capacity numbers. Godot composes
  their sentences and contains no hard-coded 8/12/+4 capacity facts;
- the right rail shows material state and the four decision answers outside an
  active fight, switches back to combat when danger or combat work exists,
  distinguishes movement log entries by Address, and limits the visible log to
  recent readable entries;
- all consequence rows wrap without overlap, controls accept keyboard focus,
  disabled controls retain readable contrast, rejected Burn does not invent a
  Brute interruption, and every work forecast shows the post-next-Heartbeat
  counter.

At that checkpoint the complete retained gate passed again with zero build
warnings and zero errors. Eight new 1600×900 Goal 6B captures were generated
for player review. The later hands-on result and correction are recorded below;
neither is permission to begin Goal 7.

## Player-UAT correction — focused Space and interaction reach

The first hands-on review of the corrected build passed the complete Lode,
carrying, construction, and Attunement loop, then found two blocking combat HUD
regressions. A keyboard-focused action button treated `Space` as Godot's
`ui_accept`, queued `Ready Iron Cleaver`, and returned immediately to pause, so
no hostile Heartbeat could occur. The same pending-action sentence also
overflowed the Clock plate into place and save controls.

The bounded correction reserves the Clock's `Space` action before focused GUI
buttons can activate, while retaining keyboard focus and Enter activation for
buttons. The actual Godot-input fixture focuses the Cleaver button, sends the
physical Space press and release, and proves Slow resumes and exactly one
hostile Heartbeat advances. The top rail now uses compact `PAUSED · HN` text;
the complete pending action remains in Forecast and Message Log, and exact
control rectangles plus rendered minimum sizes prove no overlap.

The same review also recorded that standing directly over an interactable felt
arbitrarily invalid. The bounded Holding rule now accepts the subject's own cell
or a cardinally adjacent cell for the existing Primer, Lode, site, and Source
actions. Core proves same-cell Primer availability and mutation; the actual
Godot journey walks onto the Primer and reads it through contextual `P` before
continuing the unchanged eight-stage material journey.

`checks/verify.ps1` then passed again with zero build warnings, zero errors, and
exit code 0, including P-GEN isolation, strict migration/replay, Inspector
parity, both Goal 6A journeys, the focused-Space regression, all eight Goal 6B
captures, and exact four-file packaging. Player combat re-UAT then passed on
2026-07-22 with “Yep pass.” This acceptance does not itself authorize Goal 7
production.

## Stop condition

Goal 6C is complete and player-accepted. Reconcile the result in the Handoff
and Roadmap, publish only with explicit authorization, and do not begin Goal 7
production, Slice 8, further Goal 6A polish, or any excluded framework without
a separately approved bounded contract.
