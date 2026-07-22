namespace Chronicle.Core;

public sealed class ChronicleSimulation
{
    private const int RecentCombatResultLimit = 12;
    private readonly List<CombatResultSnapshot> _recentCombatResults = [];

    public ChronicleSimulation(ChronicleState initialState)
    {
        State = initialState;
    }

    public ChronicleState State { get; private set; }

    /// <summary>
    /// The one Goal 6A combat/HUD snapshot. Result messages are deliberately
    /// simulation-local presentation history and never enter Chronicle saves.
    /// </summary>
    public CombatContextSnapshot CombatContext =>
        Goal6AActionPlanning.Snapshot(State, _recentCombatResults);

    public TargetPreviewSnapshot PreviewTarget(WorldAddress target) =>
        Goal6AActionPlanning.PreviewTarget(State, target);

    public PowerComesHomeContextSnapshot PowerComesHomeContext =>
        Goal6BPowerComesHome.Snapshot(State);

    public StudySourceSnapshot? CurrentStudySource =>
        StudySourceGrammar.At(State, State.Address);

    public ConflictContextSnapshot? ConflictContext => State.FirstConflict is { } conflict
        ? new ConflictContextSnapshot(
            conflict.Outcome == FirstConflictOutcome.Shattered
                ? FirstConflictSubjects.ShatteredCairnIdentity
                : FirstConflictSubjects.RivenCairnIdentity,
            FirstConflictSubjects.RiverWardIdentity,
            FirstConflictSubjects.History,
            FirstConflictSubjects.Warning,
            conflict.Address,
            conflict.ThreatenedTick,
            conflict.PendingAction == new LoadoutSlot(WordIds.Smash),
            conflict.PendingAction,
            conflict.Outcome,
            conflict.ResolvedTick,
            conflict.ResolvingIncarnationId)
        : null;

    public HomeContextSnapshot HomeContext
    {
        get
        {
            var cell = WorldArea.Generate(
                State,
                State.Address.Stratum,
                new WorldRectangle(State.Address.X, State.Address.Y, 1, 1)).Cells[0];

            var site = HomeSiteAt(cell);
            return new HomeContextSnapshot(
                Home: State.Home,
                CurrentSite: site,
                ReturnRoute: ReturnRouteTo(State.Home));
        }
    }

    public WorldAddress? FlyDestination
    {
        get
        {
            if (!State.CanFly)
            {
                return null;
            }

            if (string.Equals(
                    State.Address.Stratum,
                    SurfacePatch.SurfaceStratum,
                    StringComparison.Ordinal))
            {
                return State.Address with { Stratum = SkyStratum.StratumName };
            }

            if (string.Equals(
                    State.Address.Stratum,
                    SkyStratum.StratumName,
                    StringComparison.Ordinal))
            {
                return State.Address with { Stratum = SurfacePatch.SurfaceStratum };
            }

            return null;
        }
    }

    public ChronicleCommandResult Apply(ChronicleCommand command)
    {
        if (!State.HasLivingIncarnation && command is not CreateReplacementIncarnation)
        {
            return ChronicleCommandResult.Rejected(
                "The Chronicle is awaiting a replacement Incarnation.");
        }

        if (command is AttuneExpression attune)
        {
            return Attune(attune);
        }

        if (command is BeginPowerCommitment beginPower)
        {
            return BeginPowerWork(beginPower.Kind);
        }

        if (command is ReadBurnPrimer)
        {
            return ReadBurnPrimerAtHome();
        }

        if (command is LiftResonantLode)
        {
            return ChangeLodeCarry(Goal6BPowerComesHome.TryLift);
        }

        if (command is SetDownResonantLode)
        {
            return ChangeLodeCarry(Goal6BPowerComesHome.TryDrop);
        }

        if (command is CancelPowerCommitment)
        {
            return ChangeLodeCarry(Goal6BPowerComesHome.TryCancel);
        }

        if (command is ConfigureEngagementPlan configurePlan)
        {
            return ConfigurePlan(configurePlan);
        }

        if (command is SetWeaponStance setWeaponStance)
        {
            return SetWeaponStance(setWeaponStance);
        }

        if (command is PrepareBurn prepareBurn)
        {
            return PrepareBurnInvocation(prepareBurn);
        }

        if (command is CancelPendingTacticalAction)
        {
            return CancelTacticalAction();
        }

        if (command is SkipRecovery)
        {
            return SkipInvocationRecovery();
        }

        if (command is SetChronicleSpeed requestedSpeed &&
            Goal6AActionPlanning.IsAvailable(State) &&
            Goal6AActionPlanning.IsImmediateDanger(State) &&
            requestedSpeed.Speed is ChronicleSpeed.Normal or ChronicleSpeed.Fast)
        {
            return ChronicleCommandResult.Rejected(
                "Immediate danger may run only at Slow speed or while paused.");
        }

        if (command is ConfigureLoadoutSlot configure)
        {
            return ConfigureSlot(configure);
        }

        if (command is ClearLoadoutSlot clear)
        {
            return ClearSlot(clear);
        }

        if (command is UseLoadoutSlot use)
        {
            return UseSlot(use);
        }

        if (command is ChooseStudyWord chooseStudy)
        {
            return ChooseStudy(chooseStudy);
        }

        var before = State;
        State = command switch
        {
            SetChronicleSpeed setSpeed => SetSpeed(setSpeed.Speed),
            ChooseUpIntent => State.Intent == OpeningIntent.Unchosen
                ? State.WithIntent(OpeningIntent.Up)
                : State,
            ChooseHereIntent => State.Intent == OpeningIntent.Unchosen
                ? State.WithIntent(OpeningIntent.Here)
                : State,
            ChooseAgainstIntent => State.Intent == OpeningIntent.Unchosen &&
                                   State.WorldGrammarVersion is 3 or 4 or 5
                ? State.WithIntent(OpeningIntent.Against)
                : State,
            EndIncarnationAtBell => State.EndIncarnationAtBell(),
            CreateReplacementIncarnation => State.CreateReplacementIncarnation(),
            MoveIncarnation move when !IsCardinal(move.DeltaX, move.DeltaY) => throw new ArgumentException(
                "Incarnation movement must be exactly one cardinal step.",
                nameof(command)),
            MoveIncarnation move when Goal6AActionPlanning.IsAvailable(State) => MoveGoal6A(move),
            MoveIncarnation move => Move(move),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown Chronicle command."),
        };

        return new ChronicleCommandResult(
            Applied: State != before,
            Message: State == before ? "Nothing changed." : string.Empty);
    }

