# Goal 7A — Someone Comes Home

**Status:** Complete and player-accepted on 2026-07-22. The complete retained
automated gate passed at zero warnings and zero errors, all five approved
fixture decisions remain frozen as written, and the player passed both UAT
journeys. No later slice is authorized by this acceptance.

## Outcome

Prove the first reusable Chronicle Agent through one named person who follows a
material event to Home, arrives under their own power, states a legible need,
accepts a welcome, and remains part of Home through departure, save/load, and
Incarnation replacement.

This is the game's first production-shaped NPC contract. The player-facing word
may be NPC; `Agent` is the canonical systemic category. The implementation must
be capable of representing hundreds of generated Agents without turning this
fixture into a `FirstNpc` singleton or eagerly simulating an infinite
population. Only one Agent becomes consequential in this slice.

Goal 7A does not add enterable buildings, explicit Areas, a general settlement
or dialogue mode, social Verbs, Directives, Companion behavior, jobs, combat AI,
factions, Pressure, or off-camera history.

## Player hypothesis

After this slice, the player should believe all of the following:

- this is a particular person, not a quest marker or worker token;
- they came for a material reason caused by this Chronicle;
- pause stops their motion and Heartbeats advance it;
- welcoming offers a place but does not recruit or command them;
- their need, decision, relationship, and location belong to the Chronicle;
- changing bodies does not reset the person or their relationship with Home;
  and
- this one person is evidence of a population system, not the only authored NPC.

If the map merely gains a decorative actor, or the UI reports a resident count,
the slice fails.

## Bounded fixture

For seed `41337`, installing the first Resonant Lode in the Hearth Resonator
creates a historical cause that World Grammar can answer: a generated
road-worn listener follows the resonance from the emptied Singing Seam toward
Home.

The deterministic fixture profile is **Tamar Venn**. `Tamar Venn` is the frozen
output for this seed and provenance, not a globally fixed character. Their
profile contains:

- one stable Agent identity;
- authored archetype `wayfarer-listener`;
- generated display name `Tamar Venn`;
- origin at the Singing Seam and a provenance fact naming the installed
  Resonant Lode;
- one active need: `Refuge`, initially `Seeking`;
- one relationship to Home: initially `Unfamiliar`;
- one current World Address while locally present; and
- one current intent that presentation can state without inferring rules.

The accepted Source-completion event promotes Tamar from deterministic
possibility to a Consequential Agent. The complete profile and continuing state
then persist. Dismantling or destroying the Source afterward does not unmake,
rename, or remotely redirect them; the material event is history, not a live
tether.

## Reusable Agent boundary

### Generated possibility

Agent generation is a pure, engine-independent function of stable authored
inputs: World seed, World Grammar version, provenance identity, and a local
ordinal. The output includes stable identity, name, archetype, origin, and
initial motives. Request order, viewport order, save/load, and unrelated
generator queries cannot change it.

Querying generated possibilities creates no Chronicle state and advances no
Heartbeat. Untouched possible Agents are not serialized or processed every
tick.

### Consequential promotion

When a generated Agent participates in a material Chronicle event, Core
promotes the generated profile into the Chronicle's Agent collection. Promotion
is idempotent by stable Agent identity. It never creates a fixture-specific
field on `ChronicleState`, `WorldCell`, a Godot Node, or a UI controller.

The durable collection supports zero, one, or many Agents. Strict validation
rejects duplicate identities, duplicate exclusive occupancy, missing origins,
unknown authored values, impossible relationship transitions, and a generated
profile that does not match its stable provenance.

### Appropriate fidelity

Goal 7A resolves only the one locally present, consequential Agent. A structural
scale fixture proves hundreds of identities and saved Agent records without
rendering them or pretending that distant lives already have full simulation.
Goal 7D owns distant causal events. No per-world eager population, global
per-Heartbeat AI loop, or hidden settlement simulation is permitted here.

## Agent facts

The first Agent model contains only facts this journey consumes:

- identity: stable ID, display name, and authored archetype;
- provenance: generated origin plus the material fact that made the Agent
  consequential;
- presence: current World Address and one bounded arrival state;
- need: one authored kind and its current state;
- relationship: one directional relationship to Home, its state, cause, and
  establishment tick; and
- intent: the next locally meaningful behavior and its timing.

Names are presentation data generated from authored name parts. Needs and
relationships are bounded authored values, not free-form property bags.
Relationship is not one universal reputation integer. Intent is inspectable
state, not an invisible behavior tree.

