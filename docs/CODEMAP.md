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

The source map below describes the current strict-v8 successor runtime. Fitted
predecessor Nouns and fixture gameplay survive only as literal migration input
and neutral retained-world proof; they are not a second current play path.

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
| [`ChronicleSimulation.cs`](../src/Chronicle.Core/ChronicleSimulation.cs) | Sole command/snapshot seam for movement, fixed Heartbeats, combat, physical work, Attunement, Agent interaction, Directives, death/replacement, and retained predecessor queries | `ChronicleSimulation.Apply`, `CombatContext`, `PowerComesHomeContext`, `AgentContext`, `DirectiveContext`, `PreviewTarget`, `AdvanceClockPulse`; used by Godot and Core checks |
| [`ChronicleState.cs`](../src/Chronicle.Core/ChronicleState.cs) | Durable Chronicle, combat, Lode, Source, recorded Attunement, consequential-Agent collection, pending Directives, and response memory plus deterministic Heartbeat advancement | `ChronicleState`, `WorldAddress` |
| [`ChronicleSaveCodec.cs`](../src/Chronicle.Core/ChronicleSaveCodec.cs) | Strict save v9 serialization, envelope dispatch, literal-v8 and older migration, current/successor save mapping, and private per-version document mirrors | `ChronicleSaveCodec.Serialize`, `ChronicleSaveCodec.Deserialize` |
| [`ChronicleSaveMigrations.cs`](../src/Chronicle.Core/ChronicleSaveMigrations.cs) | Explicit v6/v5/v4/v3/v2/v1/pre-envelope migration into current state | Internal to `ChronicleSaveCodec` |
| [`ChronicleSaveDocumentValidation.cs`](../src/Chronicle.Core/ChronicleSaveDocumentValidation.cs) | Strict per-version save-document validation and malformed-document rejection | Internal to `ChronicleSaveCodec` |
| [`ChronicleStateValidation.cs`](../src/Chronicle.Core/ChronicleStateValidation.cs) | Strict loaded-state validation for Chronicle, combat, Loadout, Holding, Agent identity/provenance/occupancy, relationship, owned-place, pending-Directive, and ordered-memory invariants | Internal to `ChronicleSaveCodec` |
| [`CombatRules.cs`](../src/Chronicle.Core/CombatRules.cs) | Bounded Mire Brute encounter, equipment/HP, Engagement Plan, Expression Load/timing, Target facts, Preparation/revalidation, Burn, Recovery, pursuit, forecast, and read-only combat snapshots; depends on no Holding rule | Internal rule owner exposed only through `ChronicleSimulation` commands and `CombatContextSnapshot` |
| [`HoldingRules.cs`](../src/Chronicle.Core/HoldingRules.cs) | Bounded Singing Seam/Resonant Lode state, physical carrying, one Hearth Resonator, timed work and interruption, vulnerability/rebuilding, and read-only decision snapshots including the structured material objective | Internal rule owner exposed only through `ChronicleSimulation` commands and `PowerComesHomeContextSnapshot` |
| [`HoldingFacts.cs`](../src/Chronicle.Core/HoldingFacts.cs) | Pure carrying, commitment, occupancy, and current-versus-next capacity facts both rulebooks read, plus the material commitment seam the combat rulebook runs each Heartbeat | `HoldingFacts`, `IMaterialCommitments` |
| [`HoldingObjective.cs`](../src/Chronicle.Core/HoldingObjective.cs) | The structured material objective vocabulary: objective, subject, action, outcome, established fact, constraint, and relative offset | `HoldingObjectiveSnapshot`; composed into sentences only by Godot |
| [`AgentRules.cs`](../src/Chronicle.Core/AgentRules.cs) | Pure stable Agent generation, consequential promotion, durable collection, local approach/blocking, need/relationship/intent transitions, welcome actions, timing, events, and personal road-roll facts | Internal rule owner exposed only through `ChronicleSimulation` commands and `AgentContextSnapshot` |
| [`DirectiveRules.cs`](../src/Chronicle.Core/DirectiveRules.cs) | Authored safe/dangerous Directive definitions, Suggest/Command force, physical delivery, pending consideration, withdrawal, autonomous response/movement, blocker reasons, persistent memory, and Directive snapshots | Internal rule owner exposed only through `ChronicleSimulation` commands and `DirectiveContextSnapshot` |
| [`WordEffects.cs`](../src/Chronicle.Core/WordEffects.cs) | Order-independent composition of authored Verb and Modifier Preparation, consequence, Recovery, and damage from catalogue data alone | `WordEffects.Compose`, `WordEffects.BaseFor` |
| [`WorldSubject.cs`](../src/Chronicle.Core/WorldSubject.cs) | One semantic durable-subject abstraction for every generated Chronicle subject, including Agents and Agent-owned personal places: identity, kind, archetype, condition, owner, bounded marks, and optional progress; it contains no pack visual identifier | `WorldSubject`, `WorldSubjectKind`, `WorldSubjectMark`, `WorldSubjectProgress`, `WorldSubjects` |
| [`Home.cs`](../src/Chronicle.Core/Home.cs) | Singular Home identity/material facts, current-site eligibility snapshot, and derived physical Return Route snapshot | `HomeState`, `HomeContextSnapshot`, `HomeSiteSnapshot`, `ReturnRouteSnapshot` |
| [`FirstConflict.cs`](../src/Chronicle.Core/FirstConflict.cs) | Stable River-Ward/Riven/Shattered identities, the one-exchange persistent state, and read-only conflict context | `FirstConflictState`, `ConflictContextSnapshot`, `FirstConflictSubjects`; consumed through simulation and World Grammar seams |
| [`WordCatalogue.cs`](../src/Chronicle.Core/WordCatalogue.cs) | Stable successor Verb/Modifier definitions including `Burn`, `Quickly`, and `Lasting` with their authored effect data; predecessor Noun identities remain parseable only for explicit old-save retirement | `WordIds`, `WordCatalogue.Words`, `WordCatalogue.Get`; used by Core rules, Godot presentation, and migration checks |
| [`LanguageState.cs`](../src/Chronicle.Core/LanguageState.cs) | Canonical ordered Codex membership, word-specific Understanding, one active source/word pursuit, and their strict current/legacy JSON shapes | `CodexState`, `StudyState`, `WordUnderstanding` |
| [`StudySources.cs`](../src/Chronicle.Core/StudySources.cs) | Generated Bell Study Source identity, qualities, contextual offers, word-specific yield, and regenerated snapshot attached to the Bell's current durable Address | `StudySourceSnapshot` and `StudyOfferSnapshot` through `ChronicleSimulation.CurrentStudySource` |
| [`WorldArea.cs`](../src/Chronicle.Core/WorldArea.cs) | Versioned deterministic semantic World Grammar over bounded absolute-address rectangles; WG6 adds consequential Agents and owned road-rolls over retained WG5 power and WG4 combat subjects while older pins gain neither retroactively | `WorldArea.Generate`, `WorldRectangle`, `WorldCell`; used by player views, Inspector, and Core checks |
| [`Loadout.cs`](../src/Chronicle.Core/Loadout.cs) | Successor Verb plus unique ordered Modifiers, fixed authored Load and Link validation, and migration-only recognition of predecessor fitted-Noun slots | `LoadoutSlot`, `LoadoutState` |
| [`SurfacePatch.cs`](../src/Chronicle.Core/SurfacePatch.cs) | Retained World Grammar version 0 surface semantics | Legacy regeneration for predecessor Chronicles |
| [`SkyStratum.cs`](../src/Chronicle.Core/SkyStratum.cs) | Retained World Grammar version 0 sky semantics and The Bell That Fell Up constants | Legacy regeneration plus durable Landmark address and identity |
| [`DeterministicHash.cs`](../src/Chronicle.Core/DeterministicHash.cs) | Stable seed/address hashing shared by deterministic generation | Internal generation helper; not a gameplay interface |

