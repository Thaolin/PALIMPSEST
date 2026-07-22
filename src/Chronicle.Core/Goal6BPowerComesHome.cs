using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Chronicle.Core;

public enum ResonantLodeDisposition
{
    Embedded = 0,
    Loose = 1,
    Carried = 2,
    Committed = 3,
    Installed = 4,
}

public sealed record ResonantLodeState(
    string Identity,
    WorldAddress OriginAddress,
    ResonantLodeDisposition Disposition,
    WorldAddress? Address = null,
    long? CarrierIncarnationId = null);

public enum HearthResonatorPhase
{
    UnderConstruction = 1,
    Intact = 2,
    Damaged = 3,
    Destroyed = 4,
    Rebuilding = 5,
}

public sealed record HearthResonatorState(
    string Identity,
    WorldAddress Address,
    HearthResonatorPhase Phase,
    int Progress);

public enum PowerCommitmentKind
{
    Extract = 1,
    Build = 2,
    Dismantle = 3,
    Rebuild = 4,
}

public sealed record PowerCommitmentState(
    PowerCommitmentKind Kind,
    long ActorIncarnationId,
    string SubjectIdentity,
    WorldAddress Address,
    int CompletedTicks,
    int TotalTicks);

public sealed record PowerHomeState(
    ResonantLodeState Lode,
    int ExtractionProgress = 0,
    HearthResonatorState? Resonator = null,
    PowerCommitmentState? Commitment = null);

public sealed record LoadAttunementState(int Capacity, long Tick);

public sealed record PowerActionSnapshot(
    PowerCommitmentKind? Kind,
    string Id,
    string Label,
    bool Available,
    string Availability,
    string WhatHappensNext,
    string When,
    string Interruptions,
    string Prevents);

public sealed record ResonantLodeSnapshot(
    string Identity,
    WorldAddress OriginAddress,
    ResonantLodeDisposition Disposition,
    WorldAddress? Address,
    long? CarrierIncarnationId,
    string Status);

public sealed record HearthResonatorSnapshot(
    string Identity,
    WorldAddress Address,
    HearthResonatorPhase Phase,
    int Progress,
    int TotalProgress,
    int LoadContribution,
    string Status);

public sealed record BurnPrimerSnapshot(
    string Identity,
    WorldAddress Address,
    bool IsRead,
    string Status);

public sealed record PowerCommitmentSnapshot(
    PowerCommitmentKind Kind,
    string DisplayName,
    WorldAddress Address,
    int CompletedTicks,
    int RemainingTicks,
    int TotalTicks,
    long NextTick,
    bool WaitingForHeartbeat,
    string NextTransition,
    string Interruptions,
    string Prevents);

public sealed record AttunementCapacitySnapshot(
    int CurrentUsedLoad,
    int? CapacityAtLastAttunement,
    long? LastAttunedTick,
    int NextAttunementCapacity,
    int InherentCapacity,
    int SourceContribution,
    string CurrentStatus,
    string NextStatus);

public sealed record PowerComesHomeContextSnapshot(
    WorldAddress SeamAddress,
    string SeamIdentity,
    bool SeamIsEmpty,
    WorldAddress? ResonatorSite,
    BurnPrimerSnapshot BurnPrimer,
    ResonantLodeSnapshot Lode,
    HearthResonatorSnapshot? Resonator,
    PowerCommitmentSnapshot? Commitment,
    AttunementCapacitySnapshot Attunement,
    IReadOnlyList<PowerActionSnapshot> Actions,
    string Summary);

public enum SingingSeamVisualState
{
    Embedded = 1,
    Empty = 2,
}

public sealed record SingingSeamCellState(
    string Identity,
    SingingSeamVisualState State,
    int ExtractionProgress);

public sealed record BurnPrimerCellState(
    string Identity,
    bool IsRead);

public sealed record ResonantLodeCellState(
    string Identity,
    ResonantLodeDisposition Disposition,
    long? CarrierIncarnationId);

public sealed record HearthResonatorCellState(
    string Identity,
    HearthResonatorPhase Phase,
    int Progress,
    int TotalProgress,
    int LoadContribution);

internal readonly record struct PowerAdvanceResult(
    ChronicleState State,
    string? Message,
    WorldAddress? Address);

internal static class Goal6BPowerComesHome
{
    internal const int InherentLoadCapacity = 8;
    internal const int SourceLoadContribution = 4;
    internal const int LinkCapacity = 3;
    internal const int ExtractTicks = 2;
    internal const int BuildTicks = 3;
    internal const int DismantleTicks = 2;
    internal const int RebuildTicks = 3;

    internal static readonly WorldAddress SingingSeamAddress =
        new(SurfacePatch.SurfaceStratum, 8, 3);

    internal static readonly WorldAddress BurnPrimerAddress =
        new(SurfacePatch.SurfaceStratum, 0, 2);

    internal static string SingingSeamIdentity(long seed) =>
        $"place.singing-seam.{seed.ToString(CultureInfo.InvariantCulture)}";

    internal static string ResonantLodeIdentity(long seed) =>
        $"resource.resonant-lode.{seed.ToString(CultureInfo.InvariantCulture)}";

    internal static string HearthResonatorIdentity(long seed) =>
        $"source.hearth-resonator.{seed.ToString(CultureInfo.InvariantCulture)}";

    internal static string BurnPrimerIdentity(long seed) =>
        $"study-source.burn-primer.{seed.ToString(CultureInfo.InvariantCulture)}";

    internal static bool HasBurnPrimerKnowledge(ChronicleState state) =>
        state.Codex.Contains(WordIds.Burn) &&
        state.Codex.Contains(WordIds.Quickly) &&
        state.Codex.Contains(WordIds.Lasting);

    internal static bool IsAvailable(ChronicleState state) =>
        state.WorldGrammarVersion == 5 && state.PowerHome is not null;

    internal static PowerHomeState Create(long seed) => new(
        new ResonantLodeState(
            ResonantLodeIdentity(seed),
            SingingSeamAddress,
            ResonantLodeDisposition.Embedded,
            Address: SingingSeamAddress));

