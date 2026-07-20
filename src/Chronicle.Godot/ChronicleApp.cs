using Chronicle.Core;
using Chronicle.VisualPack;
using Chronicle.Visuals;
using Godot;
using System.Text.Json;

[GlobalClass]
public partial class ChronicleApp : Node
{
    private const string SavePath = "user://slice0_chronicle.json";
    private const long InitialSeed = 41_337;
    private const double ClockPulseSeconds = 0.25;
    private const int CanvasPixelWidth = 1600;
    private const int CanvasPixelHeight = 900;
    private const int MapPixelWidth = 1020;
    private const int MapPixelHeight = 740;
    private const string LegacyOpeningPrompt =
        "Explore and Build are Starting Vectors, not classes.\n" +
        "The sky has no road; the ground can remember.\n" +
        "Where do you intend to go?";
    private const string CombatOpeningPrompt =
        "Combat, Explore, and Build are Starting Vectors, not classes.\n" +
        "The river has a ward; the sky has no road; the ground can remember.\n" +
        "Where do you intend to go?";

    private static readonly StringName MoveNorthAction = "chronicle_move_north";
    private static readonly StringName MoveSouthAction = "chronicle_move_south";
    private static readonly StringName MoveWestAction = "chronicle_move_west";
    private static readonly StringName MoveEastAction = "chronicle_move_east";
    private static readonly StringName PauseAction = "chronicle_pause";
    private static readonly StringName SlowAction = "chronicle_slow";
    private static readonly StringName NormalAction = "chronicle_normal";
    private static readonly StringName FastAction = "chronicle_fast";
    private static readonly StringName SaveAction = "chronicle_save";
    private static readonly StringName LoadAction = "chronicle_load";

    private ChronicleSimulation _simulation = new(ChronicleState.Begin(InitialSeed));
    private WorldVisualView _surfacePatchView = null!;
    private WorldVisualView _skyStratumView = null!;
    private Label _worldViewReadout = null!;
    private Label _readout = null!;
    private Label _codexReadout = null!;
    private Label _loadoutReadout = null!;
    private Label _statusReadout = null!;
    private Label _guidanceReadout = null!;
    private Label _openingPrompt = null!;
    private Button _againstButton = null!;
    private Button _upButton = null!;
    private Button _hereButton = null!;
    private Button _flyButton = null!;
    private Button _studyButton = null!;
    private Button _equipFlyButton = null!;
    private Button _equipFoundButton = null!;
    private Button _equipSmashButton = null!;
    private Button _fitStoneButton = null!;
    private Button _clearFirstSlotButton = null!;
    private Button _ringBellButton = null!;
    private Button _pauseButton = null!;
    private Button _slowButton = null!;
    private Button _normalButton = null!;
    private Button _fastButton = null!;
    private Button _saveButton = null!;
    private Button _loadButton = null!;
    private ColorRect _studyChoicesPanel = null!;
    private Label _studySourceReadout = null!;
    private ColorRect _openingPanel = null!;
    private ColorRect _replacementPanel = null!;
    private Label _replacementReadout = null!;
    private Button _createReplacementButton = null!;
    private Button _replacementSaveButton = null!;
    private Button _replacementLoadButton = null!;
    private readonly List<Button> _directionButtons = [];
    private readonly List<Button> _hotbarSlots = [];
    private readonly List<Button> _studyOfferButtons = [];
    private readonly List<Label> _studyOfferReadouts = [];
    private CompiledVisualPack _visualPack = null!;
    private ImageTexture _visualAtlasTexture = null!;
    private Texture2D _flyGlyph = null!;
    private Texture2D _stoneGlyph = null!;
    private Texture2D _codexGlyph = null!;
    private Texture2D _loadoutGlyph = null!;
    private WorldArea? _worldArea;
    private WorldRectangle _visibleWorldBounds;
    private WorldAddress[] _renderedTargets = [];
    private int _visualCellSize;
    private double _pulseAccumulator;
    private long _renderedSeed;
    private WorldAddress _renderedAddress;
    private WorldAddress? _renderedLooseStoneAddress;
    private HomeState? _renderedHome;
    private FirstConflictState? _renderedFirstConflict;
    private int _renderedWorldGrammarVersion;
    private bool _renderedHasLivingIncarnation;
    private bool _hasRenderedWorld;
    private bool _verifyGate3BPlayer;
    private bool _verifyGoal4A;
    private bool _verifyGoal4APartial;
    private bool _verifyGoal4B;
    private bool _verifyGoal4BRestart;
    private bool _verifyGoal4C;
    private bool _verifyGoal4CRestart;
    private bool _verifyGoal4CResolved;
    private bool _verifyGoal4CFailure;
    private string _lastSaveLoadStatus = "Starting Chronicle.";
    private string _lastAnswerStatus = string.Empty;
    private string _lastCommandStatus = string.Empty;
    private int? _targetingSlot;
    private bool _deathConfirmationArmed;
    private bool _studyChoicesExposed;

    public override void _Ready()
    {
        var arguments = OS.GetCmdlineUserArgs();
        _verifyGate3BPlayer = arguments.Contains("--verify-gate3b-player", StringComparer.Ordinal);
        _verifyGoal4A = arguments.Contains("--verify-4a", StringComparer.Ordinal);
        _verifyGoal4APartial = arguments.Contains("--verify-4a-partial", StringComparer.Ordinal);
        _verifyGoal4B = arguments.Contains("--verify-4b", StringComparer.Ordinal);
        _verifyGoal4BRestart = arguments.Contains("--verify-4b-restart", StringComparer.Ordinal);
        _verifyGoal4C = arguments.Contains("--verify-4c", StringComparer.Ordinal);
        _verifyGoal4CRestart = arguments.Contains("--verify-4c-restart", StringComparer.Ordinal);
        _verifyGoal4CResolved = arguments.Contains("--verify-4c-resolved", StringComparer.Ordinal);
        _verifyGoal4CFailure = arguments.Contains("--verify-4c-failure", StringComparer.Ordinal);
        _visualCellSize = RequestedVisualCellSize(arguments);
        _visualPack = ManualVisualPack.CreateGate3B(_visualCellSize);
        _visualAtlasTexture = VisualPackGodotAdapter.CreateAtlasTexture(_visualPack);
        _flyGlyph = VisualPackGodotAdapter.CreateRegionTexture(
            _visualAtlasTexture,
            _visualPack.Resolve("glyph.codex.fly"));
        _stoneGlyph = VisualPackGodotAdapter.CreateRegionTexture(
            _visualAtlasTexture,
            _visualPack.Resolve("glyph.codex.stone"));
        _codexGlyph = VisualPackGodotAdapter.CreateRegionTexture(
            _visualAtlasTexture,
            _visualPack.Resolve("glyph.codex"));
        _loadoutGlyph = VisualPackGodotAdapter.CreateRegionTexture(
            _visualAtlasTexture,
            _visualPack.Resolve("glyph.loadout"));
        BuildWorldViews();
        BuildHotbar();
        BuildWorldGuidance();
        BuildControlPanel();
        BuildReplacementPanel();
        BuildOpeningPanel();

        LoadOrCreateChronicle();
        RefreshPresentation();
        LogState("SLICE2C READY");

        if (_verifyGoal4B)
        {
            Callable.From(RunGoal4BAcceptance).CallDeferred();
        }
        else if (_verifyGoal4BRestart)
        {
            Callable.From(RunGoal4BRestartAcceptance).CallDeferred();
        }
        else if (_verifyGoal4C)
        {
            Callable.From(RunGoal4CAcceptance).CallDeferred();
        }
        else if (_verifyGoal4CRestart)
        {
            Callable.From(RunGoal4CRestartAcceptance).CallDeferred();
        }
        else if (_verifyGoal4CResolved)
        {
            Callable.From(RunGoal4CResolvedRestartAcceptance).CallDeferred();
        }
        else if (_verifyGoal4CFailure)
        {
            Callable.From(RunGoal4CFailureAcceptance).CallDeferred();
        }
        else if (_verifyGoal4APartial)
        {
            Callable.From(RunGoal4APartialSave).CallDeferred();
        }
        else if (_verifyGoal4A)
        {
            Callable.From(RunGoal4AAcceptance).CallDeferred();
        }
        else if (_verifyGate3BPlayer ||
                 arguments.Contains("--verify-slice2c", StringComparer.Ordinal) ||
                 arguments.Contains("--verify-acceptance", StringComparer.Ordinal))
        {
            Callable.From(RunSlice2CAcceptance).CallDeferred();
        }
    }

    public override void _Process(double delta)
    {
        _pulseAccumulator += delta;

        while (_pulseAccumulator >= ClockPulseSeconds)
        {
            _simulation.AdvanceClockPulse();
            _pulseAccumulator -= ClockPulseSeconds;
        }

        RefreshPresentation();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_simulation.State.Intent == OpeningIntent.Unchosen)
        {
            return;
        }

        if (!_simulation.State.HasLivingIncarnation)
        {
            if (@event.IsActionPressed(SaveAction))
            {
                SaveChronicle();
            }
            else if (@event.IsActionPressed(LoadAction))
            {
                LoadChronicle();
            }
            else
            {
                return;
            }

            GetViewport().SetInputAsHandled();
            RefreshPresentation();
            return;
        }

        if (@event.IsActionPressed(MoveNorthAction))
        {
            MoveIncarnation(0, -1);
        }
        else if (@event.IsActionPressed(MoveSouthAction))
        {
            MoveIncarnation(0, 1);
        }
        else if (@event.IsActionPressed(MoveWestAction))
        {
            MoveIncarnation(-1, 0);
        }
        else if (@event.IsActionPressed(MoveEastAction))
        {
            MoveIncarnation(1, 0);
        }
        else if (@event.IsActionPressed(PauseAction))
        {
            SetChronicleSpeed(ChronicleSpeed.Paused);
        }
        else if (@event.IsActionPressed(SlowAction))
        {
            SetChronicleSpeed(ChronicleSpeed.Slow);
        }
        else if (@event.IsActionPressed(NormalAction))
        {
            SetChronicleSpeed(ChronicleSpeed.Normal);
        }
        else if (@event.IsActionPressed(FastAction))
        {
            SetChronicleSpeed(ChronicleSpeed.Fast);
        }
        else if (@event.IsActionPressed(SaveAction))
        {
            SaveChronicle();
        }
        else if (@event.IsActionPressed(LoadAction))
        {
            LoadChronicle();
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        RefreshPresentation();
    }

    public override void _ExitTree()
    {
        SaveChronicle();
    }

    private void BuildWorldViews()
    {
        _worldViewReadout = new Label
        {
            Name = "WorldViewReadout",
            Position = new Vector2(24, 20),
            Size = new Vector2(MapPixelWidth, 22),
        };
        _worldViewReadout.AddThemeFontSizeOverride("font_size", 13);
        _worldViewReadout.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        AddChild(_worldViewReadout);

        _surfacePatchView = new WorldVisualView
        {
            Name = "SurfacePatchView",
            Position = new Vector2(24, 50),
        };
        AddChild(_surfacePatchView);

        _skyStratumView = new WorldVisualView
        {
            Name = "SkyStratumView",
            Position = new Vector2(24, 50),
            Visible = false,
        };
        AddChild(_skyStratumView);
    }

    private void BuildHotbar()
    {
        var hotbar = new ColorRect
        {
            Name = "Hotbar",
            Position = new Vector2(24, 806),
            Size = new Vector2(MapPixelWidth, 64),
            Color = new Color(0.025f, 0.045f, 0.065f, 0.96f),
        };

        for (var slot = 0; slot < 8; slot++)
        {
            var button = new Button
            {
                Name = $"HotbarSlot{slot + 1}",
                Position = new Vector2(4 + slot * 82, 4),
                Size = new Vector2(80, 56),
                Text = "—",
                Disabled = true,
                FocusMode = Control.FocusModeEnum.None,
                IconAlignment = HorizontalAlignment.Left,
            };
            button.AddThemeFontSizeOverride("font_size", 12);
            var slotIndex = slot;
            button.Pressed += () =>
            {
                UseHotbarSlot(slotIndex);
                RefreshPresentation();
            };
            hotbar.AddChild(button);
            _hotbarSlots.Add(button);
        }

        _flyButton = _hotbarSlots[0];

        AddChild(hotbar);
    }

    private void BuildWorldGuidance()
    {
        var panel = new ColorRect
        {
            Name = "ChronicleThread",
            Position = new Vector2(1080, 696),
            Size = new Vector2(496, 174),
            Color = new Color(0.025f, 0.045f, 0.065f, 0.96f),
        };

        var heading = new Label
        {
            Position = new Vector2(14, 8),
            Size = new Vector2(468, 20),
            Text = "CHRONICLE THREAD",
        };
        heading.AddThemeFontSizeOverride("font_size", 13);
        heading.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        panel.AddChild(heading);

        _guidanceReadout = new Label
        {
            Name = "GuidanceReadout",
            Position = new Vector2(14, 32),
            Size = new Vector2(468, 130),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _guidanceReadout.AddThemeFontSizeOverride("font_size", 13);
        _guidanceReadout.AddThemeColorOverride("font_color", new Color(0.76f, 0.84f, 0.9f));
        panel.AddChild(_guidanceReadout);
        AddChild(panel);
    }

    private void BuildControlPanel()
    {
        var panel = new ColorRect
        {
            Name = "ChronicleControls",
            Position = new Vector2(1080, 24),
            Size = new Vector2(496, 658),
            Color = new Color(0.045f, 0.065f, 0.09f, 0.96f),
        };

        _readout = new Label
        {
            Name = "ChronicleReadout",
            Position = new Vector2(18, 14),
            Size = new Vector2(460, 100),
        };
        _readout.AddThemeFontSizeOverride("font_size", 17);
        _readout.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 1f));
        panel.AddChild(_readout);

        var codexPanel = new ColorRect
        {
            Name = "CodexPanel",
            Position = new Vector2(18, 120),
            Size = new Vector2(460, 180),
            Color = new Color(0.025f, 0.045f, 0.065f, 0.86f),
        };
        panel.AddChild(codexPanel);

        codexPanel.AddChild(new TextureRect
        {
            Name = "CodexGlyph",
            Position = new Vector2(16, 13),
            Size = new Vector2(_visualCellSize, _visualCellSize),
            Texture = _codexGlyph,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
        });
        _codexReadout = new Label
        {
            Name = "CodexReadout",
            Position = new Vector2(44, 11),
            Size = new Vector2(400, 101),
        };
        _codexReadout.AddThemeFontSizeOverride("font_size", 16);
        _codexReadout.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        codexPanel.AddChild(_codexReadout);

        codexPanel.AddChild(new TextureRect
        {
            Name = "LoadoutGlyph",
            Position = new Vector2(16, 118),
            Size = new Vector2(_visualCellSize, _visualCellSize),
            Texture = _loadoutGlyph,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
        });
        _loadoutReadout = new Label
        {
            Name = "LoadoutReadout",
            Position = new Vector2(44, 116),
            Size = new Vector2(400, 24),
        };
        _loadoutReadout.AddThemeFontSizeOverride("font_size", 15);
        _loadoutReadout.AddThemeColorOverride("font_color", new Color(0.84f, 0.91f, 0.97f));
        codexPanel.AddChild(_loadoutReadout);

        _equipFlyButton = AddCommandButton(
            codexPanel,
            "EQUIP FLY",
            new Vector2(4, 144),
            new Vector2(82, 32),
            () => ConfigureFirstSlot(WordIds.Fly, noun: null),
            fontSize: 10);
        _equipFoundButton = AddCommandButton(
            codexPanel,
            "EQUIP FOUND",
            new Vector2(90, 144),
            new Vector2(90, 32),
            () => ConfigureFirstSlot(WordIds.Found, noun: null),
            fontSize: 10);
        _equipSmashButton = AddCommandButton(
            codexPanel,
            "EQUIP SMASH",
            new Vector2(184, 144),
            new Vector2(95, 32),
            () => ConfigureFirstSlot(WordIds.Smash, noun: null),
            fontSize: 10);
        _fitStoneButton = AddCommandButton(
            codexPanel,
            "FIT STONE",
            new Vector2(283, 144),
            new Vector2(75, 32),
            () => ConfigureFirstSlot(WordIds.Fly, WordIds.Stone),
            fontSize: 10);
        _clearFirstSlotButton = AddCommandButton(
            codexPanel,
            "CLEAR SLOT 1",
            new Vector2(362, 144),
            new Vector2(96, 32),
            ClearFirstSlot,
            fontSize: 10);

        BuildStudyChoicePanel(codexPanel);

        var keyboardHelp = new Label
        {
            Name = "KeyboardHelp",
            Position = new Vector2(18, 308),
            Size = new Vector2(460, 38),
            Text = "Move/target: WASD/arrows   Clock: Space, 1/2/3\nUse Loadout: hotbar   Save: F5   Load: F9",
        };
        keyboardHelp.AddThemeFontSizeOverride("font_size", 14);
        keyboardHelp.AddThemeColorOverride("font_color", new Color(0.68f, 0.76f, 0.84f));
        panel.AddChild(keyboardHelp);

