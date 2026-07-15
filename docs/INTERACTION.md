# Knowledge-gated Lens and Patch interaction

## Status

Implemented for the 0.2 vertical slice. The normal inspector is the structured
Lens; the raw-source inspector is available only in explicit developer mode.
Behavior is readable but not yet Patchable, and Prototype Reach remains hidden
from ordinary Knowledge.

## Intent

Opening part of the Universe must feel like learning how reality describes
itself, not like opening a developer console. Normal play is mouse-first and
structured. Knowledge determines which meanings can be perceived, how
precisely they can be represented, which can be changed, and how far a Patch
may Reach.

PALI remains the executable language beneath the interaction. The Lens edits a
typed PALI document through safe controls and Clauses; it does not expose raw C
state or accept arbitrary source text.

## Product decisions

1. **The Lens is the normal interface.** State uses typed controls; Behavior
   and Law use readable Clauses. Raw source is reserved for developer tooling
   and a possible late root-level Lens.
2. **Knowledge shapes the interface.** Unknown concepts are not all advertised
   as locked rows. A concept can be absent, veiled as a glyph, readable, or
   patchable.
3. **Reading and changing are different.** Revelation may make a value or
   Clause readable without granting the ability to Patch it.
4. **Depth and Reach are independent.** Knowing how one apple nourishes does
   not grant the ability to alter every apple.
5. **Early Patches bind to one Entity.** The existing shared-Prototype
   mechanism becomes explicit, validated Prototype Reach in 0.2. It remains
   available to tests and developer Knowledge but is withheld from normal
   Knowledge until late progression.
6. **The player edits fictional meaning, not host bookkeeping.** Stable memory
   addresses, bytecode offsets, dirty flags, RNG internals, buffer capacities,
   and other C implementation details are never universe attributes.
7. **Patches are sparse semantic overlays.** Changing one apple's nourishment
   must not copy and freeze its unrelated Behavior or other inherited meaning.
8. **A Patch is transactional.** The live Universe changes only after the
   complete typed document passes Knowledge, Reach, structural, compilation,
   and execution-budget validation.

## What can be opened

The promise “if it exists, the player can eventually open it” applies to
in-fiction semantic things registered with the Universe: Entities, their
attributes and Behavior, Lineages, Archetypes, Laws, memories, names, and
relationships. It does not mean that the player can edit arbitrary process
memory or engine infrastructure.

Some semantic attributes may remain readable but structurally protected. An
Entity may reveal its stable identity without allowing that identity to be
reassigned. Causing an Entity to cease existing is a typed fictional effect;
deleting its storage or removing a required syntax node is not.

## Lens structure

The Lens is organized by Access Depth. A deeper section does not appear until
Knowledge can at least sense that layer.

| Access Depth | Structured presentation | Examples |
| --- | --- | --- |
| State | Facets containing typed values | color, warmth, ripeness, mass, ownership |
| Behavior | ordered trigger, condition, and effect Clauses | when used, reduce hunger, consume self |
| Lineage | ancestry and inheritance paths | parent tree, future fruit, inherited Patch |
| Archetype | semantic membership conditions | what presently qualifies as an apple |
| Law | typed relations spanning meanings | nourishment, gravity, naming, death |

The first apple Lens is approximately:

```text
APPLE                                      THIS APPLE

SENSORY
  color          [red swatch]                    readable

VITAL
  nourishment    [ - ] [■■■■□□] [ + ]           patchable
  ripeness       [ripe]                          readable

BEHAVIOR
  WHEN [used by a living entity]                 readable
  DO   [reduce their hunger by nourishment]
  THEN [this entity is consumed]

                         [DISCARD]  [INSCRIBE]
```

There is no “every apple” selector in this Lens. The target summary changes
only after Prototype Reach itself has been revealed.

State is divided into Facets rather than exposing a flat property table:

- **Sensory**: color, apparent heat, sound, smell, visible condition.
- **Material**: mass, hardness, shape, composition.
- **Vital**: ripeness, hunger, injury, growth, mortality.
- **Relational**: owner, parent, target, promise, allegiance.
- **Historical**: origin, prior Patches, remembered events, Provenance.
- **Metaphysical**: name, existence, temporal status, matched Archetypes and
  Laws. This Facet belongs to very deep Knowledge.

Facets are vocabulary, not fixed tabs that must always be shown. An early
player may see part of Sensory and Vital without knowing that Material or
Historical meaning exists.

