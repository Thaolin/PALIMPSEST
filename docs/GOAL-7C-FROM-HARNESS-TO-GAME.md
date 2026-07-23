# Goal 7C — From Harness to Game

**Status:** Complete and player-accepted on 2026-07-23. The complete
retained/new automated gate passes, and player re-UAT accepted the second
bounded presentation correction: the game feels better, tiles and current
inspection are legible, and the semantic sound is a welcome addition. The map
remains primary; keyboard WASD moves without an on-screen pad; arrow keys
navigate selection; `I` inspects; left click selects and inspects; right click
follows a transient visible-map route through existing movement commands;
existing actions form a compact bottom strip; and the right rail switches
between current Context and bounded Chronicle history. The first correction's
chronological log, bounded dialogue, identity-hiding, and mouse/Look repairs
remain intact. Commit, push, merge, Goal 7D production, and every excluded
framework remain unauthorized.

Canonical authority remains [AGENTS.md](../AGENTS.md), the
[glossary](../CONTEXT.md), [Vision](VISION.md),
[Architecture](ARCHITECTURE.md), [Roadmap](ROADMAP.md), and active
[Handoff](HANDOFF.md). The approved
[Experience Pass 1 direction](EXPERIENCE-PASS-1-FROM-HARNESS-TO-GAME.md)
defines the atmosphere and presentation principles. The accepted
[Goal 6A](GOAL-6A-A-REAL-FIGHT.md),
[Goal 6B](GOAL-6B-POWER-COMES-HOME.md),
[Goal 6C](GOAL-6C-ONE-RULEBOOK.md),
[Goal 7A](GOAL-7A-SOMEONE-COMES-HOME.md), and
[Goal 7B](GOAL-7B-A-DIRECTIVE-NOT-UNIT-CONTROL.md) contracts remain the
authority on what the game mechanically does.

## Outcome

Turn three accepted proof journeys into one coherent piece of Palimpsest:

1. **From page to fire** — a found Word becomes an understood, deliberately
   Attuned Expression whose physical and magical combat consequences read
   differently.
2. **Power made physical** — one Resonant Lode visibly becomes vulnerable Home
   power whose current and next-Attunement consequences remain understandable.
3. **A roof is not an oath** — Tamar arrives as a particular person, accepts
   hospitality and one Suggestion, then personally refuses a valid Command.

The goal adds presentation, authored visual/audio feedback, a bounded
Codex/Loadout surface, and experience-focused verification over existing
facts. It adds no gameplay mechanic.

## Product question

Can a player discover power, use it, bring it Home, and meet a person changed
by that history without the game speaking like an acceptance harness—while
every decision remains as precise, deterministic, inspectable, persistent, and
testable as the accepted runtime?

## Player promise

The player should later describe concrete events:

- “I found a book near the Hearth, understood what it taught me, built a Burn
  that fit this body, and watched the Cleaver and fire affect the Brute in
  visibly different ways.”
- “I tore singing matter out of the earth, carried it Home, and knew exactly
  why destroying the Resonator endangered my next build without turning off
  the power I already carried.”
- “Tamar accepted shelter and a small request, but when I tried to send them
  toward the Brute they looked at me and made it clear that a roof was not an
  oath to die.”

If the player instead remembers hotkeys, checklist labels, test modules, or
crossed-out objectives, the goal fails.

## Mechanical invariants

Goal 7C preserves the accepted runtime exactly:

- strict save `v9` remains current and every literal migration remains;
- World Grammar `v6` remains current and every older pin is unchanged;
- all rules continue behind `ChronicleSimulation` in engine-independent C#;
- Burn, Quickly, Lasting, Suggest, Command, the Mire Brute, equipment,
  Engagement, Attunement, the Singing Seam, Resonant Lode, Hearth Resonator,
  Tamar, welcome, Directives, inspection, death, replacement, and persistence
  keep their accepted semantics and authored values;
- no new durable field, save value, World Subject, Word, compatibility,
  relationship, Directive, damage rule, carrying rule, construction rule, or
  Chronicle-time rule enters scope;
