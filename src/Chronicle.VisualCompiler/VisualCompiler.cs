using System.Collections.Immutable;
using Chronicle.VisualPack;

namespace Chronicle.VisualCompiler;

public enum ReviewMode
{
    None,
    Standard
}

public sealed record CompilationOptions(ReviewMode ReviewMode);
public sealed record CompilerDiagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Subject,
    string Message);
public sealed record ReviewFile(string Path, ImmutableArray<byte> Bytes);

internal sealed record CompilationResult(
    CompiledVisualPack? Pack,
    ImmutableArray<CompilerDiagnostic> Diagnostics,
    string NormalizedSourceDigest,
    string PackDigest,
    ImmutableArray<ReviewFile> ReviewFiles)
{
    public bool Succeeded =>
        Pack is not null &&
        Diagnostics.All(static diagnostic =>
            diagnostic.Severity != DiagnosticSeverity.Error);
}

public static class VisualCompiler
{
    public static Palimpsest20CompilationResult CompilePalimpsest20(
        VisualCatalogue catalogue,
        CompilationOptions options)
    {
        var authoring = Compile(catalogue, options);
        if (!authoring.Succeeded || authoring.Pack is null)
        {
            return new Palimpsest20CompilationResult(
                null,
                null,
                authoring.Diagnostics,
                authoring.NormalizedSourceDigest,
                authoring.ReviewFiles);
        }

        try
        {
            var exported = Palimpsest20Exporter.Export(authoring.Pack, catalogue);
            return new Palimpsest20CompilationResult(
                exported.Pack,
                exported.Validation,
                authoring.Diagnostics,
                authoring.NormalizedSourceDigest,
                authoring.ReviewFiles);
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or InvalidOperationException)
        {
            return new Palimpsest20CompilationResult(
                null,
                null,
                authoring.Diagnostics.Add(new CompilerDiagnostic(
                    "CVC-PAL20-001",
                    DiagnosticSeverity.Error,
                    catalogue.PackId,
                    exception.Message)),
                authoring.NormalizedSourceDigest,
                ImmutableArray<ReviewFile>.Empty);
        }
    }

