using Chronicle.Core;
using Chronicle.VisualPack;
using Godot;

public sealed record ChronicleHudAction(
    string Id,
    string Label,
    ChronicleCommand? Command,
    bool Enabled,
    string? Detail = null);

public sealed record ChronicleHudTarget(
    string Id,
    string Label,
    WorldAddress Address,
    bool Selected);

public enum ChronicleRailRole
{
    Observation,
    Commitment,
    Consequence,
}

public enum ChronicleRailView
{
    Context,
    Chronicle,
}

public sealed record ChronicleHudSnapshot(
    string IncarnationStatus,
    string ClockStatus,
    string PlaceStatus,
    string TargetHeading,
    string TargetFacts,
    string TargetOutcome,
    IReadOnlyList<string> Forecast,
    IReadOnlyList<string> Messages,
    IReadOnlyList<ChronicleHudTarget> Targets,
    IReadOnlyList<ChronicleHudAction> Actions,
    int IncarnationHitPoints,
    int IncarnationMaximumHitPoints,
    int? TargetHitPoints,
    int? TargetMaximumHitPoints,
    string LoadStatus,
    string EquipmentStatus,
    string PowerHeading,
    string PowerStatus,
    string PowerCapacity,
    string PowerDecision,
    bool ShowsCombatContext,
    bool ShowsAgentContext,
    bool AwaitingReplacement,
    string ReplacementStatus,
    bool IsPaused,
    ChronicleRailRole RailRole = ChronicleRailRole.Observation,
    string? DialogueSpeaker = null,
    string? DialogueRelation = null,
    string? DialogueText = null);

/// <summary>
/// The map-first Goal 6A presentation shell. It displays Core-owned snapshots
/// and emits Core commands; it never derives simulation outcomes.
/// </summary>
public sealed partial class ChronicleHud : Control
{
    public const int CanvasWidth = 1600;
    public const int CanvasHeight = 900;
    public const int TopRailHeight = 44;
    public const int RightRailWidth = 320;
    public const int MapWidth = CanvasWidth - RightRailWidth;
    public const int MapHeight = CanvasHeight;
    public const int MapDisplayCellSize = 40;
    public const int MapColumns = MapWidth / MapDisplayCellSize;
    public const int MapRows = (MapHeight + MapDisplayCellSize - 1) / MapDisplayCellSize;

    private static readonly Color Backdrop = new(0.035f, 0.025f, 0.022f, 0.98f);
    private static readonly Color Panel = new(0.055f, 0.040f, 0.034f, 0.98f);
    private static readonly Color PanelRaised = new(0.085f, 0.060f, 0.048f, 0.98f);
    private static readonly Color Bone = new(0.90f, 0.86f, 0.76f);
    private static readonly Color Muted = new(0.66f, 0.63f, 0.57f);
    private static readonly Color Ember = new(0.96f, 0.39f, 0.22f);
    private static readonly Color Verdigris = new(0.38f, 0.80f, 0.68f);
    private static readonly Color Ochre = new(0.91f, 0.72f, 0.30f);
    private static readonly Color Insight = new(0.43f, 0.70f, 0.92f);
    private static readonly Color People = new(0.56f, 0.82f, 0.66f);
    private static readonly Color Rule = new(0.34f, 0.29f, 0.23f, 0.92f);

    private readonly Label _incarnation = new();
    private readonly Label _incarnationHealthText = new();
    private readonly Label _load = new();
    private readonly Label _clock = new();
    private readonly Label _place = new();
    private readonly Label _equipment = new();
    private readonly Label _powerHeading = new();
    private readonly Label _powerStatus = new();
    private readonly Label _powerCapacity = new();
    private readonly Label _powerDecision = new();
    private readonly Label _targetHeading = new();
    private Label _contextSectionHeading = null!;
    private Label _consequenceSectionHeading = null!;
    private Label _forecastSectionHeading = null!;
    private readonly Label _targetHealthText = new();
    private readonly Label _targetFacts = new();
    private readonly Label _targetOutcome = new();
    private readonly List<Label> _consequenceRows = [];
    private readonly Label _forecast = new();
    private readonly Label _messages = new();
    private readonly Label _replacement = new();
    private readonly Label _pauseBadge = new();
    private readonly Label _railRoleHeading = new();
    private readonly Label _railRoleTitle = new();
    private readonly Label _railRoleBody = new();
    private readonly ColorRect _railRolePanel = new();
    private readonly ColorRect _railActionPanel = new();
    private readonly Button _contextTab = new();
    private readonly Button _chronicleTab = new();
    private readonly ColorRect _dialoguePanel = new();
    private readonly Label _dialogueSpeaker = new();
    private readonly Label _dialogueText = new();
    private readonly Button _soundButton = new();
    private readonly Button _motionButton = new();
    private ColorRect _powerPanel = null!;
    private ColorRect _messagePanel = null!;
    private readonly ColorRect _pauseBadgeBackground = new();
    private readonly ColorRect[] _pauseFrame = [new(), new(), new(), new()];
    private readonly ProgressBar _incarnationHealth = new();
    private readonly ProgressBar _targetHealth = new();
    private readonly TextureRect _weaponIcon = new();
    private readonly TextureRect _armorIcon = new();
    private readonly TextureRect _accessoryIcon = new();
    private readonly List<Button> _targetButtons = [];
    private readonly List<Button> _actionButtons = [];
    private readonly Dictionary<Button, ChronicleCommand> _commands = [];
    private readonly Dictionary<Button, WorldAddress> _targets = [];
    private CompiledVisualPack? _visualPack;
    private ImageTexture? _atlasTexture;
    private bool _soundEnabled = true;
    private bool _reducedMotion;
    private ChronicleRailView _railView = ChronicleRailView.Context;
    private int _messageCount;