- Godot owns prose, layout, input translation, transient motion, and audio; it
  does not infer a simulation decision; and
- P-GEN remains authoring-only behind the Palimpsest-owned compiled-pack reader.

Core may expose a missing **read-only derived fact** only when existing state
already determines it and the presentation cannot otherwise tell the truth.
Such a fact contains no prose, input key, visual ID, audio ID, profile name, or
fixture branch and does not change strict save bytes.

## Information contract

The map remains the dominant surface. Information is allocated as follows:

| Availability | Required information |
| --- | --- |
| **Always visible** | map; Chronicle Clock and pause state; immediate danger and current-body condition; any pending commitment still awaiting time, input, or interruption |
| **Contextual while it can change the next decision** | selected subject and relation; available actions; next result and timing; interruption; prevented alternatives; Target health and decisive mitigation; current and next-Attunement Load; the reason that decided an Agent response |
| **On demand** | exact Heartbeats and World Addresses; full arithmetic and provenance; Codex compatibility and known constraints; relationship and response memory; Chronicle Records and other long-tail inspection |

The four commitment questions remain acceptance outcomes:

1. What happens next?
2. When will it happen?
3. What can interrupt it?
4. What will this action prevent?

They must be discoverable from map, context, focus, and confirmation. They may
not return as four permanent proof labels on every screen.

## Bounded Codex and Loadout surface

Goal 7C implements the accepted Goal 6C direction for the
currently shipped catalogue only:

- `C` opens one paused full-screen Codex/Loadout surface. Opening it while time
  is running first pauses without advancing a Heartbeat.
- `C` or `Escape` closes it without resuming time. `Space` closes it and
  resumes the Chronicle; it never attaches a Word or commits Attunement.
- The surface shows every Word actually present in the current Codex. The
  acceptance fixtures use only Burn, Quickly, Lasting, Suggest, and Command.
- Each Word leads with its authored fantasy and recognizable behavior, then
  shows Load, timing, compatibility, and known constraints.
- One active Verb slot appears beside its linked Modifier chain. The existing
  Link and Load limits remain Core facts.
- Selecting or removing a Word changes only a transient proposed Loadout.
  Preview immediately shows the affected preparation, consequence, recovery,
  Load, and compatibility facts.
- One explicit **Attune** action sends the existing atomic Attunement command.
  Failed Attunement leaves the active Loadout unchanged and names the nearest
  route to availability.
- Learned, proposed, and currently Attuned Words are visually distinct.
- Keyboard and mouse expose identical selection, preview, disabled reason,
  cancel, and Attune behavior.
- The `Q`, `L`, and `G` fixture macros retire from normal player presentation
  once this surface exists. They may survive privately only where a retained
  automated fixture still requires migration-era input, never as the primary
  player path.

This is not Word acquisition, a new catalogue, an inventory, a skill tree, a
general drag-and-drop framework, build presets, respec economy, or final Codex
art direction.

## Contextual rail, dialogue, and map

At the accepted `1600 × 900` fixture:

- the right rail remains exactly `320 px`; the map receives the remaining
  width and retains the accepted crisp `2×` presentation;
- the rail has one current role rather than permanent stacked modules:
  **Observation**, **Commitment**, or **Consequence**;
- Observation answers what is selected and what can be done;
- Commitment foregrounds the natural-language forecast, timing, interruption,
  and opportunity cost for the one contemplated action;
- Consequence names what changed, where, why, and what persisted, then returns
  to Observation when the player selects or begins something else;
- stale Combat, material, Agent, Directive, or test context disappears when it
  no longer affects the next decision;
- exact arithmetic, addresses, and history remain reachable through focus,
  inspection, Codex, or the Message Log;
- no essential reason truncates, overlaps another panel, or leaves the frame;
  and
- the map, context rail, dialogue, and Message Log always refer to the same
  identities and addresses.

