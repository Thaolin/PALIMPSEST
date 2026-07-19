using Chronicle.VisualPack;
using Chronicle.Visuals;
using Godot;

/// <summary>
/// One native-pixel canvas for a composed local World view.
/// </summary>
public partial class WorldVisualView : Node2D
{
    private const int BorderPixels = 2;

    private static readonly Color MapBackingColor = new(0.012f, 0.02f, 0.03f);
    private static readonly Color MapBorderColor = new(0.075f, 0.105f, 0.13f);

    private readonly Dictionary<string, ImageTexture> _atlasTextures = new(StringComparer.Ordinal);

    private CompiledVisualPack? _pack;
    private ImageTexture? _atlasTexture;
    private VisualRenderPlan? _plan;

    public WorldVisualView()
    {
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    public VisualRenderPlan? CurrentPlan => _plan;

    public int CellSize => _plan?.CellSize ?? 0;

    public int VisibleColumns => _plan?.Bounds.Width ?? 0;

    public int VisibleRows => _plan?.Bounds.Height ?? 0;

    public void SetPlan(CompiledVisualPack pack, VisualRenderPlan plan)
    {
        VisualPackGodotAdapter.ValidateRenderPlan(pack, plan);

        if (!_atlasTextures.TryGetValue(pack.Digest, out var atlasTexture))
        {
            atlasTexture = VisualPackGodotAdapter.CreateAtlasTexture(pack);
            _atlasTextures.Add(pack.Digest, atlasTexture);
        }

        _pack = pack;
        _plan = plan;
        _atlasTexture = atlasTexture;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_pack is null || _plan is null || _atlasTexture is null)
        {
            return;
        }

        VisualPackGodotAdapter.ValidateRenderPlan(_pack, _plan);

        var mapWidth = checked(_plan.Bounds.Width * _plan.CellSize);
        var mapHeight = checked(_plan.Bounds.Height * _plan.CellSize);
        DrawRect(
            new Rect2(
                -BorderPixels,
                -BorderPixels,
                mapWidth + BorderPixels * 2,
                mapHeight + BorderPixels * 2),
            MapBorderColor,
            true);
        DrawRect(new Rect2(0, 0, mapWidth, mapHeight), MapBackingColor, true);

        foreach (var mark in _plan.Marks)
        {
            DrawMark(mark, _plan.CellSize, mapWidth, mapHeight);
        }
    }

    private void DrawMark(VisualRenderMark mark, int cellSize, int mapWidth, int mapHeight)
    {
        var source = mark.AtlasRect;
        var destinationX = checked(mark.Column * cellSize + cellSize / 2 - mark.Anchor.X);
        var destinationY = checked(mark.Row * cellSize + cellSize / 2 - mark.Anchor.Y);
        var clippedLeft = Math.Max(0, destinationX);
        var clippedTop = Math.Max(0, destinationY);
        var clippedRight = Math.Min(mapWidth, destinationX + source.Width);
        var clippedBottom = Math.Min(mapHeight, destinationY + source.Height);

        if (clippedLeft >= clippedRight || clippedTop >= clippedBottom)
        {
            return;
        }

        var sourceRect = new Rect2(
            source.X + clippedLeft - destinationX,
            source.Y + clippedTop - destinationY,
            clippedRight - clippedLeft,
            clippedBottom - clippedTop);
        var destinationRect = new Rect2(
            clippedLeft,
            clippedTop,
            clippedRight - clippedLeft,
            clippedBottom - clippedTop);
        DrawTextureRectRegion(_atlasTexture!, destinationRect, sourceRect);
    }
}
