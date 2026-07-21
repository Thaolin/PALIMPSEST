# P-GEN E5.1 UAT — Materials Read as Materials

**Prepared:** 2026-07-21  
**Status:** Accepted 2026-07-21

## Corrected evidence

The refreshed P-GEN pack contains 249 definitions with canonical aggregate
`sha256:85418f3025f2944d2f58a0a981febb00903bf67edcc23cb84054b3fd9f91eae0`.
P-GEN and Palimpsest's complete retained verifier both pass. The shipped game
still contains exactly the four compiled pack files and no compiler, source
catalogue, workbench, concept art, or biome brief.

The generated surface now uses water fields and shore boundaries, tree and
mountain silhouettes, and nonconnecting rocky fords where ridges cross water:

![Corrected P-GEN surface](../.tools/gate3b-review/surface_s41337_20px_pgen.png)

The sky now renders one continuous cloud bank rather than rows of repeated
connected cloud icons:

![Corrected P-GEN sky](../.tools/gate3b-review/player_s41337_sky_bell_stone_20px_pgen.png)

These captures remain deterministic fixture windows, not approval by proxy.

## Authoring workbench

The refreshed P-GEN workbench is currently open. To reopen it later:

```powershell
Set-Location C:\DEV\PALIMPSEST
.\pgen-workbench.ps1
```

Review:

1. **Asset Lab:** filter and inspect the Incarnation, Bell, Hearthstone,
   Cairns, Stone, trees, mountains, water, crossings, and clouds at native,
   2×, and 4× scale.
2. **Material Matrix:** compare masks and variants. Water and cloud boundaries
   may respond to adjacency; trees, mountains, and rocky fords must not become
   cardinal connector arms.
3. **Biome Board:** confirm the mixed board reads before consulting metadata.
4. Compare native `20 × 20` with the display-only `20 × 30` tall view. This is
   evidence for a later Qud-like cell-profile decision, not a stretched runtime
   proposal.
5. Use **Recompile** or `Ctrl+R` and confirm the board remains available.
6. Biome-brief export is optional. It writes only after **Save biome brief**,
   refuses overwrite, and never becomes compiler or Chronicle input.

## Player journey

From `C:\DEV\PALIMPSEST`:

```powershell
.\play.ps1 -Fresh
```

Inspect water, groves, ridge, and the rocky crossing; then choose `UP` and
inspect the Incarnation, Bell, loose Stone, and cloud bank. Optional fresh
`HERE` and `AGAINST` journeys expose the Hearthstone and Cairn silhouettes.

Judge this as the corrected current pack. The newly accepted north star—one
character-scale grid across open territory and generated Areas, with mountains
represented by many meaningful cells—will require later World Grammar and
visual-profile work and is not silently implemented by E5.1.

## Decision requested

Report **Accept**, **Narrow** with a specific remaining visual/workbench defect,
or **Reject**. Acceptance closes E5.1 and the parent E5 visual gate; it does not
authorize Goal 6A, rectangular runtime cells, Area topology, or new gameplay.

## Recorded result

The player accepted on 2026-07-21: “Accept - assets look okay, actor looks
terrible but we can iterate later.” E5.1 and parent E5 are closed. Actor art is
retained as explicit non-blocking visual debt for a later authored asset pass;
it does not reopen the reader, packaging, material grammar, or workbench gates.
