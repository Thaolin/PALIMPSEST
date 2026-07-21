# P-GEN E5 UAT — The Authored World Enters the Game

**Prepared:** 2026-07-21
**Status:** First visual candidate rejected on 2026-07-21; superseded by the E5.1 correction

## What changed

P-GEN's compiled 20-pixel artifact is now the normal player and Inspector
visual pack. Chronicle rules, World Grammar, saves, map density, and semantic
mapping did not change. The manual 20-pixel pack remains available only as an
explicit golden comparison; the manual 16-pixel reference remains retained.

The complete verifier passed with zero build warnings or errors. It proved the
strict reader, all current visual IDs, P-GEN/manual semantic composition parity,
deterministic captures, exact four-file packaging, absence of compiler and
catalogue material, player-save non-mutation, and every retained v5 journey.

## Native 20-pixel comparison

The pictures below are native output, not resized concept art. The primary
subjective difference is deliberate and visible: P-GEN uses stronger connected
lattice/tiling marks, while the manual golden pack uses broader solid fields
and more pictorial individual marks.

### Player sky fixture

| P-GEN default | Manual golden comparison |
| --- | --- |
| ![P-GEN player sky](../.tools/gate3b-review/player_s41337_sky_bell_stone_20px_pgen.png) | ![Manual player sky](../.tools/gate3b-review/player_s41337_sky_bell_stone_20px_manual.png) |

### Surface seed 41337

| P-GEN default | Manual golden comparison |
| --- | --- |
| ![P-GEN surface seed 41337](../.tools/gate3b-review/surface_s41337_20px_pgen.png) | ![Manual surface seed 41337](../.tools/gate3b-review/surface_s41337_20px_manual.png) |

Additional deterministic Surface comparisons are generated for seeds `41338`
and `90421` under `.tools/gate3b-review/` with the same `_pgen` and `_manual`
suffixes.

## Interactive journey

Launch a fresh isolated P-GEN Chronicle:

```powershell
& .\play.ps1 -Fresh
```

For a direct 20-pixel golden comparison:

```powershell
& .\play.ps1 -Fresh -ManualVisualPack
```

In the default P-GEN launch:

1. compare grass, soil, water, groves, ridges, and crossings while moving;
2. choose `UP`, reach the Bell, and check Bell/Incarnation separation;
3. learn and move the loose Stone, then check all three silhouettes together;
4. optionally choose `HERE` or `AGAINST` in another fresh profile to inspect
   the Hearthstone or intact/shattered Cairn and River Ward danger mark; and
5. confirm that the accepted `20 px / 51 × 37` density still feels readable.

## Decision requested

Report one of:

- **Accept:** P-GEN becomes the accepted default and E5 closes.
- **Narrow:** keep the integration but name a specific visual blocker to fix
  inside E5 before acceptance.
- **Reject:** restore the manual pack as default while retaining the proven
  reader/packaging seam for later art revision.

Do not judge successor combat or grammar here; E5 changes authored visual data
only. Goal 6A remains a separate later authorization.

## Recorded result

The player rejected this first candidate: terrain materials read as one
connected wall lattice and important sprites were too small inside their
cells. The reader and packaging proof remain accepted evidence. Corrected
visual UAT is governed by
[P-GEN E5.1 — Materials Read as Materials](P-GEN-E5-1-VISUAL-AUTHORING-SPIKE.md).
