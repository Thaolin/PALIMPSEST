# P-GEN E4.5 Readiness Review

**Reviewed:** 2026-07-20

**Candidate:** `C:\DEV\P-GEN` at `fc9b8e8`

**Decision:** technically reproducible; not adopted; E5 remains unauthorized

This record captures Palimpsest's read-only review of the separate P-GEN
authoring compiler. It records evidence and future integration questions
without expanding the active gameplay slice or changing the governing
[Chronicle Visual Engine specification](PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md)
and archived historical
[E0–E4 build handoff](archive/prompts/CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md).

## Evidence reproduced

- P-GEN's recorded hashes for both governing Palimpsest documents matched the
  current files at review time.
- `C:\DEV\P-GEN\tools\verify.ps1` passed from a clean worktree in about
  32 seconds with zero build warnings and zero errors.
- The conformance runner, two isolated compiler outputs, committed hashes,
  filesystem ownership guards, Godot build, pack-only scene launch, bounded
  process cleanup, viewport capture, and CPU oracle all passed.
- Both canonical builds produced aggregate digest
  `sha256:f41d1e4e4f76b5e6e57921cda35050582368486e87e932d5f1273ff4c2be9bd8`.
- The 20-pixel output showed a coherent restrained palette, readable
  Incarnation, Stone, Bell, emphasis marks, and continuous adjacency families.
  This is promising review evidence, not Palimpsest player UAT.
- The exported metadata did not contain gameplay rules or Chronicle state.
- P-GEN remained clean after verification. No Palimpsest file was changed by
  the external verifier.

## What is ready

- The compiler, catalogue, conformance runner, CLI, and Godot preview remain
  separated along the intended dependency direction.
- Compiler and catalogue code are absent from the Godot preview dependency
  graph.
- The canonical output is deterministic, strict, indexed, versioned, and
  independently readable by P-GEN's pack-only preview.
- The accepted native target is singular: 20 pixels, one palette, one indexed
  atlas, concrete string-resolvable definitions, and Palimpsest layer values.
- The authoring compiler can remain completely absent from a shipped
  Palimpsest build.

These facts make a later E5 evaluation practical. They do not yet satisfy the
drop-in definition because Palimpsest has not accepted the file contract,
loaded it through its production pack type, or exercised it through the shared
player/Inspector composer.

## Unsettled before E5

### Consumer and contract ownership

Palimpsest currently constructs `CompiledVisualPack` through
`ManualVisualPack`; it has no authorized filesystem reader or pack-selection
configuration. P-GEN exposes a parallel `Palimpsest20Pack` model and codec.

An E5 gate must choose one production ownership path. The preferred proof is a
Palimpsest-owned reader in `Chronicle.VisualPack` that validates the canonical
files and constructs the existing runtime value without referencing
`Chronicle.VisualCompiler`, parsing a source catalogue, or creating a second
semantic mapping.

### Required vocabulary moves with accepted slices

P-GEN's pinned fixture contains 181 definitions from the pre-Goal-4B visual
contract. Goal 4B now composes `subject.home-hearthstone`; future accepted
combat work may add further required identifiers.

This is expected candidate drift, not a reason to interrupt the active slice.
At E5, freeze the required vocabulary from the latest accepted Palimpsest
composer, add every required identifier to the candidate fixture, and fail
conformance on missing or extra incompatible mappings.

### Motif placement ownership

P-GEN's source catalogue contains footprint, anchor, ordered-mark, occupancy,
variant, and clipping data for multi-cell motifs. The narrowed four-file
`Palimpsest20` export currently exposes only flat concrete definitions.

Before a later slice depends on pack-driven multi-cell ruins, groves, ridges,
or structures, E5 must make one ownership decision:

- Core snapshots plus `Chronicle.Visuals` continue to own placement and the
  pack supplies only resolved marks; or
- the compiled pack gains immutable motif records consumed by
  `Chronicle.Visuals`.

Neither path may require the shipped game to parse P-GEN's catalogue, reference
the compiler assembly, or duplicate hidden authoring rules in Godot.

### Cosmetic selection ownership

P-GEN includes a pinned `DeterministicSelection` utility in the authoring
compiler assembly. It is not an approved Palimpsest runtime seam: its inputs
use the authoring layer enum and do not carry a typed World/Stratum address.
Palimpsest's current `VisualGrammar` selection remains authoritative.

E5 must either leave selection wholly in the existing composer or deliberately
define a pack/composer-owned typed selection context that includes every
identity required for overlap-stable results. The runtime must not ship or
copy an algorithm merely because the authoring compiler exposes it.

### Governing specification and E4.5 profile

P-GEN's four-file `Palimpsest20` profile is intentionally narrower than the
older general Pack-v1 envelope in the governing design specification. That
narrowing matches the current single-pack runtime shape, but P-GEN's local
documents cannot revise Palimpsest's authority.

If E5 adopts the narrower profile, reconcile the governing specification,
Architecture, Codemap, packaging rules, and an ADR if the ownership decision
is hard to reverse. Until then, P-GEN's `E4.5 accepted` wording means its own
technical authoring boundary, not Palimpsest adoption.

### Review and negative proof

Before adoption, retain the current deterministic proof and add:

- an E4.5-specific comparison against the accepted manual pack at native size;
- invalid canonical bundles for every consumer validation class exercised by
  the adopted reader;
- exact Palimpsest required-mapping coverage, including newly accepted slice
  vocabulary;
- the Gate 3B semantic seeds, overlapping requests, numeric address edges,
  player view, and Inspector through the existing composer;
- a human visual decision recorded as adopt, narrow, defer, or reject.

## Earliest safe E5 gate

E5 may begin only after an explicit Palimpsest authorization at a reconciled
UAT boundary. Goal 4C and Slice 5 are now accepted, but ADR 0003 replaces their
collectible-Noun composition as the direction for future language growth. E5
vocabulary freeze and conformance therefore remain downstream of the Modifier
Grammar pressure test and any separately authorized production catalogue.

The E5 gate must:

1. freeze the accepted composer vocabulary and compatibility versions;
2. settle consumer, motif-placement, and cosmetic-selection ownership;
3. load the canonical artifact into Palimpsest's existing pack/composer/Godot
   path without changing Core rules or saves;
4. swap manual and candidate packs by packaged content or configuration;
5. prove deterministic composition, player/Inspector parity, packaging, and
   clean removal of all authoring dependencies; and
6. record adopt, narrow, defer, or reject before another visual-system change.

## Current consequence

No E5 implementation is authorized. A Palimpsest-owned reader remains
technically separable from gameplay vocabulary, but even that plumbing requires
a separate gate. Required-vocabulary freeze, conformance, and visual adoption
must wait until the successor Power Word shape settles. P-GEN remains optional
evidence and does not block the Modifier Grammar pressure test.
