---
status: accepted
---

# Capture Lineage inheritance at birth

Lineage Patches define sparse meaning for future descendants. When a child
materializes, it captures only the addressed values and Clauses together with
immutable Parentage and Lineage Provenance; untouched meaning continues to
resolve from broader definitions. Later Lineage changes never retroactively
alter an already materialized child. This rejects live ancestry lookup, which
would make old fruit change invisibly and force saves to reconstruct historical
Lineage state merely to explain the present.

## Consequences

- Descendant identity derives deterministically from progenitor identity and
  birth ordinal.
- Each birth applies a bounded Inflection. Its exact result is derived from the
  same Parentage and birth ordinal and can therefore be previewed before
  materialization.
- Saves must preserve post-Genesis Entities, Parentage, birth order, and sparse
  inherited meaning.
- Lineage storage and birth work remain explicitly bounded.
