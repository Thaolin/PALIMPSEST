# Architecture

## Implemented boundaries

- `src/main.c`: raylib window/input/timing, executable-relative asset path,
  default save path selection, and the fixed-step accumulator.
- `src/ui.c`: integer-scaled world rendering, the structured Lens, typed
  Behavior Clause controls, the future-fruit Lineage form and exact preview,
  the Inquiry panel, and the developer-only fixed-capacity source editor.
- `src/lexicon.c`: stable concept identities, Facets, semantic types, bounds,
  operations, and Reach vocabulary.
- `src/world.c`: deterministic generation, collision, survival simulation,
  tree-to-fruit births, Entity/Lineage/Prototype value and Behavior resolution,
  state-derived Inquiries, per-creature RNG, and PALI host bindings.
- `src/pali_lexer.c`: source characters to tokens with line/column locations.
- `src/pali_compiler.c`: recursive-descent parser, bounded typed document,
  deterministic formatter, semantic validation, and bytecode compiler.
- `src/pali_vm.c`: typed runtime values, stack VM, arithmetic, budget, and host
  callback boundary.
- `src/save.c`: portable little-endian v5 save encoding, checksum validation,
  sparse value and handler materialization, and reconstruction from Genesis
  with v2/v3/v4 migration.
- `src/platform.c`: isolated atomic replacement, durable file flush, directory,
  and default user-save behavior.

The core library has no raylib dependency. Tests compile and run the language,
world, and persistence paths without opening a window.

## Data flow

At startup, the platform supplies explicit asset and save paths. A new world
loads and compiles base `.pali` definitions, then terrain and Entity Genesis use
separate RNG streams derived from the displayed root seed. Each tree
subsequently owns a bounded fruit timer and birth ordinal. A load performs the
same Genesis, restores post-Genesis descendants, and applies stored Prototype
patches, Lineage definitions, sparse Entity or birth-captured Scars, Entity
changes, local Behavior handlers, Knowledge, and Embodiment state.

During play, raylib input becomes a `WorldInput`. The accumulator advances the
world only in 1/60-second steps. Inspecting pauses simulation. The normal Lens
derives visible rows from the selected Entity's typed document and projects
them through Knowledge. Attending records a distinct Prototype kind against a
stable concept ID; it can change only Knowledge. Patch controls record a Draft
against a stable concept ID. Inscribing compiles a resolved candidate and
commits only after validation. Developer source follows the same
parse-document-compile pipeline. Right-clicking a nearby visible Entity or
pressing `F` for the nearest one executes its effective `on use(actor)`
Behavior. Host property resolution composes any narrower Entity value through
a callback-only whitelist even when the handler itself has local Provenance.

Inquiries are projections, not another persistence store. Their progress and
active ordering derive from existing Entity Scars, Entity activity,
Observations, Notation, Parentage, tree birth counters, Lineage definitions,
inherited births, and Knowledge. Completing The First Scar reconciles one
monotonic Knowledge grant: Behavior Access Depth plus readable hunger. After a
Behavior Scar and a materialized descendant exist, The Fruit Remembers
reconciles Lineage Depth and Reach plus readable Parentage, warmth, and vigor. The UI
shows both transitions explicitly, while its current/completed Inquiry index is
reconstructed from the same state after load.

The application reconciles derived Knowledge at the startup and successful
reload boundaries, after Genesis or transactional save reconstruction has made
the complete state available. `save_load` reconstructs and validates its
candidate; it does not itself own that boundary reconciliation or its UI
feedback.

The presentation canvas is 720x405 and reaches the default 1440x810 window at
an exact 2x scale. World geometry remains in its original 320x224 coarse
coordinate space and is transformed by 3/2 into a 480x336 viewport. HUD and
editor text draw natively in presentation coordinates. Raising UI resolution
therefore cannot alter generation, collision, fixed-step movement, or saves.

An Entity's State property resolution is:

1. sparse instance state;
2. sparse local value for that stable entity ID and concept, carrying Entity or
   birth-captured Lineage Provenance;
3. shared prototype program.

Behavior handler resolution is independent:

1. normalized local handler for that stable Entity ID, carrying Entity or
   birth-captured Lineage Provenance, when present;
2. the shared Prototype handler otherwise.

The 32-slot Local Override pool stores up to four concept-addressed typed values
and an optional typed Behavior draft under one stable Entity binding. The
12-slot Lineage pool stores sparse future-fruit definitions by progenitor tree.
This bounded layout remains within the 256 KiB compile-time `World` cap.
Value nodes and the handler retain separate Provenance: either may exist or be
reverted without removing the other. A local handler is resolved and compiled
with the current Prototype document, while its `self.property` reads still use
the State chain above. Consequently a locally nourished apple still inherits
later unrelated Prototype color and Behavior changes until its Behavior is
separately Patched; an apple with a local handler still sees later Prototype
property changes unless that property has its own narrower value Scar.
Prototype source mutation exists only in the explicit developer profile; the
normal Lens exposes only Knowledge-granted Entity or Lineage Reach and does not
reveal the broader Prototype target.

## Lineage and birth

A tree bears at most one active descendant apple. Its initial timer is derived
from its stable ID; when its fruit ceases, the timer is reset to 300 fixed
steps. A successful birth increments the tree's ordinal. Capacity or placement
failure leaves no partial active child and retries after a bounded 60-step
delay.

Descendant identity is a deterministic mix of Genesis seed, progenitor ID,
birth ordinal, and apple Prototype. Placement and nourishment Inflection also
derive from the tree ID and ordinal, so unrelated random consumption cannot
shift them. The nourishment Inflection is an integer in `[-2, 2]`, added to the
Lineage or Prototype base and clamped to the nutrition concept's `0..100`
range. The tree Lens uses the same function to show the exact next result.

