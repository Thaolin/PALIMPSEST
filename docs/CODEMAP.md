# Codemap

Use this map to decide what to read, where a change belongs, and how that change
is proved. It describes stable ownership and navigation. Sequence lives in the
[Roadmap](ROADMAP.md), detailed acceptance in its linked contracts, and current
permitted work in the [Active Handoff Contract](HANDOFF.md).

## Read by purpose

| Need | Read |
| --- | --- |
| Understand the game being built | [Vision](VISION.md) |
| Use the project's canonical language | [Domain glossary](../CONTEXT.md) |
| Find a source file or test owner | This codemap |
| Understand the major seams and constraints | [Architecture](ARCHITECTURE.md) |
| See sequencing and current gates | [Roadmap](ROADMAP.md) |
| See exactly what may be worked on now | [Active Handoff Contract](HANDOFF.md) |
| Build, test, launch, or open the editor | [Development](DEVELOPMENT.md) |
| Understand a current slice's exact promise | The contract linked from the Roadmap |
| Understand a hard-to-reverse decision | [ADR 0001](adr/0001-use-godot-with-a-csharp-chronicle-core.md) and [ADR 0002](adr/0002-pin-world-grammar-version-per-chronicle.md) |
| Check external technical references | [References](REFERENCES.md) |

Repository-wide agent rules are in [AGENTS.md](../AGENTS.md). A fresh work
session must read the [Active Handoff Contract](HANDOFF.md) after the canonical
product and architecture documents.

## Runtime shape

```text
Godot input and lifecycle
        ↓ ChronicleCommand
ChronicleSimulation.Apply(...)
        ↓ deterministic transition
serializable ChronicleState
        ↓ snapshots and query results
Godot views and UI

Chronicle.Core.Checks crosses the same simulation interface without Godot.
```

`ChronicleSimulation` is the primary rule seam. Godot translates input into
Core commands and renders Core-owned state. Tests drive the same commands,
ticks, generation, and save codec. Do not add a second implementation of a rule
to make presentation or testing convenient.

## Source modules

### `Chronicle.Core`

Engine-independent deterministic rules. This project must always build and run
without Godot.

| Source | Owns | Interface or callers |
| --- | --- | --- |
| [`ChronicleSimulation.cs`](../src/Chronicle.Core/ChronicleSimulation.cs) | Command application, clock pulses, movement, Loadout use, target validity, Incarnation death/replacement, and command results | `ChronicleSimulation.Apply`, `AdvanceClockPulse`, `ValidTargetsForSlot`; used by Godot and Core checks |
| [`ChronicleState.cs`](../src/Chronicle.Core/ChronicleState.cs) | Serializable Chronicle state, addresses, clock and Incarnation lifecycle state, current Slice 2 Study state, deterministic tick advancement, versioned save migration, and JSON codec | `ChronicleState`, `WorldAddress`, `ChronicleSaveCodec` |
| [`Loadout.cs`](../src/Chronicle.Core/Loadout.cs) | Current authored word identifiers, eight-slot Loadout state, Expression shape, and Loadout invariants | `ChronicleVerb`, `ChronicleNoun`, `LoadoutSlot`, `LoadoutState` |
| [`SurfacePatch.cs`](../src/Chronicle.Core/SurfacePatch.cs) | Deterministic legacy surface patch semantics | `SurfacePatch.Generate` and semantic surface tiles |
| [`SkyStratum.cs`](../src/Chronicle.Core/SkyStratum.cs) | Deterministic linked sky patch and The Bell That Fell Up | Sky generation, Landmark address, and semantic sky tiles |
| [`DeterministicHash.cs`](../src/Chronicle.Core/DeterministicHash.cs) | Stable seed/address hashing shared by deterministic generation | Internal generation helper; not a gameplay interface |

The current `ChronicleVerb`, `ChronicleNoun`, and `StoneUnderstanding` values are
small Slice 2 scaffolds. The mature authored Word Catalogue and generated Study
Sources are product commitments, not permission to grow these enums or the
Bell-specific state without a vertical-slice contract.

### `Chronicle.Godot`

The presentation adapter. It owns engine lifecycle, input translation, UI,
visual composition, and the application save location. It owns no Chronicle
decision.

| Source | Owns | Must not own |
| --- | --- | --- |
| [`ChronicleApp.cs`](../src/Chronicle.Godot/ChronicleApp.cs) | Scene construction, input-to-command translation, death confirmation, replacement UI, clock pulse delivery, save-file I/O, UI readouts, and headless Godot acceptance journey | Movement legality, death eligibility, replacement continuity, Study progress, Loadout compatibility, target validity, generation meaning, or persistence semantics |
| [`SurfacePatchView.cs`](../src/Chronicle.Godot/SurfacePatchView.cs) | Rendering surface snapshots and visible actors/subjects | Surface generation or durable world state |
| [`SkyStratumView.cs`](../src/Chronicle.Godot/SkyStratumView.cs) | Rendering sky snapshots, the Bell, and visible actors/subjects | Sky generation, Landmark identity, or traversal rules |
| [`project.godot`](../src/Chronicle.Godot/project.godot) | Godot project configuration, root scene, display, and input actions | Gameplay state or rules |
| [`Chronicle.Godot.csproj`](../src/Chronicle.Godot/Chronicle.Godot.csproj) | Godot C# build and the reference to Chronicle.Core | Domain dependencies flowing back into Core |

