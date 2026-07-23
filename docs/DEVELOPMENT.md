# Development

The game project lives at `src/Chronicle.Godot`; its deterministic rules live
at `src/Chronicle.Core`. Local tool installations and generated state are
ignored under `.tools/` and `.godot/`.

This guide describes the strict v9 Goal 7B runtime: retained combat and
Home-power loops plus consequential Agents, owned personal places, bounded
Directives, response memory, and visible-cell inspection. Literal v8 and older shapes remain migration inputs only; their
`Fly[Stone]`/`Fly[Bell]` journeys are retained historical proof rather than a
parallel gameplay path. Follow the [active Handoff](HANDOFF.md) for the current
authorization boundary.

## Local tools

Gate 3B is verified with .NET SDK 8.0.423 and the official Godot 4.7.1 stable
Mono package. This workspace installs them at:

```text
.tools/dotnet/dotnet.exe
.tools/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe
.tools/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe
```

For normal editor work, close any existing Godot process and launch from the
repository root with:

```powershell
& .\open-editor.ps1
```

The launcher gives Godot the packaged SDK environment. A machine-wide .NET SDK
is not required. Opening the Godot executable directly does not provide that
environment and causes the C# editor build callback to report `.NET Sdk not
found`.

P-GEN is owned in this repository at `tools/P-GEN`. Launch its authoring-only
workbench from the repository root with:

```powershell
& .\pgen-workbench.ps1
```

The root `checks/verify.ps1` first runs P-GEN's compiler, conformance, preview,
and workbench proof, then runs Palimpsest's runtime, packaging, player,
Inspector, save, and retained-journey gate. This is the one publication check;
the production solution still has no P-GEN project reference.

## Play launcher and save profiles

Build and launch the game from the repository root with:

```powershell
& .\play.ps1
```

That command uses the normal Godot `user://` save and the packaged P-GEN
20-pixel visual pack. The launcher supplies the packaged .NET environment, builds the
current C# project, launches Godot, and restores the calling shell's
environment afterward.

Use a named profile to keep a custom Chronicle isolated under
`.tools/play-profiles/<name>/`:

```powershell
& .\play.ps1 -Profile goal7a-welcome-uat
```

The first launch of a profile starts without a save. Later launches with the
same name continue that profile. Profile names may contain letters, numbers,
dots, underscores, and hyphens.

The two Goal 7A review profiles have bounded first-launch preparation:

```powershell
& .\play.ps1 -Profile goal7a-welcome-uat
& .\play.ps1 -Profile goal7a-replacement-uat
```

The first creates Tamar at the accepted post-Resonator approach boundary. The
second creates the continuing Guest state with the first Incarnation already
ended away from Home. Preparation runs only when that profile has no save;
relaunching restores the same Chronicle for persistence UAT.

The two Goal 7B review profiles prepare the same accepted Guest/world facts,
inject Suggest and Command only into the isolated test Codex, and attune one:

```powershell
& .\play.ps1 -Profile goal7b-suggest-uat
& .\play.ps1 -Profile goal7b-command-uat
```

Both start three traversable cells north of Home so `I`/mouse inspection can
select Tamar remotely before the player moves south into physical delivery
reach. Preparation runs only when the named profile has no save; relaunching
restores pending or resolved Directive state normally.

For a guaranteed new Chronicle, let the launcher create a unique profile:

```powershell
& .\play.ps1 -Fresh
```

The launcher prints the generated profile name and the exact command needed to
resume it. `-Fresh` never deletes or overwrites an existing Chronicle. Use
`-CellSize 16` only for the retained comparison; 20 remains the default.

For direct command-line work, start a PowerShell session at the repository root
with:

```powershell
$env:DOTNET_ROOT = "$PWD\.tools\dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = "$PWD\.tools\dotnet-cli"
$env:NUGET_PACKAGES = "$env:DOTNET_CLI_HOME\.nuget\packages"
$defaultAppData = $env:APPDATA
$defaultLocalAppData = $env:LOCALAPPDATA
$dotnet = "$env:DOTNET_ROOT\dotnet.exe"
$godot = "$PWD\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe"
$godotGui = "$PWD\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe"
```

## Build and automated proof

Run the complete fail-fast gate from the repository root:

```powershell
& .\checks\verify.ps1
```