Consequential Agent speech uses one compact panel at the lower map edge,
anchored toward the speaker while Tamar and the immediate place remain
visible. It contains Tamar's name and relation, one to three short lines, and
only the natural choices currently permitted by Core. `Space` remains the
Chronicle Clock control and can never activate a response. Leaving, silence,
withdrawal, or later return remain available only where the existing rules
permit them.

## Six approved bounded fixture decisions

These decisions are the exact production boundary and are frozen for Goal 7C
only.

### Decision 1 — Feedback kit, timing, and accessible parity

Approve three presentation timing bands:

| Band | Maximum wall-clock emphasis | Uses |
| --- | ---: | --- |
| Routine | `120 ms` | ordinary movement, cursor movement, selection, lift/set-down follow-through |
| Decision consequence | `300 ms` | melee contact, mitigation, Burn release/impact, construction step, acceptance/refusal reaction |
| Chronicle consequence | `450 ms` | first scorch, Resonator completion/destruction/rebuild, welcome/Guest transition |

Presentation never advances or delays the Chronicle. New input may complete a
routine animation immediately at its correct final state. A longer consequence
may finish visually after Core resolves, but cannot block pause, inspection,
save, or the next deliberate command.

Approve these minimum semantic sound families:

- Iron Cleaver contact and a separately readable Quilted Jack/Armor answer;
- Burn Preparation, release, impact, and continuing consequence;
- Lode extraction, lift, carried burden, set-down, construction, damage,
  destruction, and rebuilding;
- discovery/Attunement success and precise rejection; and
- Tamar arrival, welcome, Suggest acceptance, and Command refusal.

Every sound has simultaneous map/text/motion evidence. Sound-off suppresses
playback without removing cue identity from automated proof. Reduced motion
replaces travel, shake, hit-pause, and particles with bounded cell emphasis,
pose, outline, and persistent marks. Neither mode changes Core, input timing,
or UAT comprehension requirements.

Two compact session-local controls in the existing top control cluster expose
`SOUND: ON/OFF` and `MOTION: FULL/REDUCED` with keyboard and mouse parity.
Defaults are Sound On and Motion Full. The controls do not enter Chronicle
state or its save; automation may select the same modes through launch
arguments. This is the complete accessibility-setting surface for the slice,
not authorization for a general settings menu.

### Decision 2 — P-GEN, Visual Grammar, and Godot ownership

Approve revision of exactly these existing P-GEN visual families:

- Incarnation;
- Mire Brute;
- Burn Primer;
- Resonant Lode, including carried emphasis;
- Hearth Resonator phases;
- Tamar Venn's `wayfarer-listener` archetype; and
- Tamar's road-roll, which must no longer read as bread.

No new semantic subject, terrain family, biome pass, cell profile, palette, or
pack schema is added. P-GEN owns static authored pixels and existing phase
variants. `Chronicle.Visuals` owns table-driven semantic emphasis, origin and
contact paths, Target/recipient/objective linkage, and transient render-plan
roles. Godot owns interpolation, particles, short screen response, rail and
dialogue layout, text, focus, and sound playback.

Any changed compiled pack advances its compatible digest through the existing
reader. Player and Inspector retain identical semantic and visual plans.
Audio is verified through a deterministic cue-plan check; the Inspector need
not play sound. The shipped compiled visual bundle remains exactly four files,
and no P-GEN compiler, catalogue, workbench, source, or review artifact enters
production packages.

### Decision 3 — Context rail and bounded Codex layout

Approve the `320 px` contextual rail, three rail roles, lower-edge dialogue,
and `C` Codex/Loadout behavior specified above. The rejected-UAT correction
places the existing bounded action set in one compact lower-map strip and gives
the rail exactly two transient views: **Context** for the current
Observation/Commitment/Consequence role and **Chronicle** for recent causal
history. Keyboard movement has no duplicated on-screen pad. The Codex surface supports
every production Word actually present in the current Codex and the existing
one-Verb, three-Link maximum shape. The three profiles exercise Burn, Quickly,
Lasting, Suggest, and Command directly; retained proof covers Fly, Found, and
Smash when an accepted Chronicle contains them. The surface contains no
fixture-only Burn branch and no profile-name branch.

