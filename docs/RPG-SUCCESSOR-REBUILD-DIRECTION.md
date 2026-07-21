# RPG Successor Rebuild Direction

**Decided:** 2026-07-21
**Status:** settled sequencing and retention boundary; production not authorized

The successor keeps the proven Chronicle substrate through Slice 3 and rebuilds
the player-facing RPG on top through bounded vertical slices. This is neither a
new repository nor an attempt to preserve every accepted predecessor rule.

Canonical authority remains [AGENTS.md](../AGENTS.md), the
[glossary](../CONTEXT.md), [Vision](VISION.md),
[Architecture](ARCHITECTURE.md), [Roadmap](ROADMAP.md), and active
[Handoff](HANDOFF.md). This direction document does not authorize production
changes or a save migration.

## Retained foundation

The successor retains and deepens these accepted foundations:

- the engine-independent C# Chronicle and thin Godot presentation seam;
- deterministic fixed Heartbeats, pause and speed controls, and the future
  calendar and day-night cycle derived from the same Chronicle Clock;
- Chronicle seed, World Address, Strata, bounded deterministic World Grammar,
  generated Landmarks, persistent deltas, and version-pinned regeneration;
- the developer World Atlas Inspector and the shared player/Inspector semantic
  snapshot path;
- the accepted 20-pixel visual scale, restrained palette, symbolic map
  language, compiled visual-pack seam, deterministic Visual Grammar, and Godot
  drawing adapters;
- strict persistence and literal predecessor migration as engineering
  disciplines, although the successor save shape will require its own contract;
  and
- P-GEN as the required authoring-time visual asset compiler behind a
  Palimpsest-owned compiled-pack reader, never as a gameplay or World Grammar
  runtime.

This is the bedrock. The successor must extend these modules through their
existing interfaces rather than create a second simulation, generator,
composer, or rendering path.

## Redesign surface

The following accepted predecessor implementation is evidence, not a shape the
successor must preserve:

- collectible Nouns and fitted `Fly[Stone]` / `Fly[Bell]` Loadout semantics;
- the Bell-specific Study fixture and its tiny catalogue;
- the one-exchange Riven Cairn conflict;
- current Starting Vector presentation and onboarding details;
- the fixed Home construction fixture beyond Home's settled identity and role;
  and
- any v5 UI composed specifically around those rules.

These rules remain supported until an authorized migration replaces them. Do
not delete them speculatively, reinterpret old saves, or layer successor rules
beside them indefinitely.

## Spatial north star

Open territory is not a symbolic strategic board. The Chronicle uses one
character-scale grid for wilderness, Holdings, combat, sprawling generated
dungeons, temples, ruins, caves, and nested stranger spaces. Distinct Areas are
connected by persistent Passages and share the same movement, Combat,
Heartbeat, building, Target, and material rules.

Large geography is composed rather than miniaturized: a mountain contains
foothills, slopes, cliffs, passes, caves, and peaks across many cells. World
Grammar may generate arbitrarily many creature instances from authored
Creature Grammar and preserve identity when an instance becomes consequential.
This direction is recorded by
[ADR 0005](adr/0005-use-one-character-scale-across-generated-areas.md); its
production sequence remains pending focused generation research and a bounded
future contract.

## Production sequence

### Gate P-GEN E5 — Integrate the completed authoring pipeline

Before Goal 6A, load P-GEN's canonical 20-pixel compiled artifact through a
Palimpsest-owned `Chronicle.VisualPack` reader and the existing composer/player/
Inspector path. The manual pack becomes a golden comparison fixture rather than
the default authoring path. This gate changes no gameplay or Chronicle save.

The first integrated required vocabulary is a versioned baseline, not a final
freeze. Each later gameplay slice adds its required visual subjects to P-GEN
and advances compatibility deliberately.

### Goal 6 — A Real RPG Loop

Build the smallest mature-enough consumer before building an economy, then
connect that consumer to Home immediately.

#### 6A — A Real Fight

Move only the passed combat and Modifier-grammar decisions into production:

- contextual Targets plus `Burn` with two desirable, mutually exclusive
  Modifier builds under shared Load;
- one Weapon, one Armor, one Accessory, visible HP, and deterministic
  Heartbeat-timed actions;
- an Engagement Plan, automatic pause on contact, Slow danger, and tactical
  input that pauses before resolution;
- one authored dangerous opponent whose movement, timing, interruption,
  resistance, or Target facts create pressure; strength must not mean only a
  large HP total;
- Preparation and Recovery instead of a generic MP bar; and
- one persistent material consequence plus strict save, death, and restart
  proof.

Do not add a broad bestiary, loot tables, inventory grid, crafting framework,
Companion framework, general statistics engine, or complete Word Catalogue.

#### 6B — Power Comes Home

Thread one material loop through the now-real RPG spine:

- one generated expedition resource with a persistent origin;
- one bounded way to carry it physically to Home;
- one buildable, vulnerable Load Source;
- increased Load applied at the next Attunement, enabling one previously
  impossible but desirable Loadout; and
- destruction or dismantling that removes future Attunement capacity without
  remotely disabling the current expedition's Loadout, followed by rebuilding.

The player-visible proof is the complete loop:

```text
explore or fight -> acquire matter -> return Home -> build capacity
-> choose a stronger or broader Loadout -> confront a new possibility
```

This is not permission for production chains, generic crafting recipes,
anonymous workers, stockpile simulation, or a broad base-building mode.

### Goal 7 — Home Has People

Only after the RPG and material loop exist, add one named Agent, one social
Directive proof, and one causally legible off-camera Pressure. A Companion then
becomes an Agent with identity and agency rather than a combat slot copied from
the prototype.

### Slice 8 — The First Raid

Let the accepted Pressure threaten material Home state, including a Load Source,
with visible causes, defenders, damage, and rebuilding. Do not invent raids as
an arbitrary timer merely to prove that structures can be destroyed.

## Why this order

Building a broad settlement economy first would create resources without a
proven use. Building a broad conventional RPG first would let gear, rewards, and
progression harden before Home could shape them. Goal 6 establishes one deep
RPG consumer and immediately feeds it from one persistent material producer.

The seam remains `ChronicleSimulation` commands and read-only snapshots. Core
owns action planning, Target validity, combat resolution, resources, Home, Load
Sources, Attunement, and persistence. Godot translates input and presents
results. The isolated prototype contributes evidence and terminology, not code
or a second public interface.

## P-GEN sequencing

P-GEN is complete and required. Integrate its canonical compiled files through
a Palimpsest-owned reader before 6A. Keep the accepted manual 20-pixel pack and
palette as golden comparison evidence during that gate, not as the long-term
authoring path. Extend the versioned P-GEN vocabulary within 6A and 6B when the
opponent, equipment, resource, structure, Target, and Modifier subjects become
accepted requirements.

P-GEN may author how generated things look. World Grammar and Chronicle state
continue to decide what exists, where it exists, and what it means.

## Next authorization

The next permissible implementation contract is **P-GEN E5 integration**.
After it passes, the next contract is **6A — A Real Fight**. That contract must
name one player journey, the exact retained interfaces, the smallest successor
save migration, automated Core proof, Godot proof, and a UAT stop. Passing 6A
does not authorize 6B.
