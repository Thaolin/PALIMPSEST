using Chronicle.VisualPack;
using Chronicle.Visuals;
using Godot;

/// <summary>
/// Godot-only expansion and rasterization for immutable compiled visual packs.
/// </summary>
public static class VisualPackGodotAdapter
{
    /// <summary>
    /// Expands the pack's indexed atlas into one native RGBA8 texture.
    /// Canvas consumers must use nearest texture filtering when drawing it.
    /// </summary>
    public static ImageTexture CreateAtlasTexture(CompiledVisualPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        var pixels = ExpandAtlas(pack);
        var image = Image.CreateFromData(
            pack.AtlasWidth,
            pack.AtlasHeight,
            useMipmaps: false,
            Image.Format.Rgba8,
            pixels);
        return ImageTexture.CreateFromImage(image);
    }

    public static AtlasTexture CreateRegionTexture(
        Texture2D atlasTexture,
        VisualDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(atlasTexture);
        ArgumentNullException.ThrowIfNull(definition);
        var source = definition.AtlasRect;
        return new AtlasTexture
        {
            Atlas = atlasTexture,
            Region = new Rect2(source.X, source.Y, source.Width, source.Height),
        };
    }

    /// <summary>
    /// Rasterizes the ordered native-pixel marks in a plan into its visible bounds.
    /// </summary>
    public static Image RasterizeNative(CompiledVisualPack pack, VisualRenderPlan plan)
    {
        ValidateRenderPlan(pack, plan);

        var width = checked(plan.Bounds.Width * pack.CellSize);
        var height = checked(plan.Bounds.Height * pack.CellSize);
        var pixels = new byte[checked(width * height * 4)];

        foreach (var mark in plan.Marks)
        {
            var source = mark.AtlasRect;
            var destinationX = checked(
                mark.Column * pack.CellSize + pack.CellSize / 2 - mark.Anchor.X);
            var destinationY = checked(
                mark.Row * pack.CellSize + pack.CellSize / 2 - mark.Anchor.Y);

            for (var sourceY = 0; sourceY < source.Height; sourceY++)
            {
                var outputY = destinationY + sourceY;
                if (outputY < 0 || outputY >= height)
                {
                    continue;
                }

                var atlasY = source.Y + sourceY;
                for (var sourceX = 0; sourceX < source.Width; sourceX++)
                {
                    var outputX = destinationX + sourceX;
                    if (outputX < 0 || outputX >= width)
                    {
                        continue;
                    }

                    var atlasIndex = atlasY * pack.AtlasWidth + source.X + sourceX;
                    var color = pack.Palette[pack.AtlasIndices[atlasIndex]];
                    if (color.Alpha == 0)
                    {
                        continue;
                    }

                    var outputIndex = (outputY * width + outputX) * 4;
                    pixels[outputIndex] = color.Red;
                    pixels[outputIndex + 1] = color.Green;
                    pixels[outputIndex + 2] = color.Blue;
                    pixels[outputIndex + 3] = color.Alpha;
                }
            }
        }

        return Image.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);
    }

    /// <summary>
    /// Rasterizes one overview pixel per logical cell, with later ordered marks
    /// replacing earlier marks at the same address.
    /// </summary>
    public static Image RasterizeOverview(CompiledVisualPack pack, VisualRenderPlan plan)
    {
        ValidateRenderPlan(pack, plan);

        var width = plan.Bounds.Width;
        var height = plan.Bounds.Height;
        var pixels = new byte[checked(width * height * 4)];

        foreach (var mark in plan.Marks)
        {
            var color = pack.Palette[mark.OverviewPaletteIndex];
            var outputIndex = (mark.Row * width + mark.Column) * 4;
            pixels[outputIndex] = color.Red;
            pixels[outputIndex + 1] = color.Green;
            pixels[outputIndex + 2] = color.Blue;
            pixels[outputIndex + 3] = color.Alpha;
        }

        return Image.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);
    }

    internal static void ValidateRenderPlan(CompiledVisualPack pack, VisualRenderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(plan.Marks);

        ValidateAtlasLayout(pack);

        if (!string.Equals(plan.PackId, pack.PackId, StringComparison.Ordinal) ||
            !string.Equals(plan.PackDigest, pack.Digest, StringComparison.Ordinal) ||
            plan.CellSize != pack.CellSize)
        {
            throw new InvalidOperationException(
                "The visual render plan does not match the compiled visual pack.");
        }

        if (plan.Bounds.Width <= 0 || plan.Bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plan),
                "Visual render plan bounds must be positive.");
        }

        _ = checked(plan.Bounds.Width * pack.CellSize);
        _ = checked(plan.Bounds.Height * pack.CellSize);

        foreach (var mark in plan.Marks)
        {
            if (mark.Column < 0 || mark.Column >= plan.Bounds.Width ||
                mark.Row < 0 || mark.Row >= plan.Bounds.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plan),
                    "Visual render plan marks must be inside the visible bounds.");
            }

            var definition = pack.Resolve(mark.VisualId);
            if (!string.Equals(mark.FamilyId, definition.FamilyId, StringComparison.Ordinal) ||
                mark.VariantOrdinal != definition.VariantOrdinal ||
                mark.Layer != definition.LayerClass ||
                mark.AtlasRect != definition.AtlasRect ||
                mark.Anchor != definition.Anchor ||
                mark.OverviewPaletteIndex != definition.OverviewPaletteIndex)
            {
                throw new InvalidOperationException(
                    $"Visual render plan mark '{mark.VisualId}' does not match its compiled definition.");
            }

            ValidateSourceRectangle(pack, mark.AtlasRect, mark.VisualId);
            if (mark.OverviewPaletteIndex < 0 || mark.OverviewPaletteIndex >= pack.Palette.Count)
            {
                throw new InvalidOperationException(
                    $"Visual render plan mark '{mark.VisualId}' has an invalid overview palette index.");
            }
        }
    }

    private static byte[] ExpandAtlas(CompiledVisualPack pack)
    {
        ValidateAtlasLayout(pack);

        var pixels = new byte[checked(pack.AtlasWidth * pack.AtlasHeight * 4)];
        for (var atlasIndex = 0; atlasIndex < pack.AtlasIndices.Count; atlasIndex++)
        {
            var paletteIndex = pack.AtlasIndices[atlasIndex];
            if (paletteIndex >= pack.Palette.Count)
            {
                throw new InvalidOperationException(
                    "The compiled visual pack contains an atlas index outside its palette.");
            }

            var color = pack.Palette[paletteIndex];
            var outputIndex = atlasIndex * 4;
            pixels[outputIndex] = color.Red;
            pixels[outputIndex + 1] = color.Green;
            pixels[outputIndex + 2] = color.Blue;
            pixels[outputIndex + 3] = color.Alpha;
        }

        return pixels;
    }

    private static void ValidateAtlasLayout(CompiledVisualPack pack)
    {
        if (pack.CellSize <= 0 || pack.AtlasWidth <= 0 || pack.AtlasHeight <= 0)
        {
            throw new InvalidOperationException("The compiled visual pack has invalid native dimensions.");
        }

        if (pack.Palette.Count == 0)
        {
            throw new InvalidOperationException("The compiled visual pack has no palette.");
        }

        if (pack.AtlasIndices.Count != checked(pack.AtlasWidth * pack.AtlasHeight))
        {
            throw new InvalidOperationException(
                "The compiled visual pack atlas does not match its declared dimensions.");
        }
    }

    private static void ValidateSourceRectangle(
        CompiledVisualPack pack,
        AtlasRect source,
        string visualId)
    {
        if (source.X < 0 || source.Y < 0 || source.Width <= 0 || source.Height <= 0 ||
            source.X > pack.AtlasWidth - source.Width ||
            source.Y > pack.AtlasHeight - source.Height)
        {
            throw new InvalidOperationException(
                $"Visual render plan mark '{visualId}' has an out-of-bounds atlas rectangle.");
        }
    }
}
