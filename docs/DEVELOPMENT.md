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
It then runs the player acceptance at `20 px / 33 × 23` and
`16 px / 41 × 29`. Each player launch drives the real Godot buttons through the
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

The Visuals executable proves both native pack sizes, stable identifiers and
pack digests, palette/atlas bounds, all connected-feature masks, exact
dry-ridge-to-water-crossing edge pixels, deterministic address-derived
variants, mapping and layer order, one-cell-halo crop, overlap agreement, and
repeatable render-plan digests without referencing Godot. It also composes the
minimum and maximum representable absolute addresses without wrapping a
cardinal neighbor or treating the numeric storage limit as authored terrain.

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

They must report `GATE3B PLAYER VISUAL ACCEPTANCE PASS` with densities `33x23`
and `41x29` respectively, followed by `SLICE2C ACCEPTANCE PASS`. Prefer the
full verifier for routine use because it creates and removes unique isolated
runtime roots automatically.

## Launch and persistence proof

Launch the game directly:

```powershell
& $godotGui --path src\Chronicle.Godot
```

Gate 3B's accepted baseline is the default 20 px pack. Launch the retained
comparison explicitly with:

```powershell
& $godotGui --path src\Chronicle.Godot -- --visual-cell-size=20
& $godotGui --path src\Chronicle.Godot -- --visual-cell-size=16
```

The runtime canvas is 1280×720. A native 33×23 generated playspace at 20 px or
41×29 playspace at 16 px fills the left side, while the Chronicle, Codex,
Loadout, commands, and status occupy the right-hand panel without overlapping.
The UAT decision and annotated fixture comparison are in
[Gate 3B Visual UAT](GATE-3B-VISUAL-UAT.md); do not remove either candidate
until that gate selects one.

Use arrow keys or WASD to move, Space to pause, `1`/`2`/`3` for slow/normal/fast,
F5 to save, and F9 to load. Fly occupies the first slot in the eight-slot
on-screen hotbar. At the Bell, the Study button starts understanding its
sky-stone clapper. The direction, clock, hotbar, Study, save, and load controls
invoke Core commands. Pause stops Chronicle ticks and Study; deliberate
commands remain available while paused. Slice 2A passed its player UAT with the
following reference journey:

1. Launch with a Slice 0 or fresh save. Confirm the modal `UP` Intent and that
   movement is unavailable.
2. Select `UP`; confirm the Codex lists `Fly`, eight hotbar slots remain, and
   `FLY UP` occupies the first slot.
3. Before reaching it, confirm the disabled button says `STUDY AT BELL` and the
   Chronicle Thread points to `sky (0, -4)` without requiring documentation.
4. Fly up. Confirm the player-centred generated sky, centred marker, and visible
   gold Bell four cells north.
5. Move north four times. Confirm the address is `sky (0, -4)` and the
   application names **The Bell That Fell Up** with its arrival line.
6. Select `STUDY SKY-STONE`. Confirm the interface names the clapper rather
   than revealing its result, shows four progress segments, and keeps the Noun
   list empty until understanding completes.
7. Pause and wait. Confirm neither Tick nor Study progress advances.
8. Resume normal speed, save before completion, move away, and load. Confirm
   the exact partial progress and active Study return.
9. Remain at the Bell until 16 Chronicle ticks of understanding complete.
   Confirm `Nouns: Stone` and four filled segments marked `KEPT`.
10. Select `STUDY AGAIN`. Confirm the Chronicle says it already keeps `Stone`
   and the Codex does not change.
11. Close and relaunch. Confirm `Fly`, `Stone`, completed Study, address, speed,
   and the generated view restore.
12. Move south four times and use `FLY DOWN`. Confirm the address is
   `surface (0, 0)` and Fly remains granted.
13. Move east, fly up, and fly down. Confirm both transitions preserve
   coordinate `(1, 0)`.

For the accepted Slice 2B reference journey:

1. Load the completed Slice 2A Chronicle. Confirm the Codex still owns `Fly`
   and `Stone`, while `LOADOUT SLOT 1` separately shows active `FLY`.
