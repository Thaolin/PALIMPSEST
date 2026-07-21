# Roadmap

Every slice must be playable, inspectable, and small enough to complete before
the next begins. A slice may add one deep rule; it may not promise a genre.

## Slice 0 — Empty Chronicle

Create the Godot 4 .NET project and engine-independent C# solution. Prove a
fixed-tick Chronicle Clock, pause/speed controls, a seed, a World Address, and
save/load of a tiny generated surface patch.

**Accept when:** a test can replay the same seed and command stream to the same
state, and Godot can render that state without owning it.

## Slice 1 — First Horizon

Offer the diegetic `UP` Intent. It grants `Fly`, opens a small linked sky
Stratum, and lets the player reach a visible sky Landmark and return.

**Accept when:** a new player can make a deliberate opening choice, use a
concrete Verb to reach an otherwise inaccessible place, and understand why it
worked without reading documentation.

Historical implementation contract:
[Slice 1 — First Horizon](archive/contracts/SLICE-1-FIRST-HORIZON.md).

## Goal 2 — A Word Kept After Death

**Status:** Complete on 2026-07-19. Slices 2A, 2B, and 2C passed automated
proof and separate player UAT.

Deliver three sequential UAT slices:

- **2A — Study a Word:** add the Codex, one Noun, and visible Study.
- **2B — Make a Word Active:** make the eight-slot Loadout real and give one
  Expression a material effect.
- **2C — Replace the Body:** end one Incarnation and prove the Codex and changed
  Chronicle survive in its replacement.

**Accept when:** a player can prove the same Codex produces a different second
build without erasing the first Chronicle’s material state. Each child slice
requires separate UAT before the next begins.

Historical implementation contract:
[Goal 2 — A Word Kept After Death](archive/contracts/GOAL-2-A-WORD-KEPT.md).

## Slice 3 — A World With Shape

**Status:** Complete on 2026-07-19. Gate 3A passed automated proof and player
UAT as a deliberately semantic/debug alpha. Gate 3B passed automated proof and
player visual UAT with `20 px / 33 × 23` selected as the local-view baseline.
The 16 px candidate remains comparison evidence only. A procedural asset
compiler prototype may be evaluated separately; it was not a Gate 3B
dependency. Underlying semantic refinement must not be disguised as visual
polish. The Inspector's default large overview remains semantic; Visual
Grammar preview remains limited to local 64 × 64 or 32 × 32 requests.

First build one deterministic World Grammar and expose it through a
developer-only interactive World Atlas Inspector. Then map the accepted
semantic truth through a coherent Visual Grammar in the denser local player
view. Keep its compiled-pack seam independent of the authoring method so a
later procedural compiler can be compared against or replace the initial
manual adapter without changing runtime meaning.

The parallel candidate compiler is specified in the
[Chronicle Visual Engine drop-in contract](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md).
It is pure C# at authoring time with a Godot preview adapter, remains absent
from the shipped runtime, and does not block Gate 3B. Its separate E0–E4
[historical build handoff](archive/prompts/CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md)
stops before
Palimpsest integration. P-GEN's E4.5 technical proof was independently
reproduced on 2026-07-20, but it is not adopted or currently drop-in. Its
[readiness review](P-GEN-E4-5-READINESS-REVIEW.md) records the consumer,
vocabulary, motif, cosmetic-selection, specification, and visual-proof
decisions required before a separately authorized E5 gate.

**Accept when:** the inspector makes generated places visibly structured rather
than random across large bounded requests and query edges; the local player
view remains stable across movement and reload; and actors, materials, and
Landmarks read clearly without labels.

Implementation contract: [Slice 3 — A World With Shape](SLICE-3-WORLD-VISUAL-GRAMMAR.md).

## Goal 4 — Three Openings, One Chronicle

**Status:** Complete and accepted on 2026-07-20. Slice 4A completed
implementation, automated proof, player UAT, and tracker reconciliation on
2026-07-19. Slice 4B completed implementation and automated proof on
2026-07-19, then passed functional player UAT, its focused Clock/Codex
correction recheck, and tracker reconciliation on 2026-07-20. Slice 4C's exact
fixture and public seams were approved, implemented, fully verified, and
passed player UAT on 2026-07-20. Closer zoom and stronger Cairn legibility
remain deferred visual notes; deliberate movement while paused is accepted
command responsiveness. Slice 5 subsequently passed its complete retained
automated gate and player UAT on 2026-07-20.

Begin only after Goal 2 and both Slice 3 gates pass. Deliver three sequential
UAT slices that turn Combat, Explore, and Build into genuine openings onto the
same Chronicle rather than separate modes or classes.

### 4A — A Choice of Words