    internal static WorldAddress? ResonatorSite(ChronicleState state) =>
        state.Home is { } home &&
        string.Equals(home.Address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal) &&
        home.Address.X < long.MaxValue
            ? home.Address with { X = home.Address.X + 1 }
            : null;

    internal static bool IsCarrying(ChronicleState state) =>
        IsAvailable(state) &&
        state.PowerHome!.Lode is
        {
            Disposition: ResonantLodeDisposition.Carried,
            CarrierIncarnationId: var carrier,
        } && carrier == state.IncarnationId;

    internal static bool HasCommitment(ChronicleState state) =>
        IsAvailable(state) && state.PowerHome!.Commitment is not null;

    internal static bool SourceContributes(ChronicleState state) =>
        IsAvailable(state) &&
        state.PowerHome!.Resonator?.Phase is
            HearthResonatorPhase.Intact or HearthResonatorPhase.Damaged;

    internal static int NextAttunementCapacity(ChronicleState state) =>
        InherentLoadCapacity + (SourceContributes(state) ? SourceLoadContribution : 0);

    internal static int LinkCapacityFor(ChronicleState state) =>
        IsAvailable(state) ? LinkCapacity : CombatState.Goal6ALinkCapacity;

    internal static int CurrentUsedLoad(ChronicleState state)
    {
        var slot = state.ActiveLoadout.Slots.FirstOrDefault(candidate => candidate.Verb is not null);
        return slot.Verb is { } verb
            ? WordCatalogue.Get(verb).Load + slot.Modifiers.Sum(id => WordCatalogue.Get(id).Load)
            : 0;
    }

    internal static WorldAddress? LodeWorldAddress(ChronicleState state)
    {
        if (!IsAvailable(state))
        {
            return null;
        }

        var lode = state.PowerHome!.Lode;
        return lode.Disposition == ResonantLodeDisposition.Carried
            ? state.Address
            : lode.Address;
    }

    internal static bool BlocksMovement(ChronicleState state, WorldAddress destination)
    {
        if (!IsAvailable(state))
        {
            return false;
        }

        var power = state.PowerHome!;
        if (destination == SingingSeamAddress &&
            power.Lode.Disposition == ResonantLodeDisposition.Embedded)
        {
            return true;
        }

        return power.Resonator is { } source &&
               source.Address == destination &&
               source.Phase != HearthResonatorPhase.Destroyed;
    }

    internal static PowerComesHomeContextSnapshot Snapshot(ChronicleState state)
    {
        if (!IsAvailable(state))
        {
            var emptyLode = new ResonantLodeSnapshot(
                ResonantLodeIdentity(state.Seed),
                SingingSeamAddress,
                ResonantLodeDisposition.Embedded,
                SingingSeamAddress,
                null,
                "This World Grammar pin has no Resonant Lode.");
            return new PowerComesHomeContextSnapshot(
                SingingSeamAddress,
                SingingSeamIdentity(state.Seed),
                SeamIsEmpty: false,
                ResonatorSite: null,
                new BurnPrimerSnapshot(
                    BurnPrimerIdentity(state.Seed),
                    BurnPrimerAddress,
                    IsRead: false,
                    "The Burn Primer is unavailable in this World Grammar pin."),
                emptyLode,
                null,
                null,
                CapacitySnapshot(state),
                [],
                "Power Comes Home is unavailable in this World Grammar pin.");
        }

        var power = state.PowerHome!;
        var lodeAddress = LodeWorldAddress(state);
        var lode = new ResonantLodeSnapshot(
            power.Lode.Identity,
            power.Lode.OriginAddress,
            power.Lode.Disposition,
            lodeAddress,
            power.Lode.CarrierIncarnationId,
            LodeStatus(state));
        var resonator = power.Resonator is { } source
            ? new HearthResonatorSnapshot(
                source.Identity,
                source.Address,
                source.Phase,
                source.Progress,
                TotalFor(source.Phase),
                SourceContributes(state) ? SourceLoadContribution : 0,
                ResonatorStatus(state, source))
            : null;
        var commitment = power.Commitment is { } active
            ? new PowerCommitmentSnapshot(
                active.Kind,
                CommitmentName(active.Kind),
                active.Address,
                active.CompletedTicks,
                active.TotalTicks - active.CompletedTicks,
                active.TotalTicks,
                checked(state.Tick + 1),
                state.Speed == ChronicleSpeed.Paused,
                NextTransition(active),
                "Cancel, hostile damage, or Incarnation death interrupts it; represented material progress remains.",
                "Movement, Weapon actions, Invocation, Attunement, Lift/Set Down, and another commitment.")
            : null;
        var primerIsRead = HasBurnPrimerKnowledge(state);
        return new PowerComesHomeContextSnapshot(
            SingingSeamAddress,
            SingingSeamIdentity(state.Seed),
            power.Lode.Disposition != ResonantLodeDisposition.Embedded,
            ResonatorSite(state),
            new BurnPrimerSnapshot(
                BurnPrimerIdentity(state.Seed),
                BurnPrimerAddress,
                primerIsRead,
                primerIsRead
                    ? "READ — Burn, Quickly, and Lasting remain in the Codex."
                    : "UNREAD — teaches Burn, Quickly, and Lasting for the Goal 6B Load test."),
            lode,
            resonator,
            commitment,
            CapacitySnapshot(state),
            Actions(state),
            Summary(state));
    }

