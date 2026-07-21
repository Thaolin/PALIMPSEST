using Chronicle.VisualPack;
using Godot;

namespace Chronicle.VisualPreview;

internal sealed class GodotPackAdapter
{
    private ImageTexture? _texture;

    private GodotPackAdapter(Palimpsest20Bundle bundle)
    {
        Pack = bundle.Pack;
        Validation = bundle.Validation;
    }

    public Palimpsest20Pack Pack { get; }
    public Palimpsest20Validation Validation { get; }
    public int TextureCount => _texture is null ? 0 : 1;

    public static GodotPackAdapter Load(string directory)
    {
        var root = Path.GetFullPath(directory);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                $"Compiled pack directory does not exist: {root}");
        }

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new PackFile(
                Path.GetRelativePath(root, path).Replace('\\', '/'),
                File.ReadAllBytes(path)))
            .OrderBy(static file => file.Path, StringComparer.Ordinal)
            .ToArray();
        return new GodotPackAdapter(Palimpsest20Codec.ReadCanonical(files));
    }

    public Palimpsest20Definition Resolve(FixtureEntry entry)
    {
        if (entry.NativeSize != Palimpsest20Pack.NativeCellSize)
        {
            throw new FormatException(
                $"CVG-PLAN-005: '{entry.VisualId}' must use native size 20.");
        }

        return Pack.Resolve(entry.VisualId);
    }

    public ImageTexture Texture()
    {
        if (_texture is not null)
        {
            return _texture;
        }

        var image = Image.CreateFromData(
            Pack.AtlasWidth,
            Pack.AtlasHeight,
            false,
            Image.Format.Rgba8,
            Expand());
        _texture = ImageTexture.CreateFromImage(image);
        return _texture;
    }

    public Image Render(FixturePlan plan)
    {
        if (!StringComparer.Ordinal.Equals(plan.PaletteId, Pack.PaletteId))
        {
            throw new FormatException(
                $"CVG-PLAN-004: expected palette '{Pack.PaletteId}', got '{plan.PaletteId}'.");
        }

        var background = ParseRgba(plan.Background);
        var rgba = new byte[checked(plan.Width * plan.Height * 4)];
        for (var index = 0; index < plan.Width * plan.Height; index++)
        {
            var offset = index * 4;
            rgba[offset] = background.Red;
            rgba[offset + 1] = background.Green;
            rgba[offset + 2] = background.Blue;
            rgba[offset + 3] = background.Alpha;
        }

        foreach (var entry in plan.Entries)
        {
            var visual = Resolve(entry);
            var rect = visual.AtlasRect;
            for (var sourceY = 0; sourceY < rect.Height; sourceY++)
            {
                for (var sourceX = 0; sourceX < rect.Width; sourceX++)
                {
                    var paletteIndex = Pack.AtlasIndices[
                        (rect.Y + sourceY) * Pack.AtlasWidth + rect.X + sourceX];
                    if (paletteIndex == 0)
                    {
                        continue;
                    }

                    var colour = Pack.Palette[paletteIndex];
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
                            rgba[output] = colour.Red;
                            rgba[output + 1] = colour.Green;
                            rgba[output + 2] = colour.Blue;
                            rgba[output + 3] = colour.Alpha;
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

    private byte[] Expand()
    {
        var rgba = new byte[Pack.AtlasIndices.Count * 4];
        for (var index = 0; index < Pack.AtlasIndices.Count; index++)
        {
            var colour = Pack.Palette[Pack.AtlasIndices[index]];
            var offset = index * 4;
            rgba[offset] = colour.Red;
            rgba[offset + 1] = colour.Green;
            rgba[offset + 2] = colour.Blue;
            rgba[offset + 3] = colour.Alpha;
        }

        return rgba;
    }

    private static Palimpsest20PaletteColor ParseRgba(string value)
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

        return new Palimpsest20PaletteColor(
            (byte)(rgba >> 24),
            (byte)(rgba >> 16),
            (byte)(rgba >> 8),
            (byte)rgba);
    }
}
