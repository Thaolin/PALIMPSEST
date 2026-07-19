using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicle.VisualPack;

namespace Chronicle.VisualCompiler;

public sealed record CataloguePalette(
    string Id,
    ImmutableArray<Rgba8> Entries,
    ImmutableArray<PaletteRole> Roles);

public sealed record CatalogueTarget(
    int NativeSize,
    ImmutableArray<string> Rows,
    PixelPoint Anchor,
    VisualLayer Layer,
    ImmutableArray<string> PaletteRoles);

public sealed record CatalogueFamily(
    string Id,
    ulong Seed,
    int VariantCount,
    ImmutableArray<CatalogueTarget> Targets,
    string Generation);

public sealed record CatalogueConnectedFamily(
    string Id,
    ulong Seed,
    int VariantCount,
    ImmutableArray<int> NativeSizes,
    VisualLayer Layer,
    string PaletteRole,
    bool RequireEdgeContinuity,
    ImmutableArray<int> Masks,
    int? FallbackMask,
    string? TransitionOwnership);

public sealed record CatalogueMotifMark(
    string VisualId,
    PixelPoint Cell,
    PixelPoint PixelOffset);

public sealed record CatalogueMotif(
    string FamilyId,
    PixelSize Footprint,
    PixelPoint AnchorCell,
    ImmutableArray<string> OccupancyTags,
    string ClippingBehavior,
    ImmutableArray<CatalogueMotifMark> Marks);

public sealed class VisualCatalogue
{
    private VisualCatalogue(
        int schemaVersion,
        string packId,
        int visualStyleVersion,
        ImmutableArray<CataloguePalette> palettes,
        ImmutableArray<CatalogueFamily> families,
        ImmutableArray<CatalogueConnectedFamily> connectedFamilies,
        ImmutableArray<CatalogueMotif> motifs,
        byte[] normalizedBytes)
    {
        SchemaVersion = schemaVersion;
        PackId = packId;
        VisualStyleVersion = visualStyleVersion;
        Palettes = palettes;
        Families = families;
        ConnectedFamilies = connectedFamilies;
        Motifs = motifs;
        NormalizedBytes = ImmutableArray.Create(normalizedBytes);
    }

    public int SchemaVersion { get; }
    public string PackId { get; }
    public int VisualStyleVersion { get; }
    public ImmutableArray<CataloguePalette> Palettes { get; }
    public ImmutableArray<CatalogueFamily> Families { get; }
    public ImmutableArray<CatalogueConnectedFamily> ConnectedFamilies { get; }
    public ImmutableArray<CatalogueMotif> Motifs { get; }
    internal ImmutableArray<byte> NormalizedBytes { get; }

