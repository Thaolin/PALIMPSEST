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
| Understand the successor Power Word grammar and its pressure-test boundary | [Modifier Grammar Course Correction](MODIFIER-GRAMMAR-DIRECTION.md) and [ADR 0003](adr/0003-use-verbs-linked-modifiers-and-world-targets.md) |
| Understand what the RPG successor retains, redesigns, and sequences next | [RPG Successor Rebuild Direction](RPG-SUCCESSOR-REBUILD-DIRECTION.md) |
| Run or inspect the passed isolated combat-grammar prototype | [Combat Grammar Pressure Test](COMBAT-GRAMMAR-PRESSURE-TEST.md) |
| Build, test, launch, or open the editor | [Development](DEVELOPMENT.md) |
| Understand a current slice's exact promise | The contract linked from the Roadmap |
| Find completed contracts, accepted UAT, or consumed prompts | [Documentation archive](archive/README.md) |
| Integrate the required P-GEN authoring pipeline through E5 | [Active E5 contract](P-GEN-E5-INTEGRATION.md), [P-GEN E4.5 readiness review](P-GEN-E4-5-READINESS-REVIEW.md), and [ADR 0004](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md) |
| Understand a hard-to-reverse decision | [ADR 0001](adr/0001-use-godot-with-a-csharp-chronicle-core.md), [ADR 0002](adr/0002-pin-world-grammar-version-per-chronicle.md), [ADR 0003](adr/0003-use-verbs-linked-modifiers-and-world-targets.md), and [ADR 0004](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md) |
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

The source map below describes the accepted v5 predecessor runtime, including
its fitted `Fly[Stone]` and `Fly[Bell]` behavior. The successor Verb + linked
Modifier + contextual Target grammar is documented but not implemented; do not
rewrite source ownership descriptions as if that migration has occurred.

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
| [`ChronicleSimulation.cs`](../src/Chronicle.Core/ChronicleSimulation.cs) | Command application, current Study Source, Home, and first-conflict queries, clock pulses, movement, shared fitted-Fly subject resolution, Loadout use, target validity, Incarnation death/replacement, and command results | `ChronicleSimulation.Apply`, `CurrentStudySource`, `HomeContext`, `ConflictContext`, `AdvanceClockPulse`, `ValidTargetsForSlot`; used by Godot and Core checks |
| [`ChronicleState.cs`](../src/Chronicle.Core/ChronicleState.cs) | Serializable Chronicle state, durable Bell Address, singular Home, first-conflict delta, addresses, clock and Incarnation lifecycle state, deterministic tick advancement, strict current v5 canonical persistence, and explicit v4/v3/v2/v1/pre-envelope migration | `ChronicleState`, `WorldAddress`, `ChronicleSaveCodec` |
| [`Home.cs`](../src/Chronicle.Core/Home.cs) | Singular Home identity/material facts, current-site eligibility snapshot, and derived physical Return Route snapshot | `HomeState`, `HomeContextSnapshot`, `HomeSiteSnapshot`, `ReturnRouteSnapshot` |
| [`FirstConflict.cs`](../src/Chronicle.Core/FirstConflict.cs) | Stable River-Ward/Riven/Shattered identities, the one-exchange persistent state, and read-only conflict context | `FirstConflictState`, `ConflictContextSnapshot`, `FirstConflictSubjects`; consumed through simulation and World Grammar seams |
| [`WordCatalogue.cs`](../src/Chronicle.Core/WordCatalogue.cs) | Stable Word identities and the authored read-only `Fly`, `Found`, `Smash`, `Stone`, and `Bell` definitions, kinds, meanings, thresholds, and compatibility | `WordIds`, `WordCatalogue.Words`, `WordCatalogue.Get`; used by Core rules, Godot presentation, and Core checks |
| [`LanguageState.cs`](../src/Chronicle.Core/LanguageState.cs) | Canonical ordered Codex membership, word-specific Understanding, one active source/word pursuit, and their strict current/legacy JSON shapes | `CodexState`, `StudyState`, `WordUnderstanding` |
| [`StudySources.cs`](../src/Chronicle.Core/StudySources.cs) | Generated Bell Study Source identity, qualities, contextual offers, word-specific yield, and regenerated snapshot attached to the Bell's current durable Address | `StudySourceSnapshot` and `StudyOfferSnapshot` through `ChronicleSimulation.CurrentStudySource` |
| [`WorldArea.cs`](../src/Chronicle.Core/WorldArea.cs) | Versioned deterministic semantic World Grammar over bounded absolute-address rectangles, adjacency context, motif identity, deterministic Riven Cairn selection, and Bell/loose-Stone/Hearthstone/Cairn durable-subject overlays | `WorldArea.Generate`, `WorldRectangle`, `WorldCell`; used by player views, inspector, and Core checks |
| [`Loadout.cs`](../src/Chronicle.Core/Loadout.cs) | Eight-slot Loadout state, `WordId` Expression shape, catalogue-kind/Codex/compatibility validation, and duplicate-Verb invariant | `LoadoutSlot`, `LoadoutState` |
| [`SurfacePatch.cs`](../src/Chronicle.Core/SurfacePatch.cs) | Retained World Grammar version 0 surface semantics | Legacy regeneration for predecessor Chronicles |
| [`SkyStratum.cs`](../src/Chronicle.Core/SkyStratum.cs) | Retained World Grammar version 0 sky semantics and The Bell That Fell Up constants | Legacy regeneration plus durable Landmark address and identity |
| [`DeterministicHash.cs`](../src/Chronicle.Core/DeterministicHash.cs) | Stable seed/address hashing shared by deterministic generation | Internal generation helper; not a gameplay interface |