        _directionButtons.Add(AddCommandButton(
            panel,
            "N",
            new Vector2(90, 358),
            new Vector2(64, 38),
            () => MoveIncarnation(0, -1)));
        _directionButtons.Add(AddCommandButton(
            panel,
            "W",
            new Vector2(18, 402),
            new Vector2(64, 38),
            () => MoveIncarnation(-1, 0)));
        _directionButtons.Add(AddCommandButton(
            panel,
            "S",
            new Vector2(90, 402),
            new Vector2(64, 38),
            () => MoveIncarnation(0, 1)));
        _directionButtons.Add(AddCommandButton(
            panel,
            "E",
            new Vector2(162, 402),
            new Vector2(64, 38),
            () => MoveIncarnation(1, 0)));

        _pauseButton = AddCommandButton(
            panel,
            "Pause",
            new Vector2(18, 452),
            new Vector2(72, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Paused));
        _slowButton = AddCommandButton(
            panel,
            "Slow",
            new Vector2(98, 452),
            new Vector2(64, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Slow));
        _normalButton = AddCommandButton(
            panel,
            "Normal",
            new Vector2(170, 452),
            new Vector2(82, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Normal));
        _fastButton = AddCommandButton(
            panel,
            "Fast",
            new Vector2(260, 452),
            new Vector2(64, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Fast));

        _saveButton = AddCommandButton(
            panel,
            "Save",
            new Vector2(18, 502),
            new Vector2(72, 40),
            SaveChronicle);
        _loadButton = AddCommandButton(
            panel,
            "Load",
            new Vector2(98, 502),
            new Vector2(72, 40),
            LoadChronicle);

        _studyButton = AddCommandButton(
            panel,
            "STUDY AT BELL",
            new Vector2(178, 502),
            new Vector2(132, 40),
            ExposeStudySource);
        _studyButton.Disabled = true;

        _ringBellButton = AddCommandButton(
            panel,
            "RING AT BELL",
            new Vector2(318, 502),
            new Vector2(160, 40),
            RingBell);
        _ringBellButton.Disabled = true;

        _statusReadout = new Label
        {
            Name = "StatusReadout",
            Position = new Vector2(18, 550),
            Size = new Vector2(460, 108),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _statusReadout.AddThemeFontSizeOverride("font_size", 13);
        _statusReadout.AddThemeColorOverride("font_color", new Color(0.83f, 0.87f, 0.68f));
        panel.AddChild(_statusReadout);
        AddChild(panel);
    }