The map remains visible behind dialogue but yields to the paused full-screen
Codex only while the player deliberately builds a Loadout. The Clock and
current/next capacity remain visible in that surface. No permanent quest
checklist, dock paging, tooltip framework, general tab/action-bar system, or
broader menu redesign enters scope.

### Decision 4 — Tamar's bounded authored voice

Approve one `wayfarer-listener` voice kit built from existing Core facts:
archetype, origin, current need, Home relationship, owned road-roll, current
Directive, and response memory.

The kit contains exactly five reaction intents:

1. arrival and need for Refuge;
2. welcome accepted and the road-roll placed;
3. safe Suggestion accepted;
4. dangerous Command refused because Guest is not violent service; and
5. later inspection or reload recognition of the remembered exchange.

Each intent supplies two authored cadence variants selected deterministically
from stable Agent identity. Tamar's selected cadence remains stable across
reload and replacement without entering Chronicle state. A second generated
`wayfarer-listener` identity exercises the alternate cadence in automation.
Templates may insert only Core-supplied names, places, relationships,
objectives, and reasons. No runtime prose generation, language model,
free-text intent, dialogue tree, personality statistics, or new Agent state is
added.

### Decision 5 — Acceptance oracles

Approve semantic and structural automation rather than frozen prose:

- assert Core facts, command results, strict save bytes, replay order,
  availability, timing, interruption, prevention, and persistence exactly;
- assert the information tier, rail role, dialogue anchor, map identity,
  semantic visual role, cue-plan role, focus route, and layout bounds;
- assert no ordinary movement log says only that an Incarnation moved and no
  result says only that an action completed;
- assert normal screens do not permanently print the four proof labels;
- assert required facts remain available under sound-off and reduced motion;
- assert keyboard/mouse parity and that focused controls never consume Space;
- assert deterministic capture manifests and cue-plan order, while avoiding
  exact full-sentence, line-break, and incidental pixel-digest locks; and
- retain human review of native captures and player comprehension as the
  oracle for atmosphere, physicality, personality, and whether the road-roll
  still looks like bread.

### Decision 6 — Three isolated UAT profiles

Approve these profiles:

| Profile | World fixture |
| --- | --- |
| `goal7c-page-to-fire-uat` | WG6 seed `41337`; Home and unread Burn Primer; empty Codex/Loadout; equipped Goal 6A body; living Mire Brute; no consequential Agent |
| `goal7c-power-made-physical-uat` | WG5 seed `41337`; Primer already read; Burn, Quickly, and Lasting known; `Burn + Quickly` Attuned under Load `8`; embedded Lode; no Source; living distant Brute |
| `goal7c-roof-not-oath-uat` | WG6 seed `41337`; intact Resonator has promoted Tamar, who is approaching Home; both social Words known but not Attuned; road-roll appears only after welcome; living distant Brute |

Each initializes only when its isolated save is absent. Relaunch restores
normally. No normal Chronicle branches on a profile name. WG5 deliberately
isolates the accepted material journey from the later Agent trigger; this is a
fixture boundary, not a second runtime or current-grammar rollback.

## Journey-specific presentation contract

### From page to fire

The map identifies the unread Burn Primer before text instructs the player to
press a key. Reading produces a short discovery consequence: the book remains
at its Address, the Codex visibly gains Burn, Quickly, and Lasting, and concise
language explains that learned is not Attuned.

In the Codex, Burn leads with fire applied to a valid Target. Adding Quickly
changes Preparation and Load visibly; removing it restores the baseline.
Attunement success names the chosen Expression and capacity. An unavailable
combination names its violated Load, Link, safety, carrying, or commitment fact
without claiming the Word itself is unusable.

Against the Mire Brute:

- the Iron Cleaver visibly originates at the adjacent Incarnation and reaches
  the adjacent Brute;