World Grammar version `3` preserves version `2` terrain and Bell Study semantics
and adds the deterministic Riven Cairn subject. Version `2` delegates physical
Surface/Sky cells to version `1` and adds the two-offer Bell Study Source.
Version `0` retains its single-Stone compatibility source so predecessor
Chronicles can finish already-started Study. Generated source snapshots and
authored catalogue presentation never enter Chronicle saves.

### `Chronicle.VisualPack`

Engine-independent compiled visual data. It owns the validated, immutable
pack seam and the compact manually authored Gate 3B reference pack.

| Source | Owns | Interface or callers |
| --- | --- | --- |
| [`CompiledVisualPack.cs`](../src/Chronicle.VisualPack/CompiledVisualPack.cs) | Pack identity and versions, indexed atlas, palette roles, stable visual definitions, anchors, layer classes, adjacency masks, validation, and digest | `CompiledVisualPack`, `VisualDefinition`; consumed by the pure composer and Godot adapter |
| [`CanonicalVisualPackReader.cs`](../src/Chronicle.VisualPack/CanonicalVisualPackReader.cs) | Strict canonical four-file P-GEN bundle validation and construction of the existing runtime pack value | `ReadDirectory`, `ReadCanonical`; filesystem and in-memory Adapter inputs |
| [`ManualVisualPack.cs`](../src/Chronicle.VisualPack/ManualVisualPack.cs) | Native 16 px and 20 px golden comparison atlases, including intact/shattered Cairn and static danger marks | `ManualVisualPack.CreateGate3B`; retained verification fixture until and after required P-GEN E5 integration |

### External P-GEN authoring pipeline

`C:\DEV\P-GEN` is the completed required authoring-time visual compiler. Its
canonical `Palimpsest20` output is the source artifact for the mandatory E5
reader/conformance gate. Palimpsest never ships or references P-GEN compiler or
catalogue assemblies; `Chronicle.VisualPack` owns the runtime reader.

### `Chronicle.Visuals`

Engine-independent Palimpsest-specific Visual Grammar.

| Source | Owns | Interface or callers |
| --- | --- | --- |
| [`VisualGrammar.cs`](../src/Chronicle.Visuals/VisualGrammar.cs) | Read-only semantic-to-visual mapping, adjacency composition, stable address-derived variants, Core-fed static danger emphasis, layer order, visible crop, and render-plan digest | `VisualGrammar.Compose`, `VisualCompositionInput`, `VisualRenderPlan`; shared by player view and Inspector |
| [`VisualViewportBounds.cs`](../src/Chronicle.Visuals/VisualViewportBounds.cs) | Finite viewport centering, numeric-domain-safe panning, and the largest representable one-cell semantic halo | Shared by player and Inspector request adapters; numeric storage limits do not become authored World edges |

### `Chronicle.Godot`

The presentation adapter. It owns engine lifecycle, input translation, UI,
visual composition, and the application save location. It owns no Chronicle
decision.

