using System.Text.Json;

namespace Chronicle.Core;

public static partial class ChronicleSaveCodec
{
    private static void ValidateCurrentState(ChronicleState state)
    {
        if (state.Tick < 0 || state.Tick == long.MaxValue)
        {
            throw new InvalidOperationException(
                "A Chronicle tick must be non-negative and leave room for another fixed tick.");
        }

        if (!Enum.IsDefined(state.Speed))
        {
            throw new InvalidOperationException($"Unknown Chronicle speed '{state.Speed}'.");
        }

        if (!Enum.IsDefined(state.Intent))
        {
            throw new InvalidOperationException($"Unknown opening Intent '{state.Intent}'.");
        }

        if (state.WorldGrammarVersion is not (0 or 1 or 2 or 3 or 4 or 5 or 6))
        {
            throw new InvalidOperationException(
                $"Unsupported World Grammar version '{state.WorldGrammarVersion}'.");
        }

        if (!Enum.IsDefined(state.IncarnationLife))
        {
            throw new InvalidOperationException(
                $"Unknown Incarnation life state '{state.IncarnationLife}'.");
        }

        if (state.IncarnationId <= 0 || state.IncarnationId == long.MaxValue)
        {
            throw new InvalidOperationException(
                "A current Incarnation identity must be positive and leave room for replacement.");
        }

        ValidateOpeningIntentProvenance(state);
        ValidateCurrentAddress(state.Address, "Incarnation");
        var looseStoneAddress = state.LooseStoneAddress
            ?? throw new InvalidOperationException("Current saves require the loose-Stone Address.");
        ValidateCurrentAddress(looseStoneAddress, "Loose Stone");
        if (looseStoneAddress.X != ChronicleState.InitialLooseStoneAddress.X ||
            looseStoneAddress.Y != ChronicleState.InitialLooseStoneAddress.Y)
        {
            throw new InvalidOperationException(
                "The loose Stone must retain its fixed X/Y provenance across Strata.");
        }
        var bellAddress = state.BellAddress
            ?? throw new InvalidOperationException("Current saves require the Bell Address.");
        ValidateCurrentAddress(bellAddress, "Bell");
        if (bellAddress.X != SkyStratum.LandmarkAddress.X ||
            bellAddress.Y != SkyStratum.LandmarkAddress.Y)
        {
            throw new InvalidOperationException(
                "The Bell must retain its fixed X/Y provenance across Strata.");
        }
        if (bellAddress == looseStoneAddress || bellAddress == state.Home?.Address)
        {
            throw new InvalidOperationException(
                "The Bell cannot overlap the loose Stone or Home.");
        }
        if (state.Loadout is null)
        {
            throw new InvalidOperationException("Current saves require all eight Loadout slots.");
        }

        state.Loadout.Value.Validate(state.Codex);
        ValidateCurrentStudy(state);
        ValidateHome(state);
        ValidateGeneratedCairnNonOverlap(state);
        ValidateFirstConflict(state);
        ValidateCombat(state);
        ValidatePowerHome(state);
        ValidateAgents(state);
    }

    private static void ValidateOpeningIntentProvenance(ChronicleState state)
    {
        if (state.Intent == OpeningIntent.Up && !state.Codex.Contains(WordIds.Fly))
        {
            throw new InvalidOperationException("UP Chronicles must retain Fly in the Codex.");
        }

        if (state.Intent == OpeningIntent.Here && !state.Codex.Contains(WordIds.Found))
        {
            throw new InvalidOperationException("HERE Chronicles must retain Found in the Codex.");
        }

        if (state.Intent == OpeningIntent.Against)
        {
            if (state.WorldGrammarVersion is not (3 or 4 or 5 or 6))
            {
                throw new InvalidOperationException("AGAINST is only available in World Grammar version 3 through 6.");
            }

            var firstVerb = state.WorldGrammarVersion is 4 or 5 or 6 ? WordIds.Burn : WordIds.Smash;
            if (!state.Codex.Contains(firstVerb))
            {
                throw new InvalidOperationException(
                    $"AGAINST Chronicles must retain {WordCatalogue.Get(firstVerb).DisplayName} in the Codex.");
            }
        }
    }

    private static void ValidateCurrentStudy(ChronicleState state)
    {
        foreach (var word in WordCatalogue.Words.Where(word => word.UnderstandingRequired > 0))
        {
            var understanding = state.Study.UnderstandingFor(word.Id);
            if (state.Codex.Contains(word.Id) && understanding != word.UnderstandingRequired)
            {
                throw new InvalidOperationException(
                    $"Learned {word.DisplayName} must retain complete Understanding.");
            }

            if (!state.Codex.Contains(word.Id) && understanding == word.UnderstandingRequired)
            {
                throw new InvalidOperationException(
                    $"Complete {word.DisplayName} Understanding must be retained in the Codex.");
            }
        }

        if (state.Study.ActiveSourceId is not { } activeSourceId ||
            state.Study.ActiveWord is not { } activeWord)
        {
            return;
        }

        if (!state.HasLivingIncarnation)
        {
            throw new InvalidOperationException("Awaiting-replacement Chronicles cannot retain an active Study pursuit.");
        }

        var source = StudySourceGrammar.At(state, state.Address);
        if (source is null ||
            source.Id != activeSourceId ||
            source.Offers.All(offer => offer.Word.Id != activeWord) ||
            state.Codex.Contains(activeWord))
        {
            throw new InvalidOperationException("Current Study pursuit does not match the Chronicle state.");
        }
    }