- physical contact, Armor mitigation, remaining harm, and cadence agree among
  map, rail, sound, and Message Log;
- Burn Preparation remains attached to actor and Target, exposes its next
  Heartbeat and interruption, then releases along a distinct path;
- Quickly changes exposure in preview and release timing, not merely a number;
- scorch persists on the map after the immediate effect; and
- routine movement and damage do not flood the log.

### Power made physical

The Singing Seam, Lode, carrier, Home, build site, construction, Resonator
phases, exposed Lode, and rebuild remain immediately distinguishable. The
carried Lode follows the Incarnation and changes motion and sound without
becoming an inventory icon.

Observation states what the selected material is and where it came from.
Commitment states the next material transition, remaining active Heartbeats,
interruption, Cancel route, and locked actions. Consequence shows the actual
map change, then recedes.

The Codex/Loadout surface must make these transitions unmistakable:

- before the Source, combined Burn is learned but cannot fit under `8`;
- after construction, next Attunement capacity is `12` while the current
  Loadout remains unchanged;
- after Attunement, the combined Expression records `12 / 12`;
- damaged still contributes `+4`;
- destroyed leaves current `12 / 12` usable but next Attunement at `8`; and
- rebuilding restores next capacity to `12` without changing the Loadout.

No checklist may replace the physical map loop. A short contextual objective
may name the current goal, but it disappears when the relevant state is
understood and never crosses out the experience as a completed test.

### A roof is not an oath

Tamar's sprite, movement, name, need, resonance history, and intended waiting
place agree. Arrival pauses once and opens a compact spoken beat rather than a
resident count or proof panel. Welcome uses natural intent, explains that it
offers Refuge rather than employment, and safely preserves Space as the Clock
control.

Acceptance places a clearly personal road-roll beside the Hearth and gives
Tamar a stable Guest voice. The player may inspect both owner and belonging.

After Attuning Suggest, the safe Directive shows Tamar and the road-roll
together. Acceptance uses Tamar's authored voice and motion, then records the
existing memory. After Attuning Command, the dangerous Directive shows Tamar
and the Mire Brute together. Command is visibly sufficient to express the
request, but the pending state promises only consideration. Refusal speaks the
decisive Guest/no-violent-service reason and produces no movement or combat.

The result must feel like Tamar refusing, not a disabled control becoming a
red error message.

## Player-facing language

Presentation follows these rules:

- use **you** for the current body unless history must distinguish bodies;
- use particular people, places, objects, materials, and causes;
- prefer concrete verbs such as tears, braces, strikes, burns, accepts, and
  refuses over applied, resolved, completed, or state changed;
- keep exact arithmetic and provenance available after the fictional result;
- let Tamar speak only facts and priorities the Chronicle owns;
- omit ordinary movement logs unless the step creates danger, arrival,
  contact, interruption, or another consequence; and
- treat direction examples as tone, never frozen production sentences.

## Explicit negative acceptance examples

Goal 7C fails if it reproduces any of the following:

- an invisible paused queue, catch-up step, duplicate response, or auto-pause
  latch;
- unexplained disabled Attunement or Words that appear useless after learning;
- opaque mitigation or a raw number without the Cleaver, Armor, Source, or
  other cause;
- melee, carrying, extraction, building, damage, destruction, or social
  response without a clear origin and affected map cell;
- repeated `STATE / WHEN / INTERRUPTS / PREVENTS` proof forms;
- routine “Incarnation moved” or generic “action completed” narration;
- stacked instructions, crossed-out fixture objectives, or prose that tells
  the player the answer being tested;
- stale Combat or material rails during an unrelated Agent decision;
- a road-roll that reads as bread or Tamar remembered only as “the NPC”;
- text and map naming different identities, addresses, causes, or outcomes;
- clipping, overlap, tiny principal subjects, low-contrast disabled controls,
  inaccessible focus, or sound/motion as the only carrier of meaning; or
- exact-copy tests that make clear player-facing language harder to improve.

## Persistence, replay, migration, and replacement