Visual Grammar belongs in this module, but semantic terrain and generated
identity belong in Core. Cosmetic variation may be deterministic without
becoming serialized simulation state.

### Checks

| Source | Proves |
| --- | --- |
| [`Chronicle.Core.Checks/Program.cs`](../checks/Chronicle.Core.Checks/Program.cs) | Dependency-free Core determinism, commands, ticks, generation, replay, migration, and save/load contracts |
| [`verify.ps1`](../checks/verify.ps1) | Packaged .NET build, Core checks, Godot C# build, editor callback, headless startup, control journey, save creation, and next-launch restoration |

Exact supported commands and the packaged executable locations remain in the
[Development guide](DEVELOPMENT.md).

## Documentation map

| Document | Canonical responsibility |
| --- | --- |
| [`AGENTS.md`](../AGENTS.md) | Non-negotiable product, technology, and working rules for contributors |
| [`CONTEXT.md`](../CONTEXT.md) | Domain glossary only; settled terms without implementation details |
| [`VISION.md`](VISION.md) | Product promise, connected loops, design pillars, scale, and non-goals |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Runtime seams, ownership, persistence/generation shape, and technical constraints |
| [`ROADMAP.md`](ROADMAP.md) | Slice sequence, acceptance headlines, and links to implementation contracts |
| [`GOAL-2-A-WORD-KEPT.md`](GOAL-2-A-WORD-KEPT.md) | Goal 2's three UAT-gated vertical slices |
| [`SLICE-1-FIRST-HORIZON.md`](SLICE-1-FIRST-HORIZON.md) | Historical Slice 1 implementation and acceptance contract |
| [`SLICE-3-WORLD-VISUAL-GRAMMAR.md`](SLICE-3-WORLD-VISUAL-GRAMMAR.md) | Proposed World and Visual Grammar contract |
| [`DEVELOPMENT.md`](DEVELOPMENT.md) | Exact build, test, editor, and launch instructions |
| [`REFERENCES.md`](REFERENCES.md) | Primary external technical references |
| [`HANDOFF.md`](HANDOFF.md) | Current execution contract: active gate, permitted scope, known proof, stop condition, and forbidden next work |
| [`adr/`](adr/) | Accepted hard-to-reverse decisions and their reasoning |

## Route a change

| Change | Primary owner | Required proof or companion update |
| --- | --- | --- |
| Settle or rename a domain term | `CONTEXT.md` | Update Vision or a contract only if the player promise changed |
| Change a player-facing product promise | `VISION.md` | Reconcile Roadmap, relevant contract, and AGENTS rules |
| Add or change a simulation command | `ChronicleSimulation.cs` | Core checks through `Apply`; Godot translates input only |
| Add persistent state or change save shape | `ChronicleState.cs` and `ChronicleSaveCodec` | Literal predecessor fixture, migration check, replay, and real Godot restore |
| Change Loadout or Expression compatibility | `Loadout.cs` plus simulation rule | Core rejection/success checks and visible Godot proof |
| Change semantic World Grammar | `Chronicle.Core` generation source | Determinism, overlap, version pinning, save delta, and fixture-seed checks |
| Change Visual Grammar | Godot view or render-plan source | Stable mapping checks and visual UAT; no Core decision may change |
| Change controls, layout, or feedback | `ChronicleApp.cs` | Godot build/headless check and player UAT |
| Add a vertical slice | Roadmap plus one slice contract | Player-visible hypothesis, automated Core proof, Godot proof, save compatibility, and UAT gate |
| Complete or cross a UAT gate | Active contract Status and `HANDOFF.md` | Record exact proof, reconcile Roadmap, and stop before unauthorized next work |
| Make a hard-to-reverse technical choice | `docs/adr/` | Link the accepted ADR from Architecture or the owning contract |
| Add, remove, or move a module or canonical document | This codemap | Repair entry-point links and rerun the Markdown link check |

## Current contracts

- [Slice 1 — First Horizon](SLICE-1-FIRST-HORIZON.md)
- [Goal 2 — A Word Kept After Death](GOAL-2-A-WORD-KEPT.md)
- [Slice 3 — A World With Shape](SLICE-3-WORLD-VISUAL-GRAMMAR.md)

Read the [Roadmap](ROADMAP.md), the relevant contract's Status section, and the
[Active Handoff Contract](HANDOFF.md) together. Do not infer status from source
names, save filenames, headless marker strings, or this codemap.

## Maintenance rule

Update this codemap only when ownership, module interfaces, file locations,
verification entry points, or canonical documentation changes. Do not add
progress reports, design brainstorming, exhaustive symbol lists, or duplicated
build commands. Short-lived execution status belongs only in the Handoff.
