# Goal 7B — A Directive, Not Unit Control

**Status:** Complete and player-accepted on 2026-07-22. All five bounded
fixture decisions were approved exactly as written, the complete automated and
retained gate is green, and the player passed both isolated UAT journeys.
Goal 7C and every excluded framework remain unauthorized.

## Outcome

Prove the smallest useful social-language loop through two authored social
Verbs, two authored Directives, and the accepted Agent Tamar Venn:

- `Suggest` successfully communicates one safe intent that Tamar accepts and
  carries out in their own Chronicle time;
- `Suggest` cannot express one dangerous intent whose minimum force is
  `Command`;
- `Command` makes that dangerous Directive admissible but does not make Tamar
  obey it; and
- Tamar's consideration, response, movement, reason, and memory remain
  deterministic and persistent.

This slice separates three decisions that must never collapse into one button:

1. **Language:** is the active social Verb strong enough to express this
   Directive?
2. **Delivery:** can this Incarnation address this Agent about this real
   Chronicle subject or place now?
3. **Agency:** how does this Agent respond from their own state and
   relationship?

Passing the language and delivery gates creates an attempt, not obedience.
Goal 7B does not add a conversation game, persuasion statistics, job control,
Companion behavior, Agent combat, or general settlement management.

## Player hypothesis

After this slice, the player should believe all of the following:

- Suggest and Command are authored Power Words in the Codex and active Loadout,
  not universal NPC-menu permissions;
- a Directive is high-level intent tied to actual people, places, and subjects,
  not a coordinate order or per-Heartbeat unit command;
- Suggest is useful for safe intent but cannot merely be retried as a weak
  Command against dangerous intent;
- Command satisfies a force requirement but Tamar still considers, accepts,
  delays, or refuses from their own facts;
- “cannot be expressed” and “Tamar refused” are different outcomes with
  different causes;
- pausing freezes consideration, one active Heartbeat resolves it, and no
  hidden queue or auto-pause latch remains afterward;
- a Directive response belongs to the Chronicle and still names its issuing
  Incarnation after save/load, death, and replacement; and
- any visible map cell can be inspected directly enough to identify the
  terrain and subjects involved in the decision.

If Command is presented as a stronger action button that directly moves Tamar,
if refusal is indistinguishable from an unavailable control, or if the player
must guess what a sprite represents, the slice fails.

## Bounded fixture

Goal 7B continues the accepted seed-`41337` Chronicle after Goal 7A:

- Tamar Venn is a Consequential Agent, Refuge is satisfied, and their
  relationship to Home is `Guest`;
- Tamar is at their accepted waiting place immediately west of the Hearth;
- Tamar's named road-roll remains one cell south of that place;
- the Hearth Resonator and its installed Resonant Lode remain intact;
- the generated Mire Brute remains alive at its existing World Address but is
  outside immediate danger range; and
- the current Incarnation is alive three traversable cells north of Home in
  each isolated UAT profile, so remote inspection and explicit movement into
  physical delivery reach are visible before the Directive preview.

The two isolated player profiles prepare the same accepted world facts and add
both social Verbs to the Codex. Journey A begins with only `Suggest` attuned;
Journey B begins with only `Command` attuned. This preparation is visible in
the interface and exists only to test the bounded social rule. It does not
grant these Words to ordinary new or migrated Chronicles and does not establish
a Word-acquisition rule.

### Social Words

The production Word Catalogue gains exactly two authored Verbs:

| Word | Stable identity | Authored Load | Directive force | Modifiers in 7B |
| --- | --- | ---: | --- | --- |
| Suggest | `word.suggest` | 1 | Suggest | none |
| Command | `word.command` | 3 | Command | none |

Their Load is fixed authored data independent of recipient or Directive.
Neither Verb bypasses the Codex, active-Verb-slot, Load, or Attunement rules.
The existing one-active-Verb limit remains unchanged. No social Modifier,
persuasion level, command resource, or generic force statistic is added.

The previously settled paused Codex/Loadout-builder direction remains future
work. Goal 7B uses two explicit prepared profiles rather than inventing another
hotkey macro or silently expanding this slice into that interface.

