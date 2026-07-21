# Roadmap

| Stage | Scope | Status |
| --- | --- | --- |
| E0 | Immutable authoring-pack contract | Complete |
| E1 | Deterministic compiler, packing, review sheets | Complete |
| E2 | Connected forms and motifs | Complete |
| E3 | PALIMPSEST-shaped specimen breadth | Complete |
| E4 | Godot 4.7.1 .NET pack-only preview | Complete |
| E4.5 | Canonical PALIMPSEST-compatible 20px authoring compiler | Complete |
| E5 | PALIMPSEST runtime loading and packaged compiled artifact | Accepted and closed in Palimpsest on 2026-07-21 |
| E5.1 | Material-specific visual correction and authoring workbench spike | Accepted and closed in Palimpsest on 2026-07-21; actor art deferred |

Current status: **E5.1 accepted and closed; actor-art quality is deferred non-blocking debt.**

The first E5 candidate passed automated integration but failed player visual
UAT because material families read as connected walls and key silhouettes were
too small. The bounded
[E5.1 contract](E5-1-VISUAL-AUTHORING-SPIKE.md) produced the accepted correction.
The player deferred only actor-art quality as non-blocking debt.

## E4.5 reconciled gate

`tools/verify.ps1` is the sole clean-checkout acceptance entry point. It proves:

- the exact versioned PALIMPSEST vocabulary and layer mapping;
- one indexed 20px atlas, one palette, centered anchors, and direct
  concrete-ID resolution;
- required `validation.json` and every compatibility boundary;
- byte-identical repeated canonical output;
- committed file, pack, and aggregate hashes;
- deterministic signed/large-coordinate, seed, variant, mask, and motif
  clipping vectors;
- separate material grammars and nearest-neighbour 20px review sheets;
- review-only manual-baseline evidence outside the versioned pack;
- a Godot preview that reads only the exported canonical artifact; and
- safe output ownership plus bounded Godot process cleanup.

Pinned canonical values:

- indexed atlas:
  `sha256:3c7697eb5d8df50fc920bc52faf422f0077d7d8c2eb60b78582005c41c2126bb`
- manifest:
  `sha256:ebe73660faa1d9f441eee78d5f146a186d072a568f0a749d3aedaaa78deb3bdd`
- PALIMPSEST-shaped pack:
  `sha256:6ff87d0e52c494fe4e0ff79044606dd8694a559aa68fa0d412844ba639167acf`
- canonical aggregate:
  `sha256:85418f3025f2944d2f58a0a981febb00903bf67edcc23cb84054b3fd9f91eae0`

Palimpsest owns the authorized E5 reader and packaged runtime Adapter. P-GEN
continues to own only authored catalogue input, deterministic compilation, and
the canonical four-file output. Player visual UAT remains pending there.
