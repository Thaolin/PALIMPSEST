# PROTOTYPE — Combat Grammar

This throwaway logic prototype asks whether tick-timed physical combat, one
autonomous screening Companion, and mutually exclusive `Quickly`/`Lasting`
links make `Burn` tactically interesting without passive waiting.

It is not production code. It has no persistence, Godot integration, save
migration, broad combat framework, or balance authority. The pure model and
terminal shell exist only to support the player journey in
[the active pressure-test contract](../../docs/COMBAT-GRAMMAR-PRESSURE-TEST.md).

From the repository root:

```powershell
& .\prototype-combat.ps1
```

The terminal redraws the complete relevant state after every key. Press `q` to
exit. Cycle the three builds with `m`, toggle the Companion with `c`, and use
the exact journey in the contract before judging the model.
