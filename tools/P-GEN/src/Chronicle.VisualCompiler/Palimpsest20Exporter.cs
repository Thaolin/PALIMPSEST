using System.Collections.Immutable;
using Chronicle.VisualPack;

namespace Chronicle.VisualCompiler;

public sealed record Palimpsest20CompilationResult(
    Palimpsest20Pack? Pack,
    Palimpsest20Validation? Validation,
    ImmutableArray<CompilerDiagnostic> Diagnostics,
    string NormalizedSourceDigest,
    ImmutableArray<ReviewFile> ReviewFiles)
{
    public bool Succeeded =>
        Pack is not null &&
        Validation is not null &&
        Diagnostics.All(static diagnostic =>
            diagnostic.Severity != DiagnosticSeverity.Error);
}

internal static class Palimpsest20Exporter
{
    private const int CellSize = Palimpsest20Pack.NativeCellSize;
    private const int ShelfColumns = 8;
    private const string AtlasId = "chronicle.world-20";

    private static readonly ImmutableArray<string> AuthoritativeRoleNames =
    [
        "actor.dark",
        "actor.primary",
        "actor.read",
        "cloud.primary",
        "cloud.shadow",
        "emphasis.active",
        "landmark.bright",
        "landmark.gold",
        "sky.deep",
        "sky.ground",
        "stone.dark",
        "stone.primary",
        "surface.dark",
        "surface.grass",
        "surface.soil",
        "ui.light",
        "water.deep",
        "water.primary",
        "water.shine",
    ];

