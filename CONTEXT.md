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
The deterministic time of a Chronicle, advancing through fixed Heartbeats from
which its calendar and day–night cycle derive. Pausing freezes autonomous world
change and time-driven behavior while inspection, Loadout configuration,
deliberate movement, and action preparation remain responsive. Only suitably
large or durable acts commit substantial in-world time, and safe waiting skips
to meaningful change.
_Avoid_: Hard turn sequence, twitch combat clock

**Heartbeat**:
One fixed deterministic pulse of the Chronicle Clock. Immediate danger runs at
a legible Slow pace or remains paused; it never requires reflex-speed input.
_Avoid_: Action-point turn, hidden timer, mandatory real-time speed

**Stratum**:
A two-dimensional spatial layer of a World, such as sky, surface, underworld,
or a stranger place. Persistent passages and Verbs connect Strata and may
eventually open routes to other Worlds.
_Avoid_: Flat world map, fully simulated voxel layer

**Area**:
A connected character-scale playspace within a Stratum. Open territory,
Holdings, dungeons, temples, ruins, and their nested spaces all use the same
movement, Combat, building, and Chronicle-time rules even when passages place
them in distinct generated Areas.
_Avoid_: Strategic-scale tile, battle instance, fixed authored level

**Passage**:
A persistent traversable connection between Areas, Strata, or Worlds. A
Passage may be a door, stair, cave mouth, route, portal, or stranger threshold,
but entering one never implies a different combat or movement game.
_Avoid_: Level-select button, unexplained loading portal

**World Address**:
A stable World, Stratum, Area, and coordinate used to locate a place, Agent,
Holding, or Passage whether or not that Area is presently active. A Chronicle
retains its addresses and changes as it expands.
_Avoid_: Temporary loaded area, disposable chunk

**World Grammar**:
The composable authored rules that generate a Chronicle's Worlds, Strata,
Areas, resources, Passages, Agents, Pressures, and Landmarks. They create
unbounded structured territory and arbitrarily many generated inhabitants from
seed and persistent history without hand-built levels or an authored outer
edge.
_Avoid_: Random-content machine, fixed campaign map, hand-authored level

**World Subject**:
A durable semantic thing at a World Address, identified by its persistent
identity, authored archetype, current condition, a bounded set of meaningful
marks, and at most one bounded current/maximum measure. Creatures, Targets,
Study Sources, material resources, construction sites, and Load Sources share
this vocabulary; presentation decides how each appears.
_Avoid_: Per-feature cell field, render identifier, unbounded property bag

**Visual Grammar**:
The coherent presentation rules that make generated Chronicle state readable
and distinctive through palette, symbols, tile composition, adjacency, and
controlled variation. World Grammar decides what exists; Visual Grammar decides
how it appears without changing simulation meaning.
_Avoid_: Random cosmetic noise, gameplay rules hidden in the renderer, one-off
art direction per location

**World Atlas**:
A symbolic overview of a World's generated geography, Strata, Landmarks, known
routes, and material history at a broader scale than the Incarnation's local
view. A player-facing Atlas represents what the Chronicle has made knowable,
not omniscient world truth.
_Avoid_: Miniature tactical scene, eagerly generated finite map, free
fast-travel menu

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

**Found**:
The Build Starting Vector's First Verb. Found establishes the singular Home at
the Incarnation's current valid place by giving existing supported matter a
durable Hearthstone identity.
_Avoid_: Base-management command, remote claim, conjured terrain, extra Holding

**Agent**:
An autonomous person, mutant, creature, construct, or stranger with continuing
identity, needs, relationships, and agency in a Chronicle. Agent is a systemic
category, not a species or a promise of obedience.
_Avoid_: Worker token, population count, helper species

**Creature Grammar**:
The authored body plans, materials, traits, capabilities, ecologies, behaviors,
and visual kits from which World Grammar can generate arbitrarily many distinct
creature Agents. Generated instances gain durable identity when Chronicle
history makes them consequential.
_Avoid_: Infinite authored bestiary, unconstrained random monster, disposable spawn

**Companion**:
An Agent currently traveling or fighting beside an Incarnation through a
relationship, shared purpose, or accepted Directive. A Companion chooses its
own actions and is never a directly controlled unit.
_Avoid_: Helper unit, pet slot, party pawn

**Combat**:
A consequence-bearing Chronicle conflict resolved through positioning,
physical actions, equipment, autonomous Companions, and Invocations. It remains
part of ordinary world state and time rather than entering a separate mode.
_Avoid_: Spellcasting-only duel, reflex arena, isolated battle screen

