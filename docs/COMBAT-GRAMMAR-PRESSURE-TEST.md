# Combat Grammar Pressure Test

**Authorized:** 2026-07-21
**Status:** active isolated logic prototype; not production

## Question

Does a small tick-based fight become fun when physical equipment, autonomous
help, and a linked `Burn` Expression compete for time, safety, and shared Load?

The prototype exists to make the player feel the timing decisions directly. It
does not prove production architecture, final balance, persistence, or content
breadth.

## Hypothesis

A fight is interesting when the player can:

- rely on a Weapon while an Invocation prepares or recovers;
- trade `Quickly` against a longer-burning Modifier under shared Load;
- abandon Preparation when immediate survival matters;
- let one autonomous Companion screen the caster without becoming a unit;
- retreat without clearing Recovery; and
- understand why `Burn` can affect one Target but not another.

If these choices feel flat in a transparent terminal model, building their
production framework would be premature.

## Isolated artifact

Build a clearly marked C# console prototype outside every production project.
It has in-memory state only, a pure deterministic combat model, and a thin
interactive terminal shell that redraws the full state after every command.

Run it with one command from the repository root:

```powershell
& .\prototype-combat.ps1
```

The prototype must not enter the solution, Chronicle saves, World Grammar,
Godot, or the retained verification gate.

## Fixed fixture

These values are pressure-test inputs, not balance promises:

- **Equipment:** one Iron Cleaver Weapon, Quilted Jack Armor, and Copper Ward
  Accessory.
- **Incarnation:** 30 base HP; the Accessory adds 4 HP. Armor mitigates 2 damage
  from each physical hit.
- **Weapon:** 5 damage every 2 Chronicle ticks while attacking and not occupied
  by Preparation.
- **Hostile:** one Mire Brute with 45 HP, dealing 7 physical damage every 3
  ticks.
- **Companion:** one optional Ash Hound with 20 HP, dealing 3 damage every 2
  ticks. While screening, the Brute attacks it before the Incarnation. It acts
  autonomously.
- **Targets:** the flammable Mire Brute and a nonflammable Basalt Cairn. Preview
  explains Target facts; it never prescribes a Word recipe.
- **Load:** 8 available. `Burn` costs 1, `Quickly` costs 6, and `Lasting` costs
  5. Both Modifiers cannot fit together.
- **Base Burn:** 3 ticks of Preparation, then 4 fire damage for 3 ticks, followed
  by 8 ticks of Recovery.
- **Burn + Quickly:** 1 tick of Preparation with the base duration and damage.
- **Burn + Lasting:** base Preparation and damage for 6 ticks.

`Burn` Recovery advances with the Chronicle Clock whether or not the Brute is
currently threatening the Incarnation. Safe skipping may advance to Recovery
completion; retreat never clears or restarts it.

## Required interactions

The terminal must let the player:

- advance one Chronicle tick;
- start or stop repeated Weapon attacks;
- start or abandon `Burn` Preparation;
- cycle the base, `Quickly`, and `Lasting` builds by resetting the fixture;
- inspect and select either Target;
- add or remove the fixed Companion by resetting the fixture;
- retreat, re-engage, and skip safe Recovery time; and
- reset or quit.

Every frame shows HP bars, equipment, Load, selected Expression and Target,
Preparation, Recovery, burning duration, attack readiness, danger, Companion
state, durable scorch state, and the latest event.

## Pressure-test journey

1. Fight once with `Burn + Quickly` and no Companion.
2. Fight once with `Burn + Lasting` and the Companion screening.
3. During one Preparation, abandon it to resume physical fighting.
4. Retreat during Recovery, re-engage before it completes, and confirm the
   remaining Recovery was preserved.
5. Select the Basalt Cairn and confirm factual rejection without a missing-Word
   recipe.
6. Decide whether at least two builds create defensible, meaningfully different
   play rather than one obvious answer.

## Pass evidence

The pressure test passes only when the player reports that:

- tick timing creates decisions rather than passive waiting;
- Weapon attacks and `Burn` remain mutually relevant;
- `Quickly` and `Lasting` produce different desirable plans under shared Load;
- the Companion changes the viable casting plan without requiring unit control;
- HP and readiness are legible enough to predict danger; and
- the player wants to retry with another build.

An engineering success or clean terminal interaction is insufficient. Player
judgment is the gate.

## Forbidden drift

- no production combat, equipment, Agent, Expression, Target, or Recovery code;
- no save migration or persistence;
- no Godot UI or visual polish;
- no inventory, loot, crafting, bestiary, healing economy, damage types, or
  general statistics framework;
- no final balance conclusion from the fixture numbers; and
- no successor implementation contract until player UAT passes and a separate
  production slice is authorized.

## Stop condition

When the prototype runs and the retained baseline still passes, stop and hand
the command and journey to the player. Do not interpret automated execution as
player acceptance.