    public event Action<ChronicleCommand>? CommandRequested;
    public event Action<WorldAddress>? TargetRequested;
    public event Action? SaveRequested;
    public event Action? LoadRequested;
    public event Action? ReplacementRequested;
    public event Action? CodexRequested;
    public event Action<string>? PresentationActionRequested;
    public event Action<bool>? SoundChanged;
    public event Action<bool>? ReducedMotionChanged;

    public IReadOnlyList<Button> ActionButtons => _actionButtons;
    public IReadOnlyList<Button> TargetButtons => _targetButtons;
    public Label TargetHeading => _targetHeading;
    public Label TargetFacts => _targetFacts;
    public Label TargetOutcome => _targetOutcome;
    public Label ForecastReadout => _forecast;
    public Label MessageReadout => _messages;
    public Label PauseBadge => _pauseBadge;
    public Label IncarnationHealthText => _incarnationHealthText;
    public Label TargetHealthText => _targetHealthText;
    public IReadOnlyList<Label> ConsequenceRows => _consequenceRows;
    public ProgressBar IncarnationHealthBar => _incarnationHealth;
    public ProgressBar TargetHealthBar => _targetHealth;
    public Label PowerHeading => _powerHeading;
    public Label PowerStatus => _powerStatus;
    public Label PowerCapacity => _powerCapacity;
    public Label PowerDecision => _powerDecision;
    public Label ClockReadout => _clock;
    public Label PlaceReadout => _place;
    public Label ReplacementReadout => _replacement;
    public Label RailRoleHeading => _railRoleHeading;
    public Label RailRoleBody => _railRoleBody;
    public ChronicleRailView RailView => _railView;
    public Control ActionStrip => _railActionPanel;
    public Button ContextTab => _contextTab;
    public Button ChronicleTab => _chronicleTab;
    public Control DialoguePanel => _dialoguePanel;
    public bool SoundEnabled => _soundEnabled;
    public bool ReducedMotion => _reducedMotion;

