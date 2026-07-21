namespace Chronicle.VisualPack;

public static class PixelConnectivity
{
    public static bool IsFourConnected(
        ReadOnlySpan<byte> pixels,
        int stride,
        PixelRect rectangle)
    {
        if (stride <= 0 || rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return false;
        }

        var cellCount = checked(rectangle.Width * rectangle.Height);
        var occupied = new bool[cellCount];
        var occupiedCount = 0;
        var firstOccupied = -1;
        for (var y = 0; y < rectangle.Height; y++)
        {
            for (var x = 0; x < rectangle.Width; x++)
            {
                var sourceIndex =
                    (rectangle.Y + y) * stride +
                    rectangle.X +
                    x;
                if ((uint)sourceIndex >= (uint)pixels.Length ||
                    pixels[sourceIndex] == 0)
                {
                    continue;
                }

                var localIndex = y * rectangle.Width + x;
                occupied[localIndex] = true;
                occupiedCount++;
                if (firstOccupied < 0)
                {
                    firstOccupied = localIndex;
                }
            }
        }

        if (firstOccupied < 0)
        {
            return false;
        }

        var reached = new bool[cellCount];
        var pending = new Queue<int>();
        pending.Enqueue(firstOccupied);
        var reachedCount = 0;
        while (pending.TryDequeue(out var index))
        {
            if (!occupied[index] || reached[index])
            {
                continue;
            }

            reached[index] = true;
            reachedCount++;
            var x = index % rectangle.Width;
            var y = index / rectangle.Width;
            if (x > 0)
            {
                pending.Enqueue(index - 1);
            }
            if (x + 1 < rectangle.Width)
            {
                pending.Enqueue(index + 1);
            }
            if (y > 0)
            {
                pending.Enqueue(index - rectangle.Width);
            }
            if (y + 1 < rectangle.Height)
            {
                pending.Enqueue(index + rectangle.Width);
            }
        }

        return reachedCount == occupiedCount;
    }
}
