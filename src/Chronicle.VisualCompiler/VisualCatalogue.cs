using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicle.VisualPack;

namespace Chronicle.VisualCompiler;

public enum GenerationStrategy
{
    AuthoredRows,
    RoleMask
}

public enum ConcreteIdentity
{
    Single,
    Variant,
    AlwaysTwoDigitMask,
    MaskedVariant,
    Masked,
    ReviewOnly
}

public enum MaterialTreatment
{
    Water,
    Cloud,
    Grove,
    Ridge,
    Crossing,
    Wall,
    Path,
    Transition
}

public enum TransitionOwnership
{
    None,
    PrimaryGroundPlusOneTransition
}

public enum ClippingBehavior
{
    Clip,
    Reject
}

public sealed record CataloguePalette(
    string Id,
    ImmutableArray<Rgba8> Entries,
    ImmutableArray<PaletteRole> Roles);

public sealed record CatalogueTarget(
    int NativeSize,
    ImmutableArray<string> Rows,
    PixelPoint Anchor,
    VisualLayer Layer,
    ImmutableArray<string> PaletteRoles,
    string OverviewPaletteRole,
    bool RequireConnected);

public sealed record CatalogueFamily(
    string Id,
    ulong Seed,
    int VariantCount,
    ImmutableArray<CatalogueTarget> Targets,
    GenerationStrategy Generation,
    ConcreteIdentity Identity);

public sealed record CatalogueConnectedFamily(
    string Id,
    ulong Seed,
    int VariantCount,
    ImmutableArray<int> NativeSizes,
    VisualLayer Layer,
    string PaletteRole,
    ImmutableArray<string> PaletteRoles,
    string OverviewPaletteRole,
    MaterialTreatment MaterialTreatment,
    ConcreteIdentity Identity,
    bool RequireEdgeContinuity,
    ImmutableArray<int> Masks,
    int? FallbackMask,
    TransitionOwnership TransitionOwnership);

public sealed record CatalogueMotifMark(
    string VisualId,
    PixelPoint Cell,
    PixelPoint PixelOffset,
    int VariantOrdinal);

