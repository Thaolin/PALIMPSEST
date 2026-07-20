# Development

The game project lives at `src/Chronicle.Godot`; its deterministic rules live
at `src/Chronicle.Core`. Local tool installations and generated state are
ignored under `.tools/` and `.godot/`.

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

## Play launcher and save profiles

Build and launch the game from the repository root with:

```powershell
& .\play.ps1
```

That command uses the normal Godot `user://` save and the accepted 20-pixel
visual pack. The launcher supplies the packaged .NET environment, builds the
current C# project, launches Godot, and restores the calling shell's
environment afterward.

Use a named profile to keep a custom Chronicle isolated under
`.tools/play-profiles/<name>/`:

```powershell
& .\play.ps1 -Profile goal4b-uat
```

The first launch of a profile starts without a save. Later launches with the
same name continue that profile. Profile names may contain letters, numbers,
dots, underscores, and hyphens.

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

It restores and builds Core, VisualPack, Visuals, and Godot, then runs the
dependency-free Core and Visuals checks. It starts the developer World Atlas
Inspector in isolated application-data directories at both 20 px and 16 px,
proving the semantic and shared Visual Grammar paths, deterministic captures,
and player-save non-mutation beside both an absent save and a sentinel save.
It then runs the player acceptance at the current native densities `20 px /
51 × 37` and `16 px / 63 × 45`. Each player launch drives the real Godot buttons through the
complete `UP` → Fly → Bell → Study → pause → partial save/load → learn `Stone`
→ configure/clear the Loadout → move and return the loose Stone with
`Fly[Stone]` → confirm Bell death → save/load while awaiting replacement →
create Incarnation `2` with an empty Loadout → re-equip Fly → observe the moved
Stone journey. The Visual Grammar assertions cover exact pack identity,
native density, Bell/actor/Stone layers, target emphasis, stable save/load
plan digests, and review-capture bytes. Godot's editor callback, clean startup,
save creation, and next-launch restore remain in the gate. All launches use
temporary isolated application-data directories, so verification neither
opens a window nor touches the interactive save.

Goal 4A adds an isolated current-grammar journey after those retained
regressions. One launch uses the real controls to save active `Stone` Study at
`5/16`; a separate application launch restores that exact source/word pursuit.
The next launch carries it through death, an empty replacement Loadout,
deliberate return and completion, then runs an independent `Bell` choice through
partial save/load and `16/16` completion without advancing `Stone`. A final
application launch restores the completed Bell-only Chronicle.

The earlier standalone 4A automated gate ended with:

```text
PASS: Goal 4A Core and Godot Study choice, partial/completed restarts, every Goal 2 regression, Gate 3A Inspector, and Gate 3B visual acceptance verified.
```

Goal 4B adds an isolated two-process Home gate. The initial process drives the
fresh seed-`41337` controls through `HERE`, the flooded-Stone rejection at
`surface (0, 0)`, founding at soil-supported Stone on `surface (0, 3)`, and
ordinary departure, then writes the current strict canonical save. The
standalone historical 4B gate required envelope v3; the retained verifier now
reads envelope v4 with explicit Home before a second
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
verifier inspects the threatened and resolved v4 envelopes between processes,
retains old grammar pins, and proves the 20-pixel opening, conflict readout,
Loadout controls, static danger emphasis, and material consequence through the
real Godot shell.

The current full automated gate ends with:

```text
GOAL4C CORE ACCEPTANCE PASS openings=AGAINST,UP,HERE grammar=3 cairn=surface(1,3) smash=word.smash save=4
PASS: Goal 4C conflict restart and failure acceptance, Goal 4B Home and restart acceptance, Goal 4A Study choice, every Goal 2 regression, Gate 3A Inspector, and Gate 3B visual acceptance verified.
```

That gate passed on 2026-07-20 with zero build errors. Its only warning was
`NU1900` because the NuGet vulnerability feed was unreachable. It makes the
[focused Goal 4C UAT](archive/uat/GOAL-4C-UAT.md) runnable. The player
subsequently passed that journey on 2026-07-20; the archived sheet retains the
closer-zoom and Cairn-legibility notes.

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

