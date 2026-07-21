using System.Collections.Immutable;
using Chronicle.VisualCompiler;
using Chronicle.VisualPack;

/// <summary>
/// Contract tracer for deterministic motif selection and placement.  The expected
/// selection values are independently pinned SHA-256 vectors, rather than a
/// second implementation of the selection algorithm.
/// </summary>
public static class MotifDeterminismConformance
{
    public static bool Run()
    {
        if (!SelectionVectorsMatch())
        {
            return false;
        }

        return MotifPlacementHonorsVariantsOrderingAndClipping();
    }

    private static bool SelectionVectorsMatch()
    {
        // Each ExpectedVariant is the first eight SHA-256 bytes interpreted
        // little-endian and reduced modulo VariantCount, for this exact UTF-8:
        // pgen-selection-v1\nseed={seed}\nstyle={style}\nfamily={family}\nlayer={layer}\n
        // x={x}\ny={y}\nmask={mask|-1}\nsalt={salt}\n
        // Keep these values literal so this remains an independent vector test.
        var vectors = new[]
        {
            new SelectionVector(0UL, 1, "terrain.surface.grass", VisualLayer.Ground,
                0L, 0L, 4, null, 0UL, 2),
            new SelectionVector(1UL, 1, "terrain.surface.water.edge", VisualLayer.Adjacency,
                -17L, 42L, 16, 13, 0UL, 14),
            new SelectionVector(ulong.MaxValue, 20, "atmosphere.cloud", VisualLayer.Feature,
                long.MinValue, long.MaxValue, 16, 0, 999UL, 15),
            new SelectionVector(42UL, 7, "structure.wall", VisualLayer.Structure,
                long.MaxValue, long.MinValue, 2, 15, 12345UL, 1),
            new SelectionVector(1234567890123456789UL, 3, "terrain.grove", VisualLayer.Feature,
                -909L, 808L, 7, null, ulong.MaxValue, 0),
            new SelectionVector(987654321UL, 5, "route.path", VisualLayer.Structure,
                -1L, -1L, 3, 10, 77UL, 1)
        };

        foreach (var vector in vectors)
        {
            var actual = DeterministicSelection.SelectVariant(
                vector.Seed,
                vector.VisualStyleVersion,
                vector.FamilyId,
                vector.Layer,
                vector.X,
                vector.Y,
                vector.VariantCount,
                vector.AdjacencyMask,
                vector.Salt);
            if (actual != vector.ExpectedVariant)
            {
                Console.Error.WriteLine(
                    $"CVC-E45-MOTIF-SELECTION: expected {vector.ExpectedVariant} for " +
                    $"{vector.FamilyId} at ({vector.X},{vector.Y}), got {actual}.");
                return false;
            }
        }

        return true;
    }

    private static bool MotifPlacementHonorsVariantsOrderingAndClipping()
    {
        var clip = new MotifRecord(
            "motif.contract",
            99UL,
            2,
            new PixelSize(3, 2),
            new PixelPoint(0, 0),
            ImmutableArray.Create(
                new MotifMark("marker.variant-0", new PixelPoint(0, 0), new PixelPoint(0, 0), 0),
                new MotifMark("marker.variant-1.first", new PixelPoint(1, 0), new PixelPoint(2, -1), 1),
                new MotifMark("marker.variant-1.clipped", new PixelPoint(2, 1), new PixelPoint(0, 0), 1),
                new MotifMark("marker.variant-1.offset-clipped", new PixelPoint(1, 1), new PixelPoint(100, 0), 1),
                new MotifMark("marker.variant-1.second", new PixelPoint(0, 1), new PixelPoint(-1, 3), 1),
                new MotifMark("marker.variant-0.last", new PixelPoint(1, 1), new PixelPoint(0, 0), 0)),
            ImmutableArray.Create("terrain"),
            MotifClippingBehavior.Clip);

        if (clip.Seed != 99UL ||
            clip.VariantCount != 2 ||
            clip.Marks[1].VariantOrdinal != 1 ||
            clip.ClippingBehavior != MotifClippingBehavior.Clip)
        {
            Console.Error.WriteLine(
                "CVC-E45-MOTIF-MODEL: motif seed, variant count, mark variants, or typed clipping were lost.");
            return false;
        }

        var placed = MotifPlacement.Resolve(
            clip,
            variantOrdinal: 1,
            origin: new PixelPoint(1, 0),
            bounds: new PixelSize(3, 2),
            cellSize: 20);
        if (placed.Length != 2 ||
            !Matches(placed[0], "marker.variant-1.first", new PixelPoint(2, 0), new PixelPoint(2, -1), 1) ||
            !Matches(placed[1], "marker.variant-1.second", new PixelPoint(1, 1), new PixelPoint(-1, 3), 1))
        {
            Console.Error.WriteLine(
                "CVC-E45-MOTIF-CLIP: clip did not retain the ordered, in-bounds nonzero-variant marks.");
            return false;
        }

        var rejected = clip with { ClippingBehavior = MotifClippingBehavior.Reject };
        try
        {
            _ = MotifPlacement.Resolve(
                rejected,
                variantOrdinal: 1,
                origin: new PixelPoint(1, 0),
                bounds: new PixelSize(3, 2),
                cellSize: 20);
            Console.Error.WriteLine(
                "CVC-E45-MOTIF-REJECT: reject accepted an out-of-bounds selected mark.");
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    private static bool Matches(
        MotifMark actual,
        string visualId,
        PixelPoint cell,
        PixelPoint pixelOffset,
        int variantOrdinal) =>
        actual.VisualId == visualId &&
        actual.Cell == cell &&
        actual.PixelOffset == pixelOffset &&
        actual.VariantOrdinal == variantOrdinal;

    private sealed record SelectionVector(
        ulong Seed,
        int VisualStyleVersion,
        string FamilyId,
        VisualLayer Layer,
        long X,
        long Y,
        int VariantCount,
        int? AdjacencyMask,
        ulong Salt,
        int ExpectedVariant);
}