| Source | Owns | Must not own |
| --- | --- | --- |
| [`ChronicleApp.cs`](../src/Chronicle.Godot/ChronicleApp.cs) | Scene construction, nonbinding Starting Vector and mixed-Codex Loadout controls, catalogue-derived composition guidance, input-to-command translation, moved-Bell presentation, Home/route and first-conflict readouts, death/replacement UI, clock pulse delivery, save-file I/O, and headless Godot acceptance journeys | Movement legality, Home-site or conflict eligibility, Return Route calculation or movement, conflict timing/result, death eligibility, replacement continuity, Study progress, Loadout compatibility, target validity, generation meaning, or persistence semantics |
| [`WorldVisualView.cs`](../src/Chronicle.Godot/WorldVisualView.cs) | One batched player-view draw surface over a shared `VisualRenderPlan` | Semantic generation, variant selection, or durable world state |
| [`VisualPackGodotAdapter.cs`](../src/Chronicle.Godot/VisualPackGodotAdapter.cs) | Indexed-atlas expansion, native and overview rasterization, atlas-region textures, and Godot draw adaptation | Pack authorship, semantic mapping, or gameplay rules |
| [`PackagedVisualPackLoader.cs`](../src/Chronicle.Godot/PackagedVisualPackLoader.cs) | One shared P-GEN-default/manual-comparison selection path for player and Inspector | Pack validation or authorship |
| [`WorldAtlasInspector.cs`](../src/Chronicle.Godot/WorldAtlasInspector.cs) and [`WorldAtlasInspector.tscn`](../src/Chronicle.Godot/WorldAtlasInspector.tscn) | Direct developer-only bounded World Grammar inspection, semantic diagnostics, shared Visual Grammar preview, and deterministic capture | Player save I/O, Chronicle advancement, generation rules, or a player-facing Atlas |
| [`project.godot`](../src/Chronicle.Godot/project.godot) | Godot project configuration, root scene, display, and input actions | Gameplay state or rules |
| [`Chronicle.Godot.csproj`](../src/Chronicle.Godot/Chronicle.Godot.csproj) | Godot C# build and references to Core, VisualPack, and Visuals | Domain dependencies flowing back into Core or the engine-independent visual projects |

Godot draws the pure render plan. Semantic terrain and generated identity stay
in Core; pack validation and composition stay outside Godot. Cosmetic
variation is deterministic and transient, never serialized simulation state.

### Checks

| Source | Proves |
| --- | --- |
| [`Chronicle.Core.Checks/Program.cs`](../checks/Chronicle.Core.Checks/Program.cs) | Dependency-free Core determinism, commands, ticks, generation, shared `Fly[Stone]`/`Fly[Bell]` resolution, first-conflict invariants, replay, literal predecessor migration, and strict v5 save/load contracts |
| [`Chronicle.Visuals.Checks/Program.cs`](../checks/Chronicle.Visuals.Checks/Program.cs) | Pack vocabulary and bounds, exact adjacency-edge compatibility, deterministic variants, moved-Bell and Cairn/danger mapping, layering, crop, overlap, numeric-address edges, and render-plan digest |
| [`verify.ps1`](../checks/verify.ps1) | Packaged .NET builds, Core and Visual checks, isolated 16 px and 20 px player/Inspector acceptance, save non-mutation, deterministic review artifacts, Godot editor callback, retained Goal 2/4 phases, and fresh/restarted Slice 5 `Fly[Bell]` proof with strict v5 inspection |

Exact supported commands and the packaged executable locations remain in the
[Development guide](DEVELOPMENT.md).

### Isolated prototype