    internal static (Palimpsest20Pack Pack, Palimpsest20Validation Validation)
        Export(
            CompiledVisualPack source,
            VisualCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(catalogue);

        ValidateSource(source, catalogue);
        var palette = source.Palettes[0];
        var paletteRoles = BuildPaletteRoleIndexes(palette);
        var sourceVisuals = BuildSourceVisualIndex(source);
        var sourceAtlases = BuildSourceAtlasIndex(source);
        var concrete = BuildConcreteVisuals(
            catalogue,
            sourceVisuals,
            paletteRoles);

        if (concrete.Count == 0)
        {
            throw new FormatException(
                "CVC-PAL20-002: no exportable Palimpsest20 definitions were produced.");
        }

        var duplicate = concrete
            .GroupBy(static visual => visual.Id, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new FormatException(
                $"CVC-PAL20-003: duplicate concrete visual ID '{duplicate.Key}'.");
        }

        concrete.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.Id, right.Id));

        var atlasWidth = checked(ShelfColumns * CellSize);
        var atlasHeight = checked(
            ((concrete.Count + ShelfColumns - 1) / ShelfColumns) * CellSize);
        var atlasIndices = new byte[checked(atlasWidth * atlasHeight)];
        var definitions = new List<Palimpsest20Definition>(concrete.Count);

        for (var ordinal = 0; ordinal < concrete.Count; ordinal++)
        {
            var visual = concrete[ordinal];
            var cellX = ordinal % ShelfColumns * CellSize;
            var cellY = ordinal / ShelfColumns * CellSize;
            CopySourceRaster(
                visual,
                sourceAtlases,
                atlasIndices,
                atlasWidth,
                cellX,
                cellY);

            definitions.Add(new Palimpsest20Definition(
                visual.Id,
                new Palimpsest20AtlasRect(cellX, cellY, CellSize, CellSize),
                visual.FamilyId,
                visual.CanonicalVariantOrdinal,
                visual.LayerClass,
                new Palimpsest20PixelAnchor(10, 10),
                visual.AdjacencyMask is int mask
                    ? (Palimpsest20AdjacencyMask)mask
                    : null,
                visual.OverviewPaletteIndex,
                visual.PaletteRoleIndexes));
        }

        var pack = new Palimpsest20Pack(
            source.PackId,
            Palimpsest20Pack.SupportedFormatVersion,
            Palimpsest20Pack.SupportedStyleVersion,
            Palimpsest20Pack.SupportedComposerVersion,
            CellSize,
            AtlasId,
            palette.Id,
            atlasWidth,
            atlasHeight,
            atlasIndices,
            palette.Entries.Select(static entry => new Palimpsest20PaletteColor(
                entry.R,
                entry.G,
                entry.B,
                entry.A)).ToArray(),
            AuthoritativeRoleNames.ToDictionary(
                static name => name,
                name => RequirePaletteRole(paletteRoles, name, "palette"),
                StringComparer.Ordinal),
            definitions);
        var validation = new Palimpsest20Validation(
            PackFormatVersion: Palimpsest20Pack.SupportedFormatVersion,
            ComposerContractVersion: Palimpsest20Pack.SupportedComposerVersion,
            VisualStyleVersion: Palimpsest20Pack.SupportedStyleVersion,
            MinimumReaderVersion: "1.0.0");

        return (pack, validation);
    }

    private static void ValidateSource(
        CompiledVisualPack source,
        VisualCatalogue catalogue)
    {
        if (!StringComparer.Ordinal.Equals(source.PackId, catalogue.PackId))
        {
            throw new FormatException(
                "CVC-PAL20-004: the source pack ID does not match the catalogue.");
        }

        if (source.Palettes.Length != 1)
        {
            throw new FormatException(
                "CVC-PAL20-005: Palimpsest20 requires exactly one source palette.");
        }

        if (source.RequiredNativeSizes.Length != 1 ||
            source.RequiredNativeSizes[0] != CellSize ||
            source.Atlases.Any(static atlas => atlas.NativeSize != CellSize) ||
            source.Visuals.Any(static visual => visual.NativeSize != CellSize))
        {
            throw new FormatException(
                "CVC-PAL20-006: Palimpsest20 requires only native-size 20 source visuals.");
        }

        if (source.Compatibility.PackFormatVersion !=
                Palimpsest20Pack.SupportedFormatVersion ||
            source.Compatibility.ComposerContractVersion !=
                Palimpsest20Pack.SupportedComposerVersion ||
            source.Compatibility.VisualStyleVersion !=
                Palimpsest20Pack.SupportedStyleVersion)
        {
            throw new FormatException(
                "CVC-PAL20-007: Palimpsest20 requires source compatibility versions 1.");
        }
    }

    private static Dictionary<string, int> BuildPaletteRoleIndexes(
        PaletteRecord palette)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var role in palette.Roles)
        {
            if (!result.TryAdd(role.Name, role.Index))
            {
                throw new FormatException(
                    $"CVC-PAL20-008: duplicate source palette role '{role.Name}'.");
            }

            if (role.Index >= palette.Entries.Length)
            {
                throw new FormatException(
                    $"CVC-PAL20-009: palette role '{role.Name}' has an invalid index.");
            }
        }

        return result;
    }

    private static Dictionary<SourceTuple, VisualRecord> BuildSourceVisualIndex(
        CompiledVisualPack source)
    {
        var result = new Dictionary<SourceTuple, VisualRecord>();
        foreach (var visual in source.Visuals)
        {
            var tuple = new SourceTuple(
                visual.FamilyId,
                visual.VariantOrdinal,
                visual.AdjacencyMask);
            if (!result.TryAdd(tuple, visual))
            {
                throw new FormatException(
                    "CVC-PAL20-010: source contains duplicate visual tuples.");
            }
        }

        return result;
    }

    private static Dictionary<string, SourceAtlas> BuildSourceAtlasIndex(
        CompiledVisualPack source)
    {
        var result = new Dictionary<string, SourceAtlas>(StringComparer.Ordinal);
        foreach (var atlas in source.Atlases)
        {
            if (atlas.Width <= 0 || atlas.Height <= 0)
            {
                throw new FormatException(
                    $"CVC-PAL20-011: source atlas '{atlas.Id}' has invalid dimensions.");
            }

            var indices = source.GetAtlasIndices(atlas.Id);
            if (indices.Length != checked(atlas.Width * atlas.Height))
            {
                throw new FormatException(
                    $"CVC-PAL20-012: source atlas '{atlas.Id}' has an invalid buffer length.");
            }

            if (!result.TryAdd(atlas.Id, new SourceAtlas(atlas, indices)))
            {
                throw new FormatException(
                    $"CVC-PAL20-013: duplicate source atlas '{atlas.Id}'.");
            }
        }

        return result;
    }

    private static List<ConcreteVisual> BuildConcreteVisuals(
        VisualCatalogue catalogue,
        IReadOnlyDictionary<SourceTuple, VisualRecord> sourceVisuals,
        IReadOnlyDictionary<string, int> paletteRoles)
    {
        var result = new List<ConcreteVisual>();

        foreach (var family in catalogue.Families)
        {
            if (family.Identity == ConcreteIdentity.ReviewOnly)
            {
                continue;
            }

            if (family.Targets.IsEmpty)
            {
                throw new FormatException(
                    $"CVC-PAL20-014: family '{family.Id}' has no 20px target.");
            }

            foreach (var target in family.Targets)
            {
                if (target.NativeSize != CellSize)
                {
                    throw new FormatException(
                        $"CVC-PAL20-015: family '{family.Id}' is not native-size 20.");
                }

                var metadata = BuildPaletteMetadata(
                    family.Id,
                    target.OverviewPaletteRole,
                    target.PaletteRoles,
                    paletteRoles);
                var layer = MapLayer(family.Id, target.Layer);

                switch (family.Identity)
                {
                    case ConcreteIdentity.Single:
                        RequireVariantCount(family.Id, family.VariantCount, 1);
                        result.Add(CreateConcreteVisual(
                            family.Id,
                            family.Id,
                            sourceVariantOrdinal: 0,
                            adjacencyMask: null,
                            canonicalVariantOrdinal: 0,
                            layer,
                            metadata,
                            sourceVisuals));
                        break;

                    case ConcreteIdentity.Variant:
                        RequireAtLeastOneVariant(family.Id, family.VariantCount);
                        for (var variant = 0; variant < family.VariantCount; variant++)
                        {
                            result.Add(CreateConcreteVisual(
                                family.Id,
                                VariantId(family.Id, variant),
                                variant,
                                null,
                                variant,
                                layer,
                                metadata,
                                sourceVisuals));
                        }
                        break;

                    default:
                        throw new FormatException(
                            $"CVC-PAL20-016: family '{family.Id}' has unsupported identity '{family.Identity}'.");
                }
            }
        }

        foreach (var family in catalogue.ConnectedFamilies)
        {
            if (family.Identity == ConcreteIdentity.ReviewOnly)
            {
                continue;
            }

            if (family.NativeSizes.Length != 1 || family.NativeSizes[0] != CellSize)
            {
                throw new FormatException(
                    $"CVC-PAL20-017: connected family '{family.Id}' is not native-size 20.");
            }

            var metadata = BuildPaletteMetadata(
                family.Id,
                family.OverviewPaletteRole,
                family.PaletteRoles,
                paletteRoles);
            var layer = MapLayer(family.Id, family.Layer);
            ValidateMasks(family.Id, family.Masks);

            switch (family.Identity)
            {
                case ConcreteIdentity.AlwaysTwoDigitMask:
                    RequireVariantCount(family.Id, family.VariantCount, 1);
                    foreach (var mask in family.Masks)
                    {
                        result.Add(CreateConcreteVisual(
                            family.Id,
                            $"{family.Id}.{mask:00}",
                            sourceVariantOrdinal: 0,
                            adjacencyMask: mask,
                            canonicalVariantOrdinal: mask,
                            layer,
                            metadata,
                            sourceVisuals));
                    }
                    break;

                case ConcreteIdentity.MaskedVariant:
                    RequireAtLeastOneVariant(family.Id, family.VariantCount);
                    foreach (var mask in family.Masks)
                    {
                        for (var variant = 0; variant < family.VariantCount; variant++)
                        {
                            result.Add(CreateConcreteVisual(
                                family.Id,
                                MaskedVariantId(family.Id, mask, variant),
                                variant,
                                mask,
                                variant,
                                layer,
                                metadata,
                                sourceVisuals));
                        }
                    }
                    break;

                case ConcreteIdentity.Masked:
                    RequireVariantCount(family.Id, family.VariantCount, 1);
                    foreach (var mask in family.Masks)
                    {
                        result.Add(CreateConcreteVisual(
                            family.Id,
                            MaskedId(family.Id, mask),
                            sourceVariantOrdinal: 0,
                            adjacencyMask: mask,
                            canonicalVariantOrdinal: mask,
                            layer,
                            metadata,
                            sourceVisuals));
                    }
                    break;

                default:
                    throw new FormatException(
                        $"CVC-PAL20-018: connected family '{family.Id}' has unsupported identity '{family.Identity}'.");
            }
        }

        return result;
    }

    private static PaletteMetadata BuildPaletteMetadata(
        string familyId,
        string overviewPaletteRole,
        ImmutableArray<string> paletteRoleNames,
        IReadOnlyDictionary<string, int> paletteRoles)
    {
        if (string.IsNullOrWhiteSpace(overviewPaletteRole) ||
            paletteRoleNames.IsDefaultOrEmpty)
        {
            throw new FormatException(
                $"CVC-PAL20-019: '{familyId}' is missing palette metadata.");
        }

        return new PaletteMetadata(
            RequirePaletteRole(paletteRoles, overviewPaletteRole, familyId),
            paletteRoleNames.Select(role =>
                RequirePaletteRole(paletteRoles, role, familyId)).ToImmutableArray());
    }

    private static int RequirePaletteRole(
        IReadOnlyDictionary<string, int> paletteRoles,
        string roleName,
        string subject)
    {
        if (string.IsNullOrWhiteSpace(roleName) ||
            !paletteRoles.TryGetValue(roleName, out var index))
        {
            throw new FormatException(
                $"CVC-PAL20-020: '{subject}' references missing palette role '{roleName}'.");
        }

        return index;
    }

    private static ConcreteVisual CreateConcreteVisual(
        string familyId,
        string id,
        int sourceVariantOrdinal,
        int? adjacencyMask,
        int canonicalVariantOrdinal,
        Palimpsest20LayerClass layer,
        PaletteMetadata metadata,
        IReadOnlyDictionary<SourceTuple, VisualRecord> sourceVisuals)
    {
        var tuple = new SourceTuple(familyId, sourceVariantOrdinal, adjacencyMask);
        if (!sourceVisuals.TryGetValue(tuple, out var source))
        {
            throw new FormatException(
                $"CVC-PAL20-021: source tuple '{familyId}:20:{sourceVariantOrdinal}:{adjacencyMask}' is missing.");
        }

        return new ConcreteVisual(
            id,
            familyId,
            source,
            canonicalVariantOrdinal,
            layer,
            adjacencyMask,
            metadata.OverviewPaletteIndex,
            metadata.PaletteRoleIndexes);
    }

    private static Palimpsest20LayerClass MapLayer(string familyId, VisualLayer layer) =>
        layer switch
        {
            VisualLayer.Ground => Palimpsest20LayerClass.GroundField,
            VisualLayer.Adjacency => Palimpsest20LayerClass.Adjacency,
            VisualLayer.Feature => Palimpsest20LayerClass.EnvironmentalFeature,
            VisualLayer.Landmark => Palimpsest20LayerClass.LandmarkOrSubject,
            VisualLayer.Actor => Palimpsest20LayerClass.Actor,
            VisualLayer.Effect => Palimpsest20LayerClass.TemporaryAction,
            VisualLayer.Emphasis => Palimpsest20LayerClass.TargetOrSelection,
            VisualLayer.Overlay => Palimpsest20LayerClass.UiGlyph,
            _ => throw new FormatException(
                $"CVC-PAL20-022: exported family '{familyId}' has unsupported layer '{layer}'.")
        };

    private static void RequireVariantCount(
        string familyId,
        int actual,
        int expected)
    {
        if (actual != expected)
        {
            throw new FormatException(
                $"CVC-PAL20-023: '{familyId}' requires exactly {expected} variant(s).");
        }
    }

    private static void RequireAtLeastOneVariant(string familyId, int count)
    {
        if (count < 1)
        {
            throw new FormatException(
                $"CVC-PAL20-024: '{familyId}' requires at least one variant.");
        }
    }

    private static void ValidateMasks(string familyId, ImmutableArray<int> masks)
    {
        if (masks.IsDefaultOrEmpty ||
            masks.Any(static mask => mask is < 0 or > 15) ||
            masks.Distinct().Count() != masks.Length)
        {
            throw new FormatException(
                $"CVC-PAL20-025: '{familyId}' has unsupported mask cardinality.");
        }
    }

    private static string VariantId(string familyId, int variant) =>
        variant == 0 ? familyId : $"{familyId}.v{variant}";

    private static string MaskedId(string familyId, int mask) =>
        mask == 0 ? familyId : $"{familyId}.mask.{mask:00}";

    private static string MaskedVariantId(
        string familyId,
        int mask,
        int variant)
    {
        var masked = MaskedId(familyId, mask);
        return variant == 0 ? masked : $"{masked}.v{variant}";
    }

    private static void CopySourceRaster(
        ConcreteVisual visual,
        IReadOnlyDictionary<string, SourceAtlas> sourceAtlases,
        byte[] destination,
        int destinationWidth,
        int destinationX,
        int destinationY)
    {
        if (!sourceAtlases.TryGetValue(visual.Source.AtlasId, out var sourceAtlas))
        {
            throw new FormatException(
                $"CVC-PAL20-026: source atlas '{visual.Source.AtlasId}' is missing.");
        }

        var rectangle = visual.Source.Rectangle;
        if (rectangle.X < 0 || rectangle.Y < 0 ||
            rectangle.Width < 0 || rectangle.Height < 0 ||
            rectangle.X + rectangle.Width > sourceAtlas.Atlas.Width ||
            rectangle.Y + rectangle.Height > sourceAtlas.Atlas.Height)
        {
            throw new FormatException(
                $"CVC-PAL20-027: source rectangle for '{visual.Id}' is invalid.");
        }

        var cellHasVisiblePixel = false;
        for (var sourceY = 0; sourceY < rectangle.Height; sourceY++)
        {
            for (var sourceX = 0; sourceX < rectangle.Width; sourceX++)
            {
                var targetX = 10 - visual.Source.Anchor.X + sourceX;
                var targetY = 10 - visual.Source.Anchor.Y + sourceY;
                if (targetX is < 0 or >= CellSize ||
                    targetY is < 0 or >= CellSize)
                {
                    continue;
                }

                var index = sourceAtlas.Indices.Span[
                    (rectangle.Y + sourceY) * sourceAtlas.Atlas.Width +
                    rectangle.X + sourceX];
                destination[(destinationY + targetY) * destinationWidth +
                    destinationX + targetX] = index;
                cellHasVisiblePixel |= index != 0;
            }
        }

        if (!cellHasVisiblePixel)
        {
            throw new FormatException(
                $"CVC-PAL20-028: copied cell '{visual.Id}' is empty.");
        }
    }

    private sealed record SourceTuple(
        string FamilyId,
        int VariantOrdinal,
        int? AdjacencyMask);

    private sealed record SourceAtlas(
        AtlasRecord Atlas,
        ReadOnlyMemory<byte> Indices);

    private sealed record PaletteMetadata(
        int OverviewPaletteIndex,
        ImmutableArray<int> PaletteRoleIndexes);

    private sealed record ConcreteVisual(
        string Id,
        string FamilyId,
        VisualRecord Source,
        int CanonicalVariantOrdinal,
        Palimpsest20LayerClass LayerClass,
        int? AdjacencyMask,
        int OverviewPaletteIndex,
        ImmutableArray<int> PaletteRoleIndexes);
}
