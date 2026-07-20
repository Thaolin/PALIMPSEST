# Goal 4A UAT — A Choice of Words

## Status

Accepted by the player on 2026-07-19. Every UAT item passed after
`checks/verify.ps1` passed the complete automated Goal 4A gate, including
partial and completed Study across real application restarts. Slice 4A is
complete. Slice 4B remains separately gated and is not yet authorized.

## What this UAT decides

The implementation passes only if the Bell feels like a strange situation the
player interprets, not a disguised reward button or generic experience bar.
The player should be able to explain:

- what the Study Source is;
- why it plausibly offers both `Stone` and `Bell`;
- which word was deliberately selected;
- why only that word advanced;
- what survived save/load and the body's death;
- where the completed word went.

## Isolated 20-pixel launch

Close other Godot instances. From the repository root, use an isolated UAT save
so the interactive Chronicle is untouched:

```powershell
$env:DOTNET_ROOT = "$PWD\.tools\dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = "$PWD\.tools\dotnet-cli"
$env:NUGET_PACKAGES = "$env:DOTNET_CLI_HOME\.nuget\packages"
$env:APPDATA = (New-Item -ItemType Directory -Force "$PWD\.tools\goal4a-uat\Roaming").FullName
$env:LOCALAPPDATA = (New-Item -ItemType Directory -Force "$PWD\.tools\goal4a-uat\Local").FullName
$godotGui = "$PWD\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe"
& $godotGui --path src\Chronicle.Godot -- --visual-cell-size=20
```

If this UAT root already contains a prior run, use a new directory name rather
than deleting it during the review.

## Player journey

1. On **THE FIRST HORIZON**, confirm `UP` is visibly the **Explore Starting
   Vector**, grants `Fly`, and does not read like a permanent class choice.
2. Choose it. Confirm `Fly` appears in the Codex and the first Loadout slot,
   then use Fly and move north four cells to **The Bell That Fell Up**.
3. Select `STUDY SKY-STONE`. Confirm one contextual surface opens over the
   Codex panel without disturbing the 20-pixel map or hotbar.
4. Without consulting this document, identify:
   - the **Sky-Stone Clapper** and its situation;
   - `Rare / Lethal / Landmark`;
   - a possible contribution of `16 Understanding`;
   - `Stone` first and `Bell` second;
   - a distinct visible reason for each offer.
5. Choose `Stone`. Confirm it says `SELECTED`, begins at `0/16`, and `Bell`
   remains `0/16`. Neither Noun should enter the Codex early.
6. Pause and wait for at least two clock pulses. Confirm Tick and both
   Understanding values remain fixed. Resume at Slow speed and let Stone reach
   a clearly partial amount.
7. Save, move away, and load. Confirm the exact address, selected `Stone`
   pursuit, partial Stone amount, zero Bell amount, Codex, and Loadout return.
8. Save once more, close the application, relaunch with the same command, and
   confirm the active partial pursuit survives a real process restart.
9. At the Bell, select `END THIS BODY`, read the warning, then select
   `CONFIRM DEATH`. Confirm active Study clears while the partial Stone amount
   and Codex remain.
10. Create the replacement. Confirm Incarnation `2` begins at
    `surface (0, 0)` with eight empty Loadout slots, while `Fly` remains in the
    Codex and the partial Stone amount survives.
11. Re-equip `Fly`, return to the Bell, open the source again, and deliberately
    choose `Stone` again. Confirm it resumes from the retained amount rather
    than zero.
12. Complete `16/16`. Confirm `Stone` appears once under Codex Nouns, the offer
    says `LEARNED`, Bell remains unlearned at zero, and no active pursuit
    remains.
13. Select learned Stone again. Confirm the rejection is legible and nothing
    duplicates or advances.
14. Reconfirm the accepted regression: return to the surface, fit Stone into
    Fly, target the adjacent loose Stone, and see `Fly[Stone]` move only that
    material subject.
15. Save, close, and relaunch. Confirm the completed word, Understanding,
    replacement identity, Loadout, address, and loose-Stone delta restore.

## Acceptance record

- [x] I understood what I was studying.
- [x] Both offers felt plausible for visible, distinct reasons.
- [x] My selection was deliberate and visibly active.
- [x] Only the selected word advanced.
- [x] Save/load and a real restart preserved exact partial Study.
- [x] Death removed the pursuit, not the Chronicle's progress.
- [x] The replacement inherited Codex and Understanding with an empty Loadout.
- [x] Completion put the chosen word in the Codex exactly once.
- [x] The 20-pixel map, hotbar, and prior Fly/Stone journey still read correctly.
- [x] **Accept 4A**
- [ ] Reject 4A

Notes:

```text
Player report, 2026-07-19: "All UAT passes."

Accepted follow-up: the playspace needs to grow. The text and menu regions are
currently much larger than the visible world region. This is not a 4A blocker,
but the next authorized UI-bearing slice must protect a substantially larger
world view before adding more interface surface.
```

## Stop condition

This sheet, the Goal 4 contract, Roadmap, and Handoff were reconciled on
acceptance. Work remains stopped between slices. Slice 4B still requires
separate authorization.
