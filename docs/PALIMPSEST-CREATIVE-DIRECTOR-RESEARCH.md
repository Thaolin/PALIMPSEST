# Palimpsest's Creative Director — Primary-Source Research

**Researched:** 2026-07-22  
**Status:** research brief for a project-specific Codex skill; not a production
contract and not authority to settle open mechanics

## Purpose

This brief repairs the source and direction problems in the generic
`procedural-rpg-design` skill. Its intended successor is not a universal game
design adviser. It is **Palimpsest's Creative Director**: a project-specific
decision instrument that reads Palimpsest's canon first, uses inspirations as
evidence rather than authority, protects the intended player experience across
systems and presentation, and proposes bounded proofs instead of unrequested
frameworks.

The governing project mantra is already stronger and more specific than any
borrowed north star:

> **Generate everything. Player power is the point. The world remembers.**

The inspirations clarify how to pursue parts of that promise. They do not
combine into a recipe, and none of them can overrule `AGENTS.md`, `CONTEXT.md`,
the Vision, an active contract, or the Handoff.

## Research method and source policy

Load-bearing claims below use developer-authored papers, official game and
studio pages, official development logs, or talks delivered by the developers.
Community wikis, analytics blogs, generic management sites, SEO explainers, and
unsourced summaries are deliberately excluded. The official Caves of Qud wiki
is used only for documented game structure and modding interfaces; the
developers' paper is the stronger source for design intent.

An inspiration fact is labelled **evidence**. A recommendation for Palimpsest
is labelled **take** or **reject** and is an inference from that evidence plus
project canon. This prevents a source from appearing to settle a Palimpsest
mechanic it never discusses.

## Executive synthesis

The five inspirations occupy different jobs:

| Inspiration | Evidence it contributes | Palimpsest must not infer |
| --- | --- | --- |
| Caves of Qud | authored identity joined to deep local generation; generated history expressed through places, objects, and voice | that Qud's static world map or ex-post historical rationalization is Palimpsest's topology or simulation model |
| Dwarf Fortress | material simulation, autonomous actors, asymmetric relationships, and history that remains available to play | that every possible person and place must be eagerly simulated at full depth |
| RimWorld | character-readable story generation and pacing that arranges pressures into emotional sequences | that the player directly controls Agents, or that a hidden storyteller may violate deterministic world truth |
| Path of Exile | capability-transforming combinations and unapologetic player power | that sockets, item economy, multiplicative opacity, or a mandatory build meta belong here |
| Cogmind | map-first information, direct inspection, contextual help, and redundant semantic feedback | that all information must remain visible, or that ASCII density is a universal UX law |

The shared lesson is not “simulate more” or “show more.” It is: make authored
meaning recombine, make consequences material, make people and places
specific, and make the player able to understand the cause while still feeling
surprised by the outcome.

## Caves of Qud: authored backbone, procedural depth

### Evidence

