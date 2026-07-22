using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Chronicle.Core;
using Chronicle.VisualPack;
using Chronicle.Visuals;
using Godot;

[GlobalClass]
public partial class WorldAtlasInspector : Node
{
    private const long InitialSeed = 41_337;
    private const string PlayerSavePath = "user://slice0_chronicle.json";
    private const int MaxVisualPreviewRequestWidth = 64;
    private static readonly int[] RequestWidths = [1024, 512, 256, 128, 64, 32];

    private static readonly Color PanelColor = new(0.027f, 0.043f, 0.065f, 0.98f);
    private static readonly Color PanelInsetColor = new(0.012f, 0.022f, 0.036f, 1f);
    private static readonly Color TextColor = new(0.84f, 0.9f, 0.95f);
    private static readonly Color MutedTextColor = new(0.56f, 0.68f, 0.78f);
    private static readonly Color AccentColor = new(0.94f, 0.78f, 0.31f);
    private static readonly Color GrassColor = new(0.20f, 0.43f, 0.24f);
    private static readonly Color SoilColor = new(0.34f, 0.28f, 0.18f);
    private static readonly Color WaterColor = new(0.08f, 0.33f, 0.65f);
    private static readonly Color OpenSkyColor = new(0.045f, 0.14f, 0.25f);
    private static readonly Color VegetationColor = new(0.06f, 0.30f, 0.13f);
    private static readonly Color StoneColor = new(0.48f, 0.50f, 0.53f);
    private static readonly Color CloudColor = new(0.72f, 0.84f, 0.91f);
    private static readonly Color LandmarkColor = new(1f, 0.72f, 0.12f);
    private static readonly Color LooseStoneColor = new(0.89f, 0.92f, 0.95f);
    private static readonly Color AddressGuideColor = new(0.82f, 0.93f, 1f);
    private static readonly Color RequestBoundColor = new(1f, 0.34f, 0.25f);

    private ChronicleState _generationInput = ChronicleState.Begin(InitialSeed);
    private long _seed = InitialSeed;
    private string _stratum = SurfacePatch.SurfaceStratum;
    private long _centerX;
    private long _centerY;
    private int _zoomIndex;
    private bool _showSemanticClasses = true;
    private bool _showAbsoluteAddresses;
    private bool _showRequestBounds;
    private bool _showMotifIdentity;
    private bool _showDurableIdentity;
    private bool _showVisualGrammar;
    private bool _verifyGate3B;
    private int _visualCellSize;
    private CompiledVisualPack _visualPack = null!;
    private VisualRenderPlan? _visualPlan;
    private WorldArea? _area;
    private Image? _raster;
    // ponytail: one texture is sufficient for this bounded, read-only query.
    private ImageTexture? _texture;
    private int _textureWidth;
    private int _textureHeight;
    private double _lastBuildMilliseconds;
    private int _nodeCount;
    private string _status = "Ready. This inspector never opens a player Chronicle.";
    private string? _lastCapturePngPath;
    private string? _lastCaptureMetadataPath;

    private TextureRect _rasterView = null!;
    private Label _mapReadout = null!;
    private Label _addressReadout = null!;
    private Label _stateReadout = null!;
    private Label _overlayReadout = null!;
    private Label _statusReadout = null!;
    private LineEdit _seedEntry = null!;
    private Button _fixture41337Button = null!;
    private Button _fixture41338Button = null!;
    private Button _fixture90421Button = null!;
    private Button _applySeedButton = null!;
    private Button _surfaceButton = null!;
    private Button _skyButton = null!;
    private Button _originButton = null!;
    private Button _incarnationButton = null!;
    private Button _bellButton = null!;
    private Button _panNorthButton = null!;
    private Button _panSouthButton = null!;
    private Button _panWestButton = null!;
    private Button _panEastButton = null!;
    private Button _zoomInButton = null!;
    private Button _zoomOutButton = null!;
    private CheckButton _semanticClassesToggle = null!;
    private CheckButton _absoluteAddressesToggle = null!;
    private CheckButton _requestBoundsToggle = null!;
    private CheckButton _motifIdentityToggle = null!;
    private CheckButton _durableIdentityToggle = null!;
    private CheckButton _visualGrammarToggle = null!;
    private Button _captureButton = null!;

    public override void _Ready()
    {
        var arguments = OS.GetCmdlineUserArgs();
        _verifyGate3B = arguments.Contains("--verify-gate3b-atlas", StringComparer.Ordinal);
        _visualCellSize = RequestedVisualCellSize(arguments);
        _visualPack = PackagedVisualPackLoader.Load(arguments, _visualCellSize);
        BuildInterface();
        _nodeCount = CountNodes(this);
        RebuildArea();

        if (_verifyGate3B ||
            arguments.Contains("--verify-world-atlas", StringComparer.Ordinal))
        {
            Callable.From(RunAtlasAcceptance).CallDeferred();
        }
    }