    public static VisualCatalogue ParseJson(ReadOnlySpan<byte> utf8)
    {
        var sourceBytes = utf8.ToArray();
        using var document = JsonDocument.Parse(sourceBytes);
        RejectDuplicateProperties(document.RootElement);
        var source = JsonSerializer.Deserialize<SourceCatalogue>(sourceBytes, JsonOptions)
            ?? throw new FormatException("CVC-JSON-001: catalogue is empty.");
        ValidateSource(source);

        var palettes = source.Palettes
            .OrderBy(static palette => palette.Id, StringComparer.Ordinal)
            .Select(static palette => new CataloguePalette(
                palette.Id,
                palette.Entries.Select(ParseRgba).ToImmutableArray(),
                palette.Roles.OrderBy(static role => role.Key, StringComparer.Ordinal)
                    .Select(static role => new PaletteRole(role.Key, role.Value))
                    .ToImmutableArray()))
            .ToImmutableArray();
        var families = source.Families
            .OrderBy(static family => family.Id, StringComparer.Ordinal)
            .Select(static family => new CatalogueFamily(
                family.Id,
                family.Seed,
                family.VariantCount,
                family.Targets.OrderBy(static target => target.NativeSize)
                    .Select(static target => new CatalogueTarget(
                        target.NativeSize,
                        (target.Rows ?? []).ToImmutableArray(),
                        target.Anchor is { Length: 2 }
                            ? new PixelPoint(target.Anchor[0], target.Anchor[1])
                            : throw new FormatException("CVC-CAT-001: anchor needs two integers."),
                        Enum.TryParse<VisualLayer>(target.Layer, true, out var layer)
                            ? layer
                            : throw new FormatException($"CVC-CAT-002: invalid layer '{target.Layer}'."),
                        target.PaletteRoles.Order(StringComparer.Ordinal).ToImmutableArray()))
                    .ToImmutableArray(),
                family.Generation ?? "authored"))
            .ToImmutableArray();
        var connectedFamilies = (source.ConnectedFamilies ?? [])
            .OrderBy(static family => family.Id, StringComparer.Ordinal)
            .Select(static family => new CatalogueConnectedFamily(
                family.Id,
                family.Seed,
                family.VariantCount,
                family.NativeSizes.Order().ToImmutableArray(),
                Enum.TryParse<VisualLayer>(family.Layer, true, out var layer)
                    ? layer
                    : throw new FormatException(
                        $"CVC-CAT-002: invalid layer '{family.Layer}'."),
                family.PaletteRole,
                family.RequireEdgeContinuity,
                (family.Masks ?? Enumerable.Range(0, 16).ToArray())
                    .Order()
                    .ToImmutableArray(),
                family.FallbackMask,
                family.TransitionOwnership))
            .ToImmutableArray();
        var motifs = (source.Motifs ?? [])
            .OrderBy(static motif => motif.FamilyId, StringComparer.Ordinal)
            .Select(static motif => new CatalogueMotif(
                motif.FamilyId,
                PairSize(motif.Footprint, "footprint"),
                PairPoint(motif.AnchorCell, "anchorCell"),
                motif.OccupancyTags.Order(StringComparer.Ordinal).ToImmutableArray(),
                motif.ClippingBehavior,
                motif.Marks.Select(static mark => new CatalogueMotifMark(
                    mark.VisualId,
                    PairPoint(mark.Cell, "cell"),
                    PairPoint(mark.PixelOffset, "pixelOffset"))).ToImmutableArray()))
            .ToImmutableArray();

        var normalized = JsonSerializer.SerializeToUtf8Bytes(
            new NormalizedCatalogue(
                source.SchemaVersion,
                source.PackId,
                source.VisualStyleVersion,
                palettes,
                families,
                connectedFamilies,
                motifs),
            JsonOptions);
        return new VisualCatalogue(
            source.SchemaVersion,
            source.PackId,
            source.VisualStyleVersion,
            palettes,
            families,
            connectedFamilies,
            motifs,
            normalized);
    }

    private static Rgba8 ParseRgba(string value)
    {
        if (value.Length != 8 ||
            !uint.TryParse(
                value,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out var rgba))
        {
            throw new FormatException($"CVC-PAL-001: invalid RGBA8 '{value}'.");
        }

        return new Rgba8(
            (byte)(rgba >> 24),
            (byte)(rgba >> 16),
            (byte)(rgba >> 8),
            (byte)rgba);
    }

    private static PixelPoint PairPoint(int[] values, string name) =>
        values is { Length: 2 }
            ? new PixelPoint(values[0], values[1])
            : throw new FormatException($"CVC-CAT-001: {name} needs two integers.");

    private static PixelSize PairSize(int[] values, string name) =>
        values is { Length: 2 }
            ? new PixelSize(values[0], values[1])
            : throw new FormatException($"CVC-CAT-001: {name} needs two integers.");

