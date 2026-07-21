# Goal 4C UAT — A Consequential Fight

## Status

**Accepted on 2026-07-20.** The fixture and seams were approved, implementation
completed, the complete automated gate passed, and the player reported:
“All pass.”

UAT retained two non-blocking alpha notes:

- the local view still needs a separately scoped closer-zoom comparison;
- the Riven Cairn was extremely difficult to discern and needs stronger visual
  hierarchy in a later visual pass.

The player also observed that movement remains available while paused. That is
the accepted rule, not a defect: pause freezes Chronicle ticks, autonomous
objects, and time-driven presentation while deliberate commands—including the
retreat exercised in step 7—remain responsive.

## What this UAT decides

4C passes only if the first fight feels like a deliberate confrontation with
a dangerous part of the world. It must not feel like a reflex test, a separate
combat screen, a one-button loot dispenser, or the foundation of a generic
health-and-damage system.

The player should be able to explain:

- why `AGAINST` is a Combat Starting Vector rather than a class;
- what `Smash` permits through the normal Codex and Loadout;
- what the River-Ward will do on the next active Chronicle tick;
- why pause creates deliberation rather than safety without consequence;
- what choice was prepared before time resumed; and
- what material fact remained after the exchange.

## Isolated launch

Launch a new isolated 20-pixel Chronicle:

```powershell
.\play.ps1 -Fresh
```

Keep the printed profile name if the journey must be resumed. Do not use the
normal player save for this gate.

## Success journey

1. Confirm the opening offers `AGAINST — COMBAT`, `UP — EXPLORE`, and
   `HERE — BUILD` together.
2. Choose `AGAINST`. Confirm the Codex gains authored Verb `Smash`, slot one
   fits intrinsic `SMASH`, and nothing identifies the body as a permanent
   class.
3. Follow the Chronicle Thread to **The Riven Cairn**. For seed `41337`, the
   exact Address is `surface (1, 3)`, on dry Stone one cell east of the accepted
   Home fixture.
4. Before entry, confirm the Cairn reads as a distinct world subject rather
   than an icon floating over unrelated terrain.
5. Enter the Cairn. Confirm the Clock pauses before Tick advances and the
   interface names **The River-Ward**, explains its flood/ford history, and
   states plainly that the next active tick will end this body unless a valid
   action is prepared.
6. Wait while paused. Confirm neither the threat nor Tick resolves, and that
   the River-Ward, Cairn, danger phase, and any time-driven object presentation
   remain frozen even though menus and selection feedback remain responsive.
7. Leave while paused, then return. Confirm retreat is possible and the intact
   ward remains.
8. Use intrinsic `SMASH` through the existing hotbar. Confirm it becomes the
   pending action but the Cairn has not shattered yet.
9. Save and reload the isolated profile. Confirm the paused threat and pending
   `Smash` return exactly.
10. Resume the Clock. Confirm the very next Chronicle tick resolves the
    prepared action, leaves the Incarnation alive at the same Address, and
    changes the world subject to **The Shattered Cairn**.
11. Leave and return. Confirm there is no second fight, loot screen, experience
    reward, or new Study Source.
12. Save, close, and relaunch the same profile. Confirm the Shattered Cairn
    remains and the underlying Soil/Stone ridge, loose Stone, Codex, Loadout,
    and Chronicle facts restore.
13. Confirm the accepted 20-pixel playspace still reads clearly and no new
    combat panel crowds the map.

The automated gate separately proves the intentional no-action branch: resume
without prepared `Smash`, lose the body on that first tick, create a
replacement with retained `Smash` and an empty Loadout, and find the River-Ward
still intact.

## Accept if

- time stops before danger can resolve;
- world objects and their time-driven presentation visibly stop with it;
- the exact danger and consequence are understandable without documentation;
- retreat, preparation, and resumption are deliberate;
- the existing Codex, Loadout, hotbar, Clock, and world view carry the whole
  encounter;
- the next-tick outcome is deterministic;
- Shattered is a visible, persistent material consequence; and
- the slice remains one authored confrontation rather than a hidden combat
  framework.

## Reject if

- a frame, animation, or button-speed test can kill the player before they can
  pause and understand;
- `Smash` works because `AGAINST` was chosen rather than because the Verb is
  equipped;
- the action resolves immediately instead of on the stated Chronicle tick;
- Godot owns the warning, eligibility, timing, or result;
- the Cairn resets after leaving, save/load, restart, death, or replacement;
- the fight introduces health, damage values, turns, AI, loot, a bestiary, or
  a combat screen; or
- 4C absorbs the camera zoom, water traversal, route arrow, P-GEN, or Goal 5
  work.

## Stop boundary

This pass closed Slice 4C and Goal 4 after the Goal 4 contract, Roadmap,
Codemap, Development guide, and Handoff were reconciled. It did not authorize
Goal 5 or the deferred visual work.
