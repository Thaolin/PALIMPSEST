# Untitled Chronicle RPG

A procedural RPG in which a persistent generated cosmos outlives individual
playable bodies. The player discovers and combines durable capabilities while
one Chronicle expands from a strange first encounter into world-scale power.

## Chronicle and World

**Chronicle**:
A procedurally generated cosmos and its continuing history, including every
World the player can reach. A Chronicle persists after an Incarnation dies.
_Avoid_: Run, round, disposable save

**World**:
A coherent generated realm within a Chronicle, with its own linked Strata,
geography, factions, ecologies, resources, and history. A Chronicle may expand
into further Worlds through durable passages or extreme traversal.
_Avoid_: Chronicle, level, disposable map

**Chronicle Clock**:
The fixed-tick continuous time of a Chronicle. The player may pause or change
its speed for deliberation, but survival never depends on reflex input.
_Avoid_: Hard turn sequence, twitch combat clock

**Stratum**:
A two-dimensional spatial layer of a World, such as sky, surface, underworld,
or a stranger place. Persistent passages and Verbs connect Strata and may
eventually open routes to other Worlds.
_Avoid_: Flat world map, fully simulated voxel layer

**World Address**:
A stable World, Stratum, and coordinate used to locate a place, Agent, Holding,
or passage whether or not that area is presently active. A Chronicle retains
its addresses and changes as it expands.
_Avoid_: Temporary loaded area, disposable chunk

**World Grammar**:
The composable authored rules that generate a Chronicle's Worlds, Strata,
resources, passages, Agents, Pressures, and Landmarks. They create unbounded
structured territory from seed and persistent history without hand-built
levels or an authored outer edge.
_Avoid_: Random-content machine, fixed campaign map, hand-authored level

**Visual Grammar**:
The coherent presentation rules that make generated Chronicle state readable
and distinctive through palette, symbols, tile composition, adjacency, and
controlled variation. World Grammar decides what exists; Visual Grammar decides
how it appears without changing simulation meaning.
_Avoid_: Random cosmetic noise, gameplay rules hidden in the renderer, one-off
art direction per location

**Landmark**:
A persistent, memorable place created by the World Grammar, such as a ruin,
faction seat, impossible organism, deep passage, or potential World Claim.
_Avoid_: Generic point of interest, quest marker

**Pressure**:
A durable conflict of needs, territory, resources, or belief among the
Chronicle's factions and ecologies. Pressures create opportunities and
consequences, including raids, trade, alliances, and dangerous Landmarks.
_Avoid_: Scripted quest chain, random encounter table

**Faction**:
A persistent association of Agents with a shared identity, interests,
relationships, and material presence. A faction may change, fracture, ally, or
be destroyed as Chronicle history accumulates.
_Avoid_: Team colour, reputation meter, static quest giver

**Incarnation**:
One playable body in a Chronicle. It may die and be replaced without erasing
the Chronicle or the player's learned language.
_Avoid_: Run, permanent character

**Holding**:
A persistent player-established place in a Chronicle, including its structures,
residents, and material history. One Holding may hold the singular role of
Home; others remain outposts, seats, refuges, or stranger establishments.
_Avoid_: Temporary camp, instance

**Home**:
The singular Holding that anchors the player materially and emotionally within
a Chronicle. It may be founded in any World or Stratum and remains Home through
absence, Incarnation death, damage, or ruin.
_Avoid_: Mandatory management mode, automatic fast-travel hub, disposable base

**Agent**:
An autonomous person, mutant, creature, construct, or stranger with continuing
identity, needs, relationships, and agency in a Chronicle. Agent is a systemic
category, not a species or a promise of obedience.
_Avoid_: Worker token, population count, helper species

**Return Route**:
A known, physically traversable way from an Incarnation to one of the player's
Holdings. Death never erases knowledge of a Return Route, though travel along
it may remain dangerous until later Verbs change it.
_Avoid_: Free respawn teleport, lost location

**Raid**:
A hostile incursion against a Holding that can alter or destroy its material
state while its owner is elsewhere in the Chronicle.
_Avoid_: Cosmetic base-attack event, automatic repair tax

**Chronicle Record**:
A durable, inspectable account of a consequential event that happened while an
Incarnation was absent. It identifies the causes, participants, and material
outcome rather than reducing history to a notification.
_Avoid_: Combat log, offline summary

**World Claim**:
An optional, discovered world-scale conclusion a player may pursue in a
Chronicle, such as founding a power, remaking a region, escaping, or becoming a
catastrophe. A Palimpsest may make a Claim possible, but a Chronicle can contain
several Claims and no required ending.
_Avoid_: Campaign completion, single victory condition

