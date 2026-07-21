# Modifier Grammar Course Correction

**Decided:** 2026-07-20
**Refined:** 2026-07-21
**Status:** accepted product direction; production successor not implemented or
authorized

This document replaces the original modifier-grammar proposal after further
design pressure testing. It records the smallest settled direction, the current
predecessor boundary, and the questions a player-facing pressure test must
answer before production work.

Canonical authority remains [AGENTS.md](../AGENTS.md), the
[glossary](../CONTEXT.md), [Vision](VISION.md),
[Architecture](ARCHITECTURE.md), [Roadmap](ROADMAP.md), and active
[Handoff](HANDOFF.md). [ADR 0003](adr/0003-use-verbs-linked-modifiers-and-world-targets.md)
records the hard-to-reverse grammar decision.

## Why the course changed

Slice 5 correctly proved that one shared rule could resolve `Fly[Stone]` and
`Fly[Bell]`. The player's later post-UAT assessment was that this still was not
fun enough: choosing a learned Noun mostly selected a different subject for the
same operation. It multiplied content and migration work without producing
enough new decisions.

The project will not expand that axis by inertia. Slice 5 remains accepted
deterministic, persistence, and migration evidence for the v5 runtime, but it
is a predecessor model rather than the future language.

## Settled grammar

```text
Verb + linked Modifiers -> Expression
Expression + contextual Target -> Invocation -> bounded result
```

- A **Verb** is a discoverable Power Word defining the base magic: `Fly`,
  `Burn`, `Smash`, or another authored capability.
- A **Modifier** is a discoverable Power Word linked to a Verb to change how it
  acts: reach, speed, scale, area, duration, persistence, precision, notice,
  collateral, or another bounded authored property.
- A **Target** is an actual subject or place in Chronicle state. It is not a
  Word in the Codex.
- An **Expression** is one active Verb and its linked compatible Modifiers,
  configured in the Loadout independently of any particular Target.
- An **Invocation** is one attempt to apply an active Expression to a chosen
  Target.
- A **Link** is one occupied position in the connected Word chain. The Verb
  occupies the first Link, so a six-link Expression is one Verb plus five
  Modifiers.
- **Load** is the shared fitting burden across every active Verb and Modifier
  in the Loadout.

The three axes answer different player questions:

| Axis | Question |
| --- | --- |
| Verb | What magic occurs? |
| Modifiers | How does it act? |
| Target | Which existing thing am I trying to affect? |

## Composition laws

- Learning a Modifier makes it reusable Codex knowledge. It may support more
  than one active Verb, paying its Load on every attachment, but cannot appear
  twice in the same Expression.
- Modifier order has no semantics. The same Words always form the same
  Expression regardless of selection order, and presentation uses one canonical
  order rather than exposing a programming sequence.
- Every Verb and Modifier attachment has fixed authored Load. The selected
  Target may change eligibility, preparation, material power, danger, or
  consequence, but never the configured Expression's Load.

## Targets make the world matter

Targets expose authored Chronicle facts such as matter, mass, scale,
resistance, identity, agency, position, and current state. An Invocation
succeeds, fails precisely, or produces a bounded consequence against those
facts. Targets never contain an exact required Word string; different chains
may overcome the same constraints in different ways.

Invalid Targets remain selectable for inspection and preview. The player sees
factual gaps such as excessive mass, an anchored foundation, or the need for
continuous lift without spending time and without being handed the exact
missing Modifiers.

`Fly` aimed at the acting Incarnation can work with its ordinary limits.
`Fly` aimed at a mountain should fail because the Target exceeds those limits,
not because the player lacks a collectible `Mountain` Noun. A sufficiently deep
chain could eventually extend scale, force, stability, and persistence until a
floating mountain base becomes possible.

This is the desired long-range power fantasy: new Words change which constraints
the player can overcome, while generated and persistent world subjects provide
the things worth overcoming them for.

## Build constraints

The Loadout has three distinct constraints:

1. **Verb slots** limit breadth.
2. **Link capacity** limits the depth of one Expression.
3. **Shared Load** forces trade-offs across the whole build.

The player may prefer several lightly linked Verbs or commit much of the build
to one extreme chain. An illustrative early budget could be 16 Load, with a
basic Verb costing 1 and a transformative speed Modifier such as `Quickly`
costing 6; those values are pressure-test inputs, not final balance.

Individual discoveries may add one Load, but most long-term capacity comes from
vulnerable Load Sources built at Home. Capacity is checked at Attunement. If a
Raid destroys a Source, the current expedition keeps its already-attuned
Loadout; the loss applies at the next Attunement, including configuration for a
replacement Incarnation. Rebuilding the Source restores that capacity. Lesser
damage and repair thresholds remain unsettled.

This first-pass rule prevents an off-screen event from disabling `Fly` over a
chasm while still letting raids attack future build depth. Live tethers,
partial suspension, overload priority, and other harsher limits remain later
possibilities. Exact link-capacity unlocks are still unsettled and may not come
from repeated casts.

## Runtime price and Chronicle time

Link count is not a universal cast-time surcharge. Some players must be able to
fight and explore with deep builds without watching progress bars.

- Ordinary tactical Invocations resolve immediately or on the next meaningful
  Chronicle step.
- Powerful local Invocations may take several simulation steps and permit
  interruption.
- Massive, durable, or world-scale Invocations may consume in-world hours or
  days.
