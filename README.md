# Untitled Chronicle RPG

The body may die. Its Chronicle does not.

Untitled Chronicle RPG is a persistent-world role-playing game about finding
authored power Words, combining them into Expressions, and using them to leave
material changes in a generated world. Knowledge survives death. Places retain
their history. Home becomes powerful because of what you physically discover,
carry back, build, lose, and rebuild.

The long-term target sits somewhere between *Caves of Qud*, *Dwarf Fortress*,
and power fantasy: an unbounded world of ruins, creatures, Holdings, factions,
and rare impossibilities where the player keeps asking, “What the fuck was
that? I can do that?”

## The game

- **Build a persistent Codex.** Discover authored Verbs and Modifiers, then
  combine a bounded selection into an active Loadout.
- **Act on the actual world.** Expressions target Chronicle creatures, objects,
  and places whose material, scale, identity, and agency matter.
- **Prepare instead of twitching.** Combat and exploration advance on
  deterministic Heartbeats with pause and speed controls.
- **Bring power Home.** Major Load capacity comes from vulnerable structures
  built with matter carried back from expeditions.
- **Outlive the current body.** Incarnations die and are replaced; the Codex,
  Home, discoveries, and material consequences remain.
- **Generate history, not disposable maps.** Territory appears on demand, but
  consequential people, places, damage, and changes retain stable identities.

## Current playable slice

Goal 6 is complete as a prototype-quality vertical slice. A fresh Chronicle
begins at the First Hearth with a visible Burn Primer nearby. From there you
can:

1. learn `Burn`, `Quickly`, and `Lasting`;
2. attune a combat Loadout and confront a generated Mire Brute;
3. extract one Resonant Lode from its persistent Singing Seam;
4. carry it physically Home and build the vulnerable Hearth Resonator;
5. gain additional Load at the next Attunement;
6. use a previously impossible three-Word Expression; and
7. dismantle, lose, and rebuild the Source without remotely disabling the
   currently attuned Loadout.

This is proof of the RPG’s central expedition–Home–power loop, not a finished
content set. Broader Word discovery and equipping, robust inventory, creatures,
construction, Agents, and factional history remain later work.

## Play

The repository carries its pinned .NET and Godot toolchain. From PowerShell at
the repository root:

```powershell
.\play.ps1
```

Use a clean isolated Chronicle or resume a named testing profile with:

```powershell
.\play.ps1 -Fresh
.\play.ps1 -Profile <name>
```

The game explains contextual commands, timing, interruptions, and unavailable
actions in the HUD. Build, editor, save-profile, and verification details are
in the [development guide](docs/DEVELOPMENT.md).

## Technology

- **Godot 4.7.1 .NET** owns presentation, input, UI, audio, and authoring
  adapters.
- **.NET SDK 8.0.423 / C#** owns all production code.
- **Chronicle.Core** owns deterministic, engine-independent rules, generation,
  persistence, migration, and replay.
- **P-GEN** is the in-repository visual asset compiler.

### P-GEN

[P-GEN](tools/P-GEN/README.md) deterministically compiles an authored visual
catalogue into the game’s indexed 20-pixel atlas, palette, definitions, and
pinned hashes. Its compiler, catalogue, review evidence, and Godot workbench
stay on the authoring side of a strict boundary: the game references only the
compiled four-file pack and its small runtime reader.

Launch the visual-authoring workbench with:

```powershell
.\pgen-workbench.ps1
```

The complete repository gate verifies both sides of that boundary:

```powershell
.\checks\verify.ps1
```

## Project map

- [Vision](docs/VISION.md) — the intended game and player experience
- [Glossary](CONTEXT.md) — settled domain language
- [Roadmap](docs/ROADMAP.md) — milestone sequence and acceptance state
- [Architecture](docs/ARCHITECTURE.md) — runtime and dependency boundaries
- [Codemap](docs/CODEMAP.md) — source ownership and verification entry points
- [Active handoff](docs/HANDOFF.md) — the current authorization boundary

The project is deliberately untitled. A public name comes after the game earns
one.
