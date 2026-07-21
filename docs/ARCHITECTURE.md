# Architecture

## Boundary

Godot 4 .NET is the self-contained shell for rendering, input, UI, audio, and
authoring. C# is the sole production language. The deterministic Chronicle
simulation must compile and test without Godot scenes, Nodes, or frame timing.

```text
Godot scenes and C# Nodes
        ↓ input / presentation
Chronicle.Godot adapter
        ↓ draw plans / commands
engine-independent C# Visual Grammar
        ↓ read-only semantic snapshots
Chronicle.Core
        ↓
fixed-tick Chronicle state + persistence + Word Catalogue + World Grammar
```

## Successor rebuild boundary

The RPG successor preserves the accepted Slice 0–3 substrate and replaces
player-facing predecessor rules incrementally. This is one runtime evolving
through strict migrations, not a new application or a second simulation path.

Retained interfaces and implementations include deterministic Chronicle time,
World Addresses and Strata, bounded versioned World Grammar, semantic area
snapshots, persistent deltas, the developer World Atlas Inspector, the accepted
20-pixel compiled visual pack and palette, `VisualGrammar.Compose`, and the
Godot drawing adapters. `ChronicleSimulation` remains the external rule seam
used by Godot and Core checks, although its internal implementation may deepen
around successor action planning.

Collectible-Noun Loadout rules, fitted `Fly[Stone]` / `Fly[Bell]`, the Bell
Study fixture, the one-exchange Cairn conflict, and their fixture-specific UI
remain supported predecessor behavior until one authorized successor migration
replaces them. Do not preserve them as parallel successor interfaces or delete
their literal migration proof prematurely.

P-GEN is the required authoring-time visual compiler, co-located at
`tools/P-GEN` so compiler and consumer changes are atomic. Palimpsest consumes
its compiled content through a Palimpsest-owned `Chronicle.VisualPack` reader;
P-GEN never owns semantic World Grammar, Chronicle state, Target facts, or
gameplay generation. E5 makes the packaged P-GEN artifact the normal 20-pixel
player/Inspector path while the manual pack remains a golden verification and
explicit comparison fixture. See
[ADR 0004](adr/0004-use-p-gen-as-the-visual-authoring-pipeline.md) and
[ADR 0006](adr/0006-co-locate-p-gen-without-crossing-the-runtime-seam.md).

## Simulation shape

- The Chronicle Clock advances through fixed deterministic Heartbeats and
  supports pause and speed controls.
- The in-world calendar and day–night cycle derive deterministically from that
  same Clock. When no meaningful interruption is possible, advancing a long
  commitment may skip directly to the next relevant change instead of making
  the player watch elapsed time.
- Autonomous object state changes only on Chronicle ticks. While paused, Core
  subjects do not move, react, or change phase, and Godot freezes their
  time-driven presentation. Inspection, Loadout configuration, deliberate
  Incarnation movement, and action preparation remain commands; any action
  specified to resolve on a tick stays pending until that tick is delivered.
- A World Address ultimately identifies every persistent location by World,
  Stratum, Area, and coordinate across linked two-dimensional layers. Slice
  0–3 state has one implicit World and one implicit Area per Stratum; explicit
  World or Area identity requires a deliberate save migration.
- Areas are topology, not scale changes. Open territory, Holdings, combat,
  dungeons, temples, ruins, and nested spaces use one character-scale grid and
  one simulation. Persistent Passages connect them; no strategic overworld or
  separate tactical battle state may emerge. See
  [ADR 0005](adr/0005-use-one-character-scale-across-generated-areas.md).
- Physical scale belongs to semantic composition: a mountain is a multi-cell
  region with parts, not a one-cell peer of an actor or tree. A visual pack's
  pixel dimensions and camera density are presentation choices and may not
  redefine Chronicle distance.
- Generation is deterministic from a Chronicle seed plus durable deltas.
- Each Chronicle pins its World Grammar version so later generator changes do
  not silently rewrite untouched territory. See
  [ADR 0002](adr/0002-pin-world-grammar-version-per-chronicle.md).
- World Grammar answers bounded area requests in absolute World Addresses.
  Request bounds are presentation and verification windows, never generated
  World edges. Query order, camera movement, and any internal cache or chunk
  size may not change the returned semantic state.
- Near the Incarnation, state is resolved at full entity fidelity. Relevant but
  distant regions resolve as deterministic events; untouched territory remains
  generated on demand.
- A Chronicle may expand into additional Worlds and has no authored geographic
  edge. "Infinite" means deterministic generation on demand, never eager
  generation or full-fidelity simulation of unreachable territory.