public sealed record CatalogueMotif(
    string FamilyId,
    ulong Seed,
    int VariantCount,
    PixelSize Footprint,
    PixelPoint AnchorCell,
    ImmutableArray<string> OccupancyTags,
    ClippingBehavior ClippingBehavior,
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

        var palettes = source.Palettes!
            .OrderBy(static palette => palette.Id, StringComparer.Ordinal)
            .Select(static palette => new CataloguePalette(
                palette.Id!,
                palette.Entries!.Select(ParseRgba).ToImmutableArray(),
                palette.Roles!.OrderBy(static role => role.Key, StringComparer.Ordinal)
                    .Select(static role => new PaletteRole(role.Key, role.Value))
                    .ToImmutableArray()))
            .ToImmutableArray();
        var families = source.Families!
            .OrderBy(static family => family.Id, StringComparer.Ordinal)
            .Select(family =>
            {
                var generation = ParseGenerationStrategy(
                    family.Generation,
                    source.SchemaVersion);
                return new CatalogueFamily(
                    family.Id!,
                    family.Seed,
                    family.VariantCount,
                    family.Targets!.OrderBy(static target => target.NativeSize)
                        .Select(target =>
                        {
                            var paletteRoles = target.PaletteRoles!.ToImmutableArray();
                            return new CatalogueTarget(
                                target.NativeSize,
                                (target.Rows ?? []).ToImmutableArray(),
                                PairPoint(target.Anchor!, "anchor"),
                                ParseLayer(target.Layer),
                                paletteRoles,
                                source.SchemaVersion == 1
                                    ? paletteRoles.IsEmpty
                                        ? string.Empty
                                        : paletteRoles[0]
                                    : target.OverviewPaletteRole!,
                                target.RequireConnected);
                        })
                        .ToImmutableArray(),
                    generation,
                    source.SchemaVersion == 1
                        ? ConcreteIdentity.ReviewOnly
                        : ParseConcreteIdentity(family.Identity));
            })
            .ToImmutableArray();
        var connectedFamilies = (source.ConnectedFamilies ?? [])
            .OrderBy(static family => family.Id, StringComparer.Ordinal)
            .Select(family =>
            {
                var paletteRoles = source.SchemaVersion == 1
                    ? ImmutableArray.Create(family.PaletteRole!)
                    : family.PaletteRoles!.ToImmutableArray();
                return new CatalogueConnectedFamily(
                    family.Id!,
                    family.Seed,
                    family.VariantCount,
                    family.NativeSizes!.Order().ToImmutableArray(),
                    ParseLayer(family.Layer),
                    family.PaletteRole!,
                    paletteRoles,
                    source.SchemaVersion == 1
                        ? family.PaletteRole!
                        : family.OverviewPaletteRole!,
                    source.SchemaVersion == 1
                        ? MaterialTreatment.Path
                        : ParseMaterialTreatment(family.MaterialTreatment),
                    source.SchemaVersion == 1
                        ? ConcreteIdentity.ReviewOnly
                        : ParseConcreteIdentity(family.Identity),
                    family.RequireEdgeContinuity,
                    (family.Masks ?? Enumerable.Range(0, 16).ToArray())
                        .Order()
                        .ToImmutableArray(),
                    family.FallbackMask,
                    ParseTransitionOwnership(family.TransitionOwnership));
            })
            .ToImmutableArray();
        var motifs = (source.Motifs ?? [])
            .OrderBy(static motif => motif.FamilyId, StringComparer.Ordinal)
            .Select(motif => new CatalogueMotif(
                motif.FamilyId!,
                source.SchemaVersion == 1 ? 0UL : motif.Seed!.Value,
                source.SchemaVersion == 1 ? 1 : motif.VariantCount!.Value,
                PairSize(motif.Footprint!, "footprint"),
                PairPoint(motif.AnchorCell!, "anchorCell"),
                motif.OccupancyTags!.Order(StringComparer.Ordinal).ToImmutableArray(),
                ParseClippingBehavior(motif.ClippingBehavior),
                motif.Marks!.Select(mark => new CatalogueMotifMark(
                    mark.VisualId!,
                    PairPoint(mark.Cell!, "cell"),
                    PairPoint(mark.PixelOffset!, "pixelOffset"),
                    source.SchemaVersion == 1 ? 0 : mark.VariantOrdinal!.Value))
                    .ToImmutableArray()))
            .ToImmutableArray();

        var normalized = JsonSerializer.SerializeToUtf8Bytes(
            new NormalizedCatalogue(
                source.SchemaVersion,
                source.PackId!,
                source.VisualStyleVersion,
                palettes,
                families,
                connectedFamilies,
                motifs),
            JsonOptions);
        return new VisualCatalogue(
            source.SchemaVersion,
            source.PackId!,
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

    private static GenerationStrategy ParseGenerationStrategy(
        string? value,
        int schemaVersion)
    {
        if (value is null && schemaVersion == 1)
        {
            return GenerationStrategy.AuthoredRows;
        }

        var strategy = value switch
        {
            "authored" => GenerationStrategy.AuthoredRows,
            "role-mask" => GenerationStrategy.RoleMask,
            _ => throw UnknownToken("generation", value)
        };
        if (schemaVersion == 1 && strategy == GenerationStrategy.RoleMask)
        {
            throw UnknownToken("generation", value);
        }

        return strategy;
    }

    private static ConcreteIdentity ParseConcreteIdentity(string? value) => value switch
    {
        "single" => ConcreteIdentity.Single,
        "variant" => ConcreteIdentity.Variant,
        "always-two-digit-mask" => ConcreteIdentity.AlwaysTwoDigitMask,
        "masked-variant" => ConcreteIdentity.MaskedVariant,
        "masked" => ConcreteIdentity.Masked,
        "review-only" => ConcreteIdentity.ReviewOnly,
        _ => throw UnknownToken("identity", value)
    };

    private static MaterialTreatment ParseMaterialTreatment(string? value) => value switch
    {
        "water" => MaterialTreatment.Water,
        "cloud" => MaterialTreatment.Cloud,
        "grove" => MaterialTreatment.Grove,
        "ridge" => MaterialTreatment.Ridge,
        "crossing" => MaterialTreatment.Crossing,
        "wall" => MaterialTreatment.Wall,
        "path" => MaterialTreatment.Path,
        "transition" => MaterialTreatment.Transition,
        _ => throw UnknownToken("materialTreatment", value)
    };

    private static TransitionOwnership ParseTransitionOwnership(string? value) => value switch
    {
        null or "none" => TransitionOwnership.None,
        "primary-ground-plus-one-transition" =>
            TransitionOwnership.PrimaryGroundPlusOneTransition,
        _ => throw UnknownToken("transitionOwnership", value)
    };

    private static ClippingBehavior ParseClippingBehavior(string? value) => value switch
    {
        "clip" => ClippingBehavior.Clip,
        "reject" => ClippingBehavior.Reject,
        _ => throw UnknownToken("clippingBehavior", value)
    };

    private static VisualLayer ParseLayer(string? value) => value switch
    {
        "ground" => VisualLayer.Ground,
        "adjacency" => VisualLayer.Adjacency,
        "feature" => VisualLayer.Feature,
        "structure" => VisualLayer.Structure,
        "landmark" => VisualLayer.Landmark,
        "actor" => VisualLayer.Actor,
        "effect" => VisualLayer.Effect,
        "emphasis" => VisualLayer.Emphasis,
        "overlay" => VisualLayer.Overlay,
        _ => throw new FormatException($"CVC-CAT-002: invalid layer '{value}'.")
    };

    private static FormatException UnknownToken(string member, string? value) =>
        new($"CVC-CAT-003: invalid {member} '{value}'.");

    private static void ValidateSource(SourceCatalogue source)
    {
        if (string.IsNullOrWhiteSpace(source.PackId) ||
            source.Palettes is null ||
            source.Families is null)
        {
            MissingRequiredMember();
        }

        if (source.SchemaVersion is not (1 or 2))
        {
            throw new FormatException(
                $"CVC-CAT-005: unsupported schema version '{source.SchemaVersion}'.");
        }

        foreach (var palette in source.Palettes!)
        {
            if (palette is null ||
                string.IsNullOrWhiteSpace(palette.Id) ||
                palette.Entries is null ||
                palette.Roles is null)
            {
                MissingRequiredMember();
            }
        }

        foreach (var family in source.Families!)
        {
            if (family is null ||
                string.IsNullOrWhiteSpace(family.Id) ||
                family.Targets is null ||
                (source.SchemaVersion == 2 &&
                    (family.Generation is null || family.Identity is null)))
            {
                MissingRequiredMember();
            }

            var generation = ParseGenerationStrategy(
                family.Generation,
                source.SchemaVersion);
            if (source.SchemaVersion == 2)
            {
                _ = ParseConcreteIdentity(family.Identity);
            }

            foreach (var target in family.Targets!)
            {
                if (target is null ||
                    target.Anchor is null ||
                    target.Layer is null ||
                    target.PaletteRoles is null ||
                    target.Rows is null ||
                    (source.SchemaVersion == 2 &&
                        target.OverviewPaletteRole is null))
                {
                    MissingRequiredMember();
                }

                _ = ParseLayer(target.Layer);
            }
        }

        foreach (var family in source.ConnectedFamilies ?? [])
        {
            if (family is null ||
                string.IsNullOrWhiteSpace(family.Id) ||
                family.NativeSizes is null ||
                family.Layer is null ||
                family.PaletteRole is null ||
                family.Masks is { Length: 0 } ||
                (source.SchemaVersion == 2 &&
                    (family.PaletteRoles is null ||
                     family.OverviewPaletteRole is null ||
                     family.MaterialTreatment is null ||
                     family.Identity is null)))
            {
                MissingRequiredMember();
            }

            if (family.Masks?.Any(static mask => mask is < 0 or > 15) ?? false ||
                family.Masks?.Distinct().Count() != family.Masks?.Length ||
                family.FallbackMask is < 0 or > 15 ||
                (family.FallbackMask is int fallback &&
                    family.Masks is not null &&
                    !family.Masks.Contains(fallback)) ||
                (family.Masks is { Length: < 16 } && family.FallbackMask is null))
            {
                throw new FormatException(
                    "CVC-CAT-006: connected-family mask data is invalid.");
            }

            _ = ParseLayer(family.Layer);
            _ = ParseTransitionOwnership(family.TransitionOwnership);
            if (source.SchemaVersion == 2)
            {
                _ = ParseMaterialTreatment(family.MaterialTreatment);
                _ = ParseConcreteIdentity(family.Identity);
            }
        }

        foreach (var motif in source.Motifs ?? [])
        {
            if (motif is null ||
                string.IsNullOrWhiteSpace(motif.FamilyId) ||
                motif.Footprint is null ||
                motif.AnchorCell is null ||
                motif.OccupancyTags is null ||
                motif.ClippingBehavior is null ||
                motif.Marks is null ||
                (source.SchemaVersion == 2 &&
                    (!motif.Seed.HasValue || !motif.VariantCount.HasValue)))
            {
                MissingRequiredMember();
            }

            _ = ParseClippingBehavior(motif.ClippingBehavior);
            foreach (var mark in motif.Marks!)
            {
                if (mark is null ||
                    mark.VisualId is null ||
                    mark.Cell is null ||
                    mark.PixelOffset is null ||
                    (source.SchemaVersion == 2 && !mark.VariantOrdinal.HasValue))
                {
                    MissingRequiredMember();
                }
            }
        }

        if (source.SchemaVersion == 2)
        {
            ValidateSchema2(source);
        }
    }

    private static void ValidateSchema2(SourceCatalogue source)
    {
        var familyIds = source.Families!
            .Select(static family => family.Id!)
            .Concat((source.ConnectedFamilies ?? [])
                .Select(static family => family.Id!))
            .Concat((source.Motifs ?? [])
                .Select(static motif => motif.FamilyId!))
            .ToArray();
        if (familyIds.Distinct(StringComparer.Ordinal).Count() != familyIds.Length)
        {
            InvalidSchema2("family identifiers must be globally unique.");
        }

        if (source.Palettes!.Length != 1)
        {
            InvalidSchema2("schema-v2 requires exactly one palette.");
        }

        var palette = source.Palettes![0]!;
        if (palette.Entries!.Length == 0)
        {
            InvalidSchema2("schema-v2 palette entries cannot be empty.");
        }

        var paletteRoles = new HashSet<string>(palette.Roles!.Keys, StringComparer.Ordinal);
        foreach (var role in palette.Roles!)
        {
            if (string.IsNullOrWhiteSpace(role.Key) || role.Value >= palette.Entries!.Length)
            {
                InvalidSchema2("palette role data is invalid.");
            }
        }

        foreach (var family in source.Families!)
        {
            var generation = ParseGenerationStrategy(family.Generation, source.SchemaVersion);
            _ = ParseConcreteIdentity(family.Identity);
            ValidateVariantCount(family.VariantCount, $"family '{family.Id}'");
            if (family.Targets!.Length == 0)
            {
                InvalidSchema2($"family '{family.Id}' has no targets.");
            }

            var nativeSizes = new HashSet<int>();
            foreach (var target in family.Targets!)
            {
                if (target.NativeSize != 20 || !nativeSizes.Add(target.NativeSize))
                {
                    InvalidSchema2(
                        $"family '{family.Id}' targets must use nativeSize 20 exactly once.");
                }

                ValidateTargetSchema2(target, generation, paletteRoles, family.Id!);
            }
        }

        foreach (var family in source.ConnectedFamilies ?? [])
        {
            ValidateVariantCount(family.VariantCount, $"connected family '{family.Id}'");
            if (family.NativeSizes!.Length != 1 || family.NativeSizes![0] != 20)
            {
                InvalidSchema2(
                    $"connected family '{family.Id}' must use nativeSize 20 exactly once.");
            }

            if (family.PaletteRoles!.Length == 0)
            {
                InvalidSchema2(
                    $"connected family '{family.Id}' paletteRoles cannot be empty.");
            }

            ValidatePaletteRole(
                family.PaletteRole,
                paletteRoles,
                $"connected family '{family.Id}' paletteRole");
            foreach (var paletteRole in family.PaletteRoles!)
            {
                ValidatePaletteRole(
                    paletteRole,
                    paletteRoles,
                    $"connected family '{family.Id}' paletteRoles");
            }

            ValidatePaletteRole(
                family.OverviewPaletteRole,
                paletteRoles,
                $"connected family '{family.Id}' overviewPaletteRole");
        }

        foreach (var motif in source.Motifs ?? [])
        {
            ValidateVariantCount(motif.VariantCount!.Value, $"motif '{motif.FamilyId}'");
            if (motif.Marks!.Length == 0)
            {
                InvalidSchema2($"motif '{motif.FamilyId}' has no marks.");
            }

            var footprint = PairSize(motif.Footprint!, "footprint");
            if (footprint.Width <= 0 || footprint.Height <= 0)
            {
                InvalidSchema2($"motif '{motif.FamilyId}' footprint is invalid.");
            }
            if (motif.OccupancyTags!.Length == 0 ||
                motif.OccupancyTags.Any(string.IsNullOrWhiteSpace) ||
                motif.OccupancyTags.Distinct(StringComparer.Ordinal).Count() !=
                    motif.OccupancyTags.Length)
            {
                InvalidSchema2(
                    $"motif '{motif.FamilyId}' occupancyTags must be nonempty, " +
                    "nonblank, and unique.");
            }

            var anchorCell = PairPoint(motif.AnchorCell!, "anchorCell");
            ValidateCell(anchorCell, footprint, $"motif '{motif.FamilyId}' anchorCell");
            foreach (var mark in motif.Marks!)
            {
                if (mark.VariantOrdinal!.Value < 0 ||
                    mark.VariantOrdinal!.Value >= motif.VariantCount!.Value)
                {
                    InvalidSchema2(
                        $"motif '{motif.FamilyId}' mark variantOrdinal is invalid.");
                }

                ValidateCell(
                    PairPoint(mark.Cell!, "cell"),
                    footprint,
                    $"motif '{motif.FamilyId}' mark cell");
                _ = PairPoint(mark.PixelOffset!, "pixelOffset");
            }
        }

        ValidateMotifReviewBounds(source.Motifs ?? []);
    }

    private static void ValidateMotifReviewBounds(SourceMotif[] motifs)
    {
        const int nativeSize = 20;
        var width = (long)nativeSize;
        long height = 0;
        foreach (var motif in motifs)
        {
            var footprint = PairSize(motif.Footprint!, "footprint");
            var motifWidth = (long)footprint.Width * nativeSize;
            var motifHeight =
                (long)footprint.Height * nativeSize * motif.VariantCount!.Value;
            if (motifWidth > ReviewLimits.MaximumCanvasPixels ||
                motifHeight > ReviewLimits.MaximumCanvasPixels ||
                height > ReviewLimits.MaximumCanvasPixels - motifHeight)
            {
                InvalidSchema2(
                    "motif review output exceeds the bounded authoring canvas.");
            }

            width = Math.Max(width, motifWidth);
            height += motifHeight;
        }

        height = Math.Max(height, nativeSize);
        if (width > ReviewLimits.MaximumCanvasPixels / height)
        {
            InvalidSchema2(
                "motif review output exceeds the bounded authoring canvas.");
        }
    }

    private static void ValidateTargetSchema2(
        SourceTarget target,
        GenerationStrategy generation,
        HashSet<string> paletteRoles,
        string familyId)
    {
        if (target.PaletteRoles!.Length == 0)
        {
            InvalidSchema2($"family '{familyId}' target paletteRoles cannot be empty.");
        }

        foreach (var paletteRole in target.PaletteRoles!)
        {
            ValidatePaletteRole(
                paletteRole,
                paletteRoles,
                $"family '{familyId}' target paletteRoles");
        }

        ValidatePaletteRole(
            target.OverviewPaletteRole,
            paletteRoles,
            $"family '{familyId}' target overviewPaletteRole");

        var anchor = PairPoint(target.Anchor!, "anchor");
        if (anchor.X < 0 || anchor.X >= target.NativeSize ||
            anchor.Y < 0 || anchor.Y >= target.NativeSize)
        {
            InvalidSchema2($"family '{familyId}' target anchor is outside its native size.");
        }

        if (generation == GenerationStrategy.RoleMask)
        {
            ValidateRoleMaskRows(target.Rows!, target.PaletteRoles!.Length, familyId);
        }

        if (target.Rows is { Length: > 0 })
        {
            var width = target.Rows[0].Length;
            if (anchor.X >= width || anchor.Y >= target.Rows.Length)
            {
                InvalidSchema2(
                    $"family '{familyId}' target anchor is outside its authored rows.");
            }
        }
    }

    private static void ValidateRoleMaskRows(
        string[] rows,
        int paletteRoleCount,
        string familyId)
    {
        if (rows.Length == 0 || paletteRoleCount is < 1 or > 9)
        {
            InvalidSchema2($"family '{familyId}' role-mask rows are invalid.");
        }

        var width = rows[0]?.Length ?? 0;
        if (width == 0 || width > 20 || rows.Length > 20)
        {
            InvalidSchema2($"family '{familyId}' role-mask rows are invalid.");
        }

        foreach (var row in rows)
        {
            if (row is null || row.Length != width)
            {
                InvalidSchema2(
                    $"family '{familyId}' role-mask rows must be rectangular.");
            }

            foreach (var value in row)
            {
                if (value is < '0' or > '9' || value - '0' > paletteRoleCount)
                {
                    InvalidSchema2(
                        $"family '{familyId}' role-mask rows use an invalid palette digit.");
                }
            }
        }
    }

    private static void ValidatePaletteRole(
        string? paletteRole,
        HashSet<string> paletteRoles,
        string member)
    {
        if (string.IsNullOrWhiteSpace(paletteRole) ||
            !paletteRoles.Contains(paletteRole))
        {
            InvalidSchema2($"{member} is not present in the palette.");
        }
    }

    private static void ValidateVariantCount(int variantCount, string subject)
    {
        if (variantCount is < 1 or > 16)
        {
            InvalidSchema2($"{subject} variantCount must be in the range 1..16.");
        }
    }

    private static void ValidateCell(PixelPoint cell, PixelSize footprint, string member)
    {
        if (cell.X < 0 || cell.X >= footprint.Width ||
            cell.Y < 0 || cell.Y >= footprint.Height)
        {
            InvalidSchema2($"{member} is outside the footprint.");
        }
    }

    [DoesNotReturn]
    private static void MissingRequiredMember() => throw new FormatException(
        "CVC-CAT-004: catalogue is missing a required member.");

    [DoesNotReturn]
    private static void InvalidSchema2(string message) => throw new FormatException(
        $"CVC-CAT-005: {message}");

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
        string? PackId,
        int VisualStyleVersion,
        SourcePalette[]? Palettes,
        SourceFamily[]? Families,
        SourceConnectedFamily[]? ConnectedFamilies = null,
        SourceMotif[]? Motifs = null);

    private sealed record SourcePalette(
        string? Id,
        string[]? Entries,
        Dictionary<string, byte>? Roles);

    private sealed record SourceFamily(
        string? Id,
        ulong Seed,
        int VariantCount,
        SourceTarget[]? Targets,
        string? Generation = null,
        string? Identity = null);

    private sealed record SourceTarget(
        int NativeSize,
        string[]? Rows,
        int[]? Anchor,
        string? Layer,
        string[]? PaletteRoles,
        string? OverviewPaletteRole = null,
        bool RequireConnected = true);

    private sealed record SourceConnectedFamily(
        string? Id,
        ulong Seed,
        int VariantCount,
        int[]? NativeSizes,
        string? Layer,
        string? PaletteRole,
        bool RequireEdgeContinuity,
        int[]? Masks = null,
        int? FallbackMask = null,
        string? TransitionOwnership = null,
        string[]? PaletteRoles = null,
        string? OverviewPaletteRole = null,
        string? MaterialTreatment = null,
        string? Identity = null);

    private sealed record SourceMotif(
        string? FamilyId,
        int[]? Footprint,
        int[]? AnchorCell,
        string[]? OccupancyTags,
        string? ClippingBehavior,
        SourceMotifMark[]? Marks,
        ulong? Seed = null,
        int? VariantCount = null);

    private sealed record SourceMotifMark(
        string? VisualId,
        int[]? Cell,
        int[]? PixelOffset,
        int? VariantOrdinal = null);

    private sealed record NormalizedCatalogue(
        int SchemaVersion,
        string PackId,
        int VisualStyleVersion,
        ImmutableArray<CataloguePalette> Palettes,
        ImmutableArray<CatalogueFamily> Families,
        ImmutableArray<CatalogueConnectedFamily> ConnectedFamilies,
        ImmutableArray<CatalogueMotif> Motifs);
}
