# Gate 3B Visual UAT

## Status

**Accepted on 2026-07-19 — 20 px / 33 × 23 selected.**

This review selected one native cell size and judged the authored Visual
Grammar. It did not reopen the accepted Gate 3A World Grammar. Gate 3B and
Slice 3 are complete.

## Candidate choice

| Candidate | Local density at 1280 × 720 | Strongest quality | Trade-off |
| --- | ---: | --- | --- |
| 20 px | 33 × 23 cells | Incarnation, Bell, loose Stone, crossings, and feature silhouettes read most clearly | Shows less territory |
| 16 px | 41 × 29 cells | Strongest sense of surrounding territory | Actor and subject silhouettes lose too much visual weight |

**Accepted baseline: 20 px.** It meets the density floor exactly and preserves
the hierarchy required by the Gate 3B journey. The 16 px candidate remains a
comparison artifact and diagnostic launch option, not the player-view
baseline.

## Four-image review sheet

The verifier regenerates every PNG and JSON sidecar under
`.tools/gate3b-review/`. The final `& .\checks\verify.ps1` run passed on
2026-07-19 and proved that equivalent candidate runs reproduce the same bytes.

### Seed 41337 — Surface

| 16 px / 41 × 29 | 20 px / 33 × 23 — selected |
| --- | --- |
| ![Seed 41337 Surface at 16 px](../.tools/gate3b-review/surface_s41337_16px.png) | ![Seed 41337 Surface at 20 px](../.tools/gate3b-review/surface_s41337_20px.png) |

- Reads immediately: connected water, grass field, dark grove, crag ridge,
  low-stone water crossing, red/cream Incarnation, and pale loose Stone.
- Visual noise: repeated crags remain the busiest family; ground texture stays
  deliberately quiet.
- Connected forms: water and grove boundaries join across the crop; the ridge
  changes to low shoals over water and rejoins the dry crags at matching edge
  pixels.
- Hierarchy: the Incarnation and loose Stone remain readable at the crossing.

### Seed 41338 — Surface

| 16 px / 41 × 29 | 20 px / 33 × 23 — selected |
| --- | --- |
| ![Seed 41338 Surface at 16 px](../.tools/gate3b-review/surface_s41338_16px.png) | ![Seed 41338 Surface at 20 px](../.tools/gate3b-review/surface_s41338_20px.png) |

- Reads immediately: a different soil/grass balance, watercourse orientation,
  grove placement, and ridge path using the same visual language.
- Visual noise: the broad diagonal ridge still dominates because that is the
  accepted semantic form, not cosmetic variation.
- Connected forms: the shoal treatment keeps water visible instead of placing
  mountain silhouettes in it.
- Hierarchy: actor and loose Stone stay distinct against grass near the
  crossing.

### Seed 90421 — Surface

| 16 px / 41 × 29 | 20 px / 33 × 23 — selected |
| --- | --- |
| ![Seed 90421 Surface at 16 px](../.tools/gate3b-review/surface_s90421_16px.png) | ![Seed 90421 Surface at 20 px](../.tools/gate3b-review/surface_s90421_20px.png) |

- Reads immediately: wide clearing, soil boundary, long crag ridge, actor, and
  loose Stone.
- Visual noise: this fixture is intentionally quieter and exposes the dry
  ridge repetition most clearly.
- Connected forms: the ridge remains continuous beyond the crop and its water
  interaction is visible at the upper edge.
- Hierarchy: the red/cream Incarnation and pale Stone dominate the restrained
  ground field.

### Seed 41337 — Sky, Bell, and moved Stone

| 16 px / 41 × 29 | 20 px / 33 × 23 — selected |
| --- | --- |
| ![Seed 41337 Sky at 16 px](../.tools/gate3b-review/player_s41337_sky_bell_stone_16px.png) | ![Seed 41337 Sky at 20 px](../.tools/gate3b-review/player_s41337_sky_bell_stone_20px.png) |

- Reads immediately: connected pale cloud bank, open dark sky, gold Bell,
  red/cream Incarnation, and pale moved Stone.
- Visual noise: open-sky texture is subordinate; the stepped cloud edge
  exposes the one-address grid without filling every cell with a symbol.
- Connected forms: the cloud bank joins without per-cell holes or viewport-edge
  changes.
- Hierarchy: the Bell and Incarnation are captured on adjacent addresses so
  both silhouettes can be judged; gold remains exclusive to the Landmark.

The same joins, layers, palette roles, and deterministic composition apply.
The comparison question is whether the additional eight columns and six rows
are worth the reduced subject and Landmark legibility.

## Interactive recheck

From the repository root, launch each player candidate:

```powershell
$godotGui = "$PWD\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe"
& $godotGui --path src\Chronicle.Godot -- --visual-cell-size=20
& $godotGui --path src\Chronicle.Godot -- --visual-cell-size=16
```

For the Inspector fixture review, launch the Inspector. It opens at a protected
1024 × 1024 semantic overview; use `ZOOM IN` until the header shows a 64 × 64
or 32 × 32 request, then enable `VISUAL GRAMMAR PREVIEW` and compare seeds
`41337`, `41338`, and `90421`. Visual preview is intentionally unavailable at
larger request sizes.

```powershell
& $godotGui --path src\Chronicle.Godot --scene res://WorldAtlasInspector.tscn -- --visual-cell-size=20
```

Accept Gate 3B only if:

1. water, grove, dry ridge, ridge/water crossing, actor, and loose Stone read
   without terrain labels;
2. newly revealed edges continue the existing forms;
3. the Bell reads before its label and stays distinct from the actor;
4. Codex, Loadout, hotbar, and contextual text do not clip;
5. one exact size is selected: `20 px / 33 × 23` or `16 px / 41 × 29`.

## Player decision

- [x] Accept `20 px / 33 × 23`.
- [ ] Accept `16 px / 41 × 29`.
- [ ] Reject both; record the specific failed read, join, hierarchy, noise, or
  layout assertion before another Gate 3B edit.

Decision and rationale: **20 px accepted on 2026-07-19.** The actor, Bell,
loose Stone, crossings, and feature hierarchy justify the stronger native
readability. A later authorized camera treatment may zoom the accepted view
out without reopening the compiled-pack baseline; Gate 3B does not add that
runtime zoom control.

## Known semantic limit

The authored pack makes the accepted semantics coherent; it cannot remove the
Gate 3A grammar's broad diagonals, periodic placement, or symmetry. Changing
those macro forms requires separately authorized World Grammar refinement,
Core determinism proof, and another Inspector evaluation. It is not hidden
inside this visual UAT.
