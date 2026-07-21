using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Chronicle.VisualCompiler;
using Chronicle.VisualPack;
using Godot;

namespace Chronicle.VisualWorkbench;

// PROTOTYPE — three authoring views answer what the durable workbench should be.
public sealed partial class WorkbenchRoot : Control
{
    private static readonly Color Ink = new("e7edf2");
    private static readonly Color MutedInk = new("aebbc5");
    private static readonly Color Accent = new("d4b45f");
    private static readonly Color Surface = new("18212a");
    private static readonly Color DeepSurface = new("10161c");

    private string _cataloguePath = string.Empty;
    private string _briefDirectory = string.Empty;
    private Palimpsest20Pack? _pack;
    private Palimpsest20Definition[] _filtered = [];
    private Palimpsest20Definition? _selected;
    private WorkbenchView _view;

    private ItemList _assetList = null!;
    private LineEdit _filter = null!;
    private Label _status = null!;
    private Label _metadata = null!;
    private Label _viewExplanation = null!;
    private WorkbenchCanvas _canvas = null!;
    private OptionButton _aspect = null!;
    private VBoxContainer _briefPanel = null!;
    private LineEdit _briefId = null!;
    private LineEdit _briefName = null!;
    private LineEdit _briefMood = null!;
    private LineEdit _briefPalette = null!;
    private LineEdit _briefLandmark = null!;
    private TextEdit _briefNotes = null!;
    private readonly List<Button> _viewButtons = [];

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        try
        {
            ParseArguments(OS.GetCmdlineUserArgs());
            BuildInterface();
            CompileCatalogue();
        }
        catch (Exception exception)
        {
            GD.PushError($"PGEN-WB-001: {exception}");
            GetTree().Quit(1);
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key ||
            GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit)
        {
            return;
        }

        if (key.Keycode == Key.Left)
        {
            SelectView((WorkbenchView)(((int)_view + 2) % 3));
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Right)
        {
            SelectView((WorkbenchView)(((int)_view + 1) % 3));
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.R && key.CtrlPressed)
        {
            CompileCatalogue();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildInterface()
    {
        var background = new ColorRect
        {
            Color = DeepSurface,
            MouseFilter = MouseFilterEnum.Ignore
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 22);
        margin.AddThemeConstantOverride("margin_right", 22);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var shell = Vertical(12);
        margin.AddChild(shell);
        shell.AddChild(BuildHeader());
        shell.AddChild(BuildViewSwitch());

        var body = Horizontal(12);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        shell.AddChild(body);
        body.AddChild(BuildAssetRail());

        var canvasPanel = Panel(new Vector2(800, 680));
        canvasPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        canvasPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _canvas = new WorkbenchCanvas();
        canvasPanel.AddChild(_canvas);
        body.AddChild(canvasPanel);
        body.AddChild(BuildInspector());

        shell.AddChild(BuildFooter());
        SelectView(WorkbenchView.AssetLab);
    }

    private Control BuildHeader()
    {
        var header = Horizontal(14);
        var title = Label("P-GEN  /  VISUAL WORKBENCH", 24, Accent);
        title.CustomMinimumSize = new Vector2(360, 0);
        header.AddChild(title);

        var path = Label(_cataloguePath, 14, MutedInk);
        path.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        path.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        header.AddChild(path);

        var reload = new Button
        {
            Text = "Recompile  Ctrl+R",
            CustomMinimumSize = new Vector2(160, 40)
        };
        reload.Pressed += CompileCatalogue;
        header.AddChild(reload);
        return header;
    }

    private Control BuildViewSwitch()
    {
        var row = Horizontal(8);
        AddViewButton("A  Asset Lab", WorkbenchView.AssetLab);
        AddViewButton("B  Material Matrix", WorkbenchView.MaterialMatrix);
        AddViewButton("C  Biome Board", WorkbenchView.BiomeBoard);

        _viewExplanation = Label(string.Empty, 14, MutedInk);
        _viewExplanation.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _viewExplanation.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(_viewExplanation);

        _aspect = new OptionButton { CustomMinimumSize = new Vector2(190, 38) };
        _aspect.AddItem("Native 20 × 20");
        _aspect.AddItem("Tall preview 20 × 30");
        _aspect.ItemSelected += _ => RefreshPresentation();
        row.AddChild(_aspect);
        return row;

        void AddViewButton(string text, WorkbenchView view)
        {
            var button = new Button
            {
                Text = text,
                ToggleMode = true,
                CustomMinimumSize = new Vector2(170, 38)
            };
            button.Pressed += () => SelectView(view);
            _viewButtons.Add(button);
            row.AddChild(button);
        }
    }

    private Control BuildAssetRail()
    {
        var panel = Panel(new Vector2(280, 680));
        var margin = InnerMargin();
        panel.AddChild(margin);
        var content = Vertical(8);
        margin.AddChild(content);
        content.AddChild(Label("Compiled assets", 18, Ink));
        _filter = new LineEdit
        {
            PlaceholderText = "Filter ID or family…",
            ClearButtonEnabled = true
        };
        _filter.TextChanged += _ => RebuildAssetList();
        content.AddChild(_filter);
        _assetList = new ItemList
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AllowReselect = true
        };
        _assetList.ItemSelected += SelectAsset;
        content.AddChild(_assetList);
        return panel;
    }

