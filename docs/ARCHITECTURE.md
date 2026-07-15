# Architecture

## Implemented boundaries

- `src/main.c`: raylib window/input/timing, executable-relative asset path,
  default save path selection, and the fixed-step accumulator.
- `src/ui.c`: integer-scaled world rendering, the structured Lens, and the
  developer-only fixed-capacity source editor.
- `src/lexicon.c`: stable concept identities, Facets, semantic types, bounds,
  operations, and Reach vocabulary.
- `src/world.c`: deterministic generation, collision, survival simulation,
  entity/prototype resolution, per-creature RNG, and PALI host bindings.
- `src/pali_lexer.c`: source characters to tokens with line/column locations.
- `src/pali_compiler.c`: recursive-descent parser, bounded typed document,
  deterministic formatter, semantic validation, and bytecode compiler.
- `src/pali_vm.c`: typed runtime values, stack VM, arithmetic, budget, and host
  callback boundary.
- `src/save.c`: portable little-endian save encoding, checksum validation,
  sparse materialization, and reconstruction from genesis.
- `src/platform.c`: isolated atomic replacement, durable file flush, directory,
  and default user-save behavior.

The core library has no raylib dependency. Tests compile and run the language,
world, and persistence paths without opening a window.

## Data flow

At startup, the platform supplies explicit asset and save paths. A new world
loads and compiles base `.pali` definitions, then terrain and entity genesis use
separate RNG streams derived from the displayed root seed. A load performs the
same genesis and applies stored prototype patches, semantic Entity Scars, entity
changes, and player state.

During play, raylib input becomes a `WorldInput`. The accumulator advances the
world only in 1/60-second steps. Inspecting pauses simulation. The normal Lens
projects a typed `PaliDocument` through Knowledge and records a Draft against a
stable concept ID. Inscribing compiles a resolved candidate and commits only
after validation. Developer source follows the same parse-document-compile
pipeline. Using an entity executes its Prototype bytecode while host property
resolution composes any narrower Entity value through a callback-only
whitelist.

The presentation canvas is 720x405 and reaches the default 1440x810 window at
an exact 2x scale. World geometry remains in its original 320x224 coarse
coordinate space and is transformed by 3/2 into a 480x336 viewport. HUD and
editor text draw natively in presentation coordinates. Raising UI resolution
therefore cannot alter generation, collision, fixed-step movement, or saves.

Property resolution is:

1. sparse instance state;
2. sparse Entity Patch value for that stable entity ID and concept;
3. shared prototype program.

The four-slot Entity Patch pool stores only concept-addressed typed values. It
never copies a complete Prototype program. Consequently a locally nourished
apple still inherits later unrelated Prototype color and Behavior changes.
Prototype source mutation exists only in the explicit developer profile; the
normal Lens exposes Entity Reach and does not reveal the broader target.

## Identity layers

`World` contains three peer stores rather than treating the player as the
universe:

- `UniverseState`: root seed, tick, terrain, entities, definitions, overrides;
- `KnowledgeState`: known concepts and access capability bits;
- `EmbodimentState`: stable body ID, position, hunger, and warmth.

This is the seam for future knowledge progression and embodiment transfer; no
speculative transfer system is built.

## Memory and trust contract

PALIMPSEST host code performs no heap allocation during generation, fixed-step
simulation, PALI compilation/execution, or saving. raylib owns its internal
graphics allocations, and the optional screenshot helper allocates an image
outside simulation.

Hard capacities are checked rather than overrun:

- `World`: compile-time maximum 256 KiB
- entities: 64
- Entity Patch bindings: 4; values per binding: 4
- PALI source: 4095 bytes plus terminator
- properties/document: 16; statements: 16; expression nodes: 64; instance
  state properties/entity: 4
- constants: 32; instructions: 128; VM stack values: 48
- default execution budget: 256 instructions
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

The payload stores the root recipe plus player/tick/Knowledge state, changed
normalized Prototype source, stable concept IDs and typed Entity Patch values,
and only dirty generated Entities. Terrain and unchanged definitions are
regenerated rather than duplicated.
