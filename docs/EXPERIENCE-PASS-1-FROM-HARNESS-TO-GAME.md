# Experience Pass 1 — From Harness to Game

**Status:** Approved experience direction on 2026-07-23. Documentation only.
Its bounded implementation contract is now the approved
[Goal 7C — From Harness to Game](GOAL-7C-FROM-HARNESS-TO-GAME.md); production
remains unauthorized. This direction does not authorize UI implementation, new
mechanics, catalogue expansion, save changes, Goal 7D, or a broad visual
rewrite.

## Why this pass exists

The simulation is coherent, deterministic, and interesting. The interface
still often speaks like the acceptance harness that proved it:

- routine actions announce that an Incarnation moved or an action completed;
- meaningful decisions are forced into repeated `STATE`, `WHEN`, `INTERRUPTS`,
  and `PREVENTS` labels;
- checklists explain fixtures more often than characters and places express
  themselves;
- effects resolve mechanically before they feel physical, dramatic, or
  personal; and
- exact copy assertions risk making test language harder to improve than the
  underlying rules.

The four decision questions remain mandatory. They are a player-understanding
outcome, not a compulsory visual form.

## Atmosphere north star

### Design mantra

**Generate everything. Player power is the point. The world remembers.**

- **Generate everything:** territory, creatures, people, material histories,
  pressures, discoveries, and situations arise from authored Grammar rather
  than authored corridors. Generation must produce specificity and causality,
  not soup.
- **Player power is the point:** the game should continually reveal something
  audacious the player can attempt. Difficulty exists to make discovery,
  preparation, and expression matter, not to keep the player small.
- **The world remembers:** moved matter, changed people, destroyed places,
  promises, disasters, and dead bodies remain part of the Chronicle.

Supporting principles:

- **People are not furniture.** Agents possess voice, desire, memory, and the
  right to surprise or refuse.
- **Clarity without bureaucracy.** The player understands consequences through
  the map, motion, sound, concise language, and optional detail—not a form they
  must read before every click.
- **Specific beats generic.** “Tamar will not fight for a roof” is stronger
  than “Directive refused due to relationship state.”
- **Spectacle is earned, then delivered.** When the player assembles an
  impossible Expression, the game should not resolve it like a checkbox.
- **The map is the stage.** Panels support the world; they do not become the
  world.

## Physical scene

A player sits close to a dark screen late at night, reading a dense and
dangerous generated place. Mineral colors, strong silhouettes, motion, and
brief human reactions pull attention into the map. Interface rails recede like
useful marginalia until a real decision needs them. A strange event should
feel discovered in a place, not received from a dashboard.

This retains the established dark, restrained palette and symbolic pixel
world. It rejects both sterile debug telemetry and ornamental fantasy chrome.

## Desired emotional rhythm

Normal play should move among four feelings:

1. **Curiosity:** What is that? Can I reach it? What happened here?
2. **Comprehension:** I understand the immediate opportunity and danger.
3. **Commitment:** I choose a risk, cost, relationship, or Expression.
4. **Consequence:** The map, people, sound, and Chronicle react in a way worth
   remembering.

The interface should not give all four the same visual volume. Curiosity is
quiet. Commitment is focused. Consequence is allowed to be loud.

## Player-facing language

### Voice rules

- Address ordinary action as **you**, not “the Incarnation,” unless history is
  distinguishing this body from another.
- Name particular people, places, objects, materials, and causes.
- Prefer a concrete verb over “action,” “state,” “applied,” “resolved,” or
  “completed.”
- Do not log ordinary movement unless the step itself creates a consequence.
- Keep system precision available in inspection, forecasts, and breakdowns;
  do not make it the first sentence the player reads.
- Let Agents speak with their own cadence and priorities. The UI narrator must
  not speak every character's feelings for them.
- Use Chronicle terminology when it adds fantasy or precision, not as a tax on
  every sentence.

### Directional examples

