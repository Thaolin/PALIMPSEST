using Chronicle.Core;
using Godot;

public partial class SurfacePatchView : Node2D
{
    private const float CellSize = 44f;
    private static readonly Color GridLineColor = new(0.1f, 0.14f, 0.17f);
    private static readonly Color MarkerOutlineColor = new(1f, 0.89f, 0.25f);
    private static readonly Color MarkerFillColor = new(0.92f, 0.22f, 0.18f);
    private static readonly Color LooseStoneOutlineColor = new(0.08f, 0.1f, 0.12f);
    private static readonly Color LooseStoneFillColor = new(0.72f, 0.75f, 0.78f);
    private static readonly Color TargetColor = new(1f, 0.76f, 0.12f);

    private SurfacePatch? _patch;
    private WorldAddress? _looseStoneAddress;
    private WorldAddress[] _highlightedTargets = [];

    public void SetPatch(SurfacePatch patch)
    {
        ArgumentNullException.ThrowIfNull(patch);
        _patch = patch;
        QueueRedraw();
    }

    public void SetSubjects(
        WorldAddress? looseStoneAddress,
        IReadOnlyList<WorldAddress> highlightedTargets)
    {
        if (_looseStoneAddress == looseStoneAddress &&
            _highlightedTargets.SequenceEqual(highlightedTargets))
        {
            return;
        }

        _looseStoneAddress = looseStoneAddress;
        _highlightedTargets = highlightedTargets.ToArray();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_patch is null)
        {
            return;
        }

        var patchSize = new Vector2(SurfacePatch.Width * CellSize, SurfacePatch.Height * CellSize);
        DrawRect(new Rect2(new Vector2(-4, -4), patchSize + new Vector2(8, 8)), new Color(0.015f, 0.025f, 0.035f), true);

        foreach (var tile in _patch.Tiles)
        {
            var column = tile.Address.X - _patch.Center.X + SurfacePatch.Width / 2;
            var row = tile.Address.Y - _patch.Center.Y + SurfacePatch.Height / 2;
            var tileOrigin = new Vector2(column * CellSize, row * CellSize);
            var tileRect = new Rect2(tileOrigin + Vector2.One, new Vector2(CellSize - 2, CellSize - 2));

            DrawRect(tileRect, TerrainColor(tile.Terrain), true);
            DrawRect(new Rect2(tileOrigin, new Vector2(CellSize, CellSize)), GridLineColor, false, 1f);
        }

        DrawLooseStone();

        var markerCenter = new Vector2(
            SurfacePatch.Width / 2f * CellSize,
            SurfacePatch.Height / 2f * CellSize);
        DrawCircle(markerCenter, CellSize * 0.34f, MarkerOutlineColor);
        DrawCircle(markerCenter, CellSize * 0.21f, MarkerFillColor);
    }

    private void DrawLooseStone()
    {
        if (_patch is null ||
            _looseStoneAddress is not { } address ||
            !string.Equals(address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal))
        {
            return;
        }

        var column = address.X - _patch.Center.X + SurfacePatch.Width / 2;
        var row = address.Y - _patch.Center.Y + SurfacePatch.Height / 2;
        if (column is < 0 or >= SurfacePatch.Width || row is < 0 or >= SurfacePatch.Height)
        {
            return;
        }

        var tileOrigin = new Vector2(column * CellSize, row * CellSize);
        var center = tileOrigin + Vector2.One * CellSize / 2f;
        if (_highlightedTargets.Contains(address))
        {
            DrawRect(
                new Rect2(tileOrigin + Vector2.One * 3f, new Vector2(CellSize - 6f, CellSize - 6f)),
                TargetColor,
                false,
                3f);
        }

        DrawCircle(center, CellSize * 0.27f, LooseStoneOutlineColor);
        DrawCircle(center + new Vector2(-1f, -2f), CellSize * 0.19f, LooseStoneFillColor);
        DrawCircle(center + new Vector2(4f, -5f), CellSize * 0.045f, Colors.White);
    }

    private static Color TerrainColor(SurfaceTerrain terrain) => terrain switch
    {
        SurfaceTerrain.Grass => new Color(0.28f, 0.54f, 0.27f),
        SurfaceTerrain.Forest => new Color(0.08f, 0.31f, 0.17f),
        SurfaceTerrain.Stone => new Color(0.43f, 0.46f, 0.49f),
        SurfaceTerrain.Water => new Color(0.13f, 0.37f, 0.66f),
        _ => Colors.Magenta,
    };
}