    public void AdvanceOneTick()
    {
        if (!Goal6AActionPlanning.IsAvailable(State))
        {
            State = State.AdvanceTick();
            return;
        }

        var result = Goal6AActionPlanning.Advance(State);
        State = result.State;
        AddCombatResults(result.Results);
    }

    public void AdvanceClockPulse()
    {
        for (var tick = TicksPerClockPulse(State.Speed); tick > 0; tick--)
        {
            AdvanceOneTick();
        }
    }

    public IReadOnlyList<WorldAddress> ValidTargetsForSlot(int slotIndex)
    {
        if (Goal6AActionPlanning.IsAvailable(State))
        {
            if (!State.HasLivingIncarnation || !IsSlotIndex(slotIndex) ||
                State.ActiveLoadout[slotIndex].Verb != WordIds.Burn)
            {
                return [];
            }

            return [State.Combat!.MireBrute.Address];
        }

        if (!State.HasLivingIncarnation || !IsSlotIndex(slotIndex))
        {
            return [];
        }

        var slot = State.ActiveLoadout[slotIndex];
        var subject = FittedFlySubject(slot);
        if (subject is not { } target ||
            !IsAdjacent(State.Address, target.Address) ||
            MatchingStratumDestination(target.Address) is not { } destination ||
            DurableOccupantAt(destination) is not null)
        {
            return [];
        }

        return [target.Address];
    }

    private ChronicleState SetSpeed(ChronicleSpeed speed)
    {
        if (Goal6AActionPlanning.IsAvailable(State) &&
            Goal6AActionPlanning.IsImmediateDanger(State) &&
            speed is ChronicleSpeed.Normal or ChronicleSpeed.Fast)
        {
            return State;
        }

        return State.WithSpeed(speed);
    }

