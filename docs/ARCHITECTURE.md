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
fixed-tick Chronicle state + persistence + World Grammar
```

## Simulation shape

- A continuous fixed-tick Chronicle Clock supports pause and speed controls.
- A World Address ultimately identifies every persistent location by World,
  Stratum, and coordinate across linked two-dimensional layers. Slice 0–3 state
  has one implicit World; multi-World work must introduce explicit World
  identity through a deliberate save migration.
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
- The Word Catalogue is authored domain data. Chronicle.Core deterministically
  generates Study Sources, contextual word offers, and Understanding yield
  from Chronicle state; it never synthesizes new word meanings at runtime.
- Slice 0 proves this model with a deliberately tiny generated surface patch.
  Slice 1 adds the first linked sky Stratum; neither slice attempts a whole
  planet or a voxel world.

## Ownership

| Concern | Owner |
| --- | --- |
| Rules, time, identities, persistence, generation | `Chronicle.Core` |
| Input translation, rendering, scene lifecycle | `Chronicle.Godot` |
| Versioned compiled-pack values and the authored Gate 3B reference pack | `Chronicle.VisualPack` |
| Deterministic semantic-to-visual composition and transient render plans | `Chronicle.Visuals` |
| Texture loading, drawing, visual preview, and UI layout | `Chronicle.Godot` |
| Authoring-time visual compilation | Pure C# compiler outside the shipped runtime |
| Developer World Grammar inspection and capture | `Chronicle.Godot`, reading the same Core area snapshots as the player view |
| Tests for domain and visual composition rules | .NET check projects outside Godot |

The target drop-in seam for any procedural visual engine is a versioned
compiled pack, not the engine's source catalogue or implementation. The
compiler runs at authoring time without Godot. A game-specific pure C# composer
maps Core snapshots and the pack to a transient render plan; Godot draws that
plan. See the
[Chronicle Visual Engine specification](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md).

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
- Any future Palimpsest interaction is a bounded, deterministic Core state
  transition with an inspectable persistent result. Godot never interprets
  arbitrary prose as a simulation rule.
- Palimpsest generation is versioned World Grammar with durable identity and a
  hard maximum of two artifacts per World. Most Worlds may generate fewer.
- Social Verbs determine whether a Directive can be attempted. Agent response
  remains a separate Core decision, so Command never becomes guaranteed unit
  control in Godot.
- Home is a durable domain identity attached to one Holding, not a Godot scene,
  automatic teleport destination, or UI mode. Any eventual return Expression
  is an explicit Core rule over persistent addresses.
- Add native code only after profiling identifies a measured bottleneck.