    internal static bool TryBeginCommitment(
        ChronicleState state,
        PowerCommitmentKind kind,
        out ChronicleState updated,
        out string message)
    {
        updated = state;
        if (!IsAvailable(state))
        {
            message = "Power Comes Home actions require a World Grammar v5 Chronicle.";
            return false;
        }

        if (!state.HasLivingIncarnation)
        {
            message = "A replacement Incarnation is required before committing to physical work.";
            return false;
        }

        if (Goal6AActionPlanning.IsImmediateDanger(state))
        {
            message = "Physical work requires safety; leave the Mire Brute's immediate threat range.";
            return false;
        }

        var power = state.PowerHome!;
        if (power.Commitment is not null ||
            state.Combat?.PendingAction is not null ||
            state.Combat?.Preparation is not null)
        {
            message = "Finish or cancel the current commitment or tactical action first.";
            return false;
        }

        PowerCommitmentState commitment;
        switch (kind)
        {
            case PowerCommitmentKind.Extract:
                if (power.Lode.Disposition != ResonantLodeDisposition.Embedded)
                {
                    message = "The Resonant Lode has already left its Singing Seam.";
                    return false;
                }

                if (!AreAdjacent(state.Address, SingingSeamAddress))
                {
                    message = $"Stand cardinally adjacent to the Singing Seam at {SingingSeamAddress}.";
                    return false;
                }

                commitment = NewCommitment(
                    state,
                    kind,
                    power.Lode.Identity,
                    SingingSeamAddress,
                    power.ExtractionProgress,
                    ExtractTicks);
                break;

            case PowerCommitmentKind.Build:
                if (ResonatorSite(state) is not { } buildSite)
                {
                    message = "Found Home before raising a Hearth Resonator.";
                    return false;
                }

                if (!AreAdjacent(state.Address, buildSite))
                {
                    message = $"Stand cardinally adjacent to the highlighted Home site at {buildSite}.";
                    return false;
                }

                if (power.Resonator is null)
                {
                    if (!IsCarrying(state))
                    {
                        message = "Carry the Resonant Lode to the highlighted Home site before building.";
                        return false;
                    }

                    var startedSource = new HearthResonatorState(
                        HearthResonatorIdentity(state.Seed),
                        buildSite,
                        HearthResonatorPhase.UnderConstruction,
                        Progress: 0);
                    power = power with
                    {
                        Lode = power.Lode with
                        {
                            Disposition = ResonantLodeDisposition.Committed,
                            Address = buildSite,
                            CarrierIncarnationId = null,
                        },
                        Resonator = startedSource,
                    };
                }
                else if (power.Resonator is not { Phase: HearthResonatorPhase.UnderConstruction } existing)
                {
                    message = "The sole Hearth Resonator is not awaiting construction.";
                    return false;
                }

                var source = power.Resonator!;
                commitment = NewCommitment(
                    state,
                    kind,
                    source.Identity,
                    source.Address,
                    source.Progress,
                    BuildTicks);
                break;

            case PowerCommitmentKind.Dismantle:
                if (power.Resonator is not { Phase: HearthResonatorPhase.Intact or HearthResonatorPhase.Damaged } dismantled)
                {
                    message = "Only an intact or damaged Hearth Resonator can be dismantled.";
                    return false;
                }

                if (!AreAdjacent(state.Address, dismantled.Address))
                {
                    message = $"Stand cardinally adjacent to the Hearth Resonator at {dismantled.Address}.";
                    return false;
                }

                commitment = NewCommitment(
                    state,
                    kind,
                    dismantled.Identity,
                    dismantled.Address,
                    dismantled.Phase == HearthResonatorPhase.Damaged ? 1 : 0,
                    DismantleTicks);
                break;

            case PowerCommitmentKind.Rebuild:
                if (power.Resonator is not
                    { Phase: HearthResonatorPhase.Destroyed or HearthResonatorPhase.Rebuilding } destroyed)
                {
                    message = "Only the destroyed Hearth Resonator can be rebuilt.";
                    return false;
                }

                if (!AreAdjacent(state.Address, destroyed.Address))
                {
                    message = $"Stand cardinally adjacent to the destroyed Source at {destroyed.Address}.";
                    return false;
                }

                if (destroyed.Phase == HearthResonatorPhase.Destroyed &&
                    (power.Lode is not
                     {
                         Disposition: ResonantLodeDisposition.Loose,
                         Address: var looseAddress,
                     } || looseAddress != destroyed.Address))
                {
                    message = "The same Resonant Lode must be exposed at the destroyed Source.";
                    return false;
                }

                if (destroyed.Phase == HearthResonatorPhase.Destroyed)
                {
                    power = power with
                    {
                        Lode = power.Lode with
                        {
                            Disposition = ResonantLodeDisposition.Committed,
                            CarrierIncarnationId = null,
                        },
                        Resonator = destroyed with
                        {
                            Phase = HearthResonatorPhase.Rebuilding,
                            Progress = 0,
                        },
                    };
                }
                commitment = NewCommitment(
                    state,
                    kind,
                    destroyed.Identity,
                    destroyed.Address,
                    destroyed.Phase == HearthResonatorPhase.Rebuilding ? destroyed.Progress : 0,
                    RebuildTicks);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Power Comes Home commitment.");
        }

        updated = state with
        {
            Speed = ChronicleSpeed.Paused,
            PowerHome = power with { Commitment = commitment },
            Combat = state.Combat is { } combat
                ? combat with { WeaponStanceActive = false }
                : null,
        };
        message = $"{CommitmentName(kind)} pending at {commitment.Address}: " +
                  $"{commitment.CompletedTicks}/{commitment.TotalTicks} Heartbeats. " +
                  "PAUSED — progress waits for SPACE. Cancel, hostile damage, or death interrupts; " +
                  "movement, combat actions, Invocation, Attunement, and other physical work are disabled.";
        return true;
    }

    internal static bool TryReadBurnPrimer(
        ChronicleState state,
        out ChronicleState updated,
        out string message)
    {
        updated = state;
        if (!IsAvailable(state))
        {
            message = "The Burn Primer exists only in the Goal 6B testing Chronicle.";
            return false;
        }

        if (HasBurnPrimerKnowledge(state))
        {
            message = "The Codex already keeps Burn, Quickly, and Lasting.";
            return false;
        }

        if (HasCommitment(state))
        {
            message = "Finish or cancel the physical commitment before reading the Burn Primer.";
            return false;
        }

        if (IsCarrying(state))
        {
            message = "Set down the Resonant Lode before reading the Burn Primer.";
            return false;
        }

        if (Goal6AActionPlanning.IsImmediateDanger(state))
        {
            message = "Leave immediate danger before reading the Burn Primer.";
            return false;
        }

        if (!AreAdjacent(state.Address, BurnPrimerAddress))
        {
            message = $"Stand cardinally adjacent to the Burn Primer at {BurnPrimerAddress}.";
            return false;
        }

        updated = state with
        {
            Codex = state.Codex
                .Learn(WordIds.Burn)
                .Learn(WordIds.Quickly)
                .Learn(WordIds.Lasting),
        };
        message = "Read the Burn Primer. Burn, Quickly, and Lasting entered the persistent Codex; no Heartbeat was spent.";
        return true;
    }

