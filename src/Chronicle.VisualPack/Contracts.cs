using System.Collections.Immutable;

namespace Chronicle.VisualPack;

public static class PackVersions
{
    public const int PackFormatVersion = 1;
}

public readonly record struct Rgba8(byte R, byte G, byte B, byte A);
public readonly record struct PixelPoint(int X, int Y);
public readonly record struct PixelSize(int Width, int Height);
public readonly record struct PixelRect(int X, int Y, int Width, int Height);

[Flags]
public enum TransformFlags
{
    None = 0,
    FlipHorizontal = 1,
    FlipVertical = 2,
    RotateQuarter = 4
}

public enum VisualLayer
{
    Ground,
    Adjacency,
    Feature,
    Structure,
    Landmark,
    Actor,
    Effect,
    Emphasis,
    Overlay
}

public sealed record CompatibilityRecord(
    int PackFormatVersion,
    int CatalogueSchemaVersion,
    int VisualStyleVersion,
    int ComposerContractVersion,
    string MinimumReaderVersion);

public sealed record CompilerRecord(string Id, string Version);

public sealed record PaletteRole(string Name, byte Index);

public sealed record PaletteRecord(
    string Id,
    ImmutableArray<Rgba8> Entries,
    ImmutableArray<PaletteRole> Roles,
    byte TransparentIndex,
    string Digest);

public sealed record AtlasRecord(
    string Id,
    int NativeSize,
    int Width,
    int Height,
    string BufferPath,
    ImmutableArray<string> CompatiblePalettes,
    int Padding,
    int Extrusion,
    string Digest);

public sealed record VisualRecord(
    string Id,
    string AtlasId,
    PixelRect Rectangle,
    PixelSize LogicalSize,
    PixelPoint Anchor,
    VisualLayer Layer,
    string FamilyId,
    int VariantOrdinal,
    int NativeSize,
    int? AdjacencyMask,
    TransformFlags AllowedTransforms,
    ImmutableArray<string> PaletteRoles,
    bool RequireConnected,
    string GeometryDigest,
    ImmutableArray<string> Tags);

public sealed record MotifMark(
    string VisualId,
    PixelPoint Cell,
    PixelPoint PixelOffset,
    int VariantOrdinal = 0);

public enum MotifClippingBehavior
{
    Clip,
    Reject
}

public sealed record MotifRecord(
    string FamilyId,
    ulong Seed,
    int VariantCount,
    PixelSize Footprint,
    PixelPoint AnchorCell,
    ImmutableArray<MotifMark> Marks,
    ImmutableArray<string> OccupancyTags,
    MotifClippingBehavior ClippingBehavior);

public sealed record AdjacencyRecord(
    string FamilyId,
    ImmutableArray<int> RequiredMasks,
    int? FallbackMask,
    bool RequireEdgeContinuity);

public sealed record ProvenanceRecord(
    string FamilyId,
    string Origin,
    string SourceId,
    string Ownership,
    string? ReviewNote);

public readonly record struct VisualHandle(string PackDigest, int Ordinal);
public readonly record struct VisualKey(
    string Id,
    int NativeSize,
    int VariantOrdinal,
    int? AdjacencyMask);

// Rich authoring state is intentionally internal. Palimpsest20Pack is the only
// public compiled-pack integration contract.
internal sealed class CompiledVisualPack
{
    private readonly ImmutableDictionary<string, ImmutableArray<byte>> _atlasBuffers;
    private readonly ImmutableDictionary<VisualKey, int> _visualOrdinals;