Freehold's own paper is explicit: Qud is a hybrid. Its narrative and world map
are static and handcrafted, while individual areas are highly procedural and
its bodies, environments, social landscape, and physical systems are mutable.
This resolves the previous skill's contradiction; the world map is handmade,
not freshly generated end to end
([Grinblat and Bucklew, pp. 1–2](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf)).
The official world documentation likewise describes one world definition that
mixes static mapped content with dynamic builders for caves, villages, lairs,
and historic sites
([Official Caves of Qud Wiki: Worlds](https://wiki.cavesofqud.com/wiki/Modding%3AWorlds)).

Qud's generated sultan histories are not a granular simulation of every cause.
The system selects curated events, changes shared historical-entity state, and
rationalizes causes afterward; those accounts then appear through shrines,
paintings, engravings, NPC exchange, and the journal
([Grinblat and Bucklew, pp. 1–3](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf)).
The paper further describes a deliberate voice goal: generated structured text
must match the handwritten text, while cultural domains give each sultan
characteristic identity without prescribing one life arc
([Grinblat and Bucklew, p. 2](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf)).

Qud's village generation crosses abstraction layers rather than running
unrelated randomizers: the developers describe villages with linked histories,
cultures, architectural styles, storytelling traditions, NPCs, and quests
([Bucklew and Grinblat, “End-to-End Procedural Generation”](https://www.gdcvault.com/play/1026313/Math-for-Game-Developers-End)).

### Take

- Treat authored semantics as the vocabulary and generation as composition.
  Palimpsest's Word Catalogue, Creature Grammar, World Grammar, and Visual
  Grammar should create surprising arrangements without inventing unbounded
  meanings at runtime. This is a project inference from Qud's hybrid structure.
- Make generated history discoverable through material evidence, affected
  places, remembered people, and consistent voice. A Chronicle Record should
  point back to actual simulation truth; prose is an interpretation of history,
  not a replacement for it.
- Generate across boundaries. A culture should alter speech, architecture,
  desire, material practice, and opportunity coherently, not merely assign a
  name and palette.

### Reject

- Do not copy Qud's static symbolic overworld or parasang topology. Qud's map
  is the settled inspiration fact; Palimpsest's canon separately calls for
  generated, unbounded territory on one character-scale grid.
- Do not describe Qud as “the whole world is procedurally generated.” That
  claim is false and obscures the more valuable hybrid.
- Do not use ex-post rationalization to contradict an event that actually
  occurred in the Chronicle. It may shape cultural testimony, uncertainty, or
  bias only when the underlying facts remain intact.

## Dwarf Fortress: simulated history and actors with their own lives

### Evidence

Bay 12 states the long-term goal directly: a fantasy world simulator in which
the player can take part in a rich history through different roles over several
games
([Dwarf Fortress development overview](https://www.bay12games.com/dwarves/dev.html)).
That is a useful distinction from a disposable scenario generator: history is
world state that later play can enter.

The developers' own logs show autonomous historical agents using cover
identities, gathering information, and forming asymmetric relationships. One
character may believe another is a friend while the other regards the
relationship as access to information
([Bay 12 development log, 19 December 2016](https://www.bay12games.com/dwarves/dev_2016.html)).
This is stronger evidence for agency than a generic personality-stat list: the
same relationship can carry different meaning for each participant and can
alter what happens later.

Freehold's Qud paper explicitly contrasts its curated historical accounts with
Dwarf Fortress's deeper simulation, which can be interrupted at different
points for play
([Grinblat and Bucklew, pp. 1–2](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf)).
The contrast is useful because it identifies two valid but different tools:
simulate causal world change when future play depends on it; generate bounded
accounts when the goal is perspective and texture.

### Take

- Preserve any Agent who becomes consequential as an identity with memories,
  relationships, possessions, and material effects. Their interpretation of a
  relationship need not equal the player's or another Agent's interpretation.
- Let history create affordances. A dead person, ruined Home, old refusal,
  stolen object, or former Incarnation must remain available to later play as
  more than flavour text.
- Simulate the facts that future decisions can touch; summarize or leave latent
  the rest. This is how Palimpsest can pursue an unbounded Chronicle without
  pretending to simulate infinity.

### Reject

- Do not equate depth with eager total simulation. Palimpsest already settles
  that possible creatures may remain latent and only consequential instances
  persist.
- Do not turn autonomous Agents into workers, order queues, or obedient unit
  icons. `Command` communicates intent and can fail; richer simulation should
  make acceptance and refusal more personal, not remove them.
- Do not mistake raw event volume for story. A log earns attention when it
  explains a cause, preserves a consequence, or changes a future choice.

## RimWorld: readable people and pressure with emotional pacing

### Evidence

Ludeon defines RimWorld as a story generator driven by an AI storyteller. The
storyteller chooses incidents such as storms, raids, and traders, while named
storytellers pursue different rhythms: rising tension, relaxation, or radical
randomness
([official RimWorld overview](https://rimworldgame.com/)). Tynan Sylvester's
GDC talk explains that framing the product as a story generator opened
different feature-selection and design mechanisms than treating it as a
conventional game
([“RimWorld: Contrarian, Ridiculous, and Impossible Game Design Methods”](https://www.gdcvault.com/play/1024232/-RimWorld)).

RimWorld makes pawns legible through connected facts: backgrounds constrain
work and skills; opinions affect love, betrayal, and fighting; surroundings,
injury, hunger, death, darkness, and crowding alter mood
([official RimWorld overview](https://rimworldgame.com/)). These are not merely
biographical labels. They change behaviour and therefore story.

The official game settings expose pacing machinery rather than pretending
pressure is pure chance. “Damage adaptation” increases challenge while the
player avoids losses and remains distinct from wealth and population effects
([RimWorld 1.2 update](https://ludeon.com/blog/2020/08/1-2-update-with-new-quests-psycasts-gear-and-more/)).
Ludeon has also reduced an expansion's incident dominance after it saturated
the normal play experience, explicitly allowing players to choose how much one
kind of pressure shapes a Chronicle
([“Integrating Anomaly”](https://ludeon.com/blog/2024/04/integrating-anomaly-more-with-the-rest-of-the-game/)).

### Take

- Judge a generated Agent by what the player can recognize: a concern, voice,
  aptitude, aversion, possession, relationship, and remembered event that can
  each affect action.
- Treat pacing as arrangement, not fabrication. Pressures can vary intensity,
  recurrence, and breathing room while remaining caused by factions,
  ecologies, routes, resources, and Chronicle state.
- Use emotional sequence as a review lens: curiosity, comprehension,
  commitment, and consequence should not all arrive at maximum volume or in
  random order.

### Reject

- Do not copy colony management, pawn drafting, jobs, or direct unit control.
  Palimpsest is centered on one Incarnation moving through the same physical
  world as autonomous Agents.
- Do not let a hidden director create events that have no world cause or break
  deterministic replay. A Palimpsest pressure director, if later authorized,
  may select among valid seeded possibilities; it may not counterfeit history.
- Do not force every quiet stretch into drama. The Anomaly correction is first-
  party evidence that one content family can saturate and flatten the wider
  experience even when the individual incidents are strong.

## Path of Exile: transformed capabilities and extravagant power

### Evidence

Grinding Gear Games describes active skills as gems whose behaviour can be
modified by as many as five linked support gems. Its own example changes a
Fireball so it chains or splits into several projectiles
([official Path of Exile game overview](https://www.pathofexile.com/game)).
The same page describes shared access to a large passive tree and keystones
that drastically change play, such as moving shield properties to minions.

The system's strength is behavioural transformation, not just bigger numbers.
GGG's minion-support announcement shows supports changing seek range, defensive
behaviour, targeting priority, aura behaviour, and self-damage, producing
distinct roles from combinations
([“New Gems for Summoners”](https://www.pathofexile.com/forum/view-thread/2623295)).
GGG's official overview foregrounds “devastating skills” and deep character
customization, making power expression a product promise rather than an
embarrassing edge case
([official Path of Exile game overview](https://www.pathofexile.com/game)).

### Take

- A Modifier should visibly change how a Verb behaves, what decision it asks,
  or what consequence it risks. `Chaining`, `Vast`, `Quietly`, and `Lasting`
  should read as different actions, not four percentage bonuses.
- Power may be spectacular. The design question is whether the player assembled
  and understood the condition for the feat, whether the Target's facts still
  matter, and whether the Chronicle can carry the consequence.
- Keep combinations open enough for discovery and strange builds. Verbs are
  capabilities, not classes; Modifiers are reusable authored knowledge.

### Reject

- Do not copy socket colours, gem items, trade economy, seasonal reset, or
  gear-link friction. Palimpsest already has Codex, Loadout, Link, Load, Study,
  and Attunement contracts serving different persistence goals.
- Do not create universally correct scalar supports. An attachment that adds
  damage without changing a choice, risk, Target relation, or visible result is
  suspect under Palimpsest's direction.
- Do not copy opacity as the price of depth. **Inference:** five supports, a
  large skill catalogue, keystones, items, and interaction rules create a
  combinatorial explanation burden even when each element is authored. For
  Palimpsest, the Expression preview must show changed reach, timing, scale,
  consequence, Load, compatibility, and known Target interaction before
  commitment. This inference rests on GGG's documented layering, not on a
  claim that Path of Exile failed its own UX goals.

## Cogmind: information architecture as a case study, not a commandment

### Evidence

Cogmind's developer identifies the map as the primary attention surface and
the log as a secondary record. Important combat outcomes appear on the map,
while expanded logs retain detail for later inspection
([“Message Log”](https://www.gridsagegames.com/blog/2014/02/message-log/),
[“UI Feedback: Map Dynamics 2.0”](https://www.gridsagegames.com/blog/2014/11/ui-feedback-map-dynamics-2-0/)).
The help design puts explanations at the point of need: stats can be inspected
directly, map labels support symbol recognition, and context-triggered guidance
teaches essential mechanics without requiring the manual
([“Tutorials and Help”](https://www.gridsagegames.com/blog/2016/07/tutorials-help/)).

Cogmind also documents the cost of its own density. Its original “show vital
information without extra windows” goal required a very large terminal, made
fonts too small for some players, and led to years of layout work; later
upscaling retained only the game-specific essentials of map view and attached
parts while exploring modal alternatives
([“An Alternative UI?”](https://www.gridsagegames.com/blog/2016/01/alternative-ui/),
[“Full UI Upscaling, Part 1”](https://www.gridsagegames.com/blog/2024/01/full-ui-upscaling-part-1-history-and-theory/)).

Cogmind uses multiple channels for one meaning when the redundancy is useful:
an event can have map animation, concise text, a HUD change, and sound. Its
developer explicitly treats sound as both feedback and a way to keep attention
on the map, while warning that instantaneous event sound can become chaotic
([“Sound in Roguelikes”](https://www.gridsagegames.com/blog/2014/04/sound-roguelikes/)).
Dialogue similarly changes presentation by importance: minor speech appears at
the speaker on the map and remains in the log, while major speech can use a
paced modal window
([“Dialogue UI”](https://www.gridsagegames.com/blog/2016/02/dialogue-ui/)).

### Take

- Make every visible tile and subject inspectable with both keyboard and mouse.
  Inspection should identify the subject, its relevant current condition, and
  available contextual actions without moving the player.
- Put immediate cause and consequence on the map; preserve concise supporting
  text and optional breakdowns for recall and mastery.
- Use semantic redundancy: motion shows the event, sound differentiates its
  kind or force, text names the exact cause when needed, and inspection retains
  detail. No single channel should carry required understanding alone.
- Progressively disclose detail based on the player's current decision. A
  commitment preview needs more information than ordinary travel.

### Reject

- Do not convert Cogmind's game-specific density into a universal “everything
  important is always visible” rule. Its developer documents both the purpose
  and the accessibility cost of that decision.
- Do not require ASCII, tiles, one fixed layout, a permanent parts list, or a
  dashboard aesthetic. Palimpsest's Visual Grammar and UI must serve its own
  map, Words, Agents, Chronicle time, and Home.
- Do not add particles, shake, sound, labels, and log lines to every event at
  equal strength. Multiple channels should reinforce hierarchy, not create
  simultaneous noise.

## Dialogue, personality, and atmosphere

The sources support a bounded alternative to both generic bark soup and a
large dialogue-tree framework:

1. **Simulation truth:** current place, condition, relationship, remembered
   event, possession, need, danger, and possible action come from Core.
2. **Persistent identity:** origin, culture, temperament, priorities, fears,
   cadence, and a few contradictions keep one Agent recognizable. RimWorld's
   linked backgrounds, relationships, and responses show why traits must affect
   play, not merely biography
   ([official RimWorld overview](https://rimworldgame.com/)).
3. **Authored expression kit:** bounded phrases and reactions express those
   facts in a consistent voice. Qud's generated-history system explicitly
   sought voice continuity with handwritten text and used limited domains for
   characteristic identity
   ([Grinblat and Bucklew, p. 2](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf)).
4. **Scene-aware presentation:** keep ordinary speech anchored to speaker and
   place; reserve a focused, safely paused panel for a consequential choice.
   Cogmind demonstrates that dialogue hierarchy can alter presentation without
   making every exchange a separate game
   ([“Dialogue UI”](https://www.gridsagegames.com/blog/2016/02/dialogue-ui/)).
5. **Persistent consequence:** acceptance, refusal, promise, betrayal, death,
   and changed belongings return to the Chronicle as future facts.

This composition is a Palimpsest recommendation, not a claim that the source
games use the same architecture. Generated dialogue must never invent world
state. Concise authored variation can make a person specific; more prose does
not make them more alive.

Atmosphere should carry meaning. The classic developer demonstration “Juice
It or Lose It” shows how colour, motion, particles, sound, and cascading
responses can make identical underlying rules feel dramatically more alive
([Jonasson and Purho, GDC Europe 2012](https://gdcvault.com/play/1016487/Juice-It-or-Lose)).
Palimpsest should adapt the principle, not the maximalism: feedback must expose
origin, contact, material, force, interruption, and persistent change. Reduced-
motion and silent-readable alternatives preserve the same facts. Spectacle is
earned by consequence; routine movement should remain quiet.

## What “Palimpsest's Creative Director” must do

An official studio description of creative direction usefully defines the job
as unifying how a game looks, the stories it tells, and the experiences it
offers around one clear purpose. Ubisoft's process page also emphasizes
research, prototypes, proofs of concept, a maintained mandate, and showing how
world, systems, gameplay, characters, and stories support one another
([Ubisoft: Creative Direction](https://www.ubisoft.com/en-us/company/careers/our-jobs/design/creative-direction),
[Ubisoft: Creative Process](https://www.ubisoft.com/en-us/company/how-we-make-games/creative-process)).
For this small project, that evidence supports a decision role, not a studio
org chart.

The skill should therefore:

1. **Read authority before ideation.** Read `AGENTS.md`, `CONTEXT.md`, Vision,
   Architecture, Roadmap, Handoff, the active contract, and any directly
   relevant direction note. State what is settled, proposed, or forbidden.
2. **Name the intended player moment.** Describe what the player sees, tries,
   feels, understands, and later remembers. Reject proposals whose only payoff
   is an internal system diagram.
3. **Trace the north star.** Explain how the decision supports generation,
   player power, persistent consequence, or a deliberate balance among them.
4. **Use inspiration precisely.** Cite the exact useful pattern, then name what
   Palimpsest must not copy. Never write “like Qud” or “like PoE” as if that
   were a requirement.
5. **Cross-check the whole experience.** Test world, system, Agent, dialogue,
   visual, sound, interface, persistence, determinism, and production scope for
   coherence. Not every decision changes every layer, but each layer must be
   considered.
6. **Offer taste, alternatives, and a recommendation.** Present the strongest
   direction, the real tradeoffs, and why the preferred option is more
   Palimpsest. Do not hide behind an unranked idea list.
7. **End in a bounded proof.** Define the smallest player journey and UAT that
   can falsify the recommendation. A direction document cannot authorize
   production; a contract and Handoff change still govern implementation.

## Required decision questions

For any proposed feature or experience pass, the skill should ask:

- What audacious new possibility does the player imagine or perform?
- What authored meaning keeps generation from becoming soup?
- What material, social, or spatial fact changes in the Chronicle?
- Which identity or desire can surprise, accept, reinterpret, or refuse?
- Can the player immediately answer what happens next, when, what interrupts
  it, and what the commitment prevents?
- Is immediate cause visible on the map, with supporting text and inspection
  agreeing about the same subjects?
- Does the feedback make scale and force felt without masking the grid or
  becoming mandatory motion?
- Does death preserve the right things and lose the right things?
- Is this one bounded vertical slice, or a disguised framework?
- Which explicit exclusion prevents the proof from swallowing the roadmap?

## Recommended response contract for the skill

Every substantial answer should use the smallest useful version of this shape:

1. **Recommendation:** one clear direction, written as an outcome.
2. **Canon check:** settled truths, active permissions, and conflicts.
3. **Player story:** two or three concrete moments from input through visible
   consequence.
4. **Inspiration trace:** primary-source pattern, adaptation, and rejection.
5. **System consequences:** effects on Words, Targets, Agents, time, material
   state, persistence, UI, and feedback where relevant.
6. **Tradeoffs:** strongest alternative, failure modes, and why it loses.
7. **Smallest proof:** bounded fixture, player UAT, automated facts, and
   explicit exclusions.
8. **Open decisions:** only questions that genuinely change the result;
   proposed mechanics remain proposed until approved.

This format keeps the Creative Director expressive without letting creative
advice silently become production authority.

## Anti-patterns the skill must call out

- generic north stars imported from another game;
- secondary-source claims presented as design laws;
- novelty that has no material or remembered consequence;
- procedural text that invents facts or substitutes for simulation;
- autonomous people reduced to resources or orders;
- power expressed only as a larger hidden scalar;
- information density mistaken for clarity;
- simultaneous map, log, animation, and audio noise without hierarchy;
- exact test prose freezing robotic player-facing language;
- a giant framework proposed where one journey could test the idea; and
- implementation authorization inferred from a direction note.

## Open Palimpsest mechanics this research does not settle

This brief deliberately does not choose body levels, attributes, skill growth,
Mana, Magic Power, Ink, the first Modifier matrix, world-scale Expression
costs, domination, Pressure pacing rules, inventory structure, dialogue data
schemas, Area generation algorithms, or a permanent UI layout. It gives
Palimpsest's Creative Director a stronger way to evaluate those decisions when
they are actually in scope.

## Recommended package shape

The successor skill should be named `palimpsest-creative-director` and keep a
short router plus focused references:

- `SKILL.md`: triggers, authority order, workflow, response contract, and stop
  rules;
- `references/palimpsest-north-star.md`: project truths and atmosphere;
- `references/inspiration-synthesis.md`: the five take/reject profiles above;
- `references/power-and-progression.md`: Body, Language, Leverage, Verbs,
  Modifiers, and power-fantasy evaluation without settling open mechanics;
- `references/agents-dialogue-and-story.md`: agency, memory, voice, pressure,
  and truth-preserving generated expression;
- `references/clarity-and-feedback.md`: map-first inspection, the four decision
  questions, hierarchy, animation, sound, and reduced-motion parity;
- `references/decision-protocol.md`: canon check, recommendation, bounded proof,
  UAT, exclusions, and authorization boundaries; and
- `references/sources.md`: only cited primary and official sources, with the
  Palimpsest adaptation stated separately.

The old generic skill should not remain as a competing adviser after the new
package validates. Keeping both would reintroduce ambiguous authority and let
the generic north star trigger on the same work.

