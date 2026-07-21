using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicle.VisualPack;

namespace Chronicle.VisualCompiler;

internal static class ReviewLimits
{
    public const long MaximumCanvasPixels = 16_777_216;
}

internal sealed record AcceptedReferenceFixture(
    AcceptedReferenceProvenance Provenance,
    string AggregateDigest,
    ImmutableArray<AcceptedReferencePaletteEntry> Palette,
    ImmutableArray<AcceptedReferenceVisual> Visuals)
{
    internal const string ExpectedVisualIdSetDigest =
        "sha256:7f2f0c09ddc9e84f483c580513a11ad79a62188c861a973cc01044a9e4e88729";

    public static AcceptedReferenceFixture Load()
    {
        using var stream = typeof(AcceptedReferenceFixture).Assembly
            .GetManifestResourceStream("Chronicle.VisualCompiler.accepted-reference.json")
            ?? throw new InvalidOperationException(
                "PAL20-REF-FIXTURE: accepted-reference fixture is missing from the compiler assembly.");
        return JsonSerializer.Deserialize(stream, AcceptedReferenceFixtureContext.Default.AcceptedReferenceFixture)
            ?? throw new FormatException(
                "PAL20-REF-FIXTURE: failed to deserialize accepted-reference fixture.");
    }

    internal ImmutableArray<(byte R, byte G, byte B, byte A)> BuildPaletteLookup()
    {
        var byIndex = new (byte R, byte G, byte B, byte A)[28];
        foreach (var entry in Palette)
        {
            if (entry.Index < 0 || entry.Index >= 28)
            {
                throw new FormatException(
                    $"PAL20-REF-FIXTURE: palette index {entry.Index} out of range.");
            }
            byIndex[entry.Index] = ParseRgba(entry.Rgba);
        }
        for (var i = 0; i < 28; i++)
        {
            if (byIndex[i] == default && Palette.All(e => e.Index != i))
            {
                throw new FormatException(
                    $"PAL20-REF-FIXTURE: palette is missing index {i}.");
            }
        }
        return ImmutableArray.Create(byIndex);
    }

    internal static ImmutableArray<string> Validate(AcceptedReferenceFixture fixture)
    {
        var errors = ImmutableArray.CreateBuilder<string>();

        if (fixture.Provenance.Repository != "Palimpsest")
            errors.Add("Provenance.Repository must be 'Palimpsest'.");
        if (fixture.Provenance.SourceCommit != "15917b3")
            errors.Add("Provenance.SourceCommit must be '15917b3'.");
        if (fixture.Provenance.SourceFile != "src/Chronicle.VisualPack/ManualVisualPack.cs")
            errors.Add("Provenance.SourceFile must be 'src/Chronicle.VisualPack/ManualVisualPack.cs'.");
        if (fixture.Provenance.NativeSize != 20)
            errors.Add("Provenance.NativeSize must be 20.");

        if (fixture.Palette.IsDefaultOrEmpty)
        {
            errors.Add("Palette is missing.");
        }
        else
        {
            if (fixture.Palette.Length != 28)
                errors.Add($"Palette must have exactly 28 entries, got {fixture.Palette.Length}.");
            var seenIndices = new bool[28];
            foreach (var entry in fixture.Palette)
            {
                if (entry.Index < 0 || entry.Index >= 28)
                {
                    errors.Add($"Palette index {entry.Index} is out of range [0, 27].");
                    continue;
                }
                if (seenIndices[entry.Index])
                {
                    errors.Add($"Duplicate palette index {entry.Index}.");
                    continue;
                }
                seenIndices[entry.Index] = true;
                if (entry.Rgba is null || entry.Rgba.Length != 8)
                {
                    errors.Add($"Palette index {entry.Index} has invalid RGBA length.");
                    continue;
                }
                try
                {
                    ParseRgba(entry.Rgba);
                }
                catch
                {
                    errors.Add($"Palette index {entry.Index} has malformed RGBA hex.");
                }
            }
            for (var i = 0; i < 28; i++)
            {
                if (!seenIndices[i])
                    errors.Add($"Palette is missing index {i}.");
            }
        }

        if (fixture.Visuals.IsDefaultOrEmpty)
        {
            errors.Add("Visuals are missing.");
        }
        else
        {
            if (fixture.Visuals.Length != 64)
                errors.Add($"Visuals must have exactly 64 entries, got {fixture.Visuals.Length}.");

            var visualIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var visual in fixture.Visuals)
            {
                if (string.IsNullOrWhiteSpace(visual.VisualId))
                {
                    errors.Add("A visual has a blank ID.");
                    continue;
                }
                if (!visualIds.Add(visual.VisualId))
                    errors.Add($"Duplicate visual ID '{visual.VisualId}'.");

                if (visual.IndexedBuffer is null || visual.IndexedBuffer.Length != 400)
                    errors.Add($"Visual '{visual.VisualId}' indexedBuffer must be exactly 400 bytes, got {visual.IndexedBuffer?.Length ?? 0}.");

                if (string.IsNullOrWhiteSpace(visual.IndexedDigest))
                    errors.Add($"Visual '{visual.VisualId}' indexedDigest is missing.");

                if (string.IsNullOrWhiteSpace(visual.RgbaDigest))
                    errors.Add($"Visual '{visual.VisualId}' rgbaDigest is missing.");
            }

            var visualIdSetDigest = ComputeVisualIdSetDigest(
                fixture.Visuals.Select(static visual => visual.VisualId));
            if (!string.Equals(
                    visualIdSetDigest,
                    ExpectedVisualIdSetDigest,
                    StringComparison.Ordinal))
            {
                errors.Add("Visual ID set digest mismatch.");
            }

            var hasPaletteForDigests = fixture.Palette.Length == 28
                && !fixture.Palette.Any(e => e.Index < 0 || e.Index >= 28);
            if (hasPaletteForDigests)
            {
                var palette = new (byte R, byte G, byte B, byte A)[28];
                foreach (var entry in fixture.Palette)
                    palette[entry.Index] = ParseRgba(entry.Rgba);

                foreach (var visual in fixture.Visuals)
                {
                    if (visual.IndexedBuffer is not { Length: 400 })
                        continue;

                    var paletteInRange = true;
                    for (var i = 0; i < 400 && paletteInRange; i++)
                    {
                        if (visual.IndexedBuffer[i] >= 28)
                        {
                            errors.Add($"Visual '{visual.VisualId}' has palette index {visual.IndexedBuffer[i]} out of range.");
                            paletteInRange = false;
                        }
                    }

                    if (paletteInRange && !string.IsNullOrWhiteSpace(visual.IndexedDigest))
                    {
                        var computedIndexed = ComputeSha256(visual.IndexedBuffer);
                        if (!string.Equals(computedIndexed, visual.IndexedDigest, StringComparison.Ordinal))
                            errors.Add($"Visual '{visual.VisualId}' indexedDigest mismatch.");
                    }

                    if (paletteInRange && !string.IsNullOrWhiteSpace(visual.RgbaDigest))
                    {
                        var rgba = ExpandIndexedToRgba(visual.IndexedBuffer, palette);
                        var computedRgba = ComputeSha256(rgba);
                        if (!string.Equals(computedRgba, visual.RgbaDigest, StringComparison.Ordinal))
                            errors.Add($"Visual '{visual.VisualId}' rgbaDigest mismatch.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(fixture.AggregateDigest))
                {
                    var sorted = fixture.Visuals
                        .Where(v => v.IndexedBuffer is { Length: 400 })
                        .OrderBy(v => v.VisualId, StringComparer.Ordinal)
                        .ToArray();
                    var concatenation = string.Concat(
                        sorted.Select(static v => v.IndexedDigest));
                    var computedAggregate = ComputeSha256(
                        System.Text.Encoding.UTF8.GetBytes(concatenation));
                    if (!string.Equals(computedAggregate, fixture.AggregateDigest, StringComparison.Ordinal))
                        errors.Add("Aggregate digest mismatch.");
                }
            }
        }

        return errors.ToImmutable();
    }

    internal static string ComputeVisualIdSetDigest(IEnumerable<string> visualIds)
    {
        var joined = string.Join(
            "\n",
            visualIds.OrderBy(static id => id, StringComparer.Ordinal));
        return ComputeSha256(System.Text.Encoding.UTF8.GetBytes(joined));
    }

    private static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] ExpandIndexedToRgba(
        byte[] indexedBuffer,
        (byte R, byte G, byte B, byte A)[] palette)
    {
        var rgba = new byte[indexedBuffer.Length * 4];
        for (var i = 0; i < indexedBuffer.Length; i++)
        {
            var entry = palette[indexedBuffer[i]];
            rgba[i * 4] = entry.R;
            rgba[i * 4 + 1] = entry.G;
            rgba[i * 4 + 2] = entry.B;
            rgba[i * 4 + 3] = entry.A;
        }
        return rgba;
    }

    private static (byte R, byte G, byte B, byte A) ParseRgba(string hex) => (
        Convert.ToByte(hex[..2], 16),
        Convert.ToByte(hex[2..4], 16),
        Convert.ToByte(hex[4..6], 16),
        Convert.ToByte(hex[6..8], 16));
}

internal sealed record AcceptedReferenceProvenance(
    string Repository,
    string SourceCommit,
    int NativeSize,
    string SourceFile);

internal sealed record AcceptedReferencePaletteEntry(int Index, string Rgba);

internal sealed record AcceptedReferenceVisual(
    string VisualId,
    string IndexedDigest,
    string RgbaDigest,
    [property: JsonConverter(typeof(Base64ByteArrayConverter))]
    byte[] IndexedBuffer);

internal sealed record AcceptedReferenceComparison(
    AcceptedReferenceVisual Reference,
    VisualRecord Candidate);

internal sealed class Base64ByteArrayConverter : JsonConverter<byte[]>
{
    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? Convert.FromBase64String(reader.GetString()!)
            : null;

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Convert.ToBase64String(value));
}

