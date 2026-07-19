using Chronicle.Core;
using Godot;

[GlobalClass]
public partial class ChronicleApp : Node
{
    private const string SavePath = "user://slice0_chronicle.json";
    private const long InitialSeed = 41_337;
    private const double ClockPulseSeconds = 0.25;

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
    private SurfacePatchView _surfacePatchView = null!;
    private SkyStratumView _skyStratumView = null!;
    private Label _readout = null!;
    private Label _codexReadout = null!;
    private Label _loadoutReadout = null!;
    private Label _statusReadout = null!;
    private Label _guidanceReadout = null!;
    private Button _upButton = null!;
    private Button _flyButton = null!;
    private Button _studyButton = null!;
    private Button _equipFlyButton = null!;
    private Button _fitStoneButton = null!;
    private Button _clearFirstSlotButton = null!;
    private Button _ringBellButton = null!;
    private Button _pauseButton = null!;
    private Button _slowButton = null!;
    private Button _normalButton = null!;
    private Button _fastButton = null!;
    private Button _saveButton = null!;
    private Button _loadButton = null!;
    private ColorRect _openingPanel = null!;
    private ColorRect _replacementPanel = null!;
    private Label _replacementReadout = null!;
    private Button _createReplacementButton = null!;
    private Button _replacementSaveButton = null!;
    private Button _replacementLoadButton = null!;
    private readonly List<Button> _directionButtons = [];
    private readonly List<Button> _hotbarSlots = [];
    private SkyStratum? _sky;
    private double _pulseAccumulator;
    private long _renderedSeed;
    private WorldAddress _renderedAddress;
    private bool _hasRenderedWorld;
    private string _lastSaveLoadStatus = "Starting Chronicle.";
    private string _lastAnswerStatus = string.Empty;
    private string _lastCommandStatus = string.Empty;
    private int? _targetingSlot;
    private bool _deathConfirmationArmed;

    public override void _Ready()
    {
        BuildWorldViews();
        BuildHotbar();
        BuildWorldGuidance();
        BuildControlPanel();
        BuildReplacementPanel();
        BuildOpeningPanel();

        LoadOrCreateChronicle();
        RefreshPresentation();
        LogState("SLICE2C READY");

        if (OS.GetCmdlineUserArgs().Contains("--verify-slice2c", StringComparer.Ordinal))
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
        _surfacePatchView = new SurfacePatchView
        {
            Name = "SurfacePatchView",
            Position = new Vector2(32, 56),
        };
        AddChild(_surfacePatchView);

        _skyStratumView = new SkyStratumView
        {
            Name = "SkyStratumView",
            Position = new Vector2(32, 56),
            Visible = false,
        };
        AddChild(_skyStratumView);
    }

    private void BuildHotbar()
    {
        var hotbar = new ColorRect
        {
            Name = "Hotbar",
            Position = new Vector2(32, 552),
            Size = new Vector2(660, 64),
            Color = new Color(0.025f, 0.045f, 0.065f, 0.96f),
        };

        for (var slot = 0; slot < 8; slot++)
        {
            var button = new Button
            {
                Name = $"HotbarSlot{slot + 1}",
                Position = new Vector2(4 + slot * 81, 4),
                Size = new Vector2(77, 56),
                Text = "—",
                Disabled = true,
                FocusMode = Control.FocusModeEnum.None,
            };
            button.AddThemeFontSizeOverride("font_size", 14);
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
            Position = new Vector2(32, 628),
            Size = new Vector2(660, 68),
            Color = new Color(0.025f, 0.045f, 0.065f, 0.96f),
        };

        var heading = new Label
        {
            Position = new Vector2(12, 7),
            Size = new Vector2(636, 20),
            Text = "CHRONICLE THREAD",
        };
        heading.AddThemeFontSizeOverride("font_size", 13);
        heading.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        panel.AddChild(heading);

        _guidanceReadout = new Label
        {
            Name = "GuidanceReadout",
            Position = new Vector2(12, 27),
            Size = new Vector2(636, 34),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _guidanceReadout.AddThemeFontSizeOverride("font_size", 14);
        _guidanceReadout.AddThemeColorOverride("font_color", new Color(0.76f, 0.84f, 0.9f));
        panel.AddChild(_guidanceReadout);
        AddChild(panel);
    }

    private void BuildControlPanel()
    {
        var panel = new ColorRect
        {
            Name = "ChronicleControls",
            Position = new Vector2(724, 24),
            Size = new Vector2(528, 672),
            Color = new Color(0.045f, 0.065f, 0.09f, 0.96f),
        };

        _readout = new Label
        {
            Name = "ChronicleReadout",
            Position = new Vector2(24, 18),
            Size = new Vector2(480, 132),
        };
        _readout.AddThemeFontSizeOverride("font_size", 18);
        _readout.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 1f));
        panel.AddChild(_readout);