    public void ConfigureVisualPack(CompiledVisualPack visualPack)
    {
        _visualPack = visualPack ?? throw new ArgumentNullException(nameof(visualPack));
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Pass;

        AddRail(new Vector2(0, 0), new Vector2(CanvasWidth, TopRailHeight), Backdrop);
        AddRail(
            new Vector2(MapWidth, TopRailHeight),
            new Vector2(RightRailWidth, CanvasHeight - TopRailHeight),
            Panel);

        ConfigureLabel(_incarnation, new Vector2(16, 8), new Vector2(176, 28), 17, Bone);
        ConfigureHealthBar(_incarnationHealth, new Vector2(196, 14), new Vector2(120, 16), Verdigris);
        ConfigureLabel(_incarnationHealthText, new Vector2(196, 12), new Vector2(120, 20), 12, Bone);
        _incarnationHealthText.HorizontalAlignment = HorizontalAlignment.Center;
        _incarnationHealthText.VerticalAlignment = VerticalAlignment.Center;
        ConfigureLabel(_load, new Vector2(328, 9), new Vector2(88, 24), 14, Bone);
        ConfigureLabel(_equipment, new Vector2(500, 10), new Vector2(240, 22), 12, Muted);
        ConfigureLabel(_clock, new Vector2(744, 4), new Vector2(280, 26), 18, Ochre);
        _clock.HorizontalAlignment = HorizontalAlignment.Center;
        _clock.ZIndex = 8;
        ConfigureLabel(_place, new Vector2(1032, 9), new Vector2(160, 24), 13, Muted);
        _place.HorizontalAlignment = HorizontalAlignment.Right;

        if (_visualPack is not null)
        {
            _atlasTexture = VisualPackGodotAdapter.CreateAtlasTexture(_visualPack);
            ConfigureIcon(_weaponIcon, "glyph.equipment.iron-cleaver", new Vector2(428, 10), "Iron Cleaver");
            ConfigureIcon(_armorIcon, "glyph.equipment.quilted-jack", new Vector2(452, 10), "Quilted Jack");
            ConfigureIcon(_accessoryIcon, "glyph.equipment.copper-ward", new Vector2(476, 10), "Copper Ward");
        }

        var save = NewButton(new Vector2(1200, 6), new Vector2(76, 32), "[F5] SAVE", 11, quiet: true);
        save.Name = "SaveAction";
        save.Pressed += () => SaveRequested?.Invoke();
        var load = NewButton(new Vector2(1284, 6), new Vector2(76, 32), "[F9] LOAD", 11, quiet: true);
        load.Name = "LoadAction";
        load.Pressed += () => LoadRequested?.Invoke();
        ConfigureSessionControl(
            _soundButton,
            new Vector2(1368, 6),
            "SOUND: ON",
            () => SetSoundEnabled(!_soundEnabled));
        ConfigureSessionControl(
            _motionButton,
            new Vector2(1480, 6),
            "MOTION: FULL",
            () => SetReducedMotion(!_reducedMotion));

        _pauseBadgeBackground.Position = new Vector2(744, 2);
        _pauseBadgeBackground.Size = new Vector2(280, 40);
        _pauseBadgeBackground.Color = new Color(0.18f, 0.12f, 0.045f, 0.96f);
        _pauseBadgeBackground.MouseFilter = MouseFilterEnum.Ignore;
        _pauseBadgeBackground.ZIndex = 7;
        AddChild(_pauseBadgeBackground);
        ConfigureLabel(_pauseBadge, new Vector2(744, 25), new Vector2(280, 16), 11, Ochre);
        _pauseBadge.Text = "SPACE RESUMES";
        _pauseBadge.HorizontalAlignment = HorizontalAlignment.Center;
        _pauseBadge.VerticalAlignment = VerticalAlignment.Center;
        _pauseBadge.ZIndex = 9;
        ConfigurePauseFrame();

        _powerPanel = AddSectionPanel(new Vector2(8, 50), new Vector2(520, 212), raised: true);
        ConfigureLabel(_powerHeading, new Vector2(18, 56), new Vector2(500, 24), 17, Ochre);
        ConfigureLabel(_powerStatus, new Vector2(18, 84), new Vector2(500, 126), 12, Bone);
        _powerStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ConfigureLabel(_powerCapacity, new Vector2(18, 216), new Vector2(500, 36), 12, Verdigris);
        _powerCapacity.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ConfigureLabel(_powerDecision, Vector2.Zero, Vector2.Zero, 11, Bone);
        _powerDecision.Visible = false;

        var railX = MapWidth;
        AddSectionPanel(new Vector2(railX + 8, 50), new Vector2(304, 150));
        _contextSectionHeading = AddSectionHeading("TARGET · [T] CYCLE", new Vector2(railX + 16, 54));
        ConfigureLabel(_targetHeading, new Vector2(railX + 16, 78), new Vector2(288, 26), 19, Bone);
        ConfigureHealthBar(_targetHealth, new Vector2(railX + 16, 106), new Vector2(180, 18), Ember);
        ConfigureLabel(_targetHealthText, new Vector2(railX + 16, 104), new Vector2(180, 22), 12, Bone);
        _targetHealthText.HorizontalAlignment = HorizontalAlignment.Center;
        _targetHealthText.VerticalAlignment = VerticalAlignment.Center;
        ConfigureLabel(_targetFacts, new Vector2(railX + 16, 130), new Vector2(288, 42), 13, Bone);
        _targetFacts.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        for (var index = 0; index < 2; index++)
        {
            var button = NewButton(
                new Vector2(railX + 16 + index * 148, 174),
                new Vector2(140, 22),
                "TARGET —",
                11);
            button.Pressed += () => EmitTarget(button);
            _targetButtons.Add(button);
        }

        AddSectionPanel(new Vector2(railX + 8, 208), new Vector2(304, 218), raised: true);
        _consequenceSectionHeading = AddSectionHeading("CONSEQUENCE", new Vector2(railX + 16, 212));
        ConfigureLabel(_targetOutcome, new Vector2(-1000, -1000), Vector2.One, 1, Bone);
        _targetOutcome.Visible = false;
        var consequenceColors = new[] { Bone, Ochre, Ember, Bone, Verdigris };
        for (var index = 0; index < 5; index++)
        {
            var row = new Label();

            var height = index == 0 ? 52 : 32;
            var top = index == 0 ? 238 : 290 + (index - 1) * 32;
            ConfigureLabel(row, new Vector2(railX + 18, top), new Vector2(284, height), index == 0 ? 15 : 13, consequenceColors[index]);
            row.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            row.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            _consequenceRows.Add(row);
        }

        AddSectionPanel(new Vector2(railX + 8, 434), new Vector2(304, 148));
        _forecastSectionHeading = AddSectionHeading("FORECAST · NEXT FOUR", new Vector2(railX + 16, 438));
        ConfigureLabel(_forecast, new Vector2(railX + 28, 466), new Vector2(276, 104), 13, Bone);
        _forecast.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddHeartbeatSpine(new Vector2(railX + 18, 468), 96);

        _messagePanel = AddSectionPanel(new Vector2(railX + 8, 94), new Vector2(304, 798));
        ConfigureLabel(_messages, new Vector2(railX + 20, 118), new Vector2(280, 748), 14, Bone);
        _messages.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _messagePanel.Visible = false;
        _messages.Visible = false;

        ConfigureRailTab(
            _contextTab,
            new Vector2(railX + 8, 50),
            "CONTEXT",
            ChronicleRailView.Context);
        ConfigureRailTab(
            _chronicleTab,
            new Vector2(railX + 162, 50),
            "CHRONICLE",
            ChronicleRailView.Chronicle);

        _railRolePanel.Position = new Vector2(railX + 8, 94);
        _railRolePanel.Size = new Vector2(304, 798);
        _railRolePanel.Color = Panel with { A = 1f };
        _railRolePanel.MouseFilter = MouseFilterEnum.Stop;
        _railRolePanel.ZIndex = 3;
        AddChild(_railRolePanel);
        ConfigureLabel(_railRoleHeading, new Vector2(railX + 20, 112), new Vector2(278, 28), 12, Ochre);
        _railRoleHeading.ZIndex = 4;
        ConfigureLabel(_railRoleTitle, new Vector2(railX + 20, 148), new Vector2(278, 62), 20, Bone);
        _railRoleTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _railRoleTitle.ZIndex = 4;
        ConfigureLabel(_railRoleBody, new Vector2(railX + 20, 222), new Vector2(278, 642), 14, Bone);
        _railRoleBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _railRoleBody.ClipText = true;
        _railRoleBody.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _railRoleBody.ZIndex = 4;
        _targetHealth.Position = new Vector2(railX + 20, 212);
        _targetHealth.Size = new Vector2(180, 18);
        _targetHealth.ZIndex = 4;
        _targetHealthText.Position = new Vector2(railX + 20, 210);
        _targetHealthText.Size = new Vector2(180, 22);
        _targetHealthText.ZIndex = 5;
        _railRoleBody.Position = new Vector2(railX + 20, 244);
        _railRoleBody.Size = new Vector2(278, 620);

        _contextSectionHeading.Visible = false;
        _consequenceSectionHeading.Visible = false;
        _forecastSectionHeading.Visible = false;
        _targetHeading.Visible = false;
        _targetFacts.Visible = false;
        _forecast.Visible = false;
        foreach (var row in _consequenceRows)
        {
            row.Visible = false;
        }

        _railActionPanel.Position = new Vector2(0, 818);
        _railActionPanel.Size = new Vector2(MapWidth, 82);
        _railActionPanel.Color = Backdrop with { A = 0.96f };
        _railActionPanel.MouseFilter = MouseFilterEnum.Stop;
        _railActionPanel.ZIndex = 12;
        AddChild(_railActionPanel);

        for (var index = 0; index < 10; index++)
        {
            var button = NewButton(
                new Vector2(8, 828),
                new Vector2(196, 58),
                "—",
                12);
            button.ZIndex = 13;
            button.Pressed += () => EmitCommand(button);
            _actionButtons.Add(button);
        }

        var replace = NewButton(new Vector2(1088, 824), new Vector2(168, 64), "[R] NEW BODY", 14);
        replace.Pressed += () => ReplacementRequested?.Invoke();
        replace.Name = "ReplacementAction";

        ConfigureLabel(
            _replacement,
            new Vector2(360, 380),
            new Vector2(560, 128),
            22,
            Ochre);
        _replacement.HorizontalAlignment = HorizontalAlignment.Center;
        _replacement.VerticalAlignment = VerticalAlignment.Center;
        _replacement.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _replacement.Visible = false;
        _replacement.ZIndex = 20;

        _dialoguePanel.Position = new Vector2(110, 688);
        _dialoguePanel.Size = new Vector2(1040, 124);
        _dialoguePanel.Color = new Color(0.035f, 0.029f, 0.032f, 0.98f);
        _dialoguePanel.MouseFilter = MouseFilterEnum.Ignore;
        _dialoguePanel.ZIndex = 30;
        AddChild(_dialoguePanel);
        ConfigureLabel(_dialogueSpeaker, new Vector2(136, 704), new Vector2(980, 26), 15, Ochre);
        _dialogueSpeaker.ZIndex = 31;
        ConfigureLabel(_dialogueText, new Vector2(136, 738), new Vector2(980, 58), 17, Bone);
        _dialogueText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _dialogueText.ZIndex = 31;
        _dialoguePanel.Visible = false;
        _dialogueSpeaker.Visible = false;
        _dialogueText.Visible = false;
    }