No Chronicle save or World Grammar change is expected. Acceptance requires:

- strict v9 bytes remain unchanged for identical pre/post-pass Core state;
- every literal v8 through pre-envelope migration remains green;
- WG6 and every older grammar pin return identical semantic subjects;
- presentation state, open panels, animation progress, sound playback, focus,
  inspection cursor, and transient proposed Loadout do not enter Chronicle
  saves;
- accepted save/load, death, and replacement results for scorch, Lode, Source,
  Tamar, road-roll, relationship, Directive memory, and issuing Incarnation
  remain exact;
- relaunch reconstructs presentation from Core state without replaying a
  discovery, consequence, dialogue, or sound as though it just happened; and
- the same semantic event stream creates deterministic rail roles, visual
  roles, and cue-plan order.

## Exact automated acceptance fixtures

`checks/verify.ps1` must retain the complete current gate and add:

### E1 — Mechanical non-drift

1. Replay the accepted Goal 6A, Goal 6B, Goal 7A, and Goal 7B command streams
   before and after presentation changes. Compare strict Core state, results,
   forecasts, save bytes, and event order.
2. Assert no new save version, grammar version, durable field, Word, Modifier,
   Agent fact, Directive, World Subject, or simulation command is required
   except the existing Attunement and interaction commands invoked from the
   new UI.
3. Re-run malformed-v9 rejection, literal migrations, deterministic replay,
   death/replacement, structural Agent scale, and existing WorldSubject proof.

### E2 — Codex and decision model

4. Exercise every current Word and accepted compatibility through the new
   surface. Assert learned, proposed, active, compatible, incompatible,
   insufficient-Load, unsafe, carrying, and pending-commitment states.
5. Prove transient edits never mutate Core before Attune; successful Attune
   sends the existing command once; rejected Attune mutates nothing.
6. Prove `C`, Escape, Enter, focused controls, Space, mouse, and keyboard paths
   produce identical commands and no leaked movement or double activation.

### E3 — Information allocation and language

7. At every captured state, assert each required fact is present in exactly
   one permitted tier and remains reachable; no stale rail role survives a
   changed subject or decision.
8. Scan normal player presentation for the bounded forbidden routine phrases
   and permanent proof-form headings while allowing exact domain terms in
   inspection and history where they add meaning.
9. Assert full decisive reasons wrap within bounds and Message Log entries are
   causally distinct rather than duplicated.

### E4 — Visual, motion, audio, and accessibility

10. Produce six native `1600 × 900` captures for each selected journey:
    Primer/Codex/combat contact/Burn/consequence; Seam/carry/build/capacity/
    destruction/rebuild; and arrival/welcome/Suggest/Command/refusal/memory.
11. Assert the `320 px` rail, map dominance, dialogue anchor, visible speaker,
    focus order, minimum control sizes, contrast, and non-overlap at the busiest
    state.
12. Assert routine, decision, and Chronicle timing envelopes; new input
    completes routine presentation without delaying the Chronicle.
13. Assert deterministic semantic cue families and order for every required
    event. Sound-off suppresses playback only; reduced motion swaps presentation
    roles while retaining every required fact.
14. Prove the corrected road-roll and the seven bounded P-GEN families through
    packaged/manual Inspector parity and the same Visual Grammar path.

### E5 — Actual Godot journeys and package isolation

15. Drive all three profiles through real Godot keyboard and mouse input,
    including focused Space, `C`, inspection, dialogue, Attunement, save,
    relaunch, and the first action after each auto-pause.
16. Assert no auto-pause latch, queued extra step, duplicate result, accidental
    response, Codex mutation, or presentation replay after restore.
17. Prove the packaged player uses the same compiled pack and semantic visual
    plan as the Inspector. The compiled bundle remains exactly four files;
    compiler, catalogue, workbench, source, and review artifacts remain absent.

A Core marker, raw JSON check, headless text oracle, silent screenshot, or
Inspector-only state cannot substitute for the actual Godot paths and player
UAT.

## Exact player UAT