**Engagement Plan**:
The small player-configured set of available combat behaviors that activate
when immediate danger is engaged, such as readying a Weapon or calling a
Companion. Engagement applies the plan and pauses before the first hostile
Heartbeat so the player may inspect, invoke, change behavior, or retreat.
_Avoid_: Combat-mode load screen, scripted opening turn, direct Companion order

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
The large authored set of Verbs, Modifiers, meanings, and compatibilities from
which Chronicles draw. World Grammar generates where and how catalogue words
can be discovered; it does not invent their semantics.
_Avoid_: Skill tree, generated definition, per-Chronicle rules vocabulary

**Power Word**:
An authored, discoverable Verb or Modifier that can enter the persistent Codex.
_Avoid_: Skill point, spell rank, generated command

**Verb**:
A discoverable capability from the Word Catalogue that remains available to
later Incarnations once learned. A Verb states the base magic that can be done,
such as Fly, Burn, or Smash.
_Avoid_: Cosmetic swap, one-life skill

**Modifier**:
A discoverable Power Word linked to a Verb to change how its magic acts, such
as its reach, speed, scale, persistence, precision, or collateral. The same
Modifier may support multiple Verbs but may appear only once in one Expression.
_Avoid_: Noun, passive stat bonus, arbitrary script

**Target**:
An actual Chronicle subject or place selected when an Expression is invoked.
Its world facts constrain the result without prescribing one exact Word recipe.
_Avoid_: Collectible Noun, target Word, abstract type filter

**Link**:
One occupied position in an Expression's connected Word chain. The Verb occupies
the first Link, so a six-link Expression contains one Verb and five Modifiers.
_Avoid_: Skill level, repeated-use rank, permanent class upgrade

**Codex**:
The player's durable library of learned Verbs and Modifiers. It persists across
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
Durable, word-specific progress toward adding an offered Verb or Modifier to the
Codex. It is gained by choosing how to interpret Study Sources rather than as
generic character experience.
_Avoid_: Experience points, character level, research currency

**Expression**:
An order-independent Loadout configuration of one active Verb and zero or more
unique compatible Modifiers. It can be invoked against a Target but is not a
separate permanent unlock.
_Avoid_: Fixed skill, Verb[Noun] pair, arbitrary semantic program

**Invocation**:
One deliberate attempt to apply an active Expression to a chosen Target. Target
facts and Chronicle state determine rejection, preparation, and resolution.
_Avoid_: Expression, reflex cast, guaranteed effect

**Preparation**:
An interruptible Chronicle-time commitment made before an action releases. It
creates exposure and may be abandoned for an incompatible immediate action.
_Avoid_: Passive cast bar, mandatory idle wait

**Recovery**:
A bounded period after an action releases during which that action cannot be
repeated, while the actor remains free to take other available actions.
_Avoid_: Universal cooldown, forced inactivity

**Load**:
The shared numeric capacity of a Loadout. Every equipped Verb and Modifier
attachment consumes a fixed authored amount independent of the chosen Target;
equipment and Companions do not consume it merely by being present.
_Avoid_: Mana, inventory weight, per-Verb link limit

**Load Source**:
A durable discovery or built part of Home that increases the Load available at
Attunement. A destroyed built Load Source contributes nothing until rebuilt;
lesser damage and repair thresholds remain distinct unsettled rules.
_Avoid_: Character level, live spell tether, generic generator currency

**Burn Primer**:
The single visible book beside Home in the Goal 6B testing Chronicle. Reading it
adds Burn, Quickly, and Lasting to the persistent Codex so the material Load
loop can be tested without an opening-path prerequisite. Reading does not equip
those Words or bypass Attunement. It remains a bounded test fixture, not
inventory, loot, or a general Study Source framework.
_Avoid_: Starting-path reward, hidden prerequisite, generic spellbook drop

**Singing Seam**:
A generated place that remains at its World Address after its one Resonant Lode
is extracted. It preserves the material's visible and historical origin.
_Avoid_: Renewable node, loot container, abstract resource spawn

**Resonant Lode**:
One identifiable piece of expedition matter with a persistent Singing Seam
origin and exactly one physical state: embedded, loose at an Address, carried
by an Incarnation, committed to a Hearth Resonator, or installed in it.
_Avoid_: Inventory count, fungible ore, crafting currency

**Hearth Resonator**:
The first vulnerable built Load Source at Home. While intact, or damaged but
still functional, it contributes Load only when an Incarnation next Attunes;
destruction never remotely disables an already-attuned Loadout.
_Avoid_: Mana generator, live tether, general building type

**Attunement**:
The act of creating or changing any part of a Loadout within the Load currently
available to an Incarnation. Later loss of a Load Source does not sever the
existing configuration but reduces capacity at the next Attunement, including
one for a replacement Incarnation.
_Avoid_: Respec tax, live power connection, automatic optimization

**Loadout**:
The bounded selection of learned Verbs and linked Modifiers made active for one
Incarnation. Verb slots, link capacity, and shared Load make it a consequential
build rather than an always-on Codex.
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