- Safe waiting advances to the next meaningful interruption rather than making
  the player sit through elapsed time.

Day and night derive from the same deterministic Chronicle Clock. A Modifier
such as `Quickly` can compress preparation, including moving an otherwise
ritual-scale act toward tactical time, by transferring cost into shared Load,
material power, instability, notice, collateral, or another legible
consequence.

`Quickly` should consume enough Load to threaten other desirable links. Without
it, a large Invocation may leave the actor exposed to attack during preparation;
with it, the player buys safety or tempo by giving up breadth or other depth.

## Combat is larger than Invocation

Expressions are one action family inside Combat, not a replacement for physical
fighting. Movement, striking, shooting, guarding, equipment, terrain, and
autonomous Companions must remain relevant alongside language.

Different actions may use different timing shapes. A prepared Invocation can
occupy its actor and be interrupted; a released consequence can resolve later
while the actor moves or fights; Recovery can prevent immediate repetition
without preventing other actions; and a true ritual can demand protection or
infrastructure. No action needs every phase, and no phase may become passive
waiting.

Companions choose their own attacks and reactions from identity, relationship,
current danger, and accepted Directives. Taming may improve a beast's bond and
learned behavior, while leadership may improve group coordination and
willingness. Neither becomes per-tick unit control. Companions and equipment do
not consume Load merely by being present; their costs belong to relationships,
loyalty, material support, risk, and the relevant capabilities.

This supports a fast battlemage using weapons between shallow Expressions, a
protected caster preparing one deep Invocation, a tamer relying on autonomous
beasts, a leader coordinating several Agents, and a solo ritualist creating
time through terrain, traps, or delayed effects. Exact action lists, combat
conditions, and balance remain unsettled.

### First candidate combat shell

The first combat-shaped pressure test should begin with exactly one Weapon
slot, one Armor slot, and one Accessory slot; visible HP bars; weapon attacks
whose authored timing resolves on Chronicle ticks; and `Burn` as the first
combat Invocation. This is enough to compare physical tempo with language
without first building inventory breadth, a bestiary, or a general combat
framework.

Exact weapon commands, damage and mitigation arithmetic, healing, Accessory
effects, HP persistence, and `Burn` semantics remain pressure-test decisions.
The accepted v5 Cairn conflict implements none of this; it remains the preserved
one-exchange predecessor proof.

An Invocation cooldown is best expressed through the existing Recovery concept,
but tying Recovery only to a combat flag is not settled. A mode-gated clock
creates an immediate boundary exploit: leaving and re-entering danger could
freeze, clear, or restart the cooldown. The simplest candidate is for Recovery
to advance on Chronicle ticks everywhere; when the actor is safe, meaningful
time skipping removes the wait, while retreat never erases elapsed state. A
derived danger indicator may still control UI emphasis and automatic pausing
without becoming a separate combat rules mode.

A stored material power resource and Home infrastructure are promising ways to
support extreme chains without adding a generic mana bar. Neither the resource
name nor its generation, carrying, recharge, or link-unlock rules are settled.
Basic combat and exploration may not depend on a developed settlement.

## Core seam to pressure-test

The likely deep module is an immutable action plan constructed in Core from the
configured Expression, actor, Target, and Chronicle state for one Invocation.
One rule should own preview, Target eligibility, cost, precise rejection,
preparation, revalidation, and deterministic resolution.

Modifier compatibility should follow the capabilities and constraints present
in that plan rather than assume that every member of a broad operation family
supports the same semantics. Triggered, chained, reflected, or recursive
Modifiers are deferred until simple geometry, scale, persistence, and time have
proved fun; the system may not become freeform player programming.

## Required pressure test

The next gate should be disposable or isolated from production saves. Use the
smallest set that can establish the player decision:

- a few Verbs and Modifiers rather than a paper catalogue;
- contextual Targets with at least one material or scale constraint;
- shared Load that makes one desirable link impossible to fit;
- one expensive speed Modifier whose omission exposes an Invocation to
  interruption;
- invalid-Target preview that explains constraints without prescribing Words;
- one changing threat or expiring opportunity so speed has meaning;
- one Weapon, Armor, and Accessory slot, visible HP, tick-timed weapon attacks,
  and `Burn` as the first combat Invocation;
- at least one persistent material consequence;
- no save migration, broad resource economy, Agents framework, or Palimpsest
  implementation.

Pass only when the player:

1. predicts how one Verb changes under different links;
2. deliberately omits an available desirable Modifier;
3. can defend at least two builds with no obvious correct answer;
4. produces one surprising systemic consequence against an actual Target; and
5. wants to retry with a different chain.

An engineering check that a second Modifier can be added cleanly is necessary
but insufficient. The gate exists to prove the grammar fun before the project
pays for a successor save model.

## Long horizon, not current promises

Home may become infrastructure for world-scale Expressions. Palimpsests may
offer mutually exclusive authored transformations such as Awakening a Verb or
Modifier beyond one ordinary limit. Around the two-hundred-hour horizon, these
choices, additional Worlds, and World Claims may open the Chronicle's true
endgame chase rather than end play.

Exact Awakened Words, resource rules, link limits, unlock sources, catalogue
size, and Palimpsest affordances remain deliberately unsettled.

## P-GEN sequencing

P-GEN reader plumbing remains technically separable, but E5 vocabulary freeze
and conformance must wait until the successor power-word vocabulary settles.
Neither P-GEN integration nor E5 is authorized by this course correction.
