using Chronicle.VisualPack;
using Godot;

namespace Chronicle.VisualWorkbench;

internal sealed class WorkbenchPackTexture
{
    private readonly Palimpsest20Pack _pack;
    private ImageTexture? _texture;

    public WorkbenchPackTexture(Palimpsest20Pack pack) => _pack = pack;

    public ImageTexture Get()
    {
        if (_texture is not null)
        {
            return _texture;
        }

        var rgba = new byte[_pack.AtlasIndices.Count * 4];
        for (var index = 0; index < _pack.AtlasIndices.Count; index++)
        {
            var colour = _pack.Palette[_pack.AtlasIndices[index]];
            var output = index * 4;
            rgba[output] = colour.Red;
            rgba[output + 1] = colour.Green;
            rgba[output + 2] = colour.Blue;
            rgba[output + 3] = colour.Alpha;
        }

        var image = Image.CreateFromData(
            _pack.AtlasWidth,
            _pack.AtlasHeight,
            false,
            Image.Format.Rgba8,
            rgba);
        _texture = ImageTexture.CreateFromImage(image);
        return _texture;
    }
}