### Authored Directives

Goal 7B adds exactly two Directive definitions:

| Directive | Stable identity | Objective | Minimum force | Fixture response |
| --- | --- | --- | --- | --- |
| Rest by your road-roll | `directive.rest-by-road-roll` | Tamar's persistent personal place | Suggest | accepted; one cardinal step on resolution |
| Approach the Mire Brute | `directive.approach-mire-brute` | the existing living Mire Brute | Command | refused; no movement or combat action |

`Rest by your road-roll` is safe, local, and personal. Tamar accepts because
they are a Guest with satisfied Refuge, own the named road-roll, and can reach
its cell in one unblocked cardinal step. The player communicates an outcome;
they do not choose a path or a per-tick action.

`Approach the Mire Brute` is dangerous because it asks Tamar to enter a known
living threat's vicinity. Suggest fails the language gate before Tamar
considers it. Command passes that gate, after which Tamar refuses because a
Guest relationship carries no commitment to violence or Companion service.
The refusal does not damage the relationship, move Tamar, alter the Brute, or
manufacture Agent combat rules.

Command also has sufficient force to express the safe Directive, but higher
force never skips Agent consideration. This monotonic language rule is
separate from the response rule.

## Directive domain boundary

### Authored intent, real objectives

A Directive definition owns a stable identity, an authored objective kind, a
minimum social force, and the World facts required to make that intent
meaningful. Its objective references a durable Agent, subject, or place in the
Chronicle. A Directive is not free text, a quest, a dialogue choice, a job, a
Target Word, or a player-authored script.

The 7B definitions are table-driven authored data. Neither Core nor Godot may
branch on Tamar's display name, seed `41337`, a UAT profile name, or a button
label to decide admissibility.

### Admissibility before agency

Core computes Directive admissibility without asking the Agent to respond. The
snapshot exposes structured facts for:

- the active social Verb and its authored force;
- the selected Directive and minimum force;
- the recipient and objective identities and addresses;
- physical delivery reach and any missing Chronicle fact;
- whether another Directive is already pending for this recipient; and
- the exact rejection reason and route to availability.

An insufficient Verb produces an immediate language rejection. It advances no
Heartbeat, creates no pending state, and adds no Agent memory because the
Directive was never expressed to the Agent.

### Agent consideration

Once admissible and delivered, a separate Core decision evaluates the Agent's
need, relationship, presence, owned place, objective facts, and local blocker.
The social Verb may be recorded as part of the event, but Command is never an
`Obey = true` branch.

The response values consumed by 7B are deliberately bounded:

| Response | Meaning in 7B |
| --- | --- |
| `Accepted` | Tamar accepts the safe intent and completes its one-step local consequence on the resolving Heartbeat. |
| `Delayed` | the safe destination became physically blocked; Tamar remains in place, names the blocker, and does not retry invisibly. |
| `Refused` | Tamar considered the dangerous Directive and declined it for a stated Agent-owned reason. |

`Delayed` clears the pending attempt and may be tried again after the visible
blocker changes. Negotiation, conditional promises, relationship damage,
obedience probability, morale, loyalty, fear, and personality-stat frameworks
remain future contracts.

### Pending state and memory

At most one Directive may be pending for one Agent. A pending Directive stores
only stable semantic facts: recipient, issuing Incarnation, social Verb,
Directive, objective, issued tick, resolving tick, and delivery address.

An Agent memory is created only when consideration resolves. It records the
same identities plus the response, reason, resolving tick, and any resulting
address. Rejected or withdrawn attempts never masquerade as Agent memories.
The memory belongs to the Agent and Chronicle; changing bodies does not rewrite
the issuing Incarnation.

This is a bounded social-history record, not a general event-sourcing system,
quest log, reputation score, or off-camera Chronicle Record. Goal 7C owns
distant causal history.

## Exact Directive rules

1. A living Incarnation may prepare a Directive only while the recipient is a
   locally present Consequential Agent on the same Stratum.
2. The player may inspect Tamar remotely, but delivery requires the
   Incarnation to stand on Tamar's cell or one cardinal cell away.