[JsonSerializable(typeof(AcceptedReferenceFixture))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AcceptedReferenceFixtureContext : JsonSerializerContext
{
}

internal static class ReviewRenderer
{
    public static ImmutableArray<ReviewFile> Render(CompiledVisualPack pack)
    {
        var reference = AcceptedReferenceFixture.Load();
        return Render(pack, reference);
    }

    internal static ImmutableArray<ReviewFile> Render(
        CompiledVisualPack pack,
        AcceptedReferenceFixture reference)
    {
        var native = new List<ReviewFile>();
        var nearest = new List<ReviewFile>();
        foreach (var group in pack.Atlases
                     .GroupBy(static item => item.NativeSize)
                     .OrderBy(static item => item.Key))
        {
            var atlases = group.OrderBy(static item => item.Id, StringComparer.Ordinal).ToArray();
            var width = atlases.Max(static item => item.Width);
            var height = atlases.Sum(static item => item.Height);
            var rgba = CreateCanvas(width, height);
            var offsetY = 0;
            foreach (var atlas in atlases)
            {
                var palette = pack.Palettes.First(item =>
                    atlas.CompatiblePalettes.Contains(item.Id, StringComparer.Ordinal));
                var source = Expand(pack.GetAtlasIndices(atlas.Id).Span, palette);
                for (var y = 0; y < atlas.Height; y++)
                {
                    source.AsSpan(y * atlas.Width * 4, atlas.Width * 4)
                        .CopyTo(rgba.AsSpan(((offsetY + y) * width) * 4));
                }
                offsetY += atlas.Height;
            }

            var nativePng = ImmutableArray.Create(PngEncoder.Encode(width, height, rgba));
            native.Add(new ReviewFile($"review/native-{group.Key}.png", nativePng));
            nearest.Add(new ReviewFile(
                $"review/nearest-{group.Key}.png",
                ImmutableArray.Create(PngEncoder.Encode(
                    ReviewDimension((long)width * 4),
                    ReviewDimension((long)height * 4),
                    Scale4(width, height, rgba)))));
            if (pack.Adjacencies.Any(adjacency => pack.Visuals.Any(visual =>
                    visual.FamilyId == adjacency.FamilyId &&
                    visual.NativeSize == group.Key)))
            {
                nearest.Add(new ReviewFile(
                    $"review/adjacency-{group.Key}.png",
                    ImmutableArray.Create(AdjacencySheet(pack, group.Key))));
                nearest.Add(new ReviewFile(
                    $"review/shifted-overlap-{group.Key}.png",
                    ImmutableArray.Create(ShiftedOverlapSheet(pack, group.Key))));
            }
            if (!pack.Motifs.IsEmpty)
            {
                nearest.Add(new ReviewFile(
                    $"review/motifs-{group.Key}.png",
                    ImmutableArray.Create(MotifSheet(pack, group.Key))));
            }
            if (pack.Visuals.Select(static visual => visual.Layer).Distinct().Count() > 1)
            {
                nearest.Add(new ReviewFile(
                    $"review/layers-{group.Key}.png",
                    ImmutableArray.Create(LayerSheet(pack, group.Key))));
            }
            if (pack.Palettes.Length > 1)
            {
                foreach (var reviewPalette in pack.Palettes)
                {
                    nearest.Add(new ReviewFile(
                        $"review/palette-{reviewPalette.Id}-{group.Key}.png",
                        ImmutableArray.Create(PaletteSheet(
                            pack,
                            group.Key,
                            reviewPalette))));
                }
            }
            if (pack.Visuals.Any(visual =>
                    visual.NativeSize == group.Key &&
                    visual.VariantOrdinal > 0))
            {
                nearest.Add(new ReviewFile(
                    $"review/variants-{group.Key}.png",
                    ImmutableArray.Create(VariantSheet(pack, group.Key))));
            }
            var referenceVisuals = reference.Visuals
                .Where(v => group.Key == reference.Provenance.NativeSize)
                .ToArray();
            if (pack.PackId == "chronicle.palimpsest20" &&
                referenceVisuals.Length > 0)
            {
                var comparisons = BuildReferenceComparisons(
                    pack,
                    reference,
                    group.Key);
                nearest.Add(new ReviewFile(
                    $"review/manual-baseline-{group.Key}.png",
                    ImmutableArray.Create(ReferenceComparisonSheet(
                        pack,
                        comparisons,
                        group.Key))));
            }
        }
        {
            if (reference.Visuals.Length > 0 &&
                pack.Provenance.Any(static item =>
                    item.Origin is "authored" or "procedural"))
            {
                var evidence = BuildAuthoringEvidence(pack, reference);
                nearest.Add(new ReviewFile(
                    "review/authoring-evidence.json",
                    ImmutableArray.Create(evidence)));
            }
        }

        return native.Concat(nearest.OrderBy(static file => file.Path, StringComparer.Ordinal))
            .ToImmutableArray();
    }

    private static byte[] BuildAuthoringEvidence(
        CompiledVisualPack pack,
        AcceptedReferenceFixture reference)
    {
        var comparisons = pack.PackId == "chronicle.palimpsest20"
            ? BuildReferenceComparisons(
                pack,
                reference,
                reference.Provenance.NativeSize)
            : ImmutableArray<AcceptedReferenceComparison>.Empty;
        return JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                comparisonLayout = new
                {
                    columns = new[] { "accepted-reference", "candidate" },
                    referenceProvenance = new
                    {
                        reference.Provenance.Repository,
                        reference.Provenance.SourceCommit,
                        reference.Provenance.NativeSize
                    }
                },
                acceptedReference = new
                {
                    reference.Provenance,
                    reference.AggregateDigest,
                    visualCount = reference.Visuals.Length,
                    visualIdSetDigest = AcceptedReferenceFixture.ComputeVisualIdSetDigest(
                        reference.Visuals.Select(static visual => visual.VisualId)),
                    comparedVisualCount = comparisons.Length,
                    comparedVisuals = comparisons.Select(static comparison => new
                    {
                        referenceVisualId = comparison.Reference.VisualId,
                        candidateVisualId = comparison.Candidate.Id,
                        candidateFamilyId = comparison.Candidate.FamilyId,
                        comparison.Candidate.AdjacencyMask,
                        comparison.Candidate.VariantOrdinal
                    })
                },
                candidates = pack.Provenance
                    .Where(static item =>
                        item.Origin is "authored" or "procedural")
                    .OrderBy(static item => item.FamilyId, StringComparer.Ordinal)
                    .Select(static item => new
                    {
                        familyId = item.FamilyId,
                        item.Origin,
                        authoringCost = item.ReviewNote
                    })
            });
    }

    private static byte[] ReferenceComparisonSheet(
        CompiledVisualPack pack,
        ImmutableArray<AcceptedReferenceComparison> comparisons,
        int nativeSize)
    {
        var width = nativeSize * 2;
        var height = Math.Max(nativeSize, comparisons.Length * nativeSize);
        var rgba = CreateCanvas(width, height);
        for (var index = 0; index < comparisons.Length; index++)
        {
            var y = index * nativeSize;
            DrawRgba(
                comparisons[index].Reference.IndexedBuffer,
                nativeSize,
                nativeSize,
                0,
                y,
                width,
                height,
                rgba);
            Draw(
                pack,
                comparisons[index].Candidate,
                nativeSize,
                y,
                width,
                height,
                rgba);
        }
        return PngEncoder.Encode(width, height, rgba);
    }

    internal static ImmutableArray<AcceptedReferenceComparison> BuildReferenceComparisons(
        CompiledVisualPack pack,
        AcceptedReferenceFixture reference,
        int nativeSize)
    {
        var comparisons = ImmutableArray.CreateBuilder<AcceptedReferenceComparison>(
            reference.Visuals.Length);
        foreach (var referenceVisual in reference.Visuals)
        {
            var key = ParseReferenceKey(referenceVisual.VisualId);
            var candidates = pack.Visuals
                .Where(candidate =>
                    candidate.FamilyId == key.FamilyId &&
                    candidate.NativeSize == nativeSize &&
                    candidate.AdjacencyMask == key.AdjacencyMask)
                .OrderBy(static candidate => candidate.VariantOrdinal)
                .ThenBy(static candidate => candidate.Id, StringComparer.Ordinal)
                .ToArray();

            if (candidates
                    .GroupBy(static candidate => candidate.VariantOrdinal)
                    .Any(static group => group.Count() != 1))
            {
                throw new FormatException(
                    $"PAL20-REF-COMPARE: candidate tuple for '{referenceVisual.VisualId}' is ambiguous.");
            }

            if (key.LocalVariantIndex < 0 ||
                key.LocalVariantIndex >= candidates.Length)
            {
                throw new FormatException(
                    $"PAL20-REF-COMPARE: no candidate matches '{referenceVisual.VisualId}'.");
            }

            if (key.AdjacencyMask is null && candidates.Length != 1)
            {
                throw new FormatException(
                    $"PAL20-REF-COMPARE: non-connected candidate for '{referenceVisual.VisualId}' is ambiguous.");
            }

            comparisons.Add(new AcceptedReferenceComparison(
                referenceVisual,
                candidates[key.LocalVariantIndex]));
        }

        if (comparisons.Count != reference.Visuals.Length)
        {
            throw new FormatException(
                "PAL20-REF-COMPARE: comparison count differs from accepted reference count.");
        }

        return comparisons.ToImmutable();
    }

    private static AcceptedReferenceKey ParseReferenceKey(string visualId)
    {
        foreach (var familyId in ConnectedReferenceFamilies)
        {
            if (visualId == familyId)
                return new AcceptedReferenceKey(familyId, 0, 0);
            if (!visualId.StartsWith(familyId + ".", StringComparison.Ordinal))
                continue;

            var suffix = visualId[familyId.Length..];
            if (suffix.StartsWith(".v", StringComparison.Ordinal) &&
                int.TryParse(suffix[2..], out var baseVariant))
            {
                return new AcceptedReferenceKey(familyId, 0, baseVariant);
            }

            const string maskPrefix = ".mask.";
            if (!suffix.StartsWith(maskPrefix, StringComparison.Ordinal))
                break;

            var variantMarker = suffix.IndexOf(".v", StringComparison.Ordinal);
            var maskText = variantMarker < 0
                ? suffix[maskPrefix.Length..]
                : suffix[maskPrefix.Length..variantMarker];
            var variantText = variantMarker < 0
                ? null
                : suffix[(variantMarker + 2)..];
            var parsedVariant = 0;
            if (int.TryParse(maskText, out var mask) &&
                mask is >= 0 and <= 15 &&
                (variantText is null ||
                 int.TryParse(variantText, out parsedVariant)))
            {
                return new AcceptedReferenceKey(
                    familyId,
                    mask,
                    variantText is null ? 0 : parsedVariant);
            }
            break;
        }

        if (visualId.Contains(".mask.", StringComparison.Ordinal))
        {
            throw new FormatException(
                $"PAL20-REF-COMPARE: unsupported connected reference ID '{visualId}'.");
        }

        return new AcceptedReferenceKey(visualId, null, 0);
    }

    private static readonly ImmutableArray<string> ConnectedReferenceFamilies =
        ImmutableArray.Create(
            "feature.surface.ridge-water-crossing",
            "terrain.surface.water.edge",
            "feature.surface.grove",
            "feature.surface.ridge",
            "terrain.sky.cloud");

    private sealed record AcceptedReferenceKey(
        string FamilyId,
        int? AdjacencyMask,
        int LocalVariantIndex);

    private static void DrawRgba(
        byte[] indexedBuffer,
        int cellWidth,
        int cellHeight,
        long destinationX,
        long destinationY,
        int destinationWidth,
        int destinationHeight,
        byte[] destination)
    {
        for (var y = 0; y < cellHeight; y++)
        {
            for (var x = 0; x < cellWidth; x++)
            {
                var outputX = destinationX + x;
                var outputY = destinationY + y;
                if ((ulong)outputX >= (ulong)destinationWidth ||
                    (ulong)outputY >= (ulong)destinationHeight)
                {
                    continue;
                }
                var paletteIndex = indexedBuffer[y * cellWidth + x];
                if (paletteIndex == 0)
                {
                    continue;
                }
                var output = checked((int)(
                    (outputY * destinationWidth + outputX) * 4));
                var entry = AcceptedReferencePalette.Entries[paletteIndex];
                destination[output] = entry.R;
                destination[output + 1] = entry.G;
                destination[output + 2] = entry.B;
                destination[output + 3] = entry.A;
            }
        }
    }

    private static byte[] AdjacencySheet(CompiledVisualPack pack, int nativeSize)
    {
        const int columns = 8;
        var visuals = pack.Adjacencies
            .OrderBy(static item => item.FamilyId, StringComparer.Ordinal)
            .SelectMany(adjacency => pack.Visuals
                .Where(visual =>
                    visual.FamilyId == adjacency.FamilyId &&
                    visual.NativeSize == nativeSize)
                .OrderBy(static visual => visual.VariantOrdinal)
                .ThenBy(static visual => visual.AdjacencyMask))
            .ToArray();
        var width = columns * nativeSize;
        var height = Math.Max(
            nativeSize,
            ((visuals.Length + columns - 1) / columns) * nativeSize);
        var rgba = CreateCanvas(width, height);
        for (var index = 0; index < visuals.Length; index++)
        {
            Draw(
                pack,
                visuals[index],
                index % columns * nativeSize,
                index / columns * nativeSize,
                width,
                height,
                rgba);
        }
        return PngEncoder.Encode(width, height, rgba);
    }

    private static byte[] ShiftedOverlapSheet(
        CompiledVisualPack pack,
        int nativeSize)
    {
        var families = pack.Adjacencies
            .Where(adjacency => pack.Visuals.Any(visual =>
                visual.FamilyId == adjacency.FamilyId &&
                visual.NativeSize == nativeSize))
            .OrderBy(static item => item.FamilyId, StringComparer.Ordinal)
            .ToArray();
        var width = nativeSize * 4;
        var familyHeight = nativeSize * 2;
        var height = Math.Max(nativeSize, families.Length * familyHeight);
        var rgba = CreateCanvas(width, height);
        for (var index = 0; index < families.Length; index++)
        {
            var adjacency = families[index];
            var baseY = index * familyHeight;
            var east = ResolveForReview(pack, adjacency, nativeSize, 2);
            var west = ResolveForReview(pack, adjacency, nativeSize, 8);
            var south = ResolveForReview(pack, adjacency, nativeSize, 4);
            var north = ResolveForReview(pack, adjacency, nativeSize, 1);
            Draw(pack, east, 0, baseY, width, height, rgba);
            Draw(pack, west, nativeSize - 1, baseY, width, height, rgba);
            Draw(pack, south, nativeSize * 3, baseY, width, height, rgba);
            Draw(
                pack,
                north,
                nativeSize * 3,
                baseY + nativeSize - 1,
                width,
                height,
                rgba);
        }
        return PngEncoder.Encode(width, height, rgba);
    }

    private static byte[] MotifSheet(CompiledVisualPack pack, int nativeSize)
    {
        var motifs = pack.Motifs
            .Where(motif => motif.Marks.Any(mark => pack.Visuals.Any(visual =>
                visual.Id == mark.VisualId &&
                visual.NativeSize == nativeSize)))
            .OrderBy(static motif => motif.FamilyId, StringComparer.Ordinal)
            .ToArray();
        var width = ReviewDimension(Math.Max(
            (long)nativeSize,
            motifs.Select(motif => (long)motif.Footprint.Width * nativeSize)
                .DefaultIfEmpty(nativeSize)
                .Max()));
        var height = ReviewDimension(Math.Max(
            (long)nativeSize,
            motifs.Sum(motif =>
                (long)motif.Footprint.Height * nativeSize * motif.VariantCount)));
        var rgba = CreateCanvas(width, height);
        var offsetY = 0;
        foreach (var motif in motifs)
        {
            for (var variant = 0; variant < motif.VariantCount; variant++)
            {
                foreach (var mark in motif.Marks.Where(mark =>
                             mark.VariantOrdinal == variant))
                {
                    var visual = pack.Visuals
                        .Where(item =>
                            item.Id == mark.VisualId &&
                            item.NativeSize == nativeSize &&
                            item.VariantOrdinal == variant &&
                            item.AdjacencyMask is null or 0)
                        .OrderBy(static item => item.AdjacencyMask.HasValue)
                        .FirstOrDefault();
                    if (visual is not null)
                    {
                        Draw(
                            pack,
                            visual,
                            (long)mark.Cell.X * nativeSize + mark.PixelOffset.X,
                            (long)offsetY +
                                (long)mark.Cell.Y * nativeSize +
                                mark.PixelOffset.Y,
                            width,
                            height,
                            rgba);
                    }
                }
                offsetY += ReviewDimension(
                    (long)motif.Footprint.Height * nativeSize);
            }
        }
        return PngEncoder.Encode(width, height, rgba);
    }

    private static byte[] LayerSheet(CompiledVisualPack pack, int nativeSize)
    {
        const int columns = 8;
        var rows = new List<VisualRecord[]>();
        foreach (var layer in pack.Visuals
                     .Where(visual => visual.NativeSize == nativeSize)
                     .Select(static visual => visual.Layer)
                     .Distinct()
                     .Order())
        {
            var layerVisuals = pack.Visuals
                .Where(visual =>
                    visual.NativeSize == nativeSize && visual.Layer == layer)
                .OrderBy(static visual => visual.Id, StringComparer.Ordinal)
                .ThenBy(static visual => visual.VariantOrdinal)
                .ThenBy(static visual => visual.AdjacencyMask)
                .Take(16)
                .ToArray();
            for (var index = 0; index < layerVisuals.Length; index += columns)
            {
                rows.Add(layerVisuals.Skip(index).Take(columns).ToArray());
            }
        }
        var width = columns * nativeSize;
        var height = Math.Max(nativeSize, rows.Count * nativeSize);
        var rgba = CreateCanvas(width, height);
        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                Draw(
                    pack,
                    rows[row][column],
                    column * nativeSize,
                    row * nativeSize,
                    width,
                    height,
                    rgba);
            }
        }
        return PngEncoder.Encode(width, height, rgba);
    }

    private static byte[] PaletteSheet(
        CompiledVisualPack pack,
        int nativeSize,
        PaletteRecord palette) =>
        VisualGrid(
            pack,
            nativeSize,
            pack.Visuals
                .Where(visual => visual.NativeSize == nativeSize)
                .OrderBy(static visual => visual.Id, StringComparer.Ordinal)
                .ThenBy(static visual => visual.VariantOrdinal)
                .ThenBy(static visual => visual.AdjacencyMask),
            palette);

    private static byte[] VariantSheet(CompiledVisualPack pack, int nativeSize) =>
        VisualGrid(
            pack,
            nativeSize,
            pack.Visuals
                .Where(visual =>
                    visual.NativeSize == nativeSize &&
                    pack.Visuals.Any(candidate =>
                        candidate.FamilyId == visual.FamilyId &&
                        candidate.NativeSize == nativeSize &&
                        candidate.VariantOrdinal > 0))
                .OrderBy(static visual => visual.FamilyId, StringComparer.Ordinal)
                .ThenBy(static visual => visual.AdjacencyMask)
                .ThenBy(static visual => visual.VariantOrdinal),
            null);

    private static byte[] ManualBaselineSheet(
        CompiledVisualPack pack,
        int nativeSize)
    {
        var reference = AcceptedReferenceFixture.Load();
        return ReferenceComparisonSheet(
            pack,
            BuildReferenceComparisons(pack, reference, nativeSize),
            nativeSize);
    }

    private static byte[] VisualGrid(
        CompiledVisualPack pack,
        int nativeSize,
        IEnumerable<VisualRecord> source,
        PaletteRecord? palette)
    {
        const int columns = 8;
        var visuals = source.ToArray();
        var width = columns * nativeSize;
        var height = Math.Max(
            nativeSize,
            ((visuals.Length + columns - 1) / columns) * nativeSize);
        var rgba = CreateCanvas(width, height);
        for (var index = 0; index < visuals.Length; index++)
        {
            Draw(
                pack,
                visuals[index],
                index % columns * nativeSize,
                index / columns * nativeSize,
                width,
                height,
                rgba,
                palette);
        }
        return PngEncoder.Encode(width, height, rgba);
    }

    private static VisualRecord ResolveForReview(
        CompiledVisualPack pack,
        AdjacencyRecord adjacency,
        int nativeSize,
        int mask)
    {
        var visual = pack.Visuals.FirstOrDefault(item =>
            item.FamilyId == adjacency.FamilyId &&
            item.NativeSize == nativeSize &&
            item.VariantOrdinal == 0 &&
            item.AdjacencyMask == mask);
        if (visual is not null)
        {
            return visual;
        }

        return pack.Visuals.First(item =>
            item.FamilyId == adjacency.FamilyId &&
            item.NativeSize == nativeSize &&
            item.VariantOrdinal == 0 &&
            item.AdjacencyMask == adjacency.FallbackMask);
    }

    private static void Draw(
        CompiledVisualPack pack,
        VisualRecord visual,
        long destinationX,
        long destinationY,
        int destinationWidth,
        int destinationHeight,
        byte[] destination,
        PaletteRecord? selectedPalette = null)
    {
        var atlas = pack.Atlases.First(item => item.Id == visual.AtlasId);
        var palette = selectedPalette ?? pack.Palettes.First(item =>
            atlas.CompatiblePalettes.Contains(item.Id, StringComparer.Ordinal));
        var indices = pack.GetAtlasIndices(atlas.Id).Span;
        for (var y = 0; y < visual.Rectangle.Height; y++)
        {
            for (var x = 0; x < visual.Rectangle.Width; x++)
            {
                var outputX = destinationX + x;
                var outputY = destinationY + y;
                if ((ulong)outputX >= (ulong)destinationWidth ||
                    (ulong)outputY >= (ulong)destinationHeight)
                {
                    continue;
                }
                var paletteIndex = indices[
                    (visual.Rectangle.Y + y) * atlas.Width +
                    visual.Rectangle.X +
                    x];
                if (paletteIndex == 0)
                {
                    continue;
                }
                var colour = palette.Entries[paletteIndex];
                var output = checked((int)(
                    (outputY * destinationWidth + outputX) * 4));
                destination[output] = colour.R;
                destination[output + 1] = colour.G;
                destination[output + 2] = colour.B;
                destination[output + 3] = colour.A;
            }
        }
    }

    private static byte[] Expand(ReadOnlySpan<byte> indices, PaletteRecord palette)
    {
        var rgba = new byte[indices.Length * 4];
        for (var index = 0; index < indices.Length; index++)
        {
            var colour = palette.Entries[indices[index]];
            var offset = index * 4;
            rgba[offset] = colour.R;
            rgba[offset + 1] = colour.G;
            rgba[offset + 2] = colour.B;
            rgba[offset + 3] = colour.A;
        }
        return rgba;
    }

    private static byte[] Scale4(int width, int height, ReadOnlySpan<byte> source)
    {
        var scaledWidth = ReviewDimension((long)width * 4);
        var scaledHeight = ReviewDimension((long)height * 4);
        var result = CreateCanvas(scaledWidth, scaledHeight);
        for (var y = 0; y < scaledHeight; y++)
        {
            for (var x = 0; x < scaledWidth; x++)
            {
                var sourceOffset = ((y / 4) * width + x / 4) * 4;
                source.Slice(sourceOffset, 4)
                    .CopyTo(result.AsSpan((y * scaledWidth + x) * 4, 4));
            }
        }
        return result;
    }

    private static int ReviewDimension(long value)
    {
        if (value <= 0 || value > int.MaxValue)
        {
            throw new FormatException(
                "CVC-REVIEW-001: review dimensions exceed supported bounds.");
        }
        return (int)value;
    }

    private static byte[] CreateCanvas(int width, int height)
    {
        var pixelCount = (long)width * height;
        if (width <= 0 ||
            height <= 0 ||
            pixelCount > ReviewLimits.MaximumCanvasPixels)
        {
            throw new FormatException(
                "CVC-REVIEW-001: review canvas exceeds supported bounds.");
        }
        return new byte[checked((int)(pixelCount * 4))];
    }

}

