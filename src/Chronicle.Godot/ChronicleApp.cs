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
    private const double SlowHeartbeatSeconds = 0.72;

    private static readonly StringName MoveNorthAction = "chronicle_move_north";
    private static readonly StringName MoveSouthAction = "chronicle_move_south";
    private static readonly StringName MoveWestAction = "chronicle_move_west";
    private static readonly StringName MoveEastAction = "chronicle_move_east";
    private static readonly StringName PauseAction = "chronicle_pause";
    private static readonly StringName SlowAction = "chronicle_slow";
    private static readonly StringName SaveAction = "chronicle_save";
    private static readonly StringName LoadAction = "chronicle_load";
    private static readonly StringName BurnAction = "chronicle_burn";
    private static readonly StringName CancelAction = "chronicle_cancel_action";
    private static readonly StringName CycleTargetAction = "chronicle_cycle_target";
    private static readonly StringName WeaponAction = "chronicle_toggle_weapon";
    private static readonly StringName QuicklyAction = "chronicle_attune_quickly";
    private static readonly StringName LastingAction = "chronicle_attune_lasting";
    private static readonly StringName ReplaceAction = "chronicle_replace_incarnation";
    private static readonly StringName CombinedAction = "chronicle_attune_combined";
    private static readonly StringName PowerPrimaryAction = "chronicle_power_primary";
    private static readonly StringName PowerSecondaryAction = "chronicle_power_secondary";

    private ChronicleSimulation _simulation = CreateTestingStart();
    private ChronicleHud _hud = null!;
    private WorldVisualView _map = null!;
    private CompiledVisualPack _visualPack = null!;
    private ColorRect _openingPanel = null!;
    private Button _againstButton = null!;
    private Button _upButton = null!;
    private Button _hereButton = null!;
    private CombatTargetKind _selectedTargetKind = CombatTargetKind.MireBrute;
    private double _heartbeatAccumulator;
    private string _presentationStatus = "Chronicle ready.";
    private string _renderKey = string.Empty;
    private bool _verifyQuickly;
    private bool _verifyQuicklyRestart;
    private bool _verifyLasting;
    private bool _verifyLastingRestart;
    private bool _verifyGoal6BVisuals;

    public override void _Ready()
    {
        var arguments = OS.GetCmdlineUserArgs();
        _verifyQuickly = arguments.Contains("--verify-goal6a-quickly", StringComparer.Ordinal);
        _verifyQuicklyRestart = arguments.Contains("--verify-goal6a-quickly-restart", StringComparer.Ordinal);
        _verifyLasting = arguments.Contains("--verify-goal6a-lasting", StringComparer.Ordinal);
        _verifyLastingRestart = arguments.Contains("--verify-goal6a-lasting-restart", StringComparer.Ordinal);
        _verifyGoal6BVisuals = arguments.Contains("--verify-goal6b-visuals", StringComparer.Ordinal);

        _visualPack = PackagedVisualPackLoader.Load(arguments, 20);
        _map = new WorldVisualView
        {
            Name = "ChronicleMap",
            Position = Vector2.Zero,
            Scale = Vector2.One * ((float)ChronicleHud.MapDisplayCellSize / _visualPack.CellSize),
        };
        AddChild(_map);

        _hud = new ChronicleHud { Name = "ChronicleHud" };
        _hud.ConfigureVisualPack(_visualPack);
        _hud.CommandRequested += Issue;
        _hud.TargetRequested += SelectTarget;
        _hud.SaveRequested += SaveChronicle;
        _hud.LoadRequested += LoadChronicle;
        _hud.ReplacementRequested += () => Issue(new CreateReplacementIncarnation());
        AddChild(_hud);
        BuildOpeningPanel();

        LoadOrCreateChronicle();
        RefreshPresentation(forceMap: true);
        GD.Print("GOAL6A READY");
        GD.Print("GOAL6B READY");

        if (_verifyGoal6BVisuals)
        {
            Callable.From(() => RunAcceptance(RunGoal6BVisualAcceptance)).CallDeferred();
        }
        else if (_verifyQuickly)
        {
            Callable.From(() => RunAcceptance(RunQuicklyAcceptance)).CallDeferred();
        }
        else if (_verifyQuicklyRestart)
        {
            Callable.From(() => RunAcceptance(RunQuicklyRestartAcceptance)).CallDeferred();
        }
        else if (_verifyLasting)
        {
            Callable.From(() => RunAcceptance(RunLastingAcceptance)).CallDeferred();
        }
        else if (_verifyLastingRestart)
        {
            Callable.From(() => RunAcceptance(RunLastingRestartAcceptance)).CallDeferred();
        }
    }

    public override void _Process(double delta)
    {
        if (!_verifyGoal6BVisuals)
        {
            _heartbeatAccumulator += delta;
            while (_heartbeatAccumulator >= SlowHeartbeatSeconds)
            {
                _heartbeatAccumulator -= SlowHeartbeatSeconds;
                _simulation.AdvanceClockPulse();
            }
        }

        RefreshPresentation();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_simulation.State.Intent == OpeningIntent.Unchosen)
        {
            if (@event.IsActionPressed(SlowAction))
            {
                ChooseIntent(new ChooseAgainstIntent(), "The Chronicle answers: Burn.");
            }
            else if (@event.IsActionPressed("chronicle_normal"))
            {
                ChooseIntent(new ChooseUpIntent(), "The Chronicle answers: Fly.");
            }
            else if (@event.IsActionPressed("chronicle_fast"))
            {
                ChooseIntent(new ChooseHereIntent(), "The Chronicle answers: Found.");
            }
            else
            {
                return;
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(SaveAction))
        {
            SaveChronicle();
        }
        else if (@event.IsActionPressed(LoadAction))
        {
            LoadChronicle();
        }
        else if (!_simulation.State.HasLivingIncarnation)
        {
            if (@event.IsActionPressed(ReplaceAction))
            {
                Issue(new CreateReplacementIncarnation());
            }
            else
            {
                return;
            }
        }
        else if (@event.IsActionPressed(MoveNorthAction))
        {
            Issue(new MoveIncarnation(0, -1));
        }
        else if (@event.IsActionPressed(MoveWestAction))
        {
            Issue(new MoveIncarnation(-1, 0));
        }
        else if (@event.IsActionPressed(MoveSouthAction))
        {
            Issue(new MoveIncarnation(0, 1));
        }
        else if (@event.IsActionPressed(MoveEastAction))
        {
            Issue(new MoveIncarnation(1, 0));
        }
        else if (@event.IsActionPressed(PauseAction))
        {
            Issue(new SetChronicleSpeed(
                _simulation.State.Speed == ChronicleSpeed.Paused
                    ? ChronicleSpeed.Slow
                    : ChronicleSpeed.Paused));
        }
        else if (@event.IsActionPressed(SlowAction))
        {
            Issue(new SetChronicleSpeed(ChronicleSpeed.Slow));
        }
        else if (@event.IsActionPressed(QuicklyAction))
        {
            Issue(new AttuneExpression(WordIds.Burn, [WordIds.Quickly]));
        }
        else if (@event.IsActionPressed(CombinedAction))
        {
            Issue(new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting]));
        }
        else if (@event.IsActionPressed(PowerPrimaryAction))
        {
            IssuePowerPrimary();
        }
        else if (@event.IsActionPressed(PowerSecondaryAction))
        {
            IssuePowerSecondary();
        }
        else if (@event.IsActionPressed(LastingAction))
        {
            Issue(new AttuneExpression(WordIds.Burn, [WordIds.Lasting]));
        }
        else if (@event.IsActionPressed(CycleTargetAction))
        {
            CycleTarget();
        }
        else if (@event.IsActionPressed(BurnAction))
        {
            IssueBurn();
        }
        else if (@event.IsActionPressed(WeaponAction))
        {
            IssueWeaponOrPlan();
        }
        else if (@event.IsActionPressed(CancelAction))
        {
            IssueCancelOrSkip();
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
        if (!IsQueuedForDeletion())
        {
            SaveChronicle();
        }
    }

    private void BuildOpeningPanel()
    {
        _openingPanel = new ColorRect
        {
            Name = "OpeningIntent",
            Position = Vector2.Zero,
            Size = new Vector2(ChronicleHud.CanvasWidth, ChronicleHud.CanvasHeight),
            Color = new Color(0.014f, 0.025f, 0.04f, 0.985f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 100,
        };

        var title = new Label
        {
            Position = new Vector2(360, 190),
            Size = new Vector2(880, 62),
            Text = "THE FIRST HORIZON",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.83f, 0.44f));
        _openingPanel.AddChild(title);

        var prompt = new Label
        {
            Position = new Vector2(330, 268),
            Size = new Vector2(940, 104),
            Text = "Choose an emphasis, not a class. Every body enters the same Chronicle.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        prompt.AddThemeFontSizeOverride("font_size", 19);
        prompt.AddThemeColorOverride("font_color", new Color(0.82f, 0.90f, 0.96f));
        _openingPanel.AddChild(prompt);

        _againstButton = OpeningButton(
            "ChooseAgainstIntent",
            "[1] AGAINST — COMBAT\nBURN",
            new Vector2(350, 430),
            () => ChooseIntent(new ChooseAgainstIntent(), "The Chronicle answers: Burn."));
        _upButton = OpeningButton(
            "ChooseUpIntent",
            "[2] UP — EXPLORE\nFLY",
            new Vector2(670, 430),
            () => ChooseIntent(new ChooseUpIntent(), "The Chronicle answers: Fly."));
        _hereButton = OpeningButton(
            "ChooseHereIntent",
            "[3] HERE — BUILD\nFOUND",
            new Vector2(990, 430),
            () => ChooseIntent(new ChooseHereIntent(), "The Chronicle answers: Found."));

        AddChild(_openingPanel);
    }

    private Button OpeningButton(string name, string text, Vector2 position, Action action)
    {
        var button = new Button
        {
            Name = name,
            Position = position,
            Size = new Vector2(260, 76),
            Text = text,
            FocusMode = Control.FocusModeEnum.None,
        };
        button.AddThemeFontSizeOverride("font_size", 18);
        button.Pressed += action;
        _openingPanel.AddChild(button);
        return button;
    }

    private void ChooseIntent(ChronicleCommand command, string answer)
    {
        var result = _simulation.Apply(command);
        _presentationStatus = result.Applied ? answer : result.Message;
        RefreshPresentation(forceMap: true);
    }

    private void Issue(ChronicleCommand command)
    {
        var result = _simulation.Apply(command);
        _presentationStatus = string.IsNullOrWhiteSpace(result.Message)
            ? result.Applied ? AppliedCommandMessage(command) : "Nothing changed."
            : result.Message;
        RefreshPresentation(forceMap: true);
    }

    private void IssueBurn()
    {
        var target = SelectedTarget();
        if (target is null)
        {
            _presentationStatus = "No contextual Target is selected.";
            return;
        }

        Issue(new PrepareBurn(target.Address));
    }

    private void IssueWeaponOrPlan()
    {
        var combat = _simulation.CombatContext;
        var power = _simulation.PowerComesHomeContext;
        Issue(combat.Danger.IsImmediate
            ? new SetWeaponStance(!combat.WeaponStanceActive)
            : new ConfigureEngagementPlan(!combat.EngagementPlan.OpenWithWeaponStance));
    }

    private void IssueCancelOrSkip()
    {
        if (_simulation.PowerComesHomeContext.Commitment is not null)
        {
            Issue(new CancelPowerCommitment());
            return;
        }

        var combat = _simulation.CombatContext;
        Issue(combat.PendingAction is not null || combat.Preparation is not null
            ? new CancelPendingTacticalAction()
            : new SkipRecovery());
    }

    private void IssuePowerPrimary()
    {
        var action = SelectPowerPrimary(_simulation.PowerComesHomeContext);
        if (action is null || PowerCommand(action) is not { } command)
        {
            _presentationStatus = action?.Availability ?? "No contextual Power Comes Home action is available here.";
            RefreshPresentation();
            return;
        }

        Issue(command);
    }

    private void IssuePowerSecondary()
    {
        if (_simulation.PowerComesHomeContext.Commitment is not null)
        {
            Issue(new CancelPowerCommitment());
        }
        else if (_simulation.PowerComesHomeContext.Lode.Disposition == ResonantLodeDisposition.Carried)
        {
            Issue(new SetDownResonantLode());
        }
        else
        {
            _presentationStatus = "No secondary physical action is available.";
            RefreshPresentation();
        }
    }

    private void SelectTarget(WorldAddress address)
    {
        var match = _simulation.CombatContext.Targets.FirstOrDefault(target => target.Address == address);
        if (match is not null)
        {
            _selectedTargetKind = match.Kind;
            _presentationStatus = $"Selected {match.DisplayName}.";
        }

        RefreshPresentation(forceMap: true);
    }

    private void CycleTarget()
    {
        _selectedTargetKind = _selectedTargetKind == CombatTargetKind.MireBrute
            ? CombatTargetKind.Basalt
            : CombatTargetKind.MireBrute;
        _presentationStatus = $"Selected {SelectedTarget()?.DisplayName ?? "Target"}.";
        RefreshPresentation(forceMap: true);
    }

    private TargetPreviewSnapshot? SelectedTarget()
    {
        var targets = _simulation.CombatContext.Targets;
        return targets.FirstOrDefault(target => target.Kind == _selectedTargetKind) ?? targets.FirstOrDefault();
    }

    private void LoadOrCreateChronicle()
    {
        if (Godot.FileAccess.FileExists(SavePath))
        {
            LoadChronicle();
            return;
        }

        _simulation = CreateTestingStart();
        _presentationStatus = "Created a strict v7 Chronicle.";
        SaveChronicle();
    }

    private static ChronicleSimulation CreateTestingStart()
    {
        var simulation = new ChronicleSimulation(ChronicleState.Begin(InitialSeed));
        var started = simulation.Apply(new ChooseHereIntent());
        if (!started.Applied)
        {
            throw new InvalidOperationException("The neutral testing Chronicle could not establish Home.");
        }

        return simulation;
    }

    private void LoadChronicle()
    {
        if (!Godot.FileAccess.FileExists(SavePath))
        {
            _presentationStatus = "Load unavailable: no Chronicle save.";
            return;
        }

        try
        {
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
            _simulation = new ChronicleSimulation(ChronicleSaveCodec.Deserialize(file.GetAsText()));
            _heartbeatAccumulator = 0;
            _renderKey = string.Empty;
            _presentationStatus = "Loaded Chronicle from user://.";
        }
        catch (Exception exception)
        {
            _presentationStatus = $"Load failed: {exception.Message}";
            GD.PushError(_presentationStatus);
        }

        RefreshPresentation(forceMap: true);
    }

    private void SaveChronicle()
    {
        try
        {
            var json = ChronicleSaveCodec.Serialize(_simulation.State);
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(json);
            _presentationStatus = "Saved strict Chronicle v7 to user://.";
        }
        catch (Exception exception)
        {
            _presentationStatus = $"Save failed: {exception.Message}";
            GD.PushError(_presentationStatus);
        }

        RefreshPresentation();
    }

    private void RefreshPresentation(bool forceMap = false)
    {
        var state = _simulation.State;
        var combat = _simulation.CombatContext;
        var power = _simulation.PowerComesHomeContext;
        var selected = SelectedTarget();
        _openingPanel.Visible = state.HasLivingIncarnation && state.Intent == OpeningIntent.Unchosen;
        _map.SetPaused(state.Speed == ChronicleSpeed.Paused);

        var key = string.Join(
            "|",
            state.Seed,
            state.Tick,
            state.Address,
            state.WorldGrammarVersion,
            state.HasLivingIncarnation,
            combat.MireBrute?.Address,
            combat.MireBrute?.HitPoints,
            combat.MireBrute?.IsBurning,
            combat.Scorch?.Address,
            selected?.Address,
            combat.Danger.IsImmediate,
            combat.PendingAction?.DisplayName,
            combat.Preparation?.RemainingTicks,
            combat.Recovery.RemainingTicks,
            power.Lode.Disposition,
            power.Lode.Address,
            power.Resonator?.Phase,
            power.Resonator?.Progress,
            power.Commitment?.CompletedTicks,
            power.Attunement.NextAttunementCapacity);
        if (forceMap || !string.Equals(key, _renderKey, StringComparison.Ordinal))
        {
            RenderMap(state, combat, selected);
            _renderKey = key;
        }

        _hud.Present(BuildHudSnapshot(state, combat, selected));
    }

    private void RenderMap(
        ChronicleState state,
        CombatContextSnapshot combat,
        TargetPreviewSnapshot? selected)
    {
        var visible = VisualViewportBounds.Centered(
            state.Address.X,
            state.Address.Y,
            ChronicleHud.MapColumns,
            ChronicleHud.MapRows);
        var area = WorldArea.Generate(
            state,
            state.Address.Stratum,
            VisualViewportBounds.WithOneCellSemanticHalo(visible));
        var emphases = new List<VisualPresentationEmphasis>();
        if (combat.PendingAction is { Target: { } pendingTarget })
        {
            emphases.Add(new VisualPresentationEmphasis(
                pendingTarget,
                VisualPresentationEmphasisKind.PendingAction));
        }
        else if (combat.PendingAction is not null)
        {
            emphases.Add(new VisualPresentationEmphasis(
                state.Address,
                VisualPresentationEmphasisKind.PendingAction));
        }

        if (combat.Preparation is { } preparation)
        {
            emphases.Add(new VisualPresentationEmphasis(
                preparation.TargetAddressAtPreparation,
                VisualPresentationEmphasisKind.Preparation));
        }

        if (combat.Recovery.RemainingTicks > 0)
        {
            emphases.Add(new VisualPresentationEmphasis(
                state.Address,
                VisualPresentationEmphasisKind.Recovery));
        }

        var plan = VisualGrammar.Compose(new VisualCompositionInput(
            area,
            visible,
            state.Seed,
            _visualPack,
            _visualPack.StyleVersion,
            state.HasLivingIncarnation ? state.Address : null,
            combat.Targets.Select(target => target.Address).ToArray(),
            selected is null ? [] : [selected.Address],
            combat.Danger.IsImmediate && combat.MireBrute is not null
                ? [combat.MireBrute.Address]
                : [],
            emphases));
        _map.SetPlan(_visualPack, plan);
    }

    private ChronicleHudSnapshot BuildHudSnapshot(
        ChronicleState state,
        CombatContextSnapshot combat,
        TargetPreviewSnapshot? selected)
    {
        var targetHeading = selected?.DisplayName.ToUpperInvariant() ?? "NO TARGET";
        var targetFacts = selected is null
            ? "No contextual Target is available."
            : TargetFactsText(selected);
        var targetOutcome = selected is null
            ? string.Empty
            : TargetOutcomeText(selected, combat);
        var power = _simulation.PowerComesHomeContext;
        var forecast = power.Commitment is { } commitment
            ? new[]
            {
                $"H{commitment.NextTick} · {commitment.NextTransition}",
                $"{commitment.CompletedTicks}/{commitment.TotalTicks} · {commitment.RemainingTicks} remaining",
            }
            : combat.Forecast.Count == 0
            ? ["— No meaningful event while the Chronicle is paused."]
            : combat.Forecast
                .Take(4)
                .Select(CompactForecastText)
                .ToArray();
        var messages = combat.RecentResults
            .Select(result => $"H{result.Tick}: {result.Text}")
            .Append(_presentationStatus)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();
        var targets = combat.Targets
            .Select(target => new ChronicleHudTarget(
                target.Kind.ToString().ToLowerInvariant(),
                target.DisplayName.ToUpperInvariant(),
                target.Address,
                target.Kind == _selectedTargetKind))
            .ToArray();
        return new ChronicleHudSnapshot(
            $"INCARNATION #{state.IncarnationId}",
            $"{state.Speed.ToString().ToUpperInvariant()} · HEARTBEAT {state.Tick}" +
            (combat.PendingAction is null ? string.Empty : $" · PENDING {combat.PendingAction.DisplayName.ToUpperInvariant()}"),
            $"{state.Address.Stratum.ToUpperInvariant()}  {state.Address.X},{state.Address.Y}",
            targetHeading,
            targetFacts,
            targetOutcome,
            forecast,
            messages,
            targets,
            BuildActions(state, combat, selected),
            combat.Incarnation.HitPoints,
            combat.Incarnation.MaximumHitPoints,
            selected?.HitPoints,
            selected?.MaximumHitPoints,
            $"LOAD {power.Attunement.CurrentUsedLoad}/{power.Attunement.CapacityAtLastAttunement?.ToString() ?? "—"}",
            $"CLEAVER {combat.Equipment.WeaponDamage}/{combat.Equipment.WeaponCadence} · " +
            $"JACK −{combat.Equipment.ArmorReduction} · WARD +{combat.Equipment.MaximumHitPointBonus}",
            PowerHeadingState(power),
            power.Summary,
            CompactPowerCapacity(power.Attunement),
            string.Empty,
            !state.HasLivingIncarnation,
            !state.HasLivingIncarnation
                ? $"BODY ENDED · CHRONICLE HELD\nBrute and scorch persist. Choose NEW BODY below."
                : string.Empty,
            state.Speed == ChronicleSpeed.Paused);
    }

    private IReadOnlyList<ChronicleHudAction> BuildActions(
        ChronicleState state,
        CombatContextSnapshot combat,
        TargetPreviewSnapshot? selected)
    {
        if (state.WorldGrammarVersion == 5)
        {
            return BuildGoal6BActions(state, combat, selected);
        }

        var alive = state.HasLivingIncarnation && state.Intent != OpeningIntent.Unchosen;
        var safe = alive && !combat.Danger.IsImmediate;
        var canAttune = safe && state.Codex.Contains(WordIds.Burn);
        var cancelOrSkip = combat.PendingAction is not null || combat.Preparation is not null
            ? new ChronicleHudAction(
                "cancel",
                "[C] CANCEL",
                new CancelPendingTacticalAction(),
                state.Speed == ChronicleSpeed.Paused,
                "abandon pending")
            : new ChronicleHudAction(
                "skip-recovery",
                "[C] SKIP",
                new SkipRecovery(),
                combat.Recovery.CanSkipSafely,
                $"REC {combat.Recovery.RemainingTicks}");
        var clockCommand = state.Speed == ChronicleSpeed.Paused
            ? (ChronicleCommand)new SetChronicleSpeed(ChronicleSpeed.Slow)
            : new SetChronicleSpeed(ChronicleSpeed.Paused);
        var weaponCommand = combat.Danger.IsImmediate
            ? (ChronicleCommand)new SetWeaponStance(!combat.WeaponStanceActive)
            : new ConfigureEngagementPlan(!combat.EngagementPlan.OpenWithWeaponStance);

        var quicklyActive = combat.Expression.Modifiers.SequenceEqual([WordIds.Quickly]);
        var lastingActive = combat.Expression.Modifiers.SequenceEqual([WordIds.Lasting]);

        return
        [
            new("attune-quickly", quicklyActive ? "◆ QUICK BURN" : "[Q] QUICK BURN", new AttuneExpression(WordIds.Burn, [WordIds.Quickly]), canAttune, quicklyActive ? $"{combat.Expression.UsedLoad}/{combat.Expression.SharedLoadCapacity} LOAD · {combat.Expression.UsedLinks}/{combat.Expression.LinkCapacity}" : "7/8 LOAD · 2/2"),
            new("attune-lasting", lastingActive ? "◆ LASTING BURN" : "[L] LASTING BURN", new AttuneExpression(WordIds.Burn, [WordIds.Lasting]), canAttune, lastingActive ? $"{combat.Expression.UsedLoad}/{combat.Expression.SharedLoadCapacity} LOAD · {combat.Expression.UsedLinks}/{combat.Expression.LinkCapacity}" : "6/8 LOAD · 2/2"),
            new("move-north", "[W]", new MoveIncarnation(0, -1), alive),
            new("move-west", "[A]", new MoveIncarnation(-1, 0), alive),
            new("move-south", "[S]", new MoveIncarnation(0, 1), alive),
            new("move-east", "[D]", new MoveIncarnation(1, 0), alive),
            new("burn", "[B] BURN", selected is null ? null : new PrepareBurn(selected.Address), alive && selected?.CanBurn == true && combat.Recovery.RemainingTicks == 0, selected is null ? "NO TARGET" : $"{selected.PreparationTicks} PREP · {selected.ConsequenceTicks} BURN"),
            new("weapon", combat.Danger.IsImmediate ? "[V] CLEAVER" : "[V] PLAN", weaponCommand, alive, combat.Danger.IsImmediate ? (combat.WeaponStanceActive ? "ACTIVE" : "LOWERED") : (combat.EngagementPlan.OpenWithWeaponStance ? "OPEN ACTIVE" : "OPEN LOWERED")),
            new("clock", state.Speed == ChronicleSpeed.Paused ? "[SPACE] RESUME" : "[SPACE] PAUSE", clockCommand, alive, "SLOW HEARTBEATS"),
            cancelOrSkip,
        ];
    }

    private IReadOnlyList<ChronicleHudAction> BuildGoal6BActions(
        ChronicleState state,
        CombatContextSnapshot combat,
        TargetPreviewSnapshot? selected)
    {
        var power = _simulation.PowerComesHomeContext;
        var alive = state.HasLivingIncarnation && state.Intent != OpeningIntent.Unchosen;
        var safe = alive && !combat.Danger.IsImmediate;
        var carrying = power.Lode.Disposition == ResonantLodeDisposition.Carried;
        var committed = power.Commitment is not null;
        var primary = SelectPowerPrimary(power);
        var primaryCommand = primary is { Available: true } ? PowerCommand(primary) : null;
        var combinedActive = combat.Expression.Modifiers.SequenceEqual([WordIds.Quickly, WordIds.Lasting]);
        var hasCombinedWords = power.BurnPrimer.IsRead;
        var canRequestAttunement = safe && !carrying && !committed && hasCombinedWords;
        var combinedDetail = combinedActive
            ? "BURN + QUICK + LAST"
            : !hasCombinedWords
                ? "READ BURN PRIMER"
                : !safe
                ? "LEAVE DANGER"
                : carrying
                    ? $"SET DOWN · NEXT {power.Attunement.NextAttunementCapacity}"
                    : committed
                        ? "FINISH OR CANCEL WORK"
                        : power.Attunement.NextAttunementCapacity < 12
                            ? "NEEDS RESONATOR"
                            : "BURN + QUICK + LAST";
        var secondary = committed
            ? new ChronicleHudAction(
                "power-secondary",
                "[X] CANCEL WORK",
                new CancelPowerCommitment(),
                alive,
                $"KEEP {power.Commitment!.CompletedTicks}/{power.Commitment.TotalTicks}")
            : carrying
                ? new ChronicleHudAction(
                    "power-secondary",
                    "[X] SET DOWN",
                    new SetDownResonantLode(),
                    alive,
                    "NO HEARTBEAT")
                : combat.PendingAction is not null || combat.Preparation is not null
                    ? new ChronicleHudAction(
                        "cancel",
                        "[C] CANCEL",
                        new CancelPendingTacticalAction(),
                        state.Speed == ChronicleSpeed.Paused,
                        "ABANDON PENDING")
                    : new ChronicleHudAction(
                        "skip-recovery",
                        "[C] SKIP",
                        new SkipRecovery(),
                        combat.Recovery.CanSkipSafely,
                        $"REC {combat.Recovery.RemainingTicks}");
        var clockCommand = state.Speed == ChronicleSpeed.Paused
            ? (ChronicleCommand)new SetChronicleSpeed(ChronicleSpeed.Slow)
            : new SetChronicleSpeed(ChronicleSpeed.Paused);
        var weaponCommand = combat.Danger.IsImmediate
            ? (ChronicleCommand)new SetWeaponStance(!combat.WeaponStanceActive)
            : new ConfigureEngagementPlan(!combat.EngagementPlan.OpenWithWeaponStance);

        return
        [
            new("attune-combined", combinedActive ? "◆ ATTUNED 12/12" : "[G] ATTUNE 12/12", new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting]), canRequestAttunement, combinedDetail),
            new(
                "power-primary",
                primary is null ? "[P] POWER" : $"[P] {primary.Label}",
                primaryCommand,
                primary?.Available == true,
                PowerPrimaryButtonDetail(primary)),
            new("move-north", "[W]", new MoveIncarnation(0, -1), alive && !committed),
            new("move-west", "[A]", new MoveIncarnation(-1, 0), alive && !committed),
            new("move-south", "[S]", new MoveIncarnation(0, 1), alive && !committed),
            new("move-east", "[D]", new MoveIncarnation(1, 0), alive && !committed),
            new("burn", "[B] BURN", selected is null ? null : new PrepareBurn(selected.Address), alive && selected?.CanBurn == true && combat.Recovery.RemainingTicks == 0, selected is null ? "NO TARGET" : $"{selected.PreparationTicks} PREP · {selected.ConsequenceTicks} BURN"),
            new("weapon", combat.Danger.IsImmediate ? "[V] CLEAVER" : "[V] PLAN", weaponCommand, alive && !carrying && !committed, carrying ? "SET DOWN LODE" : committed ? "WORK COMMITTED" : combat.Danger.IsImmediate ? (combat.WeaponStanceActive ? "ACTIVE" : "LOWERED") : (combat.EngagementPlan.OpenWithWeaponStance ? "OPEN ACTIVE" : "OPEN LOWERED")),
            new("clock", state.Speed == ChronicleSpeed.Paused ? "[SPACE] RESUME" : "[SPACE] PAUSE", clockCommand, alive, committed ? $"WORK {power.Commitment!.CompletedTicks}/{power.Commitment.TotalTicks}" : "SLOW HEARTBEATS"),
            secondary,
        ];
    }

    private static string PowerPrimaryButtonDetail(PowerActionSnapshot? action)
    {
        if (action is null)
        {
            return "FOLLOW GOAL PANEL";
        }

        if (action.Available)
        {
            return "PRESS P NOW";
        }

        return action.Id switch
        {
            "read-primer" => "NEXT TO BURN PRIMER",
            "extract" => "NEXT TO GOLD SEAM",
            "lift" => "NEXT TO LOOSE LODE",
            "build" => "AT OUTLINED SITE",
            "dismantle" => "NEXT TO RESONATOR",
            "rebuild" => "NEXT TO RESONATOR",
            _ => "READ GOAL PANEL",
        };
    }

    private static PowerActionSnapshot? SelectPowerPrimary(PowerComesHomeContextSnapshot power)
    {
        string id;
        if (!power.BurnPrimer.IsRead)
        {
            id = "read-primer";
        }
        else if (power.Commitment is { } commitment)
        {
            id = commitment.Kind.ToString().ToLowerInvariant();
        }
        else if (power.Lode.Disposition == ResonantLodeDisposition.Embedded)
        {
            id = "extract";
        }
        else if (power.Lode.Disposition == ResonantLodeDisposition.Carried)
        {
            id = "build";
        }
        else if (power.Resonator?.Phase == HearthResonatorPhase.UnderConstruction)
        {
            id = "build";
        }
        else if (power.Resonator?.Phase is HearthResonatorPhase.Intact or HearthResonatorPhase.Damaged)
        {
            id = "dismantle";
        }
        else if (power.Resonator?.Phase is HearthResonatorPhase.Destroyed or HearthResonatorPhase.Rebuilding)
        {
            id = "rebuild";
        }
        else
        {
            id = "lift";
        }

        return power.Actions.FirstOrDefault(action => action.Id == id);
    }

    private static ChronicleCommand? PowerCommand(PowerActionSnapshot action) => action.Id switch
    {
        "read-primer" => new ReadBurnPrimer(),
        "extract" => new BeginPowerCommitment(PowerCommitmentKind.Extract),
        "lift" => new LiftResonantLode(),
        "drop" => new SetDownResonantLode(),
        "build" => new BeginPowerCommitment(PowerCommitmentKind.Build),
        "dismantle" => new BeginPowerCommitment(PowerCommitmentKind.Dismantle),
        "rebuild" => new BeginPowerCommitment(PowerCommitmentKind.Rebuild),
        _ => null,
    };

    private static string CompactPowerCapacity(AttunementCapacitySnapshot capacity)
    {
        var current = capacity.CapacityAtLastAttunement is { } currentCapacity
            ? $"{capacity.CurrentUsedLoad}/{currentCapacity}"
            : "NOT ATTUNED";
        return $"CURRENT {current} · NEXT ATTUNE {capacity.NextAttunementCapacity} " +
               $"({capacity.InherentCapacity} + {capacity.SourceContribution} SOURCE)";
    }

    private static string TargetFactsText(TargetPreviewSnapshot target)
    {
        return $"{target.Facts.Matter} · {target.Facts.Scale}\n" +
               $"Distance {target.CardinalDistance} · " +
               $"{(target.Facts.IsFlammable ? "flammable" : "nonflammable")} · " +
               $"{(target.Facts.IsAnchored ? "anchored" : "not anchored")}";
    }

    private static string PowerHeadingState(PowerComesHomeContextSnapshot power)
    {
        if (!power.BurnPrimer.IsRead)
        {
            return "BURN PRIMER · ONE TILE NORTH OF HOME";
        }

        return power.Resonator?.Phase switch
        {
            HearthResonatorPhase.UnderConstruction => "HEARTH RESONATOR · BUILDING",
            HearthResonatorPhase.Intact when power.Attunement.CapacityAtLastAttunement is not 12 =>
                "HEARTH RESONATOR · +4 LOAD READY",
            HearthResonatorPhase.Intact => "HEARTH RESONATOR · 12-LOAD POWER",
            HearthResonatorPhase.Damaged => "HEARTH RESONATOR · DAMAGED, +4 STILL READY",
            HearthResonatorPhase.Destroyed => "HEARTH RESONATOR · DESTROYED, NEXT LOAD 8",
            HearthResonatorPhase.Rebuilding => "HEARTH RESONATOR · REBUILDING",
            { } phase => "HEARTH RESONATOR · " + phase.ToString().ToUpperInvariant(),
            null when power.Lode.Disposition == ResonantLodeDisposition.Embedded => "GOAL · BRING THE GOLD LODE HOME",
            null when power.Lode.Disposition == ResonantLodeDisposition.Carried => "GOLD LODE · CARRY IT HOME",
            null => "GOLD LODE · PHYSICAL AT " + (power.Lode.Address?.ToString() ?? "UNKNOWN"),
        };
    }

    private static string CompactForecastText(CombatForecastEventSnapshot item) => item.Kind switch
    {
        CombatForecastKind.BurnRelease => $"H{item.Tick} · BURN releases",
        CombatForecastKind.BurnDamage => $"H{item.Tick} · BURN {item.Damage} damage",
        CombatForecastKind.WeaponStrike => $"H{item.Tick} · CLEAVER {item.Damage} physical",
        CombatForecastKind.MireBruteMove => $"H{item.Tick} · MIRE BRUTE closes",
        CombatForecastKind.MireBruteSwing => $"H{item.Tick} · MIRE BRUTE {item.Damage} physical",
        CombatForecastKind.RecoveryComplete => $"H{item.Tick} · RECOVERY complete",
        CombatForecastKind.Engagement => $"H{item.Tick} · ENGAGEMENT pauses",
        _ => $"H{item.Tick} · {item.Text}",
    };

    private static string TargetOutcomeText(
        TargetPreviewSnapshot target,
        CombatContextSnapshot combat)
    {
        var preparationTargetsSelection =
            combat.Preparation is { TargetIdentity: var targetIdentity } &&
            target.Identity == targetIdentity;
        var pendingTargetsSelection =
            combat.PendingAction is { Kind: TacticalActionKind.PrepareBurn } pending &&
            pending.Target == target.Address;
        var eligibility = preparationTargetsSelection
            ? "ACTIVE"
            : pendingTargetsSelection
                ? "PENDING"
                : target.CanBurn ? "VALID" : "REJECTED";
        var eligibilityDetail = preparationTargetsSelection && combat.Preparation is { } activePreparation
            ? activePreparation.DisplayName
            : eligibility == "PENDING"
                ? "Burn will begin on the next Heartbeat."
                : target.EligibilityReason;
        var when = preparationTargetsSelection && combat.Preparation is { } preparing
            ? $"H+{preparing.RemainingTicks} of {preparing.TotalTicks}"
            : $"{target.PreparationTicks} Heartbeat preparation";
        var interruption = "Brute swing before release";
        var prevention = combat.Preparation is null
            ? "Nothing"
            : "Cleaver strike";
        return $"STATE  {eligibility} · {eligibilityDetail}\n" +
               $"WHEN   {when} · BURN {target.ConsequenceTicks}\n" +
               $"INTERRUPTS   {interruption}\n" +
               $"PREVENTS   {prevention}\n" +
               $"RECOVERY   {combat.Recovery.RemainingTicks}/{target.RecoveryTicks}";
    }

    private async Task RunGoal6BVisualAcceptance()
    {
        _simulation = CreateTestingStart();
        _selectedTargetKind = CombatTargetKind.MireBrute;
        _presentationStatus = "Goal 6B rendered acceptance Chronicle.";
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        await CaptureGoal6BProof("burn-primer");
        Verify(
            !_openingPanel.Visible &&
            !_simulation.State.Codex.Contains(WordIds.Burn) &&
            _hud.PowerStatus.Text.Contains("CHECKLIST · LEARN BURN", StringComparison.Ordinal) &&
            ActionButton("power-primary").Text == "[P] READ BURN PRIMER\nPRESS P NOW" &&
            ActionButton("attune-combined").Text.Contains("READ BURN PRIMER", StringComparison.Ordinal) &&
            _map.CurrentPlan!.Marks.Any(mark =>
                mark.Address == _simulation.PowerComesHomeContext.BurnPrimer.Address &&
                mark.VisualId == "glyph.codex") &&
            _map.CurrentPlan.Marks.Any(mark =>
                mark.Address == _simulation.PowerComesHomeContext.BurnPrimer.Address &&
                mark.VisualId == "emphasis.target.selected"),
            "A fresh player Chronicle must skip opening paths and visibly direct P to the nearby unread Burn Primer.");

        var primerTick = _simulation.State.Tick;
        Press(ActionButton("power-primary"));
        Verify(
            _simulation.State.Tick == primerTick &&
            _simulation.State.Codex.Contains(WordIds.Burn) &&
            _simulation.State.Codex.Contains(WordIds.Quickly) &&
            _simulation.State.Codex.Contains(WordIds.Lasting) &&
            _simulation.PowerComesHomeContext.BurnPrimer.IsRead &&
            _hud.PowerStatus.Text.Contains("CHECKLIST · GET THE GOLD LODE", StringComparison.Ordinal),
            "Reading the Burn Primer must spend no Heartbeat, persist the complete test Expression, and switch checklists.");

        await CaptureGoal6BProof("embedded");
        Verify(
            _hud.PowerStatus.Text.Contains("CHECKLIST · GET THE GOLD LODE", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Contains("GOLD SEAM", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Contains("EAST", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Contains("[ ] P — Extract", StringComparison.Ordinal) &&
            ActionButton("power-primary").Text == "[P] EXTRACT LODE\nNEXT TO GOLD SEAM" &&
            ActionButton("attune-combined").Text.Contains("NEEDS RESONATOR", StringComparison.Ordinal),
            "The first Goal 6B screen must give a short checklist with map wayfinding and the exact extraction input.");

        var beforeAttunementRequest = _simulation.State;
        Press(ActionButton("attune-combined"));
        var mouseAttunementState = ChronicleSaveCodec.Serialize(_simulation.State);
        var mouseAttunementStatus = _presentationStatus;
        _simulation = new ChronicleSimulation(beforeAttunementRequest);
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        TriggerInput(CombinedAction);
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == mouseAttunementState &&
            _presentationStatus == mouseAttunementStatus &&
            _presentationStatus.Contains("Needs 12 Load", StringComparison.Ordinal),
            "Mouse and keyboard must expose the same precise, non-mutating base-capacity Attunement rejection.");

        Goal6BMove("move-south");
        for (var index = 0; index < 7; index++)
        {
            Goal6BMove("move-east");
        }
        Goal6BMove("move-north");
        Verify(_simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3),
            "The safe rendered route must reach the Singing Seam without consuming or teleporting the Lode.");

        var beforeMouse = _simulation.State;
        Press(ActionButton("power-primary"));
        var mouseState = ChronicleSaveCodec.Serialize(_simulation.State);
        var mouseChecklist = _hud.PowerStatus.Text;
        _simulation = new ChronicleSimulation(beforeMouse);
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        TriggerInput(PowerPrimaryAction);
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == mouseState &&
            _hud.PowerStatus.Text == mouseChecklist,
            "Mouse and keyboard Power paths must expose the same Core state and checklist.");

        Press(ActionButton("clock"));
        AdvanceAndRefresh();
        var extractionOne = ChronicleSaveCodec.Serialize(_simulation.State);
        Press(_hud.GetNode<Button>("SaveAction"));
        Press(_hud.GetNode<Button>("LoadAction"));
        Verify(ChronicleSaveCodec.Serialize(_simulation.State) == extractionOne,
            "Actual Godot save/load must preserve extraction 1/2 without double advancement.");
        AdvanceAndRefresh();
        var extractionAddress = _simulation.State.Address;
        TriggerInput(MoveSouthAction);
        Verify(
            _simulation.State.Address == extractionAddress with { Y = extractionAddress.Y + 1 } &&
            _simulation.State.Speed == ChronicleSpeed.Slow &&
            _simulation.CombatContext.PendingAction is null,
            "After extraction completes, one safe keyboard move must resolve once without re-entering auto-pause or leaving a queued step.");
        TriggerInput(MoveNorthAction);
        Verify(
            _simulation.State.Address == extractionAddress &&
            _simulation.CombatContext.PendingAction is null,
            "Returning to the extracted Lode must also remain an immediate safe movement command.");
        Press(ActionButton("power-primary"));
        await CaptureGoal6BProof("carried");
        Verify(ActionButton("power-primary").Text == "[P] BUILD\nAT OUTLINED SITE",
            "Carrying must expose one short, non-overrunning Build command tied to the outlined Home site.");

        Goal6BMove("move-south");
        for (var index = 0; index < 7; index++)
        {
            Goal6BMove("move-west");
        }
        Goal6BMove("move-north");
        Verify(_simulation.State.Address == ChronicleState.AcceptedHomeFixtureAddress,
            "The rendered carrier route must return the same physical Lode to Home.");

        Press(ActionButton("power-primary"));
        Press(ActionButton("clock"));
        AdvanceAndRefresh();
        await CaptureGoal6BProof("construction");
        Press(ActionButton("power-secondary"));
        Verify(_simulation.State.PowerHome!.Resonator is { Progress: 1 } &&
               _simulation.State.PowerHome.Commitment is null,
            "The visible cancel path must preserve the one-step foundation.");
        TriggerInput(PowerPrimaryAction);
        TriggerInput(PauseAction);
        AdvanceAndRefresh();
        AdvanceAndRefresh();
        await CaptureGoal6BProof("intact-capacity-ready");
        Verify(
            _hud.PowerHeading.Text.Contains("+4 LOAD READY", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Contains("CHECKLIST · USE NEW LOAD", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Contains("G — Attune", StringComparison.Ordinal) &&
            ActionButton("attune-combined").Text.Contains("[G] ATTUNE 12/12", StringComparison.Ordinal),
            "An intact Resonator must switch to a short checklist connecting +4 future capacity to the G Attunement command.");

        Press(ActionButton("attune-combined"));
        Verify(_simulation.CombatContext.Expression is
               { UsedLoad: 12, SharedLoadCapacity: 12, UsedLinks: 3 } &&
               _hud.PowerStatus.Text.Contains("CHECKLIST · TEST SOURCE LOSS", StringComparison.Ordinal) &&
               _hud.PowerStatus.Text.Contains("P — Dismantle", StringComparison.Ordinal),
            "The rendered combined Attunement must expose 12 Load and switch to the source-loss checklist.");
        Press(ActionButton("power-primary"));
        Press(ActionButton("clock"));
        AdvanceAndRefresh();
        await CaptureGoal6BProof("damaged");
        AdvanceAndRefresh();
        Verify(_simulation.CombatContext.Expression.UsedLoad == 12,
            "Rendered Source destruction must not remotely disable the current combined Expression.");
        await CaptureGoal6BProof("destroyed-current-next");

        Press(ActionButton("power-primary"));
        Press(ActionButton("clock"));
        AdvanceAndRefresh();
        await CaptureGoal6BProof("rebuilding");
        var rebuildingOne = ChronicleSaveCodec.Serialize(_simulation.State);
        Press(_hud.GetNode<Button>("SaveAction"));
        Press(_hud.GetNode<Button>("LoadAction"));
        Verify(ChronicleSaveCodec.Serialize(_simulation.State) == rebuildingOne,
            "Actual Godot save/load must preserve rebuilding 1/3 exactly.");
        AdvanceAndRefresh();
        AdvanceAndRefresh();
        Verify(
            _simulation.PowerComesHomeContext.Resonator?.Phase == HearthResonatorPhase.Intact &&
            _simulation.PowerComesHomeContext.Attunement.NextAttunementCapacity == 12,
            "The rendered rebuild path must restore future capacity without hidden material changes.");
        Press(_hud.GetNode<Button>("SaveAction"));
        Verify(SaveVersion() == 7, "Goal 6B rendered acceptance must write strict save v7.");
        GD.Print("GOAL6B VISUAL ACCEPTANCE PASS captures=8 save=7 keyboard=mouse map=physical capacity=next-attunement");
    }

    private void Goal6BMove(string actionId)
    {
        Press(ActionButton(actionId));
        if (_simulation.CombatContext.PendingAction is null)
        {
            return;
        }

        Press(ActionButton("clock"));
        AdvanceAndRefresh();
    }

    private async Task CaptureGoal6BProof(string stage)
    {
        RefreshPresentation(forceMap: true);
        var power = _simulation.PowerComesHomeContext;
        var expectedState = stage switch
        {
            "burn-primer" => !power.BurnPrimer.IsRead &&
                !_simulation.State.Codex.Contains(WordIds.Burn),
            "embedded" => power.Lode.Disposition == ResonantLodeDisposition.Embedded,
            "carried" => power.Lode.Disposition == ResonantLodeDisposition.Carried,
            "construction" => power.Resonator is
                { Phase: HearthResonatorPhase.UnderConstruction, Progress: 1 },
            "intact-capacity-ready" => power.Resonator?.Phase == HearthResonatorPhase.Intact &&
                power.Attunement.CapacityAtLastAttunement == 8 &&
                power.Attunement.NextAttunementCapacity == 12,
            "damaged" => power.Resonator?.Phase == HearthResonatorPhase.Damaged &&
                power.Attunement.NextAttunementCapacity == 12,
            "destroyed-current-next" => power.Resonator?.Phase == HearthResonatorPhase.Destroyed &&
                power.Attunement.CurrentUsedLoad == 12 &&
                power.Attunement.NextAttunementCapacity == 8,
            "rebuilding" => power.Resonator is
                { Phase: HearthResonatorPhase.Rebuilding, Progress: 1 },
            _ => false,
        };
        Verify(
            expectedState &&
            !string.IsNullOrWhiteSpace(_hud.PowerHeading.Text) &&
            !string.IsNullOrWhiteSpace(_hud.PowerStatus.Text) &&
            _hud.PowerStatus.Text.StartsWith("CHECKLIST · ", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length <= 5 &&
            _hud.PowerStatus.Text.Contains("[ ]", StringComparison.Ordinal) &&
            _hud.PowerCapacity.Text.Contains("CURRENT ", StringComparison.Ordinal) &&
            _hud.PowerCapacity.Text.Contains("NEXT ATTUNE ", StringComparison.Ordinal) &&
            string.IsNullOrEmpty(_hud.PowerDecision.Text) &&
            !_hud.PowerDecision.Visible &&
            _hud.PowerStatus.Size.X >= 500 &&
            _hud.ActionButtons.Take(2).All(button => button.GetMinimumSize().X <= button.Size.X) &&
            _map.VisibleColumns == ChronicleHud.MapColumns &&
            _map.VisibleRows == ChronicleHud.MapRows,
            $"Goal 6B '{stage}' must keep one five-line-or-shorter state checklist, compact capacity, and the map visible.");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Verify(
            _hud.PowerStatus.GetMinimumSize().Y <= _hud.PowerStatus.Size.Y &&
            _hud.PowerCapacity.GetMinimumSize().Y <= _hud.PowerCapacity.Size.Y,
            $"Goal 6B '{stage}' must not clip its checklist or compact capacity line.");
        RenderingServer.ForceDraw(swapBuffers: false);
        var image = GetViewport().GetTexture().GetImage();
        Verify(image is not null && image.GetWidth() == 1600 && image.GetHeight() == 900,
            "Goal 6B native HUD proof must capture the exact 1600 × 900 viewport.");
        Verify(image!.SavePng($"user://goal6b-{stage}-hud.png") == Error.Ok,
            $"Goal 6B '{stage}' native HUD capture must be writable.");
        GD.Print($"GOAL6B HUD CAPTURE PASS stage={stage} size=1600x900");
    }

    private static string AppliedCommandMessage(ChronicleCommand command) => command switch
    {
        SetChronicleSpeed speed => $"Chronicle speed is now {speed.Speed}.",
        MoveIncarnation => "The Incarnation moves with everything physically attached.",
        EndIncarnationAtBell => "The Incarnation ends at the Bell; Chronicle durables remain.",
        CreateReplacementIncarnation => "A replacement Incarnation enters the existing Chronicle.",
        _ => "Action completed.",
    };

    private async void RunAcceptance(Func<Task> journey)
    {
        try
        {
            await journey();
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"GOAL ACCEPTANCE FAILED: {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private async Task RunQuicklyAcceptance()
    {
        StartFreshAcceptance();
        TriggerInput(SlowAction);
        Press(ActionButton("attune-quickly"));
        Press(ActionButton("weapon"));
        Verify(_simulation.CombatContext.EngagementPlan.OpenWithWeaponStance, "Quickly journey must enable the opening Cleaver plan.");

        MoveToThreat(useKeyboard: true);
        Verify(_simulation.State.Speed == ChronicleSpeed.Paused, "Engagement must pause before the first hostile Heartbeat.");
        Verify(_simulation.CombatContext.Danger.IsImmediate, "The Mire Brute must be an immediate threat.");
        TriggerInput(PauseAction);
        Verify(_simulation.State.Speed == ChronicleSpeed.Slow, "Space must resume Slow Heartbeats while paused.");
        TriggerInput(PauseAction);
        Verify(_simulation.State.Speed == ChronicleSpeed.Paused, "Space must pause while Slow Heartbeats are running.");

        var beforeReject = ChronicleSaveCodec.Serialize(_simulation.State);
        Press(_hud.TargetButtons[1]);
        Verify(_hud.TargetFacts.Text.Contains("nonflammable", StringComparison.OrdinalIgnoreCase), "Basalt rejection must name nonflammability.");
        Verify(beforeReject == ChronicleSaveCodec.Serialize(_simulation.State), "Invalid Target preview must cost no state or time.");
        Press(_hud.TargetButtons[0]);
        Press(ActionButton("burn"));
        Verify(_simulation.CombatContext.Preparation?.RemainingTicks == 1, "Quickly must prepare in one Heartbeat.");
        var activeBrute = _simulation.PreviewTarget(_simulation.CombatContext.MireBrute!.Address);
        Verify(
            TargetOutcomeText(
                activeBrute with { Address = activeBrute.Address with { X = activeBrute.Address.X - 1 } },
                _simulation.CombatContext).StartsWith("STATE  ACTIVE", StringComparison.Ordinal),
            "Active Preparation must follow the Brute identity when pursuit changes its Address.");
        Press(_hud.TargetButtons[1]);
        Verify(
            _hud.TargetOutcome.Text.StartsWith("STATE  REJECTED", StringComparison.Ordinal),
            "Selecting basalt during Brute Preparation must not attribute the active Burn to basalt.");
        Press(_hud.TargetButtons[0]);
        await CaptureHudProof("quickly-preparation");
        Press(ActionButton("clock"));
        AdvanceAndRefresh();
        Verify(_simulation.CombatContext.Scorch is not null, "Quickly release must create scorch.");

        FinishFightWithCleaver();
        Verify(_simulation.CombatContext.MireBrute?.IsLiving == false, "Quickly journey must defeat the Mire Brute.");
        Press(_hud.GetNode<Button>("SaveAction"));
        Verify(SaveVersion() == 7, "Quickly journey must rewrite through strict save v7.");
        GD.Print("GOAL6A QUICKLY SAVE READY hud=map-first target=basalt-rejected scorch=present brute=dead save=7");
    }

    private Task RunQuicklyRestartAcceptance()
    {
        Verify(SaveVersion() == 7, "Quickly restart must load current save v7.");
        Verify(_simulation.CombatContext.Scorch is not null, "Quickly restart must retain scorch.");
        Verify(_simulation.CombatContext.MireBrute?.IsLiving == false, "Quickly restart must retain the Brute outcome.");
        Verify(_hud.TargetHeading.Text.Contains("MIRE BRUTE", StringComparison.Ordinal), "Target rail must restore the Brute identity.");
        GD.Print("GOAL6A QUICKLY RESTART PASS scorch=present brute=dead hud=restored");
        return Task.CompletedTask;
    }

    private async Task RunLastingAcceptance()
    {
        StartFreshAcceptance();
        Press(_againstButton);
        Press(ActionButton("attune-lasting"));
        MoveToThreat(useKeyboard: false);
        Press(ActionButton("clock"));

        var guard = 0;
        while ((_simulation.CombatContext.MireBrute is not { } brute ||
                CardinalDistance(_simulation.State.Address, brute.Address) != 1 ||
                brute.SwingTicksRemaining > 2) && guard++ < 20)
        {
            AdvanceAndRefresh();
        }

        Press(ActionButton("clock"));
        Press(ActionButton("burn"));
        Press(ActionButton("clock"));
        guard = 0;
        while (_simulation.CombatContext.Preparation is not null && guard++ < 8)
        {
            AdvanceAndRefresh();
        }

        Verify(
            _simulation.CombatContext.RecentResults.Any(result => result.Kind == CombatResultKind.PreparationInterrupted),
            "Lasting journey must visibly interrupt an exposed Preparation.");
        await CaptureHudProof("lasting-interruption");

        Press(ActionButton("burn"));
        Press(ActionButton("clock"));
        guard = 0;
        while (_simulation.CombatContext.Scorch is null && guard++ < 8)
        {
            AdvanceAndRefresh();
        }

        Verify(_simulation.CombatContext.Scorch is not null, "Lasting post-swing release must create scorch.");
        if (_simulation.State.Speed == ChronicleSpeed.Paused)
        {
            Press(ActionButton("clock"));
        }

        guard = 0;
        while (_simulation.State.HasLivingIncarnation && guard++ < 80)
        {
            AdvanceAndRefresh();
            if (_simulation.State.Speed == ChronicleSpeed.Paused && _simulation.State.HasLivingIncarnation)
            {
                Press(ActionButton("clock"));
            }
        }

        Verify(!_simulation.State.HasLivingIncarnation, "Lasting journey must preserve the deliberate death branch.");
        Verify(_simulation.CombatContext.Scorch is not null, "Scorch must survive body death.");
        Press(_hud.GetNode<Button>("SaveAction"));
        GD.Print($"GOAL6A LASTING DEATH READY scorch=present bruteHp={_simulation.CombatContext.MireBrute?.HitPoints} incarnation=ended save=7");
    }

    private Task RunLastingRestartAcceptance()
    {
        Verify(!_simulation.State.HasLivingIncarnation, "Lasting restart must begin awaiting replacement.");
        var retainedHitPoints = _simulation.CombatContext.MireBrute?.HitPoints;
        Verify(retainedHitPoints is > 0 and < CombatState.MireBruteMaximumHitPoints, "Lasting restart must retain the Brute wound.");
        Verify(_simulation.CombatContext.Scorch is not null, "Lasting restart must retain scorch.");
        TriggerInput(ReplaceAction);
        Verify(_simulation.State.IncarnationId == 2, "Replacement must advance the Incarnation identity.");
        Verify(_simulation.CombatContext.Incarnation.HitPoints == _simulation.CombatContext.Incarnation.MaximumHitPoints, "Replacement equipment must restore maximum HP.");
        Press(ActionButton("attune-lasting"));
        Press(ActionButton("weapon"));
        MoveToThreat(useKeyboard: false);
        FinishFightWithCleaver();
        Verify(_simulation.CombatContext.MireBrute?.IsLiving == false, "Replacement must be able to finish the continuing encounter.");
        Press(_hud.GetNode<Button>("SaveAction"));
        GD.Print("GOAL6A LASTING RESTART PASS incarnation=2 equipment=fresh scorch=present brute=dead save=7");
        return Task.CompletedTask;
    }

    private void StartFreshAcceptance()
    {
        _simulation = new ChronicleSimulation(Goal6AFixture());
        _selectedTargetKind = CombatTargetKind.MireBrute;
        _presentationStatus = "Goal 6A acceptance Chronicle.";
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
    }

    private static ChronicleState Goal6AFixture() => ChronicleState.Begin(InitialSeed) with
    {
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        Intent = OpeningIntent.Unchosen,
        Codex = new CodexState(),
        Loadout = LoadoutState.Empty,
        WorldGrammarVersion = 4,
        Home = null,
        Combat = CombatState.Create(InitialSeed),
        PowerHome = null,
        Attunement = new LoadAttunementState(8, 0),
    };

    private async Task CaptureHudProof(string stage)
    {
        RefreshPresentation(forceMap: true);
        Verify(
            ChronicleHud.MapDisplayCellSize == 40 &&
            ChronicleHud.MapWidth * ChronicleHud.MapHeight >=
            ChronicleHud.CanvasWidth * ChronicleHud.CanvasHeight * 3 / 4,
            "The corrected Goal 6A HUD must use a crisp 2x map while keeping its physical surface dominant.");
        Verify(
            _hud.IncarnationHealthBar.Visible &&
            _hud.TargetHealthBar.Visible &&
            _hud.TargetHealthBar.Size.Y >= 16 &&
            _hud.TargetHealthBar.Size.X <= 180 &&
            _hud.TargetHealthText.Text.Contains('/') &&
            _hud.IncarnationHealthBar.Size.X <= 120 &&
            _hud.IncarnationHealthText.Text.Contains('/') &&
            _hud.PauseBadge.Visible &&
            _hud.PauseBadge.Position.Y < ChronicleHud.TopRailHeight &&
            _hud.MessageReadout.Size.Y >= 304 &&
            _hud.ConsequenceRows.Count == 5 &&
            _hud.ConsequenceRows.All(row => !string.IsNullOrWhiteSpace(row.Text)) &&
            _map.IsPaused &&
            _map.VisibleColumns == ChronicleHud.MapColumns &&
            _map.VisibleRows == ChronicleHud.MapRows &&
            _hud.ActionButtons.Count(button => button.Icon is not null) >= 4 &&
            !string.IsNullOrWhiteSpace(_hud.ForecastReadout.Text) &&
            !string.IsNullOrWhiteSpace(_hud.MessageReadout.Text),
            "The revised HUD must expose value-integrated HP bars, top-rail pause, actor contrast, consequence rows, P-GEN action icons, forecast, and the enlarged Message Log.");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(swapBuffers: false);
        var image = GetViewport().GetTexture().GetImage();
        Verify(
            image is not null &&
            image.GetWidth() == ChronicleHud.CanvasWidth &&
            image.GetHeight() == ChronicleHud.CanvasHeight,
            "The player HUD proof must capture the rendered 1600 × 900 viewport.");
        var path = $"user://goal6a-{stage}-hud.png";
        Verify(image!.SavePng(path) == Error.Ok, "The rendered player HUD proof capture must be writable.");
        GD.Print($"GOAL6A HUD CAPTURE PASS stage={stage} size=1600x900 icons=4");
    }

    private void MoveToThreat(bool useKeyboard)
    {
        var guard = 0;
        while (!_simulation.CombatContext.Danger.IsImmediate && guard++ < 160)
        {
            var brute = _simulation.CombatContext.MireBrute
                ?? throw new InvalidOperationException("The Goal 6A fixture has no Mire Brute.");
            var deltaX = Math.Sign(brute.Address.X - _simulation.State.Address.X);
            var deltaY = Math.Sign(brute.Address.Y - _simulation.State.Address.Y);
            if (deltaX != 0)
            {
                if (useKeyboard)
                {
                    TriggerInput(deltaX > 0 ? MoveEastAction : MoveWestAction);
                }
                else
                {
                    Press(ActionButton(deltaX > 0 ? "move-east" : "move-west"));
                }
            }
            else if (useKeyboard)
            {
                TriggerInput(deltaY > 0 ? MoveSouthAction : MoveNorthAction);
            }
            else
            {
                Press(ActionButton(deltaY > 0 ? "move-south" : "move-north"));
            }
        }

        Verify(guard < 160, "The acceptance journey could not reach the generated Mire Brute.");
    }

    private void FinishFightWithCleaver()
    {
        var guard = 0;
        while (_simulation.CombatContext.MireBrute?.IsLiving == true && guard++ < 100)
        {
            if (_simulation.State.Speed == ChronicleSpeed.Paused)
            {
                Press(ActionButton("clock"));
            }

            AdvanceAndRefresh();
        }

        Verify(guard < 100, "The bounded fight did not resolve.");
    }

    private void AdvanceAndRefresh()
    {
        _simulation.AdvanceOneTick();
        RefreshPresentation(forceMap: true);
    }

    private Button ActionButton(string id) => _hud.ActionButtons.Single(button =>
        string.Equals(button.Name, $"HudAction_{id}", StringComparison.Ordinal));

    private static void Press(Button button) => button.EmitSignal(Button.SignalName.Pressed);

    private void TriggerInput(StringName action)
    {
        _UnhandledInput(new InputEventAction
        {
            Action = action,
            Pressed = true,
            Strength = 1,
        });
    }

    private int SaveVersion()
    {
        using var document = JsonDocument.Parse(ChronicleSaveCodec.Serialize(_simulation.State));
        return document.RootElement.GetProperty("Version").GetInt32();
    }

    private static int CardinalDistance(WorldAddress first, WorldAddress second)
    {
        if (!string.Equals(first.Stratum, second.Stratum, StringComparison.Ordinal))
        {
            return int.MaxValue;
        }

        var distance = Int128.Abs((Int128)first.X - second.X) +
                       Int128.Abs((Int128)first.Y - second.Y);
        return distance > int.MaxValue ? int.MaxValue : (int)distance;
    }

    private static void Verify(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