Replace the Bell-specific hidden-word scaffold with the smallest real authored
Word Catalogue and one generated Study Source. Its situation offers a small set
of plausible words, the player chooses what to pursue, and its rarity, danger,
and significance determine word-specific Understanding.

This formalizes the existing `UP`/`Fly` opening as the first Explore Starting
Vector without making vertical travel the whole meaning of Explore. It does not
require a large vocabulary, procedural word meanings, a general
content-authoring system, or dozens of sources.

**Accept when:** a player can explain why the situation offered those words,
choose deliberately, retain partial Understanding through save/load and death,
and eventually add the chosen word to the Codex.

### 4B — A Place Called Home

Add the Build Starting Vector and let any Incarnation establish the singular
Home at one valid site in a currently reachable Stratum. Home has persistent
identity, one material change, and a physical Return Route. Do not add residents,
production chains, jobs, Pressure, or free teleportation yet.

**Implementation state:** complete and accepted on 2026-07-20. The fixed Home
fixture, automated proof, functional player journey, compact Tick/Clock
correction, focused recheck, and tracker reconciliation all passed. That
acceptance did not itself authorize 4C; separate authorization followed. See
the archived
[Goal 4B UAT](archive/uat/GOAL-4B-UAT.md).

**Accept when:** a builder experiences Home as an authored-by-play place, while
an explorer can leave it modest and use it as an expedition anchor without
entering a management mode.

### 4C — A Consequential Fight

**Implementation state:** complete and accepted on 2026-07-20. The `AGAINST` /
`Smash` / Riven Cairn fixture, one-exchange fixed-tick rule, strict save-v4
boundary, automated proof, and player result are preserved in the archived
[Goal 4 contract](archive/contracts/GOAL-4-THREE-OPENINGS.md) and
[Goal 4C UAT sheet](archive/uat/GOAL-4C-UAT.md).

Add the Combat Starting Vector and one deterministic, pausable conflict tied to
a generated place and its history. The encounter uses the same Loadout and
Chronicle rules as exploration and leaves a material consequence. The scoped
first fixture deliberately defers any post-fight Study opportunity. Do not
build a broad bestiary, loot economy, or reflex combat layer.

**Accept when:** the player can stop, understand the danger, choose a meaningful
Expression or action, and see the result persist outside a combat screen.

**Goal 4 accepts when:** a new Chronicle offers Combat, Explore, and Build;
each opens a different First Horizon, and all three feed the same Codex, Home,
generated world, and eventual Incarnations. Each child slice requires its own
automated proof and player UAT before the next begins.

Implementation contract:
[Goal 4 — Three Openings, One Chronicle](archive/contracts/GOAL-4-THREE-OPENINGS.md).

## Slice 5 — A Word Multiplies

**Status:** Complete and accepted on 2026-07-20. Automated proof passed with
zero warnings or errors, then the player reported: “Full UAT accept.” This did
not authorize Goal 6.

**Direction after acceptance:** Slice 5 proved deterministic shared subject
resolution and strict migration, but the player's later post-UAT assessment was
that collectible `Verb[Noun]` composition was not fun enough. Its save behavior
remains an accepted predecessor baseline; ADR 0003 replaces it as the direction
for future language growth.

Settle one authored composition rule before catalogue or social-language
growth makes the current pair-specific action pattern expensive. Preserve
authored compatibility and all accepted behavior for intrinsic `Fly`, `Found`,
and `Smash` plus `Fly[Stone]`; resolve them through the smallest shared Core
seam; then add `Fly[Bell]` without a dedicated pair branch.

Reuse The Bell That Fell Up and the loose Stone. Add no new Word or generated
situation family. The existing `Stone`/`Bell` Study choice should become a
choice between two plausible material futures, and the chosen result must
survive save/load.

**Accept when:** the player predicts what `Fly[Stone]` and `Fly[Bell]` would do
from their meanings, chooses one Word to Study first for a defensible reason,
and sees its durable result after reload without fixture-specific guidance.
Adding `Fly[Bell]` must not add an `if (slot.IsSpecificPair)` equivalent.

Implementation contract:
[Slice 5 — A Word Multiplies](archive/contracts/SLICE-5-A-WORD-MULTIPLIES.md).
Accepted player proof: [Slice 5 UAT](archive/uat/SLICE-5-UAT.md).

## Modifier Grammar Pivot — A Language Worth Building

**Status:** Product direction settled on 2026-07-20. The isolated combat-cycle
prototype and its Slow-heartbeat, pause, and Engagement-Plan refinement passed
player UAT on 2026-07-21. The production successor remains unauthorized. The
[RPG Successor Rebuild Direction](RPG-SUCCESSOR-REBUILD-DIRECTION.md) now
defines what is retained and sequences the next production goals.