    private void BuildInterface()
    {
        var background = new ColorRect
        {
            Name = "AtlasBackground",
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            Color = new Color(0.008f, 0.014f, 0.024f),
        };
        AddChild(background);

        var mapPanel = new ColorRect
        {
            Name = "AtlasMapPanel",
            Position = new Vector2(18, 18),
            Size = new Vector2(868, 684),
            Color = PanelColor,
        };
        AddChild(mapPanel);

        _mapReadout = AddLabel(
            mapPanel,
            "AtlasMapReadout",
            new Vector2(12, 9),
            new Vector2(844, 24),
            14,
            AccentColor);
        _rasterView = new TextureRect
        {
            Name = "AtlasRaster",
            Position = new Vector2(10, 38),
            Size = new Vector2(848, 636),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _rasterView.GuiInput += OnRasterInput;
        mapPanel.AddChild(_rasterView);
        _addressReadout = AddLabel(
            mapPanel,
            "AbsoluteAddressReadout",
            new Vector2(16, 650),
            new Vector2(836, 20),
            12,
            Colors.White);
        _addressReadout.MouseFilter = Control.MouseFilterEnum.Ignore;
        _addressReadout.AddThemeColorOverride("font_outline_color", Colors.Black);
        _addressReadout.AddThemeConstantOverride("outline_size", 4);

        var controls = new ColorRect
        {
            Name = "AtlasControls",
            Position = new Vector2(904, 18),
            Size = new Vector2(358, 684),
            Color = PanelColor,
        };
        AddChild(controls);

        var title = AddLabel(
            controls,
            "AtlasTitle",
            new Vector2(12, 8),
            new Vector2(334, 24),
            18,
            AccentColor);
        title.Text = "WORLD ATLAS INSPECTOR";
        _stateReadout = AddLabel(
            controls,
            "AtlasStateReadout",
            new Vector2(12, 34),
            new Vector2(334, 72),
            13,
            TextColor);
        _stateReadout.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var fixturesLabel = AddLabel(
            controls,
            "FixtureSeedsLabel",
            new Vector2(12, 112),
            new Vector2(334, 18),
            12,
            MutedTextColor);
        fixturesLabel.Text = "FIXTURE SEEDS";
        _fixture41337Button = AddButton(
            controls,
            "Fixture41337",
            "41337",
            new Vector2(12, 132),
            new Vector2(104, 30),
            () => SelectSeed(41_337));
        _fixture41338Button = AddButton(
            controls,
            "Fixture41338",
            "41338",
            new Vector2(126, 132),
            new Vector2(104, 30),
            () => SelectSeed(41_338));
        _fixture90421Button = AddButton(
            controls,
            "Fixture90421",
            "90421",
            new Vector2(240, 132),
            new Vector2(106, 30),
            () => SelectSeed(90_421));

        _seedEntry = new LineEdit
        {
            Name = "NumericSeedEntry",
            Position = new Vector2(12, 170),
            Size = new Vector2(220, 30),
            Text = InitialSeed.ToString(CultureInfo.InvariantCulture),
            PlaceholderText = "numeric seed",
        };
        _seedEntry.TextSubmitted += _ => ApplyNumericSeed();
        controls.AddChild(_seedEntry);
        _applySeedButton = AddButton(
            controls,
            "ApplyNumericSeed",
            "APPLY",
            new Vector2(240, 170),
            new Vector2(106, 30),
            ApplyNumericSeed);

        var stratumLabel = AddLabel(
            controls,
            "StratumLabel",
            new Vector2(12, 208),
            new Vector2(334, 18),
            12,
            MutedTextColor);
        stratumLabel.Text = "STRATUM";
        _surfaceButton = AddButton(
            controls,
            "SurfaceStratum",
            "SURFACE",
            new Vector2(12, 228),
            new Vector2(164, 30),
            () => SelectStratum(SurfacePatch.SurfaceStratum));
        _skyButton = AddButton(
            controls,
            "SkyStratum",
            "SKY",
            new Vector2(182, 228),
            new Vector2(164, 30),
            () => SelectStratum(SkyStratum.StratumName));

        var recenterLabel = AddLabel(
            controls,
            "RecenterLabel",
            new Vector2(12, 266),
            new Vector2(334, 18),
            12,
            MutedTextColor);
        recenterLabel.Text = "RECENTER";
        _originButton = AddButton(
            controls,
            "RecenterOrigin",
            "ORIGIN",
            new Vector2(12, 286),
            new Vector2(104, 30),
            RecenterOrigin);
        _incarnationButton = AddButton(
            controls,
            "RecenterIncarnation",
            "INCARNATION",
            new Vector2(126, 286),
            new Vector2(104, 30),
            RecenterIncarnation);
        _bellButton = AddButton(
            controls,
            "RecenterBell",
            "BELL",
            new Vector2(240, 286),
            new Vector2(106, 30),
            RecenterBell);

        var panLabel = AddLabel(
            controls,
            "PanLabel",
            new Vector2(12, 324),
            new Vector2(334, 18),
            12,
            MutedTextColor);
        panLabel.Text = "PAN";
        _panNorthButton = AddButton(
            controls,
            "PanNorth",
            "N",
            new Vector2(126, 344),
            new Vector2(104, 30),
            () => Pan(0, -1));
        _panWestButton = AddButton(
            controls,
            "PanWest",
            "W",
            new Vector2(12, 380),
            new Vector2(104, 30),
            () => Pan(-1, 0));
        _panSouthButton = AddButton(
            controls,
            "PanSouth",
            "S",
            new Vector2(126, 380),
            new Vector2(104, 30),
            () => Pan(0, 1));
        _panEastButton = AddButton(
            controls,
            "PanEast",
            "E",
            new Vector2(240, 380),
            new Vector2(106, 30),
            () => Pan(1, 0));

        var zoomLabel = AddLabel(
            controls,
            "ZoomLabel",
            new Vector2(12, 418),
            new Vector2(334, 18),
            12,
            MutedTextColor);
        zoomLabel.Text = "ZOOM REQUEST";
        _zoomOutButton = AddButton(
            controls,
            "ZoomOut",
            "ZOOM OUT",
            new Vector2(12, 438),
            new Vector2(164, 30),
            () => Zoom(-1));
        _zoomInButton = AddButton(
            controls,
            "ZoomIn",
            "ZOOM IN",
            new Vector2(182, 438),
            new Vector2(164, 30),
            () => Zoom(1));

        _visualGrammarToggle = AddToggle(
            controls,
            "VisualGrammarToggle",
            "VISUAL GRAMMAR PREVIEW",
            new Vector2(12, 474),
            _showVisualGrammar,
            ToggleVisualGrammar);
        _semanticClassesToggle = AddToggle(
            controls,
            "SemanticClassesToggle",
            "semantic classes",
            new Vector2(12, 494),
            _showSemanticClasses,
            ToggleSemanticClasses);
        _absoluteAddressesToggle = AddToggle(
            controls,
            "AbsoluteAddressesToggle",
            "absolute addresses",
            new Vector2(12, 514),
            _showAbsoluteAddresses,
            ToggleAbsoluteAddresses);
        _requestBoundsToggle = AddToggle(
            controls,
            "RequestBoundsToggle",
            "request bound",
            new Vector2(12, 534),
            _showRequestBounds,
            ToggleRequestBounds);
        _motifIdentityToggle = AddToggle(
            controls,
            "MotifIdentityToggle",
            "motif identity",
            new Vector2(12, 554),
            _showMotifIdentity,
            ToggleMotifIdentity);
        _durableIdentityToggle = AddToggle(
            controls,
            "DurableIdentityToggle",
            "durable identity",
            new Vector2(12, 574),
            _showDurableIdentity,
            ToggleDurableIdentity);

        _captureButton = AddButton(
            controls,
            "CaptureExport",
            "CAPTURE / EXPORT",
            new Vector2(12, 606),
            new Vector2(334, 34),
            Capture);
        _overlayReadout = AddLabel(
            controls,
            "OverlayReadout",
            new Vector2(12, 646),
            new Vector2(334, 20),
            11,
            MutedTextColor);
        _statusReadout = AddLabel(
            controls,
            "AtlasStatusReadout",
            new Vector2(12, 666),
            new Vector2(334, 18),
            11,
            TextColor);
    }

    private void SelectSeed(long seed)
    {
        _seed = seed;
        _generationInput = ChronicleState.Begin(seed);
        _seedEntry.Text = seed.ToString(CultureInfo.InvariantCulture);
        RebuildArea($"Fixture seed {seed} selected.");
    }

    private void ApplyNumericSeed()
    {
        if (!long.TryParse(
                _seedEntry.Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var seed))
        {
            _status = "Enter a signed whole-number seed.";
            RefreshReadouts();
            return;
        }

        SelectSeed(seed);
    }

    private void SelectStratum(string stratum)
    {
        _stratum = stratum;
        RebuildArea($"Inspecting {stratum}.");
    }

    private void RecenterOrigin()
    {
        _centerX = 0;
        _centerY = 0;
        RebuildArea("Centered on the origin.");
    }

    private void RecenterIncarnation()
    {
        _stratum = _generationInput.Address.Stratum;
        _centerX = _generationInput.Address.X;
        _centerY = _generationInput.Address.Y;
        RebuildArea("Centered on the ephemeral Incarnation.");
    }

    private void RecenterBell()
    {
        _stratum = SkyStratum.StratumName;
        _centerX = SkyStratum.LandmarkAddress.X;
        _centerY = SkyStratum.LandmarkAddress.Y;
        RebuildArea("Centered on The Bell That Fell Up.");
    }

    private void Pan(int directionX, int directionY)
    {
        var step = RequestWidth / 2;
        _centerX = VisualViewportBounds.OffsetClamped(
            _centerX,
            directionX * (long)step);
        _centerY = VisualViewportBounds.OffsetClamped(
            _centerY,
            directionY * (long)step);
        RebuildArea("Bounded request moved; the Stratum remains unbounded.");
    }

    private void Zoom(int change)
    {
        var next = Math.Clamp(_zoomIndex + change, 0, RequestWidths.Length - 1);
        if (next == _zoomIndex)
        {
            return;
        }

        _zoomIndex = next;
        RebuildArea($"Request changed to {RequestWidth} × {RequestWidth} addresses.");
    }

    private void ToggleSemanticClasses()
    {
        _showSemanticClasses = !_showSemanticClasses;
        _semanticClassesToggle.SetPressedNoSignal(_showSemanticClasses);
        RasterizeCurrentArea("Semantic-class diagnostic changed.");
    }

    private void ToggleAbsoluteAddresses()
    {
        _showAbsoluteAddresses = !_showAbsoluteAddresses;
        _absoluteAddressesToggle.SetPressedNoSignal(_showAbsoluteAddresses);
        if (_showAbsoluteAddresses)
        {
            ShowAddressAtRasterPosition(_rasterView.Size / 2f);
        }
        else
        {
            _addressReadout.Text = string.Empty;
        }

        RasterizeCurrentArea("Absolute-address diagnostic changed.");
    }

    private void ToggleRequestBounds()
    {
        _showRequestBounds = !_showRequestBounds;
        _requestBoundsToggle.SetPressedNoSignal(_showRequestBounds);
        RasterizeCurrentArea("Request-bound diagnostic changed.");
    }

    private void ToggleMotifIdentity()
    {
        _showMotifIdentity = !_showMotifIdentity;
        _motifIdentityToggle.SetPressedNoSignal(_showMotifIdentity);
        RasterizeCurrentArea("Motif-identity diagnostic changed.");
    }

    private void ToggleDurableIdentity()
    {
        _showDurableIdentity = !_showDurableIdentity;
        _durableIdentityToggle.SetPressedNoSignal(_showDurableIdentity);
        RasterizeCurrentArea("Durable-identity diagnostic changed.");
    }

    private void ToggleVisualGrammar()
    {
        if (RequestWidth > MaxVisualPreviewRequestWidth)
        {
            _showVisualGrammar = false;
            _visualGrammarToggle.SetPressedNoSignal(false);
            _status = "Zoom to 64 × 64 or 32 × 32 before enabling Visual Grammar preview.";
            RefreshReadouts();
            return;
        }

        _showVisualGrammar = !_showVisualGrammar;
        _visualGrammarToggle.SetPressedNoSignal(_showVisualGrammar);
        _visualPlan = null;
        RasterizeCurrentArea(
            _showVisualGrammar
                ? $"Visual Grammar preview: {_visualCellSize} native pixels."
                : "Semantic debug presentation restored.");
    }

    private int RequestWidth => RequestWidths[_zoomIndex];

    private void RebuildArea(string? status = null)
    {
        if (status is not null)
        {
            _status = status;
        }

        var visualPreviewAvailable = RequestWidth <= MaxVisualPreviewRequestWidth;
        _visualGrammarToggle.Disabled = !visualPreviewAvailable;
        if (!visualPreviewAvailable && _showVisualGrammar)
        {
            _showVisualGrammar = false;
            _visualGrammarToggle.SetPressedNoSignal(false);
            _status =
                "Visual Grammar preview closed; zoom to 64 × 64 or 32 × 32 to reopen it.";
        }

        var stopwatch = Stopwatch.StartNew();
        var size = RequestWidth;
        var bounds = VisualViewportBounds.Centered(
            _centerX,
            _centerY,
            size,
            size);
        _area = WorldArea.Generate(_generationInput, _stratum, bounds);
        _visualPlan = null;
        SetRaster(Rasterize(_area));
        stopwatch.Stop();
        _lastBuildMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        RefreshReadouts();
    }

    private void RasterizeCurrentArea(string status)
    {
        if (_area is null)
        {
            return;
        }

        _status = status;
        var stopwatch = Stopwatch.StartNew();
        SetRaster(Rasterize(_area));
        stopwatch.Stop();
        _lastBuildMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        RefreshReadouts();
    }

    private Image Rasterize(WorldArea area)
    {
        if (_showVisualGrammar)
        {
            if (area.Bounds.Width > MaxVisualPreviewRequestWidth)
            {
                throw new InvalidOperationException(
                    "Visual Grammar preview is limited to bounded local requests.");
            }

            var plan = RequireVisualPlan(area);
            return area.Bounds.Width <= 64
                ? VisualPackGodotAdapter.RasterizeNative(_visualPack, plan)
                : VisualPackGodotAdapter.RasterizeOverview(_visualPack, plan);
        }

        var width = area.Bounds.Width;
        var height = area.Bounds.Height;
        var pixels = new byte[checked(width * height * 4)];
        var offset = 0;
        for (var index = 0; index < area.Cells.Count; index++)
        {
            var cell = area.Cells[index];
            var color = CellColor(cell);
            var column = index % width;
            var row = index / width;
            if (cell.Feature is null && cell.DurableIdentity is null)
            {
                if (_showAbsoluteAddresses && IsAddressGuide(cell.Address, width))
                {
                    color = AddressGuideColor;
                }

                if (_showRequestBounds && IsRequestBound(column, row, width, height))
                {
                    color = RequestBoundColor;
                }
            }

            pixels[offset++] = (byte)color.R8;
            pixels[offset++] = (byte)color.G8;
            pixels[offset++] = (byte)color.B8;
            pixels[offset++] = (byte)color.A8;
        }

        return Image.CreateFromData(width, height, false, Image.Format.Rgba8, pixels);
    }

    private Color CellColor(WorldCell cell)
    {
        var color = cell.Ground switch
        {
            WorldGround.Grass => GrassColor,
            WorldGround.Soil => SoilColor,
            WorldGround.Water => WaterColor,
            WorldGround.OpenSky => OpenSkyColor,
            _ => Colors.Magenta,
        };

        if (_showSemanticClasses)
        {
            color = cell.Feature switch
            {
                WorldFeature.Vegetation => VegetationColor,
                WorldFeature.Stone => StoneColor,
                WorldFeature.Cloud => CloudColor,
                WorldFeature.Landmark => LandmarkColor,
                _ => color,
            };
        }

        if (_showMotifIdentity && cell.MotifIdentity is { } motif)
        {
            color = color.Lerp(MotifTint(motif), 0.30f);
        }

        if (_showDurableIdentity && cell.DurableIdentity is { } durable)
        {
            return string.Equals(
                durable,
                ChronicleState.LooseStoneIdentity,
                StringComparison.Ordinal)
                ? LooseStoneColor
                : LandmarkColor;
        }

        return color;
    }

    private static Color MotifTint(string motif) => motif switch
    {
        "surface-water-ridge-crossing" => new Color(0.75f, 0.32f, 0.67f),
        "surface-water-main" => new Color(0.12f, 0.73f, 0.84f),
        "surface-ridge-main" => new Color(0.76f, 0.49f, 0.20f),
        "surface-clearing-main" => new Color(0.68f, 0.68f, 0.31f),
        "sky-open-lane" => new Color(0.20f, 0.57f, 0.81f),
        _ when motif.StartsWith("surface-grove-", StringComparison.Ordinal) =>
            new Color(0.23f, 0.72f, 0.35f),
        _ when motif.StartsWith("sky-cloud-bank-", StringComparison.Ordinal) =>
            new Color(0.92f, 0.92f, 0.98f),
        _ => Colors.Magenta,
    };

    private void OnRasterInput(InputEvent input)
    {
        if (_showAbsoluteAddresses && input is InputEventMouseMotion motion)
        {
            ShowAddressAtRasterPosition(motion.Position);
        }
    }

    private void ShowAddressAtRasterPosition(Vector2 position)
    {
        var area = RequireArea();
        if (_rasterView.Size.X <= 0 || _rasterView.Size.Y <= 0)
        {
            return;
        }

        var column = Math.Clamp(
            (int)(position.X / _rasterView.Size.X * area.Bounds.Width),
            0,
            area.Bounds.Width - 1);
        var row = Math.Clamp(
            (int)(position.Y / _rasterView.Size.Y * area.Bounds.Height),
            0,
            area.Bounds.Height - 1);
        var cell = area.Cells[row * area.Bounds.Width + column];
        _addressReadout.Text =
            $"{cell.Address}  ground={cell.Ground}  feature={cell.Feature?.ToString() ?? "—"}  " +
            $"motif={cell.MotifIdentity ?? "—"}  durable={cell.DurableIdentity ?? "—"}";
    }

    private static bool IsAddressGuide(WorldAddress address, int width)
    {
        var spacing = Math.Max(1, width / 8);
        return FloorMod(address.X, spacing) == 0 ||
            FloorMod(address.Y, spacing) == 0 ||
            address.X == 0 ||
            address.Y == 0;
    }

    private static long FloorMod(long value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    private static bool IsRequestBound(int x, int y, int width, int height) =>
        x == 0 || y == 0 || x == width - 1 || y == height - 1;

    private void SetRaster(Image image)
    {
        _raster = image;
        if (_texture is null)
        {
            _texture = ImageTexture.CreateFromImage(image);
            _rasterView.Texture = _texture;
        }
        else if (_textureWidth == image.GetWidth() && _textureHeight == image.GetHeight())
        {
            _texture.Update(image);
        }
        else
        {
            _texture.SetImage(image);
        }

        _textureWidth = image.GetWidth();
        _textureHeight = image.GetHeight();
    }

    private void Capture()
    {
        var area = RequireArea();
        var raster = RequireRaster();
        var directory = CaptureDirectory;
        Directory.CreateDirectory(directory);

        var fileTokens = new List<string>
        {
            "atlas",
            $"s{CoordinateToken(_seed)}",
            $"g{_generationInput.WorldGrammarVersion}",
            _stratum,
            $"x{CoordinateToken(area.Bounds.MinX)}",
            $"y{CoordinateToken(area.Bounds.MinY)}",
            $"w{area.Bounds.Width}",
            $"h{area.Bounds.Height}",
            $"z{RequestWidth}",
        };
        if (_showVisualGrammar)
        {
            fileTokens.Add($"visual{_visualCellSize}");
        }

        fileTokens.Add(OverlayToken());
        var fileStem = string.Join("_", fileTokens);
        _lastCapturePngPath = Path.Combine(directory, $"{fileStem}.png");
        _lastCaptureMetadataPath = Path.Combine(directory, $"{fileStem}.json");

        var pngError = raster.SavePng(_lastCapturePngPath);
        if (pngError != Error.Ok)
        {
            throw new InvalidOperationException($"PNG export failed with {pngError}.");
        }

        var metadata = new AtlasCaptureMetadata(
            _seed,
            _generationInput.WorldGrammarVersion,
            _stratum,
            area.Bounds,
            RequestWidth,
            new AtlasOverlayMetadata(
                _showSemanticClasses,
                _showAbsoluteAddresses,
                _showRequestBounds,
                _showMotifIdentity,
                _showDurableIdentity),
            _showVisualGrammar ? "visual-grammar" : "semantic-debug",
            _showVisualGrammar ? _visualPack.PackId : null,
            _showVisualGrammar ? _visualPack.Digest : null,
            _showVisualGrammar ? _visualCellSize : null,
            _showVisualGrammar ? RequireVisualPlan(area).Digest : null);
        File.WriteAllText(
            _lastCaptureMetadataPath,
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
        _status = $"Captured {Path.GetFileName(_lastCapturePngPath)}.";
        RefreshReadouts();
    }

    private static string CaptureDirectory => Path.GetFullPath(
        Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", ".tools", "atlas-captures"));

    private string OverlayToken() =>
        $"o{Flag(_showSemanticClasses)}{Flag(_showAbsoluteAddresses)}{Flag(_showRequestBounds)}" +
        $"{Flag(_showMotifIdentity)}{Flag(_showDurableIdentity)}";

    private static int Flag(bool value) => value ? 1 : 0;

    private static string CoordinateToken(long value) => value < 0
        ? $"n{(-(Int128)value).ToString(CultureInfo.InvariantCulture)}"
        : value.ToString(CultureInfo.InvariantCulture);

    private void RefreshReadouts()
    {
        if (_area is null)
        {
            return;
        }

        var bounds = _area.Bounds;
        _mapReadout.Text =
            $"{_stratum.ToUpperInvariant()}  seed {_seed}  " +
            $"[{bounds.MinX}, {bounds.MinY}] {bounds.Width} × {bounds.Height}  " +
            $"{(_showVisualGrammar ? $"visual {_visualCellSize}px" : "semantic")}  " +
            $"build {_lastBuildMilliseconds:F0} ms  nodes {_nodeCount}";
        _stateReadout.Text =
            $"READ-ONLY CORE QUERY\n" +
            $"grammar v{_generationInput.WorldGrammarVersion}; center ({_centerX}, {_centerY})\n" +
            $"ephemeral Incarnation: {_generationInput.Address}\n" +
            "No player save is opened, created, or advanced.";
        _overlayReadout.Text =
            $"{(_showVisualGrammar ? $"visual {_visualCellSize}px" : "semantic debug")} · " +
            $"C{Flag(_showSemanticClasses)} A{Flag(_showAbsoluteAddresses)} " +
            $"B{Flag(_showRequestBounds)} M{Flag(_showMotifIdentity)} D{Flag(_showDurableIdentity)}";
        _statusReadout.Text = _status;
        _zoomInButton.Disabled = _zoomIndex == RequestWidths.Length - 1;
        _zoomOutButton.Disabled = _zoomIndex == 0;
    }

    private static Label AddLabel(
        Control parent,
        string name,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color)
    {
        var label = new Label
        {
            Name = name,
            Position = position,
            Size = size,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
        return label;
    }

    private static Button AddButton(
        Control parent,
        string name,
        string text,
        Vector2 position,
        Vector2 size,
        Action action)
    {
        var button = new Button
        {
            Name = name,
            Position = position,
            Size = size,
            Text = text,
            FocusMode = Control.FocusModeEnum.All,
        };
        button.AddThemeFontSizeOverride("font_size", 12);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    private static CheckButton AddToggle(
        Control parent,
        string name,
        string text,
        Vector2 position,
        bool initialValue,
        Action action)
    {
        var toggle = new CheckButton
        {
            Name = name,
            Position = position,
            Size = new Vector2(334, 20),
            Text = text,
            ButtonPressed = initialValue,
            FocusMode = Control.FocusModeEnum.All,
        };
        toggle.AddThemeFontSizeOverride("font_size", 12);
        toggle.Pressed += action;
        parent.AddChild(toggle);
        return toggle;
    }

    private void RunAtlasAcceptance()
    {
        try
        {
            var playerSaveBefore = CapturePlayerSaveFingerprint();

            var initial = RequireArea();
            Verify(
                initial.Bounds == new WorldRectangle(-512, -512, 1024, 1024),
                "Initial inspector request must be the 1024 × 1024 origin overview.");
            Verify(
                RequireRaster().GetWidth() == 1024 && RequireRaster().GetHeight() == 1024,
                "Initial inspector raster must match the 1024 × 1024 Core request.");
            Verify(_nodeCount <= 48, "Atlas overview must not create Nodes per World Address.");
            if (_verifyGate3B)
            {
                Verify(
                    _visualGrammarToggle.Disabled &&
                    !_showVisualGrammar &&
                    _visualPlan is null,
                    "The million-cell overview must keep visual preview disabled until a bounded local request.");
            }

            var originalEdge = checked(initial.Bounds.MinX + initial.Bounds.Width);
            Press(_panEastButton);
            var panned = RequireArea();
            Verify(
                panned.Bounds.MinX < originalEdge &&
                checked(panned.Bounds.MinX + panned.Bounds.Width) > originalEdge,
                "Panning must carry the initial request edge into the next bounded query.");

            Press(_zoomInButton);
            var zoomed = RequireArea();
            Verify(
                zoomed.Bounds.Width == 512 && zoomed.Bounds.Height == 512 &&
                zoomed.Bounds != initial.Bounds,
                "Zoom must issue a smaller bounded Core request.");

            Press(_fixture41338Button);
            VerifyInspectorInput(41_338);
            Press(_fixture90421Button);
            VerifyInspectorInput(90_421);
            _seedEntry.Text = "41337";
            Press(_applySeedButton);
            VerifyInspectorInput(41_337);

            Press(_surfaceButton);
            Press(_originButton);
            var surface = RequireArea();
            Verify(
                surface.Cells.Any(cell => cell.Ground == WorldGround.Water) &&
                surface.Cells.Any(cell => cell.Feature == WorldFeature.Vegetation) &&
                surface.Cells.Any(cell => cell.Feature == WorldFeature.Stone),
                "Surface request must retain Core water, vegetation, and stone semantics.");
            Verify(
                surface.Cells.Any(cell =>
                    cell.Subject(WorldSubjectKind.Creature) is { Identity: var bruteIdentity } &&
                    string.Equals(
                        bruteIdentity,
                        WorldArea.GeneratedMireBruteIdentity(_generationInput.Seed),
                        StringComparison.Ordinal)) &&
                surface.Cells.Any(cell =>
                    cell.Subject(WorldSubjectKind.Target) is
                    {
                        Archetype: WorldSubjects.BasaltArchetype,
                        Identity: var basaltIdentity,
                    } &&
                    string.Equals(
                        basaltIdentity,
                        WorldArea.GeneratedBasaltIdentity(_generationInput.Seed),
                        StringComparison.Ordinal)),
                "World Grammar v4 Inspector requests must expose the generated Mire Brute and basalt Target identities.");

            var inputBeforeOverlays = _generationInput;
            Press(_semanticClassesToggle);
            Verify(!_showSemanticClasses, "Semantic-class toggle must affect only presentation state.");
            Press(_semanticClassesToggle);
            Press(_absoluteAddressesToggle);
            ShowAddressAtRasterPosition(Vector2.Zero);
            Verify(
                _addressReadout.Text.Contains(
                    $"({surface.Bounds.MinX}, {surface.Bounds.MinY})",
                    StringComparison.Ordinal),
                "Absolute-address diagnostic must expose the hovered cell's real World Address.");
            Press(_requestBoundsToggle);
            Press(_motifIdentityToggle);
            Press(_durableIdentityToggle);
            Verify(
                _showSemanticClasses && _showAbsoluteAddresses && _showRequestBounds &&
                _showMotifIdentity && _showDurableIdentity,
                "Diagnostic controls must expose their presentation state.");
            Verify(
                _generationInput == inputBeforeOverlays,
                "Diagnostic actions must not mutate the generation input.");

            Press(_incarnationButton);
            Verify(
                _stratum == _generationInput.Address.Stratum &&
                _centerX == _generationInput.Address.X &&
                _centerY == _generationInput.Address.Y,
                "Incarnation recenter must remain an inspector-only query change.");
            Press(_skyButton);
            Press(_bellButton);
            var sky = RequireArea();
            Verify(
                sky.Cells.Any(cell => cell.Ground == WorldGround.OpenSky) &&
                sky.Cells.Any(cell => cell.Feature == WorldFeature.Cloud) &&
                sky.Cells.Any(cell => string.Equals(
                    cell.DurableIdentity,
                    SkyStratum.LandmarkName,
                    StringComparison.Ordinal)),
                "Sky request must retain open lanes, cloud banks, and Bell identity.");

            VerifyDeterministicCapture();
            if (_verifyGate3B)
            {
                VerifyGate3BVisualPreview();
            }

            Verify(
                CapturePlayerSaveFingerprint() == playerSaveBefore,
                "Inspector verification changed the existing or absent player save.");

            GD.Print($"GATE3A PLAYER SAVE PRESERVED existing={playerSaveBefore.Exists}");
            GD.Print("GATE3A ATLAS ACCEPTANCE PASS");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GATE3A ATLAS ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void VerifyGate3BVisualPreview()
    {
        var generationBefore = _generationInput;
        var saveBefore = CapturePlayerSaveFingerprint();
        while (!_zoomInButton.Disabled)
        {
            Press(_zoomInButton);
        }

        Verify(RequestWidth == 32, "Gate 3B preview verification requires the 32 × 32 local request.");
        Verify(
            !_visualGrammarToggle.Disabled,
            "The Visual Grammar preview control must enable for bounded local requests.");
        Press(_visualGrammarToggle);
        Verify(_showVisualGrammar, "The Visual Grammar preview control must switch presentation modes.");

        var area = RequireArea();
        var plan = RequireVisualPlan(area);
        Verify(
            plan.PackId == _visualPack.PackId &&
            plan.PackDigest == _visualPack.Digest &&
            plan.CellSize == _visualCellSize,
            "Inspector visual preview must consume the selected compiled pack exactly.");
        Verify(
            RequireRaster().GetWidth() == area.Bounds.Width * _visualCellSize &&
            RequireRaster().GetHeight() == area.Bounds.Height * _visualCellSize,
            "A local visual preview must rasterize the shared plan at native cell resolution.");
        Verify(
            plan.Marks.Any(mark =>
                mark.VisualId == "landmark.bell-that-fell-up" &&
                mark.Address == SkyStratum.LandmarkAddress),
            "The visual preview must retain the Bell's shared Landmark mapping.");

        var firstDigest = plan.Digest;
        RasterizeCurrentArea("Repeated the shared Visual Grammar preview.");
        Verify(
            RequireVisualPlan(RequireArea()).Digest == firstDigest,
            "Equivalent Inspector visual projection must reproduce the render-plan digest.");

        Press(_captureButton);
        VerifyCaptureMetadata();
        var pngPath = _lastCapturePngPath!;
        var metadataPath = _lastCaptureMetadataPath!;
        var png = File.ReadAllBytes(pngPath);
        var metadata = File.ReadAllBytes(metadataPath);
        Press(_captureButton);
        Verify(
            File.ReadAllBytes(pngPath).SequenceEqual(png) &&
            File.ReadAllBytes(metadataPath).SequenceEqual(metadata),
            "Equivalent visual previews must reproduce deterministic capture bytes.");
        Verify(
            _generationInput == generationBefore &&
            CapturePlayerSaveFingerprint() == saveBefore,
            "Visual preview actions must not mutate generation input or the player save.");
        Verify(_nodeCount <= 48, "Visual preview must remain a bounded raster, not Nodes per address.");
        CaptureGate3BSurfaceReviewSet();
        VerifyGoal6BInspectorParity();

        GD.Print(
            $"GATE3B SHARED COMPOSER PLAN PASS pack={_visualPack.PackId} " +
            $"style={_visualPack.StyleVersion} size={_visualCellSize} digest={firstDigest}");
        GD.Print($"GATE3B ATLAS VISUAL PREVIEW PASS size={_visualCellSize}");
    }

    private void VerifyGoal6BInspectorParity()
    {
        var initial = ChronicleState.Begin(InitialSeed);
        var initialPower = initial.PowerHome
            ?? throw new InvalidOperationException("Current World Grammar must expose Power Comes Home state.");
        var context = new ChronicleSimulation(initial).PowerComesHomeContext;
        var site = context.ResonatorSite
            ?? throw new InvalidOperationException("The accepted Home fixture must expose one Resonator site.");
        var lode = initialPower.Lode;
        var sourceIdentity = $"source.hearth-resonator.{InitialSeed.ToString(CultureInfo.InvariantCulture)}";
        var extractedLode = lode with
        {
            Disposition = ResonantLodeDisposition.Loose,
            Address = context.SeamAddress,
            CarrierIncarnationId = null,
        };
        ChronicleState WithPower(
            ResonantLodeState stagedLode,
            HearthResonatorState? source = null) => initial with
            {
                PowerHome = initialPower with
                {
                    Lode = stagedLode,
                    ExtractionProgress = stagedLode.Disposition == ResonantLodeDisposition.Embedded ? 0 : 2,
                    Resonator = source,
                    Commitment = null,
                },
            };

        var stages = new (string Name, ChronicleState State, string[] VisualIds)[]
        {
            (
                "embedded",
                initial,
                ["emphasis.home-source-site", "place.singing-seam.embedded", "resource.resonant-lode.embedded"]),
            (
                "loose",
                WithPower(extractedLode),
                ["emphasis.home-source-site", "place.singing-seam.empty", "resource.resonant-lode.loose"]),
            (
                "carried",
                WithPower(extractedLode with
                {
                    Disposition = ResonantLodeDisposition.Carried,
                    Address = null,
                    CarrierIncarnationId = initial.IncarnationId,
                }),
                ["emphasis.home-source-site", "place.singing-seam.empty", "resource.resonant-lode.carried"]),
            (
                "construction",
                WithPower(
                    extractedLode with { Disposition = ResonantLodeDisposition.Committed, Address = site },
                    new HearthResonatorState(sourceIdentity, site, HearthResonatorPhase.UnderConstruction, 1)),
                ["place.singing-seam.empty", "source.hearth-resonator.construction"]),
            (
                "intact",
                WithPower(
                    extractedLode with { Disposition = ResonantLodeDisposition.Installed, Address = site },
                    new HearthResonatorState(sourceIdentity, site, HearthResonatorPhase.Intact, 3)),
                ["place.singing-seam.empty", "source.hearth-resonator.intact"]),
            (
                "damaged",
                WithPower(
                    extractedLode with { Disposition = ResonantLodeDisposition.Installed, Address = site },
                    new HearthResonatorState(sourceIdentity, site, HearthResonatorPhase.Damaged, 1)),
                ["place.singing-seam.empty", "source.hearth-resonator.damaged"]),
            (
                "destroyed",
                WithPower(
                    extractedLode with { Address = site },
                    new HearthResonatorState(sourceIdentity, site, HearthResonatorPhase.Destroyed, 2)),
                ["place.singing-seam.empty", "resource.resonant-lode.loose", "source.hearth-resonator.destroyed"]),
            (
                "rebuilding",
                WithPower(
                    extractedLode with { Disposition = ResonantLodeDisposition.Committed, Address = site },
                    new HearthResonatorState(sourceIdentity, site, HearthResonatorPhase.Rebuilding, 1)),
                ["place.singing-seam.empty", "source.hearth-resonator.rebuilding"]),
        };
        var visibleBounds = new WorldRectangle(-2, 0, 13, 8);

        foreach (var stage in stages)
        {
            var semanticArea = WorldArea.Generate(
                stage.State,
                SurfacePatch.SurfaceStratum,
                VisualViewportBounds.WithOneCellSemanticHalo(visibleBounds));
            var seamCell = semanticArea.Cells.Single(cell => cell.Address == context.SeamAddress);
            Verify(
                seamCell.Subject(WorldSubjectKind.MaterialSeam) is { } seamSubject &&
                seamSubject.Condition == WorldSubjects.Condition(
                    stage.Name == "embedded" ? SingingSeamVisualState.Embedded : SingingSeamVisualState.Empty),
                $"Goal 6B Inspector '{stage.Name}' must expose the exact Singing Seam semantic state.");
            if (stage.State.PowerHome!.Resonator is { } source)
            {
                var sourceCell = semanticArea.Cells.Single(cell => cell.Address == source.Address);
                Verify(
                    sourceCell.Subject(WorldSubjectKind.LoadSource)?.Condition ==
                        WorldSubjects.Condition(source.Phase),
                    $"Goal 6B Inspector '{stage.Name}' must expose the exact Source semantic state.");
            }

            var plan = VisualGrammar.Compose(
                new VisualCompositionInput(
                    semanticArea,
                    visibleBounds,
                    InitialSeed,
                    _visualPack,
                    _visualPack.StyleVersion,
                    stage.State.Address,
                    TargetAddresses: [],
                    SelectedAddresses: []));
            foreach (var visualId in stage.VisualIds)
            {
                Verify(
                    plan.Marks.Any(mark => string.Equals(mark.VisualId, visualId, StringComparison.Ordinal)),
                    $"Goal 6B Inspector '{stage.Name}' is missing visual '{visualId}'.");
            }

            var raster = VisualPackGodotAdapter.RasterizeNative(_visualPack, plan);
            Verify(
                raster.GetWidth() == visibleBounds.Width * _visualCellSize &&
                raster.GetHeight() == visibleBounds.Height * _visualCellSize,
                $"Goal 6B Inspector '{stage.Name}' must rasterize through the selected runtime pack.");
        }

        GD.Print($"GOAL6B INSPECTOR PARITY PASS states={stages.Length} pack={_visualPack.PackId} size={_visualCellSize}");
    }

    private void CaptureGate3BSurfaceReviewSet()
    {
        var directory = Path.GetFullPath(
            Path.Combine(
                ProjectSettings.GlobalizePath("res://"),
                "..",
                "..",
                ".tools",
                "gate3b-review"));
        Directory.CreateDirectory(directory);
        var width = _visualCellSize == 20 ? 33 : 41;
        var height = _visualCellSize == 20 ? 23 : 29;

        foreach (var seed in new long[] { 41_337, 41_338, 90_421 })
        {
            var state = ChronicleState.Begin(seed);
            var bounds = new WorldRectangle(
                MinX: -(width / 2L),
                MinY: -(height / 2L),
                Width: width,
                Height: height);
            var semanticHalo = WorldArea.Generate(
                state,
                SurfacePatch.SurfaceStratum,
                new WorldRectangle(
                    bounds.MinX - 1,
                    bounds.MinY - 1,
                    bounds.Width + 2,
                    bounds.Height + 2));
            var plan = VisualGrammar.Compose(
                new VisualCompositionInput(
                    semanticHalo,
                    bounds,
                    seed,
                    _visualPack,
                    _visualPack.StyleVersion,
                    state.Address,
                    TargetAddresses: [],
                    SelectedAddresses: []));
            var stem = $"surface_s{seed}_{_visualCellSize}px_" +
                PackagedVisualPackLoader.ReviewTag(_visualPack);
            var pngPath = Path.Combine(directory, $"{stem}.png");
            var metadataPath = Path.Combine(directory, $"{stem}.json");
            var result = VisualPackGodotAdapter
                .RasterizeNative(_visualPack, plan)
                .SavePng(pngPath);
            if (result != Error.Ok)
            {
                throw new InvalidOperationException(
                    $"Gate 3B Surface review capture failed with {result}.");
            }

            File.WriteAllText(
                metadataPath,
                JsonSerializer.Serialize(
                    new
                    {
                        Seed = seed,
                        Stratum = SurfacePatch.SurfaceStratum,
                        plan.Bounds,
                        plan.CellSize,
                        plan.PackId,
                        plan.PackDigest,
                        RenderPlanDigest = plan.Digest,
                        Incarnation = state.Address,
                        LooseStone = state.LooseStoneAddress,
                        ReadsImmediately = new[]
                        {
                            "connected water",
                            "stone ridge",
                            "grove",
                            "Incarnation",
                            "Loose Stone",
                        },
                        KnownSemanticLimitation =
                            "Gate 3B preserves Gate 3A macro geometry; periodic or symmetric placement is not visual polish.",
                        Review =
                            "Player UAT: mark noise, broken joins, hierarchy, and 16px versus 20px density.",
                    },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private void VerifyInspectorInput(long expectedSeed)
    {
        Verify(
            _seed == expectedSeed &&
            _generationInput.Seed == expectedSeed &&
            _generationInput.Tick == 0 &&
            _generationInput.WorldGrammarVersion ==
                ChronicleState.Begin(expectedSeed).WorldGrammarVersion,
            "Fixture and numeric-seed actions must create only an ephemeral current-grammar query input.");
    }

    private void VerifyCaptureMetadata()
    {
        var pngPath = _lastCapturePngPath;
        var metadataPath = _lastCaptureMetadataPath;
        Verify(
            pngPath is not null && metadataPath is not null &&
            File.Exists(pngPath) && File.Exists(metadataPath),
            "Capture must create deterministic PNG and metadata files.");

        using var document = JsonDocument.Parse(File.ReadAllText(metadataPath!));
        var root = document.RootElement;
        Verify(root.GetProperty("Seed").GetInt64() == _seed, "Capture metadata seed mismatch.");
        Verify(
            root.GetProperty("WorldGrammarVersion").GetInt32() == _generationInput.WorldGrammarVersion,
            "Capture metadata grammar version mismatch.");
        Verify(
            string.Equals(root.GetProperty("Stratum").GetString(), _stratum, StringComparison.Ordinal),
            "Capture metadata Stratum mismatch.");
        Verify(root.TryGetProperty("Bounds", out _), "Capture metadata needs bounds.");
        Verify(root.GetProperty("Zoom").GetInt32() == RequestWidth, "Capture metadata zoom mismatch.");
        Verify(root.TryGetProperty("Overlays", out _), "Capture metadata needs overlay metadata.");
        Verify(
            string.Equals(
                root.GetProperty("Presentation").GetString(),
                _showVisualGrammar ? "visual-grammar" : "semantic-debug",
                StringComparison.Ordinal),
            "Capture metadata presentation mismatch.");
        if (_showVisualGrammar)
        {
            Verify(
                string.Equals(root.GetProperty("PackId").GetString(), _visualPack.PackId, StringComparison.Ordinal) &&
                string.Equals(root.GetProperty("PackDigest").GetString(), _visualPack.Digest, StringComparison.Ordinal) &&
                root.GetProperty("VisualCellSize").GetInt32() == _visualCellSize &&
                string.Equals(
                    root.GetProperty("RenderPlanDigest").GetString(),
                    RequireVisualPlan(RequireArea()).Digest,
                    StringComparison.Ordinal),
                "Visual capture metadata must identify its exact pack, size, and render plan.");
        }
    }

    private void VerifyDeterministicCapture()
    {
        Press(_captureButton);
        VerifyCaptureMetadata();
        var firstPngPath = _lastCapturePngPath!;
        var firstMetadataPath = _lastCaptureMetadataPath!;
        var firstPng = File.ReadAllBytes(firstPngPath);
        var firstMetadata = File.ReadAllBytes(firstMetadataPath);

        Press(_surfaceButton);
        Press(_panEastButton);
        Press(_fixture41338Button);
        Press(_motifIdentityToggle);
        Press(_motifIdentityToggle);
        Press(_fixture41337Button);
        Press(_skyButton);
        Press(_bellButton);
        Press(_captureButton);

        Verify(
            string.Equals(_lastCapturePngPath, firstPngPath, StringComparison.Ordinal) &&
            string.Equals(_lastCaptureMetadataPath, firstMetadataPath, StringComparison.Ordinal),
            "Equivalent inspector state must reuse the deterministic capture paths.");
        Verify(
            File.ReadAllBytes(firstPngPath).SequenceEqual(firstPng) &&
            File.ReadAllBytes(firstMetadataPath).SequenceEqual(firstMetadata),
            "Equivalent inspector state must reproduce identical PNG and metadata bytes.");
    }

    private static PlayerSaveFingerprint CapturePlayerSaveFingerprint()
    {
        var path = ProjectSettings.GlobalizePath(PlayerSavePath);
        return File.Exists(path)
            ? new PlayerSaveFingerprint(
                Exists: true,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
                File.GetLastWriteTimeUtc(path))
            : new PlayerSaveFingerprint(Exists: false, Hash: string.Empty, DateTime.MinValue);
    }

    private VisualRenderPlan RequireVisualPlan(WorldArea visibleArea)
    {
        if (visibleArea.Bounds.Width > MaxVisualPreviewRequestWidth ||
            visibleArea.Bounds.Height > MaxVisualPreviewRequestWidth)
        {
            throw new InvalidOperationException(
                "Visual Grammar preview is limited to bounded local requests.");
        }

        if (_visualPlan is not null && _visualPlan.Bounds == visibleArea.Bounds)
        {
            return _visualPlan;
        }

        var bounds = visibleArea.Bounds;
        var semanticHalo = WorldArea.Generate(
            _generationInput,
            _stratum,
            VisualViewportBounds.WithOneCellSemanticHalo(bounds));
        _visualPlan = VisualGrammar.Compose(
            new VisualCompositionInput(
                semanticHalo,
                bounds,
                _seed,
                _visualPack,
                _visualPack.StyleVersion,
                _generationInput.HasLivingIncarnation ? _generationInput.Address : null,
                TargetAddresses: [],
                SelectedAddresses: []));
        return _visualPlan;
    }

    private static int RequestedVisualCellSize(IReadOnlyList<string> arguments)
    {
        const string prefix = "--visual-cell-size=";
        var argument = arguments.FirstOrDefault(
            value => value.StartsWith(prefix, StringComparison.Ordinal));
        if (argument is null)
        {
            return 20;
        }

        return int.TryParse(argument[prefix.Length..], out var requested) &&
            requested is 16 or 20
                ? requested
                : throw new ArgumentException(
                    "Gate 3B visual cell size must be either 16 or 20.");
    }

    private WorldArea RequireArea() => _area ?? throw new InvalidOperationException("Atlas area was not generated.");

    private Image RequireRaster() => _raster ?? throw new InvalidOperationException("Atlas raster was not generated.");

    private static void Verify(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Press(BaseButton button) => button.EmitSignal(BaseButton.SignalName.Pressed);

    private static int CountNodes(Node node)
    {
        var count = 1;
        foreach (Node child in node.GetChildren())
        {
            count += CountNodes(child);
        }

        return count;
    }

    private sealed record AtlasCaptureMetadata(
        long Seed,
        int WorldGrammarVersion,
        string Stratum,
        WorldRectangle Bounds,
        int Zoom,
        AtlasOverlayMetadata Overlays,
        string Presentation,
        string? PackId,
        string? PackDigest,
        int? VisualCellSize,
        string? RenderPlanDigest);

    private sealed record AtlasOverlayMetadata(
        bool SemanticClasses,
        bool AbsoluteAddresses,
        bool RequestBounds,
        bool MotifIdentity,
        bool DurableIdentity);

    private sealed record PlayerSaveFingerprint(bool Exists, string Hash, DateTime LastWriteTimeUtc);
}