The first states are deliberately narrow:

| Fact | Values used by 7A |
| --- | --- |
| Presence | `ApproachingHome`, `WaitingAtHome`, `AtHome` |
| Need | `Refuge: Seeking`, `Refuge: Offered`, `Refuge: Satisfied` |
| Home relationship | `Unfamiliar`, `WelcomeOffered`, `Guest` |
| Intent | approach one step, wait, consider welcome, remain at Home |

Future contracts may add other needs, relationships, travel, death, factions,
or decisions. Goal 7A must not manufacture those frameworks in advance.

## Arrival and welcome rules

1. The first completion of an intact Hearth Resonator promotes Tamar exactly
   once and records the Resonant Lode installation as the cause.
2. Tamar begins on one prevalidated, visible, Home-relative surface route. The
   seed-`41337` fixture begins three cardinal steps west of their waiting place.
3. On each active Heartbeat, an approaching Tamar attempts exactly one
   cardinal step toward the waiting place. While paused, neither position nor
   intent changes.
4. A blocked next cell delays Tamar in place and produces a visible reason; no
   actor overlaps another and no extra step catches up later. The bounded route
   is not accepted as a general pathfinding system.
5. Reaching the waiting place changes the intent to `Wait`, records the arrival,
   and pauses once before another Heartbeat. Clearing that interruption cannot
   create the Goal 6 auto-pause latch.
6. A living Incarnation at Home may interact from Tamar's own cell or one
   cardinally adjacent cell and choose **Offer welcome**. This is hospitality,
   not Suggest, Command, recruitment, or a Directive.
7. Offering welcome immediately records an open offer, changes Refuge to
   `Offered`, and states that Tamar will answer on the next active Heartbeat.
   The Incarnation may withdraw the offer before that Heartbeat.
8. On the next active Heartbeat, Core evaluates the Agent's own facts. In the
   approved fixture Tamar accepts because Refuge is seeking, Home persists, and
   no contrary relationship exists. Acceptance changes Refuge to `Satisfied`
   and the Home relationship to `Guest`.
9. Acceptance places Tamar's named road-roll beside the Hearth as a persistent
   personal World Subject. It is not player construction, inventory, loot, a
   bed capacity, or a generic furnishing system.
10. Acceptance pauses once as a meaningful interruption. Resuming afterward
    advances normally; it does not re-answer, re-pause, or queue movement.
11. Goal 7A provides no dismissal, job assignment, follower toggle, or command.
    Tamar remains a Guest at Home. Later relationship change requires a later
    contract.

## Decision presentation

Player-facing clarity is part of the rule. The map and compact Agent panel must
make these questions answerable for movement, arrival, the open welcome, and
acceptance:

- What happens next?
- When will it happen?
- What can interrupt it?
- What will this prevent me from doing?

Core exposes structured Agent, need, relationship, intent, timing,
interruption, and constraint facts. Godot owns all sentences, key labels, and
layout. No player-facing copy enters `Chronicle.Core`.

Before welcome, the compact presentation must communicate the equivalent of:

```text
TAMAR VENN · STRANGER
NEEDS · Refuge
WHY HERE · Followed your Resonant Lode from the emptied Seam
NEXT · Waits for an answer; welcome resolves next active Heartbeat
COST · Binds no job or command; only one open welcome may exist
```

This is semantic content, not frozen wording. At most five short decision lines
appear at once. The panel changes with state; it does not stack every past and
future instruction.

An **Offer welcome** action must state before commitment:

- next: Tamar considers a place at Home;
- when: the next active Heartbeat;
- interruption: withdraw the offer, lose the living Incarnation, or invalidate
  physical contact before resolution;
- prevention: a second welcome/turn-away decision cannot be opened while this
  offer is pending; and
- consequence: acceptance creates a persistent Guest relationship, not
  obedience.

Unavailable actions state the exact reason and route to availability. A paused
pending answer remains visibly pending and tells the player that Space resumes
the Chronicle. Acceptance names Tamar, Home, the satisfied need, the
relationship, the road-roll, and the resolving Heartbeat.

## Map and visual contract

The map must visibly distinguish:

- Tamar from the Incarnation, Mire Brute, structures, and materials;
- approaching, waiting, open-offer, and Guest emphasis;
- Tamar's current position and Home's position at the same time;
- the road-roll after acceptance; and
- any blocked route cell named by the Agent panel.

