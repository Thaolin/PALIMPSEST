# P-GEN E4.5 Readiness Review

**Reviewed:** 2026-07-20

**Compiler:** `C:\DEV\P-GEN` at reviewed commit `fc9b8e8`

**Decision:** built, technically reproduced, and required; Palimpsest E5
integration remains unauthorized

This record captures Palimpsest's read-only review of the separate required
P-GEN authoring compiler. It records evidence and integration requirements
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

These facts make the required E5 integration practical. They do not mean the
integration already exists: Palimpsest has not loaded the file contract through
its production pack type or exercised it through the shared player/Inspector
composer.

## Palimpsest integration requirements

### Consumer and contract ownership

Palimpsest currently constructs `CompiledVisualPack` through
`ManualVisualPack`; it has no authorized filesystem reader or pack-selection
configuration. P-GEN exposes a parallel `Palimpsest20Pack` model and codec.

E5 must implement the settled production ownership path: a Palimpsest-owned
reader in `Chronicle.VisualPack` that validates the canonical files and
constructs the existing runtime value without referencing
`Chronicle.VisualCompiler`, parsing a source catalogue, or creating a second
semantic mapping.

### Required vocabulary moves with accepted slices

P-GEN's pinned fixture contains 181 definitions from the pre-Goal-4B visual
contract. Goal 4B now composes `subject.home-hearthstone`; future accepted
combat work may add further required identifiers.

This is expected candidate drift, not a reason to interrupt the active slice.
At E5, record the current required vocabulary as the first versioned baseline,
include every required identifier in the compiled artifact, and fail
conformance on missing or extra incompatible mappings. Later gameplay slices
extend that baseline through deliberate pack compatibility versions; E5 does
not wait for one final game-wide vocabulary freeze.

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

E5 integrates the narrower profile and must reconcile the governing
specification, Architecture, Codemap, and packaging rules. ADR 0004 already
records the hard-to-reverse choice of P-GEN as the required pipeline. P-GEN's
`E4.5 accepted` wording still describes its authoring boundary rather than
claiming the Palimpsest reader already exists.

### Review and negative proof

For integration, retain the current deterministic proof and add:

- an E4.5-specific comparison against the accepted manual pack at native size;
- invalid canonical bundles for every consumer validation class exercised by
  the required reader;
- exact Palimpsest required-mapping coverage, including newly accepted slice
  vocabulary;
- the Gate 3B semantic seeds, overlapping requests, numeric address edges,
  player view, and Inspector through the existing composer;
- a human native-size comparison confirming the integrated artifact or blocking
  the gate with an exact defect.

## Earliest safe E5 gate

E5 may begin only after an explicit Palimpsest authorization at a reconciled
boundary. It is the required next integration gate and precedes Goal 6A. The
current mapping set becomes a versioned baseline; Goal 6A and 6B subsequently
extend P-GEN with their opponent, equipment, resource, structure, Target, and
Modifier presentation vocabulary.

The E5 gate must:

1. freeze the accepted composer vocabulary and compatibility versions;
2. settle consumer, motif-placement, and cosmetic-selection ownership;
3. load the canonical artifact into Palimpsest's existing pack/composer/Godot
   path without changing Core rules or saves;
4. swap manual and candidate packs by packaged content or configuration;
5. prove deterministic composition, player/Inspector parity, packaging, and
   clean removal of all authoring dependencies; and
6. record integration acceptance or an exact blocking defect before Goal 6A.

## Current consequence

No E5 implementation is authorized. P-GEN itself is complete and required; the
remaining work is the separately gated Palimpsest-owned reader, conformance,
packaging, and shared player/Inspector proof. Goal 6A begins only after that
integration passes.