- Creature Grammar assembles arbitrarily many deterministic Agent instances
  from bounded authored body plans, materials, traits, capabilities, ecologies,
  behaviors, and visual kits. Instances that participate in consequential
  Chronicle history receive durable identity; inactive possibility is not
  eagerly simulated.

### Accepted v5 language runtime

- The current Word Catalogue is authored domain data containing Verbs and
  Nouns. Chronicle.Core deterministically generates Study Sources, contextual
  word offers, and Understanding yield from Chronicle state; it never
  synthesizes new word meanings at runtime.
- One stable `WordId` is shared by catalogue definitions, Codex membership,
  word-specific Understanding, Study offers, and Loadout slots. Godot receives
  read-only definitions and generated snapshots; it never reconstructs word
  kinds, compatibility, eligibility, or yield.
- Starting Vectors grant authored first capabilities without choosing a class
  or locking later play. A mixed canonical Codex may independently equip its
  compatible predecessor Words through the bounded Loadout.
- Study Source snapshots are regenerated from pinned World Grammar semantics,
  durable identity, and World Address. Only canonical Codex membership,
  word-specific Understanding, and the active source/word pursuit are saved.
- Strict current save envelope v5 serializes canonical Chronicle state,
  including the Bell's durable Address, optional singular Home, the narrow
  first-conflict delta, and other durable deltas. Literal v4, v3, v2, v1, and
  pre-envelope saves deserialize through private predecessor shapes before
  constructing current state. Their exact supported World Grammar pins remain
  unchanged, so older Chronicles do not gain later generated subjects
  retroactively. Current `WordId` parsing remains string-only, so colliding old
  numeric values never cross into the unified identity model.
- Fitted `Fly` dispatches once by Verb and resolves the learned `Stone` or
  `Bell` Noun to one authored durable subject. This behavior remains a strict
  migration boundary; it does not define the successor grammar recorded by
  [ADR 0003](adr/0003-use-verbs-linked-modifiers-and-world-targets.md).

### Unimplemented successor language direction

- The target Word Catalogue contains authored Verbs and Modifiers. Targets
  retain World Addresses or durable subject identities and never become
  `WordId`s.
- Expressions configure one Verb and its linked Modifiers in the Loadout. An
  Invocation selects a Target and attempts to apply that Expression against
  current Chronicle state.
- Modifiers are reusable across Verbs, unique within one Expression, and
  order-independent. Each Verb and Modifier attachment contributes fixed
  authored Load independent of the selected Target.
- Targets expose facts and constraints, never an exact required Modifier
  recipe. Invalid Targets remain available to Core-owned preview with factual
  rejection reasons before an Invocation commits time or other cost.
- One coherent Core-owned rule must govern Target eligibility, preview, precise
  rejection, preparation, revalidation, and resolution so Godot and checks
  cannot grow parallel rulebooks. An immutable action plan is the current
  candidate interface, not a settled implementation.
- Exact Modifier applicability remains a pressure-test decision. It may use
  capabilities and constraints such as range, scale, area, duration,
  persistence, or notice; a broad operation-family label alone is not proof
  that every Modifier has coherent semantics for every Verb.
- Verb slots, link capacity, and shared Load are distinct Loadout constraints.
  Link count alone does not impose additive delay: ordinary combat and
  exploration remain responsive, while scale, persistence, chosen Modifiers,
  and any later material power economy determine whether an Invocation resolves
  tactically or as a longer Chronicle-time commitment.
- Core-owned action plans may use interruptible Preparation, release, delayed
  resolution, or Recovery, but no phase is universal. The actor's available
  alternatives, interruption points, and deterministic outcome remain Core
  decisions; Godot presents them and delivers commands and ticks.
- Combat is not an Invocation-only subsystem. Physical actions, equipment,
  terrain, Companions, and Expressions share Chronicle state and time.
  Companions remain autonomous Agents whose response to Directives is decided
  in Core, and neither Companions nor equipment consume Load merely by being
  present.
- Load is checked at Attunement, meaning any Loadout creation or change. For the
  first pass, losing a persistent Load Source changes capacity at the next
  Attunement, including one for a replacement Incarnation, and never invalidates
  the already-attuned Loadout remotely.

### Other accepted simulation rules

- Singular Home and its durable Hearthstone identity are Core state. Core also
  derives the read-only physical Return Route: it reports X-before-Y ordinary
  steps toward Home but never moves an Incarnation, teleports, or owns a Godot
  navigation rule.
- The first conflict is one Core-owned, one-exchange state transition rather
  than a general combat system. Entering its unresolved generated place pauses
  the Chronicle Clock; an exact Loadout action may be prepared while paused,
  and the first delivered tick resolves either the material consequence or the
  Incarnation's end. Godot only presents the read-only conflict context and
  translates commands.