    private static void ValidateSource(SourceCatalogue source)
    {
        if (string.IsNullOrWhiteSpace(source.PackId) ||
            source.Palettes is null ||
            source.Families is null ||
            source.Palettes.Any(static palette =>
                palette is null ||
                string.IsNullOrWhiteSpace(palette.Id) ||
                palette.Entries is null ||
                palette.Roles is null) ||
            source.Families.Any(static family =>
                family is null ||
                string.IsNullOrWhiteSpace(family.Id) ||
                family.Targets is null ||
                (family.Generation is not null &&
                    family.Generation is not ("authored" or "class-specific")) ||
                family.Targets.Any(target =>
                    target is null ||
                    (target.Rows is null &&
                        family.Generation != "class-specific") ||
                    target.Anchor is null ||
                    target.Layer is null ||
                    target.PaletteRoles is null))
            ||
            (source.ConnectedFamilies?.Any(static family =>
                family is null ||
                string.IsNullOrWhiteSpace(family.Id) ||
                family.NativeSizes is null ||
                family.Layer is null ||
                family.PaletteRole is null ||
                family.Masks is { Length: 0 } ||
                (family.Masks?.Any(static mask => mask is < 0 or > 15) ?? false) ||
                (family.Masks?.Distinct().Count() != family.Masks?.Length) ||
                family.FallbackMask is < 0 or > 15 ||
                (family.FallbackMask is int fallback &&
                    family.Masks is not null &&
                    !family.Masks.Contains(fallback)) ||
                (family.Masks is { Length: < 16 } &&
                    family.FallbackMask is null) ||
                (family.TransitionOwnership is not null &&
                    family.TransitionOwnership !=
                    "primary-ground-plus-one-transition")) ?? false)
            ||
            (source.Motifs?.Any(static motif =>
                motif is null ||
                string.IsNullOrWhiteSpace(motif.FamilyId) ||
                motif.Footprint is null ||
                motif.AnchorCell is null ||
                motif.OccupancyTags is null ||
                motif.ClippingBehavior is null ||
                motif.Marks is null ||
                motif.Marks.Any(static mark =>
                    mark is null ||
                    mark.VisualId is null ||
                    mark.Cell is null ||
                    mark.PixelOffset is null)) ?? false))
        {
            throw new FormatException(
                "CVC-CAT-004: catalogue is missing a required member.");
        }
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new FormatException(
                        $"CVC-JSON-002: duplicate property '{property.Name}'.");
                }
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicateProperties(item);
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private sealed record SourceCatalogue(
        int SchemaVersion,
        string PackId,
        int VisualStyleVersion,
        SourcePalette[] Palettes,
        SourceFamily[] Families,
        SourceConnectedFamily[]? ConnectedFamilies = null,
        SourceMotif[]? Motifs = null);

    private sealed record SourcePalette(
        string Id,
        string[] Entries,
        Dictionary<string, byte> Roles);

    private sealed record SourceFamily(
        string Id,
        ulong Seed,
        int VariantCount,
        SourceTarget[] Targets,
        string? Generation = null);

    private sealed record SourceTarget(
        int NativeSize,
        string[]? Rows,
        int[] Anchor,
        string Layer,
        string[] PaletteRoles);

    private sealed record SourceConnectedFamily(
        string Id,
        ulong Seed,
        int VariantCount,
        int[] NativeSizes,
        string Layer,
        string PaletteRole,
        bool RequireEdgeContinuity,
        int[]? Masks = null,
        int? FallbackMask = null,
        string? TransitionOwnership = null);

    private sealed record SourceMotif(
        string FamilyId,
        int[] Footprint,
        int[] AnchorCell,
        string[] OccupancyTags,
        string ClippingBehavior,
        SourceMotifMark[] Marks);

    private sealed record SourceMotifMark(
        string VisualId,
        int[] Cell,
        int[] PixelOffset);

    private sealed record NormalizedCatalogue(
        int SchemaVersion,
        string PackId,
        int VisualStyleVersion,
        ImmutableArray<CataloguePalette> Palettes,
        ImmutableArray<CatalogueFamily> Families,
        ImmutableArray<CatalogueConnectedFamily> ConnectedFamilies,
        ImmutableArray<CatalogueMotif> Motifs);
}
