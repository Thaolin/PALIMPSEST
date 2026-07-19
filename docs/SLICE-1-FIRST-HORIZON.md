# Slice 1 — First Horizon

## Status

Complete. Player acceptance rejected anchor-only Fly, so the Verb now works
from any surface or sky address. The revision also adds the minimal eight-slot
hotbar established for an Incarnation, without beginning Loadout management or
Slice 2.

## Outcome

A new player deliberately chooses `UP`, receives `Fly`, enters the sky at the
same coordinates from any surface address, reaches one visible Landmark, and
flies back to the surface.

## Player journey

1. The Chronicle opens at `surface (0, 0)` and presents the `UP` Intent.
2. The player selects `UP`. The Chronicle answers with `Fly`.
3. `Fly` appears in the first of eight visible hotbar slots.
4. The player flies to `sky (0, 0)`.
5. A generated sky patch is visible. A distinct Landmark lies four steps north.
6. The player moves north onto the Landmark and sees its arrival text.
7. The player moves south to `sky (0, 0)` and uses `FLY DOWN`.
8. The Chronicle returns to `surface (0, 0)` with `Fly` still granted.

No step is timed and the Chronicle may be paused throughout.

## Fixed product choices

| Concern | Slice 1 choice |
| --- | --- |
| Intent | `UP`, offered until deliberately selected |
| First Verb | `Fly` |
| Starting surface | `surface (0, 0)` |
| Starting sky address | `sky (0, 0)` |
| Fly rule | Between surface and sky at the same coordinates |
| Visible sky patch | 15 × 11 tiles centred on the Incarnation |
| Initial hotbar | Eight slots; `Fly` occupies the first |
| Landmark | **The Bell That Fell Up** at `sky (0, -4)` |
| Arrival line | “It hangs without support. It rings below you.” |
| Return | Walk to `sky (0, 0)`, then `FLY DOWN` |

The Landmark location is fixed so the first proof is short and legible. Seeded
cloud decoration keeps the sky visually Chronicle-specific. Later World
Grammar may vary Landmark placement; Slice 1 does not need that machinery.

## Opening presentation

Use this temporary in-game copy:

```text
THE FIRST HORIZON

The sky has no road.
Where do you intend to go?

[ UP ]
```

After selection:

```text
THE CHRONICLE ANSWERS: FLY
```

The wording may be polished later without changing any rule in this spec.

## Chronicle.Core contract

### State

Add one saved value:

```csharp
public enum OpeningIntent
{
    Unchosen = 0,
    Up = 1,
}
```

`ChronicleState` stores `OpeningIntent Intent`, defaulting to `Unchosen`.
`CanFly` is derived from `Intent == OpeningIntent.Up` and marked
`[JsonIgnore]`; it is not a second saved flag.

Do not add a Codex, Verb collection, Loadout, unlock registry, quest state, or
“sky unlocked” flag. Reaching the Landmark is derived from the current World
Address and is not saved separately.

### Commands

Add two concrete commands:

- `ChooseUpIntent`
- `FlyIncarnation`

`ChronicleSimulation` exposes `WorldAddress? FlyDestination`. It returns the
matching coordinate in the other Stratum or `null`. `FlyIncarnation` and the
Godot control use that same Core calculation; Godot must not repeat transition
or capability rules.

Rules:

- `ChooseUpIntent` changes `Unchosen` to `Up`.
- Repeating it leaves state unchanged.
- `MoveIncarnation` while Intent is `Unchosen` leaves state unchanged.
- `FlyIncarnation` without `CanFly` leaves state unchanged.
- At any surface address, `FlyIncarnation` moves to the sky at the same
  coordinates.
- At any sky address, `FlyIncarnation` moves to the surface at the same
  coordinates.
- At an address in any other Stratum, `FlyIncarnation` leaves state unchanged.
- Cardinal surface movement keeps its Slice 0 behavior.
- Cardinal sky movement keeps the same coordinate behavior as surface
  movement.

Remove the unrestricted `TravelChronicle` command. It would bypass the Fly
rule and has no production caller.

### Sky generation

Core generates a visible 15 × 11 sky patch from the Chronicle seed and current
sky address. It contains:

- a `Landmark` tile at `sky (0, -4)` when that address is visible;
- seeded `OpenSky` and `Cloud` decoration everywhere else.

Clouds are visual and traversable. There are no hazards, blockers, resources,
entities, physics, altitude levels, or stored generated tiles.

The same seed produces the same ordered sky tiles. Generated tiles are never
serialized.

## Godot contract

Godot continues to adapt input and render Core state:

- Show the opening panel modally while `Intent` is `Unchosen`; direction and
  Fly input are unavailable until the player selects `UP`.
- The `UP` button sends `ChooseUpIntent`.
- Show `First Verb: Fly` after selection.
- Show a small eight-slot hotbar below the world view. Its first slot contains
  `FLY UP` or `FLY DOWN` from Core's current Fly destination; the remaining
  seven slots are visibly empty.
