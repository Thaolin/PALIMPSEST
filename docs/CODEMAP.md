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
| Assess the external P-GEN candidate or prepare an E5 decision | [P-GEN E4.5 readiness review](P-GEN-E4-5-READINESS-REVIEW.md) |
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
        ↓ semantic snapshots and query results
Chronicle.Visuals + Chronicle.VisualPack
        ↓ transient render plans
Godot views and UI

Chronicle.Core.Checks crosses the same simulation interface without Godot.
```

`ChronicleSimulation` is the primary rule seam. Godot translates input into
Core commands and renders Core-owned state. Tests drive the same commands,
ticks, generation, and save codec. Do not add a second implementation of a rule
to make presentation or testing convenient.

World Grammar's bounded area query is the generation seam for Slice 3. The
player viewport, developer World Atlas Inspector, and Core checks consume the
same absolute-address semantic snapshots. The inspector is a presentation
adapter, not a second generator or an owner of Chronicle state.

## Source modules

### `Chronicle.Core`

Engine-independent deterministic rules. This project must always build and run
without Godot.

| Source | Owns | Interface or callers |
| --- | --- | --- |
| [`ChronicleSimulation.cs`](../src/Chronicle.Core/ChronicleSimulation.cs) | Command application, current Study Source and Home-context queries, clock pulses, movement, Loadout use, target validity, Incarnation death/replacement, and command results | `ChronicleSimulation.Apply`, `CurrentStudySource`, `HomeContext`, `AdvanceClockPulse`, `ValidTargetsForSlot`; used by Godot and Core checks |
| [`ChronicleState.cs`](../src/Chronicle.Core/ChronicleState.cs) | Serializable Chronicle state, singular Home, addresses, clock and Incarnation lifecycle state, deterministic tick advancement, strict current v3 canonical persistence, and explicit v2/v1/pre-envelope predecessor migration | `ChronicleState`, `WorldAddress`, `ChronicleSaveCodec` |
| [`Home.cs`](../src/Chronicle.Core/Home.cs) | Singular Home identity/material facts, current-site eligibility snapshot, and derived physical Return Route snapshot | `HomeState`, `HomeContextSnapshot`, `HomeSiteSnapshot`, `ReturnRouteSnapshot` |
| [`WordCatalogue.cs`](../src/Chronicle.Core/WordCatalogue.cs) | Stable Word identities and the authored read-only `Fly`, `Found`, `Stone`, and `Bell` definitions, kinds, meanings, thresholds, and compatibility | `WordIds`, `WordCatalogue.Words`, `WordCatalogue.Get`; used by Core rules, Godot presentation, and Core checks |
| [`LanguageState.cs`](../src/Chronicle.Core/LanguageState.cs) | Canonical ordered Codex membership, word-specific Understanding, one active source/word pursuit, and their strict current/legacy JSON shapes | `CodexState`, `StudyState`, `WordUnderstanding` |
| [`StudySources.cs`](../src/Chronicle.Core/StudySources.cs) | Generated Bell Study Source identity, qualities, contextual offers, word-specific yield, and regenerated public snapshot | `StudySourceSnapshot` and `StudyOfferSnapshot` through `ChronicleSimulation.CurrentStudySource` |
| [`WorldArea.cs`](../src/Chronicle.Core/WorldArea.cs) | Versioned deterministic semantic World Grammar over bounded absolute-address rectangles, adjacency context, motif identity, and loose-Stone/Hearthstone durable-subject overlays | `WorldArea.Generate`, `WorldRectangle`, `WorldCell`; used by player views, inspector, and Core checks |
| [`Loadout.cs`](../src/Chronicle.Core/Loadout.cs) | Eight-slot Loadout state, `WordId` Expression shape, catalogue-kind/Codex/compatibility validation, and duplicate-Verb invariant | `LoadoutSlot`, `LoadoutState` |
| [`SurfacePatch.cs`](../src/Chronicle.Core/SurfacePatch.cs) | Retained World Grammar version 0 surface semantics | Legacy regeneration for predecessor Chronicles |
| [`SkyStratum.cs`](../src/Chronicle.Core/SkyStratum.cs) | Retained World Grammar version 0 sky semantics and The Bell That Fell Up constants | Legacy regeneration plus durable Landmark address and identity |
| [`DeterministicHash.cs`](../src/Chronicle.Core/DeterministicHash.cs) | Stable seed/address hashing shared by deterministic generation | Internal generation helper; not a gameplay interface |

World Grammar version `2` delegates physical Surface/Sky cells to version `1`
and adds the two-offer Bell Study Source. Version `0` retains its single-Stone
compatibility source so predecessor Chronicles can finish already-started
Study. Generated source snapshots and authored catalogue presentation never
enter Chronicle saves.

### `Chronicle.VisualPack`

Engine-independent compiled visual data. It owns the validated, immutable
pack seam and the compact manually authored Gate 3B reference pack.

| Source | Owns | Interface or callers |
| --- | --- | --- |
| [`CompiledVisualPack.cs`](../src/Chronicle.VisualPack/CompiledVisualPack.cs) | Pack identity and versions, indexed atlas, palette roles, stable visual definitions, anchors, layer classes, adjacency masks, validation, and digest | `CompiledVisualPack`, `VisualDefinition`; consumed by the pure composer and Godot adapter |
| [`ManualVisualPack.cs`](../src/Chronicle.VisualPack/ManualVisualPack.cs) | Native 16 px and 20 px authored reference atlases for Gate 3B | `ManualVisualPack.CreateGate3B`; replaceable authoring adapter, not a runtime compiler |

### `Chronicle.Visuals`

Engine-independent Palimpsest-specific Visual Grammar.

| Source | Owns | Interface or callers |
| --- | --- | --- |
| [`VisualGrammar.cs`](../src/Chronicle.Visuals/VisualGrammar.cs) | Read-only semantic-to-visual mapping, adjacency composition, stable address-derived variants, layer order, visible crop, and render-plan digest | `VisualGrammar.Compose`, `VisualCompositionInput`, `VisualRenderPlan`; shared by player view and Inspector |
| [`VisualViewportBounds.cs`](../src/Chronicle.Visuals/VisualViewportBounds.cs) | Finite viewport centering, numeric-domain-safe panning, and the largest representable one-cell semantic halo | Shared by player and Inspector request adapters; numeric storage limits do not become authored World edges |

### `Chronicle.Godot`

The presentation adapter. It owns engine lifecycle, input translation, UI,
visual composition, and the application save location. It owns no Chronicle
decision.

| Source | Owns | Must not own |
| --- | --- | --- |
| [`ChronicleApp.cs`](../src/Chronicle.Godot/ChronicleApp.cs) | Scene construction, nonbinding Starting Vector and mixed-Codex Loadout controls, input-to-command translation, Home/Hearthstone/physical-route readouts, death/replacement UI, clock pulse delivery, save-file I/O, and headless Godot acceptance journeys | Movement legality, Home-site eligibility, Return Route calculation or movement, death eligibility, replacement continuity, Study progress, Loadout compatibility, target validity, generation meaning, or persistence semantics |
| [`WorldVisualView.cs`](../src/Chronicle.Godot/WorldVisualView.cs) | One batched player-view draw surface over a shared `VisualRenderPlan` | Semantic generation, variant selection, or durable world state |
| [`VisualPackGodotAdapter.cs`](../src/Chronicle.Godot/VisualPackGodotAdapter.cs) | Indexed-atlas expansion, native and overview rasterization, atlas-region textures, and Godot draw adaptation | Pack authorship, semantic mapping, or gameplay rules |
| [`WorldAtlasInspector.cs`](../src/Chronicle.Godot/WorldAtlasInspector.cs) and [`WorldAtlasInspector.tscn`](../src/Chronicle.Godot/WorldAtlasInspector.tscn) | Direct developer-only bounded World Grammar inspection, semantic diagnostics, shared Visual Grammar preview, and deterministic capture | Player save I/O, Chronicle advancement, generation rules, or a player-facing Atlas |
| [`project.godot`](../src/Chronicle.Godot/project.godot) | Godot project configuration, root scene, display, and input actions | Gameplay state or rules |
| [`Chronicle.Godot.csproj`](../src/Chronicle.Godot/Chronicle.Godot.csproj) | Godot C# build and references to Core, VisualPack, and Visuals | Domain dependencies flowing back into Core or the engine-independent visual projects |

Godot draws the pure render plan. Semantic terrain and generated identity stay
in Core; pack validation and composition stay outside Godot. Cosmetic
variation is deterministic and transient, never serialized simulation state.

### Checks

| Source | Proves |
| --- | --- |
| [`Chronicle.Core.Checks/Program.cs`](../checks/Chronicle.Core.Checks/Program.cs) | Dependency-free Core determinism, commands, ticks, generation, replay, migration, and save/load contracts |
| [`Chronicle.Visuals.Checks/Program.cs`](../checks/Chronicle.Visuals.Checks/Program.cs) | Pack vocabulary and bounds, exact adjacency-edge compatibility, deterministic variants, mapping, layering, crop, overlap, numeric-address edges, and render-plan digest |
| [`verify.ps1`](../checks/verify.ps1) | Packaged .NET builds, Core and Visual checks, isolated 16 px and 20 px player/Inspector acceptance, save non-mutation, deterministic review artifacts, Godot editor callback, Goal 2 regressions, the Goal 4A two-choice journey across partial/completed restarts, and the Goal 4B two-process Home save/restart gate with strict v3 save inspection |

Exact supported commands and the packaged executable locations remain in the
[Development guide](DEVELOPMENT.md).

### Repository entry points

| Source | Owns |
| --- | --- |
| [`play.ps1`](../play.ps1) | Canonical game build/launch using the normal Godot save, a named isolated profile, or a generated fresh profile |
| [`open-editor.ps1`](../open-editor.ps1) | Godot editor launch with the packaged .NET environment |

## Documentation map

| Document | Canonical responsibility |
| --- | --- |
| [`AGENTS.md`](../AGENTS.md) | Non-negotiable product, technology, and working rules for contributors |
| [`CONTEXT.md`](../CONTEXT.md) | Domain glossary only; settled terms without implementation details |
| [`VISION.md`](VISION.md) | Product promise, connected loops, design pillars, scale, and non-goals |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Runtime seams, ownership, persistence/generation shape, and technical constraints |
| [`ROADMAP.md`](ROADMAP.md) | Slice sequence, acceptance headlines, and links to implementation contracts |
| [`GOAL-2-A-WORD-KEPT.md`](GOAL-2-A-WORD-KEPT.md) | Goal 2's three UAT-gated vertical slices |
| [`GOAL-4-THREE-OPENINGS.md`](GOAL-4-THREE-OPENINGS.md) | Goal 4's sequential 4A Study choice, 4B Home, and 4C conflict contract |
| [`GOAL-4A-UAT.md`](GOAL-4A-UAT.md) | Exact isolated 20-pixel player journey, acceptance questions, and stop boundary for Slice 4A |
| [`GOAL-4B-UAT.md`](GOAL-4B-UAT.md) | Exact isolated 20-pixel Home, Hearthstone, physical Return Route, enlarged-playspace journey, and stop boundary for Slice 4B |
| [`SLICE-1-FIRST-HORIZON.md`](SLICE-1-FIRST-HORIZON.md) | Historical Slice 1 implementation and acceptance contract |
| [`SLICE-3-WORLD-VISUAL-GRAMMAR.md`](SLICE-3-WORLD-VISUAL-GRAMMAR.md) | World Grammar, developer Atlas Inspector, and Visual Grammar contract |
| [`GATE-3B-VISUAL-UAT.md`](GATE-3B-VISUAL-UAT.md) | Gate 3B candidate comparison, annotated four-image review sheet, interactive journey, and exact density decision |
| [`PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md`](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md) | Full pure-C# compiler, compiled-pack, composer, Godot-adapter, conformance, and drop-in contract for the parallel candidate engine |
| [`CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md`](CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md) | Copy-paste E0–E4 build authority for a separate engine workspace; explicitly forbids Palimpsest integration |
| [`P-GEN-E4-5-READINESS-REVIEW.md`](P-GEN-E4-5-READINESS-REVIEW.md) | Reproduced external evidence, unresolved E5 ownership questions, vocabulary drift, and the future adoption gate |
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
| Change compiled visual data | `Chronicle.VisualPack` | Pack validation, exact adjacency-edge checks, native-scale review, and no Core dependency |
| Change Visual Grammar | `Chronicle.Visuals` composer | Stable mapping, overlap, variant, and plan-digest checks plus visual UAT; no Core decision may change |
| Change Godot drawing | `WorldVisualView.cs` or `VisualPackGodotAdapter.cs` | Shared-plan player and Inspector acceptance; no second mapping path |
| Change the developer World Atlas Inspector | `Chronicle.Godot` inspection adapter | Use Core area snapshots; verify pan/zoom/seed/Stratum controls and no player-save mutation |
| Change controls, layout, or feedback | `ChronicleApp.cs` | Godot build/headless check and player UAT |
| Add a vertical slice | Roadmap plus one slice contract | Player-visible hypothesis, automated Core proof, Godot proof, save compatibility, and UAT gate |
| Complete or cross a UAT gate | Active contract Status and `HANDOFF.md` | Record exact proof, reconcile Roadmap, and stop before unauthorized next work |
| Make a hard-to-reverse technical choice | `docs/adr/` | Link the accepted ADR from Architecture or the owning contract |
| Add, remove, or move a module or canonical document | This codemap | Repair entry-point links and rerun the Markdown link check |

## Current contracts

- [Slice 1 — First Horizon](SLICE-1-FIRST-HORIZON.md)
- [Goal 2 — A Word Kept After Death](GOAL-2-A-WORD-KEPT.md)
- [Goal 4 — Three Openings, One Chronicle](GOAL-4-THREE-OPENINGS.md)
- [Slice 3 — A World With Shape](SLICE-3-WORLD-VISUAL-GRAMMAR.md)
- [Gate 3B — Visual UAT](GATE-3B-VISUAL-UAT.md)
- [Chronicle Visual Engine — Drop-in Specification](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md)
- [Build the Chronicle Visual Engine — Handoff Prompt](CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md)

Read the [Roadmap](ROADMAP.md), the relevant contract's Status section, and the
[Active Handoff Contract](HANDOFF.md) together. Do not infer status from source
names, save filenames, headless marker strings, or this codemap.

## Maintenance rule

Update this codemap only when ownership, module interfaces, file locations,
verification entry points, or canonical documentation changes. Do not add
progress reports, design brainstorming, exhaustive symbol lists, or duplicated
build commands. Short-lived execution status belongs only in the Handoff.