    private ChronicleCommandResult Attune(AttuneExpression command)
    {
        if (!Goal6AActionPlanning.IsAvailable(State))
        {
            return ChronicleCommandResult.Rejected(
                "Successor Expressions require a World Grammar v4 Chronicle.");
        }

        if (Goal6AActionPlanning.IsImmediateDanger(State))
        {
            return ChronicleCommandResult.Rejected(
                "Attunement is available only while immediate danger is absent.");
        }

        if (Goal6BPowerComesHome.IsCarrying(State))
        {
            return ChronicleCommandResult.Rejected(
                "Set down the Resonant Lode before Attunement; carrying occupies focused Attunement.");
        }

        if (Goal6BPowerComesHome.HasCommitment(State))
        {
            return ChronicleCommandResult.Rejected(
                "Finish or cancel the physical commitment before Attunement.");
        }

        var modifiers = command.Modifiers ?? [];
        if (!Goal6AActionPlanning.TryValidateExpression(
                State,
                command.Verb,
                modifiers,
                out var slot,
                out var message))
        {
            return ChronicleCommandResult.Rejected(message);
        }

        State = State with
        {
            Loadout = LoadoutState.Empty.WithSlot(0, slot),
            Attunement = new LoadAttunementState(
                Goal6BPowerComesHome.NextAttunementCapacity(State),
                State.Tick),
        };
        AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.Command, message));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleCommandResult ConfigurePlan(ConfigureEngagementPlan command)
    {
        if (!Goal6AActionPlanning.IsAvailable(State))
        {
            return ChronicleCommandResult.Rejected(
                "An Engagement Plan requires a World Grammar v4 Chronicle.");
        }

        if (Goal6AActionPlanning.IsImmediateDanger(State))
        {
            return ChronicleCommandResult.Rejected(
                "The Engagement Plan can change only while safe.");
        }

        var combat = State.Combat!;
        if (combat.EngagementPlan.OpenWithWeaponStance == command.OpenWithWeaponStance)
        {
            return ChronicleCommandResult.Rejected("That Engagement Plan is already active.");
        }

        State = State with
        {
            Combat = combat with
            {
                EngagementPlan = new EngagementPlanState(command.OpenWithWeaponStance),
            },
        };
        var message = command.OpenWithWeaponStance
            ? "Engagement Plan will ready the Iron Cleaver on contact."
            : "Engagement Plan will leave the Iron Cleaver lowered on contact.";
        AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.Command, message));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleCommandResult SetWeaponStance(SetWeaponStance command)
    {
        if (!Goal6AActionPlanning.IsAvailable(State))
        {
            return ChronicleCommandResult.Rejected(
                "Iron Cleaver stance requires a World Grammar v4 Chronicle.");
        }

        if (Goal6BPowerComesHome.IsCarrying(State))
        {
            return ChronicleCommandResult.Rejected(
                "Set down the Resonant Lode before using the Iron Cleaver; carrying occupies both hands.");
        }

        if (Goal6BPowerComesHome.HasCommitment(State))
        {
            return ChronicleCommandResult.Rejected(
                "Finish or cancel the physical commitment before using the Iron Cleaver.");
        }

        if (!Goal6AActionPlanning.IsImmediateDanger(State))
        {
            var combat = State.Combat!;
            if (combat.WeaponStanceActive == command.Active)
            {
                return ChronicleCommandResult.Rejected("That Iron Cleaver stance is already active.");
            }

            State = State with { Combat = combat with { WeaponStanceActive = command.Active } };
            var immediateMessage = command.Active
                ? "Iron Cleaver stance readied."
                : "Iron Cleaver stance lowered.";
            AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.Stance, immediateMessage));
            return ChronicleCommandResult.Succeeded(immediateMessage);
        }

        return QueueTacticalAction(new TacticalActionState(
            TacticalActionKind.SetWeaponStance,
            WeaponStanceActive: command.Active));
    }

    private ChronicleCommandResult PrepareBurnInvocation(PrepareBurn command)
    {
        if (!Goal6AActionPlanning.IsAvailable(State))
        {
            return ChronicleCommandResult.Rejected("Burn requires a World Grammar v4 Chronicle.");
        }

        var preview = PreviewTarget(command.Target);
        if (!preview.CanBurn)
        {
            return ChronicleCommandResult.Rejected(preview.EligibilityReason);
        }

        if (State.Speed == ChronicleSpeed.Slow)
        {
            return QueueTacticalAction(new TacticalActionState(
                TacticalActionKind.PrepareBurn,
                Target: command.Target));
        }

        if (State.Speed != ChronicleSpeed.Paused && Goal6AActionPlanning.IsImmediateDanger(State))
        {
            return QueueTacticalAction(new TacticalActionState(
                TacticalActionKind.PrepareBurn,
                Target: command.Target));
        }

        if (!Goal6AActionPlanning.TryStartPreparation(State, command.Target, out var prepared, out var message))
        {
            return ChronicleCommandResult.Rejected(message);
        }

        State = prepared;
        AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.PreparationStarted, message));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleCommandResult CancelTacticalAction()
    {
        if (!Goal6AActionPlanning.IsAvailable(State))
        {
            return ChronicleCommandResult.Rejected("There is no Goal 6A tactical action to cancel.");
        }

        if (State.Speed != ChronicleSpeed.Paused)
        {
            return ChronicleCommandResult.Rejected("Pause before replacing or cancelling a tactical action.");
        }

        var combat = State.Combat!;
        if (combat.PendingAction is null && combat.Preparation is null)
        {
            return ChronicleCommandResult.Rejected("There is no pending tactical action.");
        }

        State = State with
        {
            Combat = combat with { PendingAction = null, Preparation = null },
        };
        const string message = "Pending tactical action cancelled.";
        AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.PreparationInterrupted, message));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleCommandResult SkipInvocationRecovery()
    {
        if (!Goal6AActionPlanning.CanSkipRecovery(State))
        {
            return ChronicleCommandResult.Rejected(
                "Recovery can skip only while no meaningful interruption is possible.");
        }

        State = Goal6AActionPlanning.SkipRecovery(State);
        const string message = "Burn Recovery skipped safely to completion.";
        AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.RecoverySkipped, message));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleCommandResult QueueTacticalAction(TacticalActionState action)
    {
        var combat = State.Combat!;
        var abandonedPreparation = combat.Preparation is not null;
        State = State with
        {
            Speed = ChronicleSpeed.Paused,
            Combat = combat with
            {
                PendingAction = action,
                Preparation = null,
            },
        };
        var name = action.Kind switch
        {
            TacticalActionKind.Move => "Move",
            TacticalActionKind.SetWeaponStance => action.WeaponStanceActive
                ? "Ready Iron Cleaver"
                : "Lower Iron Cleaver",
            TacticalActionKind.PrepareBurn => "Prepare Burn",
            _ => throw new InvalidOperationException($"Unknown tactical action '{action.Kind}'."),
        };
        var message = abandonedPreparation
            ? $"Preparation abandoned; {name} is pending while paused."
            : $"{name} is pending while paused.";
        AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.Command, message));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleState MoveGoal6A(MoveIncarnation move)
    {
        if (State.Intent == OpeningIntent.Unchosen)
        {
            return State;
        }

        if (Goal6BPowerComesHome.HasCommitment(State))
        {
            AddCombatResult(new CombatResultSnapshot(
                State.Tick,
                CombatResultKind.Command,
                "Cancel the physical commitment before moving."));
            return State;
        }

        var destination = new WorldAddress(
            State.Address.Stratum,
            checked(State.Address.X + move.DeltaX),
            checked(State.Address.Y + move.DeltaY));
        if (Goal6AActionPlanning.IsOccupiedByLivingMireBrute(State, destination) ||
            Goal6BPowerComesHome.BlocksMovement(State, destination))
        {
            AddCombatResult(new CombatResultSnapshot(
                State.Tick,
                CombatResultKind.Command,
                Goal6AActionPlanning.IsOccupiedByLivingMireBrute(State, destination)
                    ? "The living Mire Brute occupies that cell."
                    : "A physical Goal 6B subject occupies that cell.",
                Address: destination));
            return State;
        }

        var immediateDanger = Goal6AActionPlanning.IsImmediateDanger(State);
        // Slow is tactical only within, or while crossing into, the authored
        // threat boundary. A distant living opponent must not turn ordinary
        // exploration after physical work into a repeated auto-pause queue.
        var entersImmediateDanger = !immediateDanger &&
                                    Goal6AActionPlanning.IsImmediateDanger(State with { Address = destination });
        if (immediateDanger ||
            (State.Speed == ChronicleSpeed.Slow && entersImmediateDanger))
        {
            QueueTacticalAction(new TacticalActionState(
                TacticalActionKind.Move,
                DeltaX: move.DeltaX,
                DeltaY: move.DeltaY));
            return State;
        }

        State = State.TravelTo(destination);
        State = Goal6AActionPlanning.ApplyEngagement(State, _recentCombatResults);
        AddCombatResult(new CombatResultSnapshot(State.Tick, CombatResultKind.Movement, "The Incarnation moves.", Address: State.Address));
        return State;
    }

    private ChronicleCommandResult BeginPowerWork(PowerCommitmentKind kind)
    {
        if (!Goal6BPowerComesHome.TryBeginCommitment(State, kind, out var updated, out var message))
        {
            return ChronicleCommandResult.Rejected(message);
        }

        State = updated;
        AddCombatResult(new CombatResultSnapshot(
            State.Tick,
            CombatResultKind.PowerHome,
            message,
            Address: State.PowerHome!.Commitment!.Address));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleCommandResult ChangeLodeCarry(TryPowerChange change)
    {
        if (!change(State, out var updated, out var message))
        {
            return ChronicleCommandResult.Rejected(message);
        }

        State = updated;
        AddCombatResult(new CombatResultSnapshot(
            State.Tick,
            CombatResultKind.PowerHome,
            message,
            Address: Goal6BPowerComesHome.LodeWorldAddress(State) ?? State.Address));
        return ChronicleCommandResult.Succeeded(message);
    }

    private ChronicleCommandResult ReadBurnPrimerAtHome()
    {
        if (!Goal6BPowerComesHome.TryReadBurnPrimer(State, out var updated, out var message))
        {
            return ChronicleCommandResult.Rejected(message);
        }

        State = updated;
        AddCombatResult(new CombatResultSnapshot(
            State.Tick,
            CombatResultKind.PowerHome,
            message,
            Address: Goal6BPowerComesHome.BurnPrimerAddress));
        return ChronicleCommandResult.Succeeded(message);
    }

    private void AddCombatResults(IEnumerable<CombatResultSnapshot> results)
    {
        foreach (var result in results)
        {
            AddCombatResult(result);
        }
    }

    private void AddCombatResult(CombatResultSnapshot result)
    {
        _recentCombatResults.Add(result);
        if (_recentCombatResults.Count > RecentCombatResultLimit)
        {
            _recentCombatResults.RemoveRange(0, _recentCombatResults.Count - RecentCombatResultLimit);
        }
    }

    private ChronicleCommandResult ChooseStudy(ChooseStudyWord command)
    {
        var source = CurrentStudySource;
        if (source is null)
        {
            return ChronicleCommandResult.Rejected("There is no Study Source here.");
        }

        if (source.Id != command.SourceId)
        {
            return ChronicleCommandResult.Rejected("That Study Source is no longer present.");
        }

        var offer = source.Offers.FirstOrDefault(candidate => candidate.Word.Id == command.WordId);
        if (offer is null)
        {
            return ChronicleCommandResult.Rejected("That Word is not offered by this Study Source.");
        }

        if (State.Codex.Contains(command.WordId))
        {
            return ChronicleCommandResult.Rejected($"The Codex already keeps {offer.Word.DisplayName}.");
        }

        if (State.Study.ActiveSourceId == source.Id &&
            State.Study.ActiveWord == command.WordId)
        {
            return ChronicleCommandResult.Rejected($"{offer.Word.DisplayName} is already the active pursuit.");
        }

        State = State.BeginStudy(source.Id, command.WordId);
        return ChronicleCommandResult.Succeeded($"Pursuing {offer.Word.DisplayName} at {source.Name}.");
    }

    private ChronicleCommandResult ConfigureSlot(ConfigureLoadoutSlot command)
    {
        if (Goal6AActionPlanning.IsAvailable(State))
        {
            return ChronicleCommandResult.Rejected(
                "Use AttuneExpression for the successor Verb-and-Modifier Loadout.");
        }

        if (!IsSlotIndex(command.SlotIndex))
        {
            return ChronicleCommandResult.Rejected("That Loadout slot does not exist.");
        }

        if (!WordCatalogue.TryGet(command.Verb, out var verb) ||
            verb.Kind != WordKind.Verb)
        {
            return ChronicleCommandResult.Rejected("That Verb is unknown.");
        }

        if (!State.Codex.Contains(command.Verb))
        {
            return ChronicleCommandResult.Rejected($"{verb.DisplayName} is not in the Codex.");
        }

        WordDefinition? noun = null;
        if (command.Noun is { } nounId &&
            (!WordCatalogue.TryGet(nounId, out noun) ||
             noun.Kind != WordKind.Noun ||
             !verb.CompatibleNouns.Contains(nounId)))
        {
            return ChronicleCommandResult.Rejected(
                $"That Noun is incompatible with {verb.DisplayName}.");
        }

        if (command.Noun is { } knownNounId && !State.Codex.Contains(knownNounId))
        {
            return ChronicleCommandResult.Rejected(
                $"{noun!.DisplayName} is not in the Codex.");
        }

        if (State.ActiveLoadout.Slots
            .Where((slot, index) => index != command.SlotIndex)
            .Any(slot => slot.Verb == command.Verb))
        {
            return ChronicleCommandResult.Rejected(
                $"{verb.DisplayName} already occupies another Loadout slot.");
        }

        var slot = new LoadoutSlot(command.Verb, command.Noun);
        if (State.ActiveLoadout[command.SlotIndex] == slot)
        {
            return ChronicleCommandResult.Rejected($"{slot.DisplayName} is already equipped there.");
        }

        State = State with
        {
            Loadout = State.ActiveLoadout.WithSlot(command.SlotIndex, slot),
        };
        return ChronicleCommandResult.Succeeded(
            $"Equipped {slot.DisplayName} in slot {command.SlotIndex + 1}.");
    }

    private ChronicleCommandResult ClearSlot(ClearLoadoutSlot command)
    {
        if (Goal6AActionPlanning.IsAvailable(State))
        {
            return ChronicleCommandResult.Rejected(
                "Use AttuneExpression for the successor Verb-and-Modifier Loadout.");
        }

        if (!IsSlotIndex(command.SlotIndex))
        {
            return ChronicleCommandResult.Rejected("That Loadout slot does not exist.");
        }

        if (State.ActiveLoadout[command.SlotIndex].IsEmpty)
        {
            return ChronicleCommandResult.Rejected($"Loadout slot {command.SlotIndex + 1} is already empty.");
        }

        State = State with
        {
            Loadout = State.ActiveLoadout.WithSlot(command.SlotIndex, new LoadoutSlot()),
        };
        return ChronicleCommandResult.Succeeded($"Cleared Loadout slot {command.SlotIndex + 1}.");
    }

    private ChronicleCommandResult UseSlot(UseLoadoutSlot command)
    {
        if (!IsSlotIndex(command.SlotIndex))
        {
            return ChronicleCommandResult.Rejected("That Loadout slot does not exist.");
        }

        var slot = State.ActiveLoadout[command.SlotIndex];
        if (slot.IsEmpty)
        {
            return ChronicleCommandResult.Rejected($"Loadout slot {command.SlotIndex + 1} is empty.");
        }

        if (Goal6AActionPlanning.IsAvailable(State))
        {
            return slot.Verb == WordIds.Burn && command.Target is { } target
                ? PrepareBurnInvocation(new PrepareBurn(target))
                : ChronicleCommandResult.Rejected(
                    "Use a successor Burn Expression against a selected Target.");
        }

        if (slot.Verb == WordIds.Fly)
        {
            return UseFly(command, slot);
        }

        if (slot.IsIntrinsicFound)
        {
            return FoundAtCurrentSite(command);
        }

        if (slot.IsIntrinsicSmash)
        {
            return PrepareSmashAtCurrentSite(command, slot);
        }

        return ChronicleCommandResult.Rejected("That Loadout expression is incompatible.");
    }

    private ChronicleCommandResult UseFly(
        UseLoadoutSlot command,
        LoadoutSlot slot)
    {
        if (slot.Noun is null)
        {
            if (command.Target is not null)
            {
                return ChronicleCommandResult.Rejected("Intrinsic Fly acts on the Incarnation and takes no target.");
            }

            if (FlyDestination is not { } destination)
            {
                return ChronicleCommandResult.Rejected("Fly has nowhere to go from this Stratum.");
            }

            State = EnterFirstConflictIfNeeded(State.TravelTo(destination));
            return ChronicleCommandResult.Succeeded($"Flew to {destination}.");
        }

        var subject = FittedFlySubject(slot);
        if (command.Target is not { } target)
        {
            return subject is { } known
                ? ChronicleCommandResult.Rejected($"Choose the adjacent {known.Name}.")
                : ChronicleCommandResult.Rejected("That fitted Noun has no subject here.");
        }

        var expressionName = ExpressionName(slot);
        if (subject is not { } fittedSubject)
        {
            return ChronicleCommandResult.Rejected("That fitted Noun has no subject here.");
        }

        if (target != fittedSubject.Address)
        {
            return ChronicleCommandResult.Rejected(
                $"{expressionName} can only target the {fittedSubject.Name}.");
        }

        if (!IsAdjacent(State.Address, fittedSubject.Address))
        {
            return ChronicleCommandResult.Rejected($"The {fittedSubject.Name} must be adjacent.");
        }

        if (MatchingStratumDestination(fittedSubject.Address) is not { } fittedDestination)
        {
            return ChronicleCommandResult.Rejected(
                $"The {fittedSubject.Name} cannot cross from this Stratum.");
        }

        if (DurableOccupantAt(fittedDestination) is { } occupant)
        {
            return ChronicleCommandResult.Rejected(
                $"{expressionName} cannot move the {fittedSubject.Name} onto {occupant}.");
        }

        State = MoveFittedSubject(fittedSubject.Noun, fittedDestination);
        return ChronicleCommandResult.Succeeded(
            $"{expressionName} moved the {fittedSubject.Name} to {fittedDestination}.");
    }

    private ChronicleCommandResult FoundAtCurrentSite(UseLoadoutSlot command)
    {
        if (command.Target is not null)
        {
            return ChronicleCommandResult.Rejected(
                "Intrinsic Found acts on the current site and takes no target.");
        }

        if (State.Home is not null)
        {
            return ChronicleCommandResult.Rejected(
                "The Chronicle already has its singular Home.");
        }

        var site = HomeContext.CurrentSite;
        if (!site.IsEligible)
        {
            return ChronicleCommandResult.Rejected(site.Reason);
        }

        State = State with
        {
            Home = new HomeState(
                "holding.home",
                "The First Hearth",
                State.Address,
                State.Tick,
                State.IncarnationId,
                HomeMaterialState.HearthstoneRaised),
        };
        return ChronicleCommandResult.Succeeded($"Founded The First Hearth at {State.Address}.");
    }

    private ChronicleCommandResult PrepareSmashAtCurrentSite(
        UseLoadoutSlot command,
        LoadoutSlot slot)
    {
        if (command.Target is not null)
        {
            return ChronicleCommandResult.Rejected(
                "Intrinsic Smash acts on the current site and takes no target.");
        }

        if (State.FirstConflict is not { Outcome: null } conflict ||
            State.Address != conflict.Address)
        {
            return ChronicleCommandResult.Rejected(
                "Smash can only be prepared at the unresolved Riven Cairn.");
        }

        if (State.Speed != ChronicleSpeed.Paused)
        {
            return ChronicleCommandResult.Rejected(
                "The Riven Cairn must be paused before Smash can be prepared.");
        }

        if (conflict.PendingAction is not null)
        {
            return ChronicleCommandResult.Rejected(
                "Smash is already prepared for the next active Chronicle tick.");
        }

        State = State with
        {
            FirstConflict = conflict with { PendingAction = slot },
        };
        return ChronicleCommandResult.Succeeded(
            "Prepared Smash for the next active Chronicle tick.");
    }

    private static bool IsCardinal(int deltaX, int deltaY) =>
        (deltaX is -1 or 1 && deltaY == 0) || (deltaX == 0 && deltaY is -1 or 1);

    private HomeSiteSnapshot HomeSiteAt(WorldCell cell)
    {
        if (!State.HasLivingIncarnation)
        {
            return Site(cell, false, "A living Incarnation is required to found Home.");
        }

        if (State.Home is not null)
        {
            return Site(cell, false, "The Chronicle already has its singular Home.");
        }

        if (!string.Equals(
                cell.Address.Stratum,
                SurfacePatch.SurfaceStratum,
                StringComparison.Ordinal))
        {
            return Site(cell, false, "Home must be founded on the surface.");
        }

        if (cell.Feature == WorldFeature.Landmark)
        {
            return Site(cell, false, "A Landmark cannot become Home.");
        }

        if (cell.Feature != WorldFeature.Stone)
        {
            return Site(cell, false, "Home requires an existing Stone feature.");
        }

        if (cell.Ground == WorldGround.Water)
        {
            return Site(cell, false, "The Stone here is under water.");
        }

        if (cell.DurableIdentity is not null)
        {
            return Site(cell, false, "This place already has a durable identity.");
        }

        return Site(cell, true, "The supported Stone here can become Home.");
    }

    private ReturnRouteSnapshot? ReturnRouteTo(HomeState? home)
    {
        if (home is null)
        {
            return null;
        }

        if (!string.Equals(
                State.Address.Stratum,
                home.Address.Stratum,
                StringComparison.Ordinal))
        {
            return new ReturnRouteSnapshot(
                home.Address,
                IsTraversable: false,
                Arrived: false,
                NextAddress: null,
                RemainingSteps: 0);
        }

        if (State.Address == home.Address)
        {
            return new ReturnRouteSnapshot(
                home.Address,
                IsTraversable: true,
                Arrived: true,
                NextAddress: null,
                RemainingSteps: 0);
        }

        var next = State.Address.X != home.Address.X
            ? State.Address with
            {
                X = State.Address.X < home.Address.X
                    ? State.Address.X + 1
                    : State.Address.X - 1,
            }
            : State.Address with
            {
                Y = State.Address.Y < home.Address.Y
                    ? State.Address.Y + 1
                    : State.Address.Y - 1,
            };
        return new ReturnRouteSnapshot(
            home.Address,
            IsTraversable: true,
            Arrived: false,
            NextAddress: next,
            RemainingSteps:
                CardinalDistance(State.Address.X, home.Address.X) +
                CardinalDistance(State.Address.Y, home.Address.Y));
    }

    private static UInt128 CardinalDistance(long first, long second) =>
        first >= second
            ? (UInt128)((Int128)first - second)
            : (UInt128)((Int128)second - first);

    private static HomeSiteSnapshot Site(
        WorldCell cell,
        bool isEligible,
        string reason) =>
        new(
            cell.Address,
            cell.Ground,
            cell.Feature,
            cell.DurableIdentity,
            isEligible,
            reason);

    private static bool IsSlotIndex(int index) => index is >= 0 and < LoadoutState.SlotCount;

    private FittedSubject? FittedFlySubject(LoadoutSlot slot)
    {
        if (!slot.IsFittedFly)
        {
            return null;
        }

        return slot.Noun switch
        {
            var noun when noun == WordIds.Stone && State.LooseStoneAddress is { } address =>
                new FittedSubject(noun.Value, "loose Stone", address),
            var noun when noun == WordIds.Bell =>
                new FittedSubject(noun.Value, "Bell", State.CurrentBellAddress),
            _ => null,
        };
    }

    private ChronicleState MoveFittedSubject(WordId noun, WorldAddress destination) =>
        noun == WordIds.Stone
            ? State with { LooseStoneAddress = destination }
            : noun == WordIds.Bell
                ? State with { BellAddress = destination }
                : State;

    private string? DurableOccupantAt(WorldAddress destination) =>
        WorldArea.Generate(
                State,
                destination.Stratum,
                new WorldRectangle(destination.X, destination.Y, 1, 1))
            .Cells.Single().DurableIdentity switch
        {
            ChronicleState.HomeHearthstoneIdentity => "Home",
            ChronicleState.LooseStoneIdentity => "the loose Stone",
            var identity => identity,
        };

    private static string ExpressionName(LoadoutSlot slot)
    {
        var verb = WordCatalogue.Get(slot.Verb!.Value).DisplayName;
        return slot.Noun is { } noun
            ? $"{verb}[{WordCatalogue.Get(noun).DisplayName}]"
            : verb;
    }

    private static WorldAddress? MatchingStratumDestination(WorldAddress address) =>
        address.Stratum switch
        {
            SurfacePatch.SurfaceStratum => address with { Stratum = SkyStratum.StratumName },
            SkyStratum.StratumName => address with { Stratum = SurfacePatch.SurfaceStratum },
            _ => null,
        };

    private static bool IsAdjacent(WorldAddress first, WorldAddress second) =>
        string.Equals(first.Stratum, second.Stratum, StringComparison.Ordinal) &&
        ((first.X == second.X && AreConsecutive(first.Y, second.Y)) ||
         (first.Y == second.Y && AreConsecutive(first.X, second.X)));

    private static bool AreConsecutive(long first, long second) =>
        (first != long.MaxValue && first + 1 == second) ||
        (second != long.MaxValue && second + 1 == first);

    private ChronicleState Move(MoveIncarnation move)
    {
        if (State.Intent == OpeningIntent.Unchosen)
        {
            return State;
        }

        var destination = new WorldAddress(
            State.Address.Stratum,
            State.Address.X + move.DeltaX,
            State.Address.Y + move.DeltaY);

        var moved = State.Address.Stratum switch
        {
            SurfacePatch.SurfaceStratum => State.TravelTo(destination),
            SkyStratum.StratumName => State.TravelTo(destination),
            _ => State,
        };
        return EnterFirstConflictIfNeeded(moved);
    }

    private static ChronicleState EnterFirstConflictIfNeeded(ChronicleState state)
    {
        if (!state.HasLivingIncarnation ||
            state.WorldGrammarVersion != 3 ||
            state.FirstConflict is not null ||
            state.Address != WorldArea.GeneratedCairnAddress(state.Seed))
        {
            return state;
        }

        return state with
        {
            Speed = ChronicleSpeed.Paused,
            FirstConflict = new FirstConflictState(
                FirstConflictSubjects.RiverWardSubjectId,
                state.Address,
                state.Tick),
        };
    }

    private static int TicksPerClockPulse(ChronicleSpeed speed) => speed switch
    {
        ChronicleSpeed.Paused => 0,
        ChronicleSpeed.Slow => 1,
        ChronicleSpeed.Normal => 2,
        ChronicleSpeed.Fast => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, "Unknown Chronicle speed."),
    };

    private readonly record struct FittedSubject(
        WordId Noun,
        string Name,
        WorldAddress Address);

    private delegate bool TryPowerChange(
        ChronicleState state,
        out ChronicleState updated,
        out string message);
}