Tamar is a `WorldSubject` of Agent kind. The road-roll is another durable
subject linked to Tamar's identity. Neither adds a nullable fixture field to a
cell. Visual Grammar resolves their authored archetypes and conditions through
the existing table-driven subject path.

P-GEN authors the required Agent and road-roll definitions and advances the
compatible pack contract deliberately. Player and Inspector use the same
packaged artifact, semantic snapshot, Visual Grammar, and drawing adapter. The
manual pack remains explicit comparison proof. Production projects do not
reference P-GEN compiler or catalogue assemblies, and shipped output contains
only the compiled four-file pack.

Animations may show motion or welcome, but they cannot be the only evidence of
identity, intent, need, timing, or relationship. Mechanically correct but
unclear behavior fails UAT.

## Chronicle time, departure, and death

- Agent movement and welcome response occur only on deterministic Heartbeats.
- Pause freezes Agent motion, needs, relationships, intent resolution, and
  time-driven presentation.
- Leaving the local view does not erase or reset Tamar. Returning regenerates
  the same visible Agent and road-roll from durable state.
- Incarnation death does not end Tamar, reset Refuge, remove the road-roll, or
  demote the Guest relationship. The welcoming Incarnation remains recorded as
  historical cause, while the relationship belongs to Tamar and Home.
- A replacement Incarnation must physically return and encounters the same
  Tamar. No teleport or remote resident screen is added.
- Agent injury, Agent death, inheritance, corpse handling, mourning, and
  replacement are not introduced in 7A.

## Persistence and migration

Goal 7A advances the current save envelope once, from strict v7 to strict v8,
and advances new Chronicles from World Grammar v5 to v6.

The current v8 save stores consequential Agent state and Agent-owned persistent
subjects. It does not serialize latent generated possibilities, visual IDs,
render plans, UI copy, caches, or P-GEN data.

Migration remains strict and single-runtime:

- literal v7 migrates to v8 with an empty Agent collection and retains World
  Grammar v5;
- v6 through pre-envelope fixtures continue through their literal readers and
  migrate through the same current constructor;
- old World Grammar pins do not gain Tamar or other Agents retroactively;
- new WG6 Chronicles generate the bounded possibility and promote it only when
  the material cause occurs;
- malformed v8 rejects duplicate IDs, invalid authored values, contradictory
  presence, broken provenance, impossible road-roll ownership, and relationship
  state without its cause;
- save/load at approach, arrival, pending offer, acceptance, departure from the
  viewport, death, and replacement reproduces exact state; and
- deterministic replay from seed plus commands/Heartbeats produces byte-equal
  state and the same semantic/render snapshots.

Frozen v7 save bytes and Goal 6B render-plan digests remain compatibility
oracles. There is no parallel Agent runtime or migration-only gameplay path.

## Automated acceptance fixtures

### Core Agent grammar and scale

1. Generate `512` profiles from distinct stable provenance keys in forward,
   reverse, and shuffled request order. Assert byte-equal per-key output,
   unique stable identities, authored name/archetype validity, and no Chronicle
   mutation.
2. Construct and strict-v8 round-trip `256` valid consequential Agent records.
   Assert identity, provenance, presence, needs, relationships, and owned
   subjects survive exactly. This is a structural scale proof, not 256 active
   actors or a gameplay population cap.
3. Promote Tamar twice from the same cause and assert exactly one durable Agent
   and one material cause.
4. Reject duplicate identity, invalid provenance, duplicate occupancy, unknown
   need/relationship values, and an orphaned road-roll.

### Core journey

5. Complete the Resonator, assert Tamar is promoted once at the exact generated
   route start, then destroy or dismantle the Source and assert Tamar continues
   from recorded history.
6. Prove one step per active Heartbeat, no movement while paused, blocked-step
   delay without overlap or catch-up, exact arrival, and one-shot arrival pause.
7. Prove same-cell and cardinal-neighbor interaction, rejected distant welcome,
   open-offer facts, withdrawal, one pending offer limit, next-Heartbeat
   acceptance, and one-shot acceptance pause.
8. Prove Refuge and Home relationship transitions, welcoming Incarnation cause,
   road-roll ownership/address, and rejection of repeat welcome.
9. Leave the viewport, save/load, end the Incarnation, create a replacement,
   return, and assert the same Tamar, Guest relationship, need, and road-roll.
