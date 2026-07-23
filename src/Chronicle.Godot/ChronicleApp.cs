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
    private static readonly StringName InspectAction = "chronicle_inspect";

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
    private bool _verifyGoal7AVisuals;
    private bool _verifyGoal7BVisuals;
    private bool _prepareGoal7AWelcomeUat;
    private bool _prepareGoal7AReplacementUat;
    private bool _prepareGoal7BSuggestUat;
    private bool _prepareGoal7BCommandUat;
    private bool _isInspecting;
    private WorldAddress? _inspectionCursor;
    private WorldAddress? _inspectionSelection;

    public override void _Ready()
    {
        var arguments = OS.GetCmdlineUserArgs();
        _verifyQuickly = arguments.Contains("--verify-goal6a-quickly", StringComparer.Ordinal);
        _verifyQuicklyRestart = arguments.Contains("--verify-goal6a-quickly-restart", StringComparer.Ordinal);
        _verifyLasting = arguments.Contains("--verify-goal6a-lasting", StringComparer.Ordinal);
        _verifyLastingRestart = arguments.Contains("--verify-goal6a-lasting-restart", StringComparer.Ordinal);
        _verifyGoal6BVisuals = arguments.Contains("--verify-goal6b-visuals", StringComparer.Ordinal);
        _verifyGoal7AVisuals = arguments.Contains("--verify-goal7a-visuals", StringComparer.Ordinal);
        _verifyGoal7BVisuals = arguments.Contains("--verify-goal7b-visuals", StringComparer.Ordinal);
        _prepareGoal7AWelcomeUat = arguments.Contains("--prepare-goal7a-welcome-uat", StringComparer.Ordinal);
        _prepareGoal7AReplacementUat = arguments.Contains("--prepare-goal7a-replacement-uat", StringComparer.Ordinal);
        _prepareGoal7BSuggestUat = arguments.Contains("--prepare-goal7b-suggest-uat", StringComparer.Ordinal);
        _prepareGoal7BCommandUat = arguments.Contains("--prepare-goal7b-command-uat", StringComparer.Ordinal);

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

        var hadSave = Godot.FileAccess.FileExists(SavePath);
        LoadOrCreateChronicle();
        if (!hadSave && _prepareGoal7AWelcomeUat)
        {
            PrepareGoal7AWelcomeUat();
        }
        else if (!hadSave && _prepareGoal7AReplacementUat)
        {
            PrepareGoal7AReplacementUat();
        }
        else if (!hadSave && _prepareGoal7BSuggestUat)
        {
            PrepareGoal7BUat(WordIds.Suggest);
        }
        else if (!hadSave && _prepareGoal7BCommandUat)
        {
            PrepareGoal7BUat(WordIds.Command);
        }
        RefreshPresentation(forceMap: true);
        GD.Print("GOAL6A READY");
        GD.Print("GOAL6B READY");
        GD.Print("GOAL7A READY");
        GD.Print("GOAL7B READY");

        if (_verifyGoal7BVisuals)
        {
            Callable.From(() => RunAcceptance(RunGoal7BVisualAcceptance)).CallDeferred();
        }
        else if (_verifyGoal7AVisuals)
        {
            Callable.From(() => RunAcceptance(RunGoal7AVisualAcceptance)).CallDeferred();
        }
        else if (_verifyGoal6BVisuals)
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
        if (!_verifyGoal6BVisuals && !_verifyGoal7AVisuals && !_verifyGoal7BVisuals)
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

    public override void _Input(InputEvent @event)
    {
        if (_simulation.State.Intent == OpeningIntent.Unchosen ||
            !_simulation.State.HasLivingIncarnation ||
            !@event.IsActionPressed(PauseAction))
        {
            return;
        }

        // Space is the Chronicle Clock control even while a HUD button owns
        // keyboard focus. Reserve it before Godot can also treat the same key
        // as ui_accept and activate that focused action.
        GetViewport().SetInputAsHandled();
        Issue(new SetChronicleSpeed(
            _simulation.State.Speed == ChronicleSpeed.Paused
                ? ChronicleSpeed.Slow
                : ChronicleSpeed.Paused));
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

        if (@event.IsActionPressed(InspectAction))
        {
            BeginInspection(_simulation.State.Address);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouseButton
            {
                Pressed: true,
                ButtonIndex: MouseButton.Left,
            } mouse && TryMapAddress(mouse.Position, out var clickedAddress))
        {
            BeginInspection(clickedAddress, select: true);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_isInspecting)
        {
            if (@event.IsActionPressed(MoveNorthAction))
            {
                MoveInspectionCursor(0, -1);
            }
            else if (@event.IsActionPressed(MoveWestAction))
            {
                MoveInspectionCursor(-1, 0);
            }
            else if (@event.IsActionPressed(MoveSouthAction))
            {
                MoveInspectionCursor(0, 1);
            }
            else if (@event.IsActionPressed(MoveEastAction))
            {
                MoveInspectionCursor(1, 0);
            }
            else if (@event.IsActionPressed("ui_accept"))
            {
                _inspectionSelection = _inspectionCursor;
                _presentationStatus = $"Selected visible cell {_inspectionSelection}.";
                RefreshPresentation(forceMap: true);
            }
            else if (@event.IsActionPressed("ui_cancel"))
            {
                _isInspecting = false;
                _presentationStatus = "Inspection closed. WASD moves the Incarnation again.";
                RefreshPresentation(forceMap: true);
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
            FocusMode = Control.FocusModeEnum.All,
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
        var beforeResults = _simulation.CombatContext.RecentResults;
        var beforeLast = beforeResults.LastOrDefault();
        var beforeCount = beforeResults.Count;
        var beforeAgentEvents = _simulation.AgentContext.RecentEvents;
        var beforeAgentLast = beforeAgentEvents.LastOrDefault();
        var beforeAgentCount = beforeAgentEvents.Count;
        var result = _simulation.Apply(command);
        var afterResults = _simulation.CombatContext.RecentResults;
        var afterAgentEvents = _simulation.AgentContext.RecentEvents;
        var commandRecorded = result.Applied &&
                              (afterResults.Count != beforeCount ||
                               afterResults.LastOrDefault() != beforeLast ||
                               afterAgentEvents.Count != beforeAgentCount ||
                               afterAgentEvents.LastOrDefault() != beforeAgentLast);
        _presentationStatus = commandRecorded
            ? string.Empty
            : string.IsNullOrWhiteSpace(result.Message)
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
        if (HasDirectiveSurface())
        {
            var agentIdentity = _simulation.DirectiveContext.PrimaryAgentIdentity!;
            if (_simulation.DirectiveContext.Pending is not null)
            {
                Issue(new WithdrawDirective(agentIdentity));
            }
            else
            {
                IssueDirective(DirectiveKind.RestByRoadRoll);
            }

            return;
        }

        if (_simulation.AgentContext.PrimaryAgent is { } agent)
        {
            var kind = agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered
                ? AgentActionKind.WithdrawWelcome
                : AgentActionKind.OfferWelcome;
            var agentAction = _simulation.AgentContext.Actions.Single(candidate => candidate.Kind == kind);
            if (!agentAction.Available)
            {
                _presentationStatus = AgentPresentation.ActionUnavailable(agent, agentAction);
                RefreshPresentation();
                return;
            }

            Issue(kind == AgentActionKind.OfferWelcome
                ? new OfferWelcome(agent.Identity)
                : new WithdrawWelcome(agent.Identity));
            return;
        }

        var action = SelectPowerPrimary(_simulation.PowerComesHomeContext);
        if (action is null || PowerCommand(action) is not { } command)
        {
            _presentationStatus = action is null
                ? "No contextual Power Comes Home action is available here."
                : HoldingPresentation.ActionAvailability(action);
            RefreshPresentation();
            return;
        }

        Issue(command);
    }

    private void IssuePowerSecondary()
    {
        if (HasDirectiveSurface())
        {
            IssueDirective(DirectiveKind.ApproachMireBrute);
            return;
        }

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

    private void BeginInspection(WorldAddress address, bool select = false)
    {
        _isInspecting = true;
        _inspectionCursor = address;
        if (select)
        {
            _inspectionSelection = address;
        }

        _presentationStatus = select
            ? $"Selected visible cell {address}. Escape returns WASD to movement."
            : "Inspection open. WASD moves the cursor; Enter selects; Escape exits.";
        RefreshPresentation(forceMap: true);
    }

    private void MoveInspectionCursor(int deltaX, int deltaY)
    {
        var visible = CurrentVisibleBounds();
        var current = _inspectionCursor ?? _simulation.State.Address;
        var x = Math.Clamp(current.X + deltaX, visible.MinX, visible.MinX + visible.Width - 1L);
        var y = Math.Clamp(current.Y + deltaY, visible.MinY, visible.MinY + visible.Height - 1L);
        _inspectionCursor = new WorldAddress(current.Stratum, x, y);
        _presentationStatus = $"Inspecting {_inspectionCursor}. Enter selects; no time passes.";
        RefreshPresentation(forceMap: true);
    }

    private bool TryMapAddress(Vector2 viewportPosition, out WorldAddress address)
    {
        if (viewportPosition.X < 0 || viewportPosition.Y < 0 ||
            viewportPosition.X >= ChronicleHud.MapWidth || viewportPosition.Y >= ChronicleHud.MapHeight)
        {
            address = default;
            return false;
        }

        var visible = CurrentVisibleBounds();
        var column = Math.Clamp((int)(viewportPosition.X / ChronicleHud.MapDisplayCellSize), 0, visible.Width - 1);
        var row = Math.Clamp((int)(viewportPosition.Y / ChronicleHud.MapDisplayCellSize), 0, visible.Height - 1);
        address = new WorldAddress(
            _simulation.State.Address.Stratum,
            visible.MinX + column,
            visible.MinY + row);
        return true;
    }

    private WorldRectangle CurrentVisibleBounds() => VisualViewportBounds.Centered(
        _simulation.State.Address.X,
        _simulation.State.Address.Y,
        ChronicleHud.MapColumns,
        ChronicleHud.MapRows);

    private WorldCell? InspectedCell()
    {
        var address = _isInspecting ? _inspectionCursor : _inspectionSelection;
        if (address is not { } inspected ||
            !string.Equals(inspected.Stratum, _simulation.State.Address.Stratum, StringComparison.Ordinal))
        {
            return null;
        }

        var visible = CurrentVisibleBounds();
        if (inspected.X < visible.MinX || inspected.X >= visible.MinX + visible.Width ||
            inspected.Y < visible.MinY || inspected.Y >= visible.MinY + visible.Height)
        {
            return null;
        }

        var area = WorldArea.Generate(
            _simulation.State,
            inspected.Stratum,
            new WorldRectangle(inspected.X, inspected.Y, 1, 1));
        return area.Cells[0];
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
        _presentationStatus = "Created a strict v9 Chronicle.";
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

    private static ChronicleSimulation CreateGoal6BTestingStart()
    {
        var state = ChronicleState.Begin(InitialSeed) with
        {
            WorldGrammarVersion = 5,
            Agents = default,
        };
        var simulation = new ChronicleSimulation(state);
        RequireApplied(
            simulation,
            new ChooseHereIntent(),
            "The retained Goal 6B fixture could not establish Home.");
        return simulation;
    }

    private void PrepareGoal7AWelcomeUat()
    {
        var prepared = CompleteGoal7AResonatorForFixture();
        _simulation = new ChronicleSimulation(prepared.State with { Speed = ChronicleSpeed.Paused });
        _renderKey = string.Empty;
        SaveChronicle();
        _presentationStatus = "UAT A ready: Tamar approaches Home. Space resumes one Heartbeat at a time.";
    }

    private void PrepareGoal7AReplacementUat()
    {
        var prepared = CompleteGoal7AWelcomeForFixture();
        prepared = AtForFixture(prepared, prepared.State.CurrentBellAddress);
        RequireApplied(
            prepared,
            new EndIncarnationAtBell(),
            "The Goal 7A replacement fixture could not end the first Incarnation at the Bell.");
        _simulation = prepared;
        _renderKey = string.Empty;
        SaveChronicle();
        _presentationStatus = "UAT B ready: the prior body ended away from Home; Tamar remains Home's Guest.";
    }

    private void PrepareGoal7BUat(WordId activeVerb)
    {
        var prepared = CompleteGoal7AWelcomeForFixture();
        var learned = prepared.State with
        {
            Codex = prepared.State.Codex.Learn(WordIds.Suggest).Learn(WordIds.Command),
            Speed = ChronicleSpeed.Paused,
        };
        prepared = new ChronicleSimulation(learned);
        RequireApplied(
            prepared,
            new AttuneExpression(activeVerb, []),
            $"The Goal 7B fixture could not attune {activeVerb.Value}.");
        _simulation = new ChronicleSimulation(prepared.State with
        {
            Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
            Speed = ChronicleSpeed.Paused,
        });
        _isInspecting = false;
        _inspectionCursor = null;
        _inspectionSelection = null;
        _renderKey = string.Empty;
        SaveChronicle();
        _presentationStatus =
            $"Goal 7B ready: {WordCatalogue.Get(activeVerb).DisplayName} attuned. Press I to inspect; move south to reach Tamar.";
    }

    private static ChronicleSimulation CompleteGoal7AResonatorForFixture()
    {
        var simulation = CreateTestingStart();
        RequireApplied(simulation, new ReadBurnPrimer(), "The Goal 7A fixture could not read the Burn Primer.");
        simulation = AtForFixture(
            simulation,
            new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3));
        RequireApplied(
            simulation,
            new BeginPowerCommitment(PowerCommitmentKind.Extract),
            "The Goal 7A fixture could not begin Lode extraction.");
        AdvanceActiveForFixture(simulation, 2);
        RequireApplied(simulation, new LiftResonantLode(), "The Goal 7A fixture could not lift the Lode.");
        simulation = AtForFixture(simulation, ChronicleState.AcceptedHomeFixtureAddress);
        RequireApplied(
            simulation,
            new BeginPowerCommitment(PowerCommitmentKind.Build),
            "The Goal 7A fixture could not begin Resonator construction.");
        AdvanceActiveForFixture(simulation, 3);
        return simulation;
    }

    private static ChronicleSimulation CompleteGoal7AWelcomeForFixture()
    {
        var simulation = CompleteGoal7AResonatorForFixture();
        AdvanceActiveForFixture(simulation, 3);
        var identity = simulation.AgentContext.PrimaryAgent?.Identity
            ?? throw new InvalidOperationException("The Goal 7A fixture did not promote Tamar.");
        RequireApplied(simulation, new OfferWelcome(identity), "The Goal 7A fixture could not offer welcome.");
        AdvanceActiveForFixture(simulation, 1);
        return simulation;
    }

    private static ChronicleSimulation AtForFixture(
        ChronicleSimulation simulation,
        WorldAddress address) =>
        new(simulation.State with { Address = address, Speed = ChronicleSpeed.Paused });

    private static void AdvanceActiveForFixture(ChronicleSimulation simulation, int ticks)
    {
        if (simulation.State.Speed != ChronicleSpeed.Slow)
        {
            RequireApplied(
                simulation,
                new SetChronicleSpeed(ChronicleSpeed.Slow),
                "The Goal 7A fixture could not resume at Slow speed.");
        }

        for (var index = 0; index < ticks; index++)
        {
            simulation.AdvanceOneTick();
        }
    }

    private void IssueDirective(DirectiveKind directive)
    {
        var context = _simulation.DirectiveContext;
        if (context.PrimaryAgentIdentity is not { } identity)
        {
            _presentationStatus = "No consequential recipient is available.";
            RefreshPresentation();
            return;
        }

        var action = context.Actions.Single(candidate => candidate.Directive == directive);
        if (!action.Available)
        {
            _presentationStatus = DirectivePresentation.ActionUnavailable(action);
            RefreshPresentation();
            return;
        }

        Issue(new DeliverDirective(0, identity, directive));
    }

    private bool HasDirectiveSurface() =>
        _simulation.State.Codex.Contains(WordIds.Suggest) ||
        _simulation.State.Codex.Contains(WordIds.Command) ||
        _simulation.DirectiveContext.Pending is not null ||
        _simulation.DirectiveContext.Memories.Count > 0;

    private static void RequireApplied(
        ChronicleSimulation simulation,
        ChronicleCommand command,
        string message)
    {
        if (!simulation.Apply(command).Applied)
        {
            throw new InvalidOperationException(message);
        }
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
            _presentationStatus = "Saved strict Chronicle v9 to user://.";
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
        var agents = _simulation.AgentContext;
        var agent = agents.PrimaryAgent;
        var directives = _simulation.DirectiveContext;
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
            power.Attunement.NextAttunementCapacity,
            agent?.Identity,
            agent?.Address,
            agent?.Presence,
            agent?.Need.Status,
            agent?.HomeRelationship.Kind,
            agent?.RoadRollAddress,
            agent?.Blocker,
            directives.Pending?.Directive,
            directives.Pending?.ObjectiveAddress,
            directives.Memories.LastOrDefault()?.ResolvedTick,
            directives.Memories.LastOrDefault()?.Response,
            _isInspecting,
            _inspectionCursor,
            _inspectionSelection);
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

        var agent = _simulation.AgentContext.PrimaryAgent;
        if (agent is { Blocker: not AgentBlockerKind.None, NextAddress: { } blockedAddress })
        {
            emphases.Add(new VisualPresentationEmphasis(
                blockedAddress,
                VisualPresentationEmphasisKind.AgentBlockedRoute));
        }

        var directive = _simulation.DirectiveContext;
        if (directive.Pending is { } pending && agent is not null)
        {
            emphases.Add(new VisualPresentationEmphasis(
                agent.Address,
                VisualPresentationEmphasisKind.PendingAction));
            emphases.Add(new VisualPresentationEmphasis(
                pending.ObjectiveAddress,
                VisualPresentationEmphasisKind.PendingAction));
        }

        var inspectionAddress = _isInspecting ? _inspectionCursor : _inspectionSelection;
        IReadOnlyList<WorldAddress> selectedAddresses = inspectionAddress is { } inspected
            ? [inspected]
            : selected is null
                ? []
                : [selected.Address];

        var plan = VisualGrammar.Compose(new VisualCompositionInput(
            area,
            visible,
            state.Seed,
            _visualPack,
            _visualPack.StyleVersion,
            state.HasLivingIncarnation ? state.Address : null,
            combat.Targets.Select(target => target.Address).ToArray(),
            selectedAddresses,
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
        var agents = _simulation.AgentContext;
        var agent = agents.PrimaryAgent;
        var directives = _simulation.DirectiveContext;
        var inspected = InspectedCell();
        var urgentCombat =
            combat.Danger.IsImmediate ||
            combat.PendingAction is not null ||
            combat.Preparation is not null ||
            combat.Recovery.RemainingTicks > 0 ||
            combat.MireBrute?.IsBurning == true;
        var showsInspection = _isInspecting && inspected is not null;
        var showsDirectiveContext = HasDirectiveSurface() && agent is not null &&
                                    !urgentCombat && power.Commitment is null && !showsInspection;
        var showsAgentContext = agent is not null && !urgentCombat && power.Commitment is null &&
                                !showsInspection && !showsDirectiveContext;
        var showsCombatContext =
            !showsInspection && !showsDirectiveContext && !showsAgentContext &&
            (state.WorldGrammarVersion is not (5 or 6) || urgentCombat);
        if (showsInspection)
        {
            targetHeading = InspectionPresentation.Heading(inspected!.Value);
            targetFacts = InspectionPresentation.Facts(inspected.Value);
            targetOutcome = InspectionPresentation.Decision(
                inspected.Value,
                _inspectionSelection == inspected.Value.Address);
        }
        else if (showsDirectiveContext)
        {
            targetHeading = DirectivePresentation.Heading(agent!);
            targetFacts = DirectivePresentation.Facts(state, agent!, directives);
            targetOutcome = DirectivePresentation.Decision(
                directives,
                agent!,
                state.Speed == ChronicleSpeed.Paused);
        }
        else if (showsAgentContext)
        {
            targetHeading = AgentPresentation.Heading(agent!);
            targetFacts = AgentPresentation.Facts(agent!);
            targetOutcome = AgentPresentation.Decision(agent!, state.Speed == ChronicleSpeed.Paused);
        }
        else if (!showsCombatContext)
        {
            targetHeading = HoldingPresentation.MaterialHeading(power);
            targetFacts = HoldingPresentation.MaterialFacts(power);
            targetOutcome = HoldingPresentation.MaterialDecision(power);
        }
        var forecast = power.Commitment is not null
            ? HoldingPresentation.CommitmentForecast(power)
            : showsInspection
            ? InspectionPresentation.Forecast(inspected!.Value)
            : showsDirectiveContext
            ? DirectivePresentation.Forecast(directives, agent!, state.Speed == ChronicleSpeed.Paused)
            : showsAgentContext
            ? AgentPresentation.Forecast(agent!, state.Speed == ChronicleSpeed.Paused)
            : !showsCombatContext
            ? HoldingPresentation.MaterialForecast(power)
            : combat.Forecast.Count == 0
            ? [EmptyForecastReason(state, combat)]
            : combat.Forecast
                .Take(4)
                .Select(CompactForecastText)
                .ToArray();
        var logged = combat.RecentResults.Select(LogText)
            .Concat(agents.RecentEvents.Select(item => AgentPresentation.Log(
                item,
                agents.Agents.FirstOrDefault(candidate => candidate.Identity == item.AgentIdentity)
                    ?.DisplayName ?? "An Agent")))
            .Concat(directives.RecentEvents.Select(item => DirectivePresentation.Log(
                item,
                agents.Agents.FirstOrDefault(candidate => candidate.Identity == item.AgentIdentity)
                    ?.DisplayName ?? "An Agent")))
            .ToArray();
        var messages = logged
            .Append(_presentationStatus)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .TakeLast(3)
            .ToArray();
        var targets = (showsCombatContext ? combat.Targets : [])
            .Select(target => new ChronicleHudTarget(
                target.Kind.ToString().ToLowerInvariant(),
                target.DisplayName.ToUpperInvariant(),
                target.Address,
                target.Kind == _selectedTargetKind))
            .ToArray();
        return new ChronicleHudSnapshot(
            $"INCARNATION #{state.IncarnationId}",
            $"{state.Speed.ToString().ToUpperInvariant()} · H{state.Tick}",
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
            showsCombatContext ? selected?.HitPoints : null,
            showsCombatContext ? selected?.MaximumHitPoints : null,
            $"LOAD {power.Attunement.CurrentUsedLoad}/{power.Attunement.CapacityAtLastAttunement?.ToString() ?? "—"}",
            $"CLEAVER {combat.Equipment.WeaponDamage}/{combat.Equipment.WeaponCadence} · " +
            $"JACK −{combat.Equipment.ArmorReduction} · WARD +{combat.Equipment.MaximumHitPointBonus}",
            showsInspection
                ? "INSPECTION · READ ONLY"
                : showsDirectiveContext
                    ? DirectivePresentation.Banner(directives)
                    : showsAgentContext
                        ? AgentPresentation.Banner(agent!)
                        : PowerHeadingState(power),
            showsInspection
                ? InspectionPresentation.Checklist(
                    inspected!.Value,
                    _inspectionSelection == inspected.Value.Address)
                : showsDirectiveContext
                    ? DirectivePresentation.Checklist(state, directives, agent!, state.Speed == ChronicleSpeed.Paused)
                    : showsAgentContext
                ? AgentPresentation.Checklist(agent!, state.Speed == ChronicleSpeed.Paused)
                : HoldingPresentation.Checklist(power),
            showsInspection
                ? "READ ONLY · I / MOUSE · WASD CURSOR · ENTER · ESC"
                : showsDirectiveContext
                    ? $"CODEX · SUGGEST + COMMAND · ACTIVE {state.ActiveLoadout[0].DisplayName}"
                    : showsAgentContext
                ? $"CAUSE · RESONANT LODE FROM {agent!.OriginAddress} · " +
                  $"HOME · {agent.HomeRelationship.Kind.ToString().ToUpperInvariant()}"
                : CompactPowerCapacity(power.Attunement),
            string.Empty,
            showsCombatContext,
            showsInspection || showsDirectiveContext || showsAgentContext,
            !state.HasLivingIncarnation,
            !state.HasLivingIncarnation ? AgentPresentation.ReplacementStatus(agent) : string.Empty,
            state.Speed == ChronicleSpeed.Paused);
    }

    private IReadOnlyList<ChronicleHudAction> BuildActions(
        ChronicleState state,
        CombatContextSnapshot combat,
        TargetPreviewSnapshot? selected)
    {
        if (_isInspecting)
        {
            return [];
        }

        if (HasDirectiveSurface() && _simulation.AgentContext.PrimaryAgent is not null)
        {
            return BuildGoal7BActions(state);
        }

        if (state.WorldGrammarVersion == 6 && _simulation.AgentContext.PrimaryAgent is not null)
        {
            return BuildGoal7AActions(state, combat, selected);
        }

        if (state.WorldGrammarVersion is 5 or 6)
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

    private IReadOnlyList<ChronicleHudAction> BuildGoal7BActions(ChronicleState state)
    {
        var context = _simulation.DirectiveContext;
        var identity = context.PrimaryAgentIdentity
            ?? throw new InvalidOperationException("Goal 7B actions require a consequential recipient.");
        var alive = state.HasLivingIncarnation && state.Intent != OpeningIntent.Unchosen;
        var rest = context.Actions.Single(action => action.Directive == DirectiveKind.RestByRoadRoll);
        var danger = context.Actions.Single(action => action.Directive == DirectiveKind.ApproachMireBrute);
        var clockCommand = state.Speed == ChronicleSpeed.Paused
            ? (ChronicleCommand)new SetChronicleSpeed(ChronicleSpeed.Slow)
            : new SetChronicleSpeed(ChronicleSpeed.Paused);
        var primary = context.Pending is not null
            ? new ChronicleHudAction(
                "directive-withdraw",
                "[P] WITHDRAW",
                new WithdrawDirective(identity),
                alive,
                "BEFORE ANSWER · NO MEMORY")
            : new ChronicleHudAction(
                "directive-rest",
                "[P] REST",
                rest.Available ? new DeliverDirective(0, identity, rest.Directive) : null,
                rest.Available,
                DirectivePresentation.ActionDetail(rest));

        return
        [
            primary,
            new(
                "directive-danger",
                "[X] BRUTE",
                danger.Available ? new DeliverDirective(0, identity, danger.Directive) : null,
                danger.Available,
                DirectivePresentation.ActionDetail(danger)),
            new("move-north", "[W]", new MoveIncarnation(0, -1), alive),
            new("move-west", "[A]", new MoveIncarnation(-1, 0), alive),
            new("move-south", "[S]", new MoveIncarnation(0, 1), alive),
            new("move-east", "[D]", new MoveIncarnation(1, 0), alive),
            new(
                "clock",
                state.Speed == ChronicleSpeed.Paused ? "[SPACE] RESUME" : "[SPACE] PAUSE",
                clockCommand,
                alive,
                context.Pending is { } pending ? $"ANSWER H{pending.ResolvesAtTick}" : "SLOW HEARTBEATS"),
        ];
    }

    private IReadOnlyList<ChronicleHudAction> BuildGoal7AActions(
        ChronicleState state,
        CombatContextSnapshot combat,
        TargetPreviewSnapshot? selected)
    {
        var agent = _simulation.AgentContext.PrimaryAgent
            ?? throw new InvalidOperationException("Goal 7A actions require a consequential Agent.");
        var actionKind = agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered
            ? AgentActionKind.WithdrawWelcome
            : AgentActionKind.OfferWelcome;
        var agentAction = _simulation.AgentContext.Actions.Single(action => action.Kind == actionKind);
        ChronicleCommand agentCommand = actionKind == AgentActionKind.OfferWelcome
            ? new OfferWelcome(agent.Identity)
            : new WithdrawWelcome(agent.Identity);
        var alive = state.HasLivingIncarnation && state.Intent != OpeningIntent.Unchosen;
        var safe = alive && !combat.Danger.IsImmediate;
        var power = _simulation.PowerComesHomeContext;
        var combinedActive = combat.Expression.Modifiers.SequenceEqual([WordIds.Quickly, WordIds.Lasting]);
        var canAttune = safe && power.BurnPrimer.IsRead && power.Commitment is null &&
                        power.Lode.Disposition != ResonantLodeDisposition.Carried;
        var clockCommand = state.Speed == ChronicleSpeed.Paused
            ? (ChronicleCommand)new SetChronicleSpeed(ChronicleSpeed.Slow)
            : new SetChronicleSpeed(ChronicleSpeed.Paused);
        var weaponCommand = combat.Danger.IsImmediate
            ? (ChronicleCommand)new SetWeaponStance(!combat.WeaponStanceActive)
            : new ConfigureEngagementPlan(!combat.EngagementPlan.OpenWithWeaponStance);
        var cancelOrSkip = combat.PendingAction is not null || combat.Preparation is not null
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

        return
        [
            new(
                "attune-combined",
                combinedActive ? "◆ ATTUNED 12" : "[G] ATTUNE 12",
                new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting]),
                canAttune,
                combinedActive ? "BURN + QUICK + LAST" : $"NEXT CAPACITY {power.Attunement.NextAttunementCapacity}"),
            new(
                "agent-primary",
                $"[P] {AgentPresentation.ActionLabel(agent)}",
                agentAction.Available ? agentCommand : null,
                agentAction.Available,
                AgentPresentation.ActionDetail(agentAction)),
            new("move-north", "[W]", new MoveIncarnation(0, -1), alive),
            new("move-west", "[A]", new MoveIncarnation(-1, 0), alive),
            new("move-south", "[S]", new MoveIncarnation(0, 1), alive),
            new("move-east", "[D]", new MoveIncarnation(1, 0), alive),
            new(
                "burn",
                "[B] BURN",
                selected is null ? null : new PrepareBurn(selected.Address),
                alive && selected?.CanBurn == true && combat.Recovery.RemainingTicks == 0,
                selected is null ? "NO TARGET" : $"{selected.PreparationTicks} PREP · {selected.ConsequenceTicks} BURN"),
            new(
                "weapon",
                combat.Danger.IsImmediate ? "[V] CLEAVER" : "[V] PLAN",
                weaponCommand,
                alive,
                combat.Danger.IsImmediate
                    ? combat.WeaponStanceActive ? "ACTIVE" : "LOWERED"
                    : combat.EngagementPlan.OpenWithWeaponStance ? "OPEN ACTIVE" : "OPEN LOWERED"),
            new(
                "clock",
                state.Speed == ChronicleSpeed.Paused ? "[SPACE] RESUME" : "[SPACE] PAUSE",
                clockCommand,
                alive,
                agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered
                    ? $"ANSWER H{agent.NextHeartbeat}"
                    : "SLOW HEARTBEATS"),
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
        var requiredLoad = power.Attunement.DesiredExpressionLoad;
        var nextCapacity = power.Attunement.NextAttunementCapacity;
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
                        : nextCapacity < requiredLoad
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
            new(
                "attune-combined",
                combinedActive
                    ? $"◆ ATTUNED {requiredLoad}/{power.Attunement.CapacityAtLastAttunement ?? nextCapacity}"
                    : $"[G] ATTUNE {requiredLoad}/{nextCapacity}",
                new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting]),
                canRequestAttunement,
                combinedDetail),
            new(
                "power-primary",
                primary is null ? "[P] POWER" : $"[P] {HoldingPresentation.ActionLabel(primary)}",
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
            HearthResonatorPhase.Intact when power.Attunement.CapacityAtLastAttunement != power.Attunement.DesiredExpressionLoad =>
                $"HEARTH RESONATOR · +{power.Attunement.SourceContribution} LOAD READY",
            HearthResonatorPhase.Intact =>
                $"HEARTH RESONATOR · {power.Attunement.DesiredExpressionLoad}-LOAD POWER",
            HearthResonatorPhase.Damaged =>
                $"HEARTH RESONATOR · DAMAGED, +{power.Attunement.SourceContribution} STILL READY",
            HearthResonatorPhase.Destroyed =>
                $"HEARTH RESONATOR · DESTROYED, NEXT LOAD {power.Attunement.NextAttunementCapacity}",
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

    private static string LogText(CombatResultSnapshot result) => result.Kind switch
    {
        CombatResultKind.Movement when result.Address is { } address =>
            $"H{result.Tick}: Moved to {address}.",
        _ => $"H{result.Tick}: {result.Text}",
    };

    private static string EmptyForecastReason(
        ChronicleState state,
        CombatContextSnapshot combat)
    {
        if (!state.HasLivingIncarnation)
        {
            return "— No forecast: this body has ended. Choose a new body.";
        }

        if (state.Speed == ChronicleSpeed.Paused)
        {
            return "— PAUSED · nothing resolves until the next Heartbeat.";
        }

        return combat.Danger.IsImmediate
            ? "— No timed event yet: no Invocation, Weapon action, or work is pending."
            : "— No timed event: nothing is pending and no threat is engaged.";
    }

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
        var interruption = !target.CanBurn
            ? "None — Burn cannot begin"
            : combat.Danger.IsImmediate && target.Kind == CombatTargetKind.MireBrute
                ? "Mire Brute swing before release"
                : "Target leaves range before release";
        var prevention = combat.Preparation is null
            ? "Nothing"
            : "Cleaver strike";
        return $"STATE  {eligibility} · {eligibilityDetail}\n" +
               (target.CanBurn
                   ? $"WHEN   {when} · BURN {target.ConsequenceTicks}\n"
                   : "WHEN   Rejected before time begins\n") +
               $"INTERRUPTS   {interruption}\n" +
               $"PREVENTS   {prevention}\n" +
               $"RECOVERY   {combat.Recovery.RemainingTicks}/{target.RecoveryTicks}";
    }

    private async Task RunGoal7BVisualAcceptance()
    {
        PrepareGoal7BUat(WordIds.Suggest);
        _presentationStatus = string.Empty;
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        var initialState = _simulation.State;
        var tamar = _simulation.AgentContext.PrimaryAgent
            ?? throw new InvalidOperationException("Goal 7B requires Tamar.");

        TriggerInput(InspectAction);
        TriggerInput(MoveWestAction);
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        TriggerInput("ui_accept");
        Verify(
            _simulation.State == initialState &&
            _inspectionCursor == tamar.RoadRollAddress &&
            _inspectionSelection == tamar.RoadRollAddress,
            "Keyboard inspection must select the visible road-roll without moving the Incarnation or advancing time.");
        await CaptureGoal7BProof("inspected-road-roll");
        TriggerInput("ui_cancel");

        var visible = CurrentVisibleBounds();
        var tamarScreen = new Vector2(
            (float)((tamar.Address.X - visible.MinX + 0.5) * ChronicleHud.MapDisplayCellSize),
            (float)((tamar.Address.Y - visible.MinY + 0.5) * ChronicleHud.MapDisplayCellSize));
        _UnhandledInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = tamarScreen,
        });
        Verify(
            _simulation.State == initialState && _inspectionSelection == tamar.Address &&
            InspectedCell()?.Subjects.Any(subject => subject.Kind == WorldSubjectKind.Agent) == true,
            "Mouse inspection must select Tamar's semantic cell without mutating the Chronicle.");
        TriggerInput("ui_cancel");
        Verify(
            _simulation.DirectiveContext.Actions.Single(action =>
                action.Directive == DirectiveKind.RestByRoadRoll).AvailabilityReason ==
            DirectiveAvailabilityReason.RecipientOutOfReach,
            "Remote inspection must leave delivery disabled with an explicit physical-reach reason.");
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        Verify(
            _simulation.DirectiveContext.Actions.Single(action =>
                action.Directive == DirectiveKind.RestByRoadRoll).Available,
            $"Three explicit southward steps must bring the Incarnation into delivery reach without a queued extra move; " +
            $"address={_simulation.State.Address}, Tamar={_simulation.State.Agents[0].Address}, " +
            $"reason={_simulation.DirectiveContext.Actions.Single(action => action.Directive == DirectiveKind.RestByRoadRoll).AvailabilityReason}.");
        await CaptureGoal7BProof("safe-suggest-preview");

        var beforeMouse = _simulation.State;
        Press(ActionButton("directive-rest"));
        var mousePending = ChronicleSaveCodec.Serialize(_simulation.State);
        _simulation = new ChronicleSimulation(beforeMouse);
        _presentationStatus = string.Empty;
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        await PushPhysicalKey(Key.P);
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == mousePending &&
            _simulation.DirectiveContext.Pending is not null &&
            _simulation.State.Speed == ChronicleSpeed.Paused,
            "Mouse and physical P must deliver the same safe Suggestion without spending a Heartbeat.");
        await CaptureGoal7BProof("safe-pending");

        await PushPhysicalKey(Key.P);
        Verify(
            _simulation.DirectiveContext.Pending is null &&
            _simulation.DirectiveContext.Memories.Count == 0,
            "P must withdraw the pending Suggestion without fabricating a response memory.");
        await PushPhysicalKey(Key.P);
        ActionButton("directive-withdraw").GrabFocus();
        await PushPhysicalKey(Key.Space);
        Verify(
            _simulation.State.Speed == ChronicleSpeed.Slow &&
            _simulation.DirectiveContext.Pending is not null,
            "Focused Space must resume consideration without activating the focused withdrawal button.");
        AdvanceAndRefresh();
        var accepted = _simulation.DirectiveContext.Memories.Single();
        Verify(
            accepted.Response == DirectiveResponseKind.Accepted &&
            _simulation.State.Agents[0].Address == tamar.RoadRollAddress &&
            _simulation.State.Speed == ChronicleSpeed.Paused,
            "One active Heartbeat must accept the Suggestion, move Tamar once, remember it, and pause once.");
        await CaptureGoal7BProof("accepted-movement");

        await PushPhysicalKey(Key.Space);
        AdvanceAndRefresh();
        var acceptedCount = _simulation.DirectiveContext.Memories.Count;
        TriggerInput(MoveEastAction);
        Verify(
            _simulation.State.Speed == ChronicleSpeed.Slow &&
            _simulation.DirectiveContext.Memories.Count == acceptedCount,
            "The following Heartbeat and movement must not latch auto-pause, repeat the answer, or queue Agent movement.");

        _simulation = AtForFixture(
            _simulation,
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 4));
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        var beforeRejection = _simulation.State;
        await PushPhysicalKey(Key.X);
        Verify(
            _simulation.State == beforeRejection &&
            _simulation.DirectiveContext.Memories.Count == acceptedCount &&
            _presentationStatus.Contains("requires Command", StringComparison.Ordinal),
            "Suggest must reject the dangerous Directive before consideration, with no time or memory mutation.");
        await CaptureGoal7BProof("dangerous-suggest-rejection");

        PrepareGoal7BUat(WordIds.Command);
        _presentationStatus = string.Empty;
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        await PushPhysicalKey(Key.X);
        Verify(
            _simulation.DirectiveContext.Pending is
            {
                Verb: var pendingVerb,
                Directive: DirectiveKind.ApproachMireBrute,
            } && pendingVerb == WordIds.Command &&
            _simulation.State.Speed == ChronicleSpeed.Paused,
            "Command must deliver the dangerous Directive as consideration, not immediate movement or obedience.");
        await CaptureGoal7BProof("dangerous-command-pending");

        var savedPending = ChronicleSaveCodec.Serialize(_simulation.State);
        Press(_hud.GetNode<Button>("SaveAction"));
        Press(_hud.GetNode<Button>("LoadAction"));
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == savedPending &&
            _simulation.DirectiveContext.Pending is not null && SaveVersion() == 9,
            "Actual Godot save/load must restore the exact strict-v9 pending Command and paused Clock.");
        ActionButton("directive-withdraw").GrabFocus();
        await PushPhysicalKey(Key.Space);
        AdvanceAndRefresh();
        var refused = _simulation.DirectiveContext.Memories.Single();
        Verify(
            refused is
            {
                Response: DirectiveResponseKind.Refused,
                Reason: DirectiveResponseReason.GuestHasNoViolentCommitment,
            } &&
            _simulation.State.Agents[0].Address == tamar.Address &&
            _simulation.State.Speed == ChronicleSpeed.Paused,
            "Tamar must refuse once for the Guest/no-violent-commitment reason without moving either actor.");
        await CaptureGoal7BProof("refusal");

        await PushPhysicalKey(Key.Space);
        AdvanceAndRefresh();
        var refusalCount = _simulation.DirectiveContext.Memories.Count;
        TriggerInput(MoveEastAction);
        Verify(
            _simulation.State.Speed == ChronicleSpeed.Slow &&
            _simulation.DirectiveContext.Memories.Count == refusalCount,
            "The Heartbeat after refusal must not repeat, retry, or reacquire pause.");
        var savedRefusal = ChronicleSaveCodec.Serialize(_simulation.State);
        Press(_hud.GetNode<Button>("SaveAction"));
        Press(_hud.GetNode<Button>("LoadAction"));
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == savedRefusal && SaveVersion() == 9,
            "Strict v9 must restore the refusal memory without rewriting Tamar's Guest relationship.");
        await CaptureGoal7BProof("restored-refusal");

        GD.Print("GOAL7B VISUAL ACCEPTANCE PASS captures=8 save=9 inspection=keyboard+mouse directive=agency");
    }

    private async Task CaptureGoal7BProof(string stage)
    {
        RefreshPresentation(forceMap: true);
        var agent = _simulation.AgentContext.PrimaryAgent
            ?? throw new InvalidOperationException("Goal 7B capture requires Tamar.");
        var plan = _map.CurrentPlan
            ?? throw new InvalidOperationException("Goal 7B capture requires a rendered map plan.");
        var pending = _simulation.DirectiveContext.Pending;
        var inspectedStage = stage == "inspected-road-roll";
        Verify(
            _hud.TargetOutcome.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length <= 5 &&
            _hud.TargetOutcome.Text.Contains("NEXT", StringComparison.Ordinal) &&
            _hud.TargetOutcome.Text.Contains("WHEN", StringComparison.Ordinal) &&
            _hud.TargetOutcome.Text.Contains("INTERRUPTS", StringComparison.Ordinal) &&
            _hud.TargetOutcome.Text.Contains("PREVENTS", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.StartsWith("CHECKLIST · ", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length <= 5 &&
            plan.Marks.Any(mark => mark.Address == agent.Address &&
                mark.VisualId.StartsWith("agent.", StringComparison.Ordinal)) &&
            plan.Marks.Any(mark => mark.VisualId == "place.wayfarer-road-roll.laid") &&
            plan.Marks.Any(mark => mark.VisualId == "source.hearth-resonator.intact") &&
            plan.Marks.Any(mark => mark.VisualId == "subject.mire-brute.living") &&
            (!inspectedStage || plan.Marks.Any(mark =>
                mark.Address == agent.RoadRollAddress && mark.VisualId == "emphasis.selection")) &&
            (pending is null ||
             plan.Marks.Any(mark => mark.Address == agent.Address &&
                 mark.VisualId == "emphasis.action.pending") &&
             plan.Marks.Any(mark => mark.Address == pending.ObjectiveAddress &&
                 mark.VisualId == "emphasis.action.pending")) &&
            ChronicleHud.MapWidth * ChronicleHud.MapHeight >=
                ChronicleHud.CanvasWidth * ChronicleHud.CanvasHeight * 3 / 4,
            $"Goal 7B '{stage}' must align the concise decision surface with Tamar, objective, Source, Brute, inspection, and pending emphasis.");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Verify(
            _hud.PowerStatus.GetMinimumSize().Y <= _hud.PowerStatus.Size.Y &&
            _hud.TargetHeading.GetMinimumSize().X <= _hud.TargetHeading.Size.X &&
            _hud.TargetFacts.GetMinimumSize().Y <= _hud.TargetFacts.Size.Y &&
            _hud.ConsequenceRows.Where(row => row.Visible)
                .All(row => row.GetMinimumSize().Y <= row.Size.Y) &&
            _hud.ForecastReadout.GetMinimumSize().Y <= _hud.ForecastReadout.Size.Y &&
            _hud.MessageReadout.GetMinimumSize().Y <= _hud.MessageReadout.Size.Y &&
            _hud.ActionButtons.All(button =>
                button.FocusMode == Control.FocusModeEnum.All &&
                button.GetThemeColor("font_disabled_color").A >= 0.75f),
            $"Goal 7B '{stage}' must not clip or overlap identity, timing, reason, memory, or action feedback.");
        RenderingServer.ForceDraw(swapBuffers: false);
        var image = GetViewport().GetTexture().GetImage();
        Verify(
            image is not null && image.GetWidth() == ChronicleHud.CanvasWidth &&
            image.GetHeight() == ChronicleHud.CanvasHeight,
            "Goal 7B native HUD proof must capture the exact 1600 × 900 viewport.");
        Verify(
            image!.SavePng($"user://goal7b-{stage}-hud.png") == Error.Ok,
            $"Goal 7B '{stage}' native HUD capture must be writable.");
        GD.Print($"GOAL7B HUD CAPTURE PASS stage={stage} size=1600x900");
    }

    private async Task RunGoal7AVisualAcceptance()
    {
        _simulation = CompleteGoal7AResonatorForFixture();
        _selectedTargetKind = CombatTargetKind.MireBrute;
        _presentationStatus = string.Empty;
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);

        var approaching = _simulation.AgentContext.PrimaryAgent
            ?? throw new InvalidOperationException("Goal 7A did not promote its consequential Agent.");
        Verify(
            approaching is
            {
                DisplayName: "Tamar Venn",
                Presence: AgentPresenceState.ApproachingHome,
                Need.Status: AgentNeedStatus.Seeking,
                HomeRelationship.Kind: AgentHomeRelationshipKind.Unfamiliar,
            } &&
            approaching.Address.X == approaching.WaitingAddress.X - 3,
            "The rendered Goal 7A journey must begin with generated Tamar three steps west of Home's waiting place.");
        await CaptureGoal7AProof("approaching");

        TriggerInput(PauseAction);
        var pausedState = ChronicleSaveCodec.Serialize(_simulation.State);
        AdvanceAndRefresh();
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == pausedState,
            "Pause must freeze Tamar, intent, and Chronicle time before the first approach step.");

        await PushPhysicalKey(Key.Space);
        var beforeFirstStep = _simulation.AgentContext.PrimaryAgent!.Address;
        var beforeFirstTick = _simulation.State.Tick;
        AdvanceAndRefresh();
        Verify(
            _simulation.State.Tick == beforeFirstTick + 1 &&
            _simulation.AgentContext.PrimaryAgent!.Address.X == beforeFirstStep.X + 1,
            "One focused-safe Space resume and one Heartbeat must move Tamar exactly one cardinal step.");

        for (var index = 0; index < 2; index++)
        {
            TriggerInput(PauseAction);
            var beforePause = _simulation.AgentContext.PrimaryAgent!.Address;
            AdvanceAndRefresh();
            Verify(
                _simulation.AgentContext.PrimaryAgent!.Address == beforePause,
                "A pause between Agent steps must not queue a catch-up move.");
            await PushPhysicalKey(Key.Space);
            AdvanceAndRefresh();
        }

        var waiting = _simulation.AgentContext.PrimaryAgent!;
        Verify(
            waiting.Presence == AgentPresenceState.WaitingAtHome &&
            waiting.Address == waiting.WaitingAddress &&
            _simulation.State.Speed == ChronicleSpeed.Paused,
            "Arrival must land on the visible waiting place and acquire exactly one pause.");
        await CaptureGoal7AProof("waiting");

        var arrivalTick = _simulation.State.Tick;
        var arrivalEvents = _simulation.AgentContext.RecentEvents.Count(item =>
            item.Kind == AgentEventKind.Arrived);
        await PushPhysicalKey(Key.Space);
        AdvanceAndRefresh();
        Verify(
            _simulation.State.Tick == arrivalTick + 1 &&
            _simulation.State.Speed == ChronicleSpeed.Slow &&
            _simulation.AgentContext.PrimaryAgent == waiting &&
            _simulation.AgentContext.RecentEvents.Count(item => item.Kind == AgentEventKind.Arrived) == arrivalEvents,
            "Resuming after arrival must advance normally without a second pause, repeated arrival, or queued step.");

        var beforeOffer = _simulation.State;
        Press(ActionButton("agent-primary"));
        var mouseOfferState = ChronicleSaveCodec.Serialize(_simulation.State);
        var mouseOfferChecklist = _hud.PowerStatus.Text;
        _simulation = new ChronicleSimulation(beforeOffer);
        _presentationStatus = string.Empty;
        _renderKey = string.Empty;
        RefreshPresentation(forceMap: true);
        await PushPhysicalKey(Key.P);
        var offered = _simulation.AgentContext.PrimaryAgent!;
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == mouseOfferState &&
            _hud.PowerStatus.Text == mouseOfferChecklist &&
            offered.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered &&
            offered.Need.Status == AgentNeedStatus.Offered &&
            offered.WelcomeOfferedTick == _simulation.State.Tick,
            "Mouse and physical P must open the same visible welcome offer without spending a Heartbeat.");
        ActionButton("agent-primary").GrabFocus();
        await PushPhysicalKey(Key.Space);
        Verify(
            _simulation.State.Speed == ChronicleSpeed.Paused &&
            _simulation.AgentContext.PrimaryAgent!.HomeRelationship.Kind ==
                AgentHomeRelationshipKind.WelcomeOffered,
            "Focused Space must pause the Chronicle without activating the focused Withdraw Welcome button.");
        await CaptureGoal7AProof("open-offer");

        var offerTick = _simulation.State.Tick;
        ActionButton("agent-primary").GrabFocus();
        await PushPhysicalKey(Key.Space);
        Verify(
            _simulation.State.Speed == ChronicleSpeed.Slow &&
            _simulation.AgentContext.PrimaryAgent!.HomeRelationship.Kind ==
                AgentHomeRelationshipKind.WelcomeOffered,
            "Focused Space must resume the pending answer without withdrawing or duplicating the offer.");
        AdvanceAndRefresh();
        var accepted = _simulation.AgentContext.PrimaryAgent!;
        Verify(
            _simulation.State.Tick == offerTick + 1 &&
            _simulation.State.Speed == ChronicleSpeed.Paused &&
            accepted is
            {
                Presence: AgentPresenceState.AtHome,
                Need.Status: AgentNeedStatus.Satisfied,
                HomeRelationship:
                {
                    Kind: AgentHomeRelationshipKind.Guest,
                    WelcomingIncarnationId: 1,
                },
                RoadRollAddress: not null,
            } &&
            _hud.MessageReadout.Text.Contains("Tamar Venn accepts Refuge", StringComparison.Ordinal),
            "The next active Heartbeat must name Tamar's autonomous acceptance, satisfied need, Guest relationship, and road-roll.");
        await CaptureGoal7AProof("accepted-guest");

        var acceptanceEvents = _simulation.AgentContext.RecentEvents.Count(item =>
            item.Kind == AgentEventKind.WelcomeAccepted);
        await PushPhysicalKey(Key.Space);
        AdvanceAndRefresh();
        Verify(
            _simulation.State.Speed == ChronicleSpeed.Slow &&
            _simulation.AgentContext.PrimaryAgent == accepted &&
            _simulation.AgentContext.RecentEvents.Count(item =>
                item.Kind == AgentEventKind.WelcomeAccepted) == acceptanceEvents,
            "The Heartbeat after acceptance must not latch auto-pause, answer again, or move Tamar.");

        var savedGuest = ChronicleSaveCodec.Serialize(_simulation.State);
        Press(_hud.GetNode<Button>("SaveAction"));
        Press(_hud.GetNode<Button>("LoadAction"));
        Verify(
            ChronicleSaveCodec.Serialize(_simulation.State) == savedGuest && SaveVersion() == 9,
            "Actual Godot save/load must restore the strict-v9 Guest, relationship cause, road-roll, and tick exactly.");
        await CaptureGoal7AProof("restored-guest");

        _simulation = AtForFixture(_simulation, _simulation.State.CurrentBellAddress);
        _renderKey = string.Empty;
        Issue(new EndIncarnationAtBell());
        Verify(
            !_simulation.State.HasLivingIncarnation &&
            _simulation.AgentContext.PrimaryAgent == accepted &&
            _hud.ReplacementReadout.Text.Contains("Tamar Venn", StringComparison.Ordinal) &&
            _hud.ReplacementReadout.Text.Contains("HOME", StringComparison.OrdinalIgnoreCase),
            "The ended-body summary must name Tamar and Home while preserving the prior Incarnation's welcome cause.");
        TriggerInput(ReplaceAction);
        Verify(
            _simulation.State.IncarnationId == 2 &&
            _simulation.AgentContext.PrimaryAgent == accepted,
            "Replacement must preserve the same Agent without cloning, teleporting, or recruiting them.");
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        TriggerInput(MoveSouthAction);
        Verify(
            _simulation.State.Address == ChronicleState.AcceptedHomeFixtureAddress &&
            _simulation.AgentContext.PrimaryAgent == accepted,
            "The replacement Incarnation must return physically to the same Guest at Home.");
        await CaptureGoal7AProof("replacement-return");
        Press(_hud.GetNode<Button>("SaveAction"));
        Verify(SaveVersion() == 9, "Goal 7A rendered acceptance must write strict save v9.");
        GD.Print("GOAL7A VISUAL ACCEPTANCE PASS captures=6 save=9 keyboard=mouse agent=consequential relationship=guest");
    }

    private async Task CaptureGoal7AProof(string stage)
    {
        RefreshPresentation(forceMap: true);
        var agent = _simulation.AgentContext.PrimaryAgent
            ?? throw new InvalidOperationException("Goal 7A capture requires Tamar.");
        var expectedVisual = stage switch
        {
            "approaching" => "agent.wayfarer-listener.approaching",
            "waiting" => "agent.wayfarer-listener.waiting",
            "open-offer" => "agent.wayfarer-listener.welcome-offered",
            "accepted-guest" or "restored-guest" or "replacement-return" =>
                "agent.wayfarer-listener.guest",
            _ => throw new InvalidOperationException($"Unknown Goal 7A capture stage '{stage}'."),
        };
        var expectedState = stage switch
        {
            "approaching" => agent.Presence == AgentPresenceState.ApproachingHome,
            "waiting" => agent.Presence == AgentPresenceState.WaitingAtHome &&
                agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Unfamiliar,
            "open-offer" => agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered &&
                _simulation.State.Speed == ChronicleSpeed.Paused,
            "accepted-guest" or "restored-guest" =>
                agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Guest &&
                agent.Need.Status == AgentNeedStatus.Satisfied,
            "replacement-return" => _simulation.State.IncarnationId == 2 &&
                _simulation.State.Address == ChronicleState.AcceptedHomeFixtureAddress &&
                agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Guest,
            _ => false,
        };
        var plan = _map.CurrentPlan
            ?? throw new InvalidOperationException("Goal 7A capture requires a rendered map plan.");
        var agentAction = ActionButton("agent-primary");
        Verify(
            expectedState &&
            _hud.TargetHeading.Text.Contains(agent.DisplayName.ToUpperInvariant(), StringComparison.Ordinal) &&
            _hud.TargetFacts.Text.Contains("NEEDS · Refuge", StringComparison.Ordinal) &&
            _hud.TargetFacts.Text.Contains("Resonant Lode", StringComparison.Ordinal) &&
            _hud.TargetOutcome.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length <= 5 &&
            _hud.TargetOutcome.Text.Contains("NEXT", StringComparison.Ordinal) &&
            _hud.TargetOutcome.Text.Contains("WHEN", StringComparison.Ordinal) &&
            _hud.TargetOutcome.Text.Contains("INTERRUPTS", StringComparison.Ordinal) &&
            _hud.TargetOutcome.Text.Contains("PREVENTS", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.StartsWith("CHECKLIST · ", StringComparison.Ordinal) &&
            _hud.PowerStatus.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length <= 5 &&
            _hud.TargetButtons.All(button => !button.Visible) &&
            agentAction.Text.Contains(
                agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered
                    ? "WITHDRAW WELCOME"
                    : "OFFER WELCOME",
                StringComparison.Ordinal) &&
            plan.Marks.Any(mark => mark.Address == agent.Address && mark.VisualId == expectedVisual) &&
            plan.Marks.Any(mark =>
                mark.Address == ChronicleState.AcceptedHomeFixtureAddress &&
                mark.VisualId == "subject.home-hearthstone") &&
            plan.Marks.Any(mark => mark.VisualId == "source.hearth-resonator.intact") &&
            (agent.RoadRollAddress is null || plan.Marks.Any(mark =>
                mark.Address == agent.RoadRollAddress &&
                mark.VisualId == "place.wayfarer-road-roll.laid")) &&
            ChronicleHud.MapWidth * ChronicleHud.MapHeight >=
                ChronicleHud.CanvasWidth * ChronicleHud.CanvasHeight * 3 / 4,
            $"Goal 7A '{stage}' must align the compact Agent checklist, decision, action, Home, Agent, Source, and persistent place.");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Verify(
            _hud.PowerStatus.GetMinimumSize().Y <= _hud.PowerStatus.Size.Y &&
            _hud.TargetHeading.GetMinimumSize().X <= _hud.TargetHeading.Size.X &&
            _hud.TargetFacts.GetMinimumSize().Y <= _hud.TargetFacts.Size.Y &&
            _hud.ConsequenceRows.Where(row => row.Visible)
                .All(row => row.GetMinimumSize().Y <= row.Size.Y) &&
            _hud.ForecastReadout.GetMinimumSize().Y <= _hud.ForecastReadout.Size.Y &&
            _hud.MessageReadout.GetMinimumSize().Y <= _hud.MessageReadout.Size.Y &&
            _hud.ActionButtons.All(button =>
                button.FocusMode == Control.FocusModeEnum.All &&
                button.GetThemeColor("font_disabled_color").A >= 0.75f),
            $"Goal 7A '{stage}' must not clip or overlap identity, need, timing, interruption, consequence, or action feedback.");
        RenderingServer.ForceDraw(swapBuffers: false);
        var image = GetViewport().GetTexture().GetImage();
        Verify(
            image is not null &&
            image.GetWidth() == ChronicleHud.CanvasWidth &&
            image.GetHeight() == ChronicleHud.CanvasHeight,
            "Goal 7A native HUD proof must capture the exact 1600 × 900 viewport.");
        Verify(
            image!.SavePng($"user://goal7a-{stage}-hud.png") == Error.Ok,
            $"Goal 7A '{stage}' native HUD capture must be writable.");
        GD.Print($"GOAL7A HUD CAPTURE PASS stage={stage} size=1600x900");
    }

    private async Task RunGoal6BVisualAcceptance()
    {
        _simulation = CreateGoal6BTestingStart();
        _selectedTargetKind = CombatTargetKind.MireBrute;
        _presentationStatus = string.Empty;
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
                mark.VisualId == "emphasis.target.selected") &&
            _hud.TargetHeading.Text == "BURN PRIMER · UNREAD" &&
            _hud.TargetButtons.All(button => !button.Visible),
            "A fresh player Chronicle must skip opening paths and visibly direct P to the nearby unread Burn Primer.");

        var safeBasalt = _simulation.CombatContext.Targets.Single(target => target.Kind == CombatTargetKind.Basalt);
        var safeRejection = TargetOutcomeText(safeBasalt, _simulation.CombatContext);
        Verify(
            safeRejection.Contains("Rejected before time begins", StringComparison.Ordinal) &&
            !safeRejection.Contains("Brute swing", StringComparison.Ordinal),
            "A rejected Burn outside combat must not invent a hostile interruption that the forecast says is absent.");

        TriggerInput(MoveNorthAction);
        Verify(
            _simulation.State.Address == _simulation.PowerComesHomeContext.BurnPrimer.Address &&
            ActionButton("power-primary").Text == "[P] READ BURN PRIMER\nPRESS P NOW",
            "Standing directly on the non-blocking Burn Primer must keep the contextual interaction available.");
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
        TriggerInput(MoveSouthAction);
        Verify(
            _simulation.State.Address == ChronicleState.AcceptedHomeFixtureAddress,
            "The same-cell Primer proof must return to the accepted Home start before the retained journey continues.");

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
            _simulation.CombatContext.PendingAction is null &&
            !_hud.MessageReadout.Text.Contains("physically attached", StringComparison.Ordinal),
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
        Verify(SaveVersion() == 9, "Goal 6B rendered acceptance must rewrite through strict save v9.");
        GD.Print("GOAL6B VISUAL ACCEPTANCE PASS captures=8 save=9 keyboard=mouse map=physical capacity=next-attunement");
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
            _hud.PowerCapacity.GetMinimumSize().Y <= _hud.PowerCapacity.Size.Y &&
            _hud.TargetHeading.GetMinimumSize().X <= _hud.TargetHeading.Size.X &&
            _hud.TargetFacts.GetMinimumSize().Y <= _hud.TargetFacts.Size.Y &&
            _hud.ConsequenceRows.Where(row => row.Visible)
                .All(row => row.GetMinimumSize().Y <= row.Size.Y) &&
            _hud.MessageReadout.GetMinimumSize().Y <= _hud.MessageReadout.Size.Y &&
            _hud.ActionButtons.All(button =>
                button.FocusMode == Control.FocusModeEnum.All &&
                button.GetThemeColor("font_disabled_color").A >= 0.75f),
            $"Goal 6B '{stage}' must not clip its checklist, material state, decisions, or recent log, and every action must retain keyboard focus and readable disabled text.");
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
        await VerifyFocusedSpaceResumesCombat();

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
        Verify(SaveVersion() == 9, "Quickly journey must rewrite through strict save v9.");
        GD.Print("GOAL6A QUICKLY SAVE READY hud=map-first target=basalt-rejected scorch=present brute=dead save=9");
    }

    private async Task VerifyFocusedSpaceResumesCombat()
    {
        StartFreshAcceptance();
        TriggerInput(SlowAction);
        MoveToThreat(useKeyboard: true);

        Press(ActionButton("weapon"));
        Verify(
            _simulation.CombatContext.PendingAction?.DisplayName == "Ready Iron Cleaver",
            "The pending-header fixture must queue the focused Cleaver action.");
        Verify(
            !_hud.ClockReadout.Text.Contains("PENDING", StringComparison.Ordinal) &&
            _hud.ClockReadout.GetMinimumSize().X <= _hud.ClockReadout.Size.X,
            "The top-rail Clock must remain compact instead of repeating or clipping the pending action.");
        Verify(
            _hud.ForecastReadout.Text.Contains("CLEAVER", StringComparison.Ordinal) &&
            _hud.MessageReadout.Text.Contains("pending while paused", StringComparison.OrdinalIgnoreCase),
            "The forecast and log must retain the pending-action explanation removed from the top rail.");
        Verify(
            !_hud.ClockReadout.GetRect().Intersects(_hud.PlaceReadout.GetRect()) &&
            !_hud.PauseBadge.GetRect().Intersects(_hud.PlaceReadout.GetRect()),
            "The Clock and pause plate must not overlap the top-rail place or controls.");

        StartFreshAcceptance();
        TriggerInput(SlowAction);
        MoveToThreat(useKeyboard: true);

        Verify(_simulation.State.Speed == ChronicleSpeed.Paused, "Focused-Space regression must begin at the engagement pause.");
        var pausedTick = _simulation.State.Tick;
        ActionButton("weapon").GrabFocus();
        Verify(ActionButton("weapon").HasFocus(), "Focused-Space regression must give the combat action keyboard focus.");
        await PushPhysicalKey(Key.Space);

        Verify(
            _simulation.State.Speed == ChronicleSpeed.Slow,
            "A focused HUD action must not consume Space or leave combat infinitely paused.");
        _heartbeatAccumulator = SlowHeartbeatSeconds;
        _Process(0);
        Verify(
            _simulation.State.Tick == pausedTick + 1,
            "Space after engagement must deliver the next hostile Heartbeat even when a HUD action had keyboard focus.");
    }

    private Task RunQuicklyRestartAcceptance()
    {
        Verify(SaveVersion() == 9, "Quickly restart must load current save v9.");
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
        GD.Print($"GOAL6A LASTING DEATH READY scorch=present bruteHp={_simulation.CombatContext.MireBrute?.HitPoints} incarnation=ended save=9");
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
        GD.Print("GOAL6A LASTING RESTART PASS incarnation=2 equipment=fresh scorch=present brute=dead save=9");
        return Task.CompletedTask;
    }

    private void StartFreshAcceptance()
    {
        _simulation = new ChronicleSimulation(Goal6AFixture());
        _selectedTargetKind = CombatTargetKind.MireBrute;
        _presentationStatus = string.Empty;
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
            _hud.MessageReadout.Size.Y >= 258 &&
            _hud.ConsequenceRows.Count == 5 &&
            _hud.ConsequenceRows.All(row => !string.IsNullOrWhiteSpace(row.Text)) &&
            _map.IsPaused &&
            _map.VisibleColumns == ChronicleHud.MapColumns &&
            _map.VisibleRows == ChronicleHud.MapRows &&
            _hud.ActionButtons.Count(button => button.Icon is not null) >= 4 &&
            _hud.ActionButtons.All(button => button.FocusMode == Control.FocusModeEnum.All) &&
            _hud.TargetButtons.All(button => button.FocusMode == Control.FocusModeEnum.All) &&
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

    private async Task PushPhysicalKey(Key key)
    {
        GetViewport().PushInput(new InputEventKey
        {
            Keycode = key,
            PhysicalKeycode = key,
            Pressed = true,
        });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventKey
        {
            Keycode = key,
            PhysicalKeycode = key,
            Pressed = false,
        });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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