3. The selected Loadout slot must contain the selected authored social Verb,
   with no Modifier in 7B. Merely keeping the Word in the Codex is insufficient.
4. The objective must still exist: Tamar must own the road-roll for the safe
   Directive; the Mire Brute must be alive for the dangerous Directive.
5. Core checks the Directive's minimum force before Agent consideration.
   Suggest can express the safe Directive. Suggest cannot express the dangerous
   Directive. Command can express either.
6. A successful delivery creates one pending Directive resolving at
   `current Heartbeat + 1` and pauses the Chronicle. It does not immediately
   move Tamar or predict obedience as fact.
7. Before that Heartbeat, the same living issuing Incarnation may withdraw the
   Directive while still in delivery reach. Withdrawal creates no Agent memory.
8. Moving away after delivery does not erase spoken intent. Incarnation death
   also does not unsay it; without the original living issuer it can no longer
   be withdrawn, but it resolves from the recorded facts on the next active
   Heartbeat.
9. While paused, neither consideration nor time-driven presentation advances.
   No second Directive to Tamar can be opened while one is pending. Other
   inspection, movement, and non-conflicting deliberate commands remain
   available.
10. On the resolving Heartbeat, the safe fixture produces `Accepted`, moves
    Tamar exactly one cardinal cell onto the road-roll Address, records memory,
    and pauses once. If that cell is blocked, it instead produces `Delayed`,
    names the blocker, records memory, and performs no catch-up step.
11. On the resolving Heartbeat, the dangerous Command produces `Refused`,
    records Tamar's relationship-based reason, leaves Tamar and the Mire Brute
    unchanged, and pauses once.
12. Resuming after any response advances normally. A response cannot resolve
    twice, pause again, repeat a log entry, or leave queued Agent or Incarnation
    movement.

## Visible-cell inspection

Goal 7B consumes the retained UAT debt that visible map tiles could not be
examined directly. It adds one bounded, read-only player inspection mode over
the existing map snapshot:

- `I` enters or exits inspection; mouse selection provides the same result;
- while inspection is active, WASD moves the visible cursor instead of the
  Incarnation, `Enter` selects the cell, and `Escape` exits;
- the mode and cursor must be unmistakable, and no movement command may leak
  into an Incarnation queue when inspection closes;
- inspection advances no Heartbeat, mutates no Chronicle state, and does not
  itself change the current Clock speed;
- every currently visible cell reports its World Address, terrain, and all
  visible `WorldSubject` names, kinds, conditions, and owner identity where
  present; and
- selecting Tamar exposes the same Core Agent and Directive facts used by the
  contextual action surface. Remote actions remain disabled with the physical
  route to availability.

This is not an encyclopedia, omniscient Atlas, fog-of-war system, tooltip
framework, Target-rule duplicate, or permission to reveal hidden facts. It is
one presentation adapter over already visible semantic state.

## Decision presentation

Player-facing clarity is part of the rule. For every Directive preview,
pending state, rejection, and response, the map plus compact text must answer:

- What happens next?
- When will it happen?
- What can interrupt it?
- What will this action prevent me from doing?

Core owns structured admissibility, force, recipient, objective, timing,
interruption, prevention, response, reason, memory, and address facts. Godot
owns every sentence, key label, glyph, hierarchy, and layout. No player-facing
copy enters `Chronicle.Core`.

Before the dangerous Command commits, the presentation must communicate the
semantic equivalent of:

```text
COMMAND · TAMAR VENN · APPROACH THE MIRE BRUTE
LANGUAGE · Command meets this dangerous Directive's force
NEXT · Tamar considers; Command does not guarantee obedience
WHEN · Next active Heartbeat
INTERRUPT · Withdraw before that Heartbeat
PREVENTS · No second Directive to Tamar while pending
```

This is semantic content, not frozen wording. At most five short decision lines
appear below one compact heading. State changes replace the checklist; they do
not stack instructions, history, and raw enum/value output.

The interface must visually distinguish:

- `Suggest` or `Command` being in the Codex from the one currently attuned;
- an unavailable Directive, including **requires Command**, from an Agent
  refusal after valid delivery;
