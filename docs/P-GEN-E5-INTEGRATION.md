# P-GEN E5 — The Authored World Enters the Game

**Authorized:** 2026-07-21
**Status:** Accepted and closed 2026-07-21 after corrected E5.1 player UAT
**Imported P-GEN head:** `f1287f0` under `tools/P-GEN`
**Canonical aggregate:** `sha256:85418f3025f2944d2f58a0a981febb00903bf67edcc23cb84054b3fd9f91eae0`

## Player-visible hypothesis

The accepted generated world can use P-GEN's authored 20-pixel visual pack in
the real player and Inspector without changing Chronicle meaning, map density,
or deterministic presentation. A bad or incompatible pack must fail at launch
with a precise error rather than render a plausible but incorrect world.

## Permitted production change

E5 may:

- add the current Palimpsest visual IDs missing from P-GEN's authored catalogue;
- add one strict Palimpsest-owned compiled-bundle reader to
  `Chronicle.VisualPack`;
- package the canonical P-GEN 20-pixel bundle as data, never compiler code;
- route both `ChronicleApp` and `WorldAtlasInspector` through one shared pack
  loader;
- make that P-GEN artifact the normal 20-pixel default while preserving the
  manual 16- and 20-pixel packs as explicit golden comparison fixtures; and
- add focused checks, launch/capture arguments, verification, and E5 UAT
  instructions required to prove that boundary.

The P-GEN pin and aggregate artifact digest must be updated here if catalogue
conformance requires a later P-GEN commit during this gate.

## Bundle interface

The reader accepts exactly the canonical relative files:

```text
manifest.json
hashes.json
validation.json
atlases/palimpsest20.indices
```

It rejects missing or unexpected files, duplicate paths, path escape,
non-canonical JSON, unsupported contract versions, an incompatible minimum
reader version, mismatched file or aggregate hashes, invalid atlas data, and a
manifest digest that does not match the constructed existing
`CompiledVisualPack` value.

The filesystem is a local-substitutable Adapter. The reader's narrow Interface
is canonical relative path plus bytes; checks can exercise the same validation
without depending on a working-directory layout. The reader must construct the
existing runtime model rather than expose a parallel public pack model.

## Current vocabulary baseline

The artifact must resolve every visual ID used by the current v5 player and
Inspector, including the Hearthstone, Riven Cairn, shattered Cairn, and River
Ward danger emphasis. This is a versioned integration baseline, not a promise
to freeze successor RPG vocabulary. Later slices add authored IDs through
P-GEN and bump compatible contracts deliberately.

## Required proof

Automated proof must establish:

1. the pinned P-GEN verifier passes and produces the recorded digest;
2. the canonical bundle round-trips through the Palimpsest reader and resolves
   the exact current required vocabulary;
3. missing, unexpected, duplicate, corrupt, hash-mismatched, incompatible, and
   non-canonical inputs fail with precise diagnostics;
4. the P-GEN and manual golden packs compose identical semantic scenarios
   through the same `VisualGrammar` Interface, with deterministic results;
5. player and Inspector both use the shared packaged P-GEN default at 20 px;
6. explicit manual comparison remains available at native 20 px and the 16 px
   retained reference still passes;
7. packaged output contains the four compiled files and no P-GEN compiler,
   catalogue, fixture, or authoring assembly; and
8. the complete retained Palimpsest gate passes with zero warnings or errors
   and no save or World Grammar change.

## Player UAT

Provide a native-size player/Inspector comparison sheet and a short launch
journey proving the default P-GEN world still reads clearly at the accepted
20-pixel density. Record the player's result before E5 closes.

## Explicitly forbidden

E5 does not authorize successor grammar code, save v6, combat, HP, equipment,
opponents, new world semantics, Goal 6A vocabulary, Load Sources, Home economy,
Agents, raids, camera redesign, or broad visual polish. P-GEN remains an
authoring Module outside Palimpsest's shipped dependency graph.

## Stop condition

Stop after automated acceptance and the UAT handoff. Goal 6A remains forbidden
until the player accepts E5 and separately authorizes its production contract.