    public CompiledVisualPack(
        string packId,
        CompatibilityRecord compatibility,
        CompilerRecord compiler,
        string sourceDigest,
        ImmutableArray<PaletteRecord> palettes,
        ImmutableArray<AtlasRecord> atlases,
        ImmutableArray<VisualRecord> visuals,
        ImmutableArray<MotifRecord> motifs,
        ImmutableArray<AdjacencyRecord> adjacencies,
        ImmutableArray<string> requiredMappings,
        ImmutableArray<int> requiredNativeSizes,
        ImmutableArray<ProvenanceRecord> provenance,
        IEnumerable<KeyValuePair<string, ReadOnlyMemory<byte>>> atlasBuffers,
        string packDigest = "")
    {
        PackId = packId;
        Compatibility = compatibility;
        Compiler = compiler;
        SourceDigest = sourceDigest;
        Palettes = palettes;
        Atlases = atlases;
        Visuals = visuals;
        Motifs = motifs;
        Adjacencies = adjacencies;
        RequiredMappings = requiredMappings;
        RequiredNativeSizes = requiredNativeSizes;
        Provenance = provenance;
        PackDigest = packDigest;
        _atlasBuffers = atlasBuffers.ToImmutableDictionary(
            static item => item.Key,
            static item => ImmutableArray.Create(item.Value.ToArray()),
            StringComparer.Ordinal);
        var visualOrdinals = ImmutableDictionary.CreateBuilder<VisualKey, int>();
        for (var ordinal = 0; ordinal < visuals.Length; ordinal++)
        {
            var visual = visuals[ordinal];
            visualOrdinals.TryAdd(
                new VisualKey(
                    visual.Id,
                    visual.NativeSize,
                    visual.VariantOrdinal,
                    visual.AdjacencyMask),
                ordinal);
        }
        _visualOrdinals = visualOrdinals.ToImmutable();
    }

    public string PackId { get; }
    public CompatibilityRecord Compatibility { get; }
    public CompilerRecord Compiler { get; }
    public string SourceDigest { get; }
    public ImmutableArray<PaletteRecord> Palettes { get; }
    public ImmutableArray<AtlasRecord> Atlases { get; }
    public ImmutableArray<VisualRecord> Visuals { get; }
    public ImmutableArray<MotifRecord> Motifs { get; }
    public ImmutableArray<AdjacencyRecord> Adjacencies { get; }
    public ImmutableArray<string> RequiredMappings { get; }
    public ImmutableArray<int> RequiredNativeSizes { get; }
    public ImmutableArray<ProvenanceRecord> Provenance { get; }
    public string PackDigest { get; }

    public bool TryResolve(
        string visualId,
        int nativeSize,
        int variantOrdinal,
        int? adjacencyMask,
        out VisualHandle handle)
    {
        var key = new VisualKey(visualId, nativeSize, variantOrdinal, adjacencyMask);
        if (_visualOrdinals.TryGetValue(key, out var ordinal))
        {
            handle = new VisualHandle(PackDigest, ordinal);
            return true;
        }

        handle = default;
        return false;
    }

    public VisualRecord GetVisual(VisualHandle handle)
    {
        if (!StringComparer.Ordinal.Equals(handle.PackDigest, PackDigest) ||
            (uint)handle.Ordinal >= (uint)Visuals.Length)
        {
            throw new ArgumentException("Visual handle belongs to another pack.", nameof(handle));
        }

        return Visuals[handle.Ordinal];
    }

    public ReadOnlyMemory<byte> GetAtlasIndices(string atlasId) =>
        _atlasBuffers.TryGetValue(atlasId, out var bytes)
            ? bytes.AsMemory()
            : throw new KeyNotFoundException($"Unknown atlas '{atlasId}'.");

    internal IEnumerable<KeyValuePair<string, ImmutableArray<byte>>> AtlasBuffers =>
        _atlasBuffers.OrderBy(static item => item.Key, StringComparer.Ordinal);

    internal CompiledVisualPack WithDigest(string digest) => new(
        PackId,
        Compatibility,
        Compiler,
        SourceDigest,
        Palettes,
        Atlases,
        Visuals,
        Motifs,
        Adjacencies,
        RequiredMappings,
        RequiredNativeSizes,
        Provenance,
        _atlasBuffers.Select(static item =>
            KeyValuePair.Create(item.Key, (ReadOnlyMemory<byte>)item.Value.ToArray())),
        digest);

