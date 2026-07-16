# Knowledge-gated Lens and Patch interaction

## Status

Implemented through the 0.5 vertical slice. The normal inspector is the
structured, Entity-derived Lens; the raw-source inspector is available only in
explicit developer mode. The player can now turn one Veiled concept into
qualitative and then exact meaning through persistent Observation. The First
Scar opens Behavior and readable hunger; exact mass Notation then makes one
apple's Behavior mouse-patchable through typed Clauses. A materialized
descendant opens The Fruit Remembers; its Knowledge grant reveals Parentage,
vigor, the previously veiled Aftertaste socket, and Lineage Reach. A tree then
opens as **FUTURE FRUIT / THIS LINEAGE** with an exact next-birth preview.
Prototype Reach remains hidden from ordinary Knowledge.

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
9. **Inquiries ask; state answers.** Current and completed Inquiries are derived
   from Universe and Knowledge state. They do not carry parallel completion
   flags or become a tutorial-objective subsystem.
10. **Lineage is captured at birth.** A tree's future-fruit definition is
    applied to a child only while that child materializes. The child keeps
    sparse addressed nodes and immutable Parentage/Provenance; later tree edits
    do not rewrite it.

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

After The First Scar, an apple Lens first shows readable Behavior. Once exact
mass Notation is known, that section becomes approximately:

```text
APPLE                                      THIS APPLE

SENSORY
  color          [red swatch]                    readable

MATERIAL
  { ? }                                           attend

VITAL
  nourishment    [ - ] [■■■■□□] [ + ]           patchable
  ripeness       [ripe]                          readable

BEHAVIOR
  WHEN [this entity is used by an actor]         fixed
  DO   [SOOTHE HUNGER]                           patchable
  AFTER [{ a consequence still veiled }]         veiled
  SAY  [BECOME LESS REAL]                        patchable
  THEN [CEASE]                                   patchable
       CLAUSE COST [current / 24]

                    [REVERT]  [INSCRIBE]
```

At Genesis the Lens stops at State depth. Completing The First Scar grants
Behavior Access Depth and makes hunger Readable, with an explicit Knowledge
notice. Behavior remains readable until exact mass Notation turns the known
apple grammar into the Clause controls above. Aftertaste remains veiled and
unpatchable at this depth. The Fruit Remembers later grants Lineage Depth and
makes the warmth/vigor consequences readable, turning `AFTER` into the fourth
choice control.

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

Representation precision is also learned. Mass first appears as a qualitative
meter, then as an exact number after the player learns its Notation. Color
begins as a swatch and may acquire a name or encoded value later. Nutrition is
the initial exception: its exact Notation is known at Genesis because its
bounded numeric control is the first Patch grammar.

## Observation, Revelation, and Notation

An **Observation** is an explicit click on an attendable concept row for one
active Entity. It retains the Entity's Prototype kind for that concept. Kind
means `PrototypeId` in this slice; Archetype membership is not introduced until
0.7.

- The same Prototype kind is idempotent, even when a different Entity is used.
- Only the explicitly attended, currently perceivable, type-valid concept can
  progress. Unperceived vocabulary is never scanned or leaked.
- For mass, one kind leaves the concept Veiled. A second distinct kind causes a
  Revelation and makes mass Readable as a logarithmic qualitative meter. A
  third distinct kind grants exact numeric Notation.
- Revelation does not grant Patchability. Exact mass remains Readable.
- Observation changes `KnowledgeState` only. It cannot advance time, consume
  randomness, alter an Entity, move an Embodiment, or create a Scar.
- The ledger is monotonic. Destroying an observed Entity does not erase what
  was learned, while new Observations still require an active Entity that
  currently resolves the concept.

## Inquiries

An Inquiry is a state-derived, Knowledge-guided question, not a stored quest or
tutorial objective. Its progress is recomputed from facts already belonging to
the Universe or Knowledge:

- **The First Scar** reads whether one apple carries a local nourishment Scar
  and whether invoking that apple has caused it to cease. Completion grants
  Behavior Access Depth and readable hunger exactly once, presents visible
  feedback, and yields to The Weight of Things.