    public void Present(ChronicleHudSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _incarnation.Text = snapshot.IncarnationStatus;
        _incarnationHealthText.Text = $"{snapshot.IncarnationHitPoints}/{snapshot.IncarnationMaximumHitPoints}";
        _load.Text = snapshot.LoadStatus;
        _clock.Text = snapshot.ClockStatus;
        _place.Text = snapshot.PlaceStatus;
        _equipment.Text = snapshot.EquipmentStatus;
        _powerHeading.Text = snapshot.PowerHeading;
        _powerStatus.Text = snapshot.PowerStatus;
        _powerCapacity.Text = snapshot.PowerCapacity;
        _powerDecision.Text = snapshot.PowerDecision;
        _powerPanel.Visible = false;
        _powerHeading.Visible = false;
        _powerStatus.Visible = false;
        _powerCapacity.Visible = false;
        _contextSectionHeading.Text = snapshot.ShowsAgentContext
            ? "AGENT · IDENTITY AND NEED"
            : snapshot.ShowsCombatContext
                ? "TARGET · [T] CYCLE"
                : "MATERIAL STATE";
        _consequenceSectionHeading.Text = snapshot.ShowsAgentContext
            ? "WELCOME DECISION"
            : snapshot.ShowsCombatContext
                ? "CONSEQUENCE"
                : "DECISION";
        _forecastSectionHeading.Text = snapshot.ShowsAgentContext
            ? "AGENT · NEXT CHANGE"
            : snapshot.ShowsCombatContext
                ? "FORECAST · NEXT FOUR"
                : "NEXT CHANGE";
        _targetHeading.Text = snapshot.TargetHeading;
        _targetHeading.AddThemeColorOverride(
            "font_color",
            snapshot.TargetHeading.Contains("MIRE BRUTE", StringComparison.Ordinal) ? Ember : Bone);
        _targetFacts.Text = snapshot.TargetFacts;
        _targetOutcome.Text = snapshot.TargetOutcome;
        PresentConsequence(snapshot.TargetOutcome);
        _forecast.Text = string.Join("\n", snapshot.Forecast.Take(4));
        _messageCount = Math.Min(snapshot.Messages.Count, 8);
        _messages.Text = string.Join("\n\n", snapshot.Messages.TakeLast(8).Select(ChronicleLine));
        PresentRole(snapshot);
        PresentRailView(snapshot.Messages.Count);
        PresentDialogue(snapshot);
        _replacement.Text = snapshot.ReplacementStatus;
        _replacement.Visible = snapshot.AwaitingReplacement;
        _pauseBadgeBackground.Visible = snapshot.IsPaused && !snapshot.AwaitingReplacement;
        _pauseBadge.Visible = snapshot.IsPaused && !snapshot.AwaitingReplacement;
        foreach (var framePart in _pauseFrame)
        {
            framePart.Visible = snapshot.IsPaused && !snapshot.AwaitingReplacement;
        }
        PresentHealth(
            _incarnationHealth,
            snapshot.IncarnationHitPoints,
            snapshot.IncarnationMaximumHitPoints,
            visible: true);
        PresentHealth(
            _targetHealth,
            snapshot.TargetHitPoints ?? 0,
            snapshot.TargetMaximumHitPoints ?? 1,
            snapshot.TargetHitPoints is not null && snapshot.TargetMaximumHitPoints is not null);
        _targetHealthText.Visible = _targetHealth.Visible;
        _targetHealthText.Text = snapshot.TargetHitPoints is { } targetHp && snapshot.TargetMaximumHitPoints is { } targetMax
            ? $"{targetHp}/{targetMax}"
            : string.Empty;

        _commands.Clear();
        var orderedActions = snapshot.Actions.ToArray();
        for (var index = 0; index < _actionButtons.Count; index++)
        {
            var button = _actionButtons[index];
            if (index >= orderedActions.Length)
            {
                button.Text = "—";
                button.Disabled = true;
                button.Visible = false;
                continue;
            }

            var action = orderedActions[index];
            LayoutActionButton(button, index, orderedActions.Length);
            button.Visible = true;
            button.Name = $"HudAction_{action.Id}";
            button.Text = string.IsNullOrWhiteSpace(action.Detail)
                ? action.Label
                : $"{action.Label}\n{action.Detail}";
            button.Icon = ResolveActionIcon(action.Id);
            button.Disabled = !action.Enabled ||
                              (action.Command is null && !IsPresentationAction(action.Id));
            var emphasized = action.Label.StartsWith('◆');
            var preventsCleaver = snapshot.TargetOutcome.StartsWith("STATE  ACTIVE", StringComparison.Ordinal) &&
                                  snapshot.TargetOutcome.Contains("PREVENTS  Cleaver strike", StringComparison.Ordinal) &&
                                  action.Id == "weapon";
            if (preventsCleaver)
            {
                button.Text = $"⊠ {button.Text}";
            }
            var actionColor = ActionColor(action.Id);
            button.AddThemeColorOverride(
                "font_color",
                emphasized ? Verdigris : preventsCleaver ? Muted : actionColor);
            button.AddThemeColorOverride("font_disabled_color", Muted with { A = 0.86f });
            button.AddThemeStyleboxOverride(
                "normal",
                ButtonStyle(
                    emphasized ? PanelRaised : Panel,
                    emphasized ? Verdigris : preventsCleaver ? Muted : actionColor with { A = 0.62f }));
            button.AddThemeStyleboxOverride("disabled", ButtonStyle(Backdrop, Rule with { A = 0.58f }));
            if (action.Command is { } command)
            {
                _commands[button] = command;
            }
        }

        _targets.Clear();
        for (var index = 0; index < _targetButtons.Count; index++)
        {
            var button = _targetButtons[index];
            var legacyFixture = snapshot.Actions.Any(action =>
                action.Id is "attune-quickly" or "attune-lasting" or "attune-combined");
            button.Visible = snapshot.ShowsCombatContext && legacyFixture;
            if (index >= snapshot.Targets.Count)
            {
                button.Text = "TARGET —";
                button.Disabled = true;
                continue;
            }

            var target = snapshot.Targets[index];
            button.Name = $"HudTarget_{target.Id}";
            button.Text = target.Selected ? $"◆ {target.Label}" : target.Label;
            button.Disabled = false;
            button.AddThemeColorOverride("font_color", target.Selected ? Bone : Muted);
            button.AddThemeStyleboxOverride(
                "normal",
                ButtonStyle(target.Selected ? PanelRaised : Panel, target.Selected ? Bone : Rule, target.Selected ? 2 : 1));
            _targets[button] = target.Address;
        }

        var replacementButton = GetNode<Button>("ReplacementAction");
        replacementButton.Disabled = !snapshot.AwaitingReplacement;
        replacementButton.Visible = snapshot.AwaitingReplacement;
    }

