using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using static Chronicle.Core.HoldingFacts;

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

public enum PowerActionAvailabilityReason
{
    Available = 1,
    AlreadyRead = 2,
    ImmediateDanger = 3,
    CommitmentActive = 4,
    CarryingLode = 5,
    PrimerOutOfReach = 6,
    AlreadyExtracted = 7,
    SeamOutOfReach = 8,
    LodeNotLoose = 9,
    LodeOutOfReach = 10,
    NotCarryingLode = 11,
    HomeMissing = 12,
    SiteOutOfReach = 13,
    LodeNotAtHome = 14,
    SourceUnavailable = 15,
    SourceOutOfReach = 16,
}

public sealed record PowerActionSnapshot(
    PowerCommitmentKind? Kind,
    string Id,
    bool IsResumption,
    bool Available,
    PowerActionAvailabilityReason AvailabilityReason,
    int Heartbeats,
    HoldingOutcome Outcome,
    IReadOnlyList<HoldingConstraint> Constraints);

public sealed record ResonantLodeSnapshot(
    string Identity,
    WorldAddress OriginAddress,
    ResonantLodeDisposition Disposition,
    WorldAddress? Address,
    long? CarrierIncarnationId);

public sealed record HearthResonatorSnapshot(
    string Identity,
    WorldAddress Address,
    HearthResonatorPhase Phase,
    int Progress,
    int TotalProgress,
    int LoadContribution);

public sealed record BurnPrimerSnapshot(
    string Identity,
    WorldAddress Address,
    bool IsRead);

public sealed record PowerCommitmentSnapshot(
    PowerCommitmentKind Kind,
    WorldAddress Address,
    int CompletedTicks,
    int RemainingTicks,
    int TotalTicks,
    long NextTick,
    bool WaitingForHeartbeat,
    HoldingOutcome NextOutcome,
    IReadOnlyList<HoldingConstraint> Constraints);

public sealed record AttunementCapacitySnapshot(
    int CurrentUsedLoad,
    int? CapacityAtLastAttunement,
    long? LastAttunedTick,
    int NextAttunementCapacity,
    int InherentCapacity,
    int SourceContribution,
    int MaximumSourceContribution,
    int DesiredExpressionLoad);

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
    HoldingObjectiveSnapshot Objective);

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

internal static class HoldingRules
{
    internal const int ExtractTicks = 2;
    internal const int BuildTicks = 3;
    internal const int DismantleTicks = 2;
    internal const int RebuildTicks = 3;

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