    internal static bool TryLift(
        ChronicleState state,
        out ChronicleState updated,
        out string message)
    {
        updated = state;
        if (!IsAvailable(state))
        {
            message = "There is no Resonant Lode in this World Grammar pin.";
            return false;
        }

        if (HasCommitment(state))
        {
            message = "Cancel the active physical commitment before lifting the Lode.";
            return false;
        }

        var power = state.PowerHome!;
        if (power.Lode is not
            {
                Disposition: ResonantLodeDisposition.Loose,
                Address: { } lodeAddress,
            })
        {
            message = IsCarrying(state)
                ? "The Incarnation already carries the Resonant Lode; only one large object can be carried."
                : "The Resonant Lode is not loose in the world.";
            return false;
        }

        if (!AreAdjacent(state.Address, lodeAddress))
        {
            message = $"Stand cardinally adjacent to the Resonant Lode at {lodeAddress}.";
            return false;
        }

        updated = state with
        {
            PowerHome = power with
            {
                Lode = power.Lode with
                {
                    Disposition = ResonantLodeDisposition.Carried,
                    Address = null,
                    CarrierIncarnationId = state.IncarnationId,
                },
            },
            Combat = state.Combat is { } combat
                ? combat with { WeaponStanceActive = false }
                : null,
        };
        message = "Lifted the Resonant Lode. It is visibly carried by this Incarnation; " +
                  "Iron Cleaver, Burn, Fly, and Attunement remain disabled until Set Down or Build.";
        return true;
    }

    internal static bool TryDrop(
        ChronicleState state,
        out ChronicleState updated,
        out string message)
    {
        updated = state;
        if (!IsCarrying(state))
        {
            message = "This Incarnation is not carrying the Resonant Lode.";
            return false;
        }

        if (HasCommitment(state))
        {
            message = "Cancel the active physical commitment before setting down the Lode.";
            return false;
        }

        var power = state.PowerHome!;
        updated = state with
        {
            PowerHome = power with
            {
                Lode = power.Lode with
                {
                    Disposition = ResonantLodeDisposition.Loose,
                    Address = state.Address,
                    CarrierIncarnationId = null,
                },
            },
        };
        message = $"Set down the Resonant Lode at {state.Address}; combat actions, Fly, and Attunement are available again when otherwise valid.";
        return true;
    }

    internal static bool TryCancel(
        ChronicleState state,
        out ChronicleState updated,
        out string message)
    {
        updated = state;
        if (!IsAvailable(state) || state.PowerHome!.Commitment is not { } commitment)
        {
            message = "There is no physical commitment to cancel.";
            return false;
        }

        updated = state with
        {
            Speed = ChronicleSpeed.Paused,
            PowerHome = state.PowerHome with { Commitment = null },
        };
        message = $"Cancelled {CommitmentName(commitment.Kind)} at {commitment.Address}. " +
                  "The represented material progress remains and can be resumed.";
        return true;
    }