        var codexPanel = new ColorRect
        {
            Name = "CodexPanel",
            Position = new Vector2(20, 158),
            Size = new Vector2(488, 180),
            Color = new Color(0.025f, 0.045f, 0.065f, 0.86f),
        };
        panel.AddChild(codexPanel);

        _codexReadout = new Label
        {
            Name = "CodexReadout",
            Position = new Vector2(16, 11),
            Size = new Vector2(456, 101),
        };
        _codexReadout.AddThemeFontSizeOverride("font_size", 16);
        _codexReadout.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        codexPanel.AddChild(_codexReadout);

        _loadoutReadout = new Label
        {
            Name = "LoadoutReadout",
            Position = new Vector2(16, 116),
            Size = new Vector2(456, 24),
        };
        _loadoutReadout.AddThemeFontSizeOverride("font_size", 15);
        _loadoutReadout.AddThemeColorOverride("font_color", new Color(0.84f, 0.91f, 0.97f));
        codexPanel.AddChild(_loadoutReadout);

        _equipFlyButton = AddCommandButton(
            codexPanel,
            "EQUIP FLY",
            new Vector2(16, 144),
            new Vector2(130, 32),
            () => ConfigureFirstSlot(noun: null));
        _fitStoneButton = AddCommandButton(
            codexPanel,
            "FIT STONE",
            new Vector2(154, 144),
            new Vector2(140, 32),
            () => ConfigureFirstSlot(ChronicleNoun.Stone));
        _clearFirstSlotButton = AddCommandButton(
            codexPanel,
            "CLEAR SLOT 1",
            new Vector2(302, 144),
            new Vector2(170, 32),
            ClearFirstSlot);

        var keyboardHelp = new Label
        {
            Name = "KeyboardHelp",
            Position = new Vector2(24, 342),
            Size = new Vector2(480, 42),
            Text = "Move/target: WASD/arrows   Clock: Space, 1/2/3\nUse Loadout: hotbar   Save: F5   Load: F9",
        };
        keyboardHelp.AddThemeFontSizeOverride("font_size", 14);
        keyboardHelp.AddThemeColorOverride("font_color", new Color(0.68f, 0.76f, 0.84f));
        panel.AddChild(keyboardHelp);

        _directionButtons.Add(AddCommandButton(
            panel,
            "N",
            new Vector2(96, 394),
            new Vector2(64, 38),
            () => MoveIncarnation(0, -1)));
        _directionButtons.Add(AddCommandButton(
            panel,
            "W",
            new Vector2(24, 438),
            new Vector2(64, 38),
            () => MoveIncarnation(-1, 0)));
        _directionButtons.Add(AddCommandButton(
            panel,
            "S",
            new Vector2(96, 438),
            new Vector2(64, 38),
            () => MoveIncarnation(0, 1)));
        _directionButtons.Add(AddCommandButton(
            panel,
            "E",
            new Vector2(168, 438),
            new Vector2(64, 38),
            () => MoveIncarnation(1, 0)));

        _pauseButton = AddCommandButton(
            panel,
            "Pause",
            new Vector2(24, 488),
            new Vector2(72, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Paused));
        _slowButton = AddCommandButton(
            panel,
            "Slow",
            new Vector2(104, 488),
            new Vector2(64, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Slow));
        _normalButton = AddCommandButton(
            panel,
            "Normal",
            new Vector2(176, 488),
            new Vector2(82, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Normal));
        _fastButton = AddCommandButton(
            panel,
            "Fast",
            new Vector2(266, 488),
            new Vector2(64, 38),
            () => SetChronicleSpeed(ChronicleSpeed.Fast));

        _saveButton = AddCommandButton(
            panel,
            "Save",
            new Vector2(24, 538),
            new Vector2(88, 40),
            SaveChronicle);
        _loadButton = AddCommandButton(
            panel,
            "Load",
            new Vector2(120, 538),
            new Vector2(88, 40),
            LoadChronicle);

        _studyButton = AddCommandButton(
            panel,
            "STUDY AT BELL",
            new Vector2(224, 538),
            new Vector2(136, 40),
            StudySkyStone);
        _studyButton.Disabled = true;

        _ringBellButton = AddCommandButton(
            panel,
            "RING AT BELL",
            new Vector2(368, 538),
            new Vector2(136, 40),
            RingBell);
        _ringBellButton.Disabled = true;

