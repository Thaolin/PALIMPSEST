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

public sealed record CompilationResult(
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
    public static CompilationResult Compile(
        VisualCatalogue catalogue,
        CompilationOptions options)
    {
        var diagnostics = new List<CompilerDiagnostic>();
        var sourceDigest = PackDigests.Bytes(catalogue.NormalizedBytes.AsSpan());
        if (catalogue.SchemaVersion != 1)
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
                    .Select(static item => Dimensions(item.family, item.target))
                    .ToArray();
                if (items.Any(item =>
                        Dimensions(item.family, item.target).Width >
                            item.target.NativeSize ||
                        Dimensions(item.family, item.target).Height >
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
                    var colour = target.PaletteRoles.IsEmpty
                        ? (byte)1
                        : packPalette.Roles.First(role =>
                            role.Name == target.PaletteRoles[0]).Index;
                    Raster(
                        family,
                        target,
                        pixels,
                        width,
                        x,
                        y,
                        family.Seed,
                        variant,
                        colour);
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
                        true,
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
                        true,
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
                if (paletteIndex is null)
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        "CVC-PAL-003",
                        DiagnosticSeverity.Error,
                        family.Id,
                        $"Missing palette role '{family.PaletteRole}'."));
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
                                paletteIndex.Index,
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
                                true,
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
                                ImmutableArray.Create(family.PaletteRole),
                                true,
                                geometry,
                                family.TransitionOwnership is null
                                    ? ImmutableArray.Create("connected")
                                    : ImmutableArray.Create(
                                        "connected",
                                        "transition",
                                        $"ownership:{family.TransitionOwnership}")));
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
                motif.Footprint,
                motif.AnchorCell,
                motif.Marks.Select(static mark => new MotifMark(
                    mark.VisualId,
                    mark.Cell,
                    mark.PixelOffset)).ToImmutableArray(),
                motif.OccupancyTags,
                motif.ClippingBehavior)).ToImmutableArray();
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
                        family.Generation == "class-specific"
                            ? "procedural"
                            : family.Id.StartsWith(
                                "baseline.",
                                StringComparison.Ordinal)
                                ? "manual-baseline"
                                : "authored",
                        family.Id,
                        "project",
                        family.Generation == "class-specific"
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

    private static PixelSize Dimensions(
        CatalogueFamily family,
        CatalogueTarget target)
    {
        if (family.Generation == "class-specific")
        {
            return (family.Id, target.NativeSize) switch
            {
                ("actor.incarnation", 16) => new PixelSize(7, 6),
                ("actor.incarnation", 20) => new PixelSize(9, 7),
                ("subject.loose-stone", 16) => new PixelSize(5, 5),
                ("subject.loose-stone", 20) => new PixelSize(7, 6),
                ("landmark.bell-that-fell-up", 16) => new PixelSize(7, 7),
                ("landmark.bell-that-fell-up", 20) => new PixelSize(9, 8),
                _ => throw new FormatException(
                    $"CVC-RASTER-005: no class-specific rule for '{family.Id}' " +
                    $"at {target.NativeSize}px.")
            };
        }

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
        byte colour)
    {
        var dimensions = Dimensions(family, target);
        if (family.Generation == "class-specific")
        {
            RasterClassSpecific(
                family.Id,
                destination,
                atlasWidth,
                offsetX,
                offsetY,
                dimensions.Width,
                dimensions.Height,
                colour);
        }
        else
        {
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
                        (byte)value;
                }
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

    private static void RasterClassSpecific(
        string familyId,
        byte[] destination,
        int atlasWidth,
        int offsetX,
        int offsetY,
        int width,
        int height,
        byte colour)
    {
        var center = width / 2;
        switch (familyId)
        {
            case "actor.incarnation":
                Set(center, 0);
                Fill(center - 1, center + 1, 1);
                Fill(center - 2, center + 2, 2);
                for (var y = 3; y < height - 1; y++)
                {
                    Fill(center - 1, center + 1, y);
                }
                Fill(center - 2, center - 1, height - 1);
                Fill(center + 1, center + 2, height - 1);
                break;
            case "subject.loose-stone":
                for (var y = 0; y < height; y++)
                {
                    var half = y switch
                    {
                        0 => 1,
                        1 => Math.Min(2, width / 2),
                        _ when y == height - 1 => Math.Min(2, width / 2),
                        _ => width / 2
                    };
                    Fill(center - half, center + half, y);
                }
                break;
            case "landmark.bell-that-fell-up":
                Set(center, 0);
                Fill(center - 1, center + 1, 1);
                for (var y = 2; y < height - 2; y++)
                {
                    var half = Math.Min(width / 2, 1 + y / 2);
                    Fill(center - half, center + half, y);
                }
                Fill(0, width - 1, height - 2);
                Fill(1, width - 2, height - 1);
                break;
            default:
                throw new FormatException(
                    $"CVC-RASTER-005: no class-specific rule for '{familyId}'.");
        }

        void Fill(int from, int to, int y)
        {
            for (var x = Math.Max(0, from); x <= Math.Min(width - 1, to); x++)
            {
                Set(x, y);
            }
        }

        void Set(int x, int y) =>
            destination[(offsetY + y) * atlasWidth + offsetX + x] = colour;
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
                if (Connected(
                    destination,
                    atlasWidth,
                    offsetX,
                    offsetY,
                    width,
                    height))
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
                destination[
                    (offsetY + candidate.Y) * atlasWidth +
                    offsetX +
                    candidate.X] = 1;
                return;
            }
        }

        throw new FormatException("CVC-VAR-001: requested variant cannot differ safely.");
    }

    private static bool Connected(
        ReadOnlySpan<byte> pixels,
        int atlasWidth,
        int offsetX,
        int offsetY,
        int width,
        int height)
    {
        var occupied = new HashSet<(int X, int Y)>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (pixels[(offsetY + y) * atlasWidth + offsetX + x] != 0)
                {
                    occupied.Add((x, y));
                }
            }
        }
        if (occupied.Count == 0)
        {
            return false;
        }

        var pending = new Queue<(int X, int Y)>();
        var reached = new HashSet<(int X, int Y)>();
        pending.Enqueue(occupied.First());
        while (pending.TryDequeue(out var point))
        {
            if (!occupied.Contains(point) || !reached.Add(point))
            {
                continue;
            }
            pending.Enqueue((point.X + 1, point.Y));
            pending.Enqueue((point.X - 1, point.Y));
            pending.Enqueue((point.X, point.Y + 1));
            pending.Enqueue((point.X, point.Y - 1));
        }
        return reached.Count == occupied.Count;
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
        byte colour,
        ulong seed,
        int variant)
    {
        var center = size / 2;
        for (var y = center - 1; y <= center + 1; y++)
        {
            for (var x = center - 1; x <= center + 1; x++)
            {
                Set(x, y);
            }
        }
        if ((mask & 1) != 0)
        {
            for (var y = 0; y < center; y++) Set(center, y);
        }
        if ((mask & 2) != 0)
        {
            for (var x = center + 1; x < size; x++) Set(x, center);
        }
        if ((mask & 4) != 0)
        {
            for (var y = center + 1; y < size; y++) Set(center, y);
        }
        if ((mask & 8) != 0)
        {
            for (var x = 0; x < center; x++) Set(x, center);
        }
        if (variant > 0)
        {
            var mixed = Mix(seed ^ (ulong)(mask * 31 + variant));
            var candidates = new (int X, int Y)[]
            {
                (2, 1), (1, 2), (-1, 2), (-2, 1),
                (-2, -1), (-1, -2), (1, -2), (2, -1)
            };
            var candidate = candidates[
                (int)(mixed % (ulong)candidates.Length)];
            Set(center + candidate.X, center + candidate.Y);
        }

        void Set(int x, int y) =>
            destination[(offsetY + y) * atlasWidth + offsetX + x] = colour;
    }
}