    private void BuildStudyChoicePanel(Control parent)
    {
        _studyChoicesPanel = new ColorRect
        {
            Name = "StudyChoices",
            Position = Vector2.Zero,
            Size = new Vector2(460, 180),
            Color = new Color(0.035f, 0.065f, 0.095f, 0.98f),
            Visible = false,
        };
        parent.AddChild(_studyChoicesPanel);

        _studySourceReadout = new Label
        {
            Name = "StudySourceReadout",
            Position = new Vector2(12, 2),
            Size = new Vector2(436, 66),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _studySourceReadout.AddThemeFontSizeOverride("font_size", 11);
        _studySourceReadout.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        _studyChoicesPanel.AddChild(_studySourceReadout);

        for (var offerIndex = 0; offerIndex < 2; offerIndex++)
        {
            var index = offerIndex;
            var button = AddCommandButton(
                _studyChoicesPanel,
                "CHOOSE",
                new Vector2(10, 72 + index * 56),
                new Vector2(96, 38),
                () => ChooseStudyOffer(index));
            button.Name = $"StudyOffer{index + 1}";
            button.AddThemeFontSizeOverride("font_size", 11);
            _studyOfferButtons.Add(button);

            var readout = new Label
            {
                Name = $"StudyOfferReadout{index + 1}",
                Position = new Vector2(114, 68 + index * 56),
                Size = new Vector2(332, 44),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            readout.AddThemeFontSizeOverride("font_size", 11);
            readout.AddThemeColorOverride("font_color", new Color(0.82f, 0.9f, 0.96f));
            _studyChoicesPanel.AddChild(readout);
            _studyOfferReadouts.Add(readout);
        }
    }

    private void BuildReplacementPanel()
    {
        _replacementPanel = new ColorRect
        {
            Name = "AwaitingReplacement",
            Position = Vector2.Zero,
            Size = new Vector2(CanvasPixelWidth, CanvasPixelHeight),
            Color = new Color(0.018f, 0.026f, 0.045f, 0.97f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 90,
            Visible = false,
        };

        var title = new Label
        {
            Position = new Vector2(500, 170),
            Size = new Vector2(600, 54),
            Text = "THE BODY ENDED",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 34);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        _replacementPanel.AddChild(title);

        _replacementReadout = new Label
        {
            Name = "ReplacementReadout",
            Position = new Vector2(450, 250),
            Size = new Vector2(700, 150),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _replacementReadout.AddThemeFontSizeOverride("font_size", 20);
        _replacementReadout.AddThemeColorOverride("font_color", new Color(0.86f, 0.91f, 0.96f));
        _replacementPanel.AddChild(_replacementReadout);

        _createReplacementButton = AddCommandButton(
            _replacementPanel,
            "CREATE REPLACEMENT INCARNATION",
            new Vector2(590, 430),
            new Vector2(420, 62),
            CreateReplacement);
        _createReplacementButton.AddThemeFontSizeOverride("font_size", 20);

        _replacementSaveButton = AddCommandButton(
            _replacementPanel,
            "SAVE CHRONICLE",
            new Vector2(590, 518),
            new Vector2(202, 46),
            SaveChronicle);
        _replacementLoadButton = AddCommandButton(
            _replacementPanel,
            "LOAD CHRONICLE",
            new Vector2(808, 518),
            new Vector2(202, 46),
            LoadChronicle);

        AddChild(_replacementPanel);
    }

    private void BuildOpeningPanel()
    {
        _openingPanel = new ColorRect
        {
            Name = "OpeningIntent",
            Position = Vector2.Zero,
            Size = new Vector2(CanvasPixelWidth, CanvasPixelHeight),
            Color = new Color(0.018f, 0.026f, 0.045f, 0.97f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 100,
        };

        var title = new Label
        {
            Position = new Vector2(450, 175),
            Size = new Vector2(700, 54),
            Text = "THE FIRST HORIZON",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 34);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        _openingPanel.AddChild(title);

        _openingPrompt = new Label
        {
            Position = new Vector2(400, 255),
            Size = new Vector2(800, 120),
            Text = CombatOpeningPrompt,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _openingPrompt.AddThemeFontSizeOverride("font_size", 20);
        _openingPrompt.AddThemeColorOverride("font_color", new Color(0.86f, 0.91f, 0.96f));
        _openingPanel.AddChild(_openingPrompt);

        _againstButton = new Button
        {
            Name = "ChooseAgainstIntent",
            Position = new Vector2(374, 430),
            Size = new Vector2(260, 68),
            Text = "AGAINST — COMBAT",
            FocusMode = Control.FocusModeEnum.None,
        };
        _againstButton.AddThemeFontSizeOverride("font_size", 20);
        _againstButton.Pressed += ChooseAgainstIntent;
        _openingPanel.AddChild(_againstButton);

        _upButton = new Button
        {
            Name = "ChooseUpIntent",
            Position = new Vector2(670, 430),
            Size = new Vector2(260, 68),
            Text = "UP — EXPLORE",
            FocusMode = Control.FocusModeEnum.None,
        };
        _upButton.AddThemeFontSizeOverride("font_size", 20);
        _upButton.Pressed += ChooseUpIntent;
        _openingPanel.AddChild(_upButton);

        _hereButton = new Button
        {
            Name = "ChooseHereIntent",
            Position = new Vector2(966, 430),
            Size = new Vector2(260, 68),
            Text = "HERE — BUILD",
            FocusMode = Control.FocusModeEnum.None,
        };
        _hereButton.AddThemeFontSizeOverride("font_size", 20);
        _hereButton.Pressed += ChooseHereIntent;
        _openingPanel.AddChild(_hereButton);

        ConfigureOpeningOptions(_simulation.State);
        AddChild(_openingPanel);
    }

    private void ConfigureOpeningOptions(ChronicleState state)
    {
        var offersCombat = state.WorldGrammarVersion == 3;
        _againstButton.Visible = offersCombat;
        _againstButton.Disabled = !offersCombat;
        _openingPrompt.Text = offersCombat ? CombatOpeningPrompt : LegacyOpeningPrompt;
        _upButton.Position = offersCombat
            ? new Vector2(670, 430)
            : new Vector2(522, 430);
        _hereButton.Position = offersCombat
            ? new Vector2(966, 430)
            : new Vector2(818, 430);
    }

    private void ChooseAgainstIntent()
    {
        var result = _simulation.Apply(new ChooseAgainstIntent());
        _lastCommandStatus = result.Message;
        _lastAnswerStatus = result.Applied
            ? "THE CHRONICLE ANSWERS: SMASH — COMBAT STARTING VECTOR"
            : string.Empty;
        RefreshPresentation();
    }

    private void ChooseUpIntent()
    {
        _simulation.Apply(new ChooseUpIntent());
        _lastAnswerStatus = "THE CHRONICLE ANSWERS: FLY — EXPLORE STARTING VECTOR";
        RefreshPresentation();
    }

    private void ChooseHereIntent()
    {
        var result = _simulation.Apply(new ChooseHereIntent());
        _lastCommandStatus = result.Message;
        _lastAnswerStatus = result.Applied
            ? "THE CHRONICLE ANSWERS: FOUND — BUILD STARTING VECTOR"
            : string.Empty;
        RefreshPresentation();
    }

    private void MoveIncarnation(int deltaX, int deltaY)
    {
        if (_targetingSlot is { } slotIndex)
        {
            var target = new WorldAddress(
                _simulation.State.Address.Stratum,
                _simulation.State.Address.X + deltaX,
                _simulation.State.Address.Y + deltaY);
            var result = _simulation.Apply(new UseLoadoutSlot(slotIndex, target));
            _lastCommandStatus = result.Message;
            if (result.Applied)
            {
                _targetingSlot = null;
            }

            return;
        }

        _simulation.Apply(new MoveIncarnation(deltaX, deltaY));
    }

    private void UseHotbarSlot(int slotIndex)
    {
        if (_targetingSlot == slotIndex)
        {
            _targetingSlot = null;
            _lastCommandStatus = "Targeting cancelled.";
            return;
        }

        var slot = _simulation.State.ActiveLoadout[slotIndex];
        if (slot.IsFlyStone)
        {
            _targetingSlot = slotIndex;
        }
        else
        {
            _targetingSlot = null;
        }

        var result = _simulation.Apply(new UseLoadoutSlot(slotIndex));
        _lastCommandStatus = result.Message;
    }

    private void ConfigureFirstSlot(WordId verb, WordId? noun)
    {
        var result = _simulation.Apply(
            new ConfigureLoadoutSlot(0, verb, noun));
        _targetingSlot = null;
        _lastCommandStatus = result.Message;
    }

    private void ClearFirstSlot()
    {
        var result = _simulation.Apply(new ClearLoadoutSlot(0));
        _targetingSlot = null;
        _lastCommandStatus = result.Message;
    }

    private void ExposeStudySource()
    {
        var source = _simulation.CurrentStudySource;
        if (source is null)
        {
            _studyChoicesExposed = false;
            _lastCommandStatus = "There is no Study Source here.";
            return;
        }

        _studyChoicesExposed = true;
        var learnedOffer = source.Offers.FirstOrDefault(offer => offer.IsLearned);
        if (learnedOffer is not null)
        {
            _lastCommandStatus = string.Empty;
            _lastAnswerStatus = $"The Codex already keeps {learnedOffer.Word.DisplayName}.";
            return;
        }

        _lastAnswerStatus = $"Study {source.Name}: choose what it means.";
    }

    private void ChooseStudyOffer(int offerIndex)
    {
        var source = _simulation.CurrentStudySource;
        if (source is null || offerIndex < 0 || offerIndex >= source.Offers.Count)
        {
            _lastCommandStatus = "That Study offer is no longer present.";
            return;
        }

        var offer = source.Offers[offerIndex];
        var result = _simulation.Apply(new ChooseStudyWord(source.Id, offer.Word.Id));
        _lastCommandStatus = result.Message;
        _lastAnswerStatus = result.Applied
            ? $"Studying {offer.Word.DisplayName} at {source.Name}."
            : string.Empty;
    }

    private void RingBell()
    {
        if (!_deathConfirmationArmed)
        {
            _deathConfirmationArmed = true;
            _lastCommandStatus = "The Bell will end this body. Select CONFIRM DEATH to continue.";
            return;
        }

        var result = _simulation.Apply(new EndIncarnationAtBell());
        _deathConfirmationArmed = false;
        _targetingSlot = null;
        _lastCommandStatus = result.Applied
            ? "The first Incarnation ended. The Chronicle is waiting."
            : result.Message;
    }

    private void CreateReplacement()
    {
        var result = _simulation.Apply(new CreateReplacementIncarnation());
        _deathConfirmationArmed = false;
        _targetingSlot = null;
        _lastCommandStatus = result.Message;
    }

    private void SetChronicleSpeed(ChronicleSpeed speed)
    {
        _simulation.Apply(new SetChronicleSpeed(speed));
    }

    private Button AddCommandButton(
        Control parent,
        string text,
        Vector2 position,
        Vector2 size,
        Action action,
        int fontSize = 16)
    {
        var button = new Button
        {
            Text = text,
            Position = position,
            FocusMode = Control.FocusModeEnum.None,
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.Pressed += () =>
        {
            action();
            RefreshPresentation();
        };
        parent.AddChild(button);
        // Godot clamps Size against the active in-tree theme minimum. Apply
        // the font override and attach first so compact controls do not retain
        // the default-font minimum width from construction.
        button.Size = size;
        return button;
    }

    private void LoadOrCreateChronicle()
    {
        if (Godot.FileAccess.FileExists(SavePath))
        {
            LoadChronicle();
            return;
        }

        _simulation = new ChronicleSimulation(ChronicleState.Begin(InitialSeed));
        _lastSaveLoadStatus = "Created initial Chronicle and saved it.";
        SaveChronicle();
    }

    private void LoadChronicle()
    {
        if (!Godot.FileAccess.FileExists(SavePath))
        {
            _lastSaveLoadStatus = "Load unavailable: no save file.";
            LogState("SLICE2C LOAD missing");
            return;
        }

        try
        {
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
            _simulation = new ChronicleSimulation(ChronicleSaveCodec.Deserialize(file.GetAsText()));
            _pulseAccumulator = 0;
            _hasRenderedWorld = false;
            _targetingSlot = null;
            _deathConfirmationArmed = false;
            _studyChoicesExposed = false;
            _lastCommandStatus = string.Empty;
            _lastAnswerStatus = _simulation.State.Codex.Contains(WordIds.Found)
                ? "Codex Verb: Found"
                : _simulation.State.Codex.Contains(WordIds.Smash)
                    ? "Codex Verb: Smash"
                    : _simulation.State.Codex.HasFly
                        ? "Codex Verb: Fly"
                        : string.Empty;
            _lastSaveLoadStatus = "Loaded Chronicle from user://.";
            LogState("SLICE2C LOAD");
        }
        catch (Exception exception)
        {
            _lastSaveLoadStatus = $"Load failed: {exception.Message}";
            GD.PushError($"SLICE2C LOAD failed: {exception.Message}");
        }
    }

    private void SaveChronicle()
    {
        try
        {
            var json = ChronicleSaveCodec.Serialize(_simulation.State);
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(json);
            _lastSaveLoadStatus = "Saved Chronicle to user://.";
            LogState("SLICE2C SAVE");
        }
        catch (Exception exception)
        {
            _lastSaveLoadStatus = $"Save failed: {exception.Message}";
            GD.PushError($"SLICE2C SAVE failed: {exception.Message}");
        }
    }

    private void RefreshPresentation()
    {
        var state = _simulation.State;
        var hasLivingIncarnation = state.HasLivingIncarnation;
        var atBell = state.Address == SkyStratum.LandmarkAddress;
        var currentStudySource = hasLivingIncarnation
            ? _simulation.CurrentStudySource
            : null;
        var homeContext = _simulation.HomeContext;
        var conflictContext = _simulation.ConflictContext;
        if (!hasLivingIncarnation || !atBell)
        {
            _deathConfirmationArmed = false;
        }

        if (currentStudySource is null)
        {
            _studyChoicesExposed = false;
        }

        var worldChanged =
            !_hasRenderedWorld ||
            state.Seed != _renderedSeed ||
            state.Address != _renderedAddress ||
            state.LooseStoneAddress != _renderedLooseStoneAddress ||
            state.Home != _renderedHome ||
            state.FirstConflict != _renderedFirstConflict ||
            state.WorldGrammarVersion != _renderedWorldGrammarVersion ||
            hasLivingIncarnation != _renderedHasLivingIncarnation;
        IReadOnlyList<WorldAddress> highlightedTargets = _targetingSlot is { } targetingSlot
            ? _simulation.ValidTargetsForSlot(targetingSlot)
            : [];
        var targetsChanged = !_renderedTargets.SequenceEqual(highlightedTargets);

        if (worldChanged)
        {
            var visibleColumns = VisibleCellCount(MapPixelWidth, _visualCellSize);
            var visibleRows = VisibleCellCount(MapPixelHeight, _visualCellSize);
            _visibleWorldBounds = VisualViewportBounds.Centered(
                state.Address.X,
                state.Address.Y,
                visibleColumns,
                visibleRows);
            _worldArea = WorldArea.Generate(
                state,
                state.Address.Stratum,
                VisualViewportBounds.WithOneCellSemanticHalo(_visibleWorldBounds));
        }

        WorldVisualView activeWorldView;
        if (string.Equals(state.Address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal))
        {
            _surfacePatchView.Visible = true;
            _skyStratumView.Visible = false;
            activeWorldView = _surfacePatchView;
        }
        else if (string.Equals(state.Address.Stratum, SkyStratum.StratumName, StringComparison.Ordinal))
        {
            _surfacePatchView.Visible = false;
            _skyStratumView.Visible = true;
            activeWorldView = _skyStratumView;
        }
        else
        {
            throw new InvalidOperationException($"No visual view maps Stratum '{state.Address.Stratum}'.");
        }

        if (worldChanged || targetsChanged)
        {
            var plan = VisualGrammar.Compose(
                new VisualCompositionInput(
                    _worldArea!,
                    _visibleWorldBounds,
                    state.Seed,
                    _visualPack,
                    _visualPack.StyleVersion,
                    hasLivingIncarnation ? state.Address : null,
                    highlightedTargets,
                    SelectedAddresses: [],
                    DangerAddresses: conflictContext is { IsThreatened: true } threat
                        ? [threat.Address]
                        : []));
            activeWorldView.SetPlan(_visualPack, plan);
        }

        if (worldChanged)
        {
            _renderedSeed = state.Seed;
            _renderedAddress = state.Address;
            _renderedLooseStoneAddress = state.LooseStoneAddress;
            _renderedHome = state.Home;
            _renderedFirstConflict = state.FirstConflict;
            _renderedWorldGrammarVersion = state.WorldGrammarVersion;
            _renderedHasLivingIncarnation = hasLivingIncarnation;
            _hasRenderedWorld = true;
        }

        if (worldChanged || targetsChanged)
        {
            _renderedTargets = highlightedTargets.ToArray();
        }

        _worldViewReadout.Text =
            $"{state.Address.Stratum.ToUpperInvariant()} · {_visualCellSize} PX · " +
            $"{_visibleWorldBounds.Width} × {_visibleWorldBounds.Height} CELLS · " +
            $"PACK STYLE {_visualPack.StyleVersion}";

        ConfigureOpeningOptions(state);
        _openingPanel.Visible = hasLivingIncarnation && state.Intent == OpeningIntent.Unchosen;
        _replacementPanel.Visible = !hasLivingIncarnation;
        foreach (var directionButton in _directionButtons)
        {
            directionButton.Disabled = !hasLivingIncarnation || state.Intent == OpeningIntent.Unchosen;
        }

        for (var slotIndex = 0; slotIndex < _hotbarSlots.Count; slotIndex++)
        {
            var slot = state.ActiveLoadout[slotIndex];
            var button = _hotbarSlots[slotIndex];
            button.Disabled =
                !hasLivingIncarnation ||
                state.Intent == OpeningIntent.Unchosen ||
                slot.IsEmpty;
            button.Text = _targetingSlot == slotIndex
                ? "CANCEL\nTARGET"
                : slot.IsIntrinsicFly && _simulation.FlyDestination is { } flyDestination
                    ? string.Equals(
                            flyDestination.Stratum,
                            SkyStratum.StratumName,
                            StringComparison.Ordinal)
                        ? "FLY\nUP"
                        : "FLY\nDOWN"
                    : slot.IsFlyStone
                        ? "FLY\n[STONE]"
                        : slot.DisplayName;
            button.Icon = slot.IsEmpty
                ? null
                : slot.IsFlyStone
                    ? _stoneGlyph
                    : slot.IsIntrinsicFly
                        ? _flyGlyph
                        : null;
        }

        var firstSlot = state.ActiveLoadout[0];
        _loadoutReadout.Text = $"LOADOUT SLOT 1: {firstSlot.DisplayName}";
        _equipFlyButton.Disabled =
            !hasLivingIncarnation ||
            !state.Codex.Contains(WordIds.Fly) ||
            firstSlot.IsIntrinsicFly;
        _equipFoundButton.Disabled =
            !hasLivingIncarnation ||
            !state.Codex.Contains(WordIds.Found) ||
            firstSlot.IsIntrinsicFound;
        _equipSmashButton.Disabled =
            !hasLivingIncarnation ||
            !state.Codex.Contains(WordIds.Smash) ||
            (firstSlot.Verb == WordIds.Smash && firstSlot.Noun is null);
        _fitStoneButton.Disabled =
            !hasLivingIncarnation ||
            !state.Codex.HasFly ||
            !state.Codex.HasStone ||
            firstSlot.IsFlyStone;
        _clearFirstSlotButton.Disabled = !hasLivingIncarnation || firstSlot.IsEmpty;
        ApplyCompactLoadoutControlSizes();

        _pauseButton.Disabled = !hasLivingIncarnation;
        _slowButton.Disabled = !hasLivingIncarnation;
        _normalButton.Disabled = !hasLivingIncarnation;
        _fastButton.Disabled = !hasLivingIncarnation;

        _studyButton.Disabled =
            !hasLivingIncarnation ||
            currentStudySource is null;
        _studyButton.Text = currentStudySource is null
            ? "STUDY AT BELL"
            : _studyChoicesExposed
                ? "SOURCE OPEN"
                : "STUDY SKY-STONE";
        RefreshStudyChoiceSurface(currentStudySource, hasLivingIncarnation);

        _ringBellButton.Disabled = !hasLivingIncarnation || !atBell;
        _ringBellButton.Text = _deathConfirmationArmed
            ? "CONFIRM DEATH"
            : atBell
                ? "END THIS BODY"
                : "RING AT BELL";

        _readout.Text =
            $"INCARNATION #{state.IncarnationId} — {LifeDisplayName(state.IncarnationLife)}\n" +
            $"Seed: {state.Seed}\n" +
            $"Tick: {state.Tick} · Clock: {state.Speed}\n" +
            $"Address: {state.Address}";

        _codexReadout.Text =
            $"CODEX\n" +
            $"Verbs: {CodexWordsText(state, WordKind.Verb)}\n" +
            $"Nouns: {CodexWordsText(state, WordKind.Noun)}\n" +
            $"Study: {StudyProgressText(state)}";

        _replacementReadout.Text =
            $"Incarnation #{state.IncarnationId} is gone.\n" +
            $"Tick {state.Tick} is held. Time is not advancing.\n" +
            $"Codex kept: {CodexWordsText(state, null)}\n" +
            $"Understanding kept: {StudyProgressText(state)}\n" +
            $"The changed Chronicle remains.";

        _guidanceReadout.Text = GuidanceText(state, homeContext, conflictContext);

        var placeText = CurrentPlaceText(state, homeContext, conflictContext);
        _statusReadout.Text = string.IsNullOrWhiteSpace(placeText)
            ? string.Join(
                "\n",
                new[] { _lastCommandStatus, _lastAnswerStatus, _lastSaveLoadStatus }
                    .Where(text => !string.IsNullOrWhiteSpace(text)))
            : $"{placeText}\n" +
              FirstNonEmpty(_lastCommandStatus, _lastAnswerStatus, _lastSaveLoadStatus);
    }

    private void RefreshStudyChoiceSurface(
        StudySourceSnapshot? source,
        bool hasLivingIncarnation)
    {
        var visible = hasLivingIncarnation && source is not null && _studyChoicesExposed;
        _studyChoicesPanel.Visible = visible;

        if (source is null)
        {
            _studySourceReadout.Text = string.Empty;
            foreach (var button in _studyOfferButtons)
            {
                button.Visible = false;
                button.Disabled = true;
            }

            foreach (var readout in _studyOfferReadouts)
            {
                readout.Visible = false;
                readout.Text = string.Empty;
            }

            return;
        }

        var yields = source.Offers
            .Select(offer => offer.UnderstandingYield)
            .Distinct()
            .ToArray();
        var yieldText = yields.Length == 0 ? "0" : string.Join(" / ", yields);
        _studySourceReadout.Text =
            $"{source.Name}\n" +
            $"{source.Situation}\n" +
            $"{source.Rarity} / {source.Danger} / {source.Significance} · {yieldText} UNDERSTANDING";

        for (var offerIndex = 0; offerIndex < _studyOfferButtons.Count; offerIndex++)
        {
            var hasOffer = offerIndex < source.Offers.Count;
            var button = _studyOfferButtons[offerIndex];
            var readout = _studyOfferReadouts[offerIndex];
            button.Visible = visible && hasOffer;
            button.Disabled = !visible || !hasOffer;
            readout.Visible = visible && hasOffer;

            if (!hasOffer)
            {
                readout.Text = string.Empty;
                continue;
            }

            var offer = source.Offers[offerIndex];
            var status = offer.IsLearned
                ? "LEARNED"
                : offer.IsSelected
                    ? "SELECTED"
                    : "AVAILABLE";
            button.Text =
                $"{(offer.IsLearned ? "LEARNED" : offer.IsSelected ? "SELECTED" : "CHOOSE")}\n" +
                offer.Word.DisplayName.ToUpperInvariant();
            readout.Text =
                $"{offer.Word.DisplayName} · {offer.CurrentUnderstanding}/{offer.UnderstandingRequired} · {status}\n" +
                offer.Rationale;
        }
    }

    private void ApplyCompactLoadoutControlSizes()
    {
        // These buttons are created before they enter the scene tree. Godot's
        // first themed minimum-size pass uses the default font and may expand
        // them; reapply the accepted row once their 10px overrides are active.
        _equipFlyButton.Size = new Vector2(82, 32);
        _equipFoundButton.Size = new Vector2(90, 32);
        _equipSmashButton.Size = new Vector2(95, 32);
        _fitStoneButton.Size = new Vector2(75, 32);
        _clearFirstSlotButton.Size = new Vector2(96, 32);
    }

    private string CurrentPlaceText(
        ChronicleState state,
        HomeContextSnapshot homeContext,
        ConflictContextSnapshot? conflictContext)
    {
        var sections = new List<string>();
        if (conflictContext is { } conflict &&
            conflict.Address == state.Address)
        {
            sections.Add(ConflictPlaceText(state, conflict));
        }
        else if (homeContext.Home is { } home)
        {
            sections.Add(HomeStatusText(home));
        }

        var atBell = _worldArea is not null &&
            _worldArea.Cells.Any(cell =>
                cell.Address == state.Address &&
                cell.Feature == WorldFeature.Landmark &&
                string.Equals(
                    cell.DurableIdentity,
                    SkyStratum.LandmarkName,
                    StringComparison.Ordinal));
        if (atBell)
        {
            sections.Add(
                $"{SkyStratum.LandmarkName}\n" +
                $"{SkyStratum.LandmarkArrivalLine}\n" +
                "Sky-stone clapper: STUDY to choose a Word.");
        }

        return string.Join("\n", sections);
    }

    private static string HomeStatusText(HomeState home) =>
        "THE FIRST HEARTH\n" +
        $"Address: {home.Address}\n" +
        $"Founded Tick {home.FoundedTick} · Incarnation #{home.FoundingIncarnationId}\n" +
        $"Material: {home.Material} · {ChronicleState.HomeHearthstoneIdentity}";

    private static string StudyProgressText(ChronicleState state)
    {
        return $"Stone {StudyProgressText(state, WordIds.Stone)} · " +
               $"Bell {StudyProgressText(state, WordIds.Bell)}";
    }

    private static string StudyProgressText(ChronicleState state, WordId wordId)
    {
        var word = WordCatalogue.Get(wordId);
        if (state.Codex.Contains(wordId))
        {
            return "[■■■■] KEPT";
        }

        var required = word.UnderstandingRequired;
        if (required <= 0)
        {
            return "GRANTED";
        }

        var completedSegments = state.Study.UnderstandingFor(wordId) * 4 / required;
        var segments =
            new string('■', completedSegments) +
            new string('□', 4 - completedSegments);
        return $"[{segments}] {state.Study.UnderstandingFor(wordId)}/{required}";
    }

    private static string CodexWordsText(ChronicleState state, WordKind? kind)
    {
        var words = state.Codex.Words
            .Select(WordCatalogue.Get)
            .Where(word => kind is null || word.Kind == kind)
            .Select(word => word.DisplayName)
            .ToArray();
        return words.Length == 0 ? "—" : string.Join(", ", words);
    }

    private string GuidanceText(
        ChronicleState state,
        HomeContextSnapshot homeContext,
        ConflictContextSnapshot? conflictContext)
    {
        if (!state.HasLivingIncarnation)
        {
            return "The body ended. The Chronicle and Codex remain; choose when to create the next Incarnation.";
        }

        if (conflictContext is { IsThreatened: true } threat &&
            threat.Address == state.Address)
        {
            var pending = threat.IsSmashPrepared
                ? "SMASH is prepared; resume the Clock for the next active tick."
                : "Leave while paused, or prepare SMASH before resuming.";
            return $"{threat.History}\n{threat.Warning}\n{pending}";
        }

        if (conflictContext is { IsResolved: true } resolved &&
            resolved.Address == state.Address)
        {
            var resolution = resolved.ResolvedTick is { } tick
                ? $"Resolved on Tick {tick}"
                : "Resolved";
            return $"{resolved.CairnIdentity} remains at {resolved.Address}. {resolution}; " +
                   "the underlying Stone ridge remains material.";
        }

        if (_targetingSlot is { } targetingSlot)
        {
            return _simulation.ValidTargetsForSlot(targetingSlot).Count > 0
                ? "TARGET FLY[STONE]: choose the highlighted loose Stone with a direction."
                : "TARGET FLY[STONE]: no loose Stone is adjacent. Press the slot again to cancel.";
        }

        if (homeContext.Home is { } home)
        {
            return ReturnRouteGuidance(state, home, homeContext.ReturnRoute);
        }

        if (state.Intent == OpeningIntent.Here || state.Codex.Contains(WordIds.Found))
        {
            return FoundGuidance(state, homeContext.CurrentSite);
        }

        if (_simulation.CurrentStudySource is { } source)
        {
            return _studyChoicesExposed
                ? $"{source.Name} offers contextual Words. Choose one deliberate Study pursuit."
                : $"You are at {SkyStratum.LandmarkName}. Study its {source.Name} to choose a Word.";
        }

        if (state.Codex.Contains(WordIds.Smash) &&
            conflictContext is not { IsResolved: true })
        {
            return FirstConflictGuidance(state, conflictContext);
        }

        if (state.Codex.HasStone)
        {
            var stoneAddress = state.LooseStoneAddress?.ToString() ?? "unknown";
            return state.ActiveLoadout[0].IsFlyStone
                ? $"FLY[STONE] acts on the adjacent loose Stone. It is currently at {stoneAddress}."
                : $"The Codex keeps Stone; slot 1 currently acts on self. Loose Stone: {stoneAddress}.";
        }

        if (state.Address == SkyStratum.LandmarkAddress)
        {
            return "You are at The Bell That Fell Up. Study its sky-stone clapper here.";
        }

        if (state.Intent == OpeningIntent.Against)
        {
            return "The first confrontation is settled. No active conflict follows this Incarnation; " +
                   "explore the changed Chronicle.";
        }

        return string.Equals(state.Address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal)
            ? "Fly to the sky, then reach The Bell at sky (0, -4) to study its clapper."
            : "The Bell That Fell Up is at sky (0, -4). Its clapper can be studied there.";
    }

    private string FirstConflictGuidance(
        ChronicleState state,
        ConflictContextSnapshot? conflictContext)
    {
        var cairnAddress = conflictContext?.Address ?? VisibleCairnAddress();
        var cairnIdentity = conflictContext?.CairnIdentity ??
            FirstConflictSubjects.RivenCairnIdentity;
        var loadout = state.ActiveLoadout[0].IsIntrinsicSmash
            ? "SMASH is fitted in slot 1."
            : "Re-equip SMASH in slot 1 before confronting it.";

        if (cairnAddress is not { } address)
        {
            return $"{cairnIdentity} lies on a dry Stone ridge near the origin. {loadout}";
        }

        if (!string.Equals(
                state.Address.Stratum,
                address.Stratum,
                StringComparison.Ordinal))
        {
            return $"{cairnIdentity} is at {address}; return to the surface to reach it. {loadout}";
        }

        if (state.Address == address)
        {
            return $"{cairnIdentity} is here. {loadout}";
        }

        var next = state.Address.X != address.X
            ? state.Address with
            {
                X = state.Address.X < address.X
                    ? state.Address.X + 1
                    : state.Address.X - 1,
            }
            : state.Address with
            {
                Y = state.Address.Y < address.Y
                    ? state.Address.Y + 1
                    : state.Address.Y - 1,
            };
        return $"{cairnIdentity}: WALK {DirectionTo(state.Address, next)} " +
               $"toward {address}. {loadout}";
    }

    private WorldAddress? VisibleCairnAddress() =>
        _worldArea?.Cells
            .Where(cell =>
                string.Equals(
                    cell.DurableIdentity,
                    FirstConflictSubjects.RivenCairnIdentity,
                    StringComparison.Ordinal) ||
                string.Equals(
                    cell.DurableIdentity,
                    FirstConflictSubjects.ShatteredCairnIdentity,
                    StringComparison.Ordinal))
            .Select(cell => (WorldAddress?)cell.Address)
            .FirstOrDefault();

    private static string ConflictPlaceText(
        ChronicleState state,
        ConflictContextSnapshot conflict)
    {
        if (conflict.IsThreatened)
        {
            var pending = conflict.IsSmashPrepared
                ? "SMASH — next active tick"
                : "none — next active tick ends this body";
            return $"{conflict.CairnIdentity} · {conflict.SubjectIdentity}\n" +
                   $"Threatened Tick {conflict.ThreatenedTick} · Clock: {state.Speed}\n" +
                   $"Pending: {pending}";
        }

        var resolved = conflict.ResolvedTick is { } tick
            ? $"Resolved Tick {tick}"
            : "Resolved";
        return $"{conflict.CairnIdentity} · {conflict.SubjectIdentity}\n" +
               $"Result: {conflict.Outcome} · {resolved}\n" +
               $"At {conflict.Address}";
    }

    private static string FoundGuidance(ChronicleState state, HomeSiteSnapshot site)
    {
        if (!state.ActiveLoadout[0].IsIntrinsicFound)
        {
            return "Found remains in the Codex. Re-equip FOUND in slot 1 before establishing Home.";
        }

        if (site.IsEligible)
        {
            return $"FOUND can establish THE FIRST HEARTH here: {site.Reason} " +
                   "Use slot 1 with no target.";
        }

        var fixtureHint = site.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0)
            ? " Walk south three steps to supported Stone at surface (0, 3)."
            : string.Empty;
        return $"FOUND cannot establish Home here: {site.Reason}{fixtureHint}";
    }

    private static string ReturnRouteGuidance(
        ChronicleState state,
        HomeState home,
        ReturnRouteSnapshot? route)
    {
        if (route is null)
        {
            return $"THE FIRST HEARTH is known at {home.Address}.";
        }

        var currentRoute = route.Value;
        if (!currentRoute.IsTraversable)
        {
            return $"RETURN ROUTE TO THE FIRST HEARTH: Home is at {home.Address}, " +
                   "but this route is currently untraversable from this Stratum.";
        }

        if (currentRoute.Arrived)
        {
            return $"RETURN ROUTE: ARRIVED at THE FIRST HEARTH ({home.Address}). " +
                   "The Hearthstone remains a physical anchor; it never moves you.";
        }

        if (currentRoute.NextAddress is not { } nextAddress)
        {
            return $"RETURN ROUTE TO THE FIRST HEARTH: {currentRoute.RemainingSteps} physical steps remain.";
        }

        var direction = DirectionTo(state.Address, nextAddress);
        return $"RETURN ROUTE TO THE FIRST HEARTH: WALK {direction} to {nextAddress}. " +
               $"{currentRoute.RemainingSteps} physical steps remain; it never moves you.";
    }

    private static string DirectionTo(WorldAddress current, WorldAddress next) =>
        next.X < current.X ? "WEST" :
        next.X > current.X ? "EAST" :
        next.Y < current.Y ? "NORTH" :
        next.Y > current.Y ? "SOUTH" :
        "NOWHERE";

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static int VisibleCellCount(int pixelExtent, int cellSize)
    {
        var count = pixelExtent / cellSize;
        return count % 2 == 0 ? count - 1 : count;
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

    private VisualRenderPlan RequireActiveVisualPlan()
    {
        var plan = (_surfacePatchView.Visible ? _surfacePatchView : _skyStratumView).CurrentPlan;
        return plan ?? throw new InvalidOperationException(
            "The active World view has no shared Visual Grammar plan.");
    }

    private void CaptureGate3BPlayerReview(VisualRenderPlan plan)
    {
        var directory = Path.GetFullPath(
            Path.Combine(
                ProjectSettings.GlobalizePath("res://"),
                "..",
                "..",
                ".tools",
                "gate3b-review"));
        Directory.CreateDirectory(directory);
        var stem = $"player_s{_simulation.State.Seed}_sky_bell_stone_{_visualCellSize}px";
        var pngPath = Path.Combine(directory, $"{stem}.png");
        var metadataPath = Path.Combine(directory, $"{stem}.json");
        var image = VisualPackGodotAdapter.RasterizeNative(_visualPack, plan);
        var result = image.SavePng(pngPath);
        if (result != Error.Ok)
        {
            throw new InvalidOperationException($"Gate 3B player review capture failed with {result}.");
        }

        File.WriteAllText(
            metadataPath,
            JsonSerializer.Serialize(
                new
                {
                    _simulation.State.Seed,
                    Stratum = _simulation.State.Address.Stratum,
                    plan.Bounds,
                    plan.CellSize,
                    plan.PackId,
                    plan.PackDigest,
                    RenderPlanDigest = plan.Digest,
                    Incarnation = _simulation.State.Address,
                    LooseStone = _simulation.State.LooseStoneAddress,
                    Bell = SkyStratum.LandmarkAddress,
                    ReadsImmediately = new[] { "Incarnation", "The Bell That Fell Up", "Loose Stone" },
                    Review = "Player UAT: mark noise, broken joins, and hierarchy directly on this capture.",
                },
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string LifeDisplayName(IncarnationLifeState life) => life switch
    {
        IncarnationLifeState.Alive => "ALIVE",
        IncarnationLifeState.AwaitingReplacement => "ENDED",
        _ => "?",
    };

    private void RunGoal4BAcceptance()
    {
        try
        {
            VerifyAcceptance(
                _visualCellSize == 20,
                "Goal 4B's enlarged player fixture must run at the accepted 20-pixel cell size.");
            VerifyAcceptance(
                _simulation.State.Intent == OpeningIntent.Unchosen &&
                _simulation.State.Home is null &&
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
                "Goal 4B's first journey must begin from the fresh surface fixture.");

            VerifyMixedCodexIntrinsicVerbControls();
            StartFreshGoal4BFixture();

            var openingButtons = _openingPanel.GetChildren().OfType<Button>();
            VerifyAcceptance(
                _openingPanel.Visible &&
                openingButtons.Any(button => button.Visible && button.Text == "UP — EXPLORE") &&
                openingButtons.Any(button => button.Visible && button.Text == "HERE — BUILD"),
                "The First Horizon must visibly offer exact UP — EXPLORE and HERE — BUILD siblings.");

            Press(_hereButton);
            VerifyAcceptance(
                _simulation.State.Intent == OpeningIntent.Here &&
                _simulation.State.Codex.Contains(WordIds.Found) &&
                !_simulation.State.Codex.HasFly &&
                !_simulation.State.Codex.HasStone &&
                !_simulation.State.Codex.HasBell &&
                _simulation.State.ActiveLoadout[0].IsIntrinsicFound &&
                _simulation.State.ActiveLoadout[0].Noun is null &&
                _simulation.State.Home is null &&
                _equipFlyButton.Disabled &&
                _lastAnswerStatus.Contains("FOUND — BUILD STARTING VECTOR", StringComparison.Ordinal),
                "The real HERE button must grant and equip only intrinsic Found without a noun or a world mutation.");
            VerifyAcceptance(
                !_openingPanel.Visible &&
                _flyButton.Text == "FOUND" &&
                !_flyButton.Disabled,
                "HERE must dismiss the opening and expose FOUND in the existing first hotbar slot.");

            Press(_slowButton);
            VerifyAcceptance(
                _readout.Text.Contains("Clock: Slow", StringComparison.Ordinal),
                "The compact Chronicle header must visibly report Slow.");
            VerifyChronicleReadoutLayout();
            Press(_fastButton);
            VerifyAcceptance(
                _readout.Text.Contains("Clock: Fast", StringComparison.Ordinal),
                "The compact Chronicle header must visibly report Fast.");
            VerifyChronicleReadoutLayout();
            Press(_normalButton);
            VerifyAcceptance(
                _readout.Text.Contains("Clock: Normal", StringComparison.Ordinal),
                "The compact Chronicle header must visibly report Normal.");
            VerifyChronicleReadoutLayout();

            var beforeInvalidFound = _simulation.State;
            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State == beforeInvalidFound &&
                _simulation.State.Home is null &&
                _lastCommandStatus == "The Stone here is under water." &&
                _guidanceReadout.Text.Contains("The Stone here is under water.", StringComparison.Ordinal),
                "Found at surface (0, 0) must reject through Core without changing the water-covered ridge.");
            VerifyAcceptance(
                !RequireActiveVisualPlan().Marks.Any(mark =>
                    mark.VisualId == "subject.home-hearthstone"),
                "An invalid Found attempt must not draw a Hearthstone mark.");

            for (var step = 0; step < 3; step++)
            {
                Press(_directionButtons[2]);
            }

            var validSite = _simulation.HomeContext.CurrentSite;
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
                validSite.IsEligible &&
                validSite.Ground == WorldGround.Soil &&
                validSite.Feature == WorldFeature.Stone &&
                validSite.Reason == "The supported Stone here can become Home." &&
                _guidanceReadout.Text.Contains(validSite.Reason, StringComparison.Ordinal),
                "Three physical south steps must reach the supported Stone fixture where Found can work.");

            var addressBeforeFounding = _simulation.State.Address;
            var looseStoneBeforeFounding = _simulation.State.LooseStoneAddress;
            Press(_flyButton);
            var home = _simulation.State.Home ?? throw new InvalidOperationException(
                "Found did not create a Home state.");
            VerifyAcceptance(
                home.HoldingId == "holding.home" &&
                home.DisplayName == "The First Hearth" &&
                home.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
                home.FoundingIncarnationId == _simulation.State.IncarnationId &&
                home.Material == HomeMaterialState.HearthstoneRaised &&
                _simulation.State.Address == addressBeforeFounding &&
                _simulation.State.LooseStoneAddress == looseStoneBeforeFounding,
                "Using the existing FOUND hotbar slot must found exactly one Home without moving the body or loose Stone.");

            var foundedContext = _simulation.HomeContext;
            VerifyAcceptance(
                foundedContext.Home == home &&
                foundedContext.ReturnRoute is { IsTraversable: true, Arrived: true, NextAddress: null } &&
                foundedContext.ReturnRoute.Value.RemainingSteps == (UInt128)0 &&
                _statusReadout.Text.Contains("THE FIRST HEARTH", StringComparison.Ordinal) &&
                _statusReadout.Text.Contains(home.Address.ToString(), StringComparison.Ordinal) &&
                _statusReadout.Text.Contains($"Founded Tick {home.FoundedTick}", StringComparison.Ordinal) &&
                _statusReadout.Text.Contains(
                    $"Incarnation #{home.FoundingIncarnationId}",
                    StringComparison.Ordinal) &&
                _statusReadout.Text.Contains("HearthstoneRaised", StringComparison.Ordinal) &&
                _statusReadout.Text.Contains(ChronicleState.HomeHearthstoneIdentity, StringComparison.Ordinal) &&
                _guidanceReadout.Text.Contains("RETURN ROUTE: ARRIVED", StringComparison.Ordinal),
                "Home status and guidance must present Core-owned identity, founding facts, material, and physical arrival.");
            VerifyLabelFits(_statusReadout, "Home status");
            VerifyLabelFits(_guidanceReadout, "Home arrival guidance");
            VerifyGoal4BHearthstonePresentation(home);

            Press(_directionButtons[3]);
            Press(_directionButtons[0]);
            var beforeXFirstRoute = _simulation.State;
            var xFirstRoute = _simulation.HomeContext.ReturnRoute;
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 2) &&
                _simulation.State == beforeXFirstRoute &&
                xFirstRoute is { IsTraversable: true, Arrived: false, NextAddress: not null } &&
                xFirstRoute.Value.NextAddress == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 2) &&
                xFirstRoute.Value.RemainingSteps == (UInt128)2 &&
                _guidanceReadout.Text.Contains("WALK WEST", StringComparison.Ordinal),
                "A same-surface Return Route must expose Core's X step before its Y step and never move the Incarnation.");
            Press(_directionButtons[1]);
            var ySecondRoute = _simulation.HomeContext.ReturnRoute;
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 2) &&
                ySecondRoute is { IsTraversable: true, Arrived: false, NextAddress: not null } &&
                ySecondRoute.Value.NextAddress == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
                ySecondRoute.Value.RemainingSteps == (UInt128)1 &&
                _guidanceReadout.Text.Contains("WALK SOUTH", StringComparison.Ordinal),
                "After the Core-owned X step, the same Return Route must expose its Y step.");
            Press(_directionButtons[2]);

            var foundedState = _simulation.State;
            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State == foundedState &&
                _lastCommandStatus.Contains("singular Home", StringComparison.Ordinal),
                "A repeated FOUND use must visibly reject without changing the singular Home.");

            for (var step = 1; step <= 3; step++)
            {
                Press(_directionButtons[0]);
                var beforeRouteQuery = _simulation.State;
                var route = _simulation.HomeContext.ReturnRoute;
                var expectedAddress = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3 - step);
                var expectedNext = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 4 - step);
                VerifyAcceptance(
                    _simulation.State.Address == expectedAddress &&
                    _simulation.State == beforeRouteQuery &&
                    route is { IsTraversable: true, Arrived: false, NextAddress: not null } &&
                    route.Value.NextAddress == expectedNext &&
                    route.Value.RemainingSteps == (UInt128)step &&
                    _guidanceReadout.Text.Contains("RETURN ROUTE", StringComparison.Ordinal) &&
                    _guidanceReadout.Text.Contains("WALK SOUTH", StringComparison.Ordinal) &&
                    _guidanceReadout.Text.Contains($"{step} physical steps remain", StringComparison.Ordinal),
                    "The Core-owned Return Route must guide each northward departure back south without moving the Incarnation itself.");
                VerifyLabelFits(_guidanceReadout, "Home return guidance");
            }

            Press(_saveButton);
            VerifyAcceptance(
                SaveVersion(_simulation.State) == ChronicleSaveCodec.CurrentVersion &&
                ChronicleSaveCodec.CurrentVersion == 4,
                "The Goal 4B journey must save the current Home state in strict version 4.");
            GD.Print("GOAL4B SAVE READY home=surface:0,3 route=surface:0,1 save=4");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4B ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void VerifyMixedCodexIntrinsicVerbControls()
    {
        var upOnly = new ChronicleSimulation(ChronicleState.Begin(InitialSeed));
        upOnly.Apply(new ChooseUpIntent());
        VerifyAcceptance(
            upOnly.State.Codex.HasFly &&
            !upOnly.State.Codex.Contains(WordIds.Found),
            "The UP opening alone must grant Fly without granting Found.");

        var hereOnly = new ChronicleSimulation(ChronicleState.Begin(InitialSeed));
        hereOnly.Apply(new ChooseHereIntent());
        VerifyAcceptance(
            hereOnly.State.Codex.Contains(WordIds.Found) &&
            !hereOnly.State.Codex.HasFly,
            "The HERE opening alone must grant Found without granting Fly.");

        var mixedBuilder = new ChronicleSimulation(
            upOnly.State with
            {
                Intent = OpeningIntent.Unchosen,
                Loadout = LoadoutState.Empty,
            });
        mixedBuilder.Apply(new ChooseHereIntent());
        _simulation = new ChronicleSimulation(
            mixedBuilder.State with
            {
                Intent = OpeningIntent.Up,
                Loadout = LoadoutState.Empty,
            });
        ResetGoal4BPresentation("Started mixed-Codex control fixture.");

        var loadoutControls = _equipFlyButton
            .GetParent()
            .GetChildren()
            .OfType<Button>()
            .ToArray();
        var equipFly = loadoutControls.SingleOrDefault(button => button.Text == "EQUIP FLY");
        var equipFound = loadoutControls.SingleOrDefault(button => button.Text == "EQUIP FOUND");
        VerifyAcceptance(
            _simulation.State.Intent == OpeningIntent.Up &&
            _simulation.State.Codex.HasFly &&
            _simulation.State.Codex.Contains(WordIds.Found) &&
            equipFly is { Visible: true, Disabled: false } &&
            equipFound is { Visible: true, Disabled: false },
            "A mixed Codex must expose independent EQUIP FLY and EQUIP FOUND controls regardless of Opening Intent.");

        var flyControl = equipFly!;
        var foundControl = equipFound!;
        var codexPanel = flyControl.GetParent() as Control ?? throw new InvalidOperationException(
            "Loadout controls require their existing Codex panel.");
        var orderedControls = new[]
        {
            flyControl,
            foundControl,
            _equipSmashButton,
            _fitStoneButton,
            _clearFirstSlotButton,
        };
        VerifyAcceptance(
            orderedControls.All(button =>
                button.Visible &&
                button.Position.X >= 0 &&
                button.Position.Y >= 0 &&
                button.Position.X + button.Size.X <= codexPanel.Size.X &&
                button.Position.Y + button.Size.Y <= codexPanel.Size.Y) &&
            ControlsDoNotOverlap(orderedControls),
            "EQUIP FLY, EQUIP FOUND, EQUIP SMASH, FIT STONE, and CLEAR SLOT 1 must fit without overlap " +
            $"in the existing Codex panel. panel={codexPanel.Size}; " +
            $"controls={string.Join(",", orderedControls.Select(control =>
                $"{control.Text}:{control.Position}+{control.Size}/min{control.GetMinimumSize()}"))}");
        foreach (var control in orderedControls)
        {
            VerifyControlFits(control, control.Text);
        }

        Press(foundControl);
        VerifyAcceptance(
            _simulation.State.ActiveLoadout[0].IsIntrinsicFound &&
            _simulation.State.ActiveLoadout[0].Noun is null &&
            !flyControl.Disabled &&
            foundControl.Disabled,
            "The visible EQUIP FOUND control must equip intrinsic Found under a nonmatching UP Intent.");
        Press(flyControl);
        VerifyAcceptance(
            _simulation.State.ActiveLoadout[0].IsIntrinsicFly &&
            _simulation.State.ActiveLoadout[0].Noun is null &&
            flyControl.Disabled &&
            !foundControl.Disabled,
            "The visible EQUIP FLY control must independently replace Found from the same mixed Codex.");
    }