        _statusReadout = new Label
        {
            Name = "StatusReadout",
            Position = new Vector2(24, 584),
            Size = new Vector2(480, 84),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _statusReadout.AddThemeFontSizeOverride("font_size", 13);
        _statusReadout.AddThemeColorOverride("font_color", new Color(0.83f, 0.87f, 0.68f));
        panel.AddChild(_statusReadout);
        AddChild(panel);
    }

    private void BuildReplacementPanel()
    {
        _replacementPanel = new ColorRect
        {
            Name = "AwaitingReplacement",
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            Color = new Color(0.018f, 0.026f, 0.045f, 0.97f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 90,
            Visible = false,
        };

        var title = new Label
        {
            Position = new Vector2(340, 130),
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
            Position = new Vector2(340, 210),
            Size = new Vector2(600, 150),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _replacementReadout.AddThemeFontSizeOverride("font_size", 20);
        _replacementReadout.AddThemeColorOverride("font_color", new Color(0.86f, 0.91f, 0.96f));
        _replacementPanel.AddChild(_replacementReadout);

        _createReplacementButton = AddCommandButton(
            _replacementPanel,
            "CREATE REPLACEMENT INCARNATION",
            new Vector2(430, 390),
            new Vector2(420, 62),
            CreateReplacement);
        _createReplacementButton.AddThemeFontSizeOverride("font_size", 20);

        _replacementSaveButton = AddCommandButton(
            _replacementPanel,
            "SAVE CHRONICLE",
            new Vector2(430, 478),
            new Vector2(202, 46),
            SaveChronicle);
        _replacementLoadButton = AddCommandButton(
            _replacementPanel,
            "LOAD CHRONICLE",
            new Vector2(648, 478),
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
            Size = new Vector2(1280, 720),
            Color = new Color(0.018f, 0.026f, 0.045f, 0.97f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 100,
        };

        var title = new Label
        {
            Position = new Vector2(390, 150),
            Size = new Vector2(500, 54),
            Text = "THE FIRST HORIZON",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 34);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        _openingPanel.AddChild(title);

        var prompt = new Label
        {
            Position = new Vector2(390, 240),
            Size = new Vector2(500, 110),
            Text = "The sky has no road.\nWhere do you intend to go?",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        prompt.AddThemeFontSizeOverride("font_size", 24);
        prompt.AddThemeColorOverride("font_color", new Color(0.86f, 0.91f, 0.96f));
        _openingPanel.AddChild(prompt);

        _upButton = new Button
        {
            Name = "ChooseUpIntent",
            Position = new Vector2(520, 390),
            Size = new Vector2(240, 68),
            Text = "UP",
            FocusMode = Control.FocusModeEnum.None,
        };
        _upButton.AddThemeFontSizeOverride("font_size", 26);
        _upButton.Pressed += ChooseUpIntent;
        _openingPanel.AddChild(_upButton);

        AddChild(_openingPanel);
    }

    private void ChooseUpIntent()
    {
        _simulation.Apply(new ChooseUpIntent());
        _lastAnswerStatus = "THE CHRONICLE ANSWERS: FLY";
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

    private void ConfigureFirstSlot(ChronicleNoun? noun)
    {
        var result = _simulation.Apply(
            new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, noun));
        _targetingSlot = null;
        _lastCommandStatus = result.Message;
    }

    private void ClearFirstSlot()
    {
        var result = _simulation.Apply(new ClearLoadoutSlot(0));
        _targetingSlot = null;
        _lastCommandStatus = result.Message;
    }

    private void StudySkyStone()
    {
        var alreadyLearned = _simulation.State.Codex.HasStone;
        _simulation.Apply(new StudySkyStone());
        if (alreadyLearned)
        {
            _lastAnswerStatus = "The Codex already keeps Stone.";
        }
        else if (_simulation.State.Study.IsStudyingBell)
        {
            _lastAnswerStatus = "Studying the Bell's sky-stone clapper.";
        }
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
        Action action)
    {
        var button = new Button
        {
            Text = text,
            Position = position,
            Size = size,
            FocusMode = Control.FocusModeEnum.None,
        };
        button.AddThemeFontSizeOverride("font_size", 16);
        button.Pressed += () =>
        {
            action();
            RefreshPresentation();
        };
        parent.AddChild(button);
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
            _lastCommandStatus = string.Empty;
            _lastAnswerStatus = _simulation.State.Codex.HasFly ? "Codex Verb: Fly" : string.Empty;
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
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(ChronicleSaveCodec.Serialize(_simulation.State));
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
        if (!hasLivingIncarnation || !atBell)
        {
            _deathConfirmationArmed = false;
        }

        var worldChanged = !_hasRenderedWorld || state.Seed != _renderedSeed || state.Address != _renderedAddress;
        IReadOnlyList<WorldAddress> highlightedTargets = _targetingSlot is { } targetingSlot
            ? _simulation.ValidTargetsForSlot(targetingSlot)
            : [];

        if (string.Equals(state.Address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal))
        {
            _surfacePatchView.Visible = true;
            _skyStratumView.Visible = false;

            if (worldChanged)
            {
                _surfacePatchView.SetPatch(SurfacePatch.Generate(state));
            }
        }
        else if (string.Equals(state.Address.Stratum, SkyStratum.StratumName, StringComparison.Ordinal))
        {
            _surfacePatchView.Visible = false;
            _skyStratumView.Visible = true;

            if (worldChanged)
            {
                _sky = SkyStratum.Generate(state);
                _skyStratumView.SetSky(_sky, state.Address);
            }
        }

        if (worldChanged)
        {
            _renderedSeed = state.Seed;
            _renderedAddress = state.Address;
            _hasRenderedWorld = true;
        }

        _surfacePatchView.SetSubjects(state.LooseStoneAddress, highlightedTargets);
        _skyStratumView.SetSubjects(state.LooseStoneAddress, highlightedTargets);

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
        }

        var firstSlot = state.ActiveLoadout[0];
        _loadoutReadout.Text = $"LOADOUT SLOT 1: {firstSlot.DisplayName}";
        _equipFlyButton.Disabled =
            !hasLivingIncarnation ||
            !state.Codex.HasFly ||
            firstSlot.IsIntrinsicFly;
        _fitStoneButton.Disabled =
            !hasLivingIncarnation ||
            !state.Codex.HasFly ||
            !state.Codex.HasStone ||
            firstSlot.IsFlyStone;
        _clearFirstSlotButton.Disabled = !hasLivingIncarnation || firstSlot.IsEmpty;

        _pauseButton.Disabled = !hasLivingIncarnation;
        _slowButton.Disabled = !hasLivingIncarnation;
        _normalButton.Disabled = !hasLivingIncarnation;
        _fastButton.Disabled = !hasLivingIncarnation;

        _studyButton.Disabled =
            !hasLivingIncarnation ||
            !atBell ||
            state.Study.IsStudyingBell;
        _studyButton.Text = !atBell
            ? "STUDY AT BELL"
            : state.Codex.HasStone
                ? "STUDY AGAIN"
                : state.Study.IsStudyingBell
                    ? "STUDYING"
                    : "STUDY SKY-STONE";

        _ringBellButton.Disabled = !hasLivingIncarnation || !atBell;
        _ringBellButton.Text = _deathConfirmationArmed
            ? "CONFIRM DEATH"
            : atBell
                ? "END THIS BODY"
                : "RING AT BELL";

        _readout.Text =
            $"INCARNATION #{state.IncarnationId} — {LifeDisplayName(state.IncarnationLife)}\n" +
            $"Seed: {state.Seed}\n" +
            $"Tick: {state.Tick}\n" +
            $"Address: {state.Address}\n" +
            $"Clock: {state.Speed}";

        _codexReadout.Text =
            $"CODEX\n" +
            $"Verbs: {(state.Codex.HasFly ? "Fly" : "—")}\n" +
            $"Nouns: {(state.Codex.HasStone ? "Stone" : "—")}\n" +
            $"Sky-stone clapper: {StudyProgressText(state)}";

        _replacementReadout.Text =
            $"Incarnation #{state.IncarnationId} is gone.\n" +
            $"Tick {state.Tick} is held. Time is not advancing.\n" +
            $"Codex kept: {(state.Codex.HasFly ? "Fly" : "—")}, {(state.Codex.HasStone ? "Stone" : "—")}\n" +
            $"The changed Chronicle remains.";

        _guidanceReadout.Text = GuidanceText(state);

        var landmarkText = CurrentLandmarkText(state);
        _statusReadout.Text = string.IsNullOrWhiteSpace(landmarkText)
            ? string.Join(
                "\n",
                new[] { _lastCommandStatus, _lastAnswerStatus, _lastSaveLoadStatus }
                    .Where(text => !string.IsNullOrWhiteSpace(text)))
            : $"{landmarkText}\n" +
              FirstNonEmpty(_lastCommandStatus, _lastAnswerStatus, _lastSaveLoadStatus);
    }

    private string CurrentLandmarkText(ChronicleState state)
    {
        if (_sky is null || !_sky.Contains(state.Address))
        {
            return string.Empty;
        }

        return _sky.TileAt(state.Address).Terrain == SkyTerrain.Landmark
            ? $"{SkyStratum.LandmarkName}\n{SkyStratum.LandmarkArrivalLine}\nSky-stone clapper: STUDY to understand."
            : string.Empty;
    }

    private static string StudyProgressText(ChronicleState state)
    {
        if (state.Codex.HasStone)
        {
            return "[■■■■] KEPT";
        }

        var completedSegments = state.Study.StoneUnderstanding / 4;
        var segments =
            new string('■', completedSegments) +
            new string('□', 4 - completedSegments);
        return $"[{segments}] {state.Study.StoneUnderstanding}/{StudyState.StoneUnderstandingRequired}";
    }

    private string GuidanceText(ChronicleState state)
    {
        if (!state.HasLivingIncarnation)
        {
            return "The body ended. The Chronicle and Codex remain; choose when to create the next Incarnation.";
        }

        if (_targetingSlot is { } targetingSlot)
        {
            return _simulation.ValidTargetsForSlot(targetingSlot).Count > 0
                ? "TARGET FLY[STONE]: choose the highlighted loose Stone with a direction."
                : "TARGET FLY[STONE]: no loose Stone is adjacent. Press the slot again to cancel.";
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

        return string.Equals(state.Address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal)
            ? "Fly to the sky, then reach The Bell at sky (0, -4) to study its clapper."
            : "The Bell That Fell Up is at sky (0, -4). Its clapper can be studied there.";
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string LifeDisplayName(IncarnationLifeState life) => life switch
    {
        IncarnationLifeState.Alive => "ALIVE",
        IncarnationLifeState.AwaitingReplacement => "ENDED",
        _ => "?",
    };

    private void RunSlice2CAcceptance()
    {
        try
        {
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
            VerifyAcceptance(
                _statusReadout.Text.Contains(SkyStratum.LandmarkName, StringComparison.Ordinal) &&
                _statusReadout.Text.Contains(SkyStratum.LandmarkArrivalLine, StringComparison.Ordinal),
                "The Bell name and arrival line must be visible on arrival.");
            VerifyAcceptance(
                _studyButton.Visible &&
                !_studyButton.Disabled &&
                _studyButton.Text == "STUDY SKY-STONE",
                "The Bell must expose its artifact as a Study action without revealing the learned word.");
            VerifyLabelFits(_readout, "Chronicle readout");
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
                _simulation.State.Study.IsStudyingBell,
                "The Study button must translate to an active Core Study.");
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
            Press(_directionButtons[2]);
            VerifyAcceptance(
                !_simulation.State.Study.IsStudyingBell &&
                _simulation.State.Study.StoneUnderstanding == savedState.Study.StoneUnderstanding,
                "Leaving the Bell must stop Study without losing understanding.");
            Press(_loadButton);
            VerifyAcceptance(_simulation.State == savedState, "Load must restore exact partial Study state.");

            for (var pulse = 0; pulse < 7; pulse++)
            {
                _Process(ClockPulseSeconds);
            }

            VerifyAcceptance(
                _simulation.State.Codex.HasStone &&
                _simulation.State.Study.StoneUnderstanding == StudyState.StoneUnderstandingRequired &&
                !_simulation.State.Study.IsStudyingBell,
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
        var minimum = label.GetMinimumSize();
        VerifyAcceptance(
            minimum.X <= label.Size.X && minimum.Y <= label.Size.Y,
            $"{name} content must fit its assigned bounds.");
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

    private void LogState(string prefix)
    {
        var state = _simulation.State;
        GD.Print(
            $"{prefix} seed={state.Seed} tick={state.Tick} " +
            $"address={state.Address.Stratum}:{state.Address.X},{state.Address.Y} " +
            $"speed={state.Speed} intent={state.Intent} " +
            $"codex=Fly:{state.Codex.HasFly},Stone:{state.Codex.HasStone} " +
            $"study={state.Study.StoneUnderstanding}/{StudyState.StoneUnderstandingRequired} " +
            $"incarnation={state.IncarnationId}:{state.IncarnationLife} " +
            $"loadout={state.ActiveLoadout[0].DisplayName} " +
            $"stone={AddressLogText(state.LooseStoneAddress)}");
    }

    private static string AddressLogText(WorldAddress? address) => address is { } value
        ? $"{value.Stratum}:{value.X},{value.Y}"
        : "missing";
}
