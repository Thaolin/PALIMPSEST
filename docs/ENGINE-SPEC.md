# Engine Specification

The governing read-only source contract is:

`C:\DEV\PALIMPSEST\docs\PROCEDURAL-VISUAL-GRAMMAR-ENGINE-SPEC.md`

Read-authority SHA-256:
`C3BC1D813DAD5610D55B7CD03722ABA5D01D0449CCF079B4B2912436DA0C52ED`.

The governing build handoff is:

`C:\DEV\PALIMPSEST\docs\CHRONICLE-VISUAL-ENGINE-BUILD-HANDOFF.md`

Read-authority SHA-256:
`AEF0F58FB84E1D55D73F0A571B1D0AB3C155BFAB3139F813C238A488CE21FBD6`.

P-GEN implements the authoring stages through E4.5 and targets PALIMPSEST's live
`Palimpsest20` source contract: format/composer/style version 1, 20px cells, one
palette, one atlas, concrete definition IDs, PALIMPSEST layer classes, and
overview palette indexes.

The governing design document also describes an older general Pack-v1 envelope
with compiler provenance, source digests, required mappings, and multiple
palette/atlas records. That is not the E4.5 integration profile. PALIMPSEST's
live `CompiledVisualPack` consumer and this task's explicit consolidation
requirements narrow the accepted seam to `Palimpsest20`; reproducing the richer
private envelope would recreate the forbidden dual contract. The omission is
intentional and covered by exact live-contract fixtures.

This repository does not claim that PALIMPSEST can load the exported directory.
Its production code exposes no authorized filesystem loader or public
construction seam. Adding either, or swapping packs at runtime, is E5 and
requires separate authorization. No file under `C:\DEV\PALIMPSEST` is modified
by P-GEN E4.5.

This file is a pointer and scope record, not a modified copy of the governing
contract.