internal static class AcceptedReferencePalette
{
    private static ImmutableArray<(byte R, byte G, byte B, byte A)>? _entries;

    public static ImmutableArray<(byte R, byte G, byte B, byte A)> Entries
    {
        get
        {
            if (_entries is not null)
                return _entries.Value;

            var fixture = AcceptedReferenceFixture.Load();
            var parsed = fixture.BuildPaletteLookup();

            for (var i = 0; i < KnownGood.Length; i++)
            {
                var known = KnownGood[i];
                var loaded = parsed[i];
                if (known.R != loaded.R ||
                    known.G != loaded.G ||
                    known.B != loaded.B ||
                    known.A != loaded.A)
                {
                    throw new FormatException(
                        $"PAL20-REF-FIXTURE: palette index {i} does not match known-good value.");
                }
            }

            _entries = parsed;
            return _entries.Value;
        }
    }

    private static readonly ImmutableArray<(byte R, byte G, byte B, byte A)> KnownGood =
        ImmutableArray.Create<(byte, byte, byte, byte)>(
            (0, 0, 0, 0),
            (20, 31, 25, 255),
            (47, 91, 51, 255),
            (82, 126, 70, 255),
            (65, 45, 29, 255),
            (112, 78, 48, 255),
            (18, 48, 70, 255),
            (27, 91, 117, 255),
            (83, 166, 166, 255),
            (17, 48, 29, 255),
            (37, 96, 52, 255),
            (83, 132, 72, 255),
            (47, 51, 60, 255),
            (103, 108, 119, 255),
            (177, 183, 190, 255),
            (12, 31, 53, 255),
            (20, 61, 88, 255),
            (104, 134, 148, 255),
            (173, 198, 204, 255),
            (117, 76, 20, 255),
            (225, 164, 45, 255),
            (255, 226, 112, 255),
            (39, 29, 51, 255),
            (214, 61, 88, 255),
            (255, 243, 211, 255),
            (112, 222, 220, 255),
            (9, 15, 24, 255),
            (223, 233, 235, 255));
}
