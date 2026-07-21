# Pre-alpha Design Evaluation — 2026-07-20

## Status and authority

This is a dated critical assessment of the playable build after Goal 4C's
automated gate and before its player UAT. It records evidence, risks,
opportunities, and candidate decision gates. It is not a production contract,
an authorization to expand Goal 4C, or a replacement for the
[Vision](VISION.md), [Roadmap](ROADMAP.md), or
[Active Handoff Contract](HANDOFF.md).

Goal 4C later passed player UAT on the same date; this document intentionally
remains the pre-UAT snapshot. At the time of assessment, the Handoff forbade
new Nouns, `Smash[Noun]`, terrain collision, generated-source expansion, and a
general effect registry. That was correct slice discipline while Goal 4C
awaited UAT. The concern recorded here is that the same prohibition must not
become an indefinite substitute for designing the language system on which
later slices depend. [ADR 0003](adr/0003-use-verbs-linked-modifiers-and-world-targets.md)
records the later decision to replace collectible Nouns with linked Modifiers
and contextual world Targets; the body below remains dated evidence rather than
current authority.

## Executive verdict

Two conclusions are simultaneously true:

1. The project has an unusually disciplined deterministic, persistent
   foundation for its age.
2. The current build is a thin proof of that foundation, not yet evidence that
   the promised RPG will be deep or fun.

`ChronicleSimulation.Apply(ChronicleCommand)` is a real rule seam.
`ChronicleState` advances on deterministic fixed ticks. Godot translates input
and renders Core-owned snapshots. Strict save envelopes, literal predecessor
migrations, pinned World Grammar versions, bounded absolute-address queries,
query-order neutrality, and generation without an authored edge are working
properties rather than aspirations.

That substrate is the project's strongest achievement. The design trajectory
is much less proven. The build currently contains a handful of authored
capabilities and situations, and its central Expression is implemented as a
special case. The decisive unanswered question is:

> **What is the algebra of a Word?**

The project needs a bounded, authored, compositional answer between two rejected
extremes: player-authored semantics and a bespoke code branch for every
Expression. Until that answer exists, adding catalogue entries increases
content but does not necessarily multiply possibility.

## What the current build is

### Architecture

- `Chronicle.Core` owns deterministic rules, time, generated semantics, durable
  deltas, and persistence.
- `Chronicle.Godot` translates input and presents read-only Core results.
- `Chronicle.Visuals` and `Chronicle.VisualPack` turn semantic snapshots into
  deterministic transient render plans.
- New Chronicles pin World Grammar version `3`; versions `0`, `1`, and `2`
  remain literal compatibility boundaries.
- `WorldArea.Generate(...)` answers bounded absolute-address rectangles without
  turning request bounds into World edges.

The persistence investment is conspicuous: `ChronicleState.cs` is currently
about 1,500 lines and contains strict current-save validation plus explicit
v3, v2, v1, and pre-envelope migration shapes. It would be inaccurate to call
all of that file—or two-thirds of all Core—persistence code, but persistence
and compatibility are clearly a much larger share of the implementation than
the playable rules they preserve.

### Playable inventory

| Area | Current evidence |
| --- | --- |
| Words | Five authored definitions: Verbs `Fly`, `Found`, and `Smash`; Nouns `Stone` and `Bell` |
| Compatibility | One authored Verb-to-Noun edge: `Fly` → `Stone` |
| Actions | Intrinsic `Fly`, `Found`, and `Smash`, plus `Fly[Stone]` |
| Study | One generated source identity at The Bell That Fell Up; versions 2 and 3 offer `Stone` and `Bell` |
| Understanding | Each offered Noun requires 16 active Chronicle ticks of Study at the source |
| Strata | Surface and sky |
| Generated terrain | Structured river, ridge, grove, cloud-bank, motif, and adjacency semantics |
| Memorable subjects | The fixed-address Bell, fixed initial loose Stone, a fixed Home UAT site, and one seed-deterministically selected Cairn near the origin |
| Home | One durable Hearthstone mark and a read-only physical Return Route |
| Conflict | One nullable first-conflict state with one prepare-on-pause, resolve-on-tick exchange |
| Runtime seed | The Godot app begins new Chronicles with seed `41337` |

The current player journey is therefore compact:

1. Choose `AGAINST`, `UP`, or `HERE`.
2. Walk cardinally through unconstrained terrain.
3. Use `Fly` to cross between surface and sky.
4. Study the Bell's clapper for 16 active ticks to learn `Stone` or `Bell`.
5. If `Stone` was chosen, fit `Fly[Stone]` and move the one loose Stone between
   Strata.
