# Slice 5 — A Word Multiplies

## Status

**Complete and accepted on 2026-07-20.** The automated proof passed with zero
warnings or errors, then the player reported: “Full UAT accept.” This contract
turned the dated [design evaluation](../../DESIGN-EVALUATION-2026-07-20.md)
into one bounded playable gate. Its acceptance did not authorize Goal 6.

## Player-visible hypothesis

One authored Verb should create coherent possibilities with more than one Noun
without teaching the player a separate hidden rule for every pair.

The Bell Study choice becomes the proof. `Stone` and `Bell` should both imply a
useful, predictable future when fitted into `Fly`; choosing which to Study
first must have no obviously correct answer.

## Settled composition rule

Each Verb supplies a bounded operation whose intrinsic form uses an authored
default subject; a compatible Noun instead contributes an authored subject or
medium whose identity and affordances constrain that same operation against
Chronicle state.

Compatibility remains authored catalogue truth. Not every Verb/Noun pair is
valid, Nouns do not collapse into generic property tags, and World Grammar does
not invent semantics.

### First truth table

| Action form | Valid in Slice 5 | Subject or medium | Accepted result |
| --- | --- | --- | --- |
| intrinsic `Fly` | yes | current Incarnation | move between matching surface and sky coordinates |
| `Fly[Stone]` | yes | one discrete loose Stone | move that Stone between matching surface and sky coordinates |
| `Fly[Bell]` | yes, new | The Bell That Fell Up | move that Bell between matching surface and sky coordinates |
| intrinsic `Found` | yes | current valid place | establish the singular Home |
| intrinsic `Smash` | yes | resisting material at the current conflict | preserve the accepted prepare-on-pause, resolve-on-tick Cairn result |
| `Found[Stone]`, `Found[Bell]` | no | — | reject as authored-incompatible |
| `Smash[Stone]`, `Smash[Bell]` | no | — | reject as authored-incompatible |

`Fly[Bell]` is the only new compatible Expression. Slice 5 adds no new Word.

## Resolution ownership

| Question | Owner |
| --- | --- |
| Is the pair coherent? | Authored `WordCatalogue` compatibility |
| Which concrete subject can the Noun mean here? | Core-owned Chronicle subject query using durable identity and state |
| Which targets are valid? | The same Core resolution path consumed by checks and Godot |
| What transition does the Verb perform? | Core-owned Verb operation |
| What does use cost? | Core resolution; Slice 5 adds no per-use resource or broad economy |
| Why is use rejected? | Core command result from the same resolution path |
| How is it shown? | Godot presents Core definitions, targets, and results without deciding them |

Dispatch may branch once on a Verb's bounded operation and may resolve a Noun's
authored subject identity. It may not branch on a specific Verb/Noun pair.
Bell-specific code may locate and preserve the Bell; it may not implement a
separate `Fly`-plus-`Bell` effect.

## Smallest playable situation

Reuse The Bell That Fell Up, its Study Source, and the existing loose Stone.
Do not add another Landmark, Study Source family, or generated obstacle.

The source continues to offer `Stone` and `Bell`. Their catalogue meanings and
the shared `Fly` operation let the player predict two futures:

- learn `Stone` first and move the loose Stone between Strata;
- learn `Bell` first and bring the unique Bell between Strata.

The choice is which future matters now, not a permanent exclusion. Study time
remains the existing opportunity cost; source depletion and a Word-use economy
remain outside this slice.

The Bell keeps one durable identity and carries its clapper Study Source when
moved. Its old address must not retain a duplicate. Moving it changes no
underlying generated terrain and must respect existing durable-subject
exclusions.

## Accepted behavior that must not change

- intrinsic `Fly` moves only the Incarnation and takes no target;
- intrinsic `Found` establishes one Home only at an eligible current site;
- intrinsic `Smash` retains the exact paused Cairn preparation and fixed-tick
  resolution;
- `Fly[Stone]` retains adjacency, target, matching-coordinate movement, Home
  exclusion, command messages, and durable restore behavior;
- Codex, Study, Loadout, death/replacement, Home, conflict, World Grammar,
  Visual Grammar, and old save behavior remain accepted.

Generic display text should render any valid `Verb[Noun]` from catalogue names.
Production code must not retain or add pair predicates such as `IsFlyStone` or
`IsFlyBell`.

