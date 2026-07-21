# Goal 2 — A Word Kept After Death

## Status

Complete on 2026-07-19. Slice 2A, Slice 2B, and Slice 2C each passed automated
proof and separate player UAT. Slice 2C's Codex/Loadout overlap was corrected
and visually accepted after the complete verification gate passed again. Goal
2 is closed; Slice 3 has not begun.

## Goal outcome

The player deliberately studies the Chronicle, learns the Noun `Stone`, makes
one meaningful `Fly[Stone]` build choice, loses an Incarnation, and proves that
the Chronicle, Codex, and a material world change outlive the body.

This is one planning goal, not permission to implement all three slices without
stopping.

```text
Slice 1 complete
  → 2A Study a Word
  → UAT
  → 2B Make a Word Active
  → UAT
  → 2C Replace the Body
  → UAT
  → Slice 3 World + Visual Grammar
```

## Fixed product choices

| Concern | Goal 2 choice |
| --- | --- |
| First Noun | `Stone` |
| Study source | The sky-stone clapper inside The Bell That Fell Up |
| Study pacing | 16 deterministic Chronicle ticks, shown as four visible segments |
| Loadout size | Eight slots |
| Base Verb form | `Fly` moves the Incarnation between matching surface/sky coordinates |
| First Expression | `Fly[Stone]` moves an adjacent Stone subject between matching surface/sky coordinates |
| First target | One visible loose Stone at `surface (1, 0)` |
| Death | A deliberate, confirmed interaction with the Bell |
| Replacement origin | `surface (0, 0)` |
| Replacement time | The Chronicle does not advance while awaiting replacement |

`Fly` and `Fly[Stone]` are two configurations of one learned Verb. The same
Verb cannot occupy two Loadout slots. An empty Noun socket preserves the Verb's
intrinsic form; adding `Stone` changes its subject and therefore its effect.

Temporary source copy:

```text
The Bell is gold, but its clapper is a dark stone veined with open sky.
It rises against the curve that contains it.

[ STUDY SKY-STONE ]
```

The eight-slot limit is real and serialized, but Goal 2 does not pretend it is
capacity pressure while the Codex is small. Later slices earn that pressure by
adding language, not by inventing filler.

Slice 2A's Bell is a deliberately one-candidate Study foundation: it applies
Understanding only to `Stone` and hides that word until completion. The mature
discovery loop is now settled more broadly: a generated Study Source exposes a
small selection of plausible words from the authored Word Catalogue, the
player chooses what to pursue, and the source contributes word-specific
Understanding according to its rarity, danger, and significance. Some sources
may support more than one choice. Do not retrofit that expansion into Goal 2;
a later vertical slice must replace the source-specific scaffold and prove the
choice experientially.

## Module seams

Keep `ChronicleSimulation.Apply(ChronicleCommand)` as the single Core command
interface. Add concrete state and commands behind it rather than introducing
registries, factories, or Godot-facing rule interfaces.

Chronicle-level state owns:

- the seed, clock, and opening Intent;
- the durable Codex;
- Study understanding;
- generated-world deltas, including the moved Stone;
- the current or pending Incarnation.

Incarnation-level state owns:

- a durable Incarnation identity;
- alive or awaiting-replacement status;
- current World Address;
- the eight-slot Loadout.

Godot shows these snapshots, translates selections into Core commands, and
renders the results. It does not decide Study progress, compatibility,
target validity, death, replacement, or what survives.

## Slice 2A — Study a Word

### Player-visible hypothesis

Learning a word should feel like understanding something in the world, not
receiving an automatic level reward.

### Journey

1. Load a Slice 1 Chronicle with `Fly`.
2. Fly to The Bell That Fell Up.
3. Choose `STUDY`.
4. See `Stone` understanding begin below completion.
5. Pause; confirm understanding stops.
6. Save, move or quit, then load; confirm partial understanding returns.
7. Resume Study until `Stone` enters the Codex.
8. Study the Bell again; confirm the Codex does not receive a duplicate.

### Rules

- `Fly` becomes an explicit Codex Verb rather than a second ability flag.
- Slice 2A keeps the existing first hotbar action available from learned
  `Fly`. Slice 2B replaces that compatibility behavior with the living
  Incarnation's serialized active Loadout.
- A Slice 1 save with `Intent == Up` migrates to a Codex containing `Fly`.
- Study can begin only at a valid source and while an Incarnation is alive.
- Active Study advances only on Chronicle ticks while the Incarnation remains
  at the source.
- Understanding advances from 0 to 16 ticks and is shown as four segments.
  Reaching tick 16 completes the word; selected clock speed changes wall-clock
  waiting time but not the required Chronicle work.
- Pause prevents Study advancement because it prevents ticks.
- Leaving the source stops active Study but preserves accumulated
  understanding.