6. Optionally use `Found` once to establish Home.
7. Enter the Riven Cairn, leave while paused or prepare `Smash`, then resolve
   one exchange on the next active tick.
8. Optionally end an Incarnation at the Bell and verify that Codex,
   Understanding, Home, and material changes survive.

This is a deliberate vertical slice. It proves durable state transitions very
well. It does not yet provide a broad possibility space, a repeatable conflict
loop, or many interesting decisions.

## Pillar assessment

### 1. Power Words: the load-bearing concern

#### What combines today

Compatibility is authored in `WordDefinition.CompatibleNouns`. Runtime effects
are not derived from the prose meanings or from a shared composition rule:

- `LoadoutSlot` names known forms through `IsIntrinsicFly`,
  `IsIntrinsicFound`, `IsIntrinsicSmash`, `IsFlyStone`, and a display-name
  switch.
- `ChronicleSimulation.UseSlot(...)` branches over those forms.
- `Fly[Stone]` moves one specifically represented loose Stone between matching
  surface and sky coordinates through bespoke logic.

The catalogue therefore has six nominal Verb/Noun pairs, one valid Expression,
and four usable action forms in total. Because the duplicate-Verb rule allows
only one form of each Verb in a Loadout, the player cannot equip intrinsic
`Fly` and `Fly[Stone]` simultaneously. Even so, three known Verbs do not put
meaningful pressure on eight slots. The Loadout is structurally bounded but
not yet consequential.

#### Why this matters

The current compatibility list is a total guardrail. It prevents incoherent
combinations, but it also prevents unanticipated coherent ones. `Fly[Bell]`
is rejected before play even though making the Bell that fell upward fly again
is exactly the kind of discovery the pitch teaches the player to imagine.

This does not mean every Cartesian pair must be legal. A large authored
catalogue can and should reject combinations that do not express anything
coherent. The issue is that legal combinations need a reusable semantic reason
for their behavior. If every new Expression requires another identity check
and transition branch, the catalogue grows as a curated trick list rather than
as a language.

The current rules also attach no recurring price to Word use. Study costs time
at a source, but `Rare`, `Lethal`, and `Landmark` are currently single-value
enums feeding one `4 + 4 + 4 + 4` yield formula; their danger and rarity do not
yet create a choice. There is no resource, recovery period, exposure,
attunement, or other risk economy around repeated Word use.

#### The design question

A candidate direction is a small effect-resolution layer in which:

- a Verb owns a bounded operation family or effect contract;
- a Noun contributes authored subject meaning and affordances;
- world subjects expose the material facts needed to test that contract;
- one resolver produces a valid target set, transition, cost, and legible
  rejection reason from the Expression and a Chronicle snapshot.

This is a direction, not a settled implementation design. In particular,
reducing Nouns to property tags or target filters would violate the glossary's
intent just as surely as special-casing every pair would. The model must retain
authored meaning and identity while sharing enough resolution machinery for a
new Word to create more than one new branch.

The design should be able to explain, through the same small set of rules, what
`Fly[Stone]`, `Fly[Person]`, `Smash[Stone]`, and `Found[Fire]` would do—or why
each is invalid. “Whatever this pair's branch says” is not an adequate answer.

#### Recommended decision gate

Do not implement this during Goal 4C. After its UAT boundary, settle the
smallest semantic model before either:

- materially expanding the Word Catalogue; or
- implementing Goal 5B's `Suggest`, `Command`, and Directive admissibility,
  which will need a clear relationship between a Verb, its subject, world
  state, and an Agent's separate response.

The proof should first refactor the four existing action forms without changing
accepted player behavior, then add one new Expression whose behavior follows
from the shared rule and is coherent without a dedicated pair check.

### 2. Procedural generation

#### What is generated

The World Grammar generates structured terrain and visual-semantic context:
ground types, features, motifs, adjacency, ridge and river structure, groves,
and sky cloud banks. The Cairn is also selected deterministically from eligible
terrain near the origin. The Bell and initial loose Stone use fixed addresses,
the current Home UAT uses one fixed eligible site even though `Found` may act
at other valid places, and the current Study Source is tied to the Bell's
durable identity.

There are no generated Agents, factions, histories, creatures, items, or
families of Study situations yet. The build generates a board with coherent
shape; it does not yet generate the situations that make a language of power
necessary.

#### What works