Replace collectible Nouns with contextual Targets from Chronicle state. A
Power Word is either a Verb that defines the base magic or a Modifier linked to
that Verb to change how it acts. The Loadout constrains breadth through Verb
slots and depth through link capacity plus shared Load.

Modifiers are reusable across different Verbs, unique within one Expression,
and order-independent. Every equipped Word attachment has fixed authored Load
independent of the Target. Targets expose constraints rather than one exact Word
recipe, and invalid Targets remain inspectable before an Invocation commits.

The passed pressure test used the smallest useful set of Verbs and Modifiers
against real Target facts and one changing threat. It proved that speed,
persistence, physical attacks, equipment, an autonomous helper, Preparation,
and Recovery can produce meaningful choices without reflex input or an MP bar.

Ordinary fighting and exploration must remain responsive. A Modifier such as
`Quickly` may move an Invocation from ritual time toward tactical time by
transferring cost into Load, material power, instability, notice, or collateral.
Long Chronicle-time commitments are reserved for genuinely large, durable, or
world-scale results rather than charged once per Link.

The successor Combat model is broader than Invocation: positioning, physical
actions, equipment, terrain, and autonomous Companions share the Chronicle
Clock with prepared, delayed, recovering, or ritual actions. The passed
prototype is evidence for the first production slice, not code to port or
permission to build a broad combat framework.

The passed combat shell was deliberately narrow: one Weapon slot, one
Armor slot, one Accessory slot, visible HP bars, tick-timed weapon attacks, and
`Burn` as the first combat Invocation. It also proved a Slow Heartbeat,
Engagement Plan, opening pause, pause-first tactical input, and Chronicle-time
Recovery that survives retreat.

**Accepted evidence:** the player predicted how the same Verb changed under
different links, deliberately omitted at least one desirable Modifier, and
could defend different Loadouts without one obvious correct answer. The test
also produced a persistent material consequence against an actual Target.
Omitting the expensive speed Modifier created legible exposure to interruption
rather than passive waiting, while invalid-Target preview explained constraints
without giving the player a Word recipe. This evidence supports a separately
approved bounded production contract; it does not itself authorize the
successor Core plan, save migration, catalogue expansion, Load-Source
construction, raids, or final power-resource economy.

Direction and open decisions:
[Modifier Grammar Course Correction](MODIFIER-GRAMMAR-DIRECTION.md).
Passed prototype evidence:
[Combat Grammar Pressure Test](COMBAT-GRAMMAR-PRESSURE-TEST.md).
Decision record:
[ADR 0003](adr/0003-use-verbs-linked-modifiers-and-world-targets.md).

## Goal 6 — A Real RPG Loop

**Status:** Direction settled; production not authorized. Retain the accepted
Slice 0–3 Chronicle, World Grammar, map, 20-pixel palette and Visual Grammar,
then replace predecessor gameplay incrementally through two separately gated
vertical slices. See the
[RPG Successor Rebuild Direction](RPG-SUCCESSOR-REBUILD-DIRECTION.md).

### 6A — A Real Fight

Move the passed successor grammar and combat cycle into production with one
dangerous authored opponent, contextual Targets, `Burn` plus two competing
Modifier builds, shared Load, one Weapon, one Armor, one Accessory, visible HP,
Preparation, Recovery, and the accepted paused/Slow Engagement behavior.

Use existing World Grammar, Chronicle state, Visual Grammar, and Godot drawing
interfaces. Replace the predecessor gameplay through one strict migration; do
not create a parallel simulation or port prototype types. Defer Companions until
an Agent can own that identity and agency.

**Accept when:** the player deliberately prepares for, reads, and survives one
dangerous generated-world encounter using both physical and language actions;
can defend at least two Loadouts; sees one material consequence persist through
save, death, and restart; and reports that the production interaction retains
the pressure test's deliberate real-time-with-pause feel.

### 6B — Power Comes Home

Add one generated expedition resource, physical return to Home, and one
buildable vulnerable Load Source. Its capacity applies at the next Attunement
and enables one previously impossible Loadout. Destruction removes future
Attunement capacity without disabling the current expedition; rebuilding
restores it.

**Accept when:** the player completes the full expedition-to-Home power loop,
uses the resulting Load to confront a new possibility, and understands the
consequence of losing and rebuilding the Source without encountering a generic
crafting or base-management mode.

Goal 6 deliberately does not add a broad bestiary, inventory, loot economy,
crafting framework, production chain, anonymous workers, or raid simulation.
Each child slice requires its own contract, automated proof, and player UAT.

## Goal 7 — Home Has People