A Lineage definition is applied to a child only when that child materializes.
The child captures nourishment when the Lineage addresses it or the birth
Inflection differs from broader meaning, and captures Behavior only when that
Lineage addresses Behavior. Both carry the progenitor ID and Lineage Reach as
Provenance. Untouched nodes still resolve from the current apple Prototype. No
child performs a live ancestry lookup, so later tree edits cannot retroactively
alter already materialized fruit.

The structured Behavior grammar has four optional effect groups in execution
order: hunger, Aftertaste, voice, and fate. `KINDLE` adds nourishment to warmth;
`QUICKEN` adds nourishment to vigor. Vigor decays by `0.05` each fixed step and
increases movement speed by `0.5%` per remaining point. Aftertaste therefore
runs before a possible `CEASE` fate. Its socket remains veiled at Behavior
Depth and becomes readable and patchable with Lineage Knowledge.

## Identity layers

`World` contains three peer stores rather than treating the player as the
universe:

- `UniverseState`: root seed, tick, terrain, entities, definitions, overrides;
- `KnowledgeState`: known concepts, access capability bits, concept-indexed
  notation bits, and bounded per-concept Prototype Observation masks;
- `EmbodimentState`: stable body ID, position, hunger, warmth, and decaying
  vigor.

This is the seam for future knowledge progression and embodiment transfer; no
speculative transfer system is built.

## Future Semantic Construction boundary

Semantic Construction is not implemented. When introduced, it must preserve
the existing separation of identity, meaning, and history:

- A construction remains a bounded set of stable Entities and explicit
  relations recognized through an Archetype; it is not flattened into a tile
  prefab or silently converted into a Prototype.
- A Patch Anchor references one material Entity or recognized construction.
  Saves retain sparse placement changes, the taught Archetype, and the typed
  binding belonging to the Patch Anchor rather than copying surrounding
  terrain.
- A Scarred Zone combines a deterministic boundary recipe, Patch Anchor,
  bounded typed Patches, and Provenance. The Site Blueprint may supply its
  physical location but does not own its semantic rule.
- Overlapping-zone precedence and spatial binding remain unresolved by design.
  They require an acceptance slice and save migration before runtime records
  are added; current population-based Reach must not be overloaded implicitly.
- A Scar Package may contain versioned PALI, semantic identities, an ordered
  Patch and Scar history, and Provenance but never native executable code.
  Cross-Genesis import remaps by compatible Archetype or versioned Site
  Blueprint kind and role, then explicitly selects a destination materialized
  Site or Patch Anchor rather than replaying seed-derived identities.

## Memory and trust contract

PALIMPSEST host code performs no heap allocation during generation, fixed-step
simulation, PALI compilation/execution, or saving. raylib owns its internal
graphics allocations, and the optional screenshot helper allocates an image
outside simulation.

Hard capacities are checked rather than overrun:

- `World`: compile-time maximum 256 KiB
- entities: 64
- Local Override bindings: 32; values per binding: 4
- Lineage definitions: 12; one active descendant fruit per tree
- PALI source: 4095 bytes plus terminator
- properties/document: 16; statements: 16; expression nodes: 64; instance
  state properties/entity: 4
- constants: 32; instructions: 128; VM stack values: 48
- default execution budget: 256 instructions
- local Behavior candidate and execution budget: 24 instructions
- in-memory save image: 128 KiB

Overflow, bad types, missing properties, divide-by-zero, malformed bytecode, and
exhausted budget return an error. Fictional code cannot address C memory, call C,
load files, allocate, branch indefinitely, or access a host function outside the
whitelist. Runtime failure becomes a visible anomaly message, not host failure.

## Working-directory contract

Core functions receive all paths explicitly. The game obtains the application
directory from raylib and joins `assets/pali` and `assets/fonts`; it never
searches the current working directory. Saves default to a user-state directory
in the isolated platform layer or to the exact `--save` path. Screenshot
verification exports to the exact requested path.

## Determinism contract

- Simulation advances only at 60 Hz.
- Terrain and object placement use separately derived streams.
- Each autonomous creature owns an independently derived stream saved with its
  sparse state.
- Rendering consumes no simulation randomness; animation derives from tick.
- Stable entity IDs derive from root seed, prototype, and deterministic serial.

Changing one subsystem's random consumption therefore does not silently shift
unrelated streams. Exact cross-platform floating-point identity is not yet
claimed; Windows builds with the same seed and inputs are the first target.

## Save transaction

The format has magic, explicit version, payload length, and FNV-1a checksum.
Saving writes `PATH.tmp`, flushes it to durable storage, rereads and validates
the complete file, then atomically replaces the previous save where the OS
supports it. A failed compile or failed load does not mutate the live world.

The v5 payload stores the root recipe plus Embodiment/tick/Knowledge state,
including vigor; changed normalized Prototype source; post-Genesis descendant
identity and Parentage; Lineage definitions and inherited-birth counts; stable
concept IDs, typed sparse values, and value/Behavior Provenance; normalized
local Behavior source; dirty Entity state including tree counters and timers;
and concept-indexed Observation masks. Terrain, unchanged definitions,
effective Inquiry state, typed handler documents, and bytecode are
reconstructed rather than duplicated.

The loader accepts v2, v3, and v4 transactionally. A v4 save restores its local
Behavior handlers but begins with Genesis tree state, no post-Genesis
descendants or Lineage definitions, and zero vigor. A v3 save also retains its
Observation ledger and begins with no local Behavior handlers. A v2 save begins
with an empty Observation ledger and receives exact nutrition Notation to
preserve the precision its Lens already displayed. No legacy path invents
Parentage or Inquiry completion; current and completed Inquiries derive after
reconstruction.
