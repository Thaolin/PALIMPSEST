using Chronicle.Core;

namespace Chronicle.Visuals;

/// <summary>
/// Keeps finite render requests inside the representable absolute-address
/// domain. Numeric limits are storage limits, not authored World edges.
/// </summary>
public static class VisualViewportBounds
{
    public static WorldRectangle Centered(
        long centerX,
        long centerY,
        int width,
        int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Visual viewport dimensions must be positive.");
        }

        return new WorldRectangle(
            CenteredMinimum(centerX, width),
            CenteredMinimum(centerY, height),
            width,
            height);
    }

    public static WorldRectangle WithOneCellSemanticHalo(WorldRectangle visibleBounds)
    {
        Validate(visibleBounds);

        var domainMaximumExclusive = (Int128)long.MaxValue + 1;
        var visibleMaximumX = (Int128)visibleBounds.MinX + visibleBounds.Width;
        var visibleMaximumY = (Int128)visibleBounds.MinY + visibleBounds.Height;
        var minimumX = visibleBounds.MinX == long.MinValue
            ? (Int128)visibleBounds.MinX
            : (Int128)visibleBounds.MinX - 1;
        var minimumY = visibleBounds.MinY == long.MinValue
            ? (Int128)visibleBounds.MinY
            : (Int128)visibleBounds.MinY - 1;
        var maximumX = visibleMaximumX == domainMaximumExclusive
            ? visibleMaximumX
            : visibleMaximumX + 1;
        var maximumY = visibleMaximumY == domainMaximumExclusive
            ? visibleMaximumY
            : visibleMaximumY + 1;

        return new WorldRectangle(
            (long)minimumX,
            (long)minimumY,
            ToDimension(maximumX - minimumX),
            ToDimension(maximumY - minimumY));
    }

    public static long OffsetClamped(long value, long delta)
    {
        var result = (Int128)value + delta;
        if (result < long.MinValue)
        {
            return long.MinValue;
        }

        return result > long.MaxValue ? long.MaxValue : (long)result;
    }

    private static long CenteredMinimum(long center, int length)
    {
        var candidate = (Int128)center - length / 2;
        var maximum = (Int128)long.MaxValue - length + 1;
        if (candidate < long.MinValue)
        {
            return long.MinValue;
        }

        return candidate > maximum ? (long)maximum : (long)candidate;
    }

    private static void Validate(WorldRectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bounds),
                "Visual viewport dimensions must be positive.");
        }

        var domainMaximumExclusive = (Int128)long.MaxValue + 1;
        if ((Int128)bounds.MinX + bounds.Width > domainMaximumExclusive ||
            (Int128)bounds.MinY + bounds.Height > domainMaximumExclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bounds),
                "Visual viewport bounds must remain inside the absolute-address domain.");
        }
    }

    private static int ToDimension(Int128 value)
    {
        if (value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Expanded visual viewport dimensions exceed the supported request size.");
        }

        return (int)value;
    }
}