    private Control BuildInspector()
    {
        var panel = Panel(new Vector2(310, 680));
        var margin = InnerMargin();
        panel.AddChild(margin);
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        margin.AddChild(scroll);
        var content = Vertical(9);
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(content);
        content.AddChild(Label("Inspector", 18, Ink));
        _metadata = Label("Compile a catalogue to inspect it.", 14, MutedInk);
        _metadata.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_metadata);
        content.AddChild(new HSeparator());

        _briefPanel = Vertical(7);
        _briefPanel.AddChild(Label("New biome brief", 18, Accent));
        var help = Label(
            "Describe the kit. Saving creates an authoring brief for the next P-GEN pass; it does not invent game semantics.",
            13,
            MutedInk);
        help.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _briefPanel.AddChild(help);
        _briefId = Field(_briefPanel, "Stable ID", "salt-marsh");
        _briefName = Field(_briefPanel, "Display name", "The Salt March");
        _briefMood = Field(_briefPanel, "Mood / material", "wind-scoured salt and black reeds");
        _briefPalette = Field(_briefPanel, "Palette intent", "cold teal water, chalk ground, one rust accent");
        _briefLandmark = Field(_briefPanel, "Signature landmark", "a drowned observatory");
        _briefPanel.AddChild(Label("Notes", 13, MutedInk));
        _briefNotes = new TextEdit
        {
            CustomMinimumSize = new Vector2(0, 96),
            PlaceholderText = "What must this biome communicate at a glance?"
        };
        _briefPanel.AddChild(_briefNotes);
        var save = new Button { Text = "Save biome brief", CustomMinimumSize = new Vector2(0, 40) };
        save.Pressed += SaveBiomeBrief;
        _briefPanel.AddChild(save);
        content.AddChild(_briefPanel);
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = Horizontal(12);
        _status = Label("Loading…", 13, MutedInk);
        _status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddChild(_status);
        var hint = Label("← / → changes view  •  mouse selects assets", 13, MutedInk);
        footer.AddChild(hint);
        return footer;
    }

    private void CompileCatalogue()
    {
        var selectedId = _selected?.VisualId;
        try
        {
            var timer = Stopwatch.StartNew();
            var catalogue = VisualCatalogue.ParseJson(File.ReadAllBytes(_cataloguePath));
            var result = Chronicle.VisualCompiler.VisualCompiler.CompilePalimpsest20(
                catalogue,
                new CompilationOptions(ReviewMode.None));
            timer.Stop();
            if (!result.Succeeded || result.Pack is null)
            {
                var errors = result.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}");
                throw new FormatException(string.Join(System.Environment.NewLine, errors));
            }

            _pack = result.Pack;
            _selected = selectedId is null
                ? _pack.Definitions[0]
                : _pack.Definitions.FirstOrDefault(definition =>
                    StringComparer.Ordinal.Equals(definition.VisualId, selectedId)) ??
                  _pack.Definitions[0];
            RebuildAssetList();
            _status.Text =
                $"READY  •  {_pack.Definitions.Count} assets  •  {timer.Elapsed.TotalMilliseconds:F0} ms  •  {_pack.Digest[7..19]}";
            _status.AddThemeColorOverride("font_color", new Color("8fd2b2"));
            RefreshPresentation();
        }
        catch (Exception exception) when (exception is IOException or FormatException or JsonException)
        {
            _status.Text = $"COMPILE FAILED  •  {exception.Message}";
            _status.AddThemeColorOverride("font_color", new Color("ff8c83"));
        }
    }

    private void RebuildAssetList()
    {
        if (_pack is null)
        {
            return;
        }

        var query = _filter.Text.Trim();
        _filtered = _pack.Definitions
            .Where(definition => query.Length == 0 ||
                definition.VisualId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                definition.FamilyId.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        _assetList.Clear();
        for (var index = 0; index < _filtered.Length; index++)
        {
            _assetList.AddItem(_filtered[index].VisualId);
            if (ReferenceEquals(_filtered[index], _selected) ||
                StringComparer.Ordinal.Equals(_filtered[index].VisualId, _selected?.VisualId))
            {
                _assetList.Select(index);
                _assetList.EnsureCurrentIsVisible();
            }
        }
    }

    private void SelectAsset(long index)
    {
        if ((ulong)index >= (ulong)_filtered.Length)
        {
            return;
        }
        _selected = _filtered[index];
        RefreshPresentation();
    }

    private void SelectView(WorkbenchView view)
    {
        _view = view;
        for (var index = 0; index < _viewButtons.Count; index++)
        {
            _viewButtons[index].ButtonPressed = index == (int)view;
        }
        _briefPanel.Visible = view == WorkbenchView.BiomeBoard;
        _viewExplanation.Text = view switch
        {
            WorkbenchView.AssetLab => "Silhouette, occupancy, native-size truth",
            WorkbenchView.MaterialMatrix => "Variants and masks without map noise",
            _ => "Mixed-material read plus future-biome brief"
        };
        RefreshPresentation();
    }

    private void RefreshPresentation()
    {
        if (_pack is null || _selected is null || _canvas is null)
        {
            return;
        }

        _canvas.Present(_pack, _selected, _view, _aspect.Selected == 1);
        var bounds = VisibleBounds(_pack, _selected);
        _metadata.Text =
            $"{_selected.VisualId}\n\n" +
            $"Family\n{_selected.FamilyId}\n\n" +
            $"Layer  {_selected.LayerClass}\n" +
            $"Variant  {_selected.VariantOrdinal}\n" +
            $"Mask  {_selected.AdjacencyMask?.ToString() ?? "none"}\n" +
            $"Visible bounds  {bounds.Width} × {bounds.Height}\n" +
            $"Occupied  {bounds.Occupied} px ({bounds.Percent:F0}%)\n" +
            $"Logical cell  20 × 20\n" +
            $"Preview  {(_aspect.Selected == 1 ? "20 × 30 tall" : "20 × 20 native")}";
    }

    private void SaveBiomeBrief()
    {
        try
        {
            var id = _briefId.Text.Trim();
            if (!ValidBriefId(id))
            {
                throw new FormatException(
                    "Use lowercase letters, digits, and single hyphens for the stable ID.");
            }
            if (string.IsNullOrWhiteSpace(_briefName.Text) ||
                string.IsNullOrWhiteSpace(_briefMood.Text))
            {
                throw new FormatException("Display name and mood/material are required.");
            }

            Directory.CreateDirectory(_briefDirectory);
            var path = Path.GetFullPath(Path.Combine(_briefDirectory, $"{id}.visual-brief.json"));
            var root = Path.GetFullPath(_briefDirectory) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Brief path escaped its authoring directory.");
            }
            if (File.Exists(path))
            {
                throw new IOException("That brief already exists; choose a new stable ID.");
            }

            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = 1,
                id,
                displayName = _briefName.Text.Trim(),
                moodAndMaterial = _briefMood.Text.Trim(),
                paletteIntent = _briefPalette.Text.Trim(),
                signatureLandmark = _briefLandmark.Text.Trim(),
                notes = _briefNotes.Text.Trim(),
                reference = new
                {
                    catalogue = Path.GetFileName(_cataloguePath),
                    nativeCell = new[] { 20, 20 },
                    comparisonCell = new[] { 16, 24 }
                },
                requiredVisualRoles = new[]
                {
                    "ground-field",
                    "water-and-shore-if-present",
                    "vegetation-or-material-feature",
                    "elevation-or-blocker",
                    "one-signature-landmark",
                    "actor-read-check"
                },
                constraints = new[]
                {
                    "materials must read without labels",
                    "no generic connectors except genuinely connected structures",
                    "world semantics must already exist before runtime adoption"
                }
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllBytes(path, [.. payload, (byte)'\n']);
            _status.Text = $"BRIEF SAVED  •  {path}";
            _status.AddThemeColorOverride("font_color", new Color("8fd2b2"));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
            ArgumentException or FormatException or InvalidOperationException)
        {
            _status.Text = $"BRIEF NOT SAVED  •  {exception.Message}";
            _status.AddThemeColorOverride("font_color", new Color("ff8c83"));
        }
    }

    private void ParseArguments(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length ||
                args[index] is not ("--catalogue" or "--brief-directory") ||
                !values.TryAdd(args[index], args[index + 1]))
            {
                throw new ArgumentException($"Unknown or incomplete argument '{args[index]}'.");
            }
        }
        if (!values.TryGetValue("--catalogue", out var catalogue))
        {
            throw new ArgumentException("Missing required argument '--catalogue'.");
        }
        _cataloguePath = Path.GetFullPath(catalogue);
        if (!File.Exists(_cataloguePath))
        {
            throw new FileNotFoundException("Catalogue does not exist.", _cataloguePath);
        }
        _briefDirectory = values.TryGetValue("--brief-directory", out var briefDirectory)
            ? Path.GetFullPath(briefDirectory)
            : Path.Combine(Path.GetDirectoryName(_cataloguePath)!, "briefs");
    }

    private static (int Width, int Height, int Occupied, double Percent) VisibleBounds(
        Palimpsest20Pack pack,
        Palimpsest20Definition definition)
    {
        var rect = definition.AtlasRect;
        var minX = rect.Width;
        var minY = rect.Height;
        var maxX = -1;
        var maxY = -1;
        var occupied = 0;
        for (var y = 0; y < rect.Height; y++)
        {
            for (var x = 0; x < rect.Width; x++)
            {
                if (pack.AtlasIndices[(rect.Y + y) * pack.AtlasWidth + rect.X + x] == 0)
                {
                    continue;
                }
                occupied++;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }
        return (
            maxX < minX ? 0 : maxX - minX + 1,
            maxY < minY ? 0 : maxY - minY + 1,
            occupied,
            occupied / 4.0);
    }

    private static bool ValidBriefId(string value) =>
        value.Length is >= 3 and <= 48 &&
        value[0] != '-' &&
        value[^1] != '-' &&
        !value.Contains("--", StringComparison.Ordinal) &&
        value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static PanelContainer Panel(Vector2 minimum) => new()
    {
        CustomMinimumSize = minimum,
        SelfModulate = Surface
    };

    private static VBoxContainer Vertical(int separation)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", separation);
        return container;
    }

    private static HBoxContainer Horizontal(int separation)
    {
        var container = new HBoxContainer();
        container.AddThemeConstantOverride("separation", separation);
        return container;
    }

    private static MarginContainer InnerMargin()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        return margin;
    }

    private static Label Label(string text, int size, Color colour)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);
        return label;
    }

    private static LineEdit Field(VBoxContainer parent, string label, string placeholder)
    {
        parent.AddChild(Label(label, 13, MutedInk));
        var field = new LineEdit { PlaceholderText = placeholder };
        parent.AddChild(field);
        return field;
    }
}