- preview, pending consideration, `Accepted`, `Delayed`, and `Refused`;
- Tamar, their objective, the Incarnation, Home, and the Mire Brute on the same
  map; and
- the response's immediate result from the durable memory shown after reload.

A disabled action names exactly why and how to make it available. A pending
Directive says that Space resumes time. A response names the Agent, issuing
Incarnation, Verb, Directive, objective, reason, result, and resolving
Heartbeat without reading like a debug trace.

## Map and visual contract

The existing Tamar, road-roll, Home, Resonator, Incarnation, and Mire Brute
visuals remain canonical. Goal 7B adds semantic presentation emphasis for:

- the inspection cursor and selected cell;
- the recipient and Directive objective together;
- pending consideration;
- accepted movement to the road-roll;
- a blocked/delayed destination; and
- refusal without movement or combat.

The map must show the recipient and objective at the same time whenever both
are within the existing viewport. Text and map use the same identities and
addresses. Animation may reinforce thought, movement, or refusal but cannot be
the only evidence of state or cause.

Prefer existing compiled-pack primitives and Visual Grammar emphasis rather
than adding a decorative social asset family. If implementation requires a new
pack definition, P-GEN and the compatible pack contract must advance together.
Player and Inspector continue through the same packaged P-GEN artifact,
semantic snapshot, composer, and draw adapter. The shipped package remains the
exact compiled four-file bundle with no compiler, catalogue, workbench, source,
or review artifacts.

Goal 7A's road-roll initially reading as bread, the inability to inspect map
tiles, invisible paused queues, unexplained disabled actions, verbose stacked
instructions, focused-Space capture, top-rail overlap, clipping, and map/log
disconnects are explicit negative examples. Mechanically correct but unclear
social behavior fails UAT.

## Chronicle time, save/load, death, and replacement

- Delivery is an immediate deliberate command; consideration and any accepted
  movement resolve only on deterministic Heartbeats.
- Pause freezes pending Directive resolution and time-driven presentation.
- Save/load before resolution restores the exact pending recipient, issuer,
  Verb, Directive, objective, timing, and Clock state.
- Save/load after resolution restores exact Agent position and Directive
  memories without repeating the response.
- Leaving and returning to the viewport never clears pending or remembered
  social state.
- Incarnation death preserves pending and resolved Directives. A replacement
  cannot withdraw a prior body's pending intent and sees memories attributed to
  the prior issuing Incarnation.
- Tamar is not recruited, assigned a job, made a Companion, given equipment,
  or placed into combat by either fixture.

## Persistence and migration

Goal 7B advances the strict save envelope once from v8 to v9. World Grammar
remains v6 because the slice adds no generated subject, place, or terrain rule.

Strict v9 stores pending Directives and resolved Agent memories as semantic
state. It stores no presentation copy, inspection cursor, button selection,
visual ID, render plan, cache, or P-GEN data.

Migration remains one runtime moving forward:

- literal v8 migrates to v9 with no pending Directive and no Directive memory,
  while preserving every Agent, owned place, Codex entry, Loadout, and WG6
  fact exactly;
- migration does not grant Suggest or Command to old or ordinary new
  Chronicles;
- v7 through pre-envelope fixtures continue through their literal readers and
  the same current constructor;
- old World Grammar pins remain unchanged;
- malformed v9 rejects unknown social Verbs or Directives, non-canonical
  objectives, impossible force/response combinations, missing recipient or
  issuer identities, multiple pending Directives for one Agent, response ticks
  before issue, a memory for an unexpressed Directive, and accepted movement
  inconsistent with the recorded address; and
- deterministic replay reproduces byte-equal v9 state, snapshots, response
  order, semantic area output, and render plans.

Frozen v8 bytes, the Goal 6B render-plan digest, and every retained migration
remain compatibility oracles. No v8 social sidecar, parallel Agent runtime, or
migration-only gameplay path is permitted.

## Engine and ownership boundary

All Directive definitions, social-force checks, delivery rules, Agent response
decisions, timing, movement, memory, validation, save mapping, and snapshots
remain engine-independent C# behind `ChronicleSimulation`.