10. Replay the exact command/tick stream twice and compare strict state bytes,
    Agent snapshots, World subjects, and result order.

### Migration and retained rules

11. Prove literal v7 to strict v8 plus every retained literal migration. Old
    grammar pins gain no Agent. Frozen v7 bytes remain unchanged.
12. Re-run all retained combat, material, Attunement, death/replacement, save,
    visual, Inspector, and Godot-input regressions. Agent arrival cannot change
    combat timing, Load, Source behavior, or the accepted Space/focus rules.

### Visual, Inspector, and Godot

13. Compose deterministic semantic/render plans for approaching, waiting,
    pending welcome, Guest, road-roll, blocked, and restored states through the
    table-driven `WorldSubject` path.
14. Prove packaged-P-GEN and manual-pack Inspector parity for every accepted
    Agent state with no Inspector mutation or second generation path.
15. Capture the actual `1600 × 900` Godot HUD at six stages: approaching,
    waiting, open offer, accepted Guest, restored Guest, and replacement return.
    Assert map dominance, no clipping/overlap, at most five Agent decision lines,
    action/checklist parity, focus/Space safety, and visible Home/Agent linkage.
16. Drive the actual Godot keyboard journey through arrival, `P` interaction,
    focused `Space`, acceptance, and the following Heartbeat. Assert neither
    arrival nor acceptance leaves an auto-pause latch or queued extra step.
17. Prove the shipped package contains the exact compiled visual bundle and no
    P-GEN compiler, catalogue, workbench, source, or review artifacts.

## Player UAT

The two launch commands create their isolated fixtures only when their named
profile has no save; subsequent launches restore the same Chronicle:

```powershell
.\play.ps1 -Profile goal7a-welcome-uat
.\play.ps1 -Profile goal7a-replacement-uat
```

### Journey A — A stranger becomes a Guest

Launch a fresh isolated `goal7a-welcome-uat` profile prepared at the accepted
post-Resonator event boundary.

1. Confirm the map shows Home, the Resonator, Tamar approaching from the west,
   and no resident count, quest marker, dialogue modal, or worker panel.
2. Inspect Tamar. Without documentation, explain their name, need, origin,
   reason for coming, current intent, next step, and the Heartbeat that permits
   it.
3. Pause and wait. Confirm Tamar and their movement presentation remain frozen.
4. Resume. Confirm exactly one cardinal step occurs per Heartbeat. Pause and
   resume between steps; confirm no queued catch-up step.
5. Let Tamar reach the waiting place. Confirm the Chronicle pauses once and the
   map plus text agree that Tamar is waiting at Home.
6. Stand on Tamar's cell or cardinally adjacent and press `P`. Confirm **Offer
   welcome** states what resolves next, when, interruptions, the one pending
   choice it prevents, and that it grants no job or command.
7. Leave the offer paused. Confirm nothing resolves and Space is identified as
   the way to resume.
8. Resume one Heartbeat. Confirm Tamar accepts, Refuge becomes satisfied,
   relationship becomes Guest, and the named road-roll appears beside the
   Hearth. Map and text must identify the same person, place, cause, and tick.
9. Resume again. Confirm normal time advances without another welcome,
   auto-pause, duplicated log result, or movement step.
10. Save, quit, and relaunch the same profile. Confirm Tamar, their need and
    Guest relationship, the road-roll, Resonator, Lode provenance, and Chronicle
    tick restore exactly.

### Journey B — Home remembers across bodies

Launch isolated `goal7a-replacement-uat`, prepared after Journey A with the
current Incarnation ended away from Home.

1. Before replacement, confirm the summary names Tamar and Home rather than
   reducing them to a resident count.
2. Create the replacement Incarnation through the existing control. Confirm the
   Agent is not reset, cloned, teleported, or converted into a follower.
3. Return physically to Home. Confirm the same named Tamar and road-roll are at
   the same addresses and the Guest relationship belongs to Home, while the
   recorded welcome still names the prior Incarnation.
4. Inspect Tamar and explain who they are, why they came, what they currently
   need, what relationship persists, and why the replacement cannot command
   them.
5. Save and reload once more. Confirm no duplicate Tamar, repeated arrival,
   repeated welcome, or changed generated profile appears.

### Player result — passed 2026-07-22

The player completed the isolated journeys and reported **“All pass.”** The
accepted playthrough covered Tamar's arrival and welcome, the visible durable
road-roll, completed checklist, and Tamar's persistence after Incarnation
death. The road-roll sprite initially read as bread; the player accepted the
bounded proof after clarification. Direct examination of visible map tiles is
retained as separate UX debt in the active Handoff, not an expansion of or
failure in Goal 7A.

