using Chronicle.Core;
using Godot;

public partial class SkyStratumView : Node2D
{
    private const float CellSize = 44f;
    private static readonly Color OpenSkyColor = new(0.055f, 0.16f, 0.27f);
    private static readonly Color AlternateSkyColor = new(0.06f, 0.18f, 0.3f);
    private static readonly Color GridLineColor = new(0.1f, 0.28f, 0.4f, 0.72f);
    private static readonly Color CloudColor = new(0.7f, 0.82f, 0.88f);
    private static readonly Color BellColor = new(1f, 0.76f, 0.12f);
    private static readonly Color BellCenterColor = new(0.08f, 0.055f, 0.025f);
    private static readonly Color MarkerOutlineColor = new(1f, 0.94f, 0.56f);
    private static readonly Color MarkerFillColor = new(0.92f, 0.18f, 0.36f);
    private static readonly Color LooseStoneOutlineColor = new(0.03f, 0.05f, 0.08f);
    private static readonly Color LooseStoneFillColor = new(0.72f, 0.75f, 0.78f);
    private static readonly Color TargetColor = new(1f, 0.76f, 0.12f);

    private SkyStratum? _sky;
    private WorldAddress _incarnationAddress;
    private WorldAddress? _looseStoneAddress;
    private WorldAddress[] _highlightedTargets = [];

    public void SetSky(SkyStratum sky, WorldAddress incarnationAddress)
    {
        ArgumentNullException.ThrowIfNull(sky);

        if (!sky.Contains(incarnationAddress))
        {
            throw new ArgumentOutOfRangeException(
                nameof(incarnationAddress),
                incarnationAddress,
                "The sky view requires an address inside the generated sky.");
        }

        _sky = sky;
        _incarnationAddress = incarnationAddress;
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
        if (_sky is null)
        {
            return;
        }

        var mapSize = new Vector2(SkyStratum.Width * CellSize, SkyStratum.Height * CellSize);
        DrawRect(
            new Rect2(new Vector2(-4, -4), mapSize + new Vector2(8, 8)),
            new Color(0.01f, 0.035f, 0.07f),
            true);

        foreach (var tile in _sky.Tiles)
        {
            var column = tile.Address.X - (_sky.Center.X - SkyStratum.Width / 2);
            var row = tile.Address.Y - (_sky.Center.Y - SkyStratum.Height / 2);
            var tileOrigin = new Vector2(column * CellSize, row * CellSize);
            var tileCenter = tileOrigin + Vector2.One * CellSize / 2f;
            var tileRect = new Rect2(tileOrigin + Vector2.One, new Vector2(CellSize - 2, CellSize - 2));
            var baseColor = (column + row) % 2 == 0 ? OpenSkyColor : AlternateSkyColor;

            DrawRect(tileRect, baseColor, true);
            DrawRect(new Rect2(tileOrigin, new Vector2(CellSize, CellSize)), GridLineColor, false, 1f);
            DrawTerrain(tile.Terrain, tileCenter);
        }

        DrawLooseStone();

        var markerColumn = _incarnationAddress.X - (_sky.Center.X - SkyStratum.Width / 2);
        var markerRow = _incarnationAddress.Y - (_sky.Center.Y - SkyStratum.Height / 2);
        var markerCenter = new Vector2(
            (markerColumn + 0.5f) * CellSize,
            (markerRow + 0.5f) * CellSize);
        DrawCircle(markerCenter, CellSize * 0.26f, MarkerOutlineColor);
        DrawCircle(markerCenter, CellSize * 0.15f, MarkerFillColor);
    }

    private void DrawLooseStone()
    {
        if (_sky is null ||
            _looseStoneAddress is not { } address ||
            !_sky.Contains(address))
        {
            return;
        }

        var column = address.X - (_sky.Center.X - SkyStratum.Width / 2);
        var row = address.Y - (_sky.Center.Y - SkyStratum.Height / 2);
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

    private void DrawTerrain(SkyTerrain terrain, Vector2 center)
    {
        switch (terrain)
        {
            case SkyTerrain.OpenSky:
                break;
            case SkyTerrain.Cloud:
                DrawCircle(center + new Vector2(-7, 2), 7f, CloudColor);
                DrawCircle(center + new Vector2(0, -2), 9f, CloudColor);
                DrawCircle(center + new Vector2(8, 3), 6f, CloudColor);
                break;
            case SkyTerrain.Landmark:
                DrawCircle(center, 13f, BellColor);
                DrawCircle(center, 7f, BellCenterColor);
                DrawLine(center + new Vector2(-11, -10), center + new Vector2(11, -10), BellColor, 3f);
                DrawCircle(center + new Vector2(0, 12), 3f, BellColor);
                break;
            default:
                DrawCircle(center, 8f, Colors.Magenta);
                break;
        }
    }
}