World Grammar version `6` adds deterministic latent Agent generation and emits
only consequential Agents plus their owned personal places. Version `5` adds the deterministic Singing Seam and durable
Resonant Lode/Source overlays without changing old pins. Version `4` adds the deterministic Goal 6A clearing, Mire Brute,
basalt Target, and combat overlays without adding them to old grammar pins.
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
| [`ManualVisualPack.cs`](../src/Chronicle.VisualPack/ManualVisualPack.cs) | Native 16 px and 20 px golden comparison atlases, including retained subjects plus Goal 6B material and Goal 7A Agent/road-roll states | `ManualVisualPack.CreateGate3B`; explicit developer comparison fixture outside packaged production data |

### In-repository P-GEN authoring Module

[`tools/P-GEN`](../tools/P-GEN) is the completed required authoring-time visual
compiler with its prior history preserved. Its canonical `Palimpsest20` output
is the source artifact for the accepted E5 reader/conformance gate. Production
projects never reference P-GEN compiler or catalogue assemblies;
`Chronicle.VisualPack` owns the runtime reader.

E5.1 adds a separate P-GEN authoring workbench at that seam. The
existing pack-only preview remains the runtime-artifact oracle; the workbench
may reference compiler/catalogue code but is never packaged or referenced by
Palimpsest.