| Harness language | Player-facing direction |
| --- | --- |
| “The Incarnation moves with everything physically attached.” | no log line; show the carried object move, change the gait/marker, and play a burden sound |
| “Action completed.” | show the exact change and name it only when text helps: “The Lode tears free.” |
| “Tamar accepts Refuge.” | “Tamar sets her road-roll beside the Hearth. ‘For a while, then.’” |
| “Directive refused: no violent commitment.” | “Tamar shakes her head. ‘A roof isn't an oath to die for.’” with the relationship reason available beneath |
| `STATE / WHEN / INTERRUPTS / PREVENTS` | one natural commitment summary; reveal the four precise answers on focus, inspection, or confirmation |
| “Attune 12/12” | “The Resonator can hold this Expression.” with `12 / 12 Load` retained as build detail |

These are tone examples, not frozen production copy.

## Dialogue popups

Dialogue should make generated Agents feel present without creating a separate
conversation game.

### Presentation

- A compact dialogue panel appears near the speaking Agent or along the lower
  map edge while keeping the speaker and immediate place visible.
- The Chronicle pauses before a consequential choice. Space remains the Clock
  control and cannot accidentally activate a response.
- The speaker's name, visual identity, relationship, and current concern are
  immediately legible.
- One to three short lines establish voice. Long exposition moves to an
  inspectable Chronicle Record, Codex entry, or optional continuation.
- Choices use natural intent and expose important cost or danger without
  converting every exchange into a dialogue tree.
- Leaving, declining, silence, or returning later may be valid when the fiction
  permits them.

### Generated personality

Personality comes from deterministic authored ingredients rather than generic
random prose:

- culture, origin, role, temperament, priorities, fears, and speech cadence;
- relationship and remembered events with the current or prior Incarnations;
- current need, danger, place, and material circumstance;
- bounded phrase and reaction kits written for those ingredients; and
- persistent choices that keep the same Agent recognizably themselves.

Generated dialogue must never invent simulation facts. Core supplies the
truth; presentation selects an authored way for that person to express it.

## Interface hierarchy

### The map

- Remains the dominant surface during exploration, Combat, building, Agent
  interaction, and consequences.
- Shows selection, reach, danger, pending commitments, damage, relationships,
  ownership, and persistent change spatially whenever possible.
- Uses animation and sound to make cause and result readable before the player
  opens a log.

### Context presentation

- The primary contextual surface answers: what am I looking at, what can I do
  here, and what just changed?
- Routine telemetry recedes. Exact Heartbeats, addresses, raw values, and
  causal breakdowns remain available to players who inspect them.
- Disabled actions explain the nearest useful route to availability, but do
  not fill the screen with permanent warnings.
- High-commitment actions earn a focused preview. Ordinary movement and obvious
  interactions do not.

### Information allocation

“Map-first” and “progressive disclosure” are not sufficient layout direction
without deciding where information lives.

| Availability | Information |
| --- | --- |
| **Always visible** | the dominant map; Chronicle Clock and pause state; immediate danger and current-body condition; any pending commitment that still needs time, input, or an interruption decision |
| **Contextual while it can change the next decision** | selected subject and relation; available actions; what happens next, when, interruption, and opportunity cost; Target health and decisive mitigation; current versus next-Attunement Load; the reason that decided an Agent's response |
| **On demand** | exact Heartbeats and World Addresses; full arithmetic and provenance; Codex compatibility and known constraints; relationship and memory history; Chronicle Records and other long-tail inspection |

The contextual rail changes role rather than preserving one fixed stack. It
shows observation while the player is reading the world, commitment while a
meaningful action is being considered, and consequence after resolution, then
recedes. It must not retain stale Combat or test-fixture context when the map's
subject has changed. The exact `1600 × 900` layout remains a contract fixture.

### Codex and Loadout

- The Codex should feel like a growing book of impossible capabilities, not an
  array of test flags.
- A Word shows its fantasy first, then compatible Modifiers, known constraints,
  Load, and exact authored values.
- Building an Expression should make transformations visible immediately:
  changing reach, scale, duration, timing, damage, notice, and cost.
- Invalid combinations explain why without erasing discoverability or solving
  every Target as a recipe.

The first UX implementation's Codex/Loadout scope is no longer open: the first
bounded pass includes only the existing
Primer-to-Codex-to-Attunement path required by the selected journey. It does
not authorize a complete Codex redesign or broader Word acquisition system.

