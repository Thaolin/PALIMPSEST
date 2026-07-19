using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Chronicle.VisualPack;

namespace Chronicle.VisualCompiler;

internal static class ReviewRenderer
{
    public static ImmutableArray<ReviewFile> Render(CompiledVisualPack pack)
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
            var rgba = new byte[width * height * 4];
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

            var nativePng = ImmutableArray.Create(Png(width, height, rgba));
            native.Add(new ReviewFile($"review/native-{group.Key}.png", nativePng));
            nearest.Add(new ReviewFile(
                $"review/nearest-{group.Key}.png",
                ImmutableArray.Create(Png(
                    width * 4,
                    height * 4,
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
            if (pack.Visuals.Any(visual =>
                    visual.NativeSize == group.Key &&
                    visual.Id.StartsWith("baseline.", StringComparison.Ordinal)))
            {
                nearest.Add(new ReviewFile(
                    $"review/manual-baseline-{group.Key}.png",
                    ImmutableArray.Create(ManualBaselineSheet(pack, group.Key))));
            }
        }
        if (pack.Provenance.Any(static item => item.Origin == "procedural") &&
            pack.Provenance.Any(static item => item.Origin == "manual-baseline"))
        {
            var evidence = JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    procedural = pack.Provenance
                        .Where(static item => item.Origin == "procedural")
                        .OrderBy(static item => item.FamilyId, StringComparer.Ordinal)
                        .Select(static item => new
                        {
                            familyId = item.FamilyId,
                            item.Origin,
                            authoringCost = item.ReviewNote
                        }),
                    manualBaselines = pack.Provenance
                        .Where(static item => item.Origin == "manual-baseline")
                        .OrderBy(static item => item.FamilyId, StringComparer.Ordinal)
                        .Select(static item => new
                        {
                            familyId = item.FamilyId,
                            item.Origin,
                            authoringCost = item.ReviewNote
                        })
                });
            nearest.Add(new ReviewFile(
                "review/authoring-evidence.json",
                ImmutableArray.Create(evidence)));
        }

        return native.Concat(nearest.OrderBy(static file => file.Path, StringComparer.Ordinal))
            .ToImmutableArray();
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
        var rgba = new byte[width * height * 4];
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
        return Png(width, height, rgba);
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
        var rgba = new byte[width * height * 4];
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
        return Png(width, height, rgba);
    }

    private static byte[] MotifSheet(CompiledVisualPack pack, int nativeSize)
    {
        var motifs = pack.Motifs
            .Where(motif => motif.Marks.Any(mark => pack.Visuals.Any(visual =>
                visual.Id == mark.VisualId &&
                visual.NativeSize == nativeSize)))
            .OrderBy(static motif => motif.FamilyId, StringComparer.Ordinal)
            .ToArray();
        var width = Math.Max(
            nativeSize,
            motifs.Select(motif => motif.Footprint.Width * nativeSize)
                .DefaultIfEmpty(nativeSize)
                .Max());
        var height = Math.Max(
            nativeSize,
            motifs.Sum(motif => motif.Footprint.Height * nativeSize));
        var rgba = new byte[width * height * 4];
        var offsetY = 0;
        foreach (var motif in motifs)
        {
            foreach (var mark in motif.Marks)
            {
                var visual = pack.Visuals.FirstOrDefault(item =>
                    item.Id == mark.VisualId &&
                    item.NativeSize == nativeSize &&
                    item.VariantOrdinal == 0 &&
                    item.AdjacencyMask is null);
                if (visual is not null)
                {
                    Draw(
                        pack,
                        visual,
                        mark.Cell.X * nativeSize + mark.PixelOffset.X,
                        offsetY + mark.Cell.Y * nativeSize + mark.PixelOffset.Y,
                        width,
                        height,
                        rgba);
                }
            }
            offsetY += motif.Footprint.Height * nativeSize;
        }
        return Png(width, height, rgba);
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
        var rgba = new byte[width * height * 4];
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
        return Png(width, height, rgba);
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
        var pairs = pack.Visuals
            .Where(visual =>
                visual.NativeSize == nativeSize &&
                visual.VariantOrdinal == 0 &&
                visual.AdjacencyMask is null &&
                visual.Id.StartsWith("baseline.", StringComparison.Ordinal))
            .OrderBy(static visual => visual.Id, StringComparer.Ordinal)
            .Select(baseline => (
                Baseline: baseline,
                Candidate: pack.Visuals.FirstOrDefault(candidate =>
                    candidate.Id == baseline.Id["baseline.".Length..] &&
                    candidate.NativeSize == nativeSize &&
                    candidate.VariantOrdinal == 0 &&
                    candidate.AdjacencyMask is null)))
            .Where(static pair => pair.Candidate is not null)
            .ToArray();
        var width = nativeSize * 2;
        var height = Math.Max(nativeSize, pairs.Length * nativeSize);
        var rgba = new byte[width * height * 4];
        for (var index = 0; index < pairs.Length; index++)
        {
            Draw(
                pack,
                pairs[index].Baseline,
                0,
                index * nativeSize,
                width,
                height,
                rgba);
            Draw(
                pack,
                pairs[index].Candidate!,
                nativeSize,
                index * nativeSize,
                width,
                height,
                rgba);
        }
        return Png(width, height, rgba);
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
        var rgba = new byte[width * height * 4];
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
        return Png(width, height, rgba);
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
        int destinationX,
        int destinationY,
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
                if ((uint)outputX >= (uint)destinationWidth ||
                    (uint)outputY >= (uint)destinationHeight)
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
                var output = (outputY * destinationWidth + outputX) * 4;
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
        var scaledWidth = width * 4;
        var result = new byte[scaledWidth * height * 4 * 4];
        for (var y = 0; y < height * 4; y++)
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

    private static byte[] Png(int width, int height, ReadOnlySpan<byte> rgba)
    {
        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(output, "IHDR", header);

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(
                   compressed,
                   CompressionLevel.SmallestSize,
                   leaveOpen: true))
        {
            for (var y = 0; y < height; y++)
            {
                zlib.WriteByte(0);
                zlib.Write(rgba.Slice(y * width * 4, width * 4));
            }
        }
        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", ReadOnlySpan<byte>.Empty);
        return output.ToArray();
    }

    private static void WriteChunk(
        Stream stream,
        string type,
        ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = 0xffffffffu;
        foreach (var value in typeBytes)
        {
            crc = UpdateCrc(crc, value);
        }
        foreach (var value in data)
        {
            crc = UpdateCrc(crc, value);
        }
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        stream.Write(number);
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
        {
            crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1));
        }
        return crc;
    }
}
