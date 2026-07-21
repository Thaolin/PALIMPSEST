using System.Collections.Immutable;

namespace Chronicle.VisualPack;

public static class MotifPlacement
{
    public static ImmutableArray<MotifMark> Resolve(
        MotifRecord motif,
        int variantOrdinal,
        PixelPoint origin,
        PixelSize bounds,
        int cellSize)
    {
        ArgumentNullException.ThrowIfNull(motif);
        if (variantOrdinal < 0 || variantOrdinal >= motif.VariantCount)
        {
            throw new ArgumentOutOfRangeException(nameof(variantOrdinal));
        }
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds));
        }
        if (cellSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize));
        }

        var resolved = ImmutableArray.CreateBuilder<MotifMark>();
        var boundsWidthPixels = (long)bounds.Width * cellSize;
        var boundsHeightPixels = (long)bounds.Height * cellSize;
        foreach (var mark in motif.Marks)
        {
            if (mark.VariantOrdinal != variantOrdinal)
            {
                continue;
            }

            var x = (long)origin.X + mark.Cell.X;
            var y = (long)origin.Y + mark.Cell.Y;
            var left = x * cellSize + mark.PixelOffset.X;
            var top = y * cellSize + mark.PixelOffset.Y;
            var right = left + cellSize;
            var bottom = top + cellSize;
            var fullyInside =
                left >= 0 &&
                top >= 0 &&
                right <= boundsWidthPixels &&
                bottom <= boundsHeightPixels;
            var intersects =
                right > 0 &&
                bottom > 0 &&
                left < boundsWidthPixels &&
                top < boundsHeightPixels;
            if (!fullyInside && motif.ClippingBehavior == MotifClippingBehavior.Reject)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(origin),
                    $"Motif '{motif.FamilyId}' crosses the placement bounds.");
            }
            if (!intersects)
            {
                continue;
            }
            if (x is < int.MinValue or > int.MaxValue ||
                y is < int.MinValue or > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(origin),
                    $"Motif '{motif.FamilyId}' cannot represent its resolved cell.");
            }

            resolved.Add(mark with { Cell = new PixelPoint((int)x, (int)y) });
        }

        return resolved.ToImmutable();
    }
}