- Reaching the fixed threshold adds `Stone` to the Codex exactly once.
- Repeating a completed Study changes nothing.
- Study progress and the Codex are saved; generated presentation is not.

The first implementation needs one concrete source and one concrete Noun. Do
not create a generic unlock, quest, research-tree, experience, or content
authoring system.

### Godot proof

- Add a small Codex panel listing learned Verbs and Nouns.
- At the Bell, show a `STUDY` action and visible progress.
- Clearly distinguish partial understanding from a learned Codex entry.
- Keep movement, clock, save/load, and Fly controls working.

### Automated proof

Prove:

1. A literal Slice 1 save migrates to a Codex containing `Fly`.
2. The same Study/tick stream produces the same understanding.
3. Study cannot begin away from the Bell.
4. Pause prevents understanding advancement.
5. Leaving the Bell stops active Study without losing progress.
6. Save/load restores partial Study exactly.
7. Completion adds `Stone` once.
8. Further Study cannot duplicate or regress the Noun.
9. The Codex and Study state serialize without generated tiles.
10. Every Slice 0 and Slice 1 check still passes.

### UAT gate

Stop before 2B unless the player can answer, without documentation:

- What am I studying?
- How far have I progressed?
- What did I learn?
- Where can I find the learned word later?

Reject the slice if Study feels automatic, invisible, grindy, or like an XP
bar detached from the Bell.

## Slice 2B — Make a Word Active

### Player-visible hypothesis

Owning language in the Codex is not the same as making it active in this body.
A Loadout choice must change what the hotbar action does.

### Journey

1. Open the Codex/Loadout view.
2. See exactly eight Loadout slots and `Fly` in the Codex.
3. Unequip `Fly`; confirm the first hotbar slot becomes empty and self-flight
   is unavailable.
4. Equip intrinsic `Fly`; confirm self-flight returns.
5. Fit `Stone` into Fly's Noun socket; confirm the slot reads `FLY[STONE]`.
6. Stand beside the loose Stone at `surface (1, 0)` and use the slot on it.
7. Confirm the Incarnation remains on the surface while the Stone moves to
   `sky (1, 0)`.
8. Restore intrinsic `Fly`, fly up, and see the same Stone at `sky (1, 0)`.
9. Save/load and confirm the Loadout and moved Stone return exactly.

### Rules

- A Loadout contains exactly eight ordered slots.
- A slot is empty or contains one Codex Verb with an optional compatible Codex
  Noun.
- A learned Verb may appear in at most one slot.
- `Fly` with no Noun acts on the current Incarnation.
- `Fly[Stone]` requires one adjacent Stone target and acts on that target.
- `Fly[Stone]` preserves the Stone's coordinates while changing its Stratum.
- The same Expression can return the Stone from sky to surface.
- Using an empty, unknown, incompatible, unavailable, distant, or incorrectly
  typed target leaves state unchanged and reports a legible rejection.
- The loose Stone is a single durable subject, not an inventory item, resource
  stack, physics body, or generic entity framework.
- Goal 2 exposes only that discrete loose Stone as a target. Stone terrain
  remains semantic ground until a later slice defines how terrain-scale Noun
  targeting works.
- Godot sends slot and target intent to Core; it does not implement Expression
  behavior or compatibility.

The hotbar becomes a view of the serialized Loadout. Do not maintain a second
Godot-only list.

### Minimal Core commands

- configure one Loadout slot;
- clear one Loadout slot;
- use one Loadout slot with an optional target World Address.

These commands are the interface. Concrete `Fly` and `Fly[Stone]` rules remain
inside the simulation implementation.

### Godot proof

- Reuse the existing eight-slot hotbar.
- Add the smallest workable Codex-to-Loadout configuration panel.
- When `Fly[Stone]` is selected, highlight valid adjacent Stone targets.
- A click or cardinal selection sends the target address through a Core
  command.
- Do not add drag-and-drop, tooltips for a large vocabulary, controller
  rebinding, inventory, or a general targeting mode.

### Automated proof

Prove:

1. Loadouts always contain exactly eight serializable slots.
2. Only Codex language can be equipped.
3. A Verb cannot occupy two slots.
4. Compatibility is deterministic and Core-owned.
5. Unequipped `Fly` cannot move the Incarnation.
6. Intrinsic `Fly` preserves the Slice 1 transition rule.
7. `Fly[Stone]` moves only an adjacent Stone subject.
8. The Incarnation does not move when `Fly[Stone]` succeeds.
9. The same Expression moves the Stone from sky back to surface.
10. Invalid targets leave all state unchanged.
11. Save/load and deterministic replay restore the Loadout and Stone address.
12. Godot's hotbar invokes the slot command rather than a direct transition.
13. Every earlier check still passes.