The dependency-free executable retains every Slice 0 and Slice 1 proof and adds
literal Slice 1 migration to Codex `Fly`, deterministic Bell Study, pause and
source rules, partial save/load, idempotent `Stone` completion, and inspectable
Codex/Study serialization without generated tiles. Slice 2B adds predecessor
migration to eight ordered Loadout slots, Codex-only configuration, duplicate
prevention, intrinsic Fly, fitted `Fly[Stone]`, Core-owned adjacent targeting,
the durable loose Stone delta, rejection messages, replay, and save/load. Its
project references Core but does not reference Godot. Slice 2C adds Bell-only
death, complete awaiting-replacement command and tick freeze, deterministic
replacement identity, an empty replacement Loadout, versioned save-envelope
migration, save/load on both sides of replacement, and lifecycle replay.
Gate 3A adds World Grammar version migration, deterministic ordered bounded
Surface/Sky snapshots, fixture-seed composition, whole-versus-tiled and overlap
agreement, query-order independence, adjacency context, named long forms,
durable Bell and moved-Stone overlays, read-only generation, save omission, and
render-independent replay.

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

The current runtime canvas is `1600 × 900` with a `1020 × 740` map. Its native
20-pixel view presents `51 × 37` cells; retained 16-pixel presentation presents
`63 × 45`. Goal 4B's automated marker deliberately keeps the lower
`view=50x36` acceptance minimum. The Chronicle, Codex, Loadout, commands, and
status occupy the right-hand panel without overlapping. The accepted 20-pixel
baseline and retained visual comparison remain documented in
[Gate 3B Visual UAT](GATE-3B-VISUAL-UAT.md).

The compact Chronicle header uses four lines. Tick and speed share
`Tick: … · Clock: …` so the Clock value remains fully above the Codex panel.

Use arrow keys or WASD to move, Space to pause, `1`/`2`/`3` for slow/normal/fast,
F5 to save, and F9 to load. Fly occupies the first slot in the eight-slot
on-screen hotbar. At the Bell, the Study button opens the generated sky-stone
source: its Core-owned situation, qualities, contribution, and ordered
`Stone`/`Bell` offers replace the old hidden-Stone action. The direction,
clock, hotbar, Study choice, save, and load controls invoke Core commands.
Pause stops Chronicle ticks and Study; deliberate commands remain available
while paused.

Completed player journeys are historical evidence, not launch instructions.
They are preserved in the archived
[Goal 2 contract](archive/contracts/GOAL-2-A-WORD-KEPT.md),
[Goal 4A UAT sheet](archive/uat/GOAL-4A-UAT.md), and
[Goal 4B UAT sheet](archive/uat/GOAL-4B-UAT.md), and
[Goal 4C UAT sheet](archive/uat/GOAL-4C-UAT.md).

Godot keeps the compatible file at `user://slice0_chronicle.json`. A Slice 0
file without Intent opens as `Unchosen`; a Slice 1 file with `UP` migrates to
an explicit Codex containing `Fly`; a Slice 2A file gains intrinsic Fly in
slot one and the loose Stone at `surface (1, 0)`; a Slice 2B file gains living
Incarnation identity `1` without changing its Loadout or Stone delta. Literal
v3, v2, v1, and pre-envelope saves are decoded through predecessor shapes
before constructing the strict current v4 canonical Chronicle. Current v4
saves use stable string Word identities and contain only canonical Chronicle
state, including optional singular Home, first-conflict state, and durable
deltas. Surface, sky, Study Source, Hearthstone, Cairn, and route snapshots are
regenerated from the seed, pinned grammar, durable identity, and World Address.

To open the editor instead:

```powershell
& .\open-editor.ps1
```

## Project boundary

- `Chronicle.Core` owns generation, commands, movement, clock semantics, the
  authored Word Catalogue, generated Study Sources, canonical Codex and
  word-specific Understanding, the eight-slot Loadout, Expression
  compatibility, target validity, Fly effects, Incarnation death and
  replacement, singular Home and its physical Return Route, the loose Stone
  delta, Landmark identity, and strict canonical persistence without referencing
  Godot.
- `Chronicle.VisualPack` owns immutable compiled-pack values and the compact
  manually authored 16 px/20 px reference pack without referencing Core or
  Godot.
- `Chronicle.Visuals` maps read-only Core snapshots plus pack/style inputs to
  deterministic transient render plans without referencing Godot.
- `Chronicle.Godot` translates input and wall-clock pulses, renders Core
  snapshots through those plans, expands atlas textures, renders Core-valid
  target highlights and UI glyphs, presents death confirmation and replacement
  controls, presents nonbinding Starting Vector and mixed-Codex Loadout controls,
  and owns the `user://` file lifecycle.
- Godot is an inspection and presentation surface for generated worlds, not a
  hand-authored level source.