2. Select `CLEAR SLOT 1`. Confirm the Codex remains unchanged, hotbar slot one
   becomes empty, and self-flight is unavailable.
3. Select `EQUIP FLY`. Confirm slot one and the hotbar restore intrinsic Fly.
4. Select `FIT STONE`. Confirm slot one becomes `FLY[STONE]` and self-flight is
   no longer available.
5. Move to `surface (0, 0)`, immediately west of the visibly distinct loose
   Stone at `surface (1, 0)`.
6. Select the fitted hotbar slot. Confirm the loose Stone receives a gold
   highlight and the Chronicle Thread asks for a cardinal target.
7. Select east. Confirm the Incarnation stays at `surface (0, 0)` while the
   loose Stone moves to `sky (1, 0)`.
8. Restore intrinsic `FLY`, fly to `sky (0, 0)`, and confirm the same loose
   Stone is visible one cell east.
9. Fit `STONE` again and target east. Confirm the Stone returns to
   `surface (1, 0)` while the Incarnation remains in the sky.
10. Save with a fitted Loadout and moved Stone, perturb both, then load. Confirm
    the Loadout, player address, Stone address, Codex, and Study restore exactly.

For the current Slice 2C player UAT:

1. Begin with Incarnation `1`, learned `Fly` and `Stone`, and the loose Stone
   moved to `sky (1, 0)`. Save.
2. Equip intrinsic `Fly`, reach The Bell That Fell Up at `sky (0, -4)`, and fit
   `Stone` so the active slot reads `FLY[STONE]`.
3. Select `END THIS BODY`. Confirm that the body remains alive and the control
   changes to `CONFIRM DEATH`.
4. Select `CONFIRM DEATH`. Confirm the replacement screen says the body ended,
   time is not advancing, and the Codex and changed Chronicle remain.
5. Wait. Confirm the displayed Tick does not advance. Save and load on this
   screen; confirm the same awaiting state returns.
6. Select `CREATE REPLACEMENT INCARNATION`. Confirm Incarnation `2` appears at
   `surface (0, 0)` with exactly eight empty Loadout slots.
7. Confirm `Fly`, `Stone`, completed Understanding, seed, tick, and speed remain
   unchanged, and the loose Stone is still at `sky (1, 0)`.
8. Equip intrinsic `Fly`, enter the sky, and observe the first body's moved
   Stone one cell east.
9. Save, close the application, relaunch it, and confirm Incarnation `2`, its
   fitted intrinsic `Fly`, the Codex, Understanding, clock, address, and moved
   Stone restore exactly.

Godot keeps the compatible file at `user://slice0_chronicle.json`. A Slice 0
file without Intent opens as `Unchosen`; a Slice 1 file with `UP` migrates to
an explicit Codex containing `Fly`; a Slice 2A file gains intrinsic Fly in
slot one and the loose Stone at `surface (1, 0)`; a Slice 2B file gains living
Incarnation identity `1` without changing its Loadout or Stone delta. New saves
use a minimal versioned envelope around inspectable Core state and durable
deltas. Surface and sky tiles are regenerated from the seed.

To open the editor instead:

```powershell
& .\open-editor.ps1
```

## Project boundary

- `Chronicle.Core` owns generation, commands, movement, clock semantics, Codex,
  Study rules and progress, the eight-slot Loadout, Expression compatibility,
  target validity, Fly effects, Incarnation death and replacement, the loose
  Stone delta, Landmark identity, and serialization without referencing Godot.
- `Chronicle.VisualPack` owns immutable compiled-pack values and the compact
  manually authored 16 px/20 px reference pack without referencing Core or
  Godot.
- `Chronicle.Visuals` maps read-only Core snapshots plus pack/style inputs to
  deterministic transient render plans without referencing Godot.
- `Chronicle.Godot` translates input and wall-clock pulses, renders Core
  snapshots through those plans, expands atlas textures, renders Core-valid
  target highlights and UI glyphs, presents death confirmation and replacement
  controls, and owns the `user://` file lifecycle.
- Godot is an inspection and presentation surface for generated worlds, not a
  hand-authored level source.
