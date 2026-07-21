using Chronicle.VisualPack;
using Godot;

namespace Chronicle.VisualWorkbench;

internal enum WorkbenchView
{
    AssetLab,
    MaterialMatrix,
    BiomeBoard
}

internal sealed partial class WorkbenchCanvas : Control
{
    private static readonly Color CanvasBackground = new("11161d");
    private static readonly Color CellBackground = new("18212a");
    private static readonly Color CellLine = new("344451");
    private static readonly Color NativeLine = new("d4b45f");

    private Palimpsest20Pack? _pack;
    private WorkbenchPackTexture? _texture;
    private Palimpsest20Definition? _selected;
    private WorkbenchView _view;
    private bool _tallCells;

    public WorkbenchCanvas()
    {
        CustomMinimumSize = new Vector2(760, 650);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Present(
        Palimpsest20Pack pack,
        Palimpsest20Definition selected,
        WorkbenchView view,
        bool tallCells)
    {
        if (!ReferenceEquals(_pack, pack))
        {
            _pack = pack;
            _texture = new WorkbenchPackTexture(pack);
        }
        _selected = selected;
        _view = view;
        _tallCells = tallCells;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), CanvasBackground, true);
        if (_pack is null || _texture is null || _selected is null)
        {
            return;
        }

        switch (_view)
        {
            case WorkbenchView.AssetLab:
                DrawAssetLab();
                break;
            case WorkbenchView.MaterialMatrix:
                DrawMaterialMatrix();
                break;
            case WorkbenchView.BiomeBoard:
                DrawBiomeBoard();
                break;
        }
    }

    private void DrawAssetLab()
    {
        var cellHeight = _tallCells ? 30 : 20;
        var largeScale = Math.Max(8, Math.Min(18, (int)(Size.Y / cellHeight) - 5));
        var destination = new Rect2(
            (Size.X - 20 * largeScale) / 2,
            42,
            20 * largeScale,
            cellHeight * largeScale);
        DrawRect(destination, CellBackground, true);
        DrawGrid(destination, 20, cellHeight, largeScale);
        DrawDefinition(_selected!, destination);
        DrawRect(destination, NativeLine, false, 2);

        var nativeY = destination.End.Y + 28;
        for (var scale = 1; scale <= 4; scale *= 2)
        {
            var width = 20 * scale;
            var height = cellHeight * scale;
            var x = Size.X / 2 - 160 + scale * 48;
            var rect = new Rect2(x, nativeY, width, height);
            DrawRect(rect, CellBackground, true);
            DrawDefinition(_selected!, rect);
            DrawRect(rect, CellLine, false, 1);
        }
    }

    private void DrawMaterialMatrix()
    {
        var definitions = _pack!.Definitions
            .Where(definition => StringComparer.Ordinal.Equals(
                definition.FamilyId,
                _selected!.FamilyId))
            .OrderBy(definition => definition.AdjacencyMask is null
                ? -1
                : (int)definition.AdjacencyMask.Value)
            .ThenBy(definition => definition.VariantOrdinal)
            .Take(32)
            .ToArray();
        if (definitions.Length == 0)
        {
            return;
        }

        const int columns = 8;
        const int scale = 4;
        const int gap = 8;
        var cellHeight = _tallCells ? 30 : 20;
        var itemWidth = 20 * scale;
        var itemHeight = cellHeight * scale;
        var totalWidth = columns * itemWidth + (columns - 1) * gap;
        var origin = new Vector2((Size.X - totalWidth) / 2, 44);
        for (var index = 0; index < definitions.Length; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var rect = new Rect2(
                origin.X + column * (itemWidth + gap),
                origin.Y + row * (itemHeight + gap),
                itemWidth,
                itemHeight);
            DrawRect(rect, CellBackground, true);
            DrawDefinition(definitions[index], rect);
            DrawRect(
                rect,
                ReferenceEquals(definitions[index], _selected)
                    ? NativeLine
                    : CellLine,
                false,
                ReferenceEquals(definitions[index], _selected) ? 2 : 1);
        }
    }

    private void DrawBiomeBoard()
    {
        const int columns = 20;
        const int rows = 12;
        const int scale = 2;
        var cellHeight = _tallCells ? 30 : 20;
        var cellWidthPixels = 20 * scale;
        var cellHeightPixels = cellHeight * scale;
        var origin = new Vector2(
            (Size.X - columns * cellWidthPixels) / 2,
            Math.Max(20, (Size.Y - rows * cellHeightPixels) / 2));

        var water = new bool[columns, rows];
        for (var y = 0; y < rows; y++)
        {
            var shore = 4 + y / 3;
            for (var x = 0; x < columns; x++)
            {
                water[x, y] = x < shore;
                DrawCell(
                    Resolve(water[x, y]
                        ? "terrain.surface.water"
                        : ((x + y) % 7 == 0
                            ? "terrain.surface.soil.v1"
                            : "terrain.surface.grass.v2")),
                    x,
                    y);
            }
        }

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                if (!water[x, y])
                {
                    continue;
                }
                var mask = 0;
                if (y > 0 && water[x, y - 1]) mask |= 1;
                if (x + 1 < columns && water[x + 1, y]) mask |= 2;
                if (y + 1 < rows && water[x, y + 1]) mask |= 4;
                if (x > 0 && water[x - 1, y]) mask |= 8;
                DrawCell(Resolve($"terrain.surface.water.edge.{mask:00}"), x, y);
            }
        }

        DrawFeature("feature.surface.grove", 8, 2, 0);
        DrawFeature("feature.surface.grove", 10, 3, 1);
        DrawFeature("feature.surface.grove", 12, 2, 2);
        DrawFeature("feature.surface.grove", 14, 4, 3);
        DrawFeature("feature.surface.ridge", 15, 8, 0);
        DrawFeature("feature.surface.ridge", 17, 7, 2);
        DrawFeature("feature.surface.ridge", 18, 9, 3);
        DrawCell(Resolve("landmark.bell-that-fell-up"), 8, 9);
        DrawCell(Resolve("subject.home-hearthstone"), 11, 9);
        DrawCell(Resolve("subject.loose-stone"), 13, 7);
        DrawCell(Resolve("actor.incarnation"), 9, 7);

        void DrawFeature(string family, int x, int y, int variant)
        {
            var id = variant == 0
                ? family
                : $"{family}.v{variant}";
            DrawCell(Resolve(id), x, y);
        }

        void DrawCell(Palimpsest20Definition definition, int x, int y)
        {
            var rect = new Rect2(
                origin.X + x * cellWidthPixels,
                origin.Y + y * cellHeightPixels,
                cellWidthPixels,
                cellHeightPixels);
            DrawDefinition(definition, rect);
        }
    }

    private Palimpsest20Definition Resolve(string id)
    {
        try
        {
            return _pack!.Resolve(id);
        }
        catch (KeyNotFoundException)
        {
            return _pack!.Definitions[0];
        }
    }

    private void DrawDefinition(Palimpsest20Definition definition, Rect2 destination)
    {
        var source = definition.AtlasRect;
        DrawTextureRectRegion(
            _texture!.Get(),
            destination,
            new Rect2(source.X, source.Y, source.Width, source.Height));
    }

    private void DrawGrid(Rect2 destination, int columns, int rows, int scale)
    {
        for (var x = 0; x <= columns; x++)
        {
            var px = destination.Position.X + x * scale;
            DrawLine(
                new Vector2(px, destination.Position.Y),
                new Vector2(px, destination.End.Y),
                CellLine,
                1);
        }
        for (var y = 0; y <= rows; y++)
        {
            var py = destination.Position.Y + y * scale;
            DrawLine(
                new Vector2(destination.Position.X, py),
                new Vector2(destination.End.X, py),
                CellLine,
                1);
        }
    }
}
