# Architecture

## Boundary

Godot 4 .NET is the self-contained shell for rendering, input, UI, audio, and
authoring. C# is the sole production language. The deterministic Chronicle
simulation must compile and test without Godot scenes, Nodes, or frame timing.

```text
Godot scenes and C# Nodes
        ↓ input / presentation
Chronicle.Godot adapter
        ↓ commands / snapshots
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
| Visual Grammar, visual assets, and UI layout | `Chronicle.Godot` |
| Tests for domain rules | .NET test project outside Godot |

## Non-negotiables

- No shipped GDScript and no gameplay rule hidden in a Godot scene.
- No gameplay decision depends on reflex speed.
- Simulation state stays serializable and inspectable.
- Visual Grammar may derive deterministic cosmetic variation from semantic
  snapshots, but it may never change simulation meaning.
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
