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

**Goal 2 complete — awaiting explicit Slice 3 authorization**

- Slice 2A passed player UAT.
- Slice 2B passed player UAT.
- Product and domain recalibration is captured.
- Slice 2C implementation and automated proof pass.
- Slice 2C's functional UAT journey passed on 2026-07-19.
- UAT exposed a Codex/Loadout text overlap. The layout and headless overlap
  regression check were corrected and the player accepted the visual recheck.
- Goal 2 is complete.
- Slice 3 and all later work remain forbidden.

Detailed authority: [Goal 2 — A Word Kept After Death](GOAL-2-A-WORD-KEPT.md),
especially “Slice 2C — Replace the Body,” its automated proof, UAT gate, and
goal-wide non-goals.

## Objective now

Preserve the accepted Goal 2 state and stop. Slice 3 is the next planned work,
but it requires explicit authorization in a new active gate before production
or test changes begin.

Do not change production code under this completed gate.

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

Stop for player UAT after this journey and all automated proof passes.

## Automated proof at hand

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
- `Chronicle.Core` builds and its checks run without Godot.

Exact commands and packaged executable paths remain in the
[Development guide](DEVELOPMENT.md).

## Do not drift into

- A second Verb, Noun, Study Source, Stone subject, or death cause.
- The mature Word Catalogue, generated word offers, or choice-based Study.
- Health, damage, combat, loot, inventory, equipment, or crafting.
- Combat, Explore, or Build Starting Vector selection.
- Home, Return Routes, Agents, Suggest, Command, factions, Pressure, records,
  raids, or off-camera simulation.
- Slice 3 World Grammar, Visual Grammar, final art, additional Strata, multiple
  Worlds, or Palimpsests.
- General registries, factories, event sourcing, migration frameworks, or
  interfaces justified only by future systems.

If an appealing idea falls in this list, record it in the appropriate canonical
document and return to Slice 2C.

## Known proof at hand

- `checks/verify.ps1` passed on 2026-07-19 with the packaged .NET SDK 8.0.423
  and official Godot 4.7.1 stable Mono release.
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

## Stop and hand off

Goal 2 is complete. Stop before Slice 3 and obtain explicit direction before
changing its status or beginning either Gate 3A or Gate 3B.

The next planned work is [Slice 3 — A World With Shape](SLICE-3-WORLD-VISUAL-GRAMMAR.md),
but this Handoff does not authorize it.
