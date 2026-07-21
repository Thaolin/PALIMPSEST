# Co-locate P-GEN without crossing the runtime seam

**Status:** accepted

P-GEN is a required, actively maintained authoring Module and therefore lives
in the Palimpsest repository at `tools/P-GEN` with its prior Git history. The
root verifier runs P-GEN's own verifier before Palimpsest's runtime checks, and
the root workbench launcher delegates to the authoring Module. This makes
catalogue, compiler, generated pack, consumer fixtures, and documentation
changes atomic in one branch and pull request. Co-location does not weaken the
seam: production projects never reference P-GEN assemblies or catalogues, and
shipped builds still contain only the four canonical compiled pack files.