It verifies P-GEN first, then restores and builds Core, VisualPack, Visuals,
and Godot. Core checks cover strict v9, literal migration, retained combat and
Home-power rules, deterministic Agent generation/promotion, scale, welcome,
Directive force/response/memory, death/replacement, and replay. Visual checks
cover combat, material, Agent, road-roll, inspection selection, and Directive
emphasis. The gate validates the exact four-file
package, packaged/manual Inspector parity, clean startup/restart, retained Goal
6A/6B/7A Godot journeys, and Goal 7B's eight `1600 × 900` captures including
keyboard/mouse inspection and focused-Space safety. All Godot launches use temporary isolated application-data
directories, so verification neither opens a window nor touches an interactive
save.

E5 also exercises the explicit manual 20-pixel golden comparison in both
player and Inspector. To launch that comparison interactively without changing
the normal default, use:

```powershell
& .\play.ps1 -Fresh -ManualVisualPack
```

The build output must contain only the canonical four-file P-GEN bundle under
`visual-packs/palimpsest20`; compiler assemblies, catalogues, fixtures, and
review evidence are forbidden from the shipped dependency graph.

For player Goal 6A review, use the isolated profiles named in the active
contract: `goal6a-quickly-uat` and `goal6a-lasting-return-uat`.

The earlier standalone 4A automated gate ended with:

```text
PASS: Goal 4A Core and Godot Study choice, partial/completed restarts, every Goal 2 regression, Gate 3A Inspector, and Gate 3B visual acceptance verified.
```

Goal 4B adds an isolated two-process Home gate. The initial process drives the
fresh seed-`41337` controls through `HERE`, the flooded-Stone rejection at
`surface (0, 0)`, founding at soil-supported Stone on `surface (0, 3)`, and
ordinary departure, then writes the current strict canonical save. The
standalone historical 4B gate required envelope v3; the retained verifier now
reads envelope v5 with explicit Home and Bell Address before a second
process restores it, follows the physical Return Route home, and proves the
exact Home and Hearthstone facts. Core checks retain literal
v3/v2/v1/pre-envelope migration and death/replacement coverage.

The retained standalone 4B automated gate ended with:

```text
GOAL4B ACCEPTANCE PASS home=surface:0,3 material=hearthstone route=physical view=50x36 save=3
PASS: Goal 4B Home and restart acceptance, Goal 4A Study choice, every Goal 2 regression, Gate 3A Inspector, and Gate 3B visual acceptance verified.
```

That line made the dedicated 4B UAT runnable; it did not replace player
acceptance. The functional journey passed on 2026-07-20. A later-noticed
Clock/Codex overlap was corrected by rendering Tick and Clock on one line and
asserting that the header remains a fitting four-line readout. The focused
player visual recheck passed and 4B was accepted on 2026-07-20. The complete
verifier passed again after this correction with zero errors. Its only warning
was `NU1900` because the NuGet vulnerability feed was unreachable.

Goal 4C adds four isolated application phases around the same strict save:
paused threat plus pending `Smash`, successful next-tick resolution, restart
and Shattered Cairn revisit, and the no-action death/replacement branch. The
verifier now inspects the threatened and resolved v5 envelopes between
processes, retains old grammar pins, and proves the 20-pixel opening, conflict
readout, Loadout controls, static danger emphasis, and material consequence
through the real Godot shell.

Slice 5 adds one fresh and one restarted Godot process. The first exposes the
catalogue-derived meanings for `Fly[Stone]` and `Fly[Bell]`, learns Bell, uses
the shared fitted-Fly targeting path, moves the Bell and its Study Source to
the matching surface address, and writes strict save v5. The second restores
that exact branch, proves the old sky address is empty, and confirms the moved
Bell retains its consequential death affordance.

The retained predecessor gate ended with:

```text
SLICE5 CORE ACCEPTANCE PASS expression=Fly[Bell] durable=Bell+source save=5 migration=4
SLICE5 SAVE READY bell=surface:0,-4 loadout=Fly[Bell] save=5
SLICE5 RESTART ACCEPTANCE PASS bell=surface:0,-4 source=attached death=confirmed
PASS: Slice 5 Fly[Bell] composition and restart, Goal 4C conflict, Goal 4B Home, Goal 4A Study choice, Goal 2, Gate 3A, and Gate 3B verified.
```

