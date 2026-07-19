---
status: accepted
---

# Use Godot with a C# Chronicle core

The project uses Godot 4 as the self-contained presentation and authoring
shell, while an engine-independent C#/.NET library owns deterministic Chronicle
time, procedural World Grammar, persistence, Agents, and rules. C# is also the
sole production language for Godot-facing code. This boundary keeps iteration
focused on the RPG while allowing native code only where profiling proves it is
needed.

## Consequences

Godot owns rendering, input, UI, audio, and development tools. The simulation
must remain testable without Godot scenes or frame timing, and no gameplay rule
may rely on reflex-speed input. GDScript may support disposable editor or tool
experiments, but never owns shipped gameplay or Chronicle rules.