UAT supplies a goal and starting context, not a button script or the answer.
After each journey, ask:

1. What did you think would happen next?
2. When did you expect it to happen?
3. What could have interrupted it?
4. What did the action stop you from doing?
5. What changed permanently?
6. What do you want to try now?

### Journey A — From page to fire

Launch:

```powershell
.\play.ps1 -Profile goal7c-page-to-fire-uat
```

Starting context: you are safe at Home. An unread book is visible nearby, your
Codex and Loadout are empty, and the Mire Brute remains alive beyond immediate
danger.

Goal: discover what the book offers, make one usable Burn Expression, confront
the Mire Brute with both Cleaver and Burn, and leave evidence that survives a
reload.

Pass questions:

- Can the player explain learned versus proposed versus Attuned?
- Can the player explain why the chosen Expression fits and what its Modifier
  changes?
- Do Cleaver contact, Armor mitigation, Burn Preparation, release, and scorch
  read as different physical causes?
- Can the player point to the persistent consequence after relaunch?

### Journey B — Power made physical

Launch:

```powershell
.\play.ps1 -Profile goal7c-power-made-physical-uat
```

Starting context: Burn, Quickly, and Lasting are known; Burn + Quickly is
Attuned under inherent Load `8`; the Lode remains embedded in its Singing Seam;
Home has no Resonator.

Goal: bring the Lode Home, make the combined Expression possible, then damage,
destroy, and rebuild the Source while preserving and explaining the difference
between current and next-Attunement power. Save and relaunch once while the
Source is destroyed.

Pass questions:

- Does acquisition and carrying feel physical rather than like a count?
- Can the player predict every commitment and cancellation from the map and
  contextual surface?
- Can the player explain why the combined Expression becomes available only at
  Attunement?
- Can the player explain why destruction does not remotely disable the current
  Loadout and why rebuilding does not automatically change it?

### Journey C — A roof is not an oath

Launch:

```powershell
.\play.ps1 -Profile goal7c-roof-not-oath-uat
```

Starting context: the Resonator has drawn one stranger toward Home. Suggest and
Command are known but neither is Attuned. The Mire Brute remains alive and
distant.

Goal: understand and welcome the stranger, inspect the personal place they
make, use Suggest for one safe intent, then use Command for the dangerous
request. Save and relaunch after the response.

Pass questions:

- Can the player describe Tamar without relying on name or stat line?
- Does the road-roll read as Tamar's belonging rather than bread, loot, or
  player construction?
- Can the player distinguish welcome, Suggest, Command admissibility, and
  Tamar's autonomous response?
- Does the refusal feel personal and causally truthful rather than like a
  disabled action?
- Does the remembered exchange remain attached to Tamar, the objective, and
  the original issuing body after reload?

## Complete retained gate

Before any player UAT, `checks/verify.ps1` must pass with exit code `0`, zero
warnings, and zero errors while retaining:

- P-GEN authoring verification and compatible compiled-pack validation;
- strict v9 serialization, literal v8 through pre-envelope migration,
  malformed-state rejection, deterministic replay, and frozen predecessor
  bytes;
- all accepted Goal 6A combat, pause, focused-Space, damage, Recovery, scorch,
  death, replacement, and rendered journey proof;
- all accepted Goal 6B Primer, Lode, carrying, commitments, Source,
  current/next Attunement, destruction, rebuilding, save, replay, and rendered
  proof;
- all accepted Goal 7A generation order, structural scale, arrival, welcome,
  Guest, road-roll, save/load, replacement, and rendered proof;
- all accepted Goal 7B force table, Directive delivery, withdrawal,
  Accepted/Delayed/Refused response, memory, inspection, save/load,
  replacement, and rendered proof;
- player/Inspector semantic and render parity through the packaged P-GEN and
  explicit manual comparison paths;
- actual Godot keyboard/mouse paths, layout and clipping guards, and
  deterministic native captures; and
- exact compiled four-file visual bundle isolation with no compiler or
  catalogue leakage.