The generated board is structured rather than noisy. The developer Atlas
Inspector and the shared Visual Grammar make geography legible. More
importantly, durable-subject overlays combine regenerated terrain with the
loose Stone, Hearthstone, and Riven/Shattered Cairn. Cross-subject exclusions
prevent those identities from occupying impossible combinations. This is the
“world remembers matter” pillar functioning in code.

Pinned Grammar plus saved deltas is also the right long-term shape. Untouched
territory can remain implicit while player-caused changes persist and old
Chronicles keep their original generated truth.

#### What is missing

Generation rarely constrains or offers:

- ordinary movement ignores water, ridges, and other terrain;
- only the Stratum boundary asks for a traversal capability;
- World Grammar produces one Study Source pattern;
- generated geography does not regularly pose Word-relevant obstacles,
  opportunities, or trade-offs;
- Codex growth does not alter what kinds of situations the player can expose.

The highest-leverage direction is to make geography pose problems and generate
opportunities through the same deterministic grammar. Terrain constraints and
seed-driven Study Source placement are both plausible proofs, but they should
not be bundled automatically into one broad system. A future slice should pick
the smallest situation in which one generated fact creates a legible
“I could do that if I had the Word” decision.

### 3. Player agency and meaningful choice

The current decisions are:

1. **Opening:** real and legible, but it changes the First Verb rather than the
   generated World. That is correctly nonbinding and therefore intentionally
   low-stakes.
2. **Study `Stone` or `Bell`:** fictionally legible, mechanically dominated.
   `Stone` enables the only Expression; `Bell` currently enables no active
   combination.
3. **The Cairn:** leave while paused or prepare `Smash` and resume. It can be
   tense once, but guidance supplies the exact safe answer and the conflict
   cannot recur.
4. **Loadout:** the eight-slot bound does not bind against three known Verbs.
5. **Death:** persistence works, but three Verbs do not support meaningfully
   different reinterpretations of the next body.

The important strength is durability. Home, the moved Stone, the shattered
Cairn, Codex membership, and Understanding survive where the design says they
should. The pause–prepare–resolve structure also honors deliberation over
reflex.

The next discovery proof should contain one choice with no obviously correct
answer: two Words that are both useful for different visible futures. The
player should be able to articulate a reason for either choice before learning
its outcome. Eventually the Loadout bound must also exclude something desirable
in practice, not merely in its type definition.

### 4. Fun and the interesting-decisions test

The Vision's connected loops currently map to play as follows:

| Loop | Present proof | Missing depth |
| --- | --- | --- |
| Discovery | One source, two contextual offers | Repeated varied situations and non-dominant choices |
| Build | One persistent material mark and one moved subject | Interacting materials, opportunity cost, and changing capabilities |
| Death and rebirth | Chronicle and Codex survive | Bodies and Loadouts different enough to force reinterpretation |
| Settlement | Home identity and Return Route | Agents, needs, activity, vulnerability, and history |
| World | Deterministic unbounded geography substrate | Causal history, Pressures, Claims, and Palimpsests |

The present build has one tension spike and one timed Study commitment. Its
interesting-decision density is low. That is acceptable evidence for a narrow
slice, but not evidence that more rigor around the same fixtures will create
fun on its own. The player app also begins every new Chronicle with the same
seed; arbitrary-seed generation can be inspected and tested, but it does not
yet produce player-facing replay variation.

At least one future slice should include an acceptance criterion stated as a
player decision rather than only a state transition:

> The player faces a choice with no obviously correct answer and can explain
> why either option could serve the future they currently see.

### 5. Depth, legibility, and mastery

Legibility is a real strength. Contextual Study rationales, precise rejection
messages, visible target queries, pause behavior, and UAT questions teach the
player why a result occurred.

The scaling hazard is that much of the guidance is fixture-specific.
`ChronicleApp.GuidanceText(...)` switches over the Bell, Cairn, Home, loose
Stone, known slots, and exact addresses. That is suitable for a vertical slice
but not for dozens of generated situations. Future guidance should be derived
from Core-owned situation snapshots, valid-target queries, effect previews,
and rejection reasons rather than accumulating a second handwritten rulebook
in Godot.

The project currently invests more complexity in preserving small accepted
behaviors than in creating deep interactions among them. That is a defensible
pre-alpha strategy, but it is not self-correcting. A design gate must eventually
reward depth and player judgment, not only determinism and migration coverage.

## Emergence and coupling verdict

The pillars currently coexist more than they interlock:

- Words act on a few World subjects, but World Grammar rarely creates
  Word-relevant situations.
