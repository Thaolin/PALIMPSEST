# Roadmap

| Stage | Scope | Status |
| --- | --- | --- |
| E0 | Immutable authoring-pack contract | Complete |
| E1 | Deterministic compiler, packing, review sheets | Complete |
| E2 | Connected forms and motifs | Complete |
| E3 | PALIMPSEST-shaped specimen breadth | Complete |
| E4 | Godot 4.7.1 .NET pack-only preview | Complete |
| E4.5 | Canonical PALIMPSEST-compatible 20px authoring compiler | Complete |
| E5 | PALIMPSEST runtime loading and packaged compiled artifact | Authorized in Palimpsest on 2026-07-21; integration active |

Current status: **E5 integration active in Palimpsest.**

## E4.5 reconciled gate

`tools/verify.ps1` is the sole clean-checkout acceptance entry point. It proves:

- the exact 185-definition PALIMPSEST vocabulary and layer mapping;
- one 160×480 indexed 20px atlas, one palette, centered anchors, and direct
  concrete-ID resolution;
- required `validation.json` and every compatibility boundary;
- byte-identical repeated canonical output;
- committed file, pack, and aggregate hashes;
- deterministic signed/large-coordinate, seed, variant, mask, and motif
  clipping vectors;
- separate material grammars and nearest-neighbour 20px review sheets;
- review-only manual-baseline evidence outside the versioned 185-definition pack;
- a Godot preview that reads only the exported canonical artifact; and
- safe output ownership plus bounded Godot process cleanup.

Pinned canonical values:

- indexed atlas:
  `sha256:a44f0eff2fd8f6e2d86595c8b2f399b539af5e405da9f715a8a629e5051a63fd`
- manifest:
  `sha256:6b2c34655543b2a83c0e706cc7c1b6e8dec6a7406fd6c00ce0905e495be93f1a`
- PALIMPSEST-shaped pack:
  `sha256:a63d1cfe147f22a39e84c835a33b77689c8b3b5492b49eb5e5b8fad18108a8fc`
- canonical aggregate:
  `sha256:245cb53df47d7f9866071d75359d272cbd53c56010e3d3f4921d12cf72eaf707`

Palimpsest owns the authorized E5 reader and packaged runtime Adapter. P-GEN
continues to own only authored catalogue input, deterministic compilation, and
the canonical four-file output. Player visual UAT remains pending there.