Existing tests may be refactored only to stop freezing superseded player prose.
Their mechanical, accessibility, input, identity, layout, migration, replay,
Inspector, and packaging assertions may not be deleted or weakened.

## Acceptance gate

Goal 7C passes only when:

- the complete retained and new automated gate is green;
- all three player journeys pass without this contract or fixture instructions;
- the player understands discovery, Codex, Loadout, and Attunement without
  believing a learned Word is already usable;
- physical and magical Combat actions expose origin, contact, mitigation,
  timing, interruption, and persistent consequence;
- the full Lode/Source loop is understandable without a permanent checklist;
- current and next-Attunement capacity are never confused;
- Tamar is recognizable by concern, cadence, belongings, relationship, and
  response rather than name alone;
- Suggest, Command, refusal, and control remain distinct;
- map, motion, sound, dialogue, context, and text tell the same causal story;
- sound-off and reduced-motion play retain complete comprehension;
- no accepted pause, focus, save, migration, replay, Inspector, P-GEN, or
  packaging rule regresses; and
- the player wants another Word, another material power, or another person
  after finishing.

Mechanically correct but robotic, unclear, physically disconnected, or
personality-free behavior fails UAT.

## Explicit exclusions

This contract does not add or authorize:

- Goal 7D Pressure, off-camera history, Chronicle Records, Slice 8 raids,
  Areas, Passages, dungeons, base building, or unrelated world generation;
- inventory, loot, generic equipment, crafting, recipes, production chains,
  stockpiles, vendors, workers, jobs, or settlement management;
- any new Verb, Modifier, Directive, Agent need, relationship, personality
  statistic, compatibility, Link, Load progression, level, attribute, skill,
  mana, Ink, domination, Companion, or combat rule;
- final balance, another opponent, broad bestiary, general pathfinding, ranged
  weapons, fire spread, conditions, or a damage framework;
- runtime natural-language generation, network AI, free-text intent, dialogue
  trees, portraits, full voice acting, soundtrack, weather, biome ambience, or
  a conversation framework;
- general animation, particle, audio, tooltip, menu, settings, quest, tutorial,
  notification, or design-system frameworks;
- camera-density changes, rectangular cells, palette redesign, terrain pass,
  wholesale P-GEN rewrite, new pack schema, or unrelated actor art;
- a second simulation, gameplay logic in Godot, weakened migration, a second
  visual mapping, Inspector mutation, or compiler/runtime coupling; or
- commit, push, merge, or branch cleanup; and
- production work outside this exact bounded goal.

## Approval record

On 2026-07-23 the player explicitly approved the UX Redux direction and
authorized creation of this documentation-only bounded contract. Later that
day, the player approved all six bounded fixture decisions and directed the
pass to become Goal 7C. These approvals settle the atmosphere mantra,
information allocation, contextual-rail direction, required semantic sound,
three selected journeys, exact fixture boundary, and the rule that power
dimensions must change presentation as well as arithmetic. They do not
authorize production by themselves.

The player's subsequent instruction, **“engage,”** explicitly invoked the
recommended prompt below and authorized Goal 7C production on
`codex/goal-7c-from-harness-to-game` under the frozen decisions, retained gate,
UAT boundary, and exclusions above.

## Recommended production-authorization prompt

> Implement Goal 7C under
> `docs/GOAL-7C-FROM-HARNESS-TO-GAME.md` on
> `codex/goal-7c-from-harness-to-game`. Preserve the six approved bounded
> fixture decisions and the complete retained gate, then stop with all three
> player UAT journeys pending. Do not begin Goal 7D, Slice 8, power-progression
> implementation, inventory, Areas, base building, or any excluded framework.

## Stop condition

Stop when the complete retained and Goal 7C automated gate is green and all
three isolated journeys are ready for player UAT. Reconcile this Status, the
Roadmap, and Handoff, then stop with player UAT pending. Do not commit, push,
merge, begin Goal 7D, or enter any excluded framework without explicit
follow-up authority.
