using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Chronicle.VisualPack;
using Chronicle.VisualPreview;
using Godot;

[GlobalClass]
public partial class PreviewRoot : Control
{
    private GodotPackAdapter? _adapter;
    private FixturePlan? _plan;
    private Label? _metadata;
    private string _paletteId = "";
    private int _visualIndex;
    private int _inspectionScale = 4;
    private bool _acceptance;
    private int _acceptanceFrames;
    private string? _acceptanceOutput;
    private byte[]? _acceptancePlanBytes;
    private double _loadMilliseconds;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        try
        {
            var arguments = ParseArguments(OS.GetCmdlineUserArgs());
            var packDirectory = Required(arguments, "--pack");
            var planPath = Required(arguments, "--plan");
            var timer = Stopwatch.StartNew();
            _adapter = GodotPackAdapter.Load(packDirectory);
            timer.Stop();
            var planBytes = File.ReadAllBytes(planPath);
            _plan = FixturePlan.Parse(planBytes);
            _paletteId = _plan.PaletteId;
            if (!_adapter.Pack.Palettes.Any(item => item.Id == _paletteId))
            {
                throw new FormatException(
                    $"CVG-PLAN-004: unknown palette '{_paletteId}'.");
            }

            GD.Print(
                $"CVG-E4-LOAD: {_adapter.Pack.Visuals.Length} visuals, " +
                $"{_adapter.Pack.Atlases.Length} atlases, " +
                $"{timer.Elapsed.TotalMilliseconds:F3} ms.");
            if (arguments.ContainsKey("--acceptance"))
            {
                _acceptance = true;
                _acceptanceOutput = Required(arguments, "--output");
                _acceptancePlanBytes = planBytes;
                _loadMilliseconds = timer.Elapsed.TotalMilliseconds;
                QueueRedraw();
                return;
            }

            _metadata = new Label
            {
                Position = new Vector2(8, 482),
                Size = new Vector2(624, 36)
            };
            AddChild(_metadata);
            UpdateMetadata();
            QueueRedraw();
        }
        catch (Exception exception)
        {
            GD.PushError($"CVG-E4-001: {exception}");
            GetTree().Quit(1);
        }
    }

    public override void _Process(double delta)
    {
        if (!_acceptance || ++_acceptanceFrames < 2)
        {
            return;
        }

        SetProcess(false);
        try
        {
            CaptureViewport(
                _acceptanceOutput!,
                _acceptancePlanBytes!,
                _loadMilliseconds);
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"CVG-E4-002: {exception}");
            GetTree().Quit(1);
        }
    }

    public override void _Draw()
    {
        if (_adapter is null || _plan is null)
        {
            return;
        }

        var background = new Color(_plan.Background);
        DrawRect(
            new Rect2(0, 0, _plan.Width, _plan.Height),
            background,
            true);
        foreach (var entry in _plan.Entries)
        {
            DrawEntry(entry, entry.Scale);
        }

        if (!_acceptance)
        {
            var selected = _adapter.Pack.Visuals[_visualIndex];
            var previewEntry = new FixtureEntry(
                selected.Id,
                selected.NativeSize,
                selected.VariantOrdinal,
                selected.AdjacencyMask,
                520,
                384,
                _inspectionScale);
            DrawEntry(previewEntry, _inspectionScale);
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key ||
            _adapter is null)
        {
            return;
        }

        switch (key.Keycode)
        {
            case Key.P:
                CyclePalette();
                break;
            case Key.F:
                CycleFamily();
                break;
            case Key.V:
                CycleVariant();
                break;
            case Key.M:
                CycleMask();
                break;
            case Key.L:
                CycleLayer();
                break;
            case Key.N:
                _visualIndex = (_visualIndex + 1) % _adapter.Pack.Visuals.Length;
                break;
            case Key.S:
                _inspectionScale = _inspectionScale switch
                {
                    1 => 4,
                    4 => 8,
                    _ => 1
                };
                break;
            default:
                return;
        }
        UpdateMetadata();
        QueueRedraw();
    }

    private void DrawEntry(FixtureEntry entry, int scale)
    {
        var visual = _adapter!.Resolve(entry);
        var texture = _adapter.Texture(visual.AtlasId, _paletteId);
        DrawTextureRectRegion(
            texture,
            new Rect2(
                entry.X,
                entry.Y,
                visual.Rectangle.Width * scale,
                visual.Rectangle.Height * scale),
            new Rect2(
                visual.Rectangle.X,
                visual.Rectangle.Y,
                visual.Rectangle.Width,
                visual.Rectangle.Height));
    }

    private void CaptureViewport(
        string outputDirectory,
        byte[] planBytes,
        double loadMilliseconds)
    {
        Directory.CreateDirectory(outputDirectory);
        var viewport = GetViewport()
            .GetTexture()
            .GetImage()
            .GetRegion(new Rect2I(0, 0, _plan!.Width, _plan.Height));
        viewport.Convert(Image.Format.Rgba8);
        var oracle = _adapter!.Render(_plan, _paletteId);
        var viewportBytes = viewport.GetData();
        var oracleBytes = oracle.GetData();
        var imagePath = Path.Combine(outputDirectory, "capture.png");
        var error = viewport.SavePng(imagePath);
        if (error != Error.Ok)
        {
            throw new IOException($"Godot PNG save failed: {error}.");
        }
        error = oracle.SavePng(Path.Combine(outputDirectory, "oracle.png"));
        if (error != Error.Ok)
        {
            throw new IOException($"Oracle PNG save failed: {error}.");
        }
        if (!viewportBytes.AsSpan().SequenceEqual(oracleBytes))
        {
            throw new InvalidDataException(
                $"CVG-CAP-001: Godot viewport {viewport.GetWidth()}x" +
                $"{viewport.GetHeight()} differs from the CPU pack oracle.");
        }

        var metadata = JsonSerializer.SerializeToUtf8Bytes(new
        {
            packDigest = _adapter.Pack.PackDigest,
            planId = _plan!.Id,
            planDigest = PackDigests.Bytes(planBytes),
            pixelDigest = $"sha256:{Convert.ToHexString(
                SHA256.HashData(viewportBytes)).ToLowerInvariant()}",
            oraclePixelDigest = $"sha256:{Convert.ToHexString(
                SHA256.HashData(oracleBytes)).ToLowerInvariant()}",
            viewportMatchesOracle = true,
            _plan.Width,
            _plan.Height,
            paletteId = _paletteId,
            entryCount = _plan.Entries.Length,
            visualCount = _adapter.Pack.Visuals.Length,
            atlasCount = _adapter.Pack.Atlases.Length,
            validationDiagnostics = _adapter.Diagnostics.Select(static item =>
                new { item.Code, severity = item.Severity.ToString() }),
            boundedNodeCount = 1
        });
        File.WriteAllBytes(
            Path.Combine(outputDirectory, "capture.json"),
            metadata);
        GD.Print(
            $"CVG-E4-CAPTURE: {PackDigests.Bytes(viewportBytes)}; " +
            $"load {loadMilliseconds:F3} ms.");
    }

    private void CyclePalette()
    {
        var palettes = _adapter!.Pack.Palettes;
        var index = Array.FindIndex(
            palettes.ToArray(),
            item => item.Id == _paletteId);
        _paletteId = palettes[(index + 1) % palettes.Length].Id;
    }

    private void CycleFamily()
    {
        var current = _adapter!.Pack.Visuals[_visualIndex];
        var families = _adapter.Pack.Visuals
            .Select(static item => item.FamilyId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var familyIndex = Array.IndexOf(families, current.FamilyId);
        var next = families[(familyIndex + 1) % families.Length];
        _visualIndex = Array.FindIndex(
            _adapter.Pack.Visuals.ToArray(),
            item => item.FamilyId == next);
    }

    private void CycleVariant() => CycleWithin(static (candidate, current) =>
        candidate.FamilyId == current.FamilyId &&
        candidate.NativeSize == current.NativeSize &&
        candidate.AdjacencyMask == current.AdjacencyMask);

    private void CycleMask() => CycleWithin(static (candidate, current) =>
        candidate.FamilyId == current.FamilyId &&
        candidate.NativeSize == current.NativeSize &&
        candidate.VariantOrdinal == current.VariantOrdinal);

    private void CycleLayer()
    {
        var current = _adapter!.Pack.Visuals[_visualIndex];
        var layers = _adapter.Pack.Visuals
            .Select(static item => item.Layer)
            .Distinct()
            .Order()
            .ToArray();
        var layerIndex = Array.IndexOf(layers, current.Layer);
        var next = layers[(layerIndex + 1) % layers.Length];
        _visualIndex = Array.FindIndex(
            _adapter.Pack.Visuals.ToArray(),
            item => item.Layer == next);
    }

    private void CycleWithin(
        Func<VisualRecord, VisualRecord, bool> predicate)
    {
        var current = _adapter!.Pack.Visuals[_visualIndex];
        var matches = _adapter.Pack.Visuals
            .Select((visual, index) => (visual, index))
            .Where(item => predicate(item.visual, current))
            .Select(static item => item.index)
            .ToArray();
        var position = Array.IndexOf(matches, _visualIndex);
        _visualIndex = matches[(position + 1) % matches.Length];
    }

    private void UpdateMetadata()
    {
        var visual = _adapter!.Pack.Visuals[_visualIndex];
        var atlas = _adapter.Pack.Atlases.First(item => item.Id == visual.AtlasId);
        _metadata!.Text =
            $"P palette | F family | V variant | M mask | L layer | N specimen | " +
            $"S scale\n{visual.Id} | family={visual.FamilyId} | " +
            $"size={visual.NativeSize} | variant={visual.VariantOrdinal} | " +
            $"mask={visual.AdjacencyMask?.ToString() ?? "none"} | " +
            $"layer={visual.Layer} | rect={visual.Rectangle} | " +
            $"anchor={visual.Anchor} | scale={_inspectionScale}x | " +
            $"pack={_adapter.Pack.PackDigest} | atlas={atlas.Digest} | " +
            $"geometry={visual.GeometryDigest} | " +
            $"diagnostics={_adapter.Diagnostics.Count}";
    }

    private static Dictionary<string, string?> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index++)
        {
            var name = args[index];
            if (name == "--acceptance")
            {
                result.Add(name, null);
                continue;
            }
            if (name is not ("--pack" or "--plan" or "--output") ||
                index + 1 >= args.Length)
            {
                throw new ArgumentException($"Unknown or incomplete argument '{name}'.");
            }
            result.Add(name, args[++index]);
        }
        return result;
    }

    private static string Required(
        IReadOnlyDictionary<string, string?> arguments,
        string name) =>
        arguments.TryGetValue(name, out var value) &&
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required argument '{name}'.");
}