- Slice 0 proves this model with a deliberately tiny generated surface patch.
  Slice 1 adds the first linked sky Stratum; neither slice attempts a whole
  planet or a voxel world.

## Ownership

| Concern | Owner |
| --- | --- |
| Rules, time, identities, Invocation planning, Target validity, Loadouts, Home/Return Route, persistence, generation | `Chronicle.Core` |
| Input translation, rendering, scene lifecycle | `Chronicle.Godot` |
| Versioned compiled-pack values and the authored Gate 3B reference pack | `Chronicle.VisualPack` |
| Deterministic semantic-to-visual composition and transient render plans | `Chronicle.Visuals` |
| Texture loading, drawing, visual preview, and UI layout | `Chronicle.Godot` |
| Authoring-time visual compilation | `tools/P-GEN`; pure C# Module inside the repository and outside every production dependency/package graph |
| Developer World Grammar inspection and capture | `Chronicle.Godot`, reading the same Core area snapshots as the player view |
| Tests for domain and visual composition rules | .NET check projects outside Godot |

The target drop-in seam for any procedural visual engine is a versioned
compiled pack, not the engine's source catalogue or implementation. The
compiler runs at authoring time without Godot. A game-specific pure C# composer
maps Core snapshots and the pack to a transient render plan; Godot draws that
plan. See the
[Chronicle Visual Engine specification](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md).

P-GEN integration preserves one Palimpsest-owned consumer path.
`CanonicalVisualPackReader` validates and constructs the existing
`CompiledVisualPack`; `PackagedVisualPackLoader` supplies it to both Godot
consumers;
`Chronicle.Visuals` owns semantic mapping, motif-placement interpretation, and
address-complete cosmetic selection; Godot only draws. The shipped game may not
reference the compiler, parse its catalogue, duplicate hidden authoring rules,
or treat P-GEN's private runtime utility as an accepted seam. The
[P-GEN E4.5 readiness review](P-GEN-E4-5-READINESS-REVIEW.md) records the
evidence and acceptance requirements for the required E5 gate.

Gate 3B proves that seam with `ManualVisualPack.CreateGate3B(16|20)`,
`VisualGrammar.Compose(...)`, and one `WorldVisualView`/raster adapter shared
by the player and Inspector. The one-cell semantic halo is a composer input,
not a public chunk abstraction; at the numeric storage limits it contains
every representable neighbor and does not wrap or invent an authored World
edge. The plan and selected cosmetic variants are reproduced from seed,
absolute address, style version, and presentation emphasis; none enters the
Chronicle save.

## Non-negotiables

- No shipped GDScript and no gameplay rule hidden in a Godot scene.
- No gameplay decision depends on reflex speed.
- Simulation state stays serializable and inspectable.
- Visual Grammar may derive deterministic cosmetic variation from semantic
  snapshots, but it may never change simulation meaning.
- Runtime packaging includes compiled visual packs, not compiler source,
  authoring catalogues, review artifacts, or a compiler process.
- A developer World Grammar inspector may pan, zoom, and render large bounded
  requests, but it may not pre-generate a whole Stratum, mutate player saves,
  or introduce a second generation path. Large overviews use a bounded raster
  or batched draw representation rather than one Godot Node per World Address.
- Every playable Area uses the same movement, Combat, building, Heartbeat, and
  persistence rules. Loading through a Passage may change the active bounded
  snapshot, but never the underlying spatial or tactical model.
- Any future Palimpsest interaction is a bounded, deterministic Core state
  transition with an inspectable persistent result. Godot never interprets
  arbitrary prose as a simulation rule.
- Palimpsest generation is versioned World Grammar with durable identity and a
  hard maximum of two artifacts per World. Most Worlds may generate fewer.
- Social Verbs determine whether a Directive can be attempted. Agent response
  remains a separate Core decision, so Command never becomes guaranteed unit
  control in Godot.
- Targets are Core-owned Chronicle subjects or places with material facts and
  durable identity where required. Godot may request or present a Target but
  never decides whether an Expression can affect it.
- Linked Modifiers remain bounded authored transformations over a Core action
  plan. Triggering and chaining may not become freeform player programming,
  unbounded recursion, or prose interpreted as a rule.
- Home is a durable Core domain identity attached to one Holding, not a Godot
  scene, automatic teleport destination, or UI mode. Its Return Route is a
  read-only physical X-before-Y guide that never moves the player. Any eventual
  return Expression is a separate explicit Core rule over persistent addresses.
- Add native code only after profiling identifies a measured bottleneck.