Godot may:

- translate inspection and Directive inputs into Core commands;
- render Core-owned map, inspection, admissibility, timing, and response facts;
- compose concise player-facing language; and
- prepare the two isolated UAT profiles.

Godot may not decide whether Suggest or Command is sufficient, whether Tamar
accepts or refuses, what blocks movement, which memory persists, or when a
response resolves. Checks drive the same `ChronicleSimulation` commands as the
player path.

P-GEN remains an authoring-time compiler only. Production projects never
reference its compiler or catalogue assemblies, and runtime packaging remains
isolated to compiled artifacts.

## Automated acceptance fixtures

### Social language and admissibility

1. Assert stable authored identities, kinds, meanings, fixed Load, no supported
   Modifiers, Codex membership, active-slot validation, Attunement, save, and
   migration behavior for Suggest and Command.
2. Prove the complete force table: Suggest→safe admissible,
   Command→safe admissible, Suggest→dangerous rejected before Agent
   consideration, and Command→dangerous admitted to consideration.
3. Assert an insufficient-force rejection mutates no state, advances no tick,
   opens no pending Directive, and creates no Agent memory.
4. Drive the same rules against a second generated identity with equivalent
   facts and assert the result does not depend on Tamar's name, seed, profile,
   or UI label.

### Agent response and Chronicle time

5. Prove same-cell and cardinal delivery, remote inspection with disabled
   delivery, rejected distant delivery, missing/dead objective rejection, and
   one pending Directive per recipient.
6. Prove delivery pauses, pending state survives while paused, the original
   issuer can withdraw in reach, a replacement cannot withdraw, and exactly
   one active Heartbeat resolves consideration.
7. Resolve the safe Suggestion to `Accepted`; assert one cardinal move to the
   owned road-roll, one memory, one result, one pause, and no subsequent
   catch-up movement or pause latch.
8. Occupy the road-roll destination at resolution; assert `Delayed`, a precise
   blocker, no overlap, no movement, no invisible retry, and a deterministic
   newly delivered retry after the blocker is removed.
9. Resolve the dangerous Command to `Refused`; assert the relationship-based
   reason, unchanged Tamar and Brute addresses/state, one memory, one result,
   one pause, and no combat or Companion state.
10. Assert response memories retain recipient, issuing Incarnation, Verb,
    Directive, objective, response, reason, tick, and resulting address through
    ordering and unrelated Heartbeats.

### Persistence, replacement, replay, and scale

11. Strict-v9 round-trip pending, accepted, delayed, and refused states plus a
    structural collection of `256` consequential Agents and `512` Directive
    memories without activating or rendering that population.
12. Save/load pending and resolved states, leave/return, end the issuing
    Incarnation, create a replacement, and assert exact attribution with no
    duplicate response or rewritten Agent relationship.
13. Replay the exact command/Heartbeat stream twice and compare state bytes,
    Directive snapshots, Agent snapshots, semantic subjects, render plans, and
    result order.
14. Prove literal v8→v9 and every retained literal migration, frozen v8 bytes,
    unchanged WG pins, and precise malformed-v9 rejection.

### Inspection, visual parity, Godot, and packaging

15. Inspect representative empty terrain, water/ridge, Hearth, Resonator,
    road-roll, Tamar, and Mire Brute cells. Assert address, terrain, every
    visible subject, condition, owner, and action availability come from the
    shared snapshot without mutation or hidden facts.
16. Drive keyboard and mouse inspection. Assert cursor visibility, WASD cursor
    movement without Incarnation movement, Enter selection, Escape exit, no
    leaked movement, no Heartbeat, and no changed Clock speed.
17. Compose deterministic packaged/manual render parity for inspect selection,
    recipient/objective emphasis, pending, accepted, blocked/delayed, refused,
    restored, and replacement-memory states through the shared Visual Grammar.
18. Capture the actual `1600 × 900` Godot HUD at eight stages: inspected
    road-roll, safe Suggest preview, safe pending, accepted movement,
    dangerous Suggest rejection, dangerous Command pending, refusal, and
    restored refusal memory. Assert map dominance, at-most-five decision lines,
    disabled/action parity, no overlap or clipping, and readable identities.