- Generation shapes terrain, but terrain barely changes decisions.
- Study grows the Codex, but the Codex does not yet expose qualitatively
  different generated futures.
- Persistence remembers results, but there are few causal systems whose results
  can combine.

The joints are nevertheless well chosen:

- `OverlayDurableSubject(...)` layers history over regenerated terrain.
- Cross-subject invariants keep material changes coherent.
- Pinned Grammar plus saved deltas lets the generator evolve without rewriting
  old Chronicles.
- `ValidTargetsForSlot(...)` and command-result messages provide a legible
  targeting and rejection seam.
- The first conflict proves that prepared intent can wait for a deterministic
  tick while presentation and autonomous behavior remain paused.

Those seams can support an emergent chain such as “a Verb opens a route; the
route reaches a material; the material changes a Pressure.” The current build
contains the persistence substrate for that chain, not the causal substrate.

## Top five design risks

### 1. Words remain a lookup table wearing a grammar's clothes

If every legal Expression becomes another pair-specific branch, the central
promise of combinatorial power is structurally weakened. Settle a bounded
authored composition rule before catalogue or social-Verb expansion makes the
current pattern expensive to replace.

### 2. The World generates boards rather than situations

Coherent terrain is necessary but does not by itself create play. Let generated
facts constrain movement, expose sources, place subjects, or alter danger so
the language repeatedly answers problems produced by the Chronicle.

### 3. No cost or risk economy gives repeated power little weight

Words are free to use, the Loadout does not bind, death preserves almost
everything, and Study danger is descriptive. Introduce one legible trade-off
before attempting a broad economy.

### 4. Current choices have dominant answers

`Stone` is mechanically stronger than `Bell`; the Cairn guidance supplies the
safe solution; everything known fits comfortably in the Loadout. Prove one
choice in which the player can reasonably prefer either future.

### 5. Persistence rigor outpaces game depth

Strict saves are an asset, but they can consume unlimited effort while
preserving a shallow ruleset. Add player-judgment acceptance gates alongside
state-transition proof before the project assumes that more systems will
automatically become a game.

## Five latent opportunities

### 1. Prepare on pause, resolve on tick

`FirstConflictState` is a strong seed for a deterministic danger model:
understand, prepare intent, then accept material resolution on Chronicle time.
It is more distinctive than a conventional detached combat loop.

### 2. Durable-subject overlays and exclusions

The loose Stone, Hearthstone, and Cairn demonstrate a natural home for scars,
crafted objects, opened passages, ruins, and raid damage that must coexist with
regenerated territory.

### 3. Interpretive Study

Plausible offers with contextual rationales are a genuine differentiator.
Rarity, danger, significance, and word-specific Understanding already provide
the vocabulary for an economy once those qualities vary and have material
consequences.

### 4. Pinned World Grammar and literal migration

The project can evolve generation without silently rewriting untouched
territory in existing saves. That capability is rare and valuable for a
long-lived procedural game.

### 5. Valid-target and rejection seams

Core-owned target queries and precise command results can explain why an
Expression works here, fails there, or becomes dangerous. They are the right
surface for mastering a deep language without consulting an external rulebook.

## Candidate follow-up decisions

These are review recommendations, not current authorization:

1. Finish Goal 4C player UAT and reconcile its trackers without adding any of
   this review's proposed systems.
2. Before Goal 5B or material catalogue expansion, write a narrow language
   semantics decision with:
   - one sentence describing how a Verb and Noun compose;
   - a small truth table covering valid and invalid combinations;
   - target, effect, cost, and rejection responsibilities;
   - a retrofit plan for the four accepted action forms; and
   - one new Expression that proves reuse rather than a new pair branch.
3. Keep authored compatibility as a coherence boundary, but require every
   compatible pair to resolve through a small shared semantic contract.
4. Prove generated situations separately: one deterministic World fact should
   pose one problem for which different Words imply different futures.
5. Add at least one future UAT question about a non-dominant player decision,
   not only whether the expected state transition occurred.

## The honest question

The code proves that this team can build determinism, persistence, migration,
visual coherence, and disciplined vertical slices. It has not yet proved the
rule that makes this particular game worth building:

> When a new Word enters the catalogue, what does it combine with, and why does
> the combination do what it does?

The answer must not revive freeform semantic authoring. It also cannot remain
“whatever `UseSlot(...)` special-cases.” Between those is the intended game:
bounded, authored, legible composition that lets a new Word create coherent
possibilities the player can discover and the developer does not have to
implement one pair at a time.