### `Chronicle.Visuals`

Engine-independent Palimpsest-specific Visual Grammar.

| Source | Owns | Interface or callers |
| --- | --- | --- |
| [`VisualGrammar.cs`](../src/Chronicle.Visuals/VisualGrammar.cs) | Read-only semantic-to-visual mapping, adjacency composition, combat/material/Agent/personal-place states and emphases, stable variants, layer order, visible crop, and render-plan digest | `VisualGrammar.Compose`, `VisualCompositionInput`, `VisualRenderPlan`; shared by player view and Inspector |
| [`VisualViewportBounds.cs`](../src/Chronicle.Visuals/VisualViewportBounds.cs) | Finite viewport centering, numeric-domain-safe panning, and the largest representable one-cell semantic halo | Shared by player and Inspector request adapters; numeric storage limits do not become authored World edges |

### `Chronicle.Godot`

The presentation adapter. It owns engine lifecycle, input translation, UI,
visual composition, and the application save location. It owns no Chronicle
decision.

| Source | Owns | Must not own |
| --- | --- | --- |
| [`ChronicleApp.cs`](../src/Chronicle.Godot/ChronicleApp.cs) | Lifecycle, map composition, input/HUD command routing, bounded visible-cell inspection, fixed pulses, save I/O, isolated UAT preparation, retained journeys, and eight-stage Goal 7B Godot acceptance | Damage, eligibility, Load, timing, Agent/Directive decisions, movement, relationship, or persistence semantics |
| [`ChronicleHud.cs`](../src/Chronicle.Godot/ChronicleHud.cs) | Map-first 1600×900 frame with contextual combat, material, Agent, Directive, and inspection rails; focusable actions, forecast/log surfaces, and replacement summary | Chronicle rules or alternate combat/power/Agent/Directive state; it presents snapshots and emits `ChronicleCommand` values |
| [`HoldingPresentation.cs`](../src/Chronicle.Godot/HoldingPresentation.cs) | Every Holding checklist sentence, input label, decision answer, availability explanation, timing/capacity phrase, and material-state heading composed from Core facts | Chronicle legality, timing, capacity, persistence, or material state |
| [`AgentPresentation.cs`](../src/Chronicle.Godot/AgentPresentation.cs) | Every Agent banner, checklist, identity/need fact, decision answer, timing forecast, action explanation, event log sentence, and replacement summary composed from Core facts | Agent legality, movement, timing, relationship, or persistence state |
| [`DirectivePresentation.cs`](../src/Chronicle.Godot/DirectivePresentation.cs) | Concise Directive checklist, force/reach preview, pending timing, response reason, persistent-memory summary, action detail, and event log copy composed from Core snapshots | Directive admissibility, timing, response, movement, or memory semantics |
| [`InspectionPresentation.cs`](../src/Chronicle.Godot/InspectionPresentation.cs) | Read-only address, terrain, feature, visible-subject, condition, owner, progress, and cursor guidance composed from one semantic `WorldCell` | Hidden facts, durable inspection state, World generation, or action legality |
| [`WorldVisualView.cs`](../src/Chronicle.Godot/WorldVisualView.cs) | One batched player-view draw surface over a shared `VisualRenderPlan` | Semantic generation, variant selection, or durable world state |
| [`VisualPackGodotAdapter.cs`](../src/Chronicle.Godot/VisualPackGodotAdapter.cs) | Indexed-atlas expansion, native and overview rasterization, atlas-region textures, and Godot draw adaptation | Pack authorship, semantic mapping, or gameplay rules |
| [`PackagedVisualPackLoader.cs`](../src/Chronicle.Godot/PackagedVisualPackLoader.cs) | One shared P-GEN-default/manual-comparison selection path for player and Inspector | Pack validation or authorship |
| [`WorldAtlasInspector.cs`](../src/Chronicle.Godot/WorldAtlasInspector.cs) and [`WorldAtlasInspector.tscn`](../src/Chronicle.Godot/WorldAtlasInspector.tscn) | Developer-only bounded World Grammar inspection, semantic diagnostics, shared Visual Grammar preview, deterministic capture, eight-state Goal 6B, six-state Goal 7A, and nine-state Goal 7B packaged/manual parity | Player save I/O, Chronicle advancement, generation rules, or a player-facing Atlas |
| [`project.godot`](../src/Chronicle.Godot/project.godot) | Godot project configuration, root scene, display, and input actions | Gameplay state or rules |
| [`Chronicle.Godot.csproj`](../src/Chronicle.Godot/Chronicle.Godot.csproj) | Godot C# build and references to Core, VisualPack, and Visuals | Domain dependencies flowing back into Core or the engine-independent visual projects |

