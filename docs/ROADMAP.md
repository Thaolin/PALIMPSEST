# Roadmap

| Stage | Scope | Status |
| --- | --- | --- |
| E0 | Immutable authoring-pack contract | Complete |
| E1 | Deterministic compiler, packing, review sheets | Complete |
| E2 | Connected forms and motifs | Complete |
| E3 | PALIMPSEST-shaped specimen breadth | Complete |
| E4 | Godot 4.7.1 .NET pack-only preview | Complete |
| E4.5 | Canonical PALIMPSEST-compatible 20px authoring compiler | Technically complete — awaiting Palimpsest adoption review |
| E5 | PALIMPSEST runtime loading or swapping | Forbidden without separate authorization |

Current status: **E4.5 technically complete — awaiting Palimpsest adoption review.**

## E4.5 reconciled gate

`tools/verify.ps1` is the sole clean-checkout acceptance entry point. It proves:

- the exact 181-definition PALIMPSEST vocabulary and layer mapping;
- one 160×460 indexed 20px atlas, one palette, centered anchors, and direct
  concrete-ID resolution;
- required `validation.json` and every compatibility boundary;
- byte-identical repeated canonical output;
- committed file, pack, and aggregate hashes;
- deterministic signed/large-coordinate, seed, variant, mask, and motif
  clipping vectors;
- separate material grammars and nearest-neighbour 20px review sheets;
- review-only manual-baseline evidence outside the frozen 181-definition pack;
- a Godot preview that reads only the exported canonical artifact; and
- safe output ownership plus bounded Godot process cleanup.

Pinned canonical values:

- indexed atlas:
  `sha256:052bcfe6bd97274905198decaf7f13c7c0770df097180c74a9808b0ebd18313b`
- manifest:
  `sha256:9436b487408b7c715beadb19ec5bb9178a6e3e0bf391b1f9a573c444beb086d8`
- PALIMPSEST-shaped pack:
  `sha256:4fa9fdf590310cc81f136668d1e656945f5dd04b6fddaa7ad847509385214c64`
- canonical aggregate:
  `sha256:f41d1e4e4f76b5e6e57921cda35050582368486e87e932d5f1273ff4c2be9bd8`

The next stage cannot begin in this repository alone. Palimpsest exposes no
authorized filesystem loader or live-swap seam, player visual UAT remains
pending, the accepted vocabulary must be refreshed after the active slice, and
consumer, motif-placement, and typed-selection ownership remain undecided.
Resolving any of those through production changes is E5.