    public void SetSoundEnabled(bool enabled)
    {
        _soundEnabled = enabled;
        _soundButton.Text = enabled ? "SOUND: ON" : "SOUND: OFF";
        SoundChanged?.Invoke(enabled);
    }

    public void SetReducedMotion(bool reduced)
    {
        _reducedMotion = reduced;
        _motionButton.Text = reduced ? "MOTION: REDUCED" : "MOTION: FULL";
        ReducedMotionChanged?.Invoke(reduced);
    }

    private void PresentRole(ChronicleHudSnapshot snapshot)
    {
        _railRoleHeading.Text = snapshot.RailRole.ToString().ToUpperInvariant();
        _railRoleTitle.Text = snapshot.TargetHeading;
        _railRoleTitle.AddThemeColorOverride("font_color", ContextColor(snapshot));
        var consequence = HumanizeOutcome(snapshot.TargetOutcome);
        var forecast = string.Join("\n", snapshot.Forecast.Take(2));
        _railRoleBody.Text = snapshot.RailRole switch
        {
            ChronicleRailRole.Commitment =>
                JoinBlocks(TakeLines(consequence, 5), forecast),
            ChronicleRailRole.Consequence =>
                JoinBlocks(
                    TakeLines(snapshot.Messages.LastOrDefault() ?? consequence, 2),
                    TakeLines(consequence, 4),
                    forecast),
            _ => JoinBlocks(
                TakeLines(snapshot.TargetFacts, 5),
                TakeLines(consequence, 3),
                forecast),
        };
    }