19. Drive actual focused-button and Space input through both responses and the
    following movement/Heartbeat. Assert no focused-Space consumption,
    auto-pause latch, duplicate result, queued Agent step, or queued
    Incarnation movement.
20. Re-run the complete retained Goal 7A, Goal 6, migration, Inspector, P-GEN,
    and package gate. Shipped output contains only the exact compiled four-file
    visual bundle and no P-GEN authoring artifact.

## Player UAT

The launch commands initialize their exact fixture only when the named profile
has no save. Relaunching the same profile restores normally:

```powershell
.\play.ps1 -Profile goal7b-suggest-uat
.\play.ps1 -Profile goal7b-command-uat
```

### Journey A — A Suggestion can work without becoming control

Launch a fresh `goal7b-suggest-uat` profile.

1. Confirm the map shows the accepted Home, Tamar, their road-roll, Resonator,
   and distant living Mire Brute. Confirm the interface plainly says both
   social Words are in the Codex and only `Suggest` is attuned.
2. Press `I` and inspect ordinary terrain, the Resonator, Tamar's road-roll,
   Tamar, and the Mire Brute. Confirm the map cursor and compact facts identify
   each without advancing time or moving the Incarnation.
3. Select Tamar from their visible cell. Confirm remote inspection is allowed
   and delivery explains that the Incarnation must stand on or cardinally next
   to Tamar. Move into reach without a queued extra step.
4. Preview **Rest by your road-roll**. Without documentation, explain the
   recipient, objective, why Suggest is sufficient, what happens next, which
   Heartbeat permits it, how to withdraw it, and what is blocked while pending.
5. Deliver the Suggestion while paused. Confirm nothing moves. Withdraw it,
   confirm no Agent memory appears, then deliver it again.
6. Resume exactly one Heartbeat. Confirm Tamar accepts and moves one cardinal
   cell to the named road-roll, the Chronicle pauses once, and map plus text
   name the same Agent, objective, Verb, response, address, and tick.
7. Resume and take one safe movement step. Confirm no repeated response,
   auto-pause, Agent catch-up, or queued player movement.
8. Preview **Approach the Mire Brute** with Suggest. Confirm it is unavailable
   specifically because the dangerous Directive requires Command, Tamar does
   not consider it, no tick advances, and no refusal memory is fabricated.
9. Save, quit, and relaunch. Confirm Tamar remains at the road-roll and the
   accepted Suggestion memory restores exactly.

### Journey B — Command makes an attempt, not an obedient unit

Launch a fresh `goal7b-command-uat` profile.

1. Confirm the same accepted world facts and that only `Command` is attuned.
   Inspect Tamar and the living Mire Brute directly from their map cells.
2. Preview **Approach the Mire Brute**. Explain why Command makes it
   admissible, which current Tamar facts still matter, what happens next, when,
   how it can be withdrawn, and what is prevented while pending.
3. Deliver the Command and leave the Chronicle paused. Confirm Tamar and the
   Brute do not move and the interface says consideration—not obedience—is
   pending.
4. Save, quit, and relaunch while pending. Confirm recipient, issuer, Verb,
   Directive, objective, resolving Heartbeat, and paused state restore exactly.
5. Resume one Heartbeat. Confirm Tamar refuses for the stated Guest/no-violent-
   commitment reason, neither actor moves or enters combat, and the Chronicle
   pauses once on the response.
6. Resume again and move one safe step. Confirm no second refusal, pause latch,
   hidden retry, or queued movement.
7. Save and reload. Confirm the refusal memory names Tamar, the issuing
   Incarnation, Command, the Mire Brute objective, the reason, and the original
   Heartbeat without changing Tamar's Guest relationship.

## Acceptance gate

Goal 7B passes only when all of the following are true:

- the complete repository verification gate passes with zero warnings, zero
  errors, and exit code `0`;
- strict v9, literal migration, deterministic replay, malformed-state
  rejection, structural scale, inspection, WorldSubject, Inspector parity,
  actual Godot input, rendered captures, and packaging isolation fixtures pass;