    internal static CompilationResult Compile(
        VisualCatalogue catalogue,
        CompilationOptions options)
    {
        var diagnostics = new List<CompilerDiagnostic>();
        var sourceDigest = PackDigests.Bytes(catalogue.NormalizedBytes.AsSpan());
        if (catalogue.SchemaVersion is not (1 or 2))
        {
            diagnostics.Add(new CompilerDiagnostic(
                "CVC-CAT-003",
                DiagnosticSeverity.Error,
                catalogue.PackId,
                "Unsupported catalogue schema version."));
            return Failure();
        }

        if (catalogue.Palettes.IsEmpty)
        {
            diagnostics.Add(new CompilerDiagnostic(
                "CVC-PAL-002",
                DiagnosticSeverity.Error,
                catalogue.PackId,
                "At least one palette is required."));
            return Failure();
        }
        if (catalogue.Families.Any(static family =>
                family.VariantCount is < 1 or > 16) ||
            catalogue.ConnectedFamilies.Any(static family =>
                family.VariantCount is < 1 or > 16))
        {
            diagnostics.Add(new CompilerDiagnostic(
                "CVC-VAR-002",
                DiagnosticSeverity.Error,
                catalogue.PackId,
                "Variant count must be in the range 1 through 16."));
            return Failure();
        }

        try
        {
            var palette = catalogue.Palettes[0];
            var packPalettes = catalogue.Palettes
                .Select(static item => new PaletteRecord(
                    item.Id,
                    item.Entries,
                    item.Roles,
                    0,
                    PackDigests.Palette(item.Entries)))
                .ToImmutableArray();
            var packPalette = packPalettes[0];
            var compatiblePalettes = packPalettes
                .Select(static item => item.Id)
                .ToImmutableArray();
            var primaryRoles = packPalette.Roles.ToDictionary(
                static role => role.Name,
                static role => role.Index,
                StringComparer.Ordinal);
            foreach (var candidate in packPalettes.Skip(1))
            {
                var candidateRoles = candidate.Roles.ToDictionary(
                    static role => role.Name,
                    static role => role.Index,
                    StringComparer.Ordinal);
                if (primaryRoles.Count != candidateRoles.Count ||
                    primaryRoles.Any(role =>
                        !candidateRoles.TryGetValue(role.Key, out var index) ||
                        index != role.Value))
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        "CVC-PAL-004",
                        DiagnosticSeverity.Error,
                        candidate.Id,
                        "Compatible palettes must use identical named-role indices."));
                }
            }
            if (diagnostics.Any(static item =>
                    item.Severity == DiagnosticSeverity.Error))
            {
                return Failure();
            }
            var atlases = ImmutableArray.CreateBuilder<AtlasRecord>();
            var visuals = ImmutableArray.CreateBuilder<VisualRecord>();
            var adjacencies = ImmutableArray.CreateBuilder<AdjacencyRecord>();
            var buffers = new List<KeyValuePair<string, ReadOnlyMemory<byte>>>();
            var generatedGeometry = new Dictionary<(string Family, int NativeSize), HashSet<string>>();

            foreach (var targetGroup in catalogue.Families
                         .SelectMany(static family => family.Targets.SelectMany(target =>
                             Enumerable.Range(0, Math.Max(1, family.VariantCount))
                                 .Select(variant => (family, target, variant))))
                         .GroupBy(static item => item.target.NativeSize)
                         .OrderBy(static group => group.Key))
            {
                var items = targetGroup.OrderBy(static item => item.family.Id, StringComparer.Ordinal)
                    .ToArray();
                var dimensions = items
                    .Select(static item => Dimensions(item.target))
                    .ToArray();
                if (items.Any(item =>
                        Dimensions(item.target).Width >
                            item.target.NativeSize ||
                        Dimensions(item.target).Height >
                            item.target.NativeSize))
                {
                    throw new FormatException(
                        "CVC-RASTER-004: authored silhouette exceeds its native-size slot.");
                }
                const int shelfColumns = 8;
                var columns = Math.Min(shelfColumns, items.Length);
                var width = columns * targetGroup.Key;
                var height = ((items.Length + shelfColumns - 1) / shelfColumns) *
                    targetGroup.Key;
                var pixels = new byte[width * height];
                for (var itemIndex = 0; itemIndex < items.Length; itemIndex++)
                {
                    var (family, target, variant) = items[itemIndex];
                    var targetWidth = dimensions[itemIndex].Width;
                    var targetHeight = dimensions[itemIndex].Height;
                    var x = itemIndex % shelfColumns * target.NativeSize;
                    var y = itemIndex / shelfColumns * target.NativeSize;
                    Raster(
                        family,
                        target,
                        pixels,
                        width,
                        x,
                        y,
                        family.Seed,
                        variant,
                        primaryRoles);
                    var rectangle = new PixelRect(
                        x,
                        y,
                        targetWidth,
                        targetHeight);
                    var geometry = PackDigests.Geometry(
                        rectangle,
                        new PixelSize(targetWidth, targetHeight),
                        target.Anchor,
                        target.NativeSize,
                        null,
                        TransformFlags.None,
                        target.RequireConnected,
                        pixels,
                        width);
                    var geometrySet = generatedGeometry.GetValueOrDefault((
                        family.Id,
                        target.NativeSize));
                    if (geometrySet is null)
                    {
                        geometrySet = new HashSet<string>(StringComparer.Ordinal);
                        generatedGeometry.Add((family.Id, target.NativeSize), geometrySet);
                    }
                    if (!geometrySet.Add(geometry))
                    {
                        diagnostics.Add(new CompilerDiagnostic(
                            "CVC-VAR-003",
                            DiagnosticSeverity.Error,
                            $"{family.Id}:{target.NativeSize}:{variant}",
                            "Requested variants produced duplicate geometry."));
                    }
                    visuals.Add(new VisualRecord(
                        family.Id,
                        $"world-{target.NativeSize}",
                        rectangle,
                        new PixelSize(targetWidth, targetHeight),
                        target.Anchor,
                        target.Layer,
                        family.Id,
                        variant,
                        target.NativeSize,
                        null,
                        TransformFlags.None,
                        target.PaletteRoles,
                        target.RequireConnected,
                        geometry,
                        ImmutableArray.Create("authored")));
                }

                var atlasId = $"world-{targetGroup.Key}";
                atlases.Add(new AtlasRecord(
                    atlasId,
                    targetGroup.Key,
                    width,
                    height,
                    $"atlases/{atlasId}.indices",
                    compatiblePalettes,
                    0,
                    0,
                    PackDigests.Bytes(pixels)));
                buffers.Add(KeyValuePair.Create(atlasId, (ReadOnlyMemory<byte>)pixels));
            }

            foreach (var family in catalogue.ConnectedFamilies)
            {
                var paletteIndex = packPalette.Roles
                    .FirstOrDefault(role => role.Name == family.PaletteRole);
                var treatmentColours = family.PaletteRoles
                    .Select(role => packPalette.Roles
                        .FirstOrDefault(candidate => candidate.Name == role))
                    .ToArray();
                if (paletteIndex is null ||
                    treatmentColours.Any(static role => role is null))
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        "CVC-PAL-003",
                        DiagnosticSeverity.Error,
                        family.Id,
                        "Connected family references a missing palette role."));
                    continue;
                }

                foreach (var nativeSize in family.NativeSizes)
                {
                    const int columns = 8;
                    var itemCount = family.Masks.Length * family.VariantCount;
                    var width = columns * nativeSize;
                    var height = ((itemCount + columns - 1) / columns) * nativeSize;
                    var pixels = new byte[width * height];
                    var atlasId = $"{family.Id}-{nativeSize}";
                    for (var maskOrdinal = 0;
                         maskOrdinal < family.Masks.Length;
                         maskOrdinal++)
                    {
                        var mask = family.Masks[maskOrdinal];
                        var maskGeometry = new HashSet<string>(StringComparer.Ordinal);
                        for (var variant = 0; variant < family.VariantCount; variant++)
                        {
                            var ordinal = maskOrdinal * family.VariantCount + variant;
                            var x = ordinal % columns * nativeSize;
                            var y = ordinal / columns * nativeSize;
                            RasterConnected(
                                pixels,
                                width,
                                x,
                                y,
                                nativeSize,
                                mask,
                                treatmentColours
                                    .Select(static role => role!.Index)
                                    .ToArray(),
                                family.MaterialTreatment,
                                family.Seed,
                                variant);
                            var rectangle = new PixelRect(
                                x,
                                y,
                                nativeSize,
                                nativeSize);
                            var geometry = PackDigests.Geometry(
                                rectangle,
                                new PixelSize(nativeSize, nativeSize),
                                new PixelPoint(nativeSize / 2, nativeSize / 2),
                                nativeSize,
                                mask,
                                TransformFlags.None,
                                family.MaterialTreatment != MaterialTreatment.Transition,
                                pixels,
                                width);
                            if (!maskGeometry.Add(geometry))
                            {
                                diagnostics.Add(new CompilerDiagnostic(
                                    "CVC-VAR-003",
                                    DiagnosticSeverity.Error,
                                    $"{family.Id}:{nativeSize}:{mask}:{variant}",
                                    "Requested connected variants produced duplicate geometry."));
                            }
                            visuals.Add(new VisualRecord(
                                family.Id,
                                atlasId,
                                rectangle,
                                new PixelSize(nativeSize, nativeSize),
                                new PixelPoint(nativeSize / 2, nativeSize / 2),
                                family.Layer,
                                family.Id,
                                variant,
                                nativeSize,
                                mask,
                                TransformFlags.None,
                                family.PaletteRoles,
                                family.MaterialTreatment != MaterialTreatment.Transition,
                                geometry,
                                family.TransitionOwnership == TransitionOwnership.None
                                    ? ImmutableArray.Create("connected")
                                    : ImmutableArray.Create(
                                        "connected",
                                        "transition",
                                        "ownership:primary-ground-plus-one-transition")));
                        }
                    }
                    atlases.Add(new AtlasRecord(
                        atlasId,
                        nativeSize,
                        width,
                        height,
                        $"atlases/{atlasId}.indices",
                        compatiblePalettes,
                        0,
                        0,
                        PackDigests.Bytes(pixels)));
                    buffers.Add(KeyValuePair.Create(atlasId, (ReadOnlyMemory<byte>)pixels));
                }
                adjacencies.Add(new AdjacencyRecord(
                    family.Id,
                    Enumerable.Range(0, 16).ToImmutableArray(),
                    family.FallbackMask,
                    family.RequireEdgeContinuity));
            }

            var motifs = catalogue.Motifs.Select(static motif => new MotifRecord(
                motif.FamilyId,
                motif.Seed,
                motif.VariantCount,
                motif.Footprint,
                motif.AnchorCell,
                motif.Marks.Select(static mark => new MotifMark(
                    mark.VisualId,
                    mark.Cell,
                    mark.PixelOffset,
                    mark.VariantOrdinal)).ToImmutableArray(),
                motif.OccupancyTags,
                motif.ClippingBehavior == ClippingBehavior.Clip
                    ? MotifClippingBehavior.Clip
                    : MotifClippingBehavior.Reject)).ToImmutableArray();
            var definition = new CompiledVisualPack(
                catalogue.PackId,
                new CompatibilityRecord(
                    PackVersions.PackFormatVersion,
                    catalogue.SchemaVersion,
                    catalogue.VisualStyleVersion,
                    1,
                    "1.0.0"),
                new CompilerRecord("chronicle.visual-compiler", "0.1.0"),
                sourceDigest,
                packPalettes,
                atlases.ToImmutable(),
                visuals.ToImmutable(),
                motifs,
                adjacencies.ToImmutable(),
                catalogue.Families.Select(static family => family.Id)
                    .Concat(catalogue.ConnectedFamilies.Select(static family => family.Id))
                    .Order(StringComparer.Ordinal).ToImmutableArray(),
                catalogue.Families.SelectMany(static family => family.Targets)
                    .Select(static target => target.NativeSize)
                    .Concat(catalogue.ConnectedFamilies.SelectMany(
                        static family => family.NativeSizes))
                    .Distinct().Order().ToImmutableArray(),
                catalogue.Families.Select(static family =>
                    new ProvenanceRecord(
                        family.Id,
                        family.Generation == GenerationStrategy.RoleMask
                            ? "procedural"
                            : family.Id.StartsWith(
                                "baseline.",
                                StringComparison.Ordinal)
                                ? "manual-baseline"
                                : "authored",
                        family.Id,
                        "project",
                        family.Generation == GenerationStrategy.RoleMask
                            ? $"{family.Targets.Length} target parameter sets; " +
                              "0 authored pixel rows"
                            : family.Id.StartsWith(
                                "baseline.",
                                StringComparison.Ordinal)
                                ? $"{family.Targets.Sum(target => target.Rows.Length)} " +
                                  $"authored pixel rows across {family.Targets.Length} targets"
                                : null))
                    .Concat(catalogue.ConnectedFamilies.Select(static family =>
                        new ProvenanceRecord(
                            family.Id,
                            "procedural",
                            family.Id,
                            "project",
                            null)))
                    .ToImmutableArray(),
                buffers);
            var packDiagnostics = PackValidator.Validate(definition);
            diagnostics.AddRange(packDiagnostics.Select(static item => new CompilerDiagnostic(
                item.Code,
                item.Severity,
                item.Subject,
                item.Message)));
            if (diagnostics.Any(static item => item.Severity == DiagnosticSeverity.Error))
            {
                return Failure();
            }

            var encoded = PackCodec.WriteCanonical(definition);
            var pack = new CompiledVisualPack(
                definition.PackId,
                definition.Compatibility,
                definition.Compiler,
                definition.SourceDigest,
                definition.Palettes,
                definition.Atlases,
                definition.Visuals,
                definition.Motifs,
                definition.Adjacencies,
                definition.RequiredMappings,
                definition.RequiredNativeSizes,
                definition.Provenance,
                definition.Atlases.Select(atlas =>
                    KeyValuePair.Create(atlas.Id, definition.GetAtlasIndices(atlas.Id))),
                encoded.AggregateDigest);
            return new CompilationResult(
                pack,
                diagnostics.ToImmutableArray(),
                sourceDigest,
                encoded.AggregateDigest,
                options.ReviewMode == ReviewMode.Standard
                    ? ReviewRenderer.Render(pack)
                    : ImmutableArray<ReviewFile>.Empty);
        }
        catch (FormatException exception)
        {
            diagnostics.Add(new CompilerDiagnostic(
                "CVC-RASTER-001",
                DiagnosticSeverity.Error,
                catalogue.PackId,
                exception.Message));
            return Failure();
        }

        CompilationResult Failure() => new(
            null,
            diagnostics.OrderBy(static item => item.Code, StringComparer.Ordinal)
                .ThenBy(static item => item.Subject, StringComparer.Ordinal)
                .ToImmutableArray(),
            sourceDigest,
            "",
            ImmutableArray<ReviewFile>.Empty);
    }

    private static PixelSize Dimensions(CatalogueTarget target)
    {
        if (target.Rows.IsEmpty || target.Rows[0].Length == 0 ||
            target.Rows.Any(row => row.Length != target.Rows[0].Length))
        {
            throw new FormatException(
                "CVC-RASTER-002: target rows must form a non-empty rectangle.");
        }
        return new PixelSize(target.Rows[0].Length, target.Rows.Length);
    }

    private static void Raster(
        CatalogueFamily family,
        CatalogueTarget target,
        byte[] destination,
        int atlasWidth,
        int offsetX,
        int offsetY,
        ulong seed,
        int variant,
        IReadOnlyDictionary<string, byte> paletteRoles)
    {
        var dimensions = Dimensions(target);
        for (var y = 0; y < dimensions.Height; y++)
        {
            for (var x = 0; x < dimensions.Width; x++)
            {
                var value = target.Rows[y][x] - '0';
                if ((uint)value > 9)
                {
                    throw new FormatException(
                        "CVC-RASTER-003: rows use decimal palette indexes.");
                }
                destination[(offsetY + y) * atlasWidth + offsetX + x] =
                    family.Generation == GenerationStrategy.RoleMask && value != 0
                        ? paletteRoles[target.PaletteRoles[value - 1]]
                        : (byte)value;
            }
        }

        if (variant > 0)
        {
            ApplyVariant(
                destination,
                atlasWidth,
                offsetX,
                offsetY,
                seed,
                variant,
                dimensions.Width,
                dimensions.Height);
        }
    }

    private static void ApplyVariant(
        Span<byte> destination,
        int atlasWidth,
        int offsetX,
        int offsetY,
        ulong seed,
        int variant,
        int width,
        int height)
    {
        var occupied = new List<(int X, int Y)>();
        var empty = new List<(int X, int Y)>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (destination[(offsetY + y) * atlasWidth + offsetX + x] == 0)
                {
                    empty.Add((x, y));
                }
                else
                {
                    occupied.Add((x, y));
                }
            }
        }

        var mixed = Mix(seed + (ulong)variant);
        if (occupied.Count > 1)
        {
            for (var attempt = 0; attempt < occupied.Count; attempt++)
            {
                var candidate = occupied[(int)((mixed + (ulong)attempt) % (ulong)occupied.Count)];
                var index =
                    (offsetY + candidate.Y) * atlasWidth +
                    offsetX +
                    candidate.X;
                var old = destination[index];
                destination[index] = 0;
                if (PixelConnectivity.IsFourConnected(
                    destination,
                    atlasWidth,
                    new PixelRect(offsetX, offsetY, width, height)))
                {
                    return;
                }
                destination[index] = old;
            }
        }

        foreach (var candidate in empty.OrderBy(point =>
                     Mix(mixed ^ (ulong)(point.Y * width + point.X))))
        {
            var adjacent = occupied.Any(point =>
                Math.Abs(point.X - candidate.X) + Math.Abs(point.Y - candidate.Y) == 1);
            if (adjacent)
            {
                byte colour = 0;
                foreach (var point in occupied)
                {
                    colour = destination[
                        (offsetY + point.Y) * atlasWidth + offsetX + point.X];
                    if (colour != 0)
                    {
                        break;
                    }
                }
                destination[
                    (offsetY + candidate.Y) * atlasWidth +
                    offsetX +
                    candidate.X] = colour;
                return;
            }
        }

        throw new FormatException("CVC-VAR-001: requested variant cannot differ safely.");
    }

    private static ulong Mix(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    private static void RasterConnected(
        byte[] destination,
        int atlasWidth,
        int offsetX,
        int offsetY,
        int size,
        int mask,
        IReadOnlyList<byte> colours,
        MaterialTreatment treatment,
        ulong seed,
        int variant)
    {
        var center = size / 2;
        switch (treatment)
        {
            case MaterialTreatment.Water:
                Connections(2, Colour(1));
                FillBox(center - 4, center - 4, 9, 9, Colour(1));
                Horizontal(center - 3, center + 3, center - 2, Colour(2));
                Shore(Colour(0));
                break;
            case MaterialTreatment.Cloud:
                Connections(2, Colour(1));
                FillBox(center - 4, center - 3, 9, 7, Colour(1));
                FillBox(center - 2, center - 5, 5, 11, Colour(1));
                Horizontal(center - 4, center + 4, center + 3, Colour(0));
                Set(center - 4, center - 3, Colour(0));
                Set(center + 4, center - 2, Colour(0));
                break;
            case MaterialTreatment.Grove:
                Connections(1, Colour(0));
                FillBox(center - 4, center - 3, 9, 6, Colour(1));
                FillBox(center - 2, center - 5, 5, 10, Colour(1));
                Horizontal(center - 3, center + 3, center - 3, Colour(2));
                Vertical(center, center + 1, center + 5, Colour(3));
                break;
            case MaterialTreatment.Ridge:
                Connections(3, Colour(1));
                FillBox(center - 5, center - 4, 11, 9, Colour(1));
                Horizontal(center - 4, center + 4, center - 3, Colour(2));
                Horizontal(center - 5, center + 5, center + 3, Colour(0));
                break;
            case MaterialTreatment.Crossing:
                Connections(3, Colour(1));
                FillBox(center - 5, center - 4, 11, 9, Colour(1));
                if ((variant & 1) == 0)
                {
                    Vertical(center - 1, center - 5, center + 5, Colour(3));
                    Vertical(center, center - 5, center + 5, Colour(4));
                }
                else
                {
                    Horizontal(center - 5, center + 5, center - 1, Colour(3));
                    Horizontal(center - 5, center + 5, center, Colour(4));
                }
                break;
            case MaterialTreatment.Wall:
                Connections(3, Colour(1));
                FillBox(center - 5, center - 5, 11, 11, Colour(1));
                Horizontal(center - 5, center + 5, center - 2, Colour(0));
                Horizontal(center - 5, center + 5, center + 2, Colour(2));
                break;
            case MaterialTreatment.Path:
                Connections(0, Colour(1));
                FillBox(center - 1, center - 1, 3, 3, Colour(1));
                Set(center - 1, center - 1, Colour(0));
                break;
            case MaterialTreatment.Transition:
                Connections(1, Colour(1));
                FillBox(center - 2, center - 2, 5, 5, Colour(1));
                Shore(Colour(0));
                Horizontal(center - 2, center + 2, center, Colour(2));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(treatment),
                    treatment,
                    "Unknown material treatment.");
        }

        ApplyVariantGeometry();

        void Connections(int halfWidth, byte colour)
        {
            FillBox(
                center - halfWidth,
                center - halfWidth,
                halfWidth * 2 + 1,
                halfWidth * 2 + 1,
                colour);
            if ((mask & 1) != 0)
            {
                FillBox(center - halfWidth, 0, halfWidth * 2 + 1, center + 1, colour);
            }
            if ((mask & 2) != 0)
            {
                FillBox(
                    center,
                    center - halfWidth,
                    size - center,
                    halfWidth * 2 + 1,
                    colour);
            }
            if ((mask & 4) != 0)
            {
                FillBox(
                    center - halfWidth,
                    center,
                    halfWidth * 2 + 1,
                    size - center,
                    colour);
            }
            if ((mask & 8) != 0)
            {
                FillBox(0, center - halfWidth, center + 1, halfWidth * 2 + 1, colour);
            }
        }

        void Shore(byte colour)
        {
            if ((mask & 1) == 0) Horizontal(center - 4, center + 4, center - 5, colour);
            if ((mask & 2) == 0) Vertical(center + 5, center - 4, center + 4, colour);
            if ((mask & 4) == 0) Horizontal(center - 4, center + 4, center + 5, colour);
            if ((mask & 8) == 0) Vertical(center - 5, center - 4, center + 4, colour);
        }

        void ApplyVariantGeometry()
        {
            if (variant == 0)
            {
                return;
            }

            if (treatment == MaterialTreatment.Path)
            {
                var side = (Mix(seed ^ (ulong)(mask * 31 + variant)) & 1) == 0
                    ? -2
                    : 2;
                Set(center + side, center + 1, Colour(0));
                return;
            }

            var holes = new[]
            {
                (X: center - 3, Y: center - 2),
                (X: center + 3, Y: center - 2),
                (X: center - 3, Y: center + 2)
            };
            for (var index = 0; index < variant && index < holes.Length; index++)
            {
                Clear(holes[index].X, holes[index].Y);
            }
        }

        byte Colour(int index) =>
            colours.Count == 0
                ? throw new FormatException("CVC-PAL-003: treatment has no colours.")
                : colours[Math.Min(index, colours.Count - 1)];

        void FillBox(int x, int y, int width, int height, byte colour)
        {
            for (var row = y; row < y + height; row++)
            {
                for (var column = x; column < x + width; column++)
                {
                    Set(column, row, colour);
                }
            }
        }

        void Horizontal(int fromX, int toX, int y, byte colour)
        {
            for (var x = fromX; x <= toX; x++)
            {
                Set(x, y, colour);
            }
        }

        void Vertical(int x, int fromY, int toY, byte colour)
        {
            for (var y = fromY; y <= toY; y++)
            {
                Set(x, y, colour);
            }
        }

        void Clear(int x, int y)
        {
            if ((uint)x < (uint)size && (uint)y < (uint)size)
            {
                destination[(offsetY + y) * atlasWidth + offsetX + x] = 0;
            }
        }

        void Set(int x, int y, byte colour)
        {
            if ((uint)x < (uint)size && (uint)y < (uint)size)
            {
                destination[(offsetY + y) * atlasWidth + offsetX + x] = colour;
            }
        }
    }
}