## Graphics and feedback passes

The experience work should proceed as bounded passes over shared P-GEN and
Visual Grammar seams, not one unreviewable “graphics overhaul.”

Passes A through D describe the long-term sequence, not the scope of one
implementation contract. Experience Pass 1 selects only the cues required by
its three existing journeys; it does not complete every pass, biome, actor, or
effect family.

### Pass A — Readability and identity

- Stronger actor silhouettes, facing, selection, ownership, condition, and
  relationship cues.
- Clear visual hierarchy among terrain, interactable matter, Agents, threats,
  structures, and effects.
- No clipping, overlap, tiny principal subjects, or ambiguous bread-shaped
  personal places.

### Pass B — Physical action

- Short, readable movement, interaction, melee, damage, carrying, building,
  damage, destruction, and repair motion.
- Contact and origin matter: a melee strike visibly travels from attacker to
  adjacent Target; a carried Lode remains physically attached to the carrier.
- Restrained hit pause, shake, particles, color, and sound communicate force
  without obscuring the grid or requiring reflex input.
- Reduced-motion alternatives preserve all gameplay information.

### Pass C — Expression identity

- Each Verb has a recognizable release language before Modifiers alter it.
- Modifiers visibly transform that language: Vast changes footprint, Quietly
  changes notice and sound, Chaining shows propagation, Lasting leaves a
  continuing mark.
- Every effect dimension selected by a contract must produce the corresponding
  player-facing change required by the
  [Power Progression Direction](POWER-PROGRESSION-DIRECTION.md), not only a
  tooltip or arithmetic change.
- Preparation, interruption, release, consequence, and Recovery are distinct
  without becoming generic cast bars.

### Pass D — People and place

- Dialogue reactions, idle motion, personal belongings, relationship changes,
  and remembered places give Agents presence.
- Home gains identity from its actual terrain, structures, residents, scars,
  and history rather than decorative base-screen furniture.
- Weather, day/night, ambience, and local sound eventually reinforce generated
  biome and history, but require separately bounded scope.

## Sound direction

Sound is part of causality, not an optional polish layer:

- movement communicates terrain and burden;
- weapons communicate contact, material, mitigation, and miss;
- Expressions communicate Verb identity, Modifier transformation, and scale;
- Agents use short vocal reactions or signatures without requiring full voice
  acting;
- Home and generated places develop recognizable ambience; and
- pause, pending commitment, danger, acceptance, refusal, destruction, and
  discovery each have restrained semantic cues.

Authored semantic sound is required in the first bounded pass. Its minimum
proof is:

- distinct melee contact with a separately readable Armor or mitigation answer;
- distinct Burn Preparation, release, and impact;
- burden, extraction, construction, and destruction cues for the Resonant Lode
  and Hearth Resonator; and
- short, nonverbal acceptance and refusal cues that support rather than replace
  Tamar's words and motion.

Sound never becomes the sole carrier of a rule. Sound-off play and
reduced-motion presentation must preserve the same identities, timing,
interruptions, and consequences. This direction does not imply a soundtrack,
full voice acting, or complete biome ambience.

## Quest and guidance direction

- Objectives name a person, place, desire, or material consequence rather than
  a test module.
- A short checklist is valid for an actual multi-step commitment, but it is not
  the default voice of every interaction.
- Guidance responds to the current state and disappears when understood.
- Quest text may be casual, specific, and personal. Completion should produce
  a visible world or relationship change, not merely cross out every line.
- The player should be able to ignore a suggested opportunity without the game
  pretending the Chronicle stopped existing.

## Verification philosophy

Automated proof must stop freezing awkward prose while retaining mechanical and
accessibility certainty.

### Assert exactly

- Core facts, costs, timing, availability, interruption, and persistence;
- the presence and accessibility of required information;
- identity and map/text agreement;
- input behavior, focus safety, layout bounds, contrast, and reduced-motion
  behavior;
- semantic visual roles and appropriate feedback states; and
- deterministic output where the system promises determinism.

### Do not freeze unnecessarily

- entire player-facing sentences;
- literal checklist prefixes;
- routine movement narration;
- one exact line break or phrase when several clear variants are valid; or
- UAT instructions that tell the player the answer being tested.