## Concept Access

Knowledge projects each attribute, operator, and Clause into one of four
states:

| State | Lens behavior |
| --- | --- |
| Unperceived | Nothing is rendered; the interface does not leak that the concept exists. |
| Veiled | An unresolved mark or phrase is visible, but its name and value are not exposed. |
| Readable | Meaning is shown but has no mutation control. |
| Patchable | Meaning is shown through a typed control and may enter a Patch Draft. |

Knowledge is normally persistent and monotonic. A temporary condition may
prevent applying a known operation without erasing the player's understanding.

Representation precision is also learned. A readable nourishment concept may
first appear as a qualitative meter, then as an exact number after the player
learns a suitable notation. Likewise, color may begin as a swatch and acquire a
name or encoded value later.

## Lexicon

The Lens must not hard-code special UI branches for strings such as
`nutrition`, `ripe`, or `color`. PALI names resolve through the Universe's
bounded Lexicon. Each concept entry supplies at least:

- stable concept identity and PALI name;
- semantic value type;
- Access Depth and Facet;
- permitted operations and whether the node is structurally protected;
- qualitative and exact presentation notations;
- semantic bounds, units, and step where applicable;
- which Concept Access states normal Knowledge currently grants.

Knowledge references stable concept identities rather than source positions.
A property present in bytecode is not automatically visible or mutable. It
must belong to the Lexicon and survive the Knowledge projection. Custom concept
creation is an ascended capability; the first implementation uses a small
fixed-capacity Lexicon covering the base clearing.

## Reach

Reach controls which population a valid Patch may bind to. It is not a generic
scope dropdown presented at the start of the game.

| Reach | Meaning | Resulting Provenance |
| --- | --- | --- |
| Entity | Only the selected material Entity | Local Override or sparse Entity Scar |
| Lineage | A descent relationship, usually affecting future descendants | Lineage definition |
| Prototype | Present and future Entities resolving through one Prototype | Shared Prototype Patch |
| Archetype | Any Entity satisfying a semantic membership definition | Archetype Patch |
| Universe | A relation belonging to the Universe itself | Law Patch |

Normal early Knowledge grants Entity Reach only. The Lens therefore says
“THIS APPLE” as a target summary; it does not show a menu containing four
padlocked future options.

Prototype Reach must exist in the engine, validator, save format, tests, and a
developer Knowledge profile now. It must not appear in the normal Lens until a
late Revelation grants that Reach. When it eventually appears, the preview
must distinguish materialized affected Entities from future Entities that will
inherit the Prototype.

Provenance resolves independently for each stable semantic node. Local or
narrower Provenance wins only for the values and Clauses it addresses; all
untouched nodes continue resolving from broader definitions. A Prototype Patch
does not silently erase an Entity Scar, and an Entity Patch does not snapshot
the complete Prototype.

For example:

1. One apple receives an Entity Patch changing only nourishment from 20 to 5.
2. A later Prototype Patch changes the apple use message and color.
3. The patched apple keeps nourishment 5 while inheriting the new message and
   color; other apples use the Prototype's nourishment.
4. A still later Prototype nourishment change affects the other apples but not
   the Entity node already carrying the narrower Scar.

Early broad Patches may not remove or change the type of a semantic node that
has materialized narrower Patches. The preview reports the conflict and rejects
the candidate. Explicit orphaning or conflict reconciliation belongs to a
later history milestone.

## Mouse-first interaction

1. Hovering a perceivable Entity gives it a restrained outline or glyph.
2. Left-clicking selects and opens that Entity. `E` remains a keyboard
   accelerator for the nearest Entity.
3. The Universe remains visible behind or beside the Lens so the target retains
   physical context. Simulation pauses while a Patch Draft is open.
4. Clicking a readable value explains it and, when known, shows Provenance.
5. Clicking a patchable value changes a draft through its typed control.
6. The Lens continuously summarizes the target and Reach without mutating the
   Universe.
7. **INSCRIBE** validates and commits the Patch. **UNDO** changes the draft;
   **CLOSE** discards it. Keyboard shortcuts remain accelerators, not required
   knowledge.

The first implementation does not require drag-and-drop. Clause rows, choice
menus, add buttons, and target pickers provide complete mouse operation without
making precise dragging part of the grammar.

## Typed controls

- **Number**: qualitative meter before notation is learned; bounded stepper,
  slider, or exact field afterward. The semantic schema supplies units, range,
  and step.