That predecessor gate passed on 2026-07-20 with zero build warnings or errors. The player
then reported “Full UAT accept”; the
[accepted Slice 5 UAT](archive/uat/SLICE-5-UAT.md) is archived. Goal 4C player
UAT had already passed; its archived sheet retains the closer-zoom and
Cairn-legibility notes.

The current Goal 6A gate ends with:

```text
PASS: Goal 6A real fight, strict v6 migration, rendered map-first HUD, P-GEN v2 packaging, Inspector, and retained migration/generation/composer proof verified.
```

To run the individual proof steps instead, restore once, then build and run the
engine-independent check:

```powershell
& $dotnet restore --configfile NuGet.Config checks\Chronicle.Core.Checks\Chronicle.Core.Checks.csproj
& $dotnet build --no-restore src\Chronicle.Core\Chronicle.Core.csproj
& $dotnet run --no-restore --project checks\Chronicle.Core.Checks\Chronicle.Core.Checks.csproj
& $dotnet restore --configfile NuGet.Config checks\Chronicle.Visuals.Checks\Chronicle.Visuals.Checks.csproj
& $dotnet build --no-restore checks\Chronicle.Visuals.Checks\Chronicle.Visuals.Checks.csproj
& $dotnet run --no-build --no-restore --project checks\Chronicle.Visuals.Checks\Chronicle.Visuals.Checks.csproj
```

The dependency-free executable proves the strict v6 successor: generated Mire
Brute and basalt subjects, one bounded Burn Expression, shared Load, Engagement
Plan, pending commands, deterministic pursuit and Heartbeat order, HP and
equipment, Preparation/interruption/Recovery, contextual Target revalidation,
scorch, death/replacement, save/load, and deterministic replay. Literal v5
through v1 and pre-envelope fixtures prove migration into neutral retained
durables while Noun knowledge and predecessor gameplay state retire. Separate
root-gate generation and Inspector checks retain World Grammar version pins,
deterministic ordered bounded snapshots, fixture-seed composition, overlap and
adjacency context, durable overlays, read-only generation, and shared-composer
replay; those are not additional Goal 6A Core-check groups.

Goal 4A adds stable catalogue `WordId` values shared by the Catalogue, Codex,
word-specific Understanding, Study offers, and Loadout. Its Core proof covers
the exact `Fly`/`Stone`/`Bell` catalogue, the ordered two-offer generated Bell
source, source qualities and contextual reasons, deliberate selection and
switching, rejected choices, pause/leave/death/replacement behavior, independent
Stone and Bell completion, immutable public snapshots, strict version-2 JSON,
string Loadout identities, explicit numeric predecessor migration, and unchanged
version-0/version-1 physical World semantics.

Goal 4B adds singular Home, Hearthstone material identity, site eligibility,
the derived physical Return Route, strict save v3, and predecessor proof. Goal
4C adds authored `Smash`, grammar-v3 Riven Cairn selection, the one-exchange
Core conflict state, pause/Slow/Normal/Fast resolution rules, all-opening world
independence, non-overlap invariants, strict save v4, and literal v3 grammar
pins `0`, `1`, and `2`. Its save proof also covers the resumed-before-next-tick
window, schema-appropriate predecessor Word identities, fixed loose-Stone X/Y
provenance, Home-safe `Fly[Stone]` targeting, guard-before-mutation at exhausted
tick/Incarnation counters without application exceptions, and rejection of
malformed current state.

Slice 5 replaces the pair-specific fitted-Fly seam with one Verb dispatch and
authored Noun-subject resolution. Its Core proof covers retained intrinsic
actions and `Fly[Stone]`, new `Fly[Bell]`, Bell/source relocation, old-address
suppression, moved-Bell death, strict save v5, literal v4 migration, rejection
without mutation, and predecessor rejection of forged `Fly[Bell]` state.

The Visuals executable proves both native pack sizes, stable identifiers and
pack digests, palette/atlas bounds, all connected-feature masks, exact
dry-ridge-to-water-crossing edge pixels, deterministic address-derived
variants, mapping and layer order, one-cell-halo crop, overlap agreement, and
repeatable render-plan digests without referencing Godot. It also composes the
minimum and maximum representable absolute addresses without wrapping a
cardinal neighbor or treating the numeric storage limit as authored terrain.
It additionally proves intact/shattered Cairn identity mapping and static,
Core-fed danger emphasis without mutating semantic input.