### UAT gate

Stop before 2C unless the player can:

- explain the difference between Codex and Loadout;
- make Fly disappear and deliberately restore it;
- predict what `Fly[Stone]` will affect;
- observe a material world change caused by the Expression.

Reject the slice if `Fly[Stone]` is only a renamed Fly, if unequipped language
still works, or if Godot owns any compatibility or targeting rule.

## Slice 2C — Replace the Body

### Player-visible hypothesis

Death ends a body without ending the player's learned language or erasing the
Chronicle it changed.

### Journey

1. Move the loose Stone to `sky (1, 0)` and save.
2. Reach The Bell That Fell Up.
3. Configure the current body with `Fly[Stone]`.
4. Deliberately ring the Bell and confirm the death choice.
5. See that the Incarnation has ended and the Chronicle is waiting, not
   advancing under time pressure.
6. Create the replacement Incarnation at `surface (0, 0)`.
7. See the same Codex containing `Fly` and `Stone`, but a fresh empty
   eight-slot Loadout.
8. Configure intrinsic `Fly`, enter the sky, and confirm the moved Stone is
   still at `sky (1, 0)`.
9. Save, quit, and relaunch; confirm the replacement identity, Codex, Loadout,
   Study, Stone, seed, tick, and speed.

### Rules

- Ringing the Bell is the only death cause in Goal 2.
- It is deliberate, confirmed, deterministic, and requires no reflex.
- Death changes the current Incarnation to an awaiting-replacement state.
- Movement, Study, Loadout use, and targeting are unavailable while no living
  Incarnation exists.
- Chronicle ticks do not advance while awaiting replacement, regardless of
  selected speed. Replacement restores play at the previously selected speed.
- Replacement creates a new durable Incarnation identity at `surface (0, 0)`.
- The replacement has a fresh empty eight-slot Loadout.
- Seed, tick, speed, Intent, Codex, Study, and world deltas persist.
- The moved Stone is the material proof of continuity; do not add combat,
  health, corpses, loot, Return Routes, or Chronicle Records to strengthen the
  demonstration.

### Save compatibility

- Goal 2 introduces a minimal versioned save envelope and explicit migration
  functions. It does not introduce event sourcing or a general migration
  framework.
- A Slice 0 save still loads as `Intent == Unchosen`.
- A Slice 1 save with `UP` gains Codex `Fly`, a living first Incarnation at its
  saved address, and intrinsic `Fly` in slot one.
- A 2A save gains the same first-Incarnation Loadout without losing Study or
  Codex state.
- A 2B save gains a living first Incarnation without changing its Loadout or
  Stone delta.
- Keep literal predecessor JSON fixtures in the Core checks. Do not depend on
  Git history.

### Automated proof

Prove:

1. Death is allowed only for a living Incarnation at the Bell.
2. The same death command produces the same awaiting-replacement state.
3. Movement, Study, Loadout configuration/use, targeting, repeated death, and
   ticks leave state unchanged while awaiting replacement.
4. Only the replacement command may leave the awaiting-replacement state.
5. Replacement increments Incarnation identity deterministically.
6. Replacement starts at `surface (0, 0)` with eight empty slots.
7. Codex, Study, seed, tick, speed, Intent, and world deltas survive.
8. The moved Stone remains at its changed World Address.
9. The dead body's Loadout does not leak into the replacement.
10. Save/load works both before replacement and after replacement.
11. Replay across Study, Expression use, death, and replacement is identical.
12. Every earlier check still passes.

### UAT gate

Goal 2 is complete only if the player can say:

- “That body died.”
- “My words did not.”
- “The new body has to choose what it can use.”
- “The world still contains what the first body changed.”

Reject the goal if death resembles loading a checkpoint, if the replacement
inherits an active build automatically, or if any timer pressures the
replacement decision.

## Goal-wide non-goals

Do not add:

- a second Verb or Noun;
- more than one Study source, Stone subject, or death cause;
- capacity upgrades from eight to ten slots;
- inventory, item stacks, equipment, crafting, health, damage, combat, or
  physics;
- Fallback Class choice, Return Routes, corpses, Chronicle Records, Agents,
  factions, Holdings, Pressures, raids, or World Claims;
- World Grammar or Visual Grammar implementation before all three Goal 2 UAT
  gates pass.

## Definition of done

Goal 2 is complete only when:

1. 2A, 2B, and 2C each pass automated checks and separate player UAT.
2. Chronicle.Core remains runnable without Godot.
3. Godot's editor build callback and headless acceptance pass at every gate.
4. The complete journey saves and restores across a real application restart.
5. Literal Slice 0 and Slice 1 saves still load.
6. No rule, compatibility decision, generation rule, or persistence rule is
   duplicated in Godot.
7. Slice 3 systems have not begun.