**Status:** Not authorized. Goal 4 proved the Home–expedition rhythm and Slice 5
proved shared authored predecessor composition. Goal 6 must first establish the
successor RPG and material power loop; no Goal 7 child slice may begin without
Goal 6 acceptance and a separately approved bounded contract.

### 7A — Someone Comes Home

Add the first named Agent who forms a continuing relationship with Home. Give
them identity, needs, relationships, and a material connection to one generated
faction or piece of prior world history. They may reside, visit, or travel with
an Incarnation but are never represented as a production slot.

### 7B — A Directive, Not Unit Control

Add the smallest social-language proof using Suggest and Command. One dangerous
Directive requires Command to be admissible, but the Agent still interprets,
delays, negotiates, or refuses according to its state.

### 7C — Absence Produces History

Add one causally legible Pressure. Leave Home, allow one off-camera event to
resolve, then return to a material change and a Chronicle Record that identifies
its cause, participants, and outcome.

**Goal 7 accepts when:** the player understands who acted, why they acted, what
changed while the camera was elsewhere, and why Command never became perfect
unit control.

## Slice 8 — The First Raid

Let the accepted Pressure escalate into one Raid with observable causes,
choices, defenders, and durable damage. Do not add generic base management,
anonymous population simulation, or automatic repairs first.

**Accept when:** Home can be defended, damaged, or changed in the player’s
absence without the result feeling like an invisible timer tax, and the
Chronicle can explain the history that produced the Raid.

## Long horizon, deliberately unnumbered

The numbered slices prove foundations; they are not the ceiling of the game.
Later work should advance through player-visible arcs rather than attempt all
remaining genre systems at once.

The passed Modifier Grammar pressure test answered three evidence questions
formerly left in the long horizon:

- one generated World fact that poses a Word-relevant situation;
- enough desirable language to make the bounded Loadout exclude something;
- one legible Word-use cost or risk, not a broad resource economy.

### A richer language and material world

- More Intents, First Verbs, and First Horizons within Combat, Explore, and
  Build. Add a fourth Starting Vector only if later play proves a genuinely
  different opening rather than a renamed build.
- A large authored Word Catalogue with more Verbs and Modifiers, contextual
  Targets, linked Expressions, and consequential growth in link or Load
  capacity.
- Generated Study Sources across creatures, events, artifacts, tomes,
  treasures, materials, phenomena, and Landmarks. Each offers contextual words
  and a significance-appropriate amount of Understanding.
- High-link Expressions whose cost can move among shared Load, material power,
  Chronicle time, notice, instability, and collateral. Ordinary combat and
  exploration remain responsive; world-scale magic may require Home or other
  persistent infrastructure without making settlement specialization mandatory.
- Tools, weapons, armor, structures, and crafted artifacts grounded in
  Chronicle resources and provenance rather than a detached recipe checklist.
- Consequence-bearing Combat in which physical actions, equipment, terrain,
  autonomous Companions, and Invocations create openings for one another.
  Taming and leadership improve cooperation without becoming unit control.
- Underworld and stranger Strata with persistent passages and different
  opportunities for language, matter, and Holdings.
- Additional Worlds reached through extreme height, depth, or stranger durable
  passages. The Chronicle remains unbounded by generating new territory only
  as it becomes reachable.
- Higher-order traversal words such as `Soar` discovered through high or deep
  Study Sources rather than repetitive action grinding.

### A Chronicle society

- Richer factions, ecologies, relationships, betrayal, trade, and alliances.
- Networks of Holdings in materially different environments.
- One singular Home whose identity survives absence, damage, Incarnation death,
  and expansion into remote Worlds.
- Named Agents who interpret Command, remember outcomes, and may refuse,
  leave, or reshape the intent they receive.
- Social Verbs such as Suggest and Command whose different directive force
  matters without erasing Agent autonomy.
- Pressures whose causes remain legible as they escalate into raids,
  migrations, shortages, bargains, and Chronicle Records.

### World-scale consequence

- Chronicle-specific discovery of extraordinarily rare Palimpsests, generated
  with a hard cap of two per World.
- Bounded Decrees are one possible interaction direction, not a settled system;
  no Palimpsest outcome may become freeform semantic authoring.
- Awakening a Verb or Modifier into one authored exception to an ordinary
  limit is another candidate Palimpsest choice, not a settled promise.
- Optional World Claims such as founding a power, remaking a region, escaping,
  or becoming a catastrophe; no single required ending.
- Around the two-hundred-hour horizon, Palimpsests, Awakened Words, additional
  Worlds, and incompatible Claims may open the true long-horizon chase rather
  than ending the Chronicle.

Do not number these as slices until the preceding playable evidence tells us
which dependency should come next.
