using Chronicle.VisualPack;
using Godot;

namespace Chronicle.VisualPreview;

internal sealed class GodotPackAdapter
{
    private readonly Dictionary<(string Atlas, string Palette), ImageTexture> _textures =
        new();

    private GodotPackAdapter(CompiledVisualPack pack)
    {
        Pack = pack;
        Diagnostics = PackValidator.Validate(pack);
    }

    public CompiledVisualPack Pack { get; }
    public IReadOnlyList<PackDiagnostic> Diagnostics { get; }
    public int TextureCount => _textures.Count;

    public static GodotPackAdapter Load(string directory)
    {
        var root = Path.GetFullPath(directory);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                $"Compiled pack directory does not exist: {root}");
        }

        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => (
                FullPath: path,
                RelativePath: Path.GetRelativePath(root, path)
                    .Replace('\\', '/')))
            .Where(static file =>
                file.RelativePath is
                    "manifest.json" or
                    "hashes.json" or
                    "validation.json" or
                    "provenance.json" ||
                file.RelativePath.StartsWith(
                    "atlases/",
                    StringComparison.Ordinal) &&
                file.RelativePath.EndsWith(
                    ".indices",
                    StringComparison.Ordinal))
            .OrderBy(static file => file.RelativePath, StringComparer.Ordinal)
            .Select(static file => new PackFile(
                file.RelativePath,
                File.ReadAllBytes(file.FullPath)))
            .ToArray();
        return new GodotPackAdapter(PackCodec.ReadCanonical(files));
    }

    public VisualRecord Resolve(FixtureEntry entry)
    {
        if (Pack.TryResolve(
                entry.VisualId,
                entry.NativeSize,
                entry.Variant,
                entry.AdjacencyMask,
                out var handle))
        {
            return Pack.GetVisual(handle);
        }

        var fallback = Pack.Adjacencies.FirstOrDefault(
            item => item.FamilyId == entry.VisualId)?.FallbackMask;
        if (fallback.HasValue &&
            Pack.TryResolve(
                entry.VisualId,
                entry.NativeSize,
                entry.Variant,
                fallback,
                out handle))
        {
            return Pack.GetVisual(handle);
        }

        throw new KeyNotFoundException(
            $"Missing visual '{entry.VisualId}' size={entry.NativeSize} " +
            $"variant={entry.Variant} mask={entry.AdjacencyMask?.ToString() ?? "none"}.");
    }

    public ImageTexture Texture(string atlasId, string paletteId)
    {
        var key = (atlasId, paletteId);
        if (_textures.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var atlas = Pack.Atlases.First(item => item.Id == atlasId);
        var palette = Pack.Palettes.First(item => item.Id == paletteId);
        if (!atlas.CompatiblePalettes.Contains(paletteId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Atlas '{atlasId}' is not compatible with palette '{paletteId}'.");
        }
        var image = Image.CreateFromData(
            atlas.Width,
            atlas.Height,
            false,
            Image.Format.Rgba8,
            Expand(atlas, palette));
        var texture = ImageTexture.CreateFromImage(image);
        _textures.Add(key, texture);
        return texture;
    }

    public Image Render(FixturePlan plan, string paletteId)
    {
        var palette = Pack.Palettes.First(item => item.Id == paletteId);
        var background = ParseRgba(plan.Background);
        var rgba = new byte[plan.Width * plan.Height * 4];
        for (var index = 0; index < plan.Width * plan.Height; index++)
        {
            var offset = index * 4;
            rgba[offset] = background.R;
            rgba[offset + 1] = background.G;
            rgba[offset + 2] = background.B;
            rgba[offset + 3] = background.A;
        }

        foreach (var entry in plan.Entries)
        {
            var visual = Resolve(entry);
            var atlas = Pack.Atlases.First(item => item.Id == visual.AtlasId);
            if (!atlas.CompatiblePalettes.Contains(
                    palette.Id,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Palette '{palette.Id}' cannot render '{visual.Id}'.");
            }
            var indices = Pack.GetAtlasIndices(atlas.Id).Span;
            for (var sourceY = 0; sourceY < visual.Rectangle.Height; sourceY++)
            {
                for (var sourceX = 0;
                     sourceX < visual.Rectangle.Width;
                     sourceX++)
                {
                    var paletteIndex = indices[
                        (visual.Rectangle.Y + sourceY) * atlas.Width +
                        visual.Rectangle.X +
                        sourceX];
                    if (paletteIndex == palette.TransparentIndex)
                    {
                        continue;
                    }
                    var colour = palette.Entries[paletteIndex];
                    for (var scaleY = 0; scaleY < entry.Scale; scaleY++)
                    {
                        for (var scaleX = 0; scaleX < entry.Scale; scaleX++)
                        {
                            var x = entry.X + sourceX * entry.Scale + scaleX;
                            var y = entry.Y + sourceY * entry.Scale + scaleY;
                            if ((uint)x >= (uint)plan.Width ||
                                (uint)y >= (uint)plan.Height)
                            {
                                continue;
                            }
                            var output = (y * plan.Width + x) * 4;
                            rgba[output] = colour.R;
                            rgba[output + 1] = colour.G;
                            rgba[output + 2] = colour.B;
                            rgba[output + 3] = colour.A;
                        }
                    }
                }
            }
        }

        return Image.CreateFromData(
            plan.Width,
            plan.Height,
            false,
            Image.Format.Rgba8,
            rgba);
    }

    private byte[] Expand(AtlasRecord atlas, PaletteRecord palette)
    {
        var indices = Pack.GetAtlasIndices(atlas.Id).Span;
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

    private static Rgba8 ParseRgba(string value)
    {
        if (value.Length != 8 ||
            !uint.TryParse(
                value,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out var rgba))
        {
            throw new FormatException($"CVG-PLAN-003: invalid RGBA8 '{value}'.");
        }
        return new Rgba8(
            (byte)(rgba >> 24),
            (byte)(rgba >> 16),
            (byte)(rgba >> 8),
            (byte)rgba);
    }
}