    private void PresentRailView(int messageCount)
    {
        var context = _railView == ChronicleRailView.Context;
        _railRolePanel.Visible = context;
        _railRoleHeading.Visible = context;
        _railRoleTitle.Visible = context;
        _railRoleBody.Visible = context;
        _messagePanel.Visible = !context;
        _messages.Visible = !context;
        _contextTab.Text = context ? "◆ CONTEXT" : "CONTEXT";
        _chronicleTab.Text = context
            ? $"CHRONICLE · {Math.Min(messageCount, 8)}"
            : $"◆ CHRONICLE · {Math.Min(messageCount, 8)}";
        StyleRailTab(_contextTab, context);
        StyleRailTab(_chronicleTab, !context);
    }

    private void PresentDialogue(ChronicleHudSnapshot snapshot)
    {
        var visible = !string.IsNullOrWhiteSpace(snapshot.DialogueText);
        _dialoguePanel.Visible = visible;
        _dialogueSpeaker.Visible = visible;
        _dialogueText.Visible = visible;
        _dialogueSpeaker.Text = visible
            ? $"{snapshot.DialogueSpeaker} · {snapshot.DialogueRelation}"
            : string.Empty;
        _dialogueText.Text = snapshot.DialogueText ?? string.Empty;
    }

    private static string HumanizeOutcome(string outcome)
    {
        var labels = new[] { "STATE  ", "WHEN  ", "INTERRUPTS  ", "PREVENTS  ", "RECOVERY  " };
        return string.Join(
            "\n",
            outcome.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => labels.FirstOrDefault(line.StartsWith) is { } label
                    ? line[label.Length..]
                    : line));
    }

    private static string JoinBlocks(params string[] blocks) =>
        string.Join(
            "\n\n",
            blocks.Where(block => !string.IsNullOrWhiteSpace(block)).Distinct());

    private static string TakeLines(string text, int count) =>
        string.Join(
            '\n',
            text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(count));

    private void EmitCommand(Button button)
    {
        var actionId = button.Name.ToString().Replace("HudAction_", string.Empty, StringComparison.Ordinal);
        if (actionId == "codex")
        {
            CodexRequested?.Invoke();
            return;
        }

        if (IsPresentationAction(actionId))
        {
            PresentationActionRequested?.Invoke(actionId);
            return;
        }

        if (_commands.TryGetValue(button, out var command))
        {
            CommandRequested?.Invoke(command);
        }
    }

    private static bool IsPresentationAction(string actionId) =>
        actionId is "codex" or "inspect" or "inspect-close" or "look-pin";

    private static void LayoutActionButton(Button button, int index, int actionCount)
    {
        var visibleCount = Math.Clamp(actionCount, 1, 8);
        const float gap = 6f;
        var width = (MapWidth - 16f - gap * (visibleCount - 1)) / visibleCount;
        button.Position = new Vector2(8f + index * (width + gap), 828);
        button.Size = new Vector2(width, 58);
    }

    public void FocusAction(int delta)
    {
        var visible = _actionButtons.Where(button => button.Visible && !button.Disabled).ToArray();
        if (visible.Length == 0)
        {
            return;
        }

        var focused = Array.FindIndex(visible, button => button.HasFocus());
        var next = focused < 0
            ? delta < 0 ? visible.Length - 1 : 0
            : (focused + delta % visible.Length + visible.Length) % visible.Length;
        visible[next].GrabFocus();
    }

    private void ConfigureRailTab(
        Button button,
        Vector2 position,
        string text,
        ChronicleRailView view)
    {
        button.Position = position;
        button.Size = new Vector2(146, 38);
        button.Name = view == ChronicleRailView.Context ? "ContextTab" : "ChronicleTab";
        button.Text = text;
        button.FocusMode = FocusModeEnum.All;
        button.ZIndex = 5;
        button.Pressed += () =>
        {
            _railView = view;
            PresentRailView(_messageCount);
        };
        AddChild(button);
        StyleRailTab(button, view == _railView);
    }

    private static void StyleRailTab(Button button, bool selected)
    {
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeColorOverride("font_color", selected ? Bone : Muted);
        button.AddThemeStyleboxOverride(
            "normal",
            ButtonStyle(selected ? PanelRaised : Backdrop, selected ? Ochre : Rule));
    }

    private static string ChronicleLine(string line) =>
        line.StartsWith('◆') ? line : $"· {line}";

    private static Color ContextColor(ChronicleHudSnapshot snapshot)
    {
        if (snapshot.ShowsCombatContext)
        {
            return Ember;
        }

        if (snapshot.ShowsAgentContext)
        {
            return People;
        }

        if (snapshot.PowerHeading.Contains("LOOK", StringComparison.Ordinal))
        {
            return Insight;
        }

        return Ochre;
    }

    private static Color ActionColor(string actionId) =>
        actionId switch
        {
            "burn" => Ember,
            "weapon" => Bone,
            "power-primary" or "power-secondary" => Ochre,
            "agent-primary" or "agent-secondary" or
                "directive-rest" or "directive-danger" => People,
            "codex" or "inspect" or "inspect-close" or "look-pin" => Insight,
            "clock" => Verdigris,
            _ => Bone,
        };

    private void EmitTarget(Button button)
    {
        if (_targets.TryGetValue(button, out var address))
        {
            TargetRequested?.Invoke(address);
        }
    }

    private Texture2D? ResolveActionIcon(string actionId)
    {
        var visualId = actionId switch
        {
            "attune-quickly" => "glyph.modifier.quickly",
            "attune-lasting" => "glyph.modifier.lasting",
            "burn" => "glyph.word.burn",
            "weapon" => "glyph.equipment.iron-cleaver",
            _ => null,
        };
        return visualId is null || _visualPack is null || _atlasTexture is null
            ? null
            : VisualPackGodotAdapter.CreateRegionTexture(_atlasTexture, _visualPack.Resolve(visualId));
    }

    private void ConfigureIcon(TextureRect icon, string visualId, Vector2 position, string tooltip)
    {
        icon.Position = position;
        icon.Size = new Vector2(20, 20);
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.MouseFilter = MouseFilterEnum.Stop;
        icon.TooltipText = tooltip;
        icon.Texture = VisualPackGodotAdapter.CreateRegionTexture(
            _atlasTexture!,
            _visualPack!.Resolve(visualId));
        AddChild(icon);
    }

    private void ConfigureHealthBar(ProgressBar bar, Vector2 position, Vector2 size, Color fill)
    {
        bar.Position = position;
        bar.Size = size;
        bar.ShowPercentage = false;
        bar.MinValue = 0;
        bar.AddThemeStyleboxOverride(
            "background",
            new StyleBoxFlat
            {
                BgColor = Backdrop,
                BorderColor = Rule,
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
            });
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = fill });
        bar.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(bar);
    }

    private static void PresentHealth(ProgressBar bar, int current, int maximum, bool visible)
    {
        bar.Visible = visible;
        bar.MaxValue = Math.Max(1, maximum);
        bar.Value = Math.Clamp(current, 0, Math.Max(1, maximum));
    }

    private void AddRail(Vector2 position, Vector2 size, Color color)
    {
        var rail = new ColorRect
        {
            Position = position,
            Size = size,
            Color = color,
            MouseFilter = MouseFilterEnum.Stop,
        };
        rail.ShowBehindParent = true;
        AddChild(rail);
    }

    private void AddDivider(Vector2 position, float width)
    {
        AddChild(new ColorRect
        {
            Position = position,
            Size = new Vector2(width, 1),
            Color = Rule,
            MouseFilter = MouseFilterEnum.Ignore,
        });
    }

    private ColorRect AddSectionPanel(Vector2 position, Vector2 size, bool raised = false)
    {
        var panel = new ColorRect
        {
            Position = position,
            Size = size,
            Color = raised ? PanelRaised : Panel,
            MouseFilter = MouseFilterEnum.Stop,
        };
        AddChild(panel);
        AddDivider(position, size.X);
        AddDivider(new Vector2(position.X, position.Y + size.Y - 1), size.X);
        return panel;
    }

    private void AddHeartbeatSpine(Vector2 position, float height)
    {
        AddChild(new ColorRect
        {
            Position = position,
            Size = new Vector2(1, height),
            Color = Ochre with { A = 0.52f },
            MouseFilter = MouseFilterEnum.Ignore,
        });
    }

    private void ConfigurePauseFrame()
    {
        var rects = new[]
        {
            new Rect2(0, TopRailHeight, MapWidth, 2),
            new Rect2(0, CanvasHeight - 2, MapWidth, 2),
            new Rect2(0, TopRailHeight, 2, CanvasHeight - TopRailHeight),
            new Rect2(MapWidth - 2, TopRailHeight, 2, CanvasHeight - TopRailHeight),
        };
        for (var index = 0; index < _pauseFrame.Length; index++)
        {
            var framePart = _pauseFrame[index];
            framePart.Position = rects[index].Position;
            framePart.Size = rects[index].Size;
            framePart.Color = Ochre;
            framePart.MouseFilter = MouseFilterEnum.Ignore;
            framePart.ZIndex = 10;
            AddChild(framePart);
        }
    }

    private void PresentConsequence(string outcome)
    {
        var lines = outcome.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var prefixes = new[] { "◆ ", "◷ ", "! ", "⊠ ", "↺ " };
        var labels = new[] { "STATE  ", "WHEN  ", "INTERRUPTS  ", "PREVENTS  ", "RECOVERY  " };
        for (var index = 0; index < _consequenceRows.Count; index++)
        {
            var row = _consequenceRows[index];
            var source = index < lines.Length ? lines[index] : string.Empty;
            row.Visible = !string.IsNullOrWhiteSpace(source);
            row.Text = row.Visible
                ? prefixes[index] +
                  (source.StartsWith(labels[index], StringComparison.Ordinal)
                      ? source[labels[index].Length..]
                      : source)
                : string.Empty;
        }
    }

    private Label AddSectionHeading(string text, Vector2 position)
    {
        var label = new Label();
        ConfigureLabel(label, position, new Vector2(292, 20), 11, Muted);
        label.Text = text;
        AddDivider(new Vector2(position.X, position.Y + 20), 288);
        AddDivider(new Vector2(position.X, position.Y + 23), 288);
        return label;
    }

    private void ConfigureLabel(
        Label label,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color)
    {
        label.Position = position;
        label.Size = size;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(label);
    }

    private Button NewButton(
        Vector2 position,
        Vector2 size,
        string text,
        int fontSize,
        bool quiet = false)
    {
        var button = new Button
        {
            Position = position,
            Text = text,
            FocusMode = FocusModeEnum.All,
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", quiet ? Muted : Bone);
        button.AddThemeColorOverride("font_hover_color", Bone);
        button.AddThemeColorOverride("font_disabled_color", Muted with { A = 0.78f });
        button.AddThemeStyleboxOverride("normal", ButtonStyle(quiet ? Backdrop : Panel, Rule));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(PanelRaised, Bone));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(Backdrop, Verdigris, 2));
        button.AddThemeStyleboxOverride("focus", ButtonStyle(PanelRaised, Ochre, 2));
        AddChild(button);
        button.Size = size;
        return button;
    }

    private void ConfigureSessionControl(
        Button button,
        Vector2 position,
        string text,
        Action pressed)
    {
        button.Position = position;
        button.Size = new Vector2(104, 32);
        button.Text = text;
        button.FocusMode = FocusModeEnum.All;
        button.AddThemeFontSizeOverride("font_size", 9);
        button.AddThemeColorOverride("font_color", Muted);
        button.Pressed += pressed;
        AddChild(button);
    }

    private static StyleBoxFlat ButtonStyle(Color background, Color border, int borderWidth = 1) =>
        new()
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0,
        };
}