- **The Weight of Things** reads the mass Observation ledger and exact Notation
  bit. Its phases therefore survive save/load without a completion flag.
- **The Sentence Inside** becomes current after exact mass Notation and reads
  whether one apple carries a local Behavior Scar.
- **The Fruit Remembers** reads whether any tree has produced a descendant,
  whether one tree carries a future-fruit Scar, and whether a later birth has
  captured it. The first witnessed birth visibly grants Lineage Depth and
  Reach plus readable Parentage and vigor once; the remaining steps ask the
  player to Scar that Lineage and receive its inherited proof.

The first incomplete derived Inquiry is current. The side panel can switch
between its detail and a compact index containing completed pages plus the
current one; later unknown Inquiries are not advertised. Saving persists only
the underlying Scars, Entity and tree state, Parentage, Lineage definitions,
Observation ledger, Notation, and Knowledge, so the same index is reconstructed
on load.

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
padlocked future options. The Fruit Remembers later grants Lineage Reach; an
opened tree then says **FUTURE FRUIT / THIS LINEAGE** and explicitly limits the
target to that tree's future descendants.

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
3. Right-clicking a nearby Entity invokes that explicit target's effective
   Behavior. `F` invokes the nearest Entity; an out-of-reach right-click is
   rejected rather than silently targeting something else.
4. The Inquiry panel's **INDEX** control collapses current detail into the
   completed/current index; **OPEN** restores the detail page.
5. The Universe remains visible behind or beside the Lens so the target retains
   physical context. Simulation pauses while a Patch Draft is open.
6. Clicking an attendable Veiled or imprecise value records one Observation.
7. Clicking a readable value explains it and, when known, shows Provenance.
8. Clicking a patchable value or Clause changes a draft through its typed
   control.
9. The Lens continuously summarizes the target and Reach without mutating the
   Universe.
10. **INSCRIBE** validates and commits the relevant State or Behavior Patch.
    **RESET STATE** and Behavior **REVERT** restore their respective drafts;
    **CLOSE** discards uncommitted changes. Keyboard shortcuts remain
    accelerators, not required knowledge.
11. With Lineage Knowledge, opening a tree replaces the ordinary Entity target
    with a mouse-complete future-fruit form. Its nourishment stepper and four
    Clause rows continuously recompute the exact next nourishment/Aftertaste
    preview; **INSCRIBE** commits only that tree's Lineage Draft.

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

Behavior is presented as a sentence-like structured document. The current
apple grammar keeps the trigger protected and exposes four typed choice rows,
with `AFTER` veiled until Lineage Knowledge makes its sockets legible:

```text
WHEN  [this entity is used by an actor]                 fixed
DO    [SOOTHE HUNGER | SHARPEN HUNGER | LEAVE HUNGER]
AFTER [NONE | KINDLE WARMTH | QUICKEN VIGOR]
SAY   [BECOME LESS REAL | REMEMBER BEING EATEN | SAY NOTHING]
THEN  [CEASE | REMAIN]
```

Soothe subtracts this apple's nourishment from actor hunger, sharpen adds it,
and leave omits the hunger effect. None leaves no Aftertaste; kindle adds
nourishment to warmth; quicken adds nourishment to vigor. Vigor decays while
temporarily increasing movement speed. Fade and remember emit their known
messages; silent omits the voice effect. Cease destroys the fictional Entity;
remain omits that effect. Execution and normalized source order are `DO`,
`AFTER`, `SAY`, `THEN`, so Aftertaste is applied before a possible `CEASE`.
The fixed trigger and selected effects compile to ordinary PALI rather than a
second execution system.

Each Clause declares its input and output types, required concepts, allowed
operators, structural role, and execution cost. The Lens shows current Clause
cost against a preinstall budget of 24 instructions. A candidate is normalized,
validated against Access Depth, the selected Reach, known sockets, types,
protected structure, and that budget, then compiled without touching the live
Universe.
The same 24-instruction bound applies when a local handler executes. An
over-budget or wrong-type candidate leaves the prior Behavior unchanged.

A protected trigger cannot be accidentally deleted. Optional effects may be
omitted only through the known choices above. “Cease” compiles to a safe
fictional host operation; it never deallocates arbitrary C storage.