- both player UAT journeys pass without consulting this contract;
- the player can explain why Suggest succeeds for one intent but cannot express
  the dangerous one;
- the player can distinguish a language rejection from Tamar's refusal after a
  valid Command;
- the player predicts that one active Heartbeat permits Tamar's response and
  sees no hidden queue, duplicate response, or auto-pause latch afterward;
- the map and compact text identify recipient, objective, active social Verb,
  timing, interruption, prevention, response, reason, and persistent memory;
- direct visible-cell inspection makes Tamar, road-roll, Resonator, terrain,
  and Mire Brute understandable without guessing from sprites; and
- neither code nor interface implies direct unit control, a job system,
  dialogue tree, eager population simulation, or Companion behavior.

Mechanically correct but unclear force, agency, timing, refusal, map identity,
or persistence fails UAT.

## Explicit exclusions

Do not add:

- dialogue trees, conversation mode, free-text intent, natural-language rule
  interpretation, persuasion checks, charisma, loyalty, morale, fear, mood,
  reputation, or relationship scores;
- jobs, schedules, workers, shifts, task queues, work priorities, settlement
  population screens, remote orders, command groups, macros, or per-tick unit
  control;
- Companion recruitment, following, travel, formation, combat behavior,
  obedience training, taming, leadership progression, or Agent equipment;
- Agent combat, attacks, HP, damage, injury, death, corpses, inheritance, or a
  creature-stat/AI framework;
- accepted execution of the dangerous Directive, general Agent pathfinding,
  long-distance travel, route planning, explicit Areas, Passages, interiors,
  or region loading;
- Factions, Pressure, off-camera event resolution, Chronicle Records, raids,
  trade, quests, rewards, romance, families, aging, or reproduction;
- a general Codex/Loadout builder, new Attunement rules, social Modifiers, Word
  acquisition, Study Sources, Understanding changes, additional Verb slots,
  Load progression, or ordinary-Chronicle Word grants;
- inventory, loot, crafting, production chains, stockpiles, vendors, housing,
  beds, general construction, or base-building expansion;
- an encyclopedia, omniscient Atlas, fog-of-war system, generalized tooltip
  framework, hidden-fact reveal, or unrelated HUD/visual redesign;
- new generated subjects, World Grammar v7, multiple active UAT Agents, broad
  name/archetype content, or distant Agent simulation; or
- Goal 7C, Slice 8, the first Raid, Areas, future inventory, or any unrelated
  framework.

## Approved bounded fixture decisions

1. **Social Word setup:** Suggest has authored Load `1`, Command has Load `3`,
   neither accepts Modifiers, both are visibly injected only into the isolated
   UAT Codices, and separate profiles begin with exactly one attuned.
2. **Safe Directive:** `Rest by your road-roll` requires Suggest and resolves
   on the next active Heartbeat to one accepted cardinal move; a blocker yields
   one visible `Delayed` result with no automatic retry.
3. **Dangerous Directive:** `Approach the Mire Brute` requires Command, but
   Tamar refuses because Guest does not imply violent or Companion service;
   no dangerous Agent action or combat AI is implemented.
4. **Social persistence:** admissible delivery creates one withdrawable pending
   Directive; resolved responses become Agent memories attributed to the
   issuing Incarnation and survive save/load, death, and replacement.
5. **Inspection and scope:** `I`/mouse adds read-only examination of every
   visible cell from existing semantic state; the general Codex builder,
   acquisition, dialogue, jobs, Companions, Pressure, and pathfinding remain
   excluded.

## Stop condition

Implementation stopped with both player UAT journeys pending as required. On
2026-07-22 the player passed both journeys. Do not commit, push, merge, begin
Goal 7C or Slice 8, or add Areas, base building, inventory, or any excluded
framework without fresh explicit authorization.

## Production authorization record

On 2026-07-22 the player authorized implementation on
`codex/goal-7b-directive-not-unit-control`, approved all five bounded fixture
decisions exactly as written, required the complete retained gate, and required
the implementation to stop with both player UAT journeys pending.
