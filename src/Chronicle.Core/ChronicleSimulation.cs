namespace Chronicle.Core;

public sealed class ChronicleSimulation
{
    public ChronicleSimulation(ChronicleState initialState)
    {
        State = initialState;
    }

    public ChronicleState State { get; private set; }

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

        var before = State;
        State = command switch
        {
            SetChronicleSpeed setSpeed => State.WithSpeed(setSpeed.Speed),
            ChooseUpIntent => State.Intent == OpeningIntent.Unchosen
                ? State.WithIntent(OpeningIntent.Up)
                : State,
            StudySkyStone => State.BeginStoneStudy(),
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

    private ChronicleCommandResult ConfigureSlot(ConfigureLoadoutSlot command)
    {
        if (!IsSlotIndex(command.SlotIndex))
        {
            return ChronicleCommandResult.Rejected("That Loadout slot does not exist.");
        }

        if (command.Verb != ChronicleVerb.Fly)
        {
            return ChronicleCommandResult.Rejected("That Verb is unknown.");
        }

        if (!State.Codex.HasFly)
        {
            return ChronicleCommandResult.Rejected("Fly is not in the Codex.");
        }

        if (command.Noun is { } noun && noun != ChronicleNoun.Stone)
        {
            return ChronicleCommandResult.Rejected("That Noun is incompatible with Fly.");
        }

        if (command.Noun == ChronicleNoun.Stone && !State.Codex.HasStone)
        {
            return ChronicleCommandResult.Rejected("Stone is not in the Codex.");
        }

        if (State.ActiveLoadout.Slots
            .Where((slot, index) => index != command.SlotIndex)
            .Any(slot => slot.Verb == command.Verb))
        {
            return ChronicleCommandResult.Rejected("Fly already occupies another Loadout slot.");
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

    private static bool IsCardinal(int deltaX, int deltaY) =>
        (deltaX is -1 or 1 && deltaY == 0) || (deltaX == 0 && deltaY is -1 or 1);

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

public sealed record StudySkyStone : ChronicleCommand;

public sealed record EndIncarnationAtBell : ChronicleCommand;

public sealed record CreateReplacementIncarnation : ChronicleCommand;

public sealed record MoveIncarnation(int DeltaX, int DeltaY) : ChronicleCommand;

public sealed record ConfigureLoadoutSlot(
    int SlotIndex,
    ChronicleVerb Verb,
    ChronicleNoun? Noun = null) : ChronicleCommand;

public sealed record ClearLoadoutSlot(int SlotIndex) : ChronicleCommand;

public sealed record UseLoadoutSlot(
    int SlotIndex,
    WorldAddress? Target = null) : ChronicleCommand;
