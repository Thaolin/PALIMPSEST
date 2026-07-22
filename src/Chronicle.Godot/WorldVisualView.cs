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
    private bool _paused;

    public WorldVisualView()
    {
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
    }

    public VisualRenderPlan? CurrentPlan => _plan;

    public int CellSize => _plan?.CellSize ?? 0;

    public int VisibleColumns => _plan?.Bounds.Width ?? 0;

    public int VisibleRows => _plan?.Bounds.Height ?? 0;

    public bool IsPaused => _paused;

    public void SetPaused(bool paused)
    {
        if (_paused == paused)
        {
            return;
        }

        _paused = paused;
        QueueRedraw();
    }

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

        if (_paused)
        {
            foreach (var mark in _plan.Marks.Where(mark => !IsLivingActor(mark) && mark.Layer < VisualLayerClass.TemporaryAction))
            {
                DrawMark(mark, _plan.CellSize, mapWidth, mapHeight);
            }

            DrawRect(
                new Rect2(0, 0, mapWidth, mapHeight),
                new Color(0.015f, 0.055f, 0.08f, 0.32f),
                true);

            foreach (var mark in _plan.Marks.Where(IsLivingActor))
            {
                DrawActor(mark, _plan.CellSize, mapWidth, mapHeight);
            }

            foreach (var mark in _plan.Marks.Where(mark => mark.Layer >= VisualLayerClass.TemporaryAction))
            {
                DrawMark(mark, _plan.CellSize, mapWidth, mapHeight);
            }
        }
        else
        {
            foreach (var mark in _plan.Marks)
            {
                if (IsLivingActor(mark))
                {
                    DrawActor(mark, _plan.CellSize, mapWidth, mapHeight);
                }
                else
                {
                    DrawMark(mark, _plan.CellSize, mapWidth, mapHeight);
                }
            }
        }

        foreach (var selection in _plan.Marks.Where(mark => mark.VisualId == "emphasis.target.selected"))
        {
            DrawSelectionBrackets(selection, _plan.CellSize);
        }
    }

    private void DrawActor(VisualRenderMark mark, int cellSize, int mapWidth, int mapHeight)
    {
        var center = new Vector2(
            mark.Column * cellSize + cellSize / 2.0f,
            mark.Row * cellSize + cellSize / 2.0f);
        var player = mark.VisualId == "actor.incarnation";
        var ring = player
            ? new Color(0.28f, 0.82f, 0.68f, 0.96f)
            : new Color(0.98f, 0.34f, 0.16f, 0.96f);
        DrawArc(center, cellSize * 0.40f, 0, Mathf.Tau, 32, ring, 1.0f, true);

        var keyline = new Color(0.01f, 0.008f, 0.006f, 0.90f);
        DrawMark(mark, cellSize, mapWidth, mapHeight, new Vector2(-1, 0), keyline);
        DrawMark(mark, cellSize, mapWidth, mapHeight, new Vector2(1, 0), keyline);
        DrawMark(mark, cellSize, mapWidth, mapHeight, new Vector2(0, -1), keyline);
        DrawMark(mark, cellSize, mapWidth, mapHeight, new Vector2(0, 1), keyline);
        DrawMark(mark, cellSize, mapWidth, mapHeight, new Vector2(1, 1), new Color(0, 0, 0, 0.48f));
        DrawMark(mark, cellSize, mapWidth, mapHeight);
    }

    private void DrawSelectionBrackets(VisualRenderMark mark, int cellSize)
    {
        var left = mark.Column * cellSize + 2;
        var top = mark.Row * cellSize + 2;
        var right = left + cellSize - 4;
        var bottom = top + cellSize - 4;
        var length = Math.Max(4, cellSize / 4);
        var color = new Color(0.90f, 0.86f, 0.76f, 0.96f);
        DrawLine(new Vector2(left, top), new Vector2(left + length, top), color, 1);
        DrawLine(new Vector2(left, top), new Vector2(left, top + length), color, 1);
        DrawLine(new Vector2(right, top), new Vector2(right - length, top), color, 1);
        DrawLine(new Vector2(right, top), new Vector2(right, top + length), color, 1);
        DrawLine(new Vector2(left, bottom), new Vector2(left + length, bottom), color, 1);
        DrawLine(new Vector2(left, bottom), new Vector2(left, bottom - length), color, 1);
        DrawLine(new Vector2(right, bottom), new Vector2(right - length, bottom), color, 1);
        DrawLine(new Vector2(right, bottom), new Vector2(right, bottom - length), color, 1);
    }

    private static bool IsLivingActor(VisualRenderMark mark) =>
        mark.VisualId is "actor.incarnation" or "subject.mire-brute.living";

    private void DrawMark(
        VisualRenderMark mark,
        int cellSize,
        int mapWidth,
        int mapHeight,
        Vector2 offset = default,
        Color? modulate = null)
    {
        var source = mark.AtlasRect;
        var destinationX = checked(mark.Column * cellSize + cellSize / 2 - mark.Anchor.X) + offset.X;
        var destinationY = checked(mark.Row * cellSize + cellSize / 2 - mark.Anchor.Y) + offset.Y;
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
        DrawTextureRectRegion(_atlasTexture!, destinationRect, sourceRect, modulate);
    }
}