    internal static PowerComesHomeContextSnapshot Snapshot(ChronicleState state)
    {
        if (!IsAvailable(state))
        {
            var emptyLode = new ResonantLodeSnapshot(
                ResonantLodeIdentity(state.Seed),
                SingingSeamAddress,
                ResonantLodeDisposition.Embedded,
                SingingSeamAddress,
                null);
            return new PowerComesHomeContextSnapshot(
                SingingSeamAddress,
                SingingSeamIdentity(state.Seed),
                SeamIsEmpty: false,
                ResonatorSite: null,
                new BurnPrimerSnapshot(
                    BurnPrimerIdentity(state.Seed),
                    BurnPrimerAddress,
                    IsRead: false),
                emptyLode,
                null,
                null,
                CapacitySnapshot(state),
                [],
                UnavailableObjective);
        }

        var power = state.PowerHome!;
        var lodeAddress = LodeWorldAddress(state);
        var lode = new ResonantLodeSnapshot(
            power.Lode.Identity,
            power.Lode.OriginAddress,
            power.Lode.Disposition,
            lodeAddress,
            power.Lode.CarrierIncarnationId);
        var resonator = power.Resonator is { } source
            ? new HearthResonatorSnapshot(
                source.Identity,
                source.Address,
                source.Phase,
                source.Progress,
                TotalFor(source.Phase),
                SourceContributes(state) ? SourceLoadContribution : 0)
            : null;
        var commitment = power.Commitment is { } active
            ? new PowerCommitmentSnapshot(
                active.Kind,
                active.Address,
                active.CompletedTicks,
                active.TotalTicks - active.CompletedTicks,
                active.TotalTicks,
                checked(state.Tick + 1),
                state.Speed == ChronicleSpeed.Paused,
                CommitmentOutcome(active),
                Array.AsReadOnly(new[]
                {
                    HoldingConstraint.HostileInterruptionKeepsProgress,
                    HoldingConstraint.LocksAllOtherActionsWhileActive,
                }))
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
                primerIsRead),
            lode,
            resonator,
            commitment,
            CapacitySnapshot(state),
            Actions(state),
            Objective(state));
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

        if (CombatRules.IsImmediateDanger(state))
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

                if (!IsWithinInteractionReach(state.Address, SingingSeamAddress))
                {
                    message = $"Stand on or cardinally adjacent to the Singing Seam at {SingingSeamAddress}.";
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

                if (!IsWithinInteractionReach(state.Address, buildSite))
                {
                    message = $"Stand on or cardinally adjacent to the highlighted Home site at {buildSite}.";
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

                if (!IsWithinInteractionReach(state.Address, dismantled.Address))
                {
                    message = $"Stand on or cardinally adjacent to the Hearth Resonator at {dismantled.Address}.";
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

                if (!IsWithinInteractionReach(state.Address, destroyed.Address))
                {
                    message = $"Stand on or cardinally adjacent to the destroyed Source at {destroyed.Address}.";
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
                  "PAUSED — progress waits for the next active Heartbeat. " +
                  "Cancel, hostile damage, or death interrupts; " +
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
            message = "This Chronicle contains no Burn Primer.";
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

        if (CombatRules.IsImmediateDanger(state))
        {
            message = "Leave immediate danger before reading the Burn Primer.";
            return false;
        }

        if (!IsWithinInteractionReach(state.Address, BurnPrimerAddress))
        {
            message = $"Stand on or cardinally adjacent to the Burn Primer at {BurnPrimerAddress}.";
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

        if (!IsWithinInteractionReach(state.Address, lodeAddress))
        {
            message = $"Stand on or cardinally adjacent to the Resonant Lode at {lodeAddress}.";
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
        return new AttunementCapacitySnapshot(
            current,
            attunement?.Capacity,
            attunement?.Tick,
            next,
            InherentLoadCapacity,
            contribution,
            SourceLoadContribution,
            WordCatalogue.Get(WordIds.Burn).Load +
            WordCatalogue.Get(WordIds.Quickly).Load +
            WordCatalogue.Get(WordIds.Lasting).Load);
    }

    private static IReadOnlyList<PowerActionSnapshot> Actions(ChronicleState state)
    {
        var power = state.PowerHome!;
        var committed = power.Commitment is not null;
        var safe = !CombatRules.IsImmediateDanger(state);
        var lodeAddress = power.Lode.Address;
        var site = ResonatorSite(state);
        var looseIsInReach = lodeAddress is { } looseAddress &&
                             IsWithinInteractionReach(state.Address, looseAddress);
        var dismantleSource = power.Resonator is
            { Phase: HearthResonatorPhase.Intact or HearthResonatorPhase.Damaged }
            ? power.Resonator
            : null;
        var rebuildSource = power.Resonator is
            { Phase: HearthResonatorPhase.Destroyed or HearthResonatorPhase.Rebuilding }
            ? power.Resonator
            : null;
        var primerInReach = IsWithinInteractionReach(state.Address, BurnPrimerAddress);
        var seamInReach = IsWithinInteractionReach(state.Address, SingingSeamAddress);
        var carrying = IsCarrying(state);
        var building = power.Resonator?.Phase == HearthResonatorPhase.UnderConstruction;
        var siteInReach = site is { } buildSite && IsWithinInteractionReach(state.Address, buildSite);
        return Array.AsReadOnly(new[]
        {
            Action(
                null,
                "read-primer",
                false,
                !HasBurnPrimerKnowledge(state) && safe && !committed && !carrying && primerInReach,
                HasBurnPrimerKnowledge(state) ? PowerActionAvailabilityReason.AlreadyRead :
                !safe ? PowerActionAvailabilityReason.ImmediateDanger :
                committed ? PowerActionAvailabilityReason.CommitmentActive :
                carrying ? PowerActionAvailabilityReason.CarryingLode :
                !primerInReach ? PowerActionAvailabilityReason.PrimerOutOfReach :
                PowerActionAvailabilityReason.Available,
                0,
                HoldingOutcome.BurnWordsEnterCodex,
                [HoldingConstraint.NothingStopsOrLocks]),
            Action(
                PowerCommitmentKind.Extract,
                "extract",
                false,
                safe && !committed && power.Lode.Disposition == ResonantLodeDisposition.Embedded && seamInReach,
                power.Lode.Disposition != ResonantLodeDisposition.Embedded ? PowerActionAvailabilityReason.AlreadyExtracted :
                !safe ? PowerActionAvailabilityReason.ImmediateDanger :
                committed ? PowerActionAvailabilityReason.CommitmentActive :
                !seamInReach ? PowerActionAvailabilityReason.SeamOutOfReach :
                PowerActionAvailabilityReason.Available,
                ExtractTicks - power.ExtractionProgress,
                HoldingOutcome.LodeLooseAndSeamEmpty,
                [HoldingConstraint.HostileInterruptionKeepsProgress, HoldingConstraint.LocksAllOtherActionsWhileActive]),
            Action(
                null,
                "lift",
                false,
                safe && !committed && power.Lode.Disposition == ResonantLodeDisposition.Loose && looseIsInReach,
                power.Lode.Disposition != ResonantLodeDisposition.Loose ? PowerActionAvailabilityReason.LodeNotLoose :
                !safe ? PowerActionAvailabilityReason.ImmediateDanger :
                committed ? PowerActionAvailabilityReason.CommitmentActive :
                !looseIsInReach ? PowerActionAvailabilityReason.LodeOutOfReach :
                PowerActionAvailabilityReason.Available,
                0,
                HoldingOutcome.LodeCarried,
                [HoldingConstraint.CarryingLocksWeaponInvocationFlightAttunement]),
            Action(
                null,
                "drop",
                false,
                carrying && !committed,
                !carrying ? PowerActionAvailabilityReason.NotCarryingLode :
                committed ? PowerActionAvailabilityReason.CommitmentActive :
                PowerActionAvailabilityReason.Available,
                0,
                HoldingOutcome.LodeSetDown,
                [HoldingConstraint.NothingStopsOrLocks]),
            Action(
                PowerCommitmentKind.Build,
                "build",
                building,
                safe && !committed && siteInReach && (carrying || building),
                site is null ? PowerActionAvailabilityReason.HomeMissing :
                !safe ? PowerActionAvailabilityReason.ImmediateDanger :
                committed ? PowerActionAvailabilityReason.CommitmentActive :
                !siteInReach ? PowerActionAvailabilityReason.SiteOutOfReach :
                !carrying && !building ? PowerActionAvailabilityReason.LodeNotAtHome :
                PowerActionAvailabilityReason.Available,
                BuildTicks - (building ? power.Resonator!.Progress : 0),
                HoldingOutcome.ResonatorIntactOffersFourNext,
                [HoldingConstraint.HostileInterruptionKeepsProgress, HoldingConstraint.LocksAllOtherActionsWhileActive]),
            Action(
                PowerCommitmentKind.Dismantle,
                "dismantle",
                false,
                safe && !committed && dismantleSource is { } && IsWithinInteractionReach(state.Address, dismantleSource.Address),
                dismantleSource is null ? PowerActionAvailabilityReason.SourceUnavailable :
                !safe ? PowerActionAvailabilityReason.ImmediateDanger :
                committed ? PowerActionAvailabilityReason.CommitmentActive :
                !IsWithinInteractionReach(state.Address, dismantleSource.Address) ? PowerActionAvailabilityReason.SourceOutOfReach :
                PowerActionAvailabilityReason.Available,
                power.Resonator?.Phase == HearthResonatorPhase.Damaged ? 1 : DismantleTicks,
                HoldingOutcome.DamagedThenDestroyedNextFallsToInherent,
                [HoldingConstraint.HostileInterruptionKeepsProgress, HoldingConstraint.LocksAllOtherActionsWhileActive]),
            Action(
                PowerCommitmentKind.Rebuild,
                "rebuild",
                power.Resonator?.Phase == HearthResonatorPhase.Rebuilding,
                safe && !committed && rebuildSource is { } && IsWithinInteractionReach(state.Address, rebuildSource.Address),
                rebuildSource is null ? PowerActionAvailabilityReason.SourceUnavailable :
                !safe ? PowerActionAvailabilityReason.ImmediateDanger :
                committed ? PowerActionAvailabilityReason.CommitmentActive :
                !IsWithinInteractionReach(state.Address, rebuildSource.Address) ? PowerActionAvailabilityReason.SourceOutOfReach :
                PowerActionAvailabilityReason.Available,
                RebuildTicks - (power.Resonator?.Phase == HearthResonatorPhase.Rebuilding ? power.Resonator.Progress : 0),
                HoldingOutcome.ResonatorIntactRestoresFullNext,
                [HoldingConstraint.HostileInterruptionKeepsProgress, HoldingConstraint.LocksAllOtherActionsWhileActive]),
        });
    }

    private static PowerActionSnapshot Action(
        PowerCommitmentKind? kind,
        string id,
        bool isResumption,
        bool available,
        PowerActionAvailabilityReason availabilityReason,
        int heartbeats,
        HoldingOutcome outcome,
        IReadOnlyList<HoldingConstraint> constraints) => new(
            kind,
            id,
            isResumption,
            available,
            availabilityReason,
            heartbeats,
            outcome,
            Array.AsReadOnly(constraints.ToArray()));

    private static readonly HoldingObjectiveSnapshot UnavailableObjective = new(
        HoldingObjectiveKind.ReturnTheLode,
        HoldingSubject.None,
        TravelSubjectLocated: false,
        TravelSubjectInReach: false,
        TravelOffset: null,
        HoldingActionKind.None,
        ActionHeartbeats: 0,
        HoldingEstablishedFact.None,
        HoldingOutcome.None,
        ShowsCarryHomeStep: false,
        CommitmentCompletedTicks: 0,
        CommitmentTotalTicks: 0,
        WaitingForHeartbeat: false,
        NextTick: 0,
        Array.Empty<HoldingConstraint>());

    private static HoldingObjectiveSnapshot Objective(ChronicleState state)
    {
        var power = state.PowerHome!;
        if (power.Commitment is { } commitment)
        {
            return Objective(
                HoldingObjectiveKind.Commitment,
                [
                    HoldingConstraint.HostileInterruptionKeepsProgress,
                    HoldingConstraint.LocksAllOtherActionsWhileActive,
                ],
                action: HoldingActionKind.AdvanceHeartbeat,
                nextOutcome: CommitmentOutcome(commitment),
                commitmentCompletedTicks: commitment.CompletedTicks,
                commitmentTotalTicks: commitment.TotalTicks,
                waitingForHeartbeat: state.Speed == ChronicleSpeed.Paused,
                nextTick: state.Tick + 1);
        }

        if (!HasBurnPrimerKnowledge(state))
        {
            return Objective(
                HoldingObjectiveKind.LearnBurn,
                [HoldingConstraint.NothingStopsOrLocks],
                travelSubject: HoldingSubject.BurnPrimer,
                travelTarget: BurnPrimerAddress,
                state: state,
                action: HoldingActionKind.Read,
                nextOutcome: HoldingOutcome.BurnWordsEnterCodex);
        }

        if (IsCarrying(state))
        {
            var site = ResonatorSite(state);
            return Objective(
                HoldingObjectiveKind.CarryLodeHome,
                [
                    HoldingConstraint.HostileInterruptionKeepsWorkProgress,
                    HoldingConstraint.CarryingLocksWeaponInvocationFlightAttunement,
                ],
                travelSubject: site is null ? HoldingSubject.Home : HoldingSubject.ResonatorSite,
                travelTarget: site,
                state: state,
                action: HoldingActionKind.Build,
                actionHeartbeats: BuildTicks);
        }

        if (power.Lode.Disposition == ResonantLodeDisposition.Embedded)
        {
            return Objective(
                HoldingObjectiveKind.GetTheLode,
                [
                    HoldingConstraint.HostileInterruptionKeepsProgress,
                    HoldingConstraint.LocksAllOtherActionsWhileActive,
                ],
                travelSubject: HoldingSubject.SingingSeam,
                travelTarget: SingingSeamAddress,
                state: state,
                action: HoldingActionKind.Extract,
                actionHeartbeats: ExtractTicks);
        }

        if (power.Resonator is null && power.Lode.Disposition == ResonantLodeDisposition.Loose)
        {
            return Objective(
                HoldingObjectiveKind.LiftTheLode,
                [HoldingConstraint.CarryingLocksWeaponInvocationFlightAttunement],
                travelSubject: HoldingSubject.ResonantLode,
                travelTarget: power.Lode.Address,
                state: state,
                action: HoldingActionKind.Lift,
                showsCarryHomeStep: true);
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.UnderConstruction } construction)
        {
            return Objective(
                HoldingObjectiveKind.FinishConstruction,
                [
                    HoldingConstraint.HostileInterruptionKeepsProgress,
                    HoldingConstraint.LocksAllOtherActionsWhileActive,
                ],
                travelSubject: HoldingSubject.ResonatorSite,
                travelTarget: construction.Address,
                state: state,
                action: HoldingActionKind.ResumeBuild,
                actionHeartbeats: BuildTicks - construction.Progress);
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Intact })
        {
            var currentUsesSourceCapacity =
                state.Attunement?.Capacity == InherentLoadCapacity + SourceLoadContribution;
            return currentUsesSourceCapacity
                ? Objective(
                    HoldingObjectiveKind.TestSourceLoss,
                    [HoldingConstraint.LocksMovementFightInvocationAttunement],
                    action: HoldingActionKind.Dismantle,
                    actionHeartbeats: DismantleTicks,
                    establishedFact: HoldingEstablishedFact.CurrentLoadoutSurvivesSourceLoss,
                    nextOutcome: HoldingOutcome.DamagedThenDestroyedNextFallsToInherent)
                : Objective(
                    HoldingObjectiveKind.UseNewLoad,
                    [HoldingConstraint.BlockedByCarryingWorkOrDanger],
                    action: HoldingActionKind.Attune,
                    establishedFact: HoldingEstablishedFact.SourceContributesAtNextAttunement,
                    nextOutcome: HoldingOutcome.LoadoutChangesAtAttunement);
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Damaged })
        {
            return Objective(
                HoldingObjectiveKind.FinishDismantling,
                [HoldingConstraint.LocksMovementFightInvocationAttunement],
                action: HoldingActionKind.Destroy,
                actionHeartbeats: 1,
                establishedFact: HoldingEstablishedFact.SourceDamagedStillContributes,
                nextOutcome: HoldingOutcome.ResonatorDestroyedNextFallsToInherent);
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Destroyed })
        {
            return Objective(
                HoldingObjectiveKind.RebuildPower,
                [HoldingConstraint.LocksMovementFightInvocationAttunement],
                action: HoldingActionKind.Rebuild,
                actionHeartbeats: RebuildTicks,
                establishedFact: HoldingEstablishedFact.SourceDestroyedNextIsInherent,
                nextOutcome: HoldingOutcome.ResonatorIntactRestoresFullNext);
        }

        if (power.Resonator is { Phase: HearthResonatorPhase.Rebuilding } rebuilding)
        {
            return Objective(
                HoldingObjectiveKind.FinishRebuild,
                [HoldingConstraint.LocksMovementFightInvocationAttunement],
                travelSubject: HoldingSubject.DestroyedHearthResonator,
                travelTarget: rebuilding.Address,
                state: state,
                action: HoldingActionKind.ResumeRebuild,
                actionHeartbeats: RebuildTicks - rebuilding.Progress,
                nextOutcome: HoldingOutcome.ResonatorIntactRestoresFullNext);
        }

        return Objective(
            HoldingObjectiveKind.ReturnTheLode,
            [],
            travelSubject: HoldingSubject.ResonantLode,
            travelTarget: null,
            state: state,
            showsCarryHomeStep: true);
    }

    private static HoldingObjectiveSnapshot Objective(
        HoldingObjectiveKind kind,
        IReadOnlyList<HoldingConstraint> constraints,
        HoldingSubject travelSubject = HoldingSubject.None,
        WorldAddress? travelTarget = null,
        ChronicleState? state = null,
        HoldingActionKind action = HoldingActionKind.None,
        int actionHeartbeats = 0,
        HoldingEstablishedFact establishedFact = HoldingEstablishedFact.None,
        HoldingOutcome nextOutcome = HoldingOutcome.None,
        bool showsCarryHomeStep = false,
        int commitmentCompletedTicks = 0,
        int commitmentTotalTicks = 0,
        bool waitingForHeartbeat = false,
        long nextTick = 0) =>
        new(
            kind,
            travelSubject,
            travelTarget is not null,
            travelTarget is { } target && state is { } located && IsWithinInteractionReach(located.Address, target),
            travelTarget is { } destination && state is { } from
                ? Offset(from.Address, destination)
                : null,
            action,
            actionHeartbeats,
            establishedFact,
            nextOutcome,
            showsCarryHomeStep,
            commitmentCompletedTicks,
            commitmentTotalTicks,
            waitingForHeartbeat,
            nextTick,
            Array.AsReadOnly(constraints.ToArray()));

    private static HoldingOutcome CommitmentOutcome(PowerCommitmentState commitment) => commitment.Kind switch
    {
        PowerCommitmentKind.Extract when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            HoldingOutcome.ExtractionProgressRemains,
        PowerCommitmentKind.Extract => HoldingOutcome.LodeLooseAndSeamEmpty,
        PowerCommitmentKind.Build when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            HoldingOutcome.ConstructionAdvances,
        PowerCommitmentKind.Build => HoldingOutcome.ResonatorIntactOffersFourNext,
        PowerCommitmentKind.Dismantle when commitment.CompletedTicks == 0 =>
            HoldingOutcome.ResonatorDamagedStillContributes,
        PowerCommitmentKind.Dismantle => HoldingOutcome.ResonatorDestroyedNextFallsToInherent,
        PowerCommitmentKind.Rebuild when commitment.CompletedTicks + 1 < commitment.TotalTicks =>
            HoldingOutcome.RebuildAdvances,
        PowerCommitmentKind.Rebuild => HoldingOutcome.ResonatorIntactRestoresFullNext,
        _ => HoldingOutcome.WorkAdvances,
    };

    private static HoldingOffsetSnapshot Offset(WorldAddress from, WorldAddress to) =>
        string.Equals(from.Stratum, to.Stratum, StringComparison.Ordinal)
            ? new HoldingOffsetSnapshot(true, to.Stratum, to.X - from.X, to.Y - from.Y)
            : new HoldingOffsetSnapshot(false, to.Stratum, 0, 0);

    private static int TotalFor(HearthResonatorPhase phase) => phase switch
    {
        HearthResonatorPhase.UnderConstruction => BuildTicks,
        HearthResonatorPhase.Damaged or HearthResonatorPhase.Destroyed => DismantleTicks,
        HearthResonatorPhase.Rebuilding => RebuildTicks,
        HearthResonatorPhase.Intact => BuildTicks,
        _ => 0,
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

    internal static bool IsWithinInteractionReach(WorldAddress first, WorldAddress second) =>
        string.Equals(first.Stratum, second.Stratum, StringComparison.Ordinal) &&
        ((first.X == second.X && first.Y == second.Y) ||
         (first.X == second.X && AreConsecutive(first.Y, second.Y)) ||
         (first.Y == second.Y && AreConsecutive(first.X, second.X)));

    private static bool AreConsecutive(long first, long second) =>
        (first != long.MaxValue && first + 1 == second) ||
        (second != long.MaxValue && second + 1 == first);
}

/// <summary>
/// The production material commitment hooks. This is the one place where the
/// Holding rulebook is handed to the combat rulebook.
/// </summary>
internal sealed class HoldingCommitments : IMaterialCommitments
{
    internal static readonly HoldingCommitments Instance = new();

    private HoldingCommitments()
    {
    }

    public PowerAdvanceResult AdvanceAfterTick(ChronicleState state) =>
        HoldingRules.AdvanceAfterTick(state);

    public ChronicleState InterruptAfterHostileDamage(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results) =>
        HoldingRules.InterruptAfterHostileDamage(state, results);

    public ChronicleState EndIncarnation(ChronicleState state) =>
        HoldingRules.EndIncarnation(state);
}
