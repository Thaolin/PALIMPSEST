using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Chronicle.Core;

/// <summary>
/// Composes every player-facing sentence, key label, and checklist glyph for
/// the material Holding surface. Chronicle.Core states the facts; this file
/// owns the words and the keyboard.
/// </summary>
internal static class HoldingPresentation
{
    private const string ReadKey = "P";
    private const string WorkKey = "P";
    private const string HeartbeatKey = "SPACE";
    private const string AttuneKey = "G";

    internal static string Checklist(PowerComesHomeContextSnapshot power)
    {
        var objective = power.Objective;
        var lines = new List<string>(5) { Heading(power) };
        if (objective.Kind == HoldingObjectiveKind.Commitment)
        {
            lines.Add(objective.WaitingForHeartbeat
                ? $"[ ] {HeartbeatKey} — run 1 Heartbeat"
                : $"[ ] H{objective.NextTick} — work advances");
            var nextCompleted = Math.Min(
                objective.CommitmentCompletedTicks + 1,
                objective.CommitmentTotalTicks);
            lines.Add($"[ ] NEXT — {CommitmentOutcomeText(
                objective.NextOutcome,
                power.Attunement,
                nextCompleted,
                objective.CommitmentTotalTicks)}");
            lines.AddRange(objective.Constraints.Select(ConstraintText));
            return string.Join("\n", lines);
        }

        if (objective.Kind == HoldingObjectiveKind.ReturnTheLode)
        {
            return string.Join(
                "\n",
                Heading(power),
                $"[ ] Find the {SubjectLabel(HoldingSubject.ResonantLode)}",
                $"[ ] Carry it to {SubjectLabel(HoldingSubject.Home)}");
        }

        if (TravelStep(objective) is { } travel)
        {
            lines.Add(travel);
        }

        if (objective.EstablishedFact != HoldingEstablishedFact.None)
        {
            lines.Add($"[x] {FactText(objective.EstablishedFact, power.Attunement)}");
        }

        if (ActionStep(objective, power.Attunement) is { } action)
        {
            lines.Add(action);
        }

        if (objective.ShowsCarryHomeStep)
        {
            lines.Add($"[ ] Carry it to {SubjectLabel(HoldingSubject.Home)}");
        }

        if (objective.NextOutcome != HoldingOutcome.None)
        {
            lines.Add($"[ ] NEXT — {ObjectiveOutcomeText(objective.NextOutcome, power.Attunement)}");
        }

