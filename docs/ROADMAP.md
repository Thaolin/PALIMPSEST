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

Implementation contract: [Slice 1 — First Horizon](SLICE-1-FIRST-HORIZON.md).

## Goal 2 — A Word Kept After Death

**Status:** Complete on 2026-07-19. Slices 2A, 2B, and 2C passed automated
proof and separate player UAT. Slice 3 remains the next gated work and has not
begun.

Deliver three sequential UAT slices:

- **2A — Study a Word:** add the Codex, one Noun, and visible Study.
- **2B — Make a Word Active:** make the eight-slot Loadout real and give one
  Expression a material effect.
- **2C — Replace the Body:** end one Incarnation and prove the Codex and changed
  Chronicle survive in its replacement.

**Accept when:** a player can prove the same Codex produces a different second
build without erasing the first Chronicle’s material state. Each child slice
requires separate UAT before the next begins.

Implementation contract: [Goal 2 — A Word Kept After Death](GOAL-2-A-WORD-KEPT.md).

## Slice 3 — A World With Shape

Add one deterministic World Grammar and a coherent Visual Grammar. Replace
independent coloured-tile noise with connected terrain, controlled symbolic
variation, and readable Landmarks across several fixture seeds.

**Accept when:** generated places look structured rather than random, remain
stable across movement and reload, and clearly communicate actors, materials,
and Landmarks without labels.

Implementation contract: [Slice 3 — A World With Shape](SLICE-3-WORLD-VISUAL-GRAMMAR.md).

## Goal 4 — Three Openings, One Chronicle

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

**Accept when:** a builder experiences Home as an authored-by-play place, while
an explorer can leave it modest and use it as an expedition anchor without
entering a management mode.

### 4C — A Consequential Fight

Add the Combat Starting Vector and one deterministic, pausable conflict tied to
a generated place and its history. The encounter uses the same Loadout and
Chronicle rules as exploration, leaves a material consequence, and may create a
valuable Study opportunity. Do not build a broad bestiary, loot economy, or
reflex combat layer.

**Accept when:** the player can stop, understand the danger, choose a meaningful
Expression or action, and see the result persist outside a combat screen.

**Goal 4 accepts when:** a new Chronicle offers Combat, Explore, and Build;
each opens a different First Horizon, and all three feed the same Codex, Home,
generated world, and eventual Incarnations. Each child slice requires its own
automated proof and player UAT before the next begins.

## Goal 5 — Home Has People

Begin only after Goal 4 proves the Home–expedition rhythm.

### 5A — Someone Comes Home

Add the first named Agent who forms a continuing relationship with Home. Give
them identity, needs, relationships, and a material connection to one generated
faction or piece of prior world history. They may reside, visit, or travel with
an Incarnation but are never represented as a production slot.

### 5B — A Directive, Not Unit Control

Add the smallest social-language proof using Suggest and Command. One dangerous
Directive requires Command to be admissible, but the Agent still interprets,
delays, negotiates, or refuses according to its state.

### 5C — Absence Produces History

Add one causally legible Pressure. Leave Home, allow one off-camera event to
resolve, then return to a material change and a Chronicle Record that identifies
its cause, participants, and outcome.

**Goal 5 accepts when:** the player understands who acted, why they acted, what
changed while the camera was elsewhere, and why Command never became perfect
unit control.

## Slice 6 — The First Raid

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

### A richer language and material world

- More Intents, First Verbs, and First Horizons within Combat, Explore, and
  Build. Add a fourth Starting Vector only if later play proves a genuinely
  different opening rather than a renamed build.
- A large authored Word Catalogue with more Verbs, Nouns, compatible
  Expressions, and eventually Loadout growth from eight slots toward ten.
- Generated Study Sources across creatures, events, artifacts, tomes,
  treasures, materials, phenomena, and Landmarks. Each offers contextual words
  and a significance-appropriate amount of Understanding.
- Tools, weapons, armor, structures, and crafted artifacts grounded in
  Chronicle resources and provenance rather than a detached recipe checklist.
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
- Optional World Claims such as founding a power, remaking a region, escaping,
  or becoming a catastrophe; no single required ending.
- Adverbs as a deliberately late grammar expansion that changes how an
  Expression acts. Adverbs are not a 1.0 promise.

Do not number these as slices until the preceding playable evidence tells us
which dependency should come next.