Player UAT should give a goal and enough starting context, then ask what the
player believed happened and why. If the journey requires the contract beside
the game, the interface has failed.

## Selected acceptance journeys

The approved [Goal 7C contract](GOAL-7C-FROM-HARNESS-TO-GAME.md) uses these
three existing journeys and adds no mechanic:

1. **From page to fire:** find and read the Burn Primer, understand learned
   versus attuned Words, configure the existing Expression, then engage the
   Mire Brute and distinguish adjacent Cleaver contact and mitigation from
   Burn Preparation, release, and persistent consequence.
2. **Power made physical:** tear the Resonant Lode from its Singing Seam, carry
   it Home, build the Hearth Resonator, then damage, destroy, and rebuild it
   while correctly predicting current versus next-Attunement capacity.
3. **A roof is not an oath:** meet and welcome Tamar, recognize the road-roll
   as a personal belonging, see one safe Suggestion accepted, and understand
   Tamar's personal refusal of a dangerous Command without mistaking Command
   for control.

The player receives a goal and starting context, not a scripted button
sequence. After each journey, UAT asks what the player believed would happen,
why it happened, what changed permanently, and what they now want to try.

The slice fails if fixture documentation is needed; learned and attuned Words,
Cleaver and Burn, current and next capacity, or Suggest and Command are
confused; the player cannot point to the persistent map consequence; or Tamar
is remembered only as “the NPC.”

## First-pass acceptance gate

- No routine message says only that an Incarnation moved or an action
  completed.
- No normal screen permanently prints the four proof labels as a form.
- Every meaningful commitment still makes the four answers immediately
  discoverable.
- Dialogue keeps the Agent and place visible, pauses safely, and never lets
  Space activate a response.
- At least one Agent is recognizable by voice, concern, belongings, and memory
  rather than name alone.
- Movement, melee, Invocation, interaction, construction, damage, destruction,
  acceptance, and refusal each have distinct visual feedback; the required
  semantic sound cues and their accessible equivalents are present.
- Map and supporting text always identify the same subjects, causes, and
  consequences.
- The player completes the selected journeys without fixture documentation and
  can explain what changed.
- The complete retained simulation, migration, replay, P-GEN isolation,
  packaging, Inspector, and prior acceptance gate remains green.

## Explicit exclusions

Until a production contract is approved, this direction does not add:

- Goal 7D Pressure, off-camera history, raids, Areas, base building, inventory,
  crafting, jobs, broad factions, or additional Agent simulation;
- the ten candidate Verbs or Modifiers from the Power Progression Direction;
- body levels, attributes, skills, mana, Ink, domination, army control, or
  world-scale Expressions;
- runtime natural-language generation, network AI dialogue, free-text intent,
  or dialogue-tree frameworks;
- final portraits, full voice acting, weather, a soundtrack, every biome, or a
  wholesale P-GEN rewrite by implication;
- a second simulation, second visual mapping, or gameplay logic in Godot; or
- deletion or weakening of retained tests merely because their current player
  copy is being replaced.

## Contract fixture decisions

The direction now fixes the three journeys, bounded Codex and dialogue scope,
information allocation, contextual-rail behavior, and required semantic sound.
The approved [Goal 7C contract](GOAL-7C-FROM-HARNESS-TO-GAME.md) settles the
remaining questions as six bounded fixture decisions:

1. the exact per-journey motion, timing, and sound cues, including reduced-motion
   and sound-off equivalents;
2. which cues require new P-GEN assets, which are composed by Visual Grammar,
   and which are transient Godot presentation without crossing their ownership
   boundaries;
3. the exact contextual-rail layout and state transitions at `1600 × 900`;
4. how much authored personality and reaction variation proves Tamar can be
   specific without creating a dialogue framework;
5. which screenshot, input, focus, contrast, motion, audio, and
   player-comprehension oracles replace exact prose assertions; and
6. whether the three journeys use isolated UAT profiles or one deliberately
   ordered profile without creating fixture-only player logic.

The player approved all six fixture decisions on 2026-07-23 and directed this
pass to become Goal 7C. Direction and contract approval do not authorize
production.