        lines.AddRange(objective.Constraints.Select(ConstraintText));
        return string.Join("\n", lines);
    }

    internal static string ActionLabel(PowerActionSnapshot action) => action.Id switch
    {
        "read-primer" => "READ BURN PRIMER",
        "extract" => "EXTRACT LODE",
        "lift" => "LIFT LODE",
        "drop" => "SET DOWN",
        "build" => action.IsResumption ? "RESUME BUILD" : "BUILD",
        "dismantle" => "DISMANTLE",
        "rebuild" => action.IsResumption ? "RESUME REBUILD" : "REBUILD",
        _ => "POWER",
    };

    internal static string ActionAvailability(PowerActionSnapshot action) =>
        action.AvailabilityReason switch
        {
            PowerActionAvailabilityReason.Available => "Available.",
            PowerActionAvailabilityReason.AlreadyRead => "The Burn Primer has already been read.",
            PowerActionAvailabilityReason.ImmediateDanger => "Leave immediate danger first.",
            PowerActionAvailabilityReason.CommitmentActive => "Finish or cancel the current work.",
            PowerActionAvailabilityReason.CarryingLode => "Set down the Resonant Lode first.",
            PowerActionAvailabilityReason.PrimerOutOfReach => "Stand on or beside the Burn Primer.",
            PowerActionAvailabilityReason.AlreadyExtracted => "The Lode has already been extracted.",
            PowerActionAvailabilityReason.SeamOutOfReach => "Stand on or beside the Singing Seam.",
            PowerActionAvailabilityReason.LodeNotLoose => "The Resonant Lode is not loose.",
            PowerActionAvailabilityReason.LodeOutOfReach => "Stand on or beside the loose Resonant Lode.",
            PowerActionAvailabilityReason.NotCarryingLode => "This body is not carrying the Resonant Lode.",
            PowerActionAvailabilityReason.HomeMissing => "Establish Home first.",
            PowerActionAvailabilityReason.SiteOutOfReach => "Stand on or beside the outlined site at Home.",
            PowerActionAvailabilityReason.LodeNotAtHome => "Carry the Resonant Lode to Home.",
            PowerActionAvailabilityReason.SourceUnavailable => "The required Hearth Resonator state is absent.",
            PowerActionAvailabilityReason.SourceOutOfReach => "Stand on or beside the Hearth Resonator.",
            _ => "Unavailable.",
        };

    internal static IReadOnlyList<string> CommitmentForecast(
        PowerComesHomeContextSnapshot power)
    {
        if (power.Commitment is not { } commitment)
        {
            return [];
        }

        var nextCompleted = Math.Min(commitment.CompletedTicks + 1, commitment.TotalTicks);
        var remainingAfter = commitment.TotalTicks - nextCompleted;
        return
        [
            $"H{commitment.NextTick} · {CommitmentOutcomeText(
                commitment.NextOutcome,
                power.Attunement,
                nextCompleted,
                commitment.TotalTicks)}",
            remainingAfter == 0
                ? $"NEXT {nextCompleted}/{commitment.TotalTicks} · complete"
                : $"NEXT {nextCompleted}/{commitment.TotalTicks} · {remainingAfter} remaining",
        ];
    }

    internal static string MaterialHeading(PowerComesHomeContextSnapshot power)
    {
        if (!power.BurnPrimer.IsRead)
        {
            return "BURN PRIMER · UNREAD";
        }

        if (power.Resonator is { } source)
        {
            return $"RESONATOR · {SourcePhase(source.Phase).ToUpperInvariant()}";
        }

        return $"RESONANT LODE · {power.Lode.Disposition.ToString().ToUpperInvariant()}";
    }

    internal static string MaterialFacts(PowerComesHomeContextSnapshot power)
    {
        if (!power.BurnPrimer.IsRead)
        {
            return $"Primer {power.BurnPrimer.Address} · unread\n" +
                   "Read once; Burn, Quickly, and Lasting persist in the Codex.";
        }

        var currentAddress = power.Lode.Address is { } address
            ? address.ToString()
            : power.Lode.CarrierIncarnationId is { } carrier
                ? $"carried by body {carrier}"
                : "installed at Home";
        var source = power.Resonator is null
            ? "No Load Source yet"
            : $"Source {SourcePhase(power.Resonator.Phase)}";
        return $"Origin {power.Lode.OriginAddress} · now {currentAddress}\n" +
               $"{source} · CURRENT {power.Attunement.CurrentUsedLoad} · " +
               $"NEXT {power.Attunement.NextAttunementCapacity}";
    }

    internal static string MaterialDecision(PowerComesHomeContextSnapshot power)
    {
        var objective = power.Objective;
        var next = objective.Kind == HoldingObjectiveKind.Commitment
            ? CommitmentOutcomeText(
                objective.NextOutcome,
                power.Attunement,
                Math.Min(objective.CommitmentCompletedTicks + 1, objective.CommitmentTotalTicks),
                objective.CommitmentTotalTicks)
            : ActionName(objective.Action);
        var when = objective.Kind == HoldingObjectiveKind.Commitment
            ? objective.WaitingForHeartbeat
                ? $"Next active Heartbeat (H{objective.NextTick})"
                : $"Heartbeat H{objective.NextTick}"
            : objective.ActionHeartbeats <= 0
                ? "Immediately"
                : $"After {objective.ActionHeartbeats} active " +
                  (objective.ActionHeartbeats == 1 ? "Heartbeat" : "Heartbeats");
        var interrupt = objective.Constraints.Any(constraint => constraint is
                HoldingConstraint.HostileInterruptionKeepsProgress or
                HoldingConstraint.HostileInterruptionKeepsWorkProgress or
                HoldingConstraint.LocksMovementFightInvocationAttunement)
            ? "Cancel, damage, or death; progress stays"
            : "No interruption window";
        var prevents = objective.Constraints.Any(constraint => constraint == HoldingConstraint.LocksAllOtherActionsWhileActive)
            ? "Movement, combat, Invocation, Attunement, carrying, and other work"
            : objective.Constraints.Any(constraint => constraint == HoldingConstraint.CarryingLocksWeaponInvocationFlightAttunement)
                ? "Cleaver, Burn, Fly, and Attunement while carrying"
                : objective.Constraints.Any(constraint => constraint == HoldingConstraint.BlockedByCarryingWorkOrDanger)
                    ? "Nothing after success"
                    : objective.Constraints.Any(constraint => constraint == HoldingConstraint.LocksMovementFightInvocationAttunement)
                        ? "Movement, combat, Invocation, and Attunement while working"
                        : "Nothing else";
        return $"STATE  NEXT: {next}\n" +
               $"WHEN   {when}\n" +
               $"INTERRUPTS   {interrupt}\n" +
               $"PREVENTS   {prevents}";
    }

    internal static IReadOnlyList<string> MaterialForecast(PowerComesHomeContextSnapshot power)
    {
        var objective = power.Objective;
        if (objective.Kind == HoldingObjectiveKind.Commitment)
        {
            return CommitmentForecast(power);
        }

        var outcome = objective.NextOutcome == HoldingOutcome.None
            ? ActionName(objective.Action)
            : ObjectiveOutcomeText(objective.NextOutcome, power.Attunement);
        return objective.ActionHeartbeats <= 0
            ? [$"NOW · {outcome}"]
            : [$"+{objective.ActionHeartbeats} active H · {outcome}"];
    }

    private static string ActionName(HoldingActionKind action) => action switch
    {
        HoldingActionKind.Read => "Read the Burn Primer",
        HoldingActionKind.Extract => "Extract the Resonant Lode",
        HoldingActionKind.Lift => "Lift the Resonant Lode",
        HoldingActionKind.Build => "Build the Hearth Resonator",
        HoldingActionKind.ResumeBuild => "Resume the Hearth Resonator",
        HoldingActionKind.Dismantle => "Dismantle the Hearth Resonator",
        HoldingActionKind.Destroy => "Finish dismantling",
        HoldingActionKind.Rebuild => "Rebuild the Hearth Resonator",
        HoldingActionKind.ResumeRebuild => "Resume rebuilding",
        HoldingActionKind.Attune => "Attune Burn + Quickly + Lasting",
        HoldingActionKind.AdvanceHeartbeat => "Advance the work",
        _ => "Follow the checklist",
    };

    private static string SourcePhase(HearthResonatorPhase phase) => phase switch
    {
        HearthResonatorPhase.UnderConstruction => "building",
        HearthResonatorPhase.Intact => "intact",
        HearthResonatorPhase.Damaged => "damaged",
        HearthResonatorPhase.Destroyed => "destroyed",
        HearthResonatorPhase.Rebuilding => "rebuilding",
        _ => "unknown",
    };

    private static string Heading(PowerComesHomeContextSnapshot power)
    {
        var objective = power.Objective;
        if (objective.Kind != HoldingObjectiveKind.Commitment)
        {
            return $"CHECKLIST · {ObjectiveName(objective.Kind)}";
        }

        var commitment = power.Commitment;
        var name = commitment is null
            ? "WORK"
            : CommitmentName(commitment.Kind);
        return $"CHECKLIST · {name} " +
               $"{objective.CommitmentCompletedTicks}/{objective.CommitmentTotalTicks}";
    }

    private static string ObjectiveName(HoldingObjectiveKind kind) => kind switch
    {
        HoldingObjectiveKind.LearnBurn => "LEARN BURN",
        HoldingObjectiveKind.GetTheLode => "GET THE GOLD LODE",
        HoldingObjectiveKind.LiftTheLode => "LIFT GOLD LODE",
        HoldingObjectiveKind.CarryLodeHome => "CARRY LODE HOME",
        HoldingObjectiveKind.FinishConstruction => "FINISH RESONATOR",
        HoldingObjectiveKind.UseNewLoad => "USE NEW LOAD",
        HoldingObjectiveKind.TestSourceLoss => "TEST SOURCE LOSS",
        HoldingObjectiveKind.FinishDismantling => "FINISH DISMANTLING",
        HoldingObjectiveKind.RebuildPower => "REBUILD POWER",
        HoldingObjectiveKind.FinishRebuild => "FINISH REBUILD",
        _ => "RETURN THE LODE",
    };

    private static string CommitmentName(PowerCommitmentKind kind) => kind switch
    {
        PowerCommitmentKind.Extract => "EXTRACT LODE",
        PowerCommitmentKind.Build => "BUILD RESONATOR",
        PowerCommitmentKind.Dismantle => "DISMANTLE RESONATOR",
        PowerCommitmentKind.Rebuild => "REBUILD RESONATOR",
        _ => "WORK",
    };

    private static string SubjectLabel(HoldingSubject subject) => subject switch
    {
        HoldingSubject.BurnPrimer => "BURN PRIMER",
        HoldingSubject.SingingSeam => "GOLD SEAM",
        HoldingSubject.ResonantLode => "GOLD LODE",
        HoldingSubject.Home => "HOME",
        HoldingSubject.ResonatorSite => "OUTLINED SITE",
        HoldingSubject.HearthResonator => "RESONATOR",
        HoldingSubject.DestroyedHearthResonator => "DESTROYED RESONATOR",
        _ => "SUBJECT",
    };

    private static string? TravelStep(HoldingObjectiveSnapshot objective)
    {
        if (objective.TravelSubject == HoldingSubject.None)
        {
            return null;
        }

        if (!objective.TravelSubjectLocated)
        {
            return objective.TravelSubject == HoldingSubject.Home
                ? $"[ ] Find {SubjectLabel(HoldingSubject.Home)}"
                : $"[ ] Find the {SubjectLabel(objective.TravelSubject)}";
        }

        if (objective.TravelSubjectInReach)
        {
            return $"[x] Within reach of {SubjectLabel(objective.TravelSubject)}";
        }

        // Carrying the Lode home names the destination, not the foundation.
        var destination = objective.Kind == HoldingObjectiveKind.CarryLodeHome
            ? HoldingSubject.Home
            : objective.TravelSubject;
        return $"[ ] Go toward {SubjectLabel(destination)}: {OffsetText(objective.TravelOffset)}";
    }

    private static string? ActionStep(
        HoldingObjectiveSnapshot objective,
        AttunementCapacitySnapshot capacity)
    {
        var timing = objective.ActionHeartbeats <= 0
            ? "instant"
            : objective.ActionHeartbeats == 1
                ? "1 Heartbeat"
                : $"{objective.ActionHeartbeats} Heartbeats";
        return objective.Action switch
        {
            HoldingActionKind.Read => $"[ ] {ReadKey} — Read ({timing})",
            HoldingActionKind.Extract => $"[ ] {WorkKey} — Extract ({timing})",
            HoldingActionKind.Lift => $"[ ] {WorkKey} — Lift ({timing})",
            HoldingActionKind.Build => $"[ ] {WorkKey} — Build ({timing})",
            HoldingActionKind.ResumeBuild => $"[ ] {WorkKey} — Resume Build ({timing})",
            HoldingActionKind.Dismantle => $"[ ] {WorkKey} — Dismantle ({timing})",
            HoldingActionKind.Destroy => $"[ ] {WorkKey} — Destroy ({timing})",
            HoldingActionKind.Rebuild => $"[ ] {WorkKey} — Rebuild ({timing})",
            HoldingActionKind.ResumeRebuild => $"[ ] {WorkKey} — Resume Rebuild ({timing})",
            HoldingActionKind.Attune =>
                $"[ ] {AttuneKey} — Attune Burn + Quickly + Lasting " +
                $"({capacity.DesiredExpressionLoad}/{capacity.NextAttunementCapacity})",
            _ => null,
        };
    }

    private static string FactText(
        HoldingEstablishedFact fact,
        AttunementCapacitySnapshot capacity) => fact switch
    {
        HoldingEstablishedFact.CurrentLoadoutSurvivesSourceLoss =>
            $"CURRENT Loadout uses {capacity.CurrentUsedLoad}; source loss will not disable it",
        HoldingEstablishedFact.SourceContributesAtNextAttunement =>
            $"Resonator gives +{capacity.SourceContribution} at NEXT Attunement",
        HoldingEstablishedFact.SourceDamagedStillContributes =>
            $"Resonator damaged; still +{capacity.SourceContribution} at NEXT Attunement",
        HoldingEstablishedFact.SourceDestroyedNextIsInherent =>
            $"Source destroyed: NEXT Attunement {capacity.NextAttunementCapacity}; CURRENT stays",
        _ => string.Empty,
    };

    private static string ObjectiveOutcomeText(
        HoldingOutcome outcome,
        AttunementCapacitySnapshot capacity) => outcome switch
    {
        HoldingOutcome.BurnWordsEnterCodex => "Burn + Quickly + Lasting enter Codex",
        HoldingOutcome.LoadoutChangesAtAttunement => "Loadout changes at Attunement",
        HoldingOutcome.DamagedThenDestroyedNextFallsToInherent =>
            $"damaged, then destroyed; NEXT Attunement falls to {capacity.InherentCapacity}",
        HoldingOutcome.ResonatorDestroyedNextFallsToInherent =>
            $"NEXT Attunement falls to {capacity.InherentCapacity}; CURRENT stays",
        HoldingOutcome.ResonatorIntactRestoresFullNext =>
            $"intact source restores NEXT Attunement to " +
            $"{capacity.InherentCapacity + capacity.MaximumSourceContribution}",
        _ => "the Chronicle changes",
    };

    private static string CommitmentOutcomeText(
        HoldingOutcome outcome,
        AttunementCapacitySnapshot capacity,
        int nextCompleted,
        int total) => outcome switch
    {
        HoldingOutcome.ExtractionProgressRemains =>
            $"Extraction {nextCompleted}/{total}; progress stays",
        HoldingOutcome.LodeLooseAndSeamEmpty => "Lode becomes loose; Seam becomes empty",
        HoldingOutcome.ConstructionAdvances => $"Construction {nextCompleted}/{total}",
        HoldingOutcome.ResonatorIntactOffersFourNext =>
            $"Resonator intact; +{capacity.SourceContribution} ready for NEXT Attunement",
        HoldingOutcome.ResonatorDamagedStillContributes =>
            $"Resonator damaged; still +{capacity.SourceContribution} at NEXT Attunement",
        HoldingOutcome.ResonatorDestroyedNextFallsToInherent =>
            $"Resonator destroyed; NEXT Attunement falls to {capacity.InherentCapacity}",
        HoldingOutcome.RebuildAdvances => $"Rebuild {nextCompleted}/{total}",
        HoldingOutcome.ResonatorIntactRestoresFullNext =>
            $"Resonator intact; NEXT Attunement returns to " +
            $"{capacity.InherentCapacity + capacity.MaximumSourceContribution}",
        _ => "Work advances",
    };

    private static string ConstraintText(HoldingConstraint constraint) => constraint switch
    {
        HoldingConstraint.NothingStopsOrLocks => "No time passes; nothing else is blocked.",
        HoldingConstraint.HostileInterruptionKeepsProgress =>
            "Interrupted by cancel, damage, or death. Progress stays.",
        HoldingConstraint.HostileInterruptionKeepsWorkProgress =>
            "Interrupted by cancel, damage, or death. Progress stays.",
        HoldingConstraint.LocksAllOtherActionsWhileActive =>
            "While working, other actions are unavailable.",
        HoldingConstraint.CarryingLocksWeaponInvocationFlightAttunement =>
            "Carrying disables Cleaver, Burn, Fly, and Attunement.",
        HoldingConstraint.BlockedByCarryingWorkOrDanger =>
            "Requires free hands, no work, and no danger.",
        HoldingConstraint.LocksMovementFightInvocationAttunement =>
            "Interrupted by cancel, damage, or death. Other actions unavailable.",
        _ => string.Empty,
    };

    private static string OffsetText(HoldingOffsetSnapshot? offset)
    {
        if (offset is not { } value)
        {
            return "HERE";
        }

        if (!value.SameStratum)
        {
            return $"in {value.Stratum}";
        }

        var parts = new List<string>(2);
        var deltaX = (BigInteger)value.DeltaX;
        var deltaY = (BigInteger)value.DeltaY;
        if (!deltaX.IsZero)
        {
            var distance = BigInteger.Abs(deltaX);
            parts.Add(
                $"{distance} {(distance == BigInteger.One ? "TILE" : "TILES")} " +
                $"{(deltaX.Sign > 0 ? "EAST" : "WEST")}");
        }

        if (!deltaY.IsZero)
        {
            var distance = BigInteger.Abs(deltaY);
            parts.Add(
                $"{distance} {(distance == BigInteger.One ? "TILE" : "TILES")} " +
                $"{(deltaY.Sign > 0 ? "SOUTH" : "NORTH")}");
        }

        return parts.Count == 0 ? "HERE" : string.Join(" and ", parts);
    }
}