Godot draws the pure render plan. Semantic terrain and generated identity stay
in Core; pack validation and composition stay outside Godot. Cosmetic
variation is deterministic and transient, never serialized simulation state.

### Checks

| Source | Proves |
| --- | --- |
| [`Chronicle.Core.Checks/Program.cs`](../checks/Chronicle.Core.Checks/Program.cs) | Retained Goal 6 plus Goal 7A generation order, 512-profile/256-record scale, promotion, journey, blocking, welcome, persistence, death/replacement, strict v8, malformed state, replay, literal migrations, WorldSubjects, Core-copy isolation, and frozen v7 bytes |
| [`Chronicle.Visuals.Checks/Program.cs`](../checks/Chronicle.Visuals.Checks/Program.cs) | P-GEN combat/material/Agent vocabulary, packaged/manual semantic parity, inspection selection, Directive recipient/objective/pending/blocked/refused states, adjacency, layering, crop, overlap, numeric-address proof, and retained Goal 6B render-plan oracle |
| [`verify.ps1`](../checks/verify.ps1) | P-GEN verification, packaged .NET builds, Core/Visual checks, Godot editor build, exact four-file package isolation, packaged/manual Inspector parity, retained Goal 6A/6B journeys, and Goal 7A's six HUD captures plus strict-v8 save proof |

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
| [`play.ps1`](../play.ps1) | Canonical game build/launch using the normal save, any named isolated profile, a generated fresh profile, or the two self-preparing Goal 7A UAT profile names |
| [`open-editor.ps1`](../open-editor.ps1) | Godot editor launch with the packaged .NET environment |
| [`pgen-workbench.ps1`](../pgen-workbench.ps1) | Root authoring interface delegating to the `tools/P-GEN` workbench without exposing it to production projects |

## Documentation map