    public static CompiledVisualPack ReferenceFixture()
    {
        var pixels = new byte[]
        {
            0, 0, 0, 0,
            0, 1, 1, 0,
            0, 1, 1, 0,
            0, 0, 0, 0
        };
        var paletteEntries = ImmutableArray.Create(
            new Rgba8(0, 0, 0, 0),
            new Rgba8(232, 196, 92, 255));
        var paletteDigest = PackDigests.Palette(paletteEntries);
        var atlasDigest = PackDigests.Bytes(pixels);
        var geometryDigest = PackDigests.Geometry(
            new PixelRect(0, 0, 4, 4),
            new PixelSize(4, 4),
            new PixelPoint(2, 3),
            16,
            null,
            TransformFlags.None,
            true,
            pixels,
            4);

        return new CompiledVisualPack(
            "chronicle.reference",
            new CompatibilityRecord(1, 1, 1, 1, "1.0.0"),
            new CompilerRecord("chronicle.manual-reference", "1.0.0"),
            PackDigests.Bytes("{}\n"u8),
            ImmutableArray.Create(new PaletteRecord(
                "surface",
                paletteEntries,
                ImmutableArray.Create(new PaletteRole("landmark.gold", 1)),
                0,
                paletteDigest)),
            ImmutableArray.Create(new AtlasRecord(
                "reference-16",
                16,
                4,
                4,
                "atlases/reference-16.indices",
                ImmutableArray.Create("surface"),
                0,
                0,
                atlasDigest)),
            ImmutableArray.Create(new VisualRecord(
                "landmark.reference",
                "reference-16",
                new PixelRect(0, 0, 4, 4),
                new PixelSize(4, 4),
                new PixelPoint(2, 3),
                VisualLayer.Landmark,
                "landmark.reference",
                0,
                16,
                null,
                TransformFlags.None,
                ImmutableArray.Create("landmark.gold"),
                true,
                geometryDigest,
                ImmutableArray.Create("manual", "reference"))),
            ImmutableArray.Create(new MotifRecord(
                "motif.reference",
                0,
                1,
                new PixelSize(1, 1),
                new PixelPoint(0, 0),
                ImmutableArray.Create(new MotifMark(
                    "landmark.reference",
                    new PixelPoint(0, 0),
                    new PixelPoint(0, 0),
                    0)),
                ImmutableArray.Create("occupied"),
                MotifClippingBehavior.Clip)),
            ImmutableArray<AdjacencyRecord>.Empty,
            ImmutableArray.Create("landmark.reference"),
            ImmutableArray.Create(16),
            ImmutableArray.Create(new ProvenanceRecord(
                "landmark.reference",
                "authored",
                "reference-fixture",
                "project",
                "Manually constructed E0 fixture.")),
            new[]
            {
                KeyValuePair.Create(
                    "reference-16",
                    (ReadOnlyMemory<byte>)pixels)
            });
    }
}

public sealed class PackFile
{
    public PackFile(string path, ReadOnlySpan<byte> bytes)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            System.IO.Path.IsPathRooted(path) ||
            path.Contains('\\', StringComparison.Ordinal) ||
            path.Split('/').Any(static segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException("Pack path must be a normalized forward-slash relative path.", nameof(path));
        }

        Path = path;
        Bytes = ImmutableArray.Create(bytes.ToArray());
    }

    public string Path { get; }
    public ImmutableArray<byte> Bytes { get; }
}

public sealed class CanonicalPackOutput
{
    internal CanonicalPackOutput(
        IEnumerable<KeyValuePair<string, byte[]>> canonicalFiles,
        string aggregateDigest)
    {
        Files = canonicalFiles
            .OrderBy(static item => item.Key, StringComparer.Ordinal)
            .Select(static item => new PackFile(item.Key, item.Value))
            .ToImmutableArray();
        CanonicalFiles = Files.ToImmutableSortedDictionary(
            static item => item.Path,
            static item => item.Bytes,
            StringComparer.Ordinal);
        AggregateDigest = aggregateDigest;
    }

    public ImmutableArray<PackFile> Files { get; }
    public IReadOnlyDictionary<string, ImmutableArray<byte>> CanonicalFiles { get; }
    public string AggregateDigest { get; }
}

public enum DiagnosticSeverity
{
    Warning,
    Error
}

public sealed record PackDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Subject,
    string Message);
