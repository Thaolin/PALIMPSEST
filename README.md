# PALIMPSEST: The First Patch

![PALIMPSEST — The First Scar](assets/branding/palimpest-dos-title.png)

You just stumbled into a fever dream.

Welcome.

There is a clearing. There are apples. Ordinary things open inward if you know
where to press. Learn their names. Leave a Scar. Break the Universe. Or don't.
Whatever. It was already doing something before you arrived.

PALIMPSEST is a tiny deterministic survival RPG written in C17. It remembers
what you changed and what you learned. Right now, one apple can become unlike
every other apple. With enough attention, it may answer differently, too. An
unnamed weight is involved. It would rather you worked out why.

That is currently the least alarming thing it can do.

Somewhere beyond the clearing, places remember edits you did not make.

Under the bad idea is raylib 6.0, a 720x405 presentation canvas at exact 2x
scale, deliberately coarse simulation coordinates, and primitive generated
artwork. There are no runtime downloads or online services.

## Build and test

Requirements:

- CMake 3.24 or newer
- A C17 compiler
- Git or HTTPS access during the first configure so CMake can fetch the pinned
  raylib 6.0 source archive

From the repository root:

```powershell
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
ctest --test-dir build --output-on-failure -C Release
```

With a single-config generator, the executable is `build/bin/palimpest.exe`.
A Visual Studio generator may place it below `build/bin/Release/`.

Create a self-contained release directory with:

```powershell
cmake --install build --prefix dist/PALIMPSEST-0.5.0 --config Release --component Palimpest
```

## Run

```powershell
build/bin/palimpest.exe
build/bin/palimpest.exe --new --seed 42
build/bin/palimpest.exe --new --seed 0x5eed --save C:\saves\test.pal
build/bin/palimpest.exe --new --seed 42 --developer
```

`--seed` accepts decimal or `0x` hexadecimal and implies `--new`. `--developer`
grants the explicit developer Knowledge profile and opens the raw PALI editor;
ordinary play never advertises its Prototype Reach. The seed and simulation
tick are visible in the developer HUD; ordinary play keeps the clock hidden.
`--capture FILE.png`, `--capture-inspector FILE.png`, and
`--capture-lineage FILE.png` are deterministic visual-verification helpers;
the last opens the first Patched tree, or the first tree, in the normal Lens.

The executable never uses the process working directory to find game data.
Assets resolve from the executable directory. The default save is
`%LOCALAPPDATA%\PALIMPSEST\save.pal` on Windows; `--save` provides an explicit
alternative.

## Controls

- `WASD` or arrow keys: move
- Left click: open a visible object and pause the simulation
- `E`: open the nearest object
- Right click: invoke the nearby object under the pointer
- `F`: invoke the nearest object's `on use(actor)` Behavior
- Inquiry **INDEX** / **OPEN**: collapse the current page or restore it
- Lens **ATTEND** row: retain an impression of a veiled or imprecise meaning
- Lens **−** / **+**: change an Entity or future-fruit nourishment Draft
- Behavior Clause rows: cycle known hunger, Aftertaste, voice, and fate choices
- Tree **FUTURE FRUIT** rows: change this Lineage's nourishment and Behavior
- **INSCRIBE STATE** or `Ctrl+Enter`: validate and commit the State Draft
- **RESET STATE** or `Ctrl+R`: restore the current State value in the Draft
- Behavior **INSCRIBE** / **REVERT**: commit or restore the selected Entity or
  Lineage Clause Draft
- `Esc`: close the inspector
- `F5`: validate and atomically save
- `F9`: reload the complete save
- `Ctrl+Q` or window close: save and quit

Start by clicking an apple, lower its nourishment, **INSCRIBE STATE**, close
the Lens, walk close to it, and invoke it. Open another apple to see what
failed to spread. Afterward, attend to the veiled mark inside three different
kinds of thing. The second kind gives it meaning; the third makes it submit to
number. Open an apple once the sentence inside becomes pliable, choose a
different hunger, voice, or fate, and **INSCRIBE**. Then wait. Trees are
thinking in ordinals. When **The Fruit Remembers** teaches you to notice the
family resemblance, open a tree and write into **FUTURE FRUIT / THIS
LINEAGE**. The Lens tells you exactly what the next apple will nourish and
which Aftertaste it will leave. In `--developer` mode, the original text editor
retains its mouse/keyboard editing, **APPLY**, and **REVERT** controls.

## Current slice

- Deterministic clearing with walkable grass, blocking water/thicket, trees,
  stones, apples, fires, one autonomous moth, and at most one current
  descendant fruit per tree
- Fixed 60 Hz simulation with explicit terrain, object, and per-creature random
  streams
- Hunger, warmth, decaying movement vigor, stable identities, Prototype
  definitions, sparse instance state, and semantic Scars
- Bounded PALI lexer, typed document, deterministic formatter, bytecode
  compiler/VM, runtime values, and host whitelist
- Stable Lexicon concepts projected through persistent Knowledge as
  Unperceived, Veiled, Readable, or Patchable
- Mouse-first, entity-derived Lens with Facets, veiled observations,
  qualitative and exact notations, typed nourishment controls, four typed
  apple Behavior Clauses, and a tree's exact next-fruit preview
- State-derived Inquiries whose current and completed pages form a collapsible
  index without storing parallel completion flags
- Stable Parentage and descendant identities derived from tree, birth ordinal,
  and Genesis rather than whatever randomness wandered past
- Birth-captured sparse Lineage Provenance for nourishment and Behavior, so a
  later lesson to the tree cannot crawl backward into fruit already born
- Deterministic nourishment Inflections plus Aftertaste choices: `NONE`,
  `KINDLE` warmth, or `QUICKEN` movement through decaying vigor
- Sparse Entity and Lineage Provenance for values and Behavior independently,
  so one local sentence need not freeze or copy unrelated meaning
- Checksummed v5 saves containing seed, player state, vigor, tick, descendants,
  Parentage, tree timers and birth order, Lineage definitions, sparse inherited
  nodes, valid Prototype and Entity Patches, Knowledge observations, and
  concept-addressed Scars; v2, v3, and v4 saves migrate on load
- Candidate compilation: bad source reports line/column errors while the last
  valid program remains live
- Explicit developer-only raw source inspection with a bundled readable
  monospaced font, highlighted caret row, and line/column status

This build intentionally omits audio content, cross-tree breeding, general
ecology, Archetype/Law editing, embodiment transfer, infinite terrain, and
branching history. See
[`docs/ROADMAP.md`](docs/ROADMAP.md) for the next small steps.

The interaction model and its deliberate limits are specified in
[`docs/INTERACTION.md`](docs/INTERACTION.md). Prototype Reach ("every apple")
exists underneath for developer tests, but remains a late-game Revelation.
The larger promise—build it, name the pattern, teach reality what it does—lives
in [`docs/VISION.md`](docs/VISION.md).