Behavior Patchability is itself derived from Knowledge. The First Scar grants
Behavior Access Depth and readable hunger. Exact mass Notation then makes the
hunger, voice, and fate rows Patchable at Entity Reach and visibly announces
that Clauses have opened. `AFTER` remains a veiled fourth row until Lineage
Depth reveals its warmth/vigor sockets.

The committed document is a normalized handler-only source fragment attached
to the stable Entity. Behavior Provenance and value Provenance resolve
independently even though both fit in one sparse Local Override binding:

1. A local handler, when present, replaces only that Entity's `on use(actor)`
   Behavior; otherwise the Prototype handler applies.
2. A `self` property read inside either handler still resolves instance state,
   the matching local value Scar, then the current Prototype value.
3. Reverting a local handler preserves local nourishment or other value nodes;
   changing a value does not copy or freeze Behavior.
4. Save v5 persists the normalized handler source and its Provenance, then
   reconstructs the typed document and bytecode on load rather than serializing
   executable code.

## Future-fruit Lineage interaction

Each tree owns a stable progenitor identity, a birth ordinal, and at most one
active descendant apple. After its current fruit ceases, a bounded deterministic
timer permits another birth. The child's stable ID, placement, and nourishment
Inflection derive from the tree and ordinal rather than unrelated randomness.

Once The Fruit Remembers grants Lineage Knowledge, opening a tree presents:

```text
TREE                                  FUTURE FRUIT
                                      THIS LINEAGE / birth N

INHERITANCE                          NEXT 19 / +19 VIGOR
  BASE [ - ] [NOURISH 20] [ + ]
  DO    [SOOTHE HUNGER]
  AFTER [QUICKEN VIGOR]
  SAY   [BECOME LESS REAL]
  THEN  [CEASE]
       CLAUSE COST [current / 24]

                         [REVERT] [INSCRIBE]
```

`NEXT` is exact: the Lens applies the same deterministic `[-2, 2]` nutrition
Inflection and `0..100` clamp used by the next materialization, and names the
selected plain/kindle/quicken Aftertaste. The draft is fully mouse-operable.
Inscribing compares it with broader apple meaning and stores only changed
nutrition and Behavior nodes under that tree's Lineage.

At birth, the child captures those addressed nodes with the tree ID and
Lineage Provenance. Any nutrition differing from broader meaning, including by
Inflection, is likewise captured. Untouched meaning continues to resolve from
the apple Prototype.
Current fruit, older siblings, and unrelated trees do not change; editing the
tree again affects only later births.

## Future Semantic Construction interaction

Semantic Construction must remain a complete mouse-first play loop rather than
turning ordinary building into raw-code authoring:

1. Place known material Entities and designate a bounded Arrangement.
2. Offer Arrangements or known things as Exemplars and, when learned,
   contrasting Counterexamples.
3. Let the Lens project their shared known relations as candidate Archetype
   conditions without exposing unperceived vocabulary.
4. Name the new Archetype and preview which present constructions qualify.
5. Bind only known, type-compatible meaning or Behavior to one Patch Anchor.
6. Preview the Patch Anchor, declared boundary, affected Entities, cost, and
   known conflicts without mutating the Universe.
7. Inscribe transactionally; rejected definitions leave placement, Knowledge,
   and live behavior unchanged.

The first proof is a ring of stones around a fire taught as a **Hearth**. The
Arrangement is offered as an Exemplar; the player teaches a Hearth Archetype,
and the recognized result becomes a Semantic Construction. The Archetype is not
a Prototype because it describes what qualifies independently of how the
component Entities were generated. A later proof may give one Hearth an
`on use(actor)` Behavior that transfers warmth to its invoker. Occupant fields,
broad Law, and other spatially bounded effects wait until regional binding and
conflict precedence have an explicit model.

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
4. At the 0.2 slice, the existing `on use` Behavior is rendered as readable
   Clauses but remains non-patchable in the normal Knowledge profile. The 0.4
   progression now withholds that depth until The First Scar is complete.
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

## 0.3 acceptance slice

The first Knowledge-progression milestone is complete when:

1. Lens State rows are derived from the selected Entity and bounded Lexicon,
   grouped into only the Facets that survive Knowledge projection.
2. Normal Genesis visibly proves Unperceived, Veiled, Readable, and Patchable
   Concept Access without advertising the Unperceived concept.
3. A mouse click records an Observation for an attendable mass row; another
   Entity of the same Prototype cannot advance it.
4. Two distinct Prototype kinds reveal mass as a numberless logarithmic meter,
   including a defined zero-mass presentation.
5. A third kind grants exact numeric Notation as a separate event and does not
   grant mass Patch access.
6. Observation leaves Universe, Embodiment, tick, randomness, message, and
   Scars untouched in the core model.
7. The next Inquiry is derived from Knowledge and tells the player what can be
   attended without becoming a tutorial modal.
8. Save v3 restores partial and complete Observation progress, rejects invalid
   masks, and migrates a genuine v2 payload without inventing history.

## 0.4 acceptance slice

The first Behavior-grammar milestone is complete when:

1. The First Scar is derived from a local nourishment Scar plus invoking the
   changed apple; it stores no completion flag.
2. Completing it grants Behavior Access Depth and readable hunger exactly once,
   presents visible feedback, and makes The Weight of Things current.
3. Current and completed Inquiries appear in a collapsible index, while unknown
   later questions remain absent.
4. Right-click invokes a nearby pointed Entity and `F` invokes the nearest
   Entity's effective Behavior.
5. Exact mass Notation makes the apple's fixed trigger plus hunger, voice, and
   fate choices mouse-patchable at Entity Reach; a fourth `AFTER` row remains
   veiled until Lineage Depth.
6. Known choices can soothe, sharpen, or leave hunger; fade, remember, or
   silence the apple's voice; make it cease or remain; and, once revealed,
   leave no Aftertaste, kindle warmth, or quicken decaying movement vigor.
7. Wrong-type and over-24-instruction candidates reject transactionally before
   installation, preserving the prior live handler.
8. One apple can carry a local Behavior Scar without changing another apple.
   Its handler Provenance resolves independently from any local value nodes.
9. Save v4 retains normalized local handler source, reconstructs its typed
   document and bytecode, and accepts genuine v2 and v3 payloads.

## 0.5 acceptance slice

The first Lineage milestone is complete when:

1. Each tree bears at most one current apple through a bounded deterministic
   delay; consuming that fruit permits a later birth rather than unbounded
   population growth.
2. A descendant apple has stable Parentage and an identity derived from its
   progenitor and birth ordinal, independent of unrelated randomness.
3. The state-derived Inquiry **The Fruit Remembers** recognizes a materialized
   descendant, visibly grants Lineage Depth and Reach once, and then asks for a
   future-fruit Scar and its inherited proof.
4. Opening a tree with Lineage Knowledge shows **FUTURE FRUIT / THIS LINEAGE**
   and presents mouse-complete nourishment and four Behavior rows. Its exact
   preview combines the next deterministic nutrition Inflection with the
   selected plain, kindle, or quicken Aftertaste.
5. Inscribing that form changes one tree's future-fruit definition. Its current
   apple, existing siblings, and every unrelated tree remain unchanged.
6. The next apple born to that tree captures only addressed State and Behavior
   nodes with Lineage Provenance; untouched meaning still resolves from the
   apple Prototype.
7. Editing the tree again cannot retroactively alter a materialized child.
8. Save v5 restores post-Genesis descendants, Parentage, birth order, tree
   timers, Lineage definitions, inherited nodes, and partial Inquiry progress;
   genuine v2-v4 saves migrate without invented descendants.
9. Reproduction, Lineage records, Patch bindings, compilation, and save images
   remain within explicit fixed capacities and fail transactionally.

## Deferred

- Discovery rules for concepts beyond the first mass Revelation.
- Behavior grammar beyond the bounded apple choices.
- Archetype and Universe Reach.
- Inventory, harvesting, cross-tree breeding, and a general ecology.
- Causal cost, resistance, gods protecting definitions, and broad-impact
  preview simulation.
- Controller interaction.
- Player-visible raw source and root-level free composition.
