# Goal 4B UAT — A Place Called Home

## Status

**Accepted on 2026-07-20.** Implementation and automated proof completed on
2026-07-19. The player reported that every functional journey item passed,
then found one layout rejection: the fifth-line Clock speed was covered by the
Codex panel. The correction compacts Tick and Clock onto one line and adds a
headless four-line layout assertion. The complete retained verifier passed
again, and the player confirmed the focused visual recheck looks good.

Player acceptance is the slice boundary. Do not begin 4C until this sheet is
completed, the trackers are reconciled, and the player separately authorizes
the next slice.

## What this UAT decides

4B passes only if Home feels like a singular place established through the
player's language and Chronicle matter. It must not feel like a saved
coordinate, respawn setting, management screen, or teleport destination.

The player should be able to explain:

- why `HERE` is a Build Starting Vector rather than a class;
- why `Found` fails at the water-covered starting ridge;
- what makes the supported Stone at `surface (0, 3)` valid;
- what materially changed when the Stone became **The First Hearthstone**;
- how the Return Route guides ordinary movement without performing it;
- what survives save and a real application restart;
- why Home remains useful to a player who mostly explores;
- whether the enlarged playspace now outweighs the text and menu regions.

## Required automated boundary

Before interactive review, the full verifier must end with the required 4B
marker and retained-regression pass. The confirmed marker is:

```text
GOAL4B ACCEPTANCE PASS home=surface:0,3 material=hearthstone route=physical view=50x36 save=3
```

The final verifier must also end with:

```text
PASS: Goal 4B Home and restart acceptance, Goal 4A Study choice, every Goal 2 regression, Gate 3A Inspector, and Gate 3B visual acceptance verified.
```

It retains the complete Goal 2, Gate 3A, Gate 3B, and Goal 4A proofs. A passing
focused 4B command is not enough.

## Isolated 20-pixel launch

Close other Godot instances. From the repository root, use an isolated UAT
save so the interactive Chronicle and the accepted 4A UAT remain untouched:

```powershell
& .\play.ps1 -Profile goal4b-uat-ready
```

The launcher builds the current C# project, applies the packaged .NET
environment, selects the default 20-pixel pack, and maps this profile to
`.tools/play-profiles/goal4b-uat-ready/`. Relaunch with the same command to
continue the same isolated Chronicle. If that profile already contains a prior
run, choose a new profile name rather than deleting evidence during review.
The accepted fixture depends on the fresh-Chronicle default seed, so do not
continue unless the application readout visibly says `Seed: 41337`.

The current runtime is a `1600 × 900` canvas with a `1020 × 740` map. This
20-pixel UAT presents an actual `51 × 37` view; the required automated marker
remains the lower bound `view=50x36`. The retained 16-pixel presentation is
`63 × 45`, but it is not this Home UAT's acceptance candidate.

## Player journey

1. Before choosing an opening, confirm the Chronicle readout says
   `Seed: 41337`. Stop if it does not; the coordinates below are not a
   seed-independent fixture.
2. On **THE FIRST HORIZON**, confirm both `UP — EXPLORE` and
   `HERE — BUILD` are visible as nonbinding Starting Vectors.
3. Choose `HERE`. Confirm the Chronicle answers `FOUND`, `Found` enters the
   Codex once, and intrinsic `FOUND` occupies hotbar slot one.
4. Before using any other control, confirm the 20-pixel world view shows the
   actual `51 × 37` cells, satisfies the required `50 × 36` minimum, and is
   materially larger than the text/menu region.
5. At `surface (0, 0)`, use `FOUND`. Confirm Core explains that the generated
   Stone is still under water, Home is not created, no material identity
   appears, and the Incarnation does not move.
6. Move south exactly three ordinary steps to `surface (0, 3)`. Confirm the
   current-site readout identifies supported generated Stone without claiming
   Home already exists.
7. Use `FOUND` with no target. Confirm:
   - the Incarnation remains at `surface (0, 3)`;
   - singular Home appears as **THE FIRST HEARTH**;
   - the founding Address, Chronicle tick, and Incarnation identity are
     visible;
   - the existing ridge Stone now reads as **The First Hearthstone**;
   - the underlying soil and Stone ridge still look continuous;
   - the separate loose Stone was not consumed or moved.
8. Use `FOUND` again. Confirm the singular-Home rejection is legible and
   neither identity nor material state changes.
9. Move north three ordinary steps back to `surface (0, 0)`. At each step,
   confirm the Return Route points south toward Home and decreases from three
   steps to zero only when you physically walk south again.
10. While away from Home, save, close the application, and relaunch with the
    same command. Confirm the exact Home identity, Address, founding facts,
    Hearthstone, current Incarnation Address, and next Return Route step return.
11. Follow the displayed route using only ordinary south movement. Confirm
     arrival at Home does not open a management screen or advance the Chronicle
     by a hidden action.
12. Leave again and continue a short surface expedition. Confirm the modest
     Hearthstone remains an anchor without demanding residents, production,
     storage, crafting, jobs, or maintenance.
13. Save, close, and relaunch once more. Confirm Home and its material mark
     remain exact.

## Acceptance record

- [x] `HERE` read as a Build opening, not a permanent class.
- [x] The invalid starting site and valid supported-Stone site were legible.
- [x] `Found` established exactly one Home through the Loadout.
- [x] The First Hearthstone read as a persistent change to existing matter.
- [x] Founding did not move the Incarnation or rewrite generated terrain.
- [x] A repeated founding attempt changed nothing.
- [x] The Return Route guided every physical step and never moved me.
- [x] Save/load and a real restart preserved exact Home and route facts.
- [x] Home remained a modest expedition anchor rather than a management mode.
- [x] The actual `51 × 37` view, satisfying the `50 × 36` minimum, materially
  outweighed the text/menu regions.
- [x] The prior 20-pixel visual readability remained acceptable for the alpha.
- [x] **Accept 4B**
- [ ] Reject 4B

Notes:

```text
2026-07-20 functional UAT:
- All journey items passed. The enlarged playspace looks good for the alpha.
- More world is visible, but individual detail feels less prominent. Test an
  optional roughly 30% closer camera later while retaining the accepted
  20-pixel source pack; do not change the 4B baseline without comparison UAT.
- The flooded starting cell was understood as intentional. Walking through
  water without an appropriate traversal capability reads incorrectly and
  needs a future terrain/traversal rule; it is not a 4B Home rejection.
- The textual Return Route works. A directional arrow may communicate its next
  step more directly in a later UI-bearing slice.

Post-pass layout rejection:
- The Clock speed line was visibly covered by the Codex panel.
- Correction: Tick and Clock now share the third readout line; a headless
  assertion requires Clock to remain within a four-line readout.
- Focused recheck passed on 2026-07-20: the corrected
  "Tick: ... · Clock: ..." line remains visible above CODEX and looks good.
```

## Stop condition

Accepted and reconciled on 2026-07-20 with the Goal 4 contract, Roadmap,
Codemap, Development guide, and Handoff. Work stops here. Slice 4C still
requires separate authorization.
