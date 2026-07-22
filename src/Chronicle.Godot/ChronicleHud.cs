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
    bool AwaitingReplacement,
    string ReplacementStatus,
    bool IsPaused);

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
    private readonly Label _targetHealthText = new();
    private readonly Label _targetFacts = new();
    private readonly Label _targetOutcome = new();
    private readonly List<Label> _consequenceRows = [];
    private readonly Label _forecast = new();
    private readonly Label _messages = new();
    private readonly Label _replacement = new();
    private readonly Label _pauseBadge = new();
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

    public event Action<ChronicleCommand>? CommandRequested;
    public event Action<WorldAddress>? TargetRequested;
    public event Action? SaveRequested;
    public event Action? LoadRequested;
    public event Action? ReplacementRequested;

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
        ConfigureLabel(_clock, new Vector2(744, 4), new Vector2(296, 26), 18, Ochre);
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

        _pauseBadgeBackground.Position = new Vector2(744, 2);
        _pauseBadgeBackground.Size = new Vector2(296, 40);
        _pauseBadgeBackground.Color = new Color(0.18f, 0.12f, 0.045f, 0.96f);
        _pauseBadgeBackground.MouseFilter = MouseFilterEnum.Ignore;
        _pauseBadgeBackground.ZIndex = 7;
        AddChild(_pauseBadgeBackground);
        ConfigureLabel(_pauseBadge, new Vector2(744, 26), new Vector2(296, 14), 10, Ochre);
        _pauseBadge.Text = "SPACE RESUMES";
        _pauseBadge.HorizontalAlignment = HorizontalAlignment.Center;
        _pauseBadge.VerticalAlignment = VerticalAlignment.Center;
        _pauseBadge.ZIndex = 9;
        ConfigurePauseFrame();

        AddSectionPanel(new Vector2(8, 50), new Vector2(520, 212), raised: true);
        ConfigureLabel(_powerHeading, new Vector2(18, 56), new Vector2(500, 24), 17, Ochre);
        ConfigureLabel(_powerStatus, new Vector2(18, 84), new Vector2(500, 126), 12, Bone);
        _powerStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ConfigureLabel(_powerCapacity, new Vector2(18, 216), new Vector2(500, 36), 12, Verdigris);
        _powerCapacity.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ConfigureLabel(_powerDecision, Vector2.Zero, Vector2.Zero, 11, Bone);
        _powerDecision.Visible = false;

        var railX = MapWidth;
        AddSectionPanel(new Vector2(railX + 8, 50), new Vector2(304, 150));
        AddSectionHeading("TARGET · [T] CYCLE", new Vector2(railX + 16, 54));
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

        AddSectionPanel(new Vector2(railX + 8, 208), new Vector2(304, 168), raised: true);
        AddSectionHeading("CONSEQUENCE", new Vector2(railX + 16, 212));
        ConfigureLabel(_targetOutcome, new Vector2(-1000, -1000), Vector2.One, 1, Bone);
        _targetOutcome.Visible = false;
        var consequenceColors = new[] { Bone, Ochre, Ember, Bone, Verdigris };
        for (var index = 0; index < 5; index++)
        {
            var row = new Label();
            ConfigureLabel(row, new Vector2(railX + 18, 240 + index * 25), new Vector2(284, 24), index == 0 ? 15 : 14, consequenceColors[index]);
            row.AutowrapMode = TextServer.AutowrapMode.Off;
            row.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _consequenceRows.Add(row);
        }

        AddSectionPanel(new Vector2(railX + 8, 384), new Vector2(304, 152));
        AddSectionHeading("FORECAST · NEXT FOUR", new Vector2(railX + 16, 388));
        ConfigureLabel(_forecast, new Vector2(railX + 28, 416), new Vector2(276, 108), 13, Bone);
        _forecast.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddHeartbeatSpine(new Vector2(railX + 18, 418), 100);

        AddSectionPanel(new Vector2(railX + 8, 544), new Vector2(304, 348));
        AddSectionHeading("MESSAGE LOG", new Vector2(railX + 16, 548));
        ConfigureLabel(_messages, new Vector2(railX + 16, 576), new Vector2(288, 304), 13, Bone);
        _messages.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        AddRail(new Vector2(8, 808), new Vector2(1012, 88), new Color(0.014f, 0.025f, 0.04f, 0.94f));
        var actionPositions = new[]
        {
            new Vector2(16, 828),
            new Vector2(160, 828),
            new Vector2(924, 808),
            new Vector2(880, 852),
            new Vector2(924, 852),
            new Vector2(968, 852),
            new Vector2(304, 828),
            new Vector2(448, 828),
            new Vector2(592, 828),
            new Vector2(736, 828),
        };
        for (var index = 0; index < 10; index++)
        {
            var movement = index is >= 2 and <= 5;
            var button = NewButton(
                actionPositions[index],
                movement ? new Vector2(44, 44) : new Vector2(136, 60),
                "—",
                movement ? 13 : 12);
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
        _targetHeading.Text = snapshot.TargetHeading;
        _targetHeading.AddThemeColorOverride(
            "font_color",
            snapshot.TargetHeading.Contains("MIRE BRUTE", StringComparison.Ordinal) ? Ember : Bone);
        _targetFacts.Text = snapshot.TargetFacts;
        _targetOutcome.Text = snapshot.TargetOutcome;
        PresentConsequence(snapshot.TargetOutcome);
        _forecast.Text = string.Join("\n", snapshot.Forecast.Take(4));
        _messages.Text = string.Join("\n", snapshot.Messages.TakeLast(4));
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
        for (var index = 0; index < _actionButtons.Count; index++)
        {
            var button = _actionButtons[index];
            if (index >= snapshot.Actions.Count)
            {
                button.Text = "—";
                button.Disabled = true;
                continue;
            }

            var action = snapshot.Actions[index];
            button.Name = $"HudAction_{action.Id}";
            button.Text = string.IsNullOrWhiteSpace(action.Detail)
                ? action.Label
                : $"{action.Label}\n{action.Detail}";
            button.Icon = ResolveActionIcon(action.Id);
            button.Disabled = !action.Enabled || action.Command is null;
            var emphasized = action.Label.StartsWith('◆');
            var preventsCleaver = snapshot.TargetOutcome.StartsWith("STATE  ACTIVE", StringComparison.Ordinal) &&
                                  snapshot.TargetOutcome.Contains("PREVENTS  Cleaver strike", StringComparison.Ordinal) &&
                                  action.Id == "weapon";
            if (preventsCleaver)
            {
                button.Text = $"⊠ {button.Text}";
            }
            button.AddThemeColorOverride("font_color", emphasized ? Verdigris : preventsCleaver ? Muted : Bone);
            button.AddThemeStyleboxOverride(
                "normal",
                ButtonStyle(emphasized ? PanelRaised : Panel, emphasized ? Verdigris : preventsCleaver ? Muted : Rule));
            if (action.Command is { } command)
            {
                _commands[button] = command;
            }
        }

        _targets.Clear();
        for (var index = 0; index < _targetButtons.Count; index++)
        {
            var button = _targetButtons[index];
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

    private void EmitCommand(Button button)
    {
        if (_commands.TryGetValue(button, out var command))
        {
            CommandRequested?.Invoke(command);
        }
    }

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

    private void AddSectionPanel(Vector2 position, Vector2 size, bool raised = false)
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
            row.Text = prefixes[index] +
                       (source.StartsWith(labels[index], StringComparison.Ordinal)
                           ? source[labels[index].Length..]
                           : source);
        }
    }

    private void AddSectionHeading(string text, Vector2 position)
    {
        var label = new Label();
        ConfigureLabel(label, position, new Vector2(292, 20), 11, Muted);
        label.Text = text;
        AddDivider(new Vector2(position.X, position.Y + 20), 288);
        AddDivider(new Vector2(position.X, position.Y + 23), 288);
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
            FocusMode = FocusModeEnum.None,
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", quiet ? Muted : Bone);
        button.AddThemeColorOverride("font_hover_color", Bone);
        button.AddThemeColorOverride("font_disabled_color", Muted with { A = 0.48f });
        button.AddThemeStyleboxOverride("normal", ButtonStyle(quiet ? Backdrop : Panel, quiet ? Rule with { A = 0.55f } : Rule));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(PanelRaised, Bone));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(Backdrop, Verdigris, 2));
        AddChild(button);
        button.Size = size;
        return button;
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
