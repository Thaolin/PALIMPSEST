# References

## Engine and language

- [Godot C# basics](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
  — project setup, external-editor workflow, generated C# project files, and
  current platform caveats.
- [Godot C# platform support](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
  — desktop C# support and the current lack of web export for Godot 4 C#.
- [Godot .NET build requirements](https://docs.godotengine.org/en/stable/engine_details/development/compiling/compiling_with_dotnet.html)
  — .NET SDK baseline and the distinction between using Godot and building the
  engine from source.

## Design touchstones

- Caves of Qud: discovery, systemic body transformation, and readable
  strangeness.
- Dwarf Fortress: long-lived world history and material consequence.
- RimWorld: settlement pressure and human-scale event stories.
- Path of Exile: combinatorial build expression.

These are touchstones, not specifications. Do not copy their content, UI, or
systems wholesale.

## Visual-direction notes for later slices

The user-supplied top-down references point toward:

- Dense symbolic tiles whose identity remains legible at a glance.
- Settlements and interiors that look accumulated from material use and
  history, not scattered by a content generator.
- Restricted palettes with strong contrast reserved for active bodies,
  threats, resources, and other consequential state.
- Local scenes that can carry a large amount of inspectable information
  without demanding reflex-speed play.
- Zoomed-out structure and zoomed-in character detail serving different
  questions without becoming separate visual languages.

These notes are for later presentation work. Slice 0 keeps simple shapes and
colors, and no later implementation should reproduce another game's tiles,
layout, or interface.