## Developer World Atlas Inspector

With the packaged environment above set, launch the separate read-only
inspector from the repository root:

```powershell
& $godotGui --path src\Chronicle.Godot --scene res://WorldAtlasInspector.tscn
```

It opens at seed `41337`, Surface, origin, and a bounded 1024 × 1024-address
overview. The right panel provides fixture and numeric seed selection,
Surface/Sky selection, origin/Incarnation/Bell recentering, four-way pan,
overview-to-local zoom, semantic/address/request-bound/motif/durable-identity
diagnostics, a `VISUAL GRAMMAR PREVIEW` toggle, and capture/export. Semantic
mode retains the Gate 3A debug projection. The default 1024 × 1024 overview
stays semantic: Visual Grammar preview is disabled above a 64 × 64 request.
Zoom to 64 × 64 or 32 × 32 before enabling it. At those bounded local requests,
visual mode composes the selected 16 px or 20 px pack through the same pure
render-plan path as the player. Every action issues another bounded
`Chronicle.Core` area request or changes presentation state; it never loads,
creates, advances, or saves the player Chronicle.

With `absolute addresses` enabled, hover the raster to read the exact World
Address, semantic ground, feature, motif, and durable identity for that cell.
The guide lines are aligned to absolute coordinates, so they remain stable
across overlapping requests.

`CAPTURE PNG + JSON` writes a deterministic pair under:

```text
.tools/atlas-captures/
```

The filename contains the seed, grammar version, Stratum, bounds, zoom, and
overlay bits; visual captures also include the native cell size. The JSON
sidecar records pack and plan digests when visual mode is active. Repeating the
same inspector state overwrites the same pair rather than creating a
timestamped artifact. Gate 3B's local review pairs are written under
`.tools/gate3b-review/` and are indexed in the
[visual UAT sheet](GATE-3B-VISUAL-UAT.md).

Run only the packaged headless inspector acceptance with:

```powershell
& $godot --headless --path src\Chronicle.Godot --scene res://WorldAtlasInspector.tscn -- --verify-world-atlas
```

It must print `GATE3A ATLAS ACCEPTANCE PASS`, rasterize a 1024 × 1024 Core
snapshot through a small fixed Node tree, exercise the controls and capture,
and exit without creating a player save. The complete verifier runs this path
again beside a sentinel player save and proves its bytes and timestamp remain
unchanged; both equivalent runs must reproduce identical capture bytes.

Run the shared Gate 3B visual-preview acceptance at either candidate size with:

```powershell
& $godot --headless --path src\Chronicle.Godot --scene res://WorldAtlasInspector.tscn -- --verify-gate3b-atlas --visual-cell-size=20
& $godot --headless --path src\Chronicle.Godot --scene res://WorldAtlasInspector.tscn -- --verify-gate3b-atlas --visual-cell-size=16
```

Each run must additionally print `GATE3B SHARED COMPOSER PLAN PASS` and
`GATE3B ATLAS VISUAL PREVIEW PASS size=<size>`.

Close the inspector normally. If a repository-scoped Godot process remains
after an interrupted launch, stop only processes whose command line names this
project:

```powershell
Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -like 'Godot*' -and
        $_.CommandLine -like "*$PWD\src\Chronicle.Godot*"
    } |
    ForEach-Object { Stop-Process -Id $_.ProcessId }
```

Build the Godot C# project and run the real main scene headlessly:

```powershell
& $dotnet restore --configfile NuGet.Config src\Chronicle.Godot\Chronicle.Godot.csproj
& $dotnet build --no-restore src\Chronicle.Godot\Chronicle.Godot.csproj
$env:APPDATA = (New-Item -ItemType Directory -Force "$PWD\.tools\godot-runtime\Roaming").FullName
$env:LOCALAPPDATA = (New-Item -ItemType Directory -Force "$PWD\.tools\godot-runtime\Local").FullName
& $godot --headless --path src\Chronicle.Godot --quit-after 2
$env:APPDATA = $defaultAppData
$env:LOCALAPPDATA = $defaultLocalAppData
```

The headless command must print `SLICE2C READY` and no scene, script, or resource
errors. The isolated application-data paths keep automated checks from touching
the interactive save or another Godot session.

With those isolated paths still active, run either focused player visual
acceptance with:

```powershell
& $godot --headless --path src\Chronicle.Godot -- --verify-gate3b-player --visual-cell-size=20
& $godot --headless --path src\Chronicle.Godot -- --verify-gate3b-player --visual-cell-size=16
```

They must report `GATE3B PLAYER VISUAL ACCEPTANCE PASS` with densities `51x37`
and `63x45` respectively, followed by `SLICE2C ACCEPTANCE PASS`. Prefer the
full verifier for routine use because it creates and removes unique isolated
runtime roots automatically.

## Launch and persistence proof

Use the canonical launcher with the normal interactive save:

```powershell
& .\play.ps1
```

Gate 3B's accepted baseline is the default 20 px pack. Launch the retained
comparison explicitly with:

```powershell
& .\play.ps1 -CellSize 20
& .\play.ps1 -CellSize 16
```

The corrected Goal 6A canvas is `1600 × 900`. Its map owns the complete
`1280 × 900` left surface and presents the native 20-pixel pack at crisp `2×`
scale through a centered `32 × 23` query. A 64-pixel status/pause treatment
and compact six-action plus WASD palette overlay that map; they do not remove a
permanent bottom strip. The 320-pixel right rail gives the non-overlapping
Target/HP and decision block, four-event forecast, and enlarged Message Log
their own hierarchy. The retained visual comparison remains documented in
[Gate 3B Visual UAT](GATE-3B-VISUAL-UAT.md).

Choose a Starting Vector with `1`/`2`/`3`. Use WASD to move, `I` or a map click
to inspect visible cells, WASD/Enter/Escape to operate or close that cursor, `T` to cycle
contextual Targets, `Q`/`L` to attune the two Burn plans, `B` to prepare Burn,
`V` for the Cleaver stance or strike, Space or `1` to pause/resume Slow
Heartbeats, `C` to cancel or safely skip Recovery, F5/F9 to save/load, and `R`
for a replacement Incarnation after death. Mouse buttons issue the same Core
commands. The rails expose the next four events, timing, interruption risk,
prevented actions, Target facts, HP, equipment, Load, Links, and recent results.

Completed player journeys are historical evidence, not launch instructions.
They are preserved in the archived
[Goal 2 contract](archive/contracts/GOAL-2-A-WORD-KEPT.md),
[Goal 4A UAT sheet](archive/uat/GOAL-4A-UAT.md), and
[Goal 4B UAT sheet](archive/uat/GOAL-4B-UAT.md), and
[Goal 4C UAT sheet](archive/uat/GOAL-4C-UAT.md), and
[Slice 5 UAT](archive/uat/SLICE-5-UAT.md).

Godot keeps the compatible file at `user://slice0_chronicle.json`. Strict v9
saves use stable successor Word and Agent identities and contain the current
body, Codex/Loadout, combat and Home-power state, consequential Agents, their
relationships, owned personal places, pending Directives, and ordered response
memories. Literal v8 through v1 and pre-envelope files decode through
predecessor shapes before constructing v9;
their fitted Nouns, Study/Understanding, and predecessor conflict state do not
survive as a parallel gameplay system. Generated snapshots are reconstructed
from seed, pinned World Grammar, durable identity, and World Address.

To open the editor instead:

```powershell
& .\open-editor.ps1
```

## Project boundary

- `Chronicle.Core` owns generation, commands, movement, clock semantics, the
  authored Word Catalogue, canonical Codex, bounded Verb/Modifier Loadout,
  contextual Target validity, the Goal 6A encounter and forecasts, Incarnation
  death and replacement, retained material durables, Landmark identity, and
  Agent grammar and relationships, Directive admissibility/response/memory,
  strict canonical persistence, and migration
  without referencing Godot.
- `Chronicle.VisualPack` owns immutable compiled-pack values and the compact
  manually authored 16 px/20 px reference pack without referencing Core or
  Godot.
- `Chronicle.Visuals` maps read-only Core snapshots plus pack/style inputs to
  deterministic transient render plans without referencing Godot.
- `Chronicle.Godot` translates input and wall-clock pulses, renders Core
  snapshots through those plans, expands atlas textures, presents the map-first
  status/Target/Agent/Directive/inspection rails, Target highlights, death and replacement controls,
  Starting Vector and bounded Loadout controls, and owns the `user://` file
  lifecycle.
- Godot is an inspection and presentation surface for generated worlds, not a
  hand-authored level source.