**Palimpsest**:
A persistent, world-scale artifact associated with possible permanent changes
to a Chronicle's rules. Palimpsests are extraordinarily rare: a World may
contain none, one, or two, but never more.
_Avoid_: Endgame menu, final boss, arbitrary world editor

**Decree**:
A possible bounded, deterministic rule change made through a Palimpsest, with
legible scope, cost, and durable consequences. It is one current direction for
Palimpsest interaction, not the artifact's fully settled design.
_Avoid_: Prompt, cheat code, freeform semantic program

## Player Language

**Word Catalogue**:
The large authored set of Verbs, Nouns, meanings, and compatibilities from which
Chronicles draw. World Grammar generates where and how catalogue words can be
discovered; it does not invent their semantics.
_Avoid_: Skill tree, generated definition, per-Chronicle rules vocabulary

**Verb**:
A discoverable capability from the Word Catalogue that remains available to
later Incarnations once learned. A Verb states what can be done and can be
fitted with compatible Nouns.
_Avoid_: Cosmetic swap, one-life skill

**Noun**:
A discoverable subject or medium from the Word Catalogue, such as Animal,
Person, Fire, Stone, or Face. A Noun can be fitted into a compatible Verb to
state what that Verb acts upon or through.
_Avoid_: Target filter, elemental damage type

**Codex**:
The player's durable library of learned Verbs and Nouns. It persists across
Incarnations and makes each later Loadout a different expression of a growing
personal language.
_Avoid_: One-life skill list, cosmetic collection

**Study**:
A primary discovery practice in which the player interprets a Study Source,
chooses one or more plausible offered words, and applies its Understanding to
those choices. It grows the Codex through contextual selection rather than
passive experience, automatic revelation, or invented vocabulary.
_Avoid_: Passive experience, automatic kill reward, random word drop

**Study Source**:
A generated encounter, event, artifact, tome, treasure, creature, material, or
phenomenon that offers plausible words from the Word Catalogue. Its rarity,
danger, and significance shape both the offered words and its Understanding
yield.
_Avoid_: Experience pickup, generic lore object, random loot roll

**Understanding**:
Durable, word-specific progress toward adding an offered Verb or Noun to the
Codex. It is gained by choosing how to interpret Study Sources rather than as
generic character experience.
_Avoid_: Experience points, character level, research currency

**Expression**:
A compatible Verb[Noun] combination equipped in an Incarnation's Loadout. An
Expression is a build choice, not a separate permanent unlock.
_Avoid_: Fixed skill, arbitrary modifier stack

**Loadout**:
The bounded selection of a Chronicle's learned Verbs made active for one
Incarnation. It begins with eight active slots and may eventually grow to ten,
but it must remain a consequential build choice rather than an always-on list.
_Avoid_: Full library at all times, permanent class kit

**Directive**:
A high-level intent addressed to one or more Agents through a social Verb. Its
risk and coercion determine which Verb can express it; valid expression lets
the Agents respond but never guarantees obedience.
_Avoid_: Unit order, job assignment, scripted follower action

**Suggest**:
A social Verb that offers a Directive without enough authority to demand
dangerous or coercive action. It fails before an Agent considers a Directive
whose required force is Command.
_Avoid_: Weak Command, guaranteed persuasion, dialogue option

**Command**:
A social Verb with enough directive force to attempt dangerous or
authority-gated intent. It satisfies the language requirement, but recipients
retain the ability to interpret, negotiate, delay, or refuse.
_Avoid_: Perfect unit control, forced job assignment, guaranteed obedience

**Intent**:
The player's explicit opening desire, expressed in the fiction and answered by
the Chronicle with a strange opportunity.
_Avoid_: Hidden class selection, random opening

**Starting Vector**:
One of the three nonbinding opening directions: Combat, Explore, or Build. A
Starting Vector provides a First Verb and reveals a First Horizon without
restricting later language, travel, settlement, or identity.
_Avoid_: Permanent class, locked build, exclusive archetype

**Fallback Class**:
A plain-language presentation of a Starting Vector offered after a death. It
selects the same underlying opportunity as the diegetic Intent route.
_Avoid_: Separate progression path, alternate ruleset

**First Verb**:
The small, immediate capability given by a Starting Vector, such as flight,
burrowing, fire breath, or taming.
_Avoid_: Starting gear, tutorial reward

**First Horizon**:
The newly reachable part of a Chronicle that a First Verb makes meaningful,
such as sky ruins, deep places, hostile territory, or a claimable Holding.
_Avoid_: Quest marker, linear level