    private void StartFreshGoal4BFixture()
    {
        _simulation = new ChronicleSimulation(ChronicleState.Begin(InitialSeed));
        ResetGoal4BPresentation("Reset canonical Goal 4B fixture.");
    }

    private void ResetGoal4BPresentation(string saveLoadStatus)
    {
        _pulseAccumulator = 0;
        _hasRenderedWorld = false;
        _renderedTargets = [];
        _targetingSlot = null;
        _deathConfirmationArmed = false;
        _studyChoicesExposed = false;
        _lastCommandStatus = string.Empty;
        _lastAnswerStatus = string.Empty;
        _lastSaveLoadStatus = saveLoadStatus;
        RefreshPresentation();
    }

    private void RunGoal4BRestartAcceptance()
    {
        try
        {
            VerifyAcceptance(
                _visualCellSize == 20,
                "Goal 4B's restart fixture must run at the accepted 20-pixel cell size.");
            var home = _simulation.State.Home ?? throw new InvalidOperationException(
                "Goal 4B restart did not restore Home.");
            VerifyAcceptance(
                _simulation.State.Intent == OpeningIntent.Here &&
                _simulation.State.Codex.Contains(WordIds.Found) &&
                _simulation.State.ActiveLoadout[0].IsIntrinsicFound &&
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0) &&
                home.HoldingId == "holding.home" &&
                home.DisplayName == "The First Hearth" &&
                home.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
                home.Material == HomeMaterialState.HearthstoneRaised,
                "Goal 4B restart must restore the exact saved Build Chronicle and Home.");
            VerifyChronicleReadoutLayout();

            var beforeRouteQuery = _simulation.State;
            var route = _simulation.HomeContext.ReturnRoute;
            VerifyAcceptance(
                _simulation.State == beforeRouteQuery &&
                route is { IsTraversable: true, Arrived: false, NextAddress: not null } &&
                route.Value.NextAddress == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 1) &&
                route.Value.RemainingSteps == (UInt128)3 &&
                _guidanceReadout.Text.Contains("RETURN ROUTE", StringComparison.Ordinal) &&
                _guidanceReadout.Text.Contains("WALK SOUTH", StringComparison.Ordinal) &&
                _guidanceReadout.Text.Contains("3 physical steps remain", StringComparison.Ordinal),
                "The restored Return Route must remain physical guidance to the saved Home and never move the Incarnation.");
            VerifyGoal4BHearthstonePresentation(home);