- The Fly slot sends `FlyIncarnation`; Godot does not change Strata directly.
- This hotbar is presentation scaffolding only. Slice 1 does not serialize
  slots or add Codex, equip, rearrange, or acquisition rules.
- Use the existing direction, clock, save, and load controls.
- Render surface state with the existing surface view.
- Render the current generated sky patch with a small dedicated sky view. The
  player marker remains centred as the visible patch changes.
- Draw The Bell That Fell Up as a high-contrast gold ring with a dark center.
- When Core's generated current tile is the Landmark, show its name and arrival
  line.

An additional keyboard binding is optional. The on-screen Fly slot is the
required control and proof.

## Save compatibility

- Keep `user://slice0_chronicle.json`; renaming it would require migration with
  no player benefit.
- A Slice 0 save with no `Intent` field loads as `Unchosen`.
- Choosing `UP`, saving in the sky, loading, quitting, and relaunching restore
  seed, tick, speed, address, and Intent.
- The visible sky patch, clouds, and Landmark regenerate from Core rules.

## Automated Core proof

Extend the existing dependency-free check executable to prove:

1. A literal Slice 0 JSON save loads with `Intent == Unchosen`.
2. Serialized JSON includes `Intent` but does not include derived `CanFly`.
3. Movement before choosing `UP` does not change state.
4. `ChooseUpIntent` grants `CanFly` and is deterministic when repeated.
5. `FlyDestination` is `null` before `UP`, preserves coordinates while naming
   the opposite surface/sky Stratum, and is `null` in unsupported Strata.
6. Fly before choosing `UP` does not change state.
7. Fly from an arbitrary surface address performs an exact coordinate-preserving
   round trip.
8. Fly from a second arbitrary address is deterministic.
9. A sky patch contains 165 ordered tiles centred on its requested address and
   includes the Landmark exactly when visible.
10. The same seed and centre produce the same sky; a different seed changes
    cloud decoration.
11. Sky movement crosses the former Slice 1 boundaries deterministically.
12. Four north moves from `sky (0, 0)` reach the Landmark.
13. Four south moves and Fly return to `surface (0, 0)`.
14. The same seed and command/pulse stream replay to the same final state.
15. Save/load in the sky restores complete state and regenerates the same sky.
16. Seed bits in either 32-bit half affect generated sky decoration.
17. Generation, movement, and save/load cross the former 32-bit coordinate
    boundary without wrapping.
18. Every existing Slice 0 check still passes.

Chronicle.Core must build, and the Core check executable must run, without
Godot.

## Player-visible acceptance script

Use a fresh or reset save:

1. Launch and confirm the surface patch, address, clock, modal `UP` panel, and
   unavailable movement.
2. Select `UP`; confirm `First Verb: Fly` and the eight-slot hotbar with
   `FLY UP` first.
3. Fly up; confirm `sky (0, 0)`, the centred marker, and visible Bell.
4. Move north four times; confirm `sky (0, -4)` and the Landmark arrival text.
5. Save.
6. Move away, change speed, then load; confirm the exact sky state returns.
7. Return south four times and fly down.
8. Quit and relaunch; confirm Intent, Fly, address, speed, and generated view
   restore.

The flow must be understandable from the application itself, without reading
this document.

## Likely implementation paths

Core and checks are one coupled task:

- `src/Chronicle.Core/ChronicleState.cs`
- `src/Chronicle.Core/ChronicleSimulation.cs`
- new `src/Chronicle.Core/SkyStratum.cs`
- `checks/Chronicle.Core.Checks/Program.cs`

Godot follows the stable Core contract:

- `src/Chronicle.Godot/ChronicleApp.cs`
- new `src/Chronicle.Godot/SkyStratumView.cs`
- `README.md`
- `docs/DEVELOPMENT.md`

`Main.tscn`, project files, and `SurfacePatchView.cs` should remain unchanged
unless implementation proves that necessary. Do not create a shared renderer
or generic Stratum graph for two concrete views.

## Definition of done

Slice 1 is complete only when:

1. All Slice 0 and Slice 1 Core checks pass.
2. Chronicle.Core builds and the Core checks run without Godot.
3. The Godot C# project builds.
4. Headless Godot startup has no scene, script, or resource errors.
5. The complete player-visible acceptance script passes in the actual app.
6. A pre-Slice-1 save still loads.
7. Godot contains no transition, generation, or capability rules.
8. Slice 2 systems have not begun.

## Non-goals

Do not add:

- additional Intents, Starting Vectors, First Verbs, or Landmarks;
- a generic Intent, Verb, Loadout, passage, graph, quest, or World Grammar
  framework;
- Codex, Nouns, Study, Expressions, Loadout management, death, or replacement
  Incarnations;
- Agents, factions, Holdings, routes, raids, combat, inventory, resources, or
  hazards;
- additional Strata, altitude simulation, whole-world sky simulation, or
  polished art.
