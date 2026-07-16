# PALI language: implemented subset

PALI is a small original language compiled by PALIMPSEST. It is not Lua and does
not embed or evaluate C.

The current 0.5 build parses PALI into a bounded typed document before compiling
bytecode. Normal play projects that document through Knowledge into a
mouse-first structured Lens: State uses typed controls and apple Behavior uses
typed, patchable Clauses once the relevant Knowledge is held. A tree's Lineage
form builds the same bounded apple Behavior document for future fruit; it does
not introduce a second scripting language. Deterministically normalized source
remains the save, diagnostic, explicit developer, and possible late root-level
representation.
The interaction contract is specified in [`INTERACTION.md`](INTERACTION.md).

The canonical pipeline is:

```text
source -> typed document -> semantic validation -> bounded bytecode
                 |                    ^
                 +-> Knowledge Lens --+ sparse Patch Draft
```

Stable property identity comes from the Lexicon concept ID, never a source line
or bytecode offset. A local Behavior handler has Provenance separate from its
property values. The normal Lens can Patch nourishment and the known apple
handler on one Entity, then can Patch one tree's future fruit after Lineage
Knowledge opens. Raw source in `--developer` mode can replace a complete
Prototype definition.

## Shape

One source buffer defines one prototype:

```pali
prototype apple
    tag = "food"
    mass = 140
    nutrition = 20
    ripe = true

    on use(actor)
        actor.hunger = max(0, actor.hunger - self.nutrition)
        message("The apple becomes less real.")
        destroy(self)
    end
end
```

Prototype properties use `name = literal`. Implemented literal types are:

- number: `20`, `3.5`, `-4`, `1e-5`
- Boolean: `true`, `false`
- text: `"food"`

Property declarations accept literals, not computed expressions. `--` starts a
line comment. Text supports `\n`, `\\`, and `\"` escapes.

## Use handler

At most one `on use(actor)` handler is allowed. Statements are:

```pali
actor.hunger = expression
actor.warmth = expression
actor.vigor = expression
self.some_state = expression
message(expression)
destroy(self)
```

Expressions support numeric `+`, `-`, `*`, `/`, unary `-`, parentheses,
`min(a, b)`, `max(a, b)`, literals, `self.property`, and `actor.property`.
Arithmetic type errors and division by zero are runtime errors.

Host access is deliberately small:

- actor reads: `hunger`, `warmth`, `vigor`, `x`, `y`
- actor writes: `hunger`, `warmth`, `vigor` (clamped to 0..100)
- self reads: sparse instance state, concept-addressed local value, then the
  current Prototype property
- self writes: one of four sparse instance-state slots
- calls: `message(text)` and `destroy(self)`

Names outside this list cannot reach the host. A Prototype program has a
256-instruction execution budget even though this grammar has no loops.

## Structured apple Behavior Patch

Completing The First Scar grants Behavior Access Depth and makes hunger
readable. After exact mass Notation is learned, the normal Lens can project an
apple's effective handler into four typed choice Clauses beneath the fixed
`on use(actor)` trigger. The fourth socket is present but veiled until The Fruit
Remembers grants Lineage Depth and readable warmth/vigor consequences:

- **Hunger**: soothe subtracts `self.nutrition` from actor hunger and clamps at
  zero; sharpen adds it and clamps at 100; leave omits the hunger assignment.
- **Aftertaste**: none emits no assignment; kindle adds `self.nutrition` to
  actor warmth; quicken adds it to actor vigor. Both additions clamp at 100.
- **Voice**: fade emits “The apple becomes less real.”; remember emits “The
  apple remembers being eaten.”; silent omits the message.
- **Fate**: cease calls `destroy(self)`; remain omits that call.

The normalized execution order is hunger, Aftertaste, voice, then fate:
`DO -> AFTER -> SAY -> THEN`. In particular, an Aftertaste is applied before a
`CEASE` call. Vigor decays by `0.05` per fixed step while increasing movement
speed by `0.5%` per remaining point.

The selected Clauses produce a handler-only PALI document. Its normalized form
uses the ordinary Prototype wrapper but contains no copied properties, for
example:

```pali
prototype apple
    on use(actor)
        actor.hunger = min(100, actor.hunger + self.nutrition)
        actor.vigor = min(100, actor.vigor + self.nutrition)
        message("The apple remembers being eaten.")
    end
end
```

Before installation, the handler is type-checked against known Clause sockets,
merged with the current apple Prototype document, compiled, and rejected if the
resolved program exceeds 24 instructions. A local handler executes with that
same 24-instruction budget. Failed validation leaves the prior effective
Behavior live.

The normalized handler source is the persisted local Behavior Patch. Its
Entity or birth-captured Lineage Provenance resolves separately from local
values: `self.nutrition`, for example, still follows sparse instance state, the
concept-addressed local value Scar, then the current Prototype property.
Reverting an Entity handler does not erase a nourishment Scar, and changing
nourishment does not copy the handler.

## Structured future-fruit Lineage Patch

After The Fruit Remembers grants Lineage Depth and Reach, opening a tree shows
**FUTURE FRUIT / THIS LINEAGE**. Its mouse-complete draft contains a bounded
nutrition base and the same four apple Behavior choices. The Lens previews the
exact next nourishment and selected Aftertaste before installation.

Nutrition receives a deterministic birth Inflection in `[-2, 2]`, derived from
the stable tree ID and next birth ordinal and clamped to `0..100`. On birth, the
child captures a sparse nutrition value when the Lineage addresses nutrition or
the Inflection differs from broader Prototype meaning, and captures Behavior
only if the Lineage addresses it. Those nodes carry the tree ID and Lineage
Reach as Provenance. All untouched properties and Clauses continue resolving
from the apple Prototype, and later Lineage edits cannot change an already
materialized child.

This is structured host semantics over the existing typed-document and
compiler pipeline, not PALI inheritance syntax. The persisted Lineage Behavior
is normalized PALI and is parsed, validated, and compiled again on load.

## Errors and limits

Lexer/compiler errors report one-based line and column. Applying source compiles
a temporary program; a failure leaves the last valid source and bytecode live.
Runtime errors identify the source line when possible and surface as an in-world
anomaly message.

Current limits are 4095 source bytes, 16 Prototype properties, 32 constants,
64 owned expression nodes, 16 statements, 128 bytecode instructions, and 48
VM stack values. Normalized formatting preserves finite double values exactly
and is itself constrained to the 4095-byte parseable source contract.

Not implemented: conditionals, loops, user functions, general-purpose PALI
inheritance syntax, Archetypes, Laws, collections, file/network access, dynamic
allocation, or general host calls. The implemented structured Lineage form is
deliberately narrower; these other forms are errors, not undocumented features.
