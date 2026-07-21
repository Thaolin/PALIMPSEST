# Use P-GEN as the visual authoring pipeline

**Status:** accepted

P-GEN is built and is the required authoring-time visual asset compiler for
Palimpsest. It is not an optional candidate competing indefinitely with manual
asset construction.

Palimpsest owns the runtime side of the seam. `Chronicle.VisualPack` reads and
validates P-GEN's versioned canonical compiled artifact, `Chronicle.Visuals`
maps semantic Chronicle snapshots into render plans, and Godot draws those
plans. The shipped game never references the compiler, parses P-GEN's source
catalogue, or asks P-GEN to decide what exists or what it means.

The existing manual 20-pixel pack remains a golden comparison and emergency
verification fixture while the required reader integration is proved. It is
not the long-term authoring pipeline. P-GEN vocabulary evolves through
versioned, slice-owned additions rather than waiting for one permanent final
catalogue freeze.

Palimpsest E5 integration is therefore a required enabling gate before Goal 6A.
It must load the canonical P-GEN artifact through the existing pack/composer/
Godot path, prove player and Inspector parity, fail on incompatible or missing
mappings, preserve deterministic output and packaging isolation, and leave all
Chronicle gameplay semantics in Core.
