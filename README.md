# Chronicle Visual Engine

A deterministic C# pixel compiler and versioned compiled-pack seam for
Palimpsest-specific visual authoring. This repository is an isolated candidate;
it does not modify or integrate with Palimpsest.

## Quick start

```powershell
./tools/verify.ps1
```

The proof builds and validates the pure .NET modules, compiles the specimen pack
twice, compares canonical output, then builds and exercises the Godot 4.7.1 .NET
preview and deterministic real-viewport capture. The project is complete through
E4 and stopped for human visual review; E5 integration is not implemented. See
`docs/DEVELOPMENT.md` for prerequisites and interactive preview instructions.