public abstract record ChronicleCommand;

public readonly record struct ChronicleCommandResult(bool Applied, string Message)
{
    internal static ChronicleCommandResult Succeeded(string message) => new(true, message);

    internal static ChronicleCommandResult Rejected(string message) => new(false, message);
}

public sealed record SetChronicleSpeed(ChronicleSpeed Speed) : ChronicleCommand;

public sealed record ChooseUpIntent : ChronicleCommand;

public sealed record ChooseHereIntent : ChronicleCommand;

public sealed record ChooseAgainstIntent : ChronicleCommand;

public sealed record ReadBurnPrimer : ChronicleCommand;

public sealed record ChooseStudyWord(StudySourceId SourceId, WordId WordId) : ChronicleCommand;

public sealed record EndIncarnationAtBell : ChronicleCommand;

public sealed record CreateReplacementIncarnation : ChronicleCommand;

public sealed record MoveIncarnation(int DeltaX, int DeltaY) : ChronicleCommand;

public sealed record ConfigureLoadoutSlot(
    int SlotIndex,
    WordId Verb,
    WordId? Noun = null) : ChronicleCommand;

public sealed record ClearLoadoutSlot(int SlotIndex) : ChronicleCommand;

public sealed record UseLoadoutSlot(
    int SlotIndex,
    WorldAddress? Target = null) : ChronicleCommand;

public sealed record AttuneExpression(
    WordId Verb,
    IReadOnlyList<WordId> Modifiers) : ChronicleCommand;

public sealed record ConfigureEngagementPlan(bool OpenWithWeaponStance) : ChronicleCommand;

public sealed record SetWeaponStance(bool Active) : ChronicleCommand;

public sealed record PrepareBurn(WorldAddress Target) : ChronicleCommand;

public sealed record CancelPendingTacticalAction : ChronicleCommand;

public sealed record SkipRecovery : ChronicleCommand;

public sealed record BeginPowerCommitment(PowerCommitmentKind Kind) : ChronicleCommand;

public sealed record LiftResonantLode : ChronicleCommand;

public sealed record SetDownResonantLode : ChronicleCommand;

public sealed record CancelPowerCommitment : ChronicleCommand;