    internal static PowerAdvanceResult AdvanceAfterTick(ChronicleState state)
    {
        if (!IsAvailable(state) || state.PowerHome!.Commitment is not { } commitment)
        {
            return new PowerAdvanceResult(state, null, null);
        }

        if (!state.HasLivingIncarnation || commitment.ActorIncarnationId != state.IncarnationId)
        {
            var interrupted = InterruptCommitment(state);
            return new PowerAdvanceResult(
                interrupted,
                $"{CommitmentName(commitment.Kind)} interrupted because its Incarnation is no longer present; material progress remains.",
                commitment.Address);
        }

        var completed = commitment.CompletedTicks + 1;
        var power = state.PowerHome;
        string message;
        switch (commitment.Kind)
        {
            case PowerCommitmentKind.Extract:
                if (completed < ExtractTicks)
                {
                    power = power with
                    {
                        ExtractionProgress = completed,
                        Commitment = commitment with { CompletedTicks = completed },
                    };
                    message = $"Extraction reaches {completed}/{ExtractTicks}; one more active Heartbeat unseats the Resonant Lode.";
                }
                else
                {
                    power = power with
                    {
                        ExtractionProgress = ExtractTicks,
                        Lode = power.Lode with
                        {
                            Disposition = ResonantLodeDisposition.Loose,
                            Address = SingingSeamAddress,
                            CarrierIncarnationId = null,
                        },
                        Commitment = null,
                    };
                    message = "The Resonant Lode is unseated at its persistent Singing Seam origin.";
                }
                break;

            case PowerCommitmentKind.Build:
                var building = power.Resonator!;
                if (completed < BuildTicks)
                {
                    power = power with
                    {
                        Resonator = building with { Progress = completed },
                        Commitment = commitment with { CompletedTicks = completed },
                    };
                    message = $"Hearth Resonator construction reaches {completed}/{BuildTicks}; the committed Lode remains visible at Home.";
                }
                else
                {
                    power = power with
                    {
                        Lode = power.Lode with { Disposition = ResonantLodeDisposition.Installed },
                        Resonator = building with
                        {
                            Phase = HearthResonatorPhase.Intact,
                            Progress = BuildTicks,
                        },
                        Commitment = null,
                    };
                    message = "Hearth Resonator intact: next Attunement capacity is 12 = 8 inherent + 4 Hearth Resonator. The current Loadout is unchanged.";
                }
                break;

            case PowerCommitmentKind.Dismantle:
                var source = power.Resonator!;
                if (completed < DismantleTicks)
                {
                    power = power with
                    {
                        Resonator = source with
                        {
                            Phase = HearthResonatorPhase.Damaged,
                            Progress = completed,
                        },
                        Commitment = commitment with { CompletedTicks = completed },
                    };
                    message = "Hearth Resonator damaged but still contributes +4 Load; one more dismantling Heartbeat destroys it.";
                }
                else
                {
                    power = power with
                    {
                        Lode = power.Lode with
                        {
                            Disposition = ResonantLodeDisposition.Loose,
                            Address = source.Address,
                            CarrierIncarnationId = null,
                        },
                        Resonator = source with
                        {
                            Phase = HearthResonatorPhase.Destroyed,
                            Progress = DismantleTicks,
                        },
                        Commitment = null,
                    };
                    message = "Hearth Resonator destroyed: current Loadout remains active; next Attunement capacity is 8 inherent. The same Lode is exposed at Home.";
                }
                break;

            case PowerCommitmentKind.Rebuild:
                var rebuilding = power.Resonator!;
                if (completed < RebuildTicks)
                {
                    power = power with
                    {
                        Resonator = rebuilding with { Progress = completed },
                        Commitment = commitment with { CompletedTicks = completed },
                    };
                    message = $"Hearth Resonator rebuilding reaches {completed}/{RebuildTicks}; the same Lode remains committed at Home.";
                }
                else
                {
                    power = power with
                    {
                        Lode = power.Lode with { Disposition = ResonantLodeDisposition.Installed },
                        Resonator = rebuilding with
                        {
                            Phase = HearthResonatorPhase.Intact,
                            Progress = RebuildTicks,
                        },
                        Commitment = null,
                    };
                    message = "Hearth Resonator rebuilt: next Attunement capacity is restored to 12. The current Loadout is unchanged until explicit Attunement.";
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown Power Comes Home commitment '{commitment.Kind}'.");
        }

        return new PowerAdvanceResult(state with { PowerHome = power }, message, commitment.Address);
    }

    internal static ChronicleState InterruptAfterHostileDamage(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results)
    {
        if (!HasCommitment(state))
        {
            return state;
        }

        var commitment = state.PowerHome!.Commitment!;
        var interrupted = InterruptCommitment(state);
        results.Add(new CombatResultSnapshot(
            state.Tick,
            CombatResultKind.PowerHome,
            $"Mire Brute damage interrupts {CommitmentName(commitment.Kind)}; material progress remains.",
            Address: commitment.Address));
        return interrupted;
    }

    internal static ChronicleState EndIncarnation(ChronicleState state)
    {
        if (!IsAvailable(state))
        {
            return state;
        }

        var power = state.PowerHome! with { Commitment = null };
        if (power.Lode.Disposition == ResonantLodeDisposition.Carried &&
            power.Lode.CarrierIncarnationId == state.IncarnationId)
        {
            power = power with
            {
                Lode = power.Lode with
                {
                    Disposition = ResonantLodeDisposition.Loose,
                    Address = state.Address,
                    CarrierIncarnationId = null,
                },
            };
        }

        return state with { PowerHome = power };
    }

    internal static ChronicleState InterruptCommitment(ChronicleState state) =>
        IsAvailable(state) && state.PowerHome!.Commitment is not null
            ? state with { PowerHome = state.PowerHome with { Commitment = null } }
            : state;

    private static AttunementCapacitySnapshot CapacitySnapshot(ChronicleState state)
    {
        var next = IsAvailable(state)
            ? NextAttunementCapacity(state)
            : InherentLoadCapacity;
        var contribution = next - InherentLoadCapacity;
        var current = CurrentUsedLoad(state);
        var attunement = state.Attunement;
        var currentStatus = attunement is null
            ? $"CURRENT: none. This body must Attune under the {next}-Load limit."
            : $"CURRENT: {current}/{attunement.Capacity} (Attuned H{attunement.Tick}). Stays active until another Attunement or death.";
        var sourceText = contribution > 0
            ? "8 inherent + 4 Hearth Resonator = 12"
            : "8 inherent; Resonator +0 while absent, unfinished, or destroyed";
        var applicationText = contribution > 0
            ? " Press G to apply it; building alone changes nothing."
            : " 12 Load will not fit; CURRENT stays active.";
        return new AttunementCapacitySnapshot(
            current,
            attunement?.Capacity,
            attunement?.Tick,
            next,
            InherentLoadCapacity,
            contribution,
            currentStatus,
            $"NEXT ATTUNEMENT: {next} = {sourceText}.{applicationText}");
    }

    private static IReadOnlyList<PowerActionSnapshot> Actions(ChronicleState state)
    {
        var power = state.PowerHome!;
        var committed = power.Commitment is not null;
        var safe = !Goal6AActionPlanning.IsImmediateDanger(state);
        var lodeAddress = power.Lode.Address;
        var site = ResonatorSite(state);
        var looseIsAdjacent = lodeAddress is { } looseAddress &&
                              AreAdjacent(state.Address, looseAddress);
        var dismantleSource = power.Resonator is
            { Phase: HearthResonatorPhase.Intact or HearthResonatorPhase.Damaged }
            ? power.Resonator
            : null;
        var rebuildSource = power.Resonator is
            { Phase: HearthResonatorPhase.Destroyed or HearthResonatorPhase.Rebuilding }
            ? power.Resonator
            : null;
        return Array.AsReadOnly(new[]
        {
            new PowerActionSnapshot(
                null,
                "read-primer",
                "READ BURN PRIMER",
                !HasBurnPrimerKnowledge(state) && safe && !committed && !IsCarrying(state) &&
                AreAdjacent(state.Address, BurnPrimerAddress),
                HasBurnPrimerKnowledge(state)
                    ? "The Burn Primer has already been read."
                    : !safe
                        ? "Leave immediate danger before reading."
                        : committed
                            ? "Finish or cancel the current commitment."
                            : IsCarrying(state)
                                ? "Set down the Resonant Lode before reading."
                                : !AreAdjacent(state.Address, BurnPrimerAddress)
                                    ? "Stand next to BURN PRIMER."
                                    : "Available.",
                "Add Burn, Quickly, and Lasting to the persistent Codex.",
                "Immediate; no Heartbeat is spent.",
                "Invalid state rejects without mutation.",
                "Nothing after success."),
            Action(
                PowerCommitmentKind.Extract,
                "extract",
                "EXTRACT LODE",
                safe && !committed && power.Lode.Disposition == ResonantLodeDisposition.Embedded && AreAdjacent(state.Address, SingingSeamAddress),
                power.Lode.Disposition != ResonantLodeDisposition.Embedded
                    ? "The Lode has already been extracted."
                    : !safe
                        ? "Leave immediate danger before extracting."
                        : !AreAdjacent(state.Address, SingingSeamAddress)
                            ? "Stand next to GOLD SEAM."
                            : committed ? "Finish or cancel the current commitment." : "Available.",
                "Unseat the Resonant Lode and leave the Singing Seam visibly empty.",
                $"{ExtractTicks - power.ExtractionProgress} active Heartbeat(s); paused time does not advance it."),
            new PowerActionSnapshot(
                null,
                "lift",
                "LIFT LODE",
                safe && !committed && power.Lode.Disposition == ResonantLodeDisposition.Loose && looseIsAdjacent,
                power.Lode.Disposition != ResonantLodeDisposition.Loose
                    ? "The Lode is not loose in the world."
                    : !looseIsAdjacent
                        ? "Stand next to loose GOLD LODE."
                        : committed ? "Finish or cancel the current commitment." : "Available.",
                "Attach the Lode visibly to this Incarnation.",
                "Immediate; no Heartbeat is spent.",
                "Invalid state rejects without mutation.",
                "A second carried object; Cleaver, Burn, Fly, and Attunement until Set Down or Build."),
            new PowerActionSnapshot(
                null,
                "drop",
                "SET DOWN",
                IsCarrying(state) && !committed,
                !IsCarrying(state) ? "This Incarnation is not carrying the Lode." : committed ? "Finish or cancel the current commitment." : "Available.",
                "Place exactly one loose Resonant Lode on the carrier's current cell.",
                "Immediate; no Heartbeat is spent.",
                "Invalid destination rejects without mutation.",
                "Nothing after success."),
            Action(
                PowerCommitmentKind.Build,
                "build",
                power.Resonator?.Phase == HearthResonatorPhase.UnderConstruction ? "RESUME BUILD" : "BUILD",
                safe && !committed && site is { } buildSite && AreAdjacent(state.Address, buildSite) &&
                (IsCarrying(state) || power.Resonator?.Phase == HearthResonatorPhase.UnderConstruction),
                site is null
                    ? "Found Home first."
                    : !safe
                        ? "Leave immediate danger before building."
                        : !AreAdjacent(state.Address, site.Value)
                            ? "Stand next to OUTLINED HOME SITE."
                            : !IsCarrying(state) && power.Resonator?.Phase != HearthResonatorPhase.UnderConstruction
                                ? "Carry the Resonant Lode to Home."
                                : committed ? "Finish or cancel the current commitment." : "Available.",
                "Raise the sole Hearth Resonator around the same physical Lode.",
                $"{BuildTicks - (power.Resonator?.Phase == HearthResonatorPhase.UnderConstruction ? power.Resonator.Progress : 0)} active Heartbeat(s); paused time does not advance it."),
            Action(
                PowerCommitmentKind.Dismantle,
                "dismantle",
                "DISMANTLE",
                safe && !committed && dismantleSource is { } && AreAdjacent(state.Address, dismantleSource.Address),
                dismantleSource is null
                    ? "An intact or damaged Hearth Resonator is required."
                    : !AreAdjacent(state.Address, dismantleSource.Address)
                        ? "Stand next to RESONATOR."
                        : !safe ? "Leave immediate danger before dismantling." : committed ? "Finish or cancel the current commitment." : "Available.",
                "Remove one brace. The first active Heartbeat leaves the Resonator DAMAGED but still worth +4; the second DESTROYS it and exposes the same Lode.",
                $"{(power.Resonator?.Phase == HearthResonatorPhase.Damaged ? 1 : DismantleTicks)} active Heartbeat(s)."),
            Action(
                PowerCommitmentKind.Rebuild,
                "rebuild",
                power.Resonator?.Phase == HearthResonatorPhase.Rebuilding ? "RESUME REBUILD" : "REBUILD",
                safe && !committed && rebuildSource is { } && AreAdjacent(state.Address, rebuildSource.Address),
                rebuildSource is null
                    ? "A destroyed Hearth Resonator is required."
                    : !AreAdjacent(state.Address, rebuildSource.Address)
                        ? "Stand next to DESTROYED RESONATOR."
                        : !safe ? "Leave immediate danger before rebuilding." : committed ? "Finish or cancel the current commitment." : "Available.",
                "Raise the same Source around the exposed Lode and restore +4 future capacity.",
                $"{RebuildTicks - (power.Resonator?.Phase == HearthResonatorPhase.Rebuilding ? power.Resonator.Progress : 0)} active Heartbeat(s)."),
        });
    }

    private static PowerActionSnapshot Action(
        PowerCommitmentKind kind,
        string id,
        string label,
        bool available,
        string availability,
        string next,
        string when) => new(
            kind,
            id,
            label,
            available,
            availability,
            next,
            when,
            "Cancel, hostile damage, or Incarnation death; represented progress remains.",
            "Movement, Weapon actions, Invocation, Attunement, Lift/Set Down, and another commitment while active.");

    private static string Summary(ChronicleState state)
    {
        var power = state.PowerHome!;
        if (power.Commitment is { } commitment)
        {
            var advance = state.Speed == ChronicleSpeed.Paused
                ? "[ ] SPACE — run 1 Heartbeat"
                : $"[ ] H{state.Tick + 1} — work advances";
            return $"CHECKLIST · {CommitmentChecklistName(commitment.Kind)} {commitment.CompletedTicks}/{commitment.TotalTicks}\n" +
                   advance + "\n" +
                   $"[ ] NEXT — {ChecklistTransition(commitment)}\n" +
                   "STOPS: X / damage / death · progress stays\n" +
                   "LOCKS: move / fight / invoke / Attune / carry / other work";
        }

        if (!HasBurnPrimerKnowledge(state))
        {
            var travelStep = AreAdjacent(state.Address, BurnPrimerAddress)
                ? "[x] Standing beside BURN PRIMER"
                : $"[ ] Go toward BURN PRIMER: {RelativeOffset(state.Address, BurnPrimerAddress)}";
            return "CHECKLIST · LEARN BURN\n" +
                   travelStep + "\n" +
                   "[ ] P — Read (instant)\n" +
                   "[ ] NEXT — Burn + Quickly + Lasting enter Codex\n" +
                   "STOPS: nothing · LOCKS: nothing";
        }

        if (IsCarrying(state))
        {
            var site = ResonatorSite(state);
            var travelStep = site is null
                ? "[ ] Find HOME"
                : AreAdjacent(state.Address, site.Value)
                    ? "[x] Standing beside OUTLINED SITE"
                    : $"[ ] Go toward HOME: {RelativeOffset(state.Address, site.Value)}";
            return "CHECKLIST · CARRY LODE HOME\n" +
                   travelStep + "\n" +
                   "[ ] P — Build (3 Heartbeats)\n" +
                   "STOPS WORK: X / damage / death · progress stays\n" +
                   "CARRYING LOCKS: Cleaver / Burn / Fly / Attune";
        }

        if (power.Lode.Disposition == ResonantLodeDisposition.Embedded)
        {
            var travelStep = AreAdjacent(state.Address, SingingSeamAddress)
                ? "[x] Standing beside GOLD SEAM"
                : $"[ ] Go toward GOLD SEAM: {RelativeOffset(state.Address, SingingSeamAddress)}";
            return "CHECKLIST · GET THE GOLD LODE\n" +
                   travelStep + "\n" +
                   "[ ] P — Extract (2 Heartbeats)\n" +
                   "STOPS: X / damage / death · progress stays\n" +
                   "LOCKS: move / fight / invoke / Attune / carry / other work";
        }

        if (power.Resonator is null && power.Lode.Disposition == ResonantLodeDisposition.Loose)
        {
            var lodeAddress = power.Lode.Address;
            var travelStep = lodeAddress is { } address && AreAdjacent(state.Address, address)
                ? "[x] Standing beside GOLD LODE"
                : lodeAddress is { } destination
                    ? $"[ ] Go toward GOLD LODE: {RelativeOffset(state.Address, destination)}"
                    : "[ ] Find the GOLD LODE";
            return "CHECKLIST · LIFT GOLD LODE\n" +
                   travelStep + "\n" +
                   "[ ] P — Lift (instant)\n" +
                   "[ ] Carry it to HOME\n" +
                   "CARRYING LOCKS: Cleaver / Burn / Fly / Attune";
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.UnderConstruction } construction)
        {
            return "CHECKLIST · FINISH RESONATOR\n" +
                   AdjacentChecklistStep(state, construction.Address, "OUTLINED SITE") + "\n" +
                   $"[ ] P — Resume Build ({BuildTicks - construction.Progress} Heartbeats)\n" +
                   "STOPS: X / damage / death · progress stays\n" +
                   "LOCKS: move / fight / invoke / Attune / carry / other work";
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Intact })
        {
            var isTwelveLoadCurrent = state.Attunement?.Capacity == InherentLoadCapacity + SourceLoadContribution;
            return isTwelveLoadCurrent
                ? "CHECKLIST · TEST SOURCE LOSS\n" +
                  "[x] CURRENT Loadout uses 12; source loss will not disable it\n" +
                  "[ ] P — Dismantle (2 Heartbeats)\n" +
                  "[ ] NEXT — damaged, then destroyed; NEXT Attunement falls to 8\n" +
                  "STOPS: X / damage / death · LOCKS: move / fight / invoke / Attune"
                : "CHECKLIST · USE NEW LOAD\n" +
                  "[x] Resonator gives +4 at NEXT Attunement\n" +
                  "[ ] G — Attune Burn + Quickly + Lasting (12/12)\n" +
                  "[ ] NEXT — Loadout changes at Attunement\n" +
                  "BLOCKED BY: carrying / work / immediate danger";
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Damaged })
        {
            return "CHECKLIST · FINISH DISMANTLING\n" +
                   "[x] Resonator damaged; still +4 at NEXT Attunement\n" +
                   "[ ] P — Destroy (1 Heartbeat)\n" +
                   "[ ] NEXT — NEXT Attunement falls to 8; CURRENT stays\n" +
                   "STOPS: X / damage / death · LOCKS: move / fight / invoke / Attune";
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Destroyed })
        {
            return "CHECKLIST · REBUILD POWER\n" +
                   "[x] Source destroyed: NEXT Attunement 8; CURRENT stays\n" +
                   "[ ] P — Rebuild (3 Heartbeats)\n" +
                   "[ ] NEXT — intact source restores NEXT Attunement to 12\n" +
                   "STOPS: X / damage / death · LOCKS: move / fight / invoke / Attune";
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Rebuilding } rebuilding)
        {
            return "CHECKLIST · FINISH REBUILD\n" +
                   AdjacentChecklistStep(state, rebuilding.Address, "DESTROYED RESONATOR") + "\n" +
                   $"[ ] P — Resume Rebuild ({RebuildTicks - rebuilding.Progress} Heartbeats)\n" +
                   "[ ] NEXT — intact source restores NEXT Attunement to 12\n" +
                   "STOPS: X / damage / death · LOCKS: move / fight / invoke / Attune";
        }

        return "CHECKLIST · RETURN THE LODE\n" +
               "[ ] Find the GOLD LODE\n" +
               "[ ] Carry it to HOME";
    }

    private static string AdjacentChecklistStep(ChronicleState state, WorldAddress target, string label) =>
        AreAdjacent(state.Address, target)
            ? $"[x] Standing beside {label}"
            : $"[ ] Go toward {label}: {RelativeOffset(state.Address, target)}";

    private static string CommitmentChecklistName(PowerCommitmentKind kind) => kind switch
    {
        PowerCommitmentKind.Extract => "EXTRACT LODE",
        PowerCommitmentKind.Build => "BUILD RESONATOR",
        PowerCommitmentKind.Dismantle => "DISMANTLE RESONATOR",
        PowerCommitmentKind.Rebuild => "REBUILD RESONATOR",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown commitment kind."),
    };

    private static string ChecklistTransition(PowerCommitmentState commitment) => commitment.Kind switch
    {
        PowerCommitmentKind.Extract when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            "Seam shows extraction progress",
        PowerCommitmentKind.Extract => "Lode becomes loose; Seam becomes empty",
        PowerCommitmentKind.Build when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            "Construction advances",
        PowerCommitmentKind.Build => "Resonator becomes intact; +4 ready for NEXT Attunement",
        PowerCommitmentKind.Dismantle when commitment.CompletedTicks == 0 =>
            "Resonator becomes damaged; still +4 at NEXT Attunement",
        PowerCommitmentKind.Dismantle => "Resonator destroyed; NEXT Attunement falls to 8",
        PowerCommitmentKind.Rebuild when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            "Rebuild advances",
        PowerCommitmentKind.Rebuild => "Resonator intact; NEXT Attunement returns to 12",
        _ => "Work advances",
    };

    private static string RelativeOffset(WorldAddress from, WorldAddress to)
    {
        if (!string.Equals(from.Stratum, to.Stratum, StringComparison.Ordinal))
        {
            return $"in {to.Stratum}";
        }

        var parts = new List<string>(2);
        var deltaX = (BigInteger)to.X - from.X;
        var deltaY = (BigInteger)to.Y - from.Y;
        if (!deltaX.IsZero)
        {
            var distance = BigInteger.Abs(deltaX);
            parts.Add($"{distance} {(distance == BigInteger.One ? "TILE" : "TILES")} {(deltaX.Sign > 0 ? "EAST" : "WEST")}");
        }

        if (!deltaY.IsZero)
        {
            var distance = BigInteger.Abs(deltaY);
            parts.Add($"{distance} {(distance == BigInteger.One ? "TILE" : "TILES")} {(deltaY.Sign > 0 ? "SOUTH" : "NORTH")}");
        }

        return parts.Count == 0 ? "HERE" : string.Join(" and ", parts);
    }

    private static string LodeStatus(ChronicleState state)
    {
        var lode = state.PowerHome!.Lode;
        return lode.Disposition switch
        {
            ResonantLodeDisposition.Embedded =>
                $"Resonant Lode embedded at its Singing Seam origin {lode.OriginAddress}; extraction {state.PowerHome.ExtractionProgress}/{ExtractTicks}.",
            ResonantLodeDisposition.Loose =>
                $"Resonant Lode loose at {lode.Address}; persistent origin {lode.OriginAddress}.",
            ResonantLodeDisposition.Carried =>
                $"Resonant Lode carried by Incarnation {lode.CarrierIncarnationId} at {state.Address}; persistent origin {lode.OriginAddress}.",
            ResonantLodeDisposition.Committed =>
                $"Resonant Lode committed to the Hearth Resonator at {lode.Address}; persistent origin {lode.OriginAddress}.",
            ResonantLodeDisposition.Installed =>
                $"Resonant Lode installed in the Hearth Resonator at {lode.Address}; persistent origin {lode.OriginAddress}.",
            _ => throw new InvalidOperationException($"Unknown Resonant Lode state '{lode.Disposition}'."),
        };
    }

    private static string ResonatorStatus(ChronicleState state, HearthResonatorState source) => source.Phase switch
    {
        HearthResonatorPhase.UnderConstruction =>
            $"UNDER CONSTRUCTION {source.Progress}/{BuildTicks}; contributes 0 until complete.",
        HearthResonatorPhase.Intact =>
            "INTACT — contributes +4 to the next Attunement; current Loadout changes only by explicit Attunement.",
        HearthResonatorPhase.Damaged =>
            "DAMAGED — still contributes +4; one more dismantling Heartbeat destroys it.",
        HearthResonatorPhase.Destroyed =>
            "DESTROYED — contributes 0; current Loadout remains until Attunement or death; the same Lode is exposed.",
        HearthResonatorPhase.Rebuilding =>
            $"REBUILDING {source.Progress}/{RebuildTicks}; contributes 0 until complete.",
        _ => throw new InvalidOperationException($"Unknown Hearth Resonator phase '{source.Phase}'."),
    };

    private static int TotalFor(HearthResonatorPhase phase) => phase switch
    {
        HearthResonatorPhase.UnderConstruction => BuildTicks,
        HearthResonatorPhase.Damaged or HearthResonatorPhase.Destroyed => DismantleTicks,
        HearthResonatorPhase.Rebuilding => RebuildTicks,
        HearthResonatorPhase.Intact => BuildTicks,
        _ => 0,
    };

    private static string NextTransition(PowerCommitmentState commitment) => commitment.Kind switch
    {
        PowerCommitmentKind.Extract when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            "Extraction progress remains on the Singing Seam.",
        PowerCommitmentKind.Extract => "The Lode becomes loose and the persistent Seam becomes empty.",
        PowerCommitmentKind.Build when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            "The visible foundation advances one construction step.",
        PowerCommitmentKind.Build => "The Resonator becomes intact and offers +4 at the next Attunement.",
        PowerCommitmentKind.Dismantle when commitment.CompletedTicks == 0 =>
            "The Source becomes visibly damaged but still contributes +4.",
        PowerCommitmentKind.Dismantle =>
            "The Source becomes destroyed, next capacity falls to 8, and the same Lode is exposed.",
        PowerCommitmentKind.Rebuild when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            "The visible rebuilding state advances one step.",
        PowerCommitmentKind.Rebuild =>
            "The Source becomes intact and restores +4 at the next Attunement.",
        _ => "The physical commitment advances.",
    };

    private static PowerCommitmentState NewCommitment(
        ChronicleState state,
        PowerCommitmentKind kind,
        string subjectIdentity,
        WorldAddress address,
        int completed,
        int total) => new(
            kind,
            state.IncarnationId,
            subjectIdentity,
            address,
            completed,
            total);

    private static string CommitmentName(PowerCommitmentKind kind) => kind switch
    {
        PowerCommitmentKind.Extract => "Extract Resonant Lode",
        PowerCommitmentKind.Build => "Build Hearth Resonator",
        PowerCommitmentKind.Dismantle => "Dismantle Hearth Resonator",
        PowerCommitmentKind.Rebuild => "Rebuild Hearth Resonator",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown commitment kind."),
    };

    internal static bool AreAdjacent(WorldAddress first, WorldAddress second) =>
        string.Equals(first.Stratum, second.Stratum, StringComparison.Ordinal) &&
        ((first.X == second.X && AreConsecutive(first.Y, second.Y)) ||
         (first.Y == second.Y && AreConsecutive(first.X, second.X)));

    private static bool AreConsecutive(long first, long second) =>
        (first != long.MaxValue && first + 1 == second) ||
        (second != long.MaxValue && second + 1 == first);
}