    private static void ValidateHome(ChronicleState state)
    {
        if (state.Home is not { } home)
        {
            return;
        }

        if (!state.Codex.Contains(WordIds.Found))
        {
            throw new InvalidOperationException("Home requires Found in the Codex.");
        }

        if (!string.Equals(home.HoldingId, "holding.home", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Home must use the stable holding.home identity.");
        }

        if (!string.Equals(home.DisplayName, "The First Hearth", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Home must retain the display name The First Hearth.");
        }

        if (home.Material != HomeMaterialState.HearthstoneRaised)
        {
            throw new InvalidOperationException("Home must retain its HearthstoneRaised material state.");
        }

        if (home.FoundedTick < 0 || home.FoundedTick > state.Tick)
        {
            throw new InvalidOperationException("Home founding tick must be within Chronicle time.");
        }

        if (home.FoundingIncarnationId <= 0 ||
            home.FoundingIncarnationId > state.IncarnationId)
        {
            throw new InvalidOperationException(
                "Home founding Incarnation must be positive and cannot be newer than the current Incarnation.");
        }

        if (!string.Equals(
                home.Address.Stratum,
                SurfacePatch.SurfaceStratum,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Home must occupy the surface Stratum.");
        }

        var cell = WorldArea.Generate(
            state with { Home = null },
            home.Address.Stratum,
            new WorldRectangle(home.Address.X, home.Address.Y, 1, 1)).Cells[0];
        if (cell.Feature != WorldFeature.Stone ||
            cell.Ground == WorldGround.Water ||
            cell.DurableIdentity is not null)
        {
            throw new InvalidOperationException(
                "Home must occupy an unmarked, supported Stone on non-water surface ground.");
        }
    }

    private static void ValidateGeneratedCairnNonOverlap(ChronicleState state)
    {
        if (state.WorldGrammarVersion != 3)
        {
            return;
        }

        var cairnAddress = WorldArea.GeneratedCairnAddress(state.Seed);
        if (state.LooseStoneAddress == cairnAddress)
        {
            throw new InvalidOperationException("The loose Stone cannot overlap the generated Riven Cairn.");
        }

        if (state.Home?.Address == cairnAddress)
        {
            throw new InvalidOperationException("Home cannot overlap the generated Riven Cairn.");
        }

        if (state.CurrentBellAddress == cairnAddress)
        {
            throw new InvalidOperationException("The Bell cannot overlap the generated Riven Cairn.");
        }
    }

    private static void ValidateFirstConflict(ChronicleState state)
    {
        if (state.FirstConflict is not { } conflict)
        {
            if (state.WorldGrammarVersion == 3 &&
                state.HasLivingIncarnation &&
                state.Address == WorldArea.GeneratedCairnAddress(state.Seed))
            {
                throw new InvalidOperationException(
                    "A living Incarnation at the generated Riven Cairn must retain its conflict state.");
            }

            return;
        }

        if (state.WorldGrammarVersion != 3)
        {
            throw new InvalidOperationException("First Conflict state requires World Grammar version 3.");
        }

        if (!string.Equals(
                conflict.SubjectId,
                FirstConflictSubjects.RiverWardSubjectId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("First Conflict must retain the stable River-Ward subject identity.");
        }

        var cairnAddress = WorldArea.GeneratedCairnAddress(state.Seed);
        if (conflict.Address != cairnAddress)
        {
            throw new InvalidOperationException(
                "First Conflict must retain the generated Riven Cairn Address.");
        }

        if (conflict.Address == state.LooseStoneAddress ||
            conflict.Address == state.Home?.Address ||
            conflict.Address == state.CurrentBellAddress)
        {
            throw new InvalidOperationException(
                "First Conflict cannot overlap the loose Stone, Home, or Bell.");
        }

        if (conflict.ThreatenedTick < 0 || conflict.ThreatenedTick > state.Tick)
        {
            throw new InvalidOperationException("First Conflict threat tick must be within Chronicle time.");
        }

        if (conflict.PendingAction is { } pendingAction &&
            pendingAction != new LoadoutSlot(WordIds.Smash))
        {
            throw new InvalidOperationException(
                "First Conflict may retain only the exact intrinsic Smash Loadout action.");
        }

        if (conflict.PendingAction is not null && !state.Codex.Contains(WordIds.Smash))
        {
            throw new InvalidOperationException(
                "A pending First Conflict Smash action requires Smash in the Codex.");
        }

        if (conflict.Outcome is null)
        {
            if (!state.HasLivingIncarnation)
            {
                throw new InvalidOperationException(
                    "Awaiting-replacement Chronicles cannot retain an unresolved First Conflict.");
            }

            if (conflict.ResolvedTick is not null ||
                conflict.ResolvingIncarnationId is not null)
            {
                throw new InvalidOperationException(
                    "An unresolved First Conflict cannot retain resolution provenance.");
            }

            if (state.Address != conflict.Address)
            {
                throw new InvalidOperationException(
                    "An unresolved First Conflict must retain the living Incarnation at the Riven Cairn.");
            }

            if (conflict.ThreatenedTick != state.Tick)
            {
                throw new InvalidOperationException(
                    "An unresolved First Conflict must remain on its exact threatened tick until resolution.");
            }

            return;
        }

        if (conflict.Outcome != FirstConflictOutcome.Shattered)
        {
            throw new InvalidOperationException("First Conflict has an unknown outcome.");
        }

        if (!state.Codex.Contains(WordIds.Smash))
        {
            throw new InvalidOperationException(
                "A Shattered First Conflict requires Smash in the durable Codex.");
        }

        if (conflict.PendingAction is not null)
        {
            throw new InvalidOperationException(
                "A resolved First Conflict cannot retain a pending action.");
        }

        if (conflict.ResolvedTick is not { } resolvedTick ||
            conflict.ThreatenedTick == long.MaxValue ||
            resolvedTick != conflict.ThreatenedTick + 1 ||
            resolvedTick > state.Tick)
        {
            throw new InvalidOperationException(
                "First Conflict resolution tick must follow the threat within Chronicle time.");
        }

        if (conflict.ResolvingIncarnationId is not { } resolvingIncarnationId ||
            resolvingIncarnationId <= 0 ||
            resolvingIncarnationId > state.IncarnationId)
        {
            throw new InvalidOperationException(
                "First Conflict must retain a valid resolving Incarnation identity.");
        }
    }

    private static void ValidateCombat(ChronicleState state)
    {
        if (state.WorldGrammarVersion is not (4 or 5 or 6))
        {
            if (state.Combat is not null)
            {
                throw new InvalidOperationException(
                    "Goal 6A combat state requires World Grammar version 4, 5, or 6.");
            }

            return;
        }

        if (state.Combat is not { } combat)
        {
            throw new InvalidOperationException(
                "World Grammar version 4, 5, or 6 requires its authored Goal 6A combat state.");
        }

        if (state.FirstConflict is not null)
        {
            throw new InvalidOperationException(
                "World Grammar version 4 or 5 does not retain the predecessor First Conflict.");
        }

        if (combat.Equipment != EquipmentState.Fixed)
        {
            throw new InvalidOperationException(
                "Goal 6A requires the authored Iron Cleaver, Quilted Jack, and Copper Ward equipment.");
        }

        if (combat.IncarnationHitPoints < 0 ||
            combat.IncarnationHitPoints > combat.MaximumHitPoints)
        {
            throw new InvalidOperationException("Goal 6A Incarnation HP is outside its authored bounds.");
        }

        if (combat.WeaponTicksUntilReady is < 0 or >= CombatState.IronCleaverCadence)
        {
            throw new InvalidOperationException("Iron Cleaver cadence must remain within its authored bounds.");
        }

        if (combat.RecoveryRemaining < 0 || combat.RecoveryRemaining > CombatState.BurnRecovery)
        {
            throw new InvalidOperationException("Burn Recovery is outside its authored bounds.");
        }

        var brute = combat.MireBrute;
        if (!string.Equals(
                brute.Identity,
                WorldArea.GeneratedMireBruteIdentity(state.Seed),
                StringComparison.Ordinal) ||
            brute.OriginAddress != WorldArea.GeneratedMireBruteAddress(state.Seed))
        {
            throw new InvalidOperationException(
                "Goal 6A must retain the generated Mire Brute's stable identity and origin.");
        }

        ValidateCurrentAddress(brute.Address, "Mire Brute");
        if (brute.HitPoints is < 0 or > CombatState.MireBruteMaximumHitPoints ||
            brute.SwingTicksRemaining is < 1 or > CombatState.MireBruteSwingCadence)
        {
            throw new InvalidOperationException("Mire Brute state is outside its authored bounds.");
        }

        if (brute.IsLiving == (brute.DefeatedTick is not null) ||
            brute.DefeatedTick is { } defeatedTick && (defeatedTick < 0 || defeatedTick > state.Tick))
        {
            throw new InvalidOperationException("Mire Brute outcome provenance is inconsistent.");
        }

        if (combat.PendingAction is not null && combat.Preparation is not null)
        {
            throw new InvalidOperationException(
                "Only one Goal 6A tactical action may be pending at a time.");
        }

        ValidatePendingAction(state, combat.PendingAction);
        ValidatePreparation(state, combat.Preparation);
        ValidateBurn(state, combat.OngoingBurn);

        if (combat.Scorch is { } scorch)
        {
            ValidateCurrentAddress(scorch.Address, "scorched ground");
            if (scorch.CreatedTick < 0 || scorch.CreatedTick > state.Tick)
            {
                throw new InvalidOperationException("Scorched ground creation must remain within Chronicle time.");
            }
        }

        ValidateSuccessorLoadout(state);
        if (!state.HasLivingIncarnation)
        {
            if (combat.IncarnationHitPoints != 0 ||
                combat.PendingAction is not null ||
                combat.Preparation is not null ||
                combat.RecoveryRemaining != 0 ||
                combat.WeaponStanceActive ||
                combat.EngagementActive)
            {
                throw new InvalidOperationException(
                    "A dead Goal 6A Incarnation cannot retain body-bound combat state.");
            }
        }

        if (CombatRules.IsImmediateDanger(state) &&
            state.Speed is ChronicleSpeed.Normal or ChronicleSpeed.Fast)
        {
            throw new InvalidOperationException(
                "Immediate Goal 6A danger may run only at Slow speed or while paused.");
        }
    }

    private static void ValidateSuccessorLoadout(ChronicleState state)
    {
        var occupied = state.ActiveLoadout.Slots
            .Select((slot, index) => (slot, index))
            .Where(pair => !pair.slot.IsEmpty)
            .ToArray();
        if (occupied.Length > CombatState.ActiveVerbSlots)
        {
            throw new InvalidOperationException("Goal 6A supports exactly one active Verb slot.");
        }

        foreach (var (slot, _) in occupied)
        {
            if (slot.Noun is not null || slot.Verb is not { } verbId)
            {
                throw new InvalidOperationException(
                    "Goal 6A Loadouts use Verbs and Modifiers, never fitted Nouns.");
            }

            if (!WordCatalogue.TryGet(verbId, out var verb) || verb.Kind != WordKind.Verb ||
                !state.Codex.Contains(verbId))
            {
                throw new InvalidOperationException("Goal 6A Loadout Verb is not an attuned Codex Verb.");
            }

            foreach (var modifierId in slot.Modifiers)
            {
                if (!WordCatalogue.TryGet(modifierId, out var modifier) ||
                    modifier.Kind != WordKind.Modifier ||
                    !WordCatalogue.AreCompatible(verb, modifier) ||
                    !state.Codex.Contains(modifierId))
                {
                    throw new InvalidOperationException(
                        "Goal 6A Loadout Modifier is not an attuned compatible Codex Modifier.");
                }
            }

            var load = verb.Load + slot.Modifiers.Sum(id => WordCatalogue.Get(id).Load);
            var recordedCapacity = state.Attunement?.Capacity ?? HoldingFacts.InherentLoadCapacity;
            if (load > recordedCapacity)
            {
                throw new InvalidOperationException(
                    "The current Loadout exceeds its recorded Attunement capacity.");
            }
        }
    }

    private static void ValidatePendingAction(
        ChronicleState state,
        TacticalActionState? action)
    {
        if (action is null)
        {
            return;
        }

        if (!Enum.IsDefined(action.Kind))
        {
            throw new InvalidOperationException("Goal 6A pending action has an unknown kind.");
        }

        if (action.Kind == TacticalActionKind.Move &&
            !((action.DeltaX is -1 or 1 && action.DeltaY == 0) ||
              (action.DeltaX == 0 && action.DeltaY is -1 or 1)))
        {
            throw new InvalidOperationException("Goal 6A pending movement must be one cardinal step.");
        }

        if (action.Kind == TacticalActionKind.PrepareBurn)
        {
            if (action.Target is not { } target)
            {
                throw new InvalidOperationException("Goal 6A pending Burn requires a Target.");
            }

            ValidateCurrentAddress(target, "Goal 6A pending Burn Target");
        }
    }

    private static void ValidatePreparation(
        ChronicleState state,
        BurnPreparationState? preparation)
    {
        if (preparation is null)
        {
            return;
        }

        var expression = preparation.Expression;
        var effect = WordEffects.Compose(expression);
        if (preparation.ActorIncarnationId <= 0 ||
            preparation.ActorIncarnationId > state.IncarnationId ||
            expression.Verb != WordIds.Burn ||
            expression.Noun is not null ||
            expression != state.ActiveLoadout.Slots.FirstOrDefault(slot => !slot.IsEmpty) ||
            effect.Preparation <= 0 ||
            preparation.RemainingTicks is < 1 ||
            preparation.RemainingTicks > effect.Preparation)
        {
            throw new InvalidOperationException("Goal 6A Burn Preparation is invalid.");
        }

        ValidateCurrentAddress(preparation.TargetAddressAtPreparation, "Burn Preparation Target");
    }

    private static void ValidateBurn(ChronicleState state, BurnConsequenceState? burn)
    {
        if (burn is null)
        {
            return;
        }

        var expression = state.ActiveLoadout.Slots.FirstOrDefault(slot => !slot.IsEmpty);
        var effect = WordEffects.Compose(expression);
        if (!string.Equals(
                burn.TargetIdentity,
                state.Combat!.MireBrute.Identity,
                StringComparison.Ordinal) ||
            expression.Verb != WordIds.Burn ||
            burn.Damage != effect.Damage ||
            effect.Consequence <= 0 ||
            burn.RemainingTicks is < 1 ||
            burn.RemainingTicks > effect.Consequence)
        {
            throw new InvalidOperationException("Goal 6A ongoing Burn is invalid.");
        }
    }

    private static void ValidatePowerHome(ChronicleState state)
    {
        if (state.WorldGrammarVersion is not (5 or 6))
        {
            if (state.PowerHome is not null)
            {
                throw new InvalidOperationException(
                    "Power Comes Home state requires World Grammar version 5 or 6.");
            }

            if (state.Attunement is { Capacity: not HoldingFacts.InherentLoadCapacity })
            {
                throw new InvalidOperationException(
                    "Older World Grammar pins retain only the inherent Load capacity.");
            }

            return;
        }

        var power = state.PowerHome
            ?? throw new InvalidOperationException("World Grammar version 5 or 6 requires one Resonant Lode.");
        var lode = power.Lode;
        if (!string.Equals(lode.Identity, HoldingRules.ResonantLodeIdentity(state.Seed), StringComparison.Ordinal) ||
            lode.OriginAddress != HoldingFacts.SingingSeamAddress)
        {
            throw new InvalidOperationException("The Resonant Lode must retain its generated identity and origin.");
        }

        if (!Enum.IsDefined(lode.Disposition) ||
            power.ExtractionProgress is < 0 or > HoldingRules.ExtractTicks)
        {
            throw new InvalidOperationException("Resonant Lode extraction state is outside its authored bounds.");
        }

        switch (lode.Disposition)
        {
            case ResonantLodeDisposition.Embedded:
                if (lode.Address != lode.OriginAddress || lode.CarrierIncarnationId is not null || power.Resonator is not null)
                {
                    throw new InvalidOperationException("An embedded Resonant Lode exists only at its persistent origin.");
                }
                break;
            case ResonantLodeDisposition.Loose:
                if (lode.Address is null || lode.CarrierIncarnationId is not null)
                {
                    throw new InvalidOperationException("A loose Resonant Lode requires exactly one world Address.");
                }
                ValidateCurrentAddress(lode.Address.Value, "Resonant Lode");
                break;
            case ResonantLodeDisposition.Carried:
                if (!state.HasLivingIncarnation || lode.Address is not null ||
                    lode.CarrierIncarnationId != state.IncarnationId)
                {
                    throw new InvalidOperationException(
                        "A carried Resonant Lode must belong exclusively to the living current Incarnation.");
                }
                if (state.Combat!.WeaponStanceActive)
                {
                    throw new InvalidOperationException("A Resonant Lode carrier cannot retain Iron Cleaver stance.");
                }
                break;
            case ResonantLodeDisposition.Committed:
            case ResonantLodeDisposition.Installed:
                if (power.Resonator is null || lode.Address != power.Resonator.Address || lode.CarrierIncarnationId is not null)
                {
                    throw new InvalidOperationException("Committed or installed Lode matter must remain at its Hearth Resonator.");
                }
                break;
        }

        if (power.ExtractionProgress < HoldingRules.ExtractTicks &&
            lode.Disposition != ResonantLodeDisposition.Embedded)
        {
            throw new InvalidOperationException("The Resonant Lode cannot leave its Seam before extraction completes.");
        }
        if (power.ExtractionProgress == HoldingRules.ExtractTicks &&
            lode.Disposition == ResonantLodeDisposition.Embedded)
        {
            throw new InvalidOperationException("Completed extraction must leave the Singing Seam visibly empty.");
        }

        if (power.Resonator is { } source)
        {
            var site = HoldingRules.ResonatorSite(state);
            if (site is null || source.Address != site.Value ||
                !string.Equals(source.Identity, HoldingRules.HearthResonatorIdentity(state.Seed), StringComparison.Ordinal) ||
                !Enum.IsDefined(source.Phase))
            {
                throw new InvalidOperationException("The sole Hearth Resonator must remain at Home's exact eligible site.");
            }

            var sourceStateValid = source.Phase switch
            {
                HearthResonatorPhase.UnderConstruction => source.Progress is >= 0 and < HoldingRules.BuildTicks &&
                                                          lode.Disposition == ResonantLodeDisposition.Committed,
                HearthResonatorPhase.Intact => source.Progress == HoldingRules.BuildTicks &&
                                               lode.Disposition == ResonantLodeDisposition.Installed,
                HearthResonatorPhase.Damaged => source.Progress == 1 &&
                                                lode.Disposition == ResonantLodeDisposition.Installed,
                HearthResonatorPhase.Destroyed => source.Progress == HoldingRules.DismantleTicks &&
                                                  lode.Disposition == ResonantLodeDisposition.Loose &&
                                                  lode.Address == source.Address,
                HearthResonatorPhase.Rebuilding => source.Progress is >= 0 and < HoldingRules.RebuildTicks &&
                                                   lode.Disposition == ResonantLodeDisposition.Committed,
                _ => false,
            };
            if (!sourceStateValid)
            {
                throw new InvalidOperationException("Hearth Resonator phase, progress, and Lode matter disagree.");
            }
        }

        if (power.Commitment is { } commitment)
        {
            if (!state.HasLivingIncarnation || commitment.ActorIncarnationId != state.IncarnationId ||
                !Enum.IsDefined(commitment.Kind) || commitment.CompletedTicks < 0 ||
                commitment.CompletedTicks >= commitment.TotalTicks || state.Combat!.PendingAction is not null ||
                state.Combat.Preparation is not null || lode.Disposition == ResonantLodeDisposition.Carried)
            {
                throw new InvalidOperationException("Power Comes Home commitment is not owned exclusively by the current body.");
            }

            var expectedTotal = commitment.Kind switch
            {
                PowerCommitmentKind.Extract => HoldingRules.ExtractTicks,
                PowerCommitmentKind.Build => HoldingRules.BuildTicks,
                PowerCommitmentKind.Dismantle => HoldingRules.DismantleTicks,
                PowerCommitmentKind.Rebuild => HoldingRules.RebuildTicks,
                _ => 0,
            };
            var expectedAddress = commitment.Kind == PowerCommitmentKind.Extract
                ? HoldingFacts.SingingSeamAddress
                : power.Resonator?.Address;
            var expectedSubject = commitment.Kind == PowerCommitmentKind.Extract
                ? lode.Identity
                : power.Resonator?.Identity;
            var representedProgressMatches = commitment.Kind switch
            {
                PowerCommitmentKind.Extract =>
                    lode.Disposition == ResonantLodeDisposition.Embedded &&
                    power.ExtractionProgress == commitment.CompletedTicks,
                PowerCommitmentKind.Build =>
                    power.Resonator is { Phase: HearthResonatorPhase.UnderConstruction } building &&
                    building.Progress == commitment.CompletedTicks &&
                    lode.Disposition == ResonantLodeDisposition.Committed,
                PowerCommitmentKind.Dismantle when commitment.CompletedTicks == 0 =>
                    power.Resonator is { Phase: HearthResonatorPhase.Intact },
                PowerCommitmentKind.Dismantle when commitment.CompletedTicks == 1 =>
                    power.Resonator is { Phase: HearthResonatorPhase.Damaged, Progress: 1 },
                PowerCommitmentKind.Rebuild =>
                    power.Resonator is { Phase: HearthResonatorPhase.Rebuilding } rebuilding &&
                    rebuilding.Progress == commitment.CompletedTicks &&
                    lode.Disposition == ResonantLodeDisposition.Committed,
                _ => false,
            };
            if (commitment.TotalTicks != expectedTotal || commitment.Address != expectedAddress ||
                !string.Equals(commitment.SubjectIdentity, expectedSubject, StringComparison.Ordinal) ||
                !HoldingRules.IsWithinInteractionReach(state.Address, commitment.Address) ||
                !representedProgressMatches)
            {
                throw new InvalidOperationException(
                    "Power commitment timing, subject, represented progress, or physical adjacency is invalid.");
            }
        }

        var usedLoad = HoldingRules.CurrentUsedLoad(state);
        if (state.Attunement is null)
        {
            if (usedLoad != 0)
            {
                throw new InvalidOperationException("A body awaiting fresh Attunement cannot retain an active Loadout.");
            }
        }
        else if (state.Attunement is { } attunement)
        {
            if (attunement.Capacity is not (HoldingFacts.InherentLoadCapacity or
                                            HoldingFacts.InherentLoadCapacity + HoldingFacts.SourceLoadContribution) ||
                attunement.Tick < 0 || attunement.Tick > state.Tick || usedLoad > attunement.Capacity)
            {
                throw new InvalidOperationException("Recorded Attunement capacity, tick, or active Load is invalid.");
            }

            if (attunement.Capacity > HoldingFacts.InherentLoadCapacity &&
                power.Resonator is null or { Phase: HearthResonatorPhase.UnderConstruction })
            {
                throw new InvalidOperationException("A missing or unfinished first Source cannot explain a twelve-Load Attunement.");
            }
        }
    }

    private static void ValidateAgents(ChronicleState state)
    {
        if (state.WorldGrammarVersion != 6)
        {
            if (state.Agents.Count != 0)
            {
                throw new InvalidOperationException(
                    "Consequential Agents require World Grammar version 6.");
            }

            return;
        }

        var home = state.Home
            ?? throw new InvalidOperationException("World Grammar version 6 Agents require Home.");
        var identities = new HashSet<string>(StringComparer.Ordinal);
        var occupied = new HashSet<WorldAddress>();
        var roadRolls = new HashSet<WorldAddress>();
        foreach (var agent in state.Agents)
        {
            if (!identities.Add(agent.Profile.Identity))
            {
                throw new InvalidOperationException("Consequential Agent identities must be unique.");
            }

            var generated = AgentGrammar.Generate(
                state.Seed,
                state.WorldGrammarVersion,
                agent.Profile.ProvenanceIdentity,
                agent.Profile.OriginAddress,
                agent.Profile.Ordinal);
            if (agent.Profile != generated)
            {
                throw new InvalidOperationException(
                    "A consequential Agent profile must match its stable generated provenance.");
            }

            if (!Enum.IsDefined(agent.Presence) ||
                !Enum.IsDefined(agent.Need.Kind) ||
                !Enum.IsDefined(agent.Need.Status) ||
                !Enum.IsDefined(agent.HomeRelationship.Kind) ||
                !Enum.IsDefined(agent.Intent))
            {
                throw new InvalidOperationException("A consequential Agent contains an unknown authored value.");
            }

            ValidateCurrentAddress(agent.Profile.OriginAddress, "Agent origin");
            ValidateCurrentAddress(agent.Address, "Agent");
            ValidateCurrentAddress(agent.WaitingAddress, "Agent waiting place");
            if (!occupied.Add(agent.Address))
            {
                throw new InvalidOperationException("Two consequential Agents cannot occupy one exclusive cell.");
            }

            if (agent.PromotedTick < 0 || agent.PromotedTick > state.Tick ||
                agent.ArrivalTick is { } arrivalTick &&
                (arrivalTick < agent.PromotedTick || arrivalTick > state.Tick) ||
                agent.WelcomeOfferedTick is { } offeredTick &&
                (offeredTick < agent.PromotedTick || offeredTick > state.Tick))
            {
                throw new InvalidOperationException("Agent event provenance must remain within Chronicle time.");
            }

            if (!string.Equals(
                    agent.HomeRelationship.HomeIdentity,
                    home.HoldingId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("An Agent Home relationship must name the singular Home.");
            }

            var validState = agent.Presence switch
            {
                AgentPresenceState.ApproachingHome =>
                    agent.Need.Status == AgentNeedStatus.Seeking &&
                    agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Unfamiliar &&
                    agent.Intent == AgentIntentKind.ApproachHome &&
                    agent.ArrivalTick is null &&
                    agent.WelcomeOfferedTick is null &&
                    agent.RoadRollAddress is null,
                AgentPresenceState.WaitingAtHome when
                    agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered =>
                    agent.Need.Status == AgentNeedStatus.Offered &&
                    agent.Intent == AgentIntentKind.ConsiderWelcome &&
                    agent.ArrivalTick is not null &&
                    agent.WelcomeOfferedTick is not null &&
                    agent.HomeRelationship.EstablishedTick is null &&
                    agent.HomeRelationship.WelcomingIncarnationId is > 0 &&
                    agent.RoadRollAddress is null,
                AgentPresenceState.WaitingAtHome =>
                    agent.Need.Status == AgentNeedStatus.Seeking &&
                    agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Unfamiliar &&
                    agent.Intent == AgentIntentKind.WaitForWelcome &&
                    agent.ArrivalTick is not null &&
                    agent.WelcomeOfferedTick is null &&
                    agent.HomeRelationship.EstablishedTick is null &&
                    agent.HomeRelationship.WelcomingIncarnationId is null &&
                    agent.RoadRollAddress is null,
                AgentPresenceState.AtHome =>
                    agent.Need.Status == AgentNeedStatus.Satisfied &&
                    agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Guest &&
                    agent.Intent is AgentIntentKind.RemainAtHome or AgentIntentKind.ConsiderDirective &&
                    agent.ArrivalTick is not null &&
                    agent.WelcomeOfferedTick is not null &&
                    agent.HomeRelationship.EstablishedTick is { } established &&
                    established >= agent.WelcomeOfferedTick &&
                    established <= state.Tick &&
                    agent.HomeRelationship.WelcomingIncarnationId is > 0 &&
                    agent.RoadRollAddress is not null,
                _ => false,
            };
            if (!validState)
            {
                throw new InvalidOperationException(
                    "Agent presence, need, relationship, intent, and material state disagree.");
            }

            if (agent.HomeRelationship.WelcomingIncarnationId is { } welcomingIncarnation &&
                welcomingIncarnation > state.IncarnationId)
            {
                throw new InvalidOperationException(
                    "An Agent welcome cannot name a future Incarnation.");
            }

            if (agent.RoadRollAddress is { } roadRoll)
            {
                ValidateCurrentAddress(roadRoll, "Agent road-roll");
                if (!roadRolls.Add(roadRoll))
                {
                    throw new InvalidOperationException(
                        "Agent road-roll ownership and exclusive occupancy must be unambiguous.");
                }
            }

            ValidateDirectiveState(state, agent);
        }

        if (state.Agents.Any(agent => state.Agents.Any(other =>
                !string.Equals(agent.Profile.Identity, other.Profile.Identity, StringComparison.Ordinal) &&
                other.RoadRollAddress == agent.Address)))
        {
            throw new InvalidOperationException("An Agent cannot occupy another Agent's road-roll.");
        }

        var listenerProfile = AgentRules.ResonanceListenerProfile(state);
        var listener = state.Agents.Find(listenerProfile.Identity);
        var sourceHasCompleted = state.PowerHome?.Resonator?.Phase is
            HearthResonatorPhase.Intact or
            HearthResonatorPhase.Damaged or
            HearthResonatorPhase.Destroyed or
            HearthResonatorPhase.Rebuilding;
        if (sourceHasCompleted && listener is null)
        {
            throw new InvalidOperationException(
                "A WG6 Chronicle whose first Resonator completed must retain its promoted Agent.");
        }

        if (listener is not null)
        {
            if (listener.WaitingAddress != AgentRules.ResonanceListenerWaitingAddress(state) ||
                listener.Presence == AgentPresenceState.ApproachingHome &&
                (listener.Address.Y != listener.WaitingAddress.Y ||
                 listener.Address.X < AgentRules.ResonanceListenerStartAddress(state).X ||
                 listener.Address.X >= listener.WaitingAddress.X) ||
                listener.Presence == AgentPresenceState.WaitingAtHome &&
                listener.Address != listener.WaitingAddress ||
                listener.Presence == AgentPresenceState.AtHome &&
                listener.Address != listener.WaitingAddress &&
                listener.Address != listener.RoadRollAddress ||
                listener.RoadRollAddress is { } roadRoll &&
                roadRoll != AgentRules.ResonanceListenerRoadRollAddress(state))
            {
                throw new InvalidOperationException(
                    "The generated resonance listener must retain its Home-relative arrival and road-roll addresses.");
            }
        }
    }

    private static void ValidateDirectiveState(ChronicleState state, AgentState agent)
    {
        if (agent.PendingDirective is { } pending)
        {
            if (agent.Presence != AgentPresenceState.AtHome ||
                agent.HomeRelationship.Kind != AgentHomeRelationshipKind.Guest ||
                agent.Intent != AgentIntentKind.ConsiderDirective ||
                !string.Equals(pending.AgentIdentity, agent.Profile.Identity, StringComparison.Ordinal) ||
                pending.IssuingIncarnationId <= 0 ||
                pending.IssuingIncarnationId > state.IncarnationId ||
                pending.IssuedTick < agent.PromotedTick ||
                pending.IssuedTick > state.Tick ||
                pending.ResolvesAtTick != pending.IssuedTick + 1 ||
                pending.ResolvesAtTick <= state.Tick ||
                DirectiveRules.ForceFor(pending.Verb) is not { } force ||
                force < DirectiveRules.Definition(pending.Directive).MinimumForce)
            {
                throw new InvalidOperationException(
                    "A pending Directive must retain a valid recipient, issuer, force, intent, and future Heartbeat.");
            }

            ValidateCurrentAddress(pending.ObjectiveAddress, "Directive objective");
            ValidateCurrentAddress(pending.DeliveryAddress, "Directive delivery");
            if (!DirectiveObjectiveMatches(state, agent, pending.Directive, pending.ObjectiveIdentity, pending.ObjectiveAddress))
            {
                throw new InvalidOperationException(
                    "A pending Directive must retain its canonical Chronicle objective.");
            }
        }
        else if (agent.Intent == AgentIntentKind.ConsiderDirective)
        {
            throw new InvalidOperationException(
                "An Agent cannot consider a missing pending Directive.");
        }

        var previousResolvedTick = -1L;
        var identities = new HashSet<(long IssuedTick, long Issuer, DirectiveKind Directive)>();
        foreach (var memory in agent.DirectiveMemories)
        {
            if (!string.Equals(memory.AgentIdentity, agent.Profile.Identity, StringComparison.Ordinal) ||
                memory.IssuingIncarnationId <= 0 ||
                memory.IssuingIncarnationId > state.IncarnationId ||
                memory.IssuedTick < agent.PromotedTick ||
                memory.ResolvedTick <= memory.IssuedTick ||
                memory.ResolvedTick > state.Tick ||
                memory.ResolvedTick < previousResolvedTick ||
                !identities.Add((memory.IssuedTick, memory.IssuingIncarnationId, memory.Directive)) ||
                DirectiveRules.ForceFor(memory.Verb) is not { } force ||
                force < DirectiveRules.Definition(memory.Directive).MinimumForce ||
                !Enum.IsDefined(memory.Response) ||
                !Enum.IsDefined(memory.Reason) ||
                !Enum.IsDefined(memory.Blocker))
            {
                throw new InvalidOperationException(
                    "A Directive memory contains impossible identity, force, time, order, or authored values.");
            }

            ValidateCurrentAddress(memory.ObjectiveAddress, "Directive memory objective");
            ValidateCurrentAddress(memory.ResultingAddress, "Directive memory result");
            var responseMatches = memory switch
            {
                {
                    Directive: DirectiveKind.RestByRoadRoll,
                    Response: DirectiveResponseKind.Accepted,
                    Reason: DirectiveResponseReason.RestAccepted,
                    Blocker: AgentBlockerKind.None,
                } => memory.ResultingAddress == memory.ObjectiveAddress,
                {
                    Directive: DirectiveKind.RestByRoadRoll,
                    Response: DirectiveResponseKind.Delayed,
                    Reason: DirectiveResponseReason.DestinationBlocked,
                } => memory.Blocker != AgentBlockerKind.None,
                {
                    Directive: DirectiveKind.ApproachMireBrute,
                    Response: DirectiveResponseKind.Refused,
                    Reason: DirectiveResponseReason.GuestHasNoViolentCommitment,
                    Blocker: AgentBlockerKind.None,
                } => true,
                _ => false,
            };
            if (!responseMatches || string.IsNullOrWhiteSpace(memory.ObjectiveIdentity))
            {
                throw new InvalidOperationException(
                    "A Directive memory response, reason, blocker, and result disagree.");
            }

            previousResolvedTick = memory.ResolvedTick;
        }
    }

    private static bool DirectiveObjectiveMatches(
        ChronicleState state,
        AgentState agent,
        DirectiveKind directive,
        string identity,
        WorldAddress address) => directive switch
        {
            DirectiveKind.RestByRoadRoll =>
                agent.RoadRollAddress == address &&
                string.Equals(identity, AgentRules.RoadRollIdentity(agent.Profile.Identity), StringComparison.Ordinal),
            DirectiveKind.ApproachMireBrute =>
                state.Combat?.MireBrute is { IsLiving: true } brute &&
                brute.Address == address &&
                string.Equals(identity, brute.Identity, StringComparison.Ordinal),
            _ => false,
        };

    private static void ValidateCurrentAddress(WorldAddress address, string subject)
    {
        if (!string.Equals(
                address.Stratum,
                SurfacePatch.SurfaceStratum,
                StringComparison.Ordinal) &&
            !string.Equals(
                address.Stratum,
                SkyStratum.StratumName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{subject} occupies unsupported Stratum '{address.Stratum}'.");
        }
    }
}
