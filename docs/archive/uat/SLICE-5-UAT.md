# Slice 5 UAT — A Word Multiplies

## Status

**Accepted on 2026-07-20.** The complete retained automated gate passed with
zero warnings or errors, then the player reported: “Full UAT accept.” No issue
was reported. Goal 6 remains separately gated and unauthorized.

## What this UAT decides

Slice 5 passes only if one Verb and two authored Nouns read as a coherent
language choice, not two unrelated fixture tricks.

Before acting, the player should be able to predict:

- what fitting `Stone` into `Fly` would act on and change;
- what fitting `Bell` into `Fly` would act on and change; and
- why either future could reasonably matter first.

Do not use the implementation contract or source code to answer those
questions. Use only what the game presents.

## Isolated launch

Launch a new isolated Chronicle:

```powershell
.\play.ps1 -Fresh
```

Keep the printed profile name. Do not use the normal player save.

## Journey

1. Choose `UP — EXPLORE`, use intrinsic `FLY`, and reach **The Bell That Fell
   Up**.
2. Open its Study Source. Read the situation, the authored meanings, and both
   `Stone` and `Bell` offers.
3. Before choosing, write one sentence predicting `Fly[Stone]` and one
   predicting `Fly[Bell]`.
4. Choose which Word to Study first. Record why that future matters more now;
   “the game told me this one is better” is a failure.
5. Complete Study. Confirm only the learned Noun becomes available to fit and
   the hotbar renders the resulting `FLY[NOUN]` Expression.
6. Move adjacent to the matching visible subject, use the Expression, and
   choose its highlighted target. Confirm the Incarnation stays put while the
   predicted subject moves to the matching coordinate in the other Stratum.
7. Inspect both the old and new places:

   - if `Stone` was chosen, the one loose Stone moved and the Bell/source did
     not;
   - if `Bell` was chosen, the one Bell, its Study Source, and its deliberate
     death affordance moved together, with no Bell left at the old address.

8. Save with F5, close the game, then relaunch the same profile:

   ```powershell
   .\play.ps1 -Profile <printed-profile-name>
   ```

9. Confirm the chosen subject remains moved; the Codex, Understanding,
   Loadout, other durable subject, Home/conflict absence, Incarnation, and
   Chronicle Clock facts remain intact.
10. Explain why choosing the other offered Noun first would also have been
    defensible.

## Recorded result

```text
Full UAT accept
```

This accepts the complete prediction, choice, Expression-use, durable-result,
and reload journey below. The player reported no blocking or non-blocking issue.

## Accept if

- both predictions follow naturally from the displayed Word meanings;
- the first choice has a defensible opportunity-cost reason and no obvious
  objectively correct answer;
- fitting, targeting, result text, and world presentation agree;
- exactly one intended durable subject moves;
- the result and unrelated Chronicle state survive restart; and
- no pair-specific instruction was needed to understand the effect.

## Reject if

- either Expression needs a hidden rule or fixture-specific explanation;
- one choice is plainly useless or strictly dominates the other;
- the target, result, old address, or new address contradicts the prediction;
- the Bell separates from its Study Source or appears twice;
- `Fly[Stone]`, intrinsic `Fly`, `Found`, or `Smash` regresses; or
- save/reload changes or loses the consequence.

## Stop boundary

This pass closes Slice 5 after contract, Roadmap, Handoff, Codemap, Development
guide, and UAT evidence reconciliation. It does not authorize Goal 6.
