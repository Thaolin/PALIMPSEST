# PALI language: implemented subset

PALI is a small original language compiled by PALIMPSEST. It is not Lua and does
not embed or evaluate C.

The current 0.2 build parses PALI into a bounded typed document before compiling
bytecode. Normal play projects that document through Knowledge into a
mouse-first structured Lens: State uses typed controls and Behavior uses
readable Clauses. Deterministically normalized source remains the save,
diagnostic, explicit developer, and possible late root-level representation.
The interaction contract is specified in [`INTERACTION.md`](INTERACTION.md).

The canonical pipeline is:

```text
source -> typed document -> semantic validation -> bounded bytecode
                 |                    ^
                 +-> Knowledge Lens --+ sparse Patch Draft
```

Stable property identity comes from the Lexicon concept ID, never a source line
or bytecode offset. The first Lens can Patch nourishment on one Entity; raw
source in `--developer` mode can replace a complete Prototype definition.

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
self.some_state = expression
message(expression)
destroy(self)
```

Expressions support numeric `+`, `-`, `*`, `/`, unary `-`, parentheses,
`min(a, b)`, `max(a, b)`, literals, `self.property`, and `actor.property`.
Arithmetic type errors and division by zero are runtime errors.

Host access is deliberately small:

- actor reads: `hunger`, `warmth`, `x`, `y`
- actor writes: `hunger`, `warmth` (clamped to 0..100)
- self reads: sparse instance property, then active program property
- self writes: one of four sparse instance-state slots
- calls: `message(text)` and `destroy(self)`

Names outside this list cannot reach the host. A program has a 256-instruction
execution budget even though this first grammar has no loops.

## Errors and limits

Lexer/compiler errors report one-based line and column. Applying source compiles
a temporary program; a failure leaves the last valid source and bytecode live.
Runtime errors identify the source line when possible and surface as an in-world
anomaly message.

Current limits are 4095 source bytes, 16 prototype properties, 32 constants,
64 owned expression nodes, 16 statements, 128 bytecode instructions, and 48
VM stack values. Normalized formatting preserves finite double values exactly
and is itself constrained to the 4095-byte parseable source contract.

Not implemented: conditionals, loops, user functions, inheritance syntax,
archetypes, laws, collections, file/network access, dynamic allocation, or
general host calls. Those forms are errors, not undocumented features.