| Document | Canonical responsibility |
| --- | --- |
| [`AGENTS.md`](../AGENTS.md) | Non-negotiable product, technology, and working rules for contributors |
| [`CONTEXT.md`](../CONTEXT.md) | Domain glossary only; settled terms without implementation details |
| [`VISION.md`](VISION.md) | Product promise, connected loops, design pillars, scale, and non-goals |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Runtime seams, ownership, persistence/generation shape, and technical constraints |
| [`ROADMAP.md`](ROADMAP.md) | Slice sequence, acceptance headlines, and links to implementation contracts |
| [`MODIFIER-GRAMMAR-DIRECTION.md`](MODIFIER-GRAMMAR-DIRECTION.md) | Settled successor grammar, rejected assumptions, pressure-test boundary, and open decisions; not production authorization |
| [`RPG-SUCCESSOR-REBUILD-DIRECTION.md`](RPG-SUCCESSOR-REBUILD-DIRECTION.md) | Retained Slice 0–3 foundation, redesigned gameplay surface, same-scale generated-Area north star, Goal 6–8 sequence, and P-GEN timing; not production authorization |
| [`QUD-GENERATION-ALIGNMENT.md`](QUD-GENERATION-ALIGNMENT.md) | Primary-source comparison of Qud's builders, selective WFC, history, blueprints, populations, and their bounded fit with World Grammar |
| [`COMBAT-GRAMMAR-PRESSURE-TEST.md`](COMBAT-GRAMMAR-PRESSURE-TEST.md) | Completed isolated prototype question, fixed fixture, player journey, accepted evidence, and forbidden production drift |
| [`GOAL-6A-A-REAL-FIGHT.md`](GOAL-6A-A-REAL-FIGHT.md) | Contract and accepted prototype-quality result for one generated-world fight, strict successor migration, map-first combat HUD, persistence proof, and player UAT |
| [`GOAL-6B-POWER-COMES-HOME.md`](GOAL-6B-POWER-COMES-HOME.md) | Bounded expedition-resource, physical-carrying, Home Load Source, Attunement, persistence, presentation, automated-acceptance, and player-UAT contract |
| [`GOAL-6C-ONE-RULEBOOK.md`](GOAL-6C-ONE-RULEBOOK.md) | Bounded pre-Goal-7 consolidation gate: Core owns facts not copy, authored Word effects, one durable-subject model, the save-file split, three presentation fixes, an honest verification gate, domain module names, and the recorded Codex/Loadout direction |
| [`GOAL-7A-SOMEONE-COMES-HOME.md`](GOAL-7A-SOMEONE-COMES-HOME.md) | Completed and player-accepted bounded Agent contract with green automated proof for generated arrival, welcome, Guest persistence, scale, strict v8, visuals, packaging, and both UAT journeys |
| [`GOAL-7B-A-DIRECTIVE-NOT-UNIT-CONTROL.md`](GOAL-7B-A-DIRECTIVE-NOT-UNIT-CONTROL.md) | Canonical bounded social-language contract separating Suggest/Command force, physical delivery, autonomous Agent response, Directive memory, and read-only visible-cell inspection |
| [`GOAL-4-THREE-OPENINGS.md`](archive/contracts/GOAL-4-THREE-OPENINGS.md) | Archived Goal 4 contract and accepted 4A Study, 4B Home, and 4C conflict proof |
| [`GOAL-4C-UAT.md`](archive/uat/GOAL-4C-UAT.md) | Archived fresh-Chronicle fight journey, player result, and deferred visual notes |
| [`SLICE-5-A-WORD-MULTIPLIES.md`](archive/contracts/SLICE-5-A-WORD-MULTIPLIES.md) | Archived accepted contract for shared authored Expression resolution and the `Fly[Bell]` proof |
| [`SLICE-5-UAT.md`](archive/uat/SLICE-5-UAT.md) | Archived accepted player prediction, choice, durable consequence, and reload proof |
| [`SLICE-3-WORLD-VISUAL-GRAMMAR.md`](SLICE-3-WORLD-VISUAL-GRAMMAR.md) | World Grammar, developer Atlas Inspector, and Visual Grammar contract |
| [`GATE-3B-VISUAL-UAT.md`](GATE-3B-VISUAL-UAT.md) | Gate 3B candidate comparison, annotated four-image review sheet, interactive journey, and exact density decision |
| [`PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md`](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md) | Governing pure-C# compiler, compiled-pack, composer, Godot-adapter, conformance, and drop-in contract for required P-GEN integration |
| [`P-GEN-E4-5-READINESS-REVIEW.md`](P-GEN-E4-5-READINESS-REVIEW.md) | Reproduced external evidence and acceptance requirements for the mandatory Palimpsest E5 integration gate |
| [`P-GEN-E5-INTEGRATION.md`](P-GEN-E5-INTEGRATION.md) | Accepted canonical reader, packaged artifact, shared default path, conformance proof, and corrected visual UAT result |
| [`P-GEN-E5-UAT.md`](P-GEN-E5-UAT.md) | Rejected first-candidate P-GEN/manual comparison and recorded visual defects |
| [`P-GEN-E5-1-VISUAL-AUTHORING-SPIKE.md`](P-GEN-E5-1-VISUAL-AUTHORING-SPIKE.md) | Accepted bounded correction for material-specific rendering, larger silhouettes, representative biome assets, and an authoring-only workbench |
| [`P-GEN-E5-1-UAT.md`](P-GEN-E5-1-UAT.md) | Accepted corrected captures and workbench journey, including deferred actor-art debt |
| [`DESIGN-EVALUATION-2026-07-20.md`](DESIGN-EVALUATION-2026-07-20.md) | Dated pre-alpha assessment of playable depth, design risks, latent opportunities, and candidate decision gates; evidence rather than production authority |
| [`DEVELOPMENT.md`](DEVELOPMENT.md) | Exact build, test, editor, and launch instructions |
| [`REFERENCES.md`](REFERENCES.md) | Primary external technical references |
| [`HANDOFF.md`](HANDOFF.md) | Current execution contract: active gate, permitted scope, known proof, stop condition, and forbidden next work |
| [`adr/0003-use-verbs-linked-modifiers-and-world-targets.md`](adr/0003-use-verbs-linked-modifiers-and-world-targets.md) | Accepted replacement of collectible Nouns with linked Modifiers and contextual world Targets |
| [`adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md`](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md) | Accepted use of P-GEN as the required authoring-time visual compiler behind a Palimpsest-owned reader |
| [`adr/0005-use-one-character-scale-across-generated-areas.md`](adr/0005-use-one-character-scale-across-generated-areas.md) | Accepted same-scale Area topology, multi-cell geography, and authored on-demand Creature Grammar |
| [`adr/0006-co-locate-p-gen-without-crossing-the-runtime-seam.md`](adr/0006-co-locate-p-gen-without-crossing-the-runtime-seam.md) | Accepted monorepo ownership of P-GEN while retaining compiler/runtime dependency and packaging isolation |
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
| Change controls, layout, or feedback | `ChronicleApp.cs` and `ChronicleHud.cs` | Godot build, rendered HUD journey, and player UAT |
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
- [Caves of Qud Generation Alignment](QUD-GENERATION-ALIGNMENT.md)
- [Passed Combat Grammar Pressure Test](COMBAT-GRAMMAR-PRESSURE-TEST.md)
- [Goal 6A — A Real Fight contract and accepted result](GOAL-6A-A-REAL-FIGHT.md)
- [Goal 6B — Power Comes Home implementation and pending-UAT contract](GOAL-6B-POWER-COMES-HOME.md)
- [Goal 6C — One Rulebook, One Vocabulary consolidation gate](GOAL-6C-ONE-RULEBOOK.md)
- [Goal 7A — Someone Comes Home contract and accepted result](GOAL-7A-SOMEONE-COMES-HOME.md)
- [Goal 7B — A Directive, Not Unit Control contract](GOAL-7B-A-DIRECTIVE-NOT-UNIT-CONTROL.md)
- [ADR 0003 — Verbs, linked Modifiers, and world Targets](adr/0003-use-verbs-linked-modifiers-and-world-targets.md)
- [ADR 0004 — P-GEN visual authoring pipeline](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md)
- [ADR 0005 — One character scale across generated Areas](adr/0005-use-one-character-scale-across-generated-areas.md)
- [ADR 0006 — Co-locate P-GEN without crossing the runtime seam](adr/0006-co-locate-p-gen-without-crossing-the-runtime-seam.md)
- [Documentation Archive](archive/README.md)

Read the [Roadmap](ROADMAP.md), the relevant contract's Status section, and the
[Active Handoff Contract](HANDOFF.md) together. Do not infer status from source
names, save filenames, headless marker strings, or this codemap.

## Maintenance rule

Update this codemap only when ownership, module interfaces, file locations,
verification entry points, or canonical documentation changes. Do not add
progress reports, design brainstorming, exhaustive symbol lists, or duplicated
build commands. Short-lived execution status belongs only in the Handoff.
