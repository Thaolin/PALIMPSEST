# PROTOTYPE — Combat Grammar

This throwaway logic prototype asks whether a paused/Slow heartbeat, tick-timed
physical combat, one autonomous screening Companion, and mutually exclusive
`Quickly`/`Lasting` links make `Burn` tactically interesting without passive
waiting.

It is not production code. It has no persistence, Godot integration, save
migration, broad combat framework, or balance authority. The pure model and
terminal shell exist only to support the player journey in
[the active pressure-test contract](../../docs/COMBAT-GRAMMAR-PRESSURE-TEST.md).

From the repository root:

```powershell
& .\prototype-combat.ps1
```

Choose the engagement plan with `1` and `2`, then press `e`. Contact readies the
selected weapon and Companion behaviors and pauses before danger advances.
Press Space to run the Slow heartbeat or pause it. Any tactical command entered
during Slow pauses the clock before acting. Press `q` to exit, and use the exact
journey in the contract before judging the refinement.