| Source | Owns |
| --- | --- |
| [`prototypes/Chronicle.CombatGrammar/`](../prototypes/Chronicle.CombatGrammar/) | Retained throwaway in-memory evidence for the passed combat/grammar pressure test; never referenced by production projects |
| [`prototype-combat.ps1`](../prototype-combat.ps1) | One-command bundled-.NET runner for the retained prototype evidence |

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
| [`MODIFIER-GRAMMAR-DIRECTION.md`](MODIFIER-GRAMMAR-DIRECTION.md) | Settled successor grammar, rejected assumptions, pressure-test boundary, and open decisions; not production authorization |
| [`RPG-SUCCESSOR-REBUILD-DIRECTION.md`](RPG-SUCCESSOR-REBUILD-DIRECTION.md) | Retained Slice 0–3 foundation, redesigned gameplay surface, Goal 6–8 sequence, and P-GEN timing; not production authorization |
| [`COMBAT-GRAMMAR-PRESSURE-TEST.md`](COMBAT-GRAMMAR-PRESSURE-TEST.md) | Completed isolated prototype question, fixed fixture, player journey, accepted evidence, and forbidden production drift |
| [`GOAL-4-THREE-OPENINGS.md`](archive/contracts/GOAL-4-THREE-OPENINGS.md) | Archived Goal 4 contract and accepted 4A Study, 4B Home, and 4C conflict proof |
| [`GOAL-4C-UAT.md`](archive/uat/GOAL-4C-UAT.md) | Archived fresh-Chronicle fight journey, player result, and deferred visual notes |
| [`SLICE-5-A-WORD-MULTIPLIES.md`](archive/contracts/SLICE-5-A-WORD-MULTIPLIES.md) | Archived accepted contract for shared authored Expression resolution and the `Fly[Bell]` proof |
| [`SLICE-5-UAT.md`](archive/uat/SLICE-5-UAT.md) | Archived accepted player prediction, choice, durable consequence, and reload proof |
| [`SLICE-3-WORLD-VISUAL-GRAMMAR.md`](SLICE-3-WORLD-VISUAL-GRAMMAR.md) | World Grammar, developer Atlas Inspector, and Visual Grammar contract |
| [`GATE-3B-VISUAL-UAT.md`](GATE-3B-VISUAL-UAT.md) | Gate 3B candidate comparison, annotated four-image review sheet, interactive journey, and exact density decision |
| [`PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md`](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md) | Governing pure-C# compiler, compiled-pack, composer, Godot-adapter, conformance, and drop-in contract for required P-GEN integration |
| [`P-GEN-E4-5-READINESS-REVIEW.md`](P-GEN-E4-5-READINESS-REVIEW.md) | Reproduced external evidence and acceptance requirements for the mandatory Palimpsest E5 integration gate |
| [`P-GEN-E5-INTEGRATION.md`](P-GEN-E5-INTEGRATION.md) | Active bounded contract for the canonical reader, packaged artifact, shared default path, conformance proof, and UAT stop |
| [`P-GEN-E5-UAT.md`](P-GEN-E5-UAT.md) | Native P-GEN/manual comparison sheet, interactive visual journey, and pending player decision |
| [`DESIGN-EVALUATION-2026-07-20.md`](DESIGN-EVALUATION-2026-07-20.md) | Dated pre-alpha assessment of playable depth, design risks, latent opportunities, and candidate decision gates; evidence rather than production authority |
| [`DEVELOPMENT.md`](DEVELOPMENT.md) | Exact build, test, editor, and launch instructions |
| [`REFERENCES.md`](REFERENCES.md) | Primary external technical references |
| [`HANDOFF.md`](HANDOFF.md) | Current execution contract: active gate, permitted scope, known proof, stop condition, and forbidden next work |
| [`adr/0003-use-verbs-linked-modifiers-and-world-targets.md`](adr/0003-use-verbs-linked-modifiers-and-world-targets.md) | Accepted replacement of collectible Nouns with linked Modifiers and contextual world Targets |
| [`adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md`](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md) | Accepted use of P-GEN as the required authoring-time visual compiler behind a Palimpsest-owned reader |
| [`adr/`](adr/) | Accepted hard-to-reverse decisions and their reasoning |
| [`archive/`](archive/README.md) | Completed contracts, accepted UAT evidence, and consumed prompts; historical rather than current authority |

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

## Live contracts and decision records

- [Slice 3 — A World With Shape](SLICE-3-WORLD-VISUAL-GRAMMAR.md)
- [Gate 3B — Visual UAT](GATE-3B-VISUAL-UAT.md)
- [Chronicle Visual Engine — Drop-in Specification](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md)
- [P-GEN E4.5 Readiness Review](P-GEN-E4-5-READINESS-REVIEW.md)
- [Modifier Grammar Course Correction](MODIFIER-GRAMMAR-DIRECTION.md)
- [RPG Successor Rebuild Direction](RPG-SUCCESSOR-REBUILD-DIRECTION.md)
- [Passed Combat Grammar Pressure Test](COMBAT-GRAMMAR-PRESSURE-TEST.md)
- [ADR 0003 — Verbs, linked Modifiers, and world Targets](adr/0003-use-verbs-linked-modifiers-and-world-targets.md)
- [ADR 0004 — P-GEN visual authoring pipeline](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md)
- [Documentation Archive](archive/README.md)

Read the [Roadmap](ROADMAP.md), the relevant contract's Status section, and the
[Active Handoff Contract](HANDOFF.md) together. Do not infer status from source
names, save filenames, headless marker strings, or this codemap.

## Maintenance rule

Update this codemap only when ownership, module interfaces, file locations,
verification entry points, or canonical documentation changes. Do not add
progress reports, design brainstorming, exhaustive symbol lists, or duplicated
build commands. Short-lived execution status belongs only in the Handoff.
