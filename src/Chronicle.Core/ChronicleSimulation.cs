namespace Chronicle.Core;

public sealed class ChronicleSimulation
{
    public ChronicleSimulation(ChronicleState initialState)
    {
        State = initialState;
    }

    public ChronicleState State { get; private set; }

    public StudySourceSnapshot? CurrentStudySource =>
        StudySourceGrammar.At(State, State.Address);

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
            SetChronicleSpeed setSpeed => State.WithSpeed(setSpeed.Speed),
            ChooseUpIntent => State.Intent == OpeningIntent.Unchosen
                ? State.WithIntent(OpeningIntent.Up)
                : State,
            ChooseHereIntent => State.Intent == OpeningIntent.Unchosen
                ? State.WithIntent(OpeningIntent.Here)
                : State,
            EndIncarnationAtBell => State.EndIncarnationAtBell(),
            CreateReplacementIncarnation => State.CreateReplacementIncarnation(),
            MoveIncarnation move when !IsCardinal(move.DeltaX, move.DeltaY) => throw new ArgumentException(
                "Incarnation movement must be exactly one cardinal step.",
                nameof(command)),
            MoveIncarnation move => Move(move),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown Chronicle command."),
        };

        return new ChronicleCommandResult(
            Applied: State != before,
            Message: State == before ? "Nothing changed." : string.Empty);
    }

    public void AdvanceOneTick() => State = State.AdvanceTick();

    public void AdvanceClockPulse()
    {
        for (var tick = TicksPerClockPulse(State.Speed); tick > 0; tick--)
        {
            AdvanceOneTick();
        }
    }

    public IReadOnlyList<WorldAddress> ValidTargetsForSlot(int slotIndex)
    {
        if (!State.HasLivingIncarnation || !IsSlotIndex(slotIndex))
        {
            return [];
        }

        var slot = State.ActiveLoadout[slotIndex];
        if (!slot.IsFlyStone ||
            State.LooseStoneAddress is not { } stoneAddress ||
            !IsAdjacent(State.Address, stoneAddress))
        {
            return [];
        }

        return [stoneAddress];
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

        if (slot.IsIntrinsicFly)
        {
            if (command.Target is not null)
            {
                return ChronicleCommandResult.Rejected("Intrinsic Fly acts on the Incarnation and takes no target.");
            }

            if (FlyDestination is not { } destination)
            {
                return ChronicleCommandResult.Rejected("Fly has nowhere to go from this Stratum.");
            }

            State = State.TravelTo(destination);
            return ChronicleCommandResult.Succeeded($"Flew to {destination}.");
        }

        if (slot.IsIntrinsicFound)
        {
            return FoundAtCurrentSite(command);
        }

        if (!slot.IsFlyStone)
        {
            return ChronicleCommandResult.Rejected("That Loadout expression is incompatible.");
        }

        if (command.Target is not { } target)
        {
            return ChronicleCommandResult.Rejected("Choose the adjacent loose Stone.");
        }

        if (State.LooseStoneAddress is not { } stoneAddress || target != stoneAddress)
        {
            return ChronicleCommandResult.Rejected("Fly[Stone] can only target the loose Stone.");
        }

        if (!IsAdjacent(State.Address, stoneAddress))
        {
            return ChronicleCommandResult.Rejected("The loose Stone must be adjacent.");
        }

        var stoneDestination = stoneAddress.Stratum switch
        {
            SurfacePatch.SurfaceStratum => stoneAddress with { Stratum = SkyStratum.StratumName },
            SkyStratum.StratumName => stoneAddress with { Stratum = SurfacePatch.SurfaceStratum },
            _ => throw new InvalidOperationException("The loose Stone occupies an unsupported Stratum."),
        };

        State = State with { LooseStoneAddress = stoneDestination };
        return ChronicleCommandResult.Succeeded($"Fly[Stone] moved the loose Stone to {stoneDestination}.");
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

        return State.Address.Stratum switch
        {
            SurfacePatch.SurfaceStratum => State.TravelTo(destination),
            SkyStratum.StratumName => State.TravelTo(destination),
            _ => State,
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