## Acceptance gate

Goal 7A passes only when all of the following are true:

- the complete repository verification gate passes with zero warnings and zero
  errors;
- strict v8, literal migration, deterministic replay, malformed-state
  rejection, structural scale, WorldSubject, Inspector parity, actual Godot
  input, visual capture, and packaging isolation fixtures pass;
- the player completes both UAT journeys without documentation;
- the player can identify Tamar, their need, material origin, current intent,
  timing, interruption, relationship, and persistent consequence from the map
  and compact text;
- the player understands that welcome is not recruitment or control;
- the player sees the same person and relationship after reload and replacement;
  and
- neither the code nor the interface implies that Agents are worker slots,
  resident counts, handcrafted singleton NPCs, or eagerly simulated world
  population.

Mechanically correct but unclear identity, cause, timing, or persistence fails
UAT.

### Automated checkpoint — passed 2026-07-22

`checks/verify.ps1` passed with exit code `0`, zero build warnings, and zero
build errors. The gate proved P-GEN authoring, strict v8 plus literal migration,
the `512`-profile and `256`-record scale fixtures, malformed-state rejection,
deterministic replay, retained Goals 6A/6B, packaged/manual Inspector parity,
the exact isolated four-file runtime pack, actual Godot keyboard/mouse paths,
focused-Space safety, and all six `1600 × 900` Goal 7A HUD captures. The player
subsequently passed both journeys above.

## Explicit exclusions

Do not add:

- explicit Area identity, Passages, interiors, enterable buildings, rooms,
  region loading, or a general pathfinder;
- base-building, housing capacity, beds, construction catalogues, crafting,
  production chains, stockpiles, jobs, schedules, workers, shifts, or resource
  consumption;
- factions, reputation, trade, vendors, dialogue trees, quests, rewards,
  romance, families, aging, reproduction, or settlement population screens;
- Suggest, Command, Directives, social Power Words, persuasion, obedience, or
  Companion travel/combat;
- Pressure, raids, off-camera event resolution, Chronicle Records, or distant
  Agent simulation;
- Agent combat, HP, equipment, injury, death, corpses, inheritance, or a general
  creature AI/stat framework;
- multiple playable Agent archetypes, a broad name catalogue, portraits, a
  conversation UI, unrelated actor-art redesign, or general animation system;
- inventory, loot, general personal-object ownership, furniture, or bed systems;
- new Load, Attunement, Source, combat, Word, Target, or opening rules; or
  Goal 7B, Goal 7C, Goal 7D, Slice 8, or unrelated visual work.

The road-roll is one non-interactable durable proof of Tamar's relationship to
Home. It grants no capacity and establishes no general object framework.

## Fixture decisions requiring approval

1. **Material cause:** first intact Resonator completion promotes the Agent;
   later Source loss does not undo history.
2. **Generated person:** seed `41337` freezes `Tamar Venn`, a
   `wayfarer-listener` seeking Refuge, while other provenance keys generate
   other deterministic profiles.
3. **Hospitality interaction:** Offer welcome is not a Directive; it resolves
   from Agent facts on the next active Heartbeat and can be withdrawn first.
4. **Visible residency:** acceptance creates a Guest relationship and Tamar's
   non-interactable road-roll beside the Hearth, not a worker or housing slot.
5. **Scale boundary:** automated fixtures generate `512` profiles and
   round-trip `256` consequential records, while production UAT activates only
   Tamar and performs no eager distant simulation.

## Stop condition

The player approved all five fixture decisions and authorized implementation on
`codex/goal-7a-someone-comes-home` on 2026-07-22.

Implementation and both player UAT journeys are complete. Goal 7A is closed.
Its acceptance does not authorize Areas, base building, Goal 7B, Goal 7C,
Goal 7D, Slice 8, or any excluded framework.

## Recommended production-authorization prompt

> Implement Goal 7A under `docs/GOAL-7A-SOMEONE-COMES-HOME.md` on
> `codex/goal-7a-someone-comes-home`. Approve the five fixture decisions as
> written, preserve the complete retained gate, and stop with both player UAT
> journeys pending. Do not begin Areas, base building, Goal 7B, Goal 7C, Goal
> 7D, Slice 8, or any excluded framework.