            for (var step = 1; step <= 3; step++)
            {
                Press(_directionButtons[2]);
                var currentRoute = _simulation.HomeContext.ReturnRoute;
                VerifyAcceptance(
                    _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, step) &&
                    currentRoute is not null,
                    "Following the displayed route must use only the ordinary south movement control.");
            }

            var arrivedRoute = _simulation.HomeContext.ReturnRoute;
            VerifyAcceptance(
                _simulation.State.Address == home.Address &&
                arrivedRoute is { IsTraversable: true, Arrived: true, NextAddress: null } &&
                arrivedRoute.Value.RemainingSteps == (UInt128)0 &&
                _guidanceReadout.Text.Contains("RETURN ROUTE: ARRIVED", StringComparison.Ordinal) &&
                _statusReadout.Text.Contains("THE FIRST HEARTH", StringComparison.Ordinal),
                "Physical south movement must arrive at Home without opening a management mode or moving by a hidden route action.");
            VerifyGoal4BHearthstonePresentation(home);

            Press(_saveButton);
            VerifyAcceptance(
                SaveVersion(_simulation.State) == 4,
                "The returned Home Chronicle must remain a strict version-4 save.");
            GD.Print(
                "GOAL4B ACCEPTANCE PASS home=surface:0,3 material=hearthstone " +
                "route=physical view=50x36 save=4");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4B RESTART ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void RunGoal4CAcceptance()
    {
        try
        {
            VerifyAcceptance(
                _visualCellSize == 20,
                "Goal 4C's isolated player fixture must run at the accepted 20-pixel cell size.");
            VerifyLegacyOpeningCompatibility();
            StartFreshGoal4CFixture();
            VerifyGoal4CControls();

            var threat = DriveCombatToCairn();
            VerifyGoal4CCairnPresentation(
                threat,
                "subject.riven-cairn-river-ward",
                dangerVisible: true);
            VerifyChronicleReadoutLayout();
            VerifyLabelFits(_statusReadout, "Cairn threat status");
            VerifyLabelFits(_guidanceReadout, "River-Ward threat guidance");

            var pausedThreat = _simulation.State;
            var pausedPlanDigest = RequireActiveVisualPlan().Digest;
            _Process(ClockPulseSeconds * 2);
            VerifyAcceptance(
                _simulation.State == pausedThreat &&
                _simulation.ConflictContext == threat &&
                RequireActiveVisualPlan().Digest == pausedPlanDigest,
                "Paused Godot clock pulses must leave the River-Ward, Cairn, danger emphasis, pending result, and Tick unchanged.");

            Press(_directionButtons[1]);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
                _simulation.State.Speed == ChronicleSpeed.Paused &&
                _simulation.ConflictContext is null &&
                !RequireActiveVisualPlan().Marks.Any(mark =>
                    mark.VisualId == "emphasis.danger.river-ward"),
                "Leaving the Cairn while paused must clear only the pending exchange and its time-driven danger emphasis.");
            Press(_directionButtons[3]);
            threat = RequireGoal4CThreat(prepared: false);

            var beforePrepare = _simulation.State;
            Press(_flyButton);
            var prepared = RequireGoal4CThreat(prepared: true);
            VerifyAcceptance(
                _simulation.State.Tick == beforePrepare.Tick &&
                _simulation.State.Address == beforePrepare.Address &&
                _simulation.State.FirstConflict is
                {
                    PendingAction: { IsIntrinsicSmash: true },
                    Outcome: null,
                } &&
                _lastCommandStatus == "Prepared Smash for the next active Chronicle tick." &&
                _statusReadout.Text.Contains("Pending: SMASH", StringComparison.Ordinal) &&
                _guidanceReadout.Text.Contains("SMASH is prepared", StringComparison.Ordinal),
                "The existing SMASH hotbar slot must prepare a Core-owned pending action without changing the Cairn before a tick.");
            VerifyGoal4CCairnPresentation(
                prepared,
                "subject.riven-cairn-river-ward",
                dangerVisible: true);

            Press(_saveButton);
            VerifyAcceptance(
                SaveVersion(_simulation.State) == 4,
                "The threatened pending-SMASH Chronicle must save under strict envelope version 4.");
            GD.Print("GOAL4C THREATENED SAVE READY address=surface:1,3 pending=Smash save=4");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4C ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void RunGoal4CRestartAcceptance()
    {
        try
        {
            VerifyAcceptance(
                _visualCellSize == 20,
                "Goal 4C's threatened restart must run at the accepted 20-pixel cell size.");
            var threat = RequireGoal4CThreat(prepared: true);
            VerifyAcceptance(
                _simulation.State.Intent == OpeningIntent.Against &&
                _simulation.State.Codex.Contains(WordIds.Smash) &&
                _simulation.State.ActiveLoadout[0].IsIntrinsicSmash &&
                _readout.Text.Contains("Clock: Paused", StringComparison.Ordinal) &&
                _codexReadout.Text.Contains("Verbs: Smash", StringComparison.Ordinal),
                "The separate process must restore the paused Combat Chronicle, Codex Smash, equipped Loadout, and pending ward exchange exactly.");
            VerifyGoal4CCairnPresentation(
                threat,
                "subject.riven-cairn-river-ward",
                dangerVisible: true);

            var frozen = _simulation.State;
            var frozenPlanDigest = RequireActiveVisualPlan().Digest;
            _Process(ClockPulseSeconds * 2);
            VerifyAcceptance(
                _simulation.State == frozen &&
                _simulation.ConflictContext == threat &&
                RequireActiveVisualPlan().Digest == frozenPlanDigest,
                "The separate threatened restart must keep all time-driven conflict presentation frozen until the Clock resumes.");

            var tickBeforeResolution = _simulation.State.Tick;
            Press(_slowButton);
            _Process(ClockPulseSeconds);
            var resolved = _simulation.ConflictContext ?? throw new InvalidOperationException(
                "The first active tick did not retain a resolved Cairn context.");
            VerifyAcceptance(
                _simulation.State.HasLivingIncarnation &&
                _simulation.State.Tick == tickBeforeResolution + 1 &&
                _simulation.State.Address == threat.Address &&
                resolved.IsResolved &&
                resolved.Outcome == FirstConflictOutcome.Shattered &&
                resolved.PendingAction is null &&
                resolved.ResolvedTick == _simulation.State.Tick &&
                resolved.ResolvingIncarnationId == _simulation.State.IncarnationId &&
                _statusReadout.Text.Contains(FirstConflictSubjects.ShatteredCairnIdentity, StringComparison.Ordinal) &&
                _guidanceReadout.Text.Contains("underlying Stone ridge remains material", StringComparison.Ordinal),
                "The first delivered active Chronicle tick must shatter the ward through Core while the body stays at the same Address.");
            VerifyGoal4CCairnPresentation(
                resolved,
                "subject.shattered-cairn",
                dangerVisible: false);

            Press(_saveButton);
            VerifyAcceptance(
                SaveVersion(_simulation.State) == 4,
                "The resolved Shattered Cairn Chronicle must save under strict envelope version 4.");
            GD.Print("GOAL4C SUCCESS RESOLVED address=surface:1,3 result=shattered save=4");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4C RESTART ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void RunGoal4CResolvedRestartAcceptance()
    {
        try
        {
            VerifyAcceptance(
                _visualCellSize == 20,
                "Goal 4C's resolved restart must run at the accepted 20-pixel cell size.");
            var resolved = _simulation.ConflictContext ?? throw new InvalidOperationException(
                "Goal 4C resolved restart did not restore a conflict context.");
            VerifyAcceptance(
                _simulation.State.HasLivingIncarnation &&
                _simulation.State.Intent == OpeningIntent.Against &&
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3) &&
                _simulation.State.Codex.Contains(WordIds.Smash) &&
                _simulation.State.ActiveLoadout[0].IsIntrinsicSmash &&
                resolved.IsResolved &&
                resolved.CairnIdentity == FirstConflictSubjects.ShatteredCairnIdentity &&
                resolved.Outcome == FirstConflictOutcome.Shattered &&
                _statusReadout.Text.Contains(FirstConflictSubjects.ShatteredCairnIdentity, StringComparison.Ordinal),
                "A separate application restart must restore the same living body, Smash Loadout, and durable Shattered Cairn result.");
            VerifyGoal4CCairnPresentation(
                resolved,
                "subject.shattered-cairn",
                dangerVisible: false);

            Press(_directionButtons[1]);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
                _simulation.ConflictContext is
                {
                    IsResolved: true,
                    Outcome: FirstConflictOutcome.Shattered,
                } &&
                !_statusReadout.Text.Contains(
                    FirstConflictSubjects.ShatteredCairnIdentity,
                    StringComparison.Ordinal) &&
                !_statusReadout.Text.Contains(
                    FirstConflictSubjects.RiverWardIdentity,
                    StringComparison.Ordinal) &&
                !_guidanceReadout.Text.Contains("Resolved on Tick", StringComparison.Ordinal) &&
                !_guidanceReadout.Text.Contains("WALK", StringComparison.Ordinal) &&
                !_guidanceReadout.Text.Contains("Re-equip SMASH", StringComparison.Ordinal) &&
                _guidanceReadout.Text.Contains(
                    "No active conflict follows",
                    StringComparison.Ordinal),
                "Leaving the Shattered Cairn must keep its durable Core result without presenting it as the player's current place.");
            Press(_directionButtons[3]);
            VerifyAcceptance(
                _simulation.ConflictContext is { IsResolved: true, Outcome: FirstConflictOutcome.Shattered } &&
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3) &&
                _simulation.State.Speed != ChronicleSpeed.Paused,
                "Leaving and returning to the Shattered Cairn must not create a second fight or pause the Chronicle.");
            VerifyGoal4CCairnPresentation(
                _simulation.ConflictContext!,
                "subject.shattered-cairn",
                dangerVisible: false);

            Press(_saveButton);
            VerifyAcceptance(
                SaveVersion(_simulation.State) == 4,
                "The revisited Shattered Cairn must remain a strict version-4 Chronicle save.");
            GD.Print("GOAL4C SUCCESS RESTART PASS address=surface:1,3 result=shattered save=4");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4C RESOLVED RESTART ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void RunGoal4CFailureAcceptance()
    {
        try
        {
            VerifyAcceptance(
                _visualCellSize == 20,
                "Goal 4C's intentional no-action branch must run at the accepted 20-pixel cell size.");
            StartFreshGoal4CFixture();
            var threat = DriveCombatToCairn();
            VerifyAcceptance(
                !threat.IsSmashPrepared && threat.PendingAction is null,
                "The intentional failure branch must reach the ward without preparing Smash.");

            var tickBeforeFailure = _simulation.State.Tick;
            Press(_fastButton);
            _Process(ClockPulseSeconds);
            VerifyAcceptance(
                _simulation.State.IncarnationLife == IncarnationLifeState.AwaitingReplacement &&
                !_simulation.State.HasLivingIncarnation &&
                _simulation.State.Tick == tickBeforeFailure + 1 &&
                _simulation.State.FirstConflict is null &&
                _replacementPanel.Visible &&
                _replacementReadout.Text.Contains("Time is not advancing", StringComparison.Ordinal),
                "A no-action Fast pulse must end the body on its first tick and make every later tick in that pulse inert.");
            VerifyGoal4CIntactCairnPresentation();

            Press(_createReplacementButton);
            VerifyAcceptance(
                _simulation.State.HasLivingIncarnation &&
                _simulation.State.IncarnationId == 2 &&
                _simulation.State.Intent == OpeningIntent.Against &&
                _simulation.State.Codex.Contains(WordIds.Smash) &&
                _simulation.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty) &&
                _simulation.State.FirstConflict is null &&
                !_replacementPanel.Visible &&
                !_equipSmashButton.Disabled &&
                _codexReadout.Text.Contains("Verbs: Smash", StringComparison.Ordinal),
                "The failure replacement must retain Smash in the Codex with exactly eight empty Loadout slots and an intact ward.");
            VerifyGoal4CIntactCairnPresentation();

            Press(_saveButton);
            VerifyAcceptance(
                SaveVersion(_simulation.State) == 4,
                "The intact-ward replacement branch must remain a strict version-4 Chronicle save.");
            GD.Print("GOAL4C FAILURE ACCEPTANCE PASS tick=1 replacement=2 smash=retained loadout=empty ward=intact");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4C FAILURE ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void StartFreshGoal4CFixture()
    {
        _simulation = new ChronicleSimulation(ChronicleState.Begin(InitialSeed));
        ResetGoal4BPresentation("Reset canonical Goal 4C fixture.");
    }

    private void VerifyLegacyOpeningCompatibility()
    {
        foreach (var grammarVersion in new[] { 0, 1, 2 })
        {
            _simulation = new ChronicleSimulation(
                ChronicleState.Begin(InitialSeed) with { WorldGrammarVersion = grammarVersion });
            ResetGoal4BPresentation($"Loaded grammar-{grammarVersion} opening compatibility fixture.");

            VerifyAcceptance(
                _simulation.State.Intent == OpeningIntent.Unchosen &&
                _openingPanel.Visible &&
                !_againstButton.Visible &&
                _againstButton.Disabled &&
                _openingPrompt.Text == LegacyOpeningPrompt &&
                _upButton.Visible &&
                _upButton.Position == new Vector2(522, 430) &&
                _hereButton.Visible &&
                _hereButton.Position == new Vector2(818, 430) &&
                !_openingPanel
                    .GetChildren()
                    .OfType<Button>()
                    .Any(button => button.Visible && button.Text == "AGAINST — COMBAT"),
                $"Migrated grammar-{grammarVersion} Chronicles must retain the two-vector Explore/Build opening without Combat.");
        }
    }

    private ConflictContextSnapshot DriveCombatToCairn()
    {
        var openingButtons = _openingPanel.GetChildren().OfType<Button>();
        VerifyAcceptance(
            _openingPanel.Visible &&
            openingButtons.Any(button => button.Visible && button.Text == "AGAINST — COMBAT") &&
            openingButtons.Any(button => button.Visible && button.Text == "UP — EXPLORE") &&
            openingButtons.Any(button => button.Visible && button.Text == "HERE — BUILD"),
            "The First Horizon must visibly offer AGAINST — COMBAT, UP — EXPLORE, and HERE — BUILD together.");

        Press(_againstButton);
        VerifyAcceptance(
            _simulation.State.Intent == OpeningIntent.Against &&
            _simulation.State.Codex.Contains(WordIds.Smash) &&
            !_simulation.State.Codex.Contains(WordIds.Fly) &&
            !_simulation.State.Codex.Contains(WordIds.Found) &&
            _simulation.State.ActiveLoadout[0].IsIntrinsicSmash &&
            _flyButton.Text == "SMASH" &&
            !_flyButton.Disabled &&
            _lastAnswerStatus.Contains("SMASH — COMBAT STARTING VECTOR", StringComparison.Ordinal),
            "The real AGAINST button must grant only intrinsic Smash through the normal Codex, Loadout, and first hotbar slot.");
        VerifyAcceptance(
            _guidanceReadout.Text.Contains(
                FirstConflictSubjects.RivenCairnIdentity,
                StringComparison.Ordinal) &&
            _guidanceReadout.Text.Contains("surface (1, 3)", StringComparison.Ordinal) &&
            !_guidanceReadout.Text.Contains(
                SkyStratum.LandmarkName,
                StringComparison.Ordinal),
            "The Combat Starting Vector must direct the Chronicle Thread toward the generated Riven Cairn rather than the unreachable Bell.");

        Press(_directionButtons[3]);
        for (var step = 0; step < 3; step++)
        {
            Press(_directionButtons[2]);
        }

        return RequireGoal4CThreat(prepared: false);
    }

    private ConflictContextSnapshot RequireGoal4CThreat(bool prepared)
    {
        var context = _simulation.ConflictContext ?? throw new InvalidOperationException(
            "The Riven Cairn did not expose a Core-owned conflict context.");
        VerifyAcceptance(
            _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3) &&
            _simulation.State.Speed == ChronicleSpeed.Paused &&
            context.IsThreatened &&
            !context.IsResolved &&
            context.CairnIdentity == FirstConflictSubjects.RivenCairnIdentity &&
            context.SubjectIdentity == FirstConflictSubjects.RiverWardIdentity &&
            context.History == FirstConflictSubjects.History &&
            context.Warning == FirstConflictSubjects.Warning &&
            context.Address == _simulation.State.Address &&
            context.IsSmashPrepared == prepared &&
            context.PendingAction == (prepared
                ? (LoadoutSlot?)new LoadoutSlot(WordIds.Smash)
                : null) &&
            _statusReadout.Text.Contains(FirstConflictSubjects.RivenCairnIdentity, StringComparison.Ordinal) &&
            _statusReadout.Text.Contains(FirstConflictSubjects.RiverWardIdentity, StringComparison.Ordinal) &&
            _guidanceReadout.Text.Contains(FirstConflictSubjects.History, StringComparison.Ordinal) &&
            _guidanceReadout.Text.Contains(FirstConflictSubjects.Warning, StringComparison.Ordinal),
            "Entering the unresolved Cairn must pause before a tick and present Core-owned identity, history, warning, and pending action facts.");
        return context;
    }

    private void VerifyGoal4CCairnPresentation(
        ConflictContextSnapshot context,
        string visualId,
        bool dangerVisible)
    {
        var plan = RequireActiveVisualPlan();
        VerifyAcceptance(
            plan.Bounds.Width == 51 &&
            plan.Bounds.Height == 37 &&
            plan.Bounds.Width * _visualCellSize == MapPixelWidth &&
            plan.Bounds.Height * _visualCellSize == MapPixelHeight,
            "The 20-pixel Goal 4C player view must retain the accepted 51 × 37 playspace.");
        VerifyAcceptance(
            _worldArea is not null,
            "Cairn presentation requires the shared generated World Area.");
        var cairn = _worldArea!.Cells.Single(cell => cell.Address == context.Address);
        VerifyAcceptance(
            cairn.Ground == WorldGround.Soil &&
            cairn.Feature == WorldFeature.Stone &&
            cairn.DurableIdentity == context.CairnIdentity &&
            plan.Marks.Any(mark =>
                mark.Address == context.Address &&
                mark.VisualId == visualId &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject) &&
            plan.Marks.Any(mark =>
                mark.Address == context.Address &&
                mark.VisualId == "emphasis.danger.river-ward" &&
                mark.Layer == VisualLayerClass.TemporaryAction) == dangerVisible,
            "The shared visual plan must render the Core-owned Cairn subject over unchanged Soil/Stone and only show ward danger while threatened.");
    }

    private void VerifyGoal4CIntactCairnPresentation()
    {
        var plan = RequireActiveVisualPlan();
        var cairnAddress = new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3);
        VerifyAcceptance(
            _worldArea is not null &&
            _worldArea.Cells.Single(cell => cell.Address == cairnAddress).DurableIdentity ==
            FirstConflictSubjects.RivenCairnIdentity &&
            plan.Marks.Any(mark =>
                mark.Address == cairnAddress &&
                mark.VisualId == "subject.riven-cairn-river-ward" &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject) &&
            !plan.Marks.Any(mark => mark.VisualId == "emphasis.danger.river-ward"),
            "The no-action branch must leave the intact Riven Cairn material subject without a stale danger animation or overlay.");
    }

    private void VerifyGoal4CControls()
    {
        var codexPanel = _equipFlyButton.GetParent() as Control ?? throw new InvalidOperationException(
            "Goal 4C Loadout controls require the existing Codex panel.");
        var controls = new[]
        {
            _equipFlyButton,
            _equipFoundButton,
            _equipSmashButton,
            _fitStoneButton,
            _clearFirstSlotButton,
        };
        VerifyAcceptance(
            controls.All(button =>
                button.Visible &&
                button.Position.X >= 0 &&
                button.Position.Y >= 0 &&
                button.Position.X + button.Size.X <= codexPanel.Size.X &&
                button.Position.Y + button.Size.Y <= codexPanel.Size.Y) &&
            ControlsDoNotOverlap(controls),
            "Fly, Found, Smash, Stone, and clear-slot controls must remain compact siblings in the existing Codex panel without overlap. " +
            $"panel={codexPanel.Size}; controls={string.Join(",", controls.Select(control =>
                $"{control.Text}:{control.Visible}:{control.Position}+{control.Size}"))}");
        foreach (var control in controls)
        {
            VerifyControlFits(control, control.Text);
        }

        VerifyChronicleReadoutLayout();
    }

    private void VerifyGoal4BHearthstonePresentation(HomeState home)
    {
        var plan = RequireActiveVisualPlan();
        VerifyAcceptance(
            plan.Bounds.Width >= 51 &&
            plan.Bounds.Height >= 37 &&
            plan.Bounds.Width * _visualCellSize <= MapPixelWidth &&
            plan.Bounds.Height * _visualCellSize <= MapPixelHeight,
            "The 20-pixel Home player view must expose at least 51 × 37 native cells.");
        VerifyAcceptance(
            _worldArea is not null,
            "Home presentation requires the shared generated World Area.");
        var hearthstoneCell = _worldArea!.Cells.Single(cell => cell.Address == home.Address);
        VerifyAcceptance(
            hearthstoneCell.Ground == WorldGround.Soil &&
            hearthstoneCell.Feature == WorldFeature.Stone &&
            hearthstoneCell.DurableIdentity == ChronicleState.HomeHearthstoneIdentity &&
            plan.Marks.Any(mark =>
                mark.Address == home.Address &&
                mark.VisualId == "subject.home-hearthstone" &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject),
            "The Hearthstone overlay must use the shared visual plan while retaining the generated soil ridge beneath it.");
    }

    private static int SaveVersion(ChronicleState state)
    {
        using var document = JsonDocument.Parse(ChronicleSaveCodec.Serialize(state));
        return document.RootElement.GetProperty("Version").GetInt32();
    }

    private void RunGoal4APartialSave()
    {
        try
        {
            VerifyAcceptance(
                _simulation.State.Intent == OpeningIntent.Unchosen &&
                _simulation.State.Tick == 0 &&
                !_simulation.State.Codex.HasFly,
                "Goal 4A partial-save proof must begin from a fresh Chronicle.");

            DriveExploreToBell();
            Press(_studyButton);
            VerifyGoal4AStudyChoiceSurface(selectedWord: null, stoneUnderstanding: 0, bellUnderstanding: 0);
            Press(_studyOfferButtons[0]);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord == WordIds.Stone &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 0 &&
                _simulation.State.Study.UnderstandingFor(WordIds.Bell) == 0,
                "The Stone offer control must start only Stone Study.");

            Press(_pauseButton);
            var pausedStudy = _simulation.State;
            _Process(ClockPulseSeconds * 2);
            VerifyAcceptance(
                _simulation.State == pausedStudy,
                "Paused Goal 4A Study must not advance the Chronicle or Understanding.");

            AdvanceSlowClockPulses(5);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord == WordIds.Stone &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 5 &&
                _simulation.State.Study.UnderstandingFor(WordIds.Bell) == 0 &&
                !_simulation.State.Codex.HasStone &&
                !_simulation.State.Codex.HasBell,
                "Five slow Chronicle pulses must produce the exact active Stone partial save.");

            Press(_saveButton);
            GD.Print("GOAL4A PARTIAL SAVE READY stone=5 bell=0 active=Stone");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4A PARTIAL SAVE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void RunGoal4AAcceptance()
    {
        try
        {
            VerifyGoal4APartialStoneRestart();

            Press(_studyButton);
            VerifyGoal4AStudyChoiceSurface(WordIds.Stone, stoneUnderstanding: 5, bellUnderstanding: 0);
            Press(_saveButton);
            var partialStoneSave = _simulation.State;
            Press(_directionButtons[2]);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord is null &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 5 &&
                !_studyChoicesPanel.Visible &&
                _studyOfferButtons.All(button => button.Disabled),
                "Leaving the source must stop active Stone Study without losing its Understanding.");
            Press(_loadButton);
            VerifyAcceptance(
                _simulation.State == partialStoneSave &&
                _simulation.State.Study.ActiveWord == WordIds.Stone,
                "Loading must restore the exact active Stone pursuit and partial Understanding.");

            Press(_ringBellButton);
            VerifyAcceptance(
                _deathConfirmationArmed &&
                _simulation.State.HasLivingIncarnation,
                "The Bell must still require its visible death confirmation.");
            Press(_ringBellButton);
            VerifyAcceptance(
                _simulation.State.IncarnationLife == IncarnationLifeState.AwaitingReplacement &&
                _simulation.State.Study.ActiveWord is null &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 5,
                "Death must clear active Study while preserving Stone Understanding.");

            Press(_createReplacementButton);
            VerifyAcceptance(
                _simulation.State.IncarnationLife == IncarnationLifeState.Alive &&
                _simulation.State.IncarnationId == 2 &&
                _simulation.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty) &&
                _simulation.State.Codex.HasFly &&
                !_simulation.State.Codex.HasStone &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 5 &&
                _simulation.State.Study.UnderstandingFor(WordIds.Bell) == 0,
                "A replacement must inherit the Chronicle's words and partial Understanding with eight empty slots.");

            Press(_equipFlyButton);
            VerifyAcceptance(
                _simulation.State.ActiveLoadout[0].IsIntrinsicFly,
                "The replacement must deliberately re-equip Fly from its retained Codex.");
            Press(_flyButton);
            for (var step = 0; step < 4; step++)
            {
                Press(_directionButtons[0]);
            }

            Press(_studyButton);
            VerifyGoal4AStudyChoiceSurface(selectedWord: null, stoneUnderstanding: 5, bellUnderstanding: 0);
            Press(_studyOfferButtons[0]);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord == WordIds.Stone,
                "Returning to the source must require and accept a deliberate Stone reselection.");
            AdvanceSlowClockPulses(11);
            VerifyAcceptance(
                _simulation.State.Codex.HasStone &&
                !_simulation.State.Codex.HasBell &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) ==
                WordCatalogue.Get(WordIds.Stone).UnderstandingRequired &&
                _simulation.State.Study.UnderstandingFor(WordIds.Bell) == 0 &&
                _simulation.State.Study.ActiveWord is null,
                "The selected Stone pursuit must complete exactly once without advancing Bell.");
            var completedStone = _simulation.State;
            VerifyAcceptance(
                _studyOfferReadouts[0].Text.Contains("LEARNED", StringComparison.Ordinal),
                "The completed Stone offer must visibly report learned status.");
            Press(_studyOfferButtons[0]);
            VerifyAcceptance(
                _simulation.State == completedStone,
                "Repeating a learned Stone choice must not duplicate Codex membership or progress.");

            Press(_flyButton);
            for (var step = 0; step < 4; step++)
            {
                Press(_directionButtons[2]);
            }

            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
                "The retained intrinsic Fly must still return the replacement to the surface fixture.");
            Press(_fitStoneButton);
            Press(_flyButton);
            Press(_directionButtons[3]);
            VerifyAcceptance(
                _simulation.State.ActiveLoadout[0].IsFlyStone &&
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0) &&
                _simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
                "The accepted Fly[Stone] material regression must remain available after Stone Study completes.");

            VerifyGoal4ALegacySourceSurface();
            StartFreshGoal4AFixture();
            DriveExploreToBell();
            Press(_studyButton);
            VerifyGoal4AStudyChoiceSurface(selectedWord: null, stoneUnderstanding: 0, bellUnderstanding: 0);
            Press(_studyOfferButtons[1]);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord == WordIds.Bell &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 0 &&
                _simulation.State.Study.UnderstandingFor(WordIds.Bell) == 0,
                "The independent Bell offer control must start only Bell Study.");
            AdvanceSlowClockPulses(5);
            Press(_saveButton);
            var partialBellSave = _simulation.State;
            Press(_directionButtons[2]);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord is null &&
                _simulation.State.Study.UnderstandingFor(WordIds.Bell) == 5 &&
                !_studyChoicesPanel.Visible &&
                _studyOfferButtons.All(button => button.Disabled),
                "Leaving the Bell fixture must preserve its partial Bell Understanding.");
            Press(_loadButton);
            VerifyAcceptance(
                _simulation.State == partialBellSave &&
                _simulation.State.Study.ActiveWord == WordIds.Bell,
                "Loading the independent fixture must restore the exact active Bell pursuit.");
            Press(_studyButton);
            VerifyGoal4AStudyChoiceSurface(WordIds.Bell, stoneUnderstanding: 0, bellUnderstanding: 5);
            AdvanceSlowClockPulses(11);
            VerifyAcceptance(
                _simulation.State.Codex.HasBell &&
                !_simulation.State.Codex.HasStone &&
                _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 0 &&
                _simulation.State.Study.UnderstandingFor(WordIds.Bell) ==
                WordCatalogue.Get(WordIds.Bell).UnderstandingRequired &&
                _simulation.State.Study.ActiveWord is null,
                "Bell must complete exactly once in its independent fixture while Stone remains unlearned.");
            var completedBell = _simulation.State;
            VerifyAcceptance(
                _studyOfferReadouts[1].Text.Contains("LEARNED", StringComparison.Ordinal),
                "The completed Bell offer must visibly report learned status.");
            Press(_studyOfferButtons[1]);
            VerifyAcceptance(
                _simulation.State == completedBell,
                "Repeating a learned Bell choice must not duplicate Codex membership or progress.");

            Press(_saveButton);
            GD.Print(
                "GOAL4A ACCEPTANCE PASS " +
                $"stoneBranch={completedStone.Study.UnderstandingFor(WordIds.Stone)} " +
                $"bellBranch={completedBell.Study.UnderstandingFor(WordIds.Bell)} " +
                $"finalStone={completedBell.Study.UnderstandingFor(WordIds.Stone)} " +
                $"finalBell={completedBell.Study.UnderstandingFor(WordIds.Bell)} " +
                "evidence=stone-replacement,bell-save-load");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL4A ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void DriveExploreToBell()
    {
        var openingButtons = _openingPanel.GetChildren().OfType<Button>();
        VerifyAcceptance(
            _openingPanel.Visible &&
            openingButtons.Any(button => button.Visible && button.Text == "UP — EXPLORE") &&
            openingButtons.Any(button => button.Visible && button.Text == "HERE — BUILD"),
            "The First Horizon must visibly offer HERE — BUILD alongside UP — EXPLORE.");
        Press(_upButton);
        VerifyAcceptance(
            _simulation.State.Codex.HasFly &&
            !_simulation.State.Codex.Contains(WordIds.Found) &&
            _equipFoundButton.Disabled &&
            _lastAnswerStatus.Contains("EXPLORE STARTING VECTOR", StringComparison.Ordinal),
            "The Explore Starting Vector must grant only Fly through Core.");
        Press(_flyButton);
        for (var step = 0; step < 4; step++)
        {
            Press(_directionButtons[0]);
        }

        VerifyAcceptance(
            _simulation.State.Address == SkyStratum.LandmarkAddress &&
            _simulation.CurrentStudySource is not null,
            "The real Fly and movement controls must reach the generated Study Source.");
    }

    private void VerifyGoal4APartialStoneRestart()
    {
        var source = _simulation.CurrentStudySource;
        VerifyAcceptance(
            source is not null &&
            _simulation.State.Intent == OpeningIntent.Up &&
            _simulation.State.Address == SkyStratum.LandmarkAddress &&
            _simulation.State.Codex.HasFly &&
            !_simulation.State.Codex.HasStone &&
            !_simulation.State.Codex.HasBell &&
            _simulation.State.Study.ActiveSourceId == source.Id &&
            _simulation.State.Study.ActiveWord == WordIds.Stone &&
            _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 5 &&
            _simulation.State.Study.UnderstandingFor(WordIds.Bell) == 0,
            "--verify-4a must restart from the exact active Stone=5 partial save made by --verify-4a-partial.");
    }

    private void VerifyGoal4AStudyChoiceSurface(
        WordId? selectedWord,
        int stoneUnderstanding,
        int bellUnderstanding)
    {
        const string expectedStoneRationale =
            "Its dark clapper is stone veined with open sky and rises against the curve that contains it.";
        const string expectedBellRationale =
            "The gold vessel, clapper, and impossible fall make its identity legible as a Bell.";

        var source = _simulation.CurrentStudySource;
        VerifyAcceptance(source is not null, "The Bell fixture must expose a current Study Source.");
        VerifyAcceptance(
            source!.Offers.Count == 2 &&
            source.Offers[0].Word.Id == WordIds.Stone &&
            source.Offers[1].Word.Id == WordIds.Bell &&
            source.Offers[0].Rationale == expectedStoneRationale &&
            source.Offers[1].Rationale == expectedBellRationale &&
            source.Rarity == StudySourceRarity.Rare &&
            source.Danger == StudySourceDanger.Lethal &&
            source.Significance == StudySourceSignificance.Landmark &&
            source.Offers.All(offer => offer.UnderstandingYield == 16),
            "The source snapshot must expose exact ordered Stone then Bell offers, reasons, qualities, and yield.");
        VerifyAcceptance(
            _studyChoicesPanel.Visible &&
            _studySourceReadout.Text.Contains(source.Name, StringComparison.Ordinal) &&
            _studySourceReadout.Text.Contains(source.Situation, StringComparison.Ordinal) &&
            _studySourceReadout.Text.Contains("Rare / Lethal / Landmark", StringComparison.Ordinal) &&
            _studySourceReadout.Text.Contains("16 UNDERSTANDING", StringComparison.Ordinal) &&
            _studyOfferButtons[0].Visible &&
            _studyOfferButtons[1].Visible &&
            _studyOfferReadouts[0].Text.Contains(expectedStoneRationale, StringComparison.Ordinal) &&
            _studyOfferReadouts[1].Text.Contains(expectedBellRationale, StringComparison.Ordinal),
            "The right-panel choice surface must visibly render Core-owned source context and both rationales.");
        VerifyAcceptance(
            _studyOfferButtons[0].Text.Contains("STONE", StringComparison.Ordinal) &&
            _studyOfferButtons[1].Text.Contains("BELL", StringComparison.Ordinal) &&
            _studyOfferReadouts[0].Text.Contains("Stone", StringComparison.Ordinal) &&
            _studyOfferReadouts[1].Text.Contains("Bell", StringComparison.Ordinal),
            "The visible offer controls must preserve Core's Stone-then-Bell order.");
        var expectedStoneStatus = selectedWord == WordIds.Stone ? "SELECTED" : "AVAILABLE";
        var expectedBellStatus = selectedWord == WordIds.Bell ? "SELECTED" : "AVAILABLE";
        var expectedStoneAction = selectedWord == WordIds.Stone ? "SELECTED" : "CHOOSE";
        var expectedBellAction = selectedWord == WordIds.Bell ? "SELECTED" : "CHOOSE";
        VerifyAcceptance(
            source.Offers[0].CurrentUnderstanding == stoneUnderstanding &&
            source.Offers[1].CurrentUnderstanding == bellUnderstanding &&
            source.Offers[0].IsSelected == (selectedWord == WordIds.Stone) &&
            source.Offers[1].IsSelected == (selectedWord == WordIds.Bell) &&
            _studyOfferReadouts[0].Text.Contains(
                $"{stoneUnderstanding}/{source.Offers[0].UnderstandingRequired}",
                StringComparison.Ordinal) &&
            _studyOfferReadouts[1].Text.Contains(
                $"{bellUnderstanding}/{source.Offers[1].UnderstandingRequired}",
                StringComparison.Ordinal) &&
            _studyOfferReadouts[0].Text.Contains(expectedStoneStatus, StringComparison.Ordinal) &&
            _studyOfferReadouts[1].Text.Contains(expectedBellStatus, StringComparison.Ordinal) &&
            _studyOfferButtons[0].Text.Contains(expectedStoneAction, StringComparison.Ordinal) &&
            _studyOfferButtons[1].Text.Contains(expectedBellAction, StringComparison.Ordinal),
            "Each visible offer must report Core-owned word-specific Understanding and selection status.");
        VerifyLabelFits(_studySourceReadout, "Study Source context");
        VerifyLabelFits(_studyOfferReadouts[0], "Stone offer rationale");
        VerifyLabelFits(_studyOfferReadouts[1], "Bell offer rationale");
        VerifyAcceptance(
            _studySourceReadout.Position.Y + _studySourceReadout.Size.Y <=
            _studyOfferReadouts[0].Position.Y &&
            _studyOfferReadouts[0].Position.Y + _studyOfferReadouts[0].Size.Y <=
            _studyOfferReadouts[1].Position.Y &&
            _studyOfferReadouts[1].Position.Y + _studyOfferReadouts[1].Size.Y <=
            _studyChoicesPanel.Size.Y &&
            _studyOfferReadouts.All(readout =>
                readout.Position.X + readout.Size.X <= _studyChoicesPanel.Size.X) &&
            _studyOfferButtons.All(button =>
                button.Position.X + button.Size.X <= _studyChoicesPanel.Size.X &&
                button.Position.Y + button.Size.Y <= _studyChoicesPanel.Size.Y),
            "Study Source context and ordered offers must fit without overlap inside the choice panel. " +
            $"panel={_studyChoicesPanel.Size}; source={_studySourceReadout.Position}+{_studySourceReadout.Size}; " +
            $"stone={_studyOfferReadouts[0].Position}+{_studyOfferReadouts[0].Size}; " +
            $"bell={_studyOfferReadouts[1].Position}+{_studyOfferReadouts[1].Size}; " +
            $"buttons={string.Join(",", _studyOfferButtons.Select(button => $"{button.Position}+{button.Size}"))}");
    }

    private void AdvanceSlowClockPulses(int count)
    {
        Press(_slowButton);
        for (var pulse = 0; pulse < count; pulse++)
        {
            _Process(ClockPulseSeconds);
        }
    }

    private void StartFreshGoal4AFixture()
    {
        _simulation = new ChronicleSimulation(ChronicleState.Begin(InitialSeed));
        _pulseAccumulator = 0;
        _hasRenderedWorld = false;
        _renderedTargets = [];
        _targetingSlot = null;
        _deathConfirmationArmed = false;
        _studyChoicesExposed = false;
        _lastCommandStatus = string.Empty;
        _lastAnswerStatus = string.Empty;
        _lastSaveLoadStatus = "Started independent Goal 4A Bell fixture.";
        RefreshPresentation();
    }

    private void VerifyGoal4ALegacySourceSurface()
    {
        const string legacyAtBell =
            """
            {
              "Seed": 41337,
              "Tick": 7,
              "Address": { "Stratum": "sky", "X": 0, "Y": -4 },
              "Speed": 2,
              "Intent": 1,
              "Codex": { "HasFly": true, "HasStone": false },
              "Study": { "StoneUnderstanding": 7, "IsStudyingBell": false }
            }
            """;
        _simulation = new ChronicleSimulation(ChronicleSaveCodec.Deserialize(legacyAtBell));
        _pulseAccumulator = 0;
        _hasRenderedWorld = false;
        _targetingSlot = null;
        _deathConfirmationArmed = false;
        _studyChoicesExposed = false;
        RefreshPresentation();

        Press(_studyButton);
        var source = _simulation.CurrentStudySource;
        VerifyAcceptance(
            source is not null &&
            source.Offers.Count == 1 &&
            source.Offers[0].Word.Id == WordIds.Stone &&
            _studyChoicesPanel.Visible &&
            _studyOfferButtons[0].Visible &&
            !_studyOfferButtons[0].Disabled &&
            _studyOfferReadouts[0].Text.Contains("Stone · 7/16 · AVAILABLE", StringComparison.Ordinal) &&
            !_studyOfferButtons[1].Visible &&
            _studyOfferButtons[1].Disabled &&
            !_studyOfferReadouts[1].Visible,
            "A version-0 Chronicle must render one resumable Stone offer and never expose Bell.");
        Press(_studyOfferButtons[0]);
        VerifyAcceptance(
            _simulation.State.Study.ActiveWord == WordIds.Stone &&
            _simulation.State.Study.UnderstandingFor(WordIds.Stone) == 7,
            "The version-0 Stone offer must remain selectable through the current source/word command.");
    }

    private void RunSlice2CAcceptance()
    {
        try
        {
            if (_verifyGate3BPlayer)
            {
                var openingPlan = RequireActiveVisualPlan();
                VerifyAcceptance(
                    openingPlan.PackId == _visualPack.PackId &&
                    openingPlan.PackDigest == _visualPack.Digest &&
                    openingPlan.CellSize == _visualCellSize,
                    "The player view must consume the selected compiled visual pack exactly.");
                VerifyAcceptance(
                    openingPlan.Bounds.Width >= 33 &&
                    openingPlan.Bounds.Height >= 23 &&
                    openingPlan.Bounds.Width * _visualCellSize <= MapPixelWidth &&
                    openingPlan.Bounds.Height * _visualCellSize <= MapPixelHeight,
                    "The Gate 3B player view must show at least 33 × 23 native logical cells.");
                VerifyAcceptance(
                    openingPlan.Marks.Count(mark => mark.Layer == VisualLayerClass.GroundField) ==
                    openingPlan.Bounds.Width * openingPlan.Bounds.Height,
                    "Every player-view cell must render through the shared ground mapping.");
                foreach (var glyph in new[]
                         {
                             "glyph.codex",
                             "glyph.loadout",
                             "glyph.codex.fly",
                             "glyph.codex.stone",
                         })
                {
                    VerifyAcceptance(
                        _visualPack.Resolve(glyph).VisualId == glyph,
                        $"The runtime pack must retain the '{glyph}' UI mapping.");
                }
            }

            VerifyAcceptance(
                _simulation.State.Intent == OpeningIntent.Unchosen && _openingPanel.Visible,
                "A fresh Chronicle must present the modal UP Intent.");
            VerifyAcceptance(
                _directionButtons.All(button => button.Disabled),
                "Direction controls must be unavailable before choosing UP.");
            VerifyAcceptance(
                _hotbarSlots.Count == 8 &&
                _hotbarSlots.All(button => button.Disabled),
                "The opening must show eight unavailable hotbar slots.");

            Press(_upButton);
            VerifyAcceptance(_simulation.State.CanFly, "The UP button must grant Fly through Core.");
            VerifyAcceptance(_simulation.State.Codex.HasFly, "UP must add Fly to the explicit Codex.");
            VerifyAcceptance(!_openingPanel.Visible, "Choosing UP must dismiss the opening panel.");
            VerifyAcceptance(
                !_flyButton.Disabled && _flyButton.Text == "FLY\nUP",
                "The first hotbar slot must present FLY UP.");
            if (_verifyGate3BPlayer)
            {
                VerifyControlFits(_flyButton, "20/16-pixel Fly hotbar slot");
            }
            VerifyAcceptance(
                _studyButton.Visible &&
                _studyButton.Disabled &&
                _studyButton.Text == "STUDY AT BELL" &&
                _guidanceReadout.Text.Contains("sky (0, -4)", StringComparison.Ordinal),
                "Study must remain discoverable away from its Bell source.");
            VerifyAcceptance(
                _codexReadout.Text.Contains("Verbs: Fly", StringComparison.Ordinal),
                "The Codex must visibly list Fly.");
            VerifyAcceptance(
                _statusReadout.Text.Contains("THE CHRONICLE ANSWERS: FLY", StringComparison.Ordinal),
                "The Chronicle's answer must be visible after choosing UP.");

            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 0, 0),
                "The Fly hotbar slot must preserve coordinates when entering the sky.");
            VerifyAcceptance(
                _skyStratumView.Visible && !_surfacePatchView.Visible,
                "The sky view must replace the surface view in the sky.");
            VerifyAcceptance(
                !_flyButton.Disabled && _flyButton.Text == "FLY\nDOWN",
                "The sky must present FLY DOWN in the first hotbar slot.");

            for (var step = 0; step < 4; step++)
            {
                Press(_directionButtons[0]);
            }

            VerifyAcceptance(
                _simulation.State.Address == SkyStratum.LandmarkAddress,
                "Four north button presses must reach the Bell.");
            if (_verifyGate3BPlayer)
            {
                VerifyAcceptance(
                    RequireActiveVisualPlan().Marks.Any(mark =>
                        mark.Address == SkyStratum.LandmarkAddress &&
                        mark.VisualId == "landmark.bell-that-fell-up" &&
                        mark.Layer == VisualLayerClass.LandmarkOrSubject),
                    "The player must be able to identify the Bell from its Landmark silhouette.");
            }

            VerifyAcceptance(
                _statusReadout.Text.Contains(SkyStratum.LandmarkName, StringComparison.Ordinal) &&
                _statusReadout.Text.Contains(SkyStratum.LandmarkArrivalLine, StringComparison.Ordinal),
                "The Bell name and arrival line must be visible on arrival.");
            VerifyAcceptance(
                _studyButton.Visible &&
                !_studyButton.Disabled &&
                _studyButton.Text == "STUDY SKY-STONE",
                "The Bell must expose its artifact as a Study action without revealing the learned word.");
            VerifyChronicleReadoutLayout();
            VerifyLabelFits(_codexReadout, "Codex readout");
            VerifyNoVerticalOverlap(
                _codexReadout,
                _loadoutReadout,
                "Codex readout",
                "Loadout readout");
            VerifyNoVerticalOverlap(
                _loadoutReadout,
                _equipFlyButton,
                "Loadout readout",
                "Loadout controls");
            VerifyLabelFits(_guidanceReadout, "Chronicle guidance");
            VerifyLabelFits(_statusReadout, "Bell status");

            Press(_studyButton);
            VerifyAcceptance(
                _studyChoicesPanel.Visible &&
                _studyOfferButtons.Count >= 1 &&
                !_studyOfferButtons[0].Disabled,
                "The Study action must expose a selectable Core-owned source offer.");
            Press(_studyOfferButtons[0]);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord == WordIds.Stone,
                "The Stone offer must translate to an active Core Study.");
            Press(_pauseButton);
            var pausedStudy = _simulation.State;
            _Process(ClockPulseSeconds * 2);
            VerifyAcceptance(
                _simulation.State == pausedStudy,
                "Paused Godot clock pulses must not advance Study or Tick.");

            Press(_normalButton);
            _Process(ClockPulseSeconds);
            VerifyAcceptance(
                _simulation.State.Study.StoneUnderstanding == 2 &&
                _codexReadout.Text.Contains("2/16", StringComparison.Ordinal),
                "Study progress must advance on Chronicle ticks and remain visible.");

            Press(_saveButton);
            var savedState = _simulation.State;
            var savedVisualDigest = _verifyGate3BPlayer
                ? RequireActiveVisualPlan().Digest
                : string.Empty;
            Press(_directionButtons[2]);
            VerifyAcceptance(
                _simulation.State.Study.ActiveWord is null &&
                _simulation.State.Study.StoneUnderstanding == savedState.Study.StoneUnderstanding,
                "Leaving the Bell must stop Study without losing understanding.");
            Press(_loadButton);
            VerifyAcceptance(_simulation.State == savedState, "Load must restore exact partial Study state.");
            if (_verifyGate3BPlayer)
            {
                VerifyAcceptance(
                    RequireActiveVisualPlan().Digest == savedVisualDigest,
                    "Save/load must reproduce the exact controlled visual variants.");
            }

            for (var pulse = 0; pulse < 7; pulse++)
            {
                _Process(ClockPulseSeconds);
            }

            VerifyAcceptance(
                _simulation.State.Codex.HasStone &&
                _simulation.State.Study.StoneUnderstanding ==
                WordCatalogue.Get(WordIds.Stone).UnderstandingRequired &&
                _simulation.State.Study.ActiveWord is null,
                "Sixteen Chronicle ticks of Study must keep Stone in the Codex.");
            VerifyAcceptance(
                _codexReadout.Text.Contains("Nouns: Stone", StringComparison.Ordinal) &&
                _codexReadout.Text.Contains("[■■■■] KEPT", StringComparison.Ordinal),
                "The learned Noun must be unmistakable in the Codex.");
            VerifyLabelFits(_codexReadout, "completed Codex readout");
            var completedStudy = _simulation.State;
            Press(_studyButton);
            VerifyAcceptance(
                _simulation.State == completedStudy &&
                _statusReadout.Text.Contains("already keeps Stone", StringComparison.Ordinal),
                "Repeating completed Study must visibly confirm no duplicate or state change.");

            Press(_saveButton);
            var learnedState = _simulation.State;
            Press(_directionButtons[2]);
            Press(_fastButton);
            VerifyAcceptance(_simulation.State != learnedState, "The acceptance probe must perturb the learned save.");
            Press(_loadButton);
            VerifyAcceptance(_simulation.State == learnedState, "Load must restore Fly, Stone, progress, and clock.");

            for (var step = 0; step < 4; step++)
            {
                Press(_directionButtons[2]);
            }

            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 0, 0),
                "Four south button presses must return to sky (0, 0).");
            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
                "FLY DOWN must preserve coordinates when returning to the surface.");
            VerifyAcceptance(_simulation.State.CanFly, "Returning to the surface must retain Fly.");
            VerifyAcceptance(
                _surfacePatchView.Visible && !_skyStratumView.Visible,
                "The surface view must return with the Incarnation.");

            Press(_directionButtons[3]);
            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 1, 0),
                "The Fly hotbar slot must work away from the original address.");
            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0),
                "FLY DOWN must return to the matching non-origin surface address.");

            VerifyAcceptance(
                _simulation.State.ActiveLoadout.Slots.Count == LoadoutState.SlotCount &&
                _simulation.State.ActiveLoadout[0].IsIntrinsicFly &&
                _loadoutReadout.Text.Contains("LOADOUT SLOT 1: FLY", StringComparison.Ordinal),
                "The migrated Codex must visibly drive an eight-slot Loadout with intrinsic Fly in slot one.");
            VerifyLabelFits(_loadoutReadout, "Loadout readout");

            Press(_clearFirstSlotButton);
            VerifyAcceptance(
                _simulation.State.ActiveLoadout[0].IsEmpty &&
                !_simulation.State.CanFly &&
                _flyButton.Disabled &&
                _flyButton.Text == "—",
                "Clearing slot one must visibly remove active Fly without removing it from the Codex.");

            Press(_equipFlyButton);
            VerifyAcceptance(
                _simulation.State.ActiveLoadout[0].IsIntrinsicFly &&
                _simulation.State.CanFly &&
                _flyButton.Text == "FLY\nUP",
                "Equipping intrinsic Fly must restore self-flight through the Loadout.");

            Press(_fitStoneButton);
            VerifyAcceptance(
                _simulation.State.ActiveLoadout[0].IsFlyStone &&
                !_simulation.State.CanFly &&
                _flyButton.Text == "FLY\n[STONE]",
                "Fitting Stone must replace intrinsic Fly with the visible FLY[STONE] Expression.");
            if (_verifyGate3BPlayer)
            {
                VerifyControlFits(_flyButton, "20/16-pixel Fly[Stone] hotbar slot");
            }

            Press(_directionButtons[1]);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
                "The Slice 2B target fixture must stand west of the loose Stone.");

            Press(_flyButton);
            VerifyAcceptance(
                _targetingSlot == 0 &&
                _simulation.ValidTargetsForSlot(0)
                    .SequenceEqual(new[] { ChronicleState.InitialLooseStoneAddress }) &&
                _flyButton.Text == "CANCEL\nTARGET" &&
                _guidanceReadout.Text.Contains("highlighted loose Stone", StringComparison.Ordinal),
                "Selecting FLY[STONE] must enter cardinal targeting and highlight only Core-valid targets.");
            if (_verifyGate3BPlayer)
            {
                VerifyAcceptance(
                    RequireActiveVisualPlan().Marks.Any(mark =>
                        mark.Address == ChronicleState.InitialLooseStoneAddress &&
                        mark.VisualId == "emphasis.target.valid" &&
                        mark.Layer == VisualLayerClass.TemporaryAction),
                    "A Core-valid loose Stone must receive the shared visual target emphasis.");
            }

            Press(_directionButtons[3]);
            VerifyAcceptance(
                _targetingSlot is null &&
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0) &&
                _simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
                "East target input must move only the loose Stone to matching sky coordinates.");

            Press(_equipFlyButton);
            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 0, 0) &&
                _simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0) &&
                _skyStratumView.Visible,
                "Intrinsic Fly must let the Incarnation see the same moved Stone in the sky.");
            if (_verifyGate3BPlayer)
            {
                VerifyAcceptance(
                    RequireActiveVisualPlan().Marks.Any(mark =>
                        mark.Address == new WorldAddress(SkyStratum.StratumName, 1, 0) &&
                        mark.VisualId == "subject.loose-stone" &&
                        mark.Layer == VisualLayerClass.LandmarkOrSubject),
                    "The moved loose Stone must retain its durable-subject silhouette in the sky.");
            }

            Press(_fitStoneButton);
            Press(_flyButton);
            Press(_directionButtons[3]);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 0, 0) &&
                _simulation.State.LooseStoneAddress == ChronicleState.InitialLooseStoneAddress,
                "The same fitted Expression must return the loose Stone without moving the Incarnation.");

            Press(_equipFlyButton);
            Press(_flyButton);
            Press(_fitStoneButton);
            Press(_flyButton);
            Press(_directionButtons[3]);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0) &&
                _simulation.State.ActiveLoadout[0].IsFlyStone &&
                _simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
                "The final acceptance state must retain fitted Fly and a material Stone delta.");

            Press(_saveButton);
            var fittedSave = _simulation.State;
            Press(_clearFirstSlotButton);
            Press(_directionButtons[0]);
            Press(_fastButton);
            VerifyAcceptance(_simulation.State != fittedSave, "The Slice 2B restore probe must perturb Loadout and position.");
            Press(_loadButton);
            VerifyAcceptance(
                _simulation.State == fittedSave &&
                _loadoutReadout.Text.Contains("FLY[STONE]", StringComparison.Ordinal),
                "Load must restore the exact fitted Loadout and moved loose Stone.");

            Press(_equipFlyButton);
            Press(_flyButton);
            for (var step = 0; step < 4; step++)
            {
                Press(_directionButtons[0]);
            }

            Press(_fitStoneButton);
            VerifyAcceptance(
                _simulation.State.Address == SkyStratum.LandmarkAddress &&
                _simulation.State.ActiveLoadout[0].IsFlyStone &&
                _simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0) &&
                !_ringBellButton.Disabled &&
                _ringBellButton.Text == "END THIS BODY",
                "The first Incarnation must reach the Bell with Fly[Stone] and its changed world intact.");
            if (_verifyGate3BPlayer)
            {
                var bellAndStonePlan = RequireActiveVisualPlan();
                VerifyAcceptance(
                    bellAndStonePlan.Marks.Any(mark =>
                        mark.VisualId == "landmark.bell-that-fell-up") &&
                    bellAndStonePlan.Marks.Any(mark =>
                        mark.VisualId == "subject.loose-stone") &&
                    bellAndStonePlan.Marks.Any(mark =>
                        mark.VisualId == "actor.incarnation"),
                    "The Bell, moved Stone, and first Incarnation must remain legible together.");

                Press(_directionButtons[2]);
                var reviewPlan = RequireActiveVisualPlan();
                VerifyAcceptance(
                    _simulation.State.Address ==
                    new WorldAddress(SkyStratum.StratumName, 0, -3) &&
                    reviewPlan.Marks.Any(mark =>
                        mark.Address == SkyStratum.LandmarkAddress &&
                        mark.VisualId == "landmark.bell-that-fell-up") &&
                    reviewPlan.Marks.Any(mark =>
                        mark.Address == _simulation.State.Address &&
                        mark.VisualId == "actor.incarnation"),
                    "The visual review fixture must show the Bell and Incarnation as separate readable silhouettes.");
                CaptureGate3BPlayerReview(reviewPlan);
                Press(_directionButtons[0]);
                VerifyAcceptance(
                    _simulation.State.Address == SkyStratum.LandmarkAddress,
                    "The visual review fixture must return to the Bell before its consequential action.");
            }

            var beforeConfirmation = _simulation.State;
            Press(_ringBellButton);
            VerifyAcceptance(
                _simulation.State == beforeConfirmation &&
                _deathConfirmationArmed &&
                _ringBellButton.Text == "CONFIRM DEATH",
                "The first Bell interaction must visibly confirm death without ending the body.");

            Press(_ringBellButton);
            var awaitingSave = _simulation.State;
            VerifyAcceptance(
                awaitingSave.IncarnationLife == IncarnationLifeState.AwaitingReplacement &&
                awaitingSave.IncarnationId == 1 &&
                _replacementPanel.Visible &&
                _replacementReadout.Text.Contains("Time is not advancing", StringComparison.Ordinal),
                "Confirmed Bell death must expose a timeless awaiting-replacement state.");
            VerifyAcceptance(
                _directionButtons.All(button => button.Disabled) &&
                _hotbarSlots.All(button => button.Disabled) &&
                _pauseButton.Disabled &&
                _slowButton.Disabled &&
                _normalButton.Disabled &&
                _fastButton.Disabled &&
                _studyButton.Disabled &&
                _ringBellButton.Disabled,
                "No Incarnation or clock control may remain available while awaiting replacement.");
            VerifyLabelFits(_replacementReadout, "awaiting-replacement readout");

            _Process(ClockPulseSeconds * 2);
            VerifyAcceptance(
                _simulation.State == awaitingSave,
                "Godot clock pulses must leave the awaiting Chronicle completely unchanged.");

            Press(_replacementSaveButton);
            Press(_createReplacementButton);
            VerifyAcceptance(
                _simulation.State.IncarnationLife == IncarnationLifeState.Alive &&
                _simulation.State.IncarnationId == 2,
                "The visible replacement control must create the deterministic second Incarnation.");
            Press(_loadButton);
            VerifyAcceptance(
                _simulation.State == awaitingSave && _replacementPanel.Visible,
                "A real save/load must restore the exact pre-replacement Chronicle.");

            Press(_createReplacementButton);
            VerifyAcceptance(
                _simulation.State.IncarnationLife == IncarnationLifeState.Alive &&
                _simulation.State.IncarnationId == 2 &&
                _simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0) &&
                _simulation.State.ActiveLoadout.Slots.Count == LoadoutState.SlotCount &&
                _simulation.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty) &&
                !_simulation.State.CanFly &&
                !_replacementPanel.Visible,
                "Replacement must begin alive at the origin with exactly eight empty slots.");
            VerifyAcceptance(
                _simulation.State.Seed == awaitingSave.Seed &&
                _simulation.State.Tick == awaitingSave.Tick &&
                _simulation.State.Speed == awaitingSave.Speed &&
                _simulation.State.Intent == awaitingSave.Intent &&
                _simulation.State.Codex == awaitingSave.Codex &&
                _simulation.State.Study == awaitingSave.Study &&
                _simulation.State.LooseStoneAddress == awaitingSave.LooseStoneAddress &&
                _codexReadout.Text.Contains("Verbs: Fly", StringComparison.Ordinal) &&
                _codexReadout.Text.Contains("Nouns: Stone", StringComparison.Ordinal),
                "Replacement must visibly preserve words, Understanding, clock, and changed world state.");

            Press(_equipFlyButton);
            Press(_flyButton);
            VerifyAcceptance(
                _simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 0, 0) &&
                _simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0) &&
                _simulation.State.ActiveLoadout[0].IsIntrinsicFly &&
                _skyStratumView.Visible,
                "The replacement must deliberately equip Fly and see the first body's moved Stone.");

            Press(_saveButton);
            var replacementSave = _simulation.State;
            Press(_clearFirstSlotButton);
            Press(_directionButtons[0]);
            VerifyAcceptance(
                _simulation.State != replacementSave,
                "The replacement restore probe must perturb the new body.");
            Press(_loadButton);
            VerifyAcceptance(
                _simulation.State == replacementSave &&
                _readout.Text.Contains("INCARNATION #2 — ALIVE", StringComparison.Ordinal) &&
                _loadoutReadout.Text.Contains("LOADOUT SLOT 1: FLY", StringComparison.Ordinal),
                "Save/load must restore the replacement identity, Loadout, and complete Chronicle.");

            Press(_saveButton);
            if (_verifyGate3BPlayer)
            {
                GD.Print(
                    $"GATE3B PLAYER VISUAL ACCEPTANCE PASS size={_visualCellSize} " +
                    $"density={_visibleWorldBounds.Width}x{_visibleWorldBounds.Height} " +
                    $"pack={_visualPack.PackId}");
            }

            GD.Print("SLICE2C ACCEPTANCE PASS");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"SLICE2C ACCEPTANCE failed: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static void Press(Button button)
    {
        button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private static void VerifyAcceptance(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void VerifyLabelFits(Label label, string name)
    {
        VerifyControlFits(label, name);
    }

    private static void VerifyControlFits(Control control, string name)
    {
        var minimum = control.GetMinimumSize();
        VerifyAcceptance(
            minimum.X <= control.Size.X && minimum.Y <= control.Size.Y,
            $"{name} content minimum {minimum.X}×{minimum.Y} must fit " +
            $"assigned {control.Size.X}×{control.Size.Y} bounds.");
    }

    private void VerifyChronicleReadoutLayout()
    {
        var lines = _readout.Text.Split('\n');
        VerifyAcceptance(
            lines.Length <= 4 &&
            lines.Any(line => line.Contains("Clock:", StringComparison.Ordinal)),
            "The Chronicle readout must retain Clock within four lines above the Codex panel.");
        VerifyLabelFits(_readout, "Chronicle readout");
    }

    private static void VerifyNoVerticalOverlap(
        Control upper,
        Control lower,
        string upperName,
        string lowerName)
    {
        VerifyAcceptance(
            upper.Position.Y + upper.Size.Y <= lower.Position.Y,
            $"{upperName} must not overlap {lowerName}.");
    }

    private static bool ControlsDoNotOverlap(IReadOnlyList<Control> controls)
    {
        for (var firstIndex = 0; firstIndex < controls.Count; firstIndex++)
        {
            var first = controls[firstIndex];
            for (var secondIndex = firstIndex + 1; secondIndex < controls.Count; secondIndex++)
            {
                var second = controls[secondIndex];
                var separate =
                    first.Position.X + first.Size.X <= second.Position.X ||
                    second.Position.X + second.Size.X <= first.Position.X ||
                    first.Position.Y + first.Size.Y <= second.Position.Y ||
                    second.Position.Y + second.Size.Y <= first.Position.Y;
                if (!separate)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void LogState(string prefix)
    {
        var state = _simulation.State;
        GD.Print(
            $"{prefix} seed={state.Seed} tick={state.Tick} " +
            $"address={state.Address.Stratum}:{state.Address.X},{state.Address.Y} " +
            $"speed={state.Speed} intent={state.Intent} " +
            $"codex=Fly:{state.Codex.HasFly},Stone:{state.Codex.HasStone} " +
            $"study={state.Study.StoneUnderstanding}/{WordCatalogue.Get(WordIds.Stone).UnderstandingRequired} " +
            $"incarnation={state.IncarnationId}:{state.IncarnationLife} " +
            $"loadout={state.ActiveLoadout[0].DisplayName} " +
            $"stone={AddressLogText(state.LooseStoneAddress)} " +
            $"activeStudy={ActiveStudyWordLogText(state)} " +
            $"stoneUnderstanding={state.Study.UnderstandingFor(WordIds.Stone)}/" +
            $"{WordCatalogue.Get(WordIds.Stone).UnderstandingRequired} " +
            $"bellUnderstanding={state.Study.UnderstandingFor(WordIds.Bell)}/" +
            $"{WordCatalogue.Get(WordIds.Bell).UnderstandingRequired} " +
            $"codexWords={CodexWordsText(state, null)}");
    }

    private static string ActiveStudyWordLogText(ChronicleState state) =>
        state.Study.ActiveWord is { } wordId
            ? WordCatalogue.Get(wordId).DisplayName
            : "none";

    private static string AddressLogText(WorldAddress? address) => address is { } value
        ? $"{value.Stratum}:{value.X},{value.Y}"
        : "missing";
}