## Persistence boundary

The moved Bell is a saved durable delta. The implementation must:

1. advance the strict current save envelope once;
2. migrate literal v4 saves with the Bell at its accepted fixed sky address;
3. retain literal v3, v2, v1, and pre-envelope migration behavior;
4. keep every existing World Grammar pin unchanged;
5. restore the Bell address, attached Study Source, Loadout, Codex, and other
   Chronicle state exactly after save/load and deterministic replay.

No ADR is required unless implementation discovers a hard-to-reverse,
non-obvious trade-off beyond this contract.

## Automated proof

The smallest retained Core proof must show:

1. catalogue compatibility accepts `Fly[Stone]` and `Fly[Bell]` and rejects the
   invalid table entries;
2. all four previously accepted action forms retain exact successful and
   rejected behavior;
3. Stone and Bell targets come from one shared Noun-subject resolution path;
4. `Fly[Bell]` moves the single Bell to the matching Stratum address;
5. the Bell and its Study Source exist at the new address and nowhere else;
6. occupied, missing, distant, wrong, and incompatible targets reject
   legibly without mutation;
7. strict current save/load, literal predecessor migration, deterministic
   replay, and query-order neutrality pass;
8. retained Core, visual, Godot, Goal 2, Slice 3, and Goal 4 gates still pass.

Code review must confirm that adding `Fly[Bell]` added no
`if (slot.IsSpecificPair)` equivalent and that production sources contain no
pair-specific `Fly[Stone]` or `Fly[Bell]` predicate.

The complete retained gate passed on 2026-07-20 with zero build warnings or
errors. Its Slice 5 markers were:

```text
SLICE5 CORE ACCEPTANCE PASS expression=Fly[Bell] durable=Bell+source save=5 migration=4
SLICE5 SAVE READY bell=surface:0,-4 loadout=Fly[Bell] save=5
SLICE5 RESTART ACCEPTANCE PASS bell=surface:0,-4 source=attached death=confirmed
PASS: Slice 5 Fly[Bell] composition and restart, Goal 4C conflict, Goal 4B Home, Goal 4A Study choice, Goal 2, Gate 3A, and Gate 3B verified.
```

## Godot proof and player UAT

Godot must expose catalogue meanings, compatible fitting, valid targets,
rejection reasons, and results from Core-owned data. Do not add a
Bell-expression branch to `ChronicleApp.GuidanceText(...)`.

One fresh-Chronicle UAT journey:

1. choose `UP`, reach The Bell That Fell Up, and inspect the `Stone` and `Bell`
   offers;
2. before choosing, predict what both `Fly[Stone]` and `Fly[Bell]` would do;
3. choose one Word to Study first and explain why that future matters now;
4. complete Study, fit the resulting Expression, and use it on the matching
   visible subject;
5. save, quit, and relaunch;
6. confirm the chosen subject remains moved and every unrelated accepted state
   remains intact;
7. explain why choosing the other Word would also have been defensible.

The runnable journey and result template are in
[Slice 5 UAT](../uat/SLICE-5-UAT.md).

**Accept when:** the player predicts two plausible outcomes from the Words'
meanings, chooses one for a defensible reason, and sees a persistent result
without fixture-specific instructions.

## Forbidden in Slice 5

- a universal ontology, property framework, or general effect registry;
- pair-specific action branches or a second Godot rules path;
- new Words beyond the new `Fly`/`Bell` compatibility edge;
- a broad cost, mana, cooldown, exposure, or attunement economy;
- terrain collision, water traversal, generated obstacle families, or
  procedural Study Source families;
- Agents, factions, relationships, Directives, Pressures, raids, or off-camera
  history;
- camera/Cairn polish, P-GEN adoption, Palimpsests, Decrees, World Claims,
  Adverbs, or freeform semantic authoring.

## Definition of done

Slice 5 is complete only when:

1. the composition rule and truth table remain accurate after implementation;
2. the shared Core resolution path replaces pair-specific production logic;
3. the full retained automated gate passes;
4. the player passes the focused UAT journey;
5. the contract, Roadmap, Handoff, Codemap, Development guide, and UAT evidence
   are reconciled;
6. work stops before Goal 6 until separately authorized.