- **Boolean**: a pair of meaningful states such as ripe/unripe, not a raw
  `true`/`false` toggle unless Boolean notation is known.
- **Enumeration**: a set of known, type-compatible choices.
- **Text or name**: bounded input only when language and naming concepts permit
  it.
- **Color**: swatches first; names or encoded values only after their notation
  is known.
- **Entity relation**: a target-picking mode constrained to compatible,
  perceivable Entities.
- **Behavior**: typed Clauses with compatible sockets and ordered effects.

The allowed range is part of fictional semantics, not merely input hygiene.
Deeper Knowledge may reveal a wider range or a different operator without ever
allowing malformed data.

## Behavior Clauses

Behavior is presented as a sentence-like structured document:

```text
WHEN  [used by] [a living entity]
DO    [reduce] [their hunger]
BY    [this nourishment]
THEN  [this entity is consumed]
```

Each Clause declares its input and output types, required concepts, allowed
operators, structural role, and execution cost. A player can choose only known
and type-compatible replacements.

A protected trigger cannot be accidentally deleted. An optional effect may be
removed only if the remaining document is structurally meaningful. “This
Entity is consumed” compiles to a safe fictional host operation; it never
deallocates arbitrary C storage.

The first structured Lens may render Behavior as readable but not patchable.
Clause composition becomes its own complete milestone after State interaction
and Knowledge projection are proven.

## Patch lifecycle

1. Resolve the selected Entity and its current Provenance.
2. Parse its normalized PALI source into a bounded typed document.
3. Project the document through Knowledge into a Lens model.
4. Record mouse or keyboard changes as sparse operations against stable typed
   node identities in a separate Patch Draft.
5. Validate Concept Access, Access Depth, Reach, value types, semantic ranges,
   protected structure, capacities, and execution budget.
6. Show a target and effect preview appropriate to known concepts.
7. Compile a complete candidate document without touching the live Universe.
8. On success, atomically install it and record its Scar and Provenance.
9. On failure, preserve the live definition and explain the Anomaly using only
   concepts the player can understand.

## Target architecture

PALI needs a bounded typed document between parsing and bytecode generation:

```text
Lexicon + normalized PALI source -> typed PALI document -> validated bytecode
                                           ^
                                           |
                             Knowledge projection + Lens
                                           |
                                      Patch Draft
```

Typed documents and sparse Patch operations are the canonical mutable model. A
deterministic formatter serializes them as normalized, human-readable PALI for
saves, diagnostics, and developer/root source view. The executable
`PaliProgram` remains a separate bounded bytecode product and is never edited
by the UI. Stable node identity comes from semantic concept and Clause identity,
not source line or bytecode offset.

The structured path must retain the existing trust contract: fixed capacities,
no unbounded allocation during simulation, host-call whitelisting, candidate
compilation, and a last-known-valid live program.

## 0.2 acceptance slice

The first Lens milestone is complete when:

1. A visible Entity can be selected and opened entirely with the mouse.
2. Normal play shows the structured Lens instead of the raw text editor.
3. The apple exposes only a small configured set of State Facets through typed
   controls. At least nourishment is Patchable.
4. The existing `on use` Behavior is rendered as readable Clauses but remains
   non-patchable in the normal Knowledge profile.
5. Applying a nourishment change creates a sparse Entity-bound Local Override.
   A second apple remains unchanged, while later unrelated Prototype changes
   still reach the locally patched apple.
6. Normal play shows “THIS APPLE” and does not reveal Prototype Reach.
7. A developer Knowledge profile can apply and verify a Prototype Patch to
   every apple, including save/load restoration.
8. The current source editor is available only through an explicit developer
   mode and round-trips through the same typed document and validator.
9. Rejected drafts cannot mutate the Universe, and required structure cannot be
   removed through the Lens.
10. Tests cover Knowledge projection, Concept Access, Reach rejection,
    node-level Entity-versus-Prototype resolution, unrelated inheritance after
    a Local Override, typed-document round-trip, and save restoration.

## Deferred

- Discovering concepts through ordinary play rather than a fixed test profile.
- Patchable Behavior Clauses.
- Lineage, Archetype, and Universe Reach.
- Causal cost, resistance, gods protecting definitions, and broad-impact
  preview simulation.
- Controller interaction.
- Player-visible raw source and root-level free composition.
