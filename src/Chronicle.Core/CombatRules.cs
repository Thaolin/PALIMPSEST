using System.Text.Json.Serialization;

namespace Chronicle.Core;

/// <summary>
/// Durable, bounded state for the Goal 6A encounter. It intentionally models
/// one authored opponent and one authored Invocation rather than a reusable
/// combat framework.
/// </summary>
public sealed record CombatState(
    int IncarnationHitPoints,
    EquipmentState Equipment,
    EngagementPlanState EngagementPlan,
    bool WeaponStanceActive,
    int WeaponTicksUntilReady,
    bool EngagementActive,
    MireBruteState MireBrute,
    TacticalActionState? PendingAction = null,
    BurnPreparationState? Preparation = null,
    BurnConsequenceState? OngoingBurn = null,
    int RecoveryRemaining = 0,
    ScorchedGroundState? Scorch = null)
{
    public const int BaseIncarnationHitPoints = 30;
    public const int SharedLoadCapacity = 8;
    public const int GrammarFourLinkCapacity = 2;
    public const int LinkCapacity = GrammarFourLinkCapacity;
    public const int ActiveVerbSlots = 1;
    public const int BurnRange = 3;
    public static int BurnDamage => WordEffects.BaseFor(WordIds.Burn).Damage;
    public static int BurnRecovery => WordEffects.BaseFor(WordIds.Burn).Recovery;
    public const int IronCleaverDamage = 5;
    public const int IronCleaverCadence = 2;
    public const int MireBruteMaximumHitPoints = 45;
    public const int MireBruteSwingDamage = 7;
    public const int MireBruteSwingCadence = 3;
    public const int QuiltedJackReduction = 2;

    [JsonIgnore]
    public int MaximumHitPoints => BaseIncarnationHitPoints + Equipment.MaximumHitPointBonus;

    public static CombatState Create(long seed) => new(
        IncarnationHitPoints: BaseIncarnationHitPoints + EquipmentState.Fixed.MaximumHitPointBonus,
        Equipment: EquipmentState.Fixed,
        EngagementPlan: new EngagementPlanState(OpenWithWeaponStance: false),
        WeaponStanceActive: false,
        WeaponTicksUntilReady: 0,
        EngagementActive: false,
        MireBrute: new MireBruteState(
            Identity: WorldArea.GeneratedMireBruteIdentity(seed),
            OriginAddress: WorldArea.GeneratedMireBruteAddress(seed),
            Address: WorldArea.GeneratedMireBruteAddress(seed),
            HitPoints: MireBruteMaximumHitPoints,
            SwingTicksRemaining: MireBruteSwingCadence));
}

public sealed record EquipmentState(
    string WeaponIdentity,
    string WeaponName,
    string ArmorIdentity,
    string ArmorName,
    string AccessoryIdentity,
    string AccessoryName,
    int MaximumHitPointBonus,
    int PhysicalDamageReduction)
{
    public static EquipmentState Fixed { get; } = new(
        WeaponIdentity: "equipment.iron-cleaver",
        WeaponName: "Iron Cleaver",
        ArmorIdentity: "equipment.quilted-jack",
        ArmorName: "Quilted Jack",
        AccessoryIdentity: "equipment.copper-ward",
        AccessoryName: "Copper Ward",
        MaximumHitPointBonus: 4,
        PhysicalDamageReduction: CombatState.QuiltedJackReduction);
}

public sealed record EngagementPlanState(bool OpenWithWeaponStance);

public enum TacticalActionKind
{
    Move = 1,
    SetWeaponStance = 2,
    PrepareBurn = 3,
}

public sealed record TacticalActionState(
    TacticalActionKind Kind,
    int DeltaX = 0,
    int DeltaY = 0,
    bool WeaponStanceActive = false,
    WorldAddress? Target = null);

public sealed record BurnPreparationState(
    long ActorIncarnationId,
    string TargetIdentity,
    WorldAddress TargetAddressAtPreparation,
    LoadoutSlot Expression,
    int RemainingTicks);

public sealed record BurnConsequenceState(
    string TargetIdentity,
    int Damage,
    int RemainingTicks);

public sealed record MireBruteState(
    string Identity,
    WorldAddress OriginAddress,
    WorldAddress Address,
    int HitPoints,
    int SwingTicksRemaining,
    long? DefeatedTick = null)
{
    [JsonIgnore]
    public bool IsLiving => HitPoints > 0;
}

public sealed record ScorchedGroundState(WorldAddress Address, long CreatedTick);

public enum CombatResultKind
{
    Command = 1,
    Engagement = 2,
    Movement = 3,
    Stance = 4,
    PreparationStarted = 5,
    PreparationInterrupted = 6,
    InvocationReleased = 7,
    BurnDamage = 8,
    WeaponStrike = 9,
    MireBruteMove = 10,
    MireBruteSwing = 11,
    MireBruteDied = 12,
    IncarnationDied = 13,
    RecoveryComplete = 14,
    RecoverySkipped = 15,
    PowerHome = 16,
}

public sealed record CombatResultSnapshot(
    long Tick,
    CombatResultKind Kind,
    string Text,
    int? Damage = null,
    WorldAddress? Address = null);

public enum CombatForecastKind
{
    PendingAction = 1,
    BurnRelease = 2,
    BurnDamage = 3,
    WeaponStrike = 4,
    MireBruteMove = 5,
    MireBruteSwing = 6,
    RecoveryComplete = 7,
    Engagement = 8,
}

public sealed record CombatForecastEventSnapshot(
    long Tick,
    CombatForecastKind Kind,
    string Text,
    int? Damage = null,
    WorldAddress? Address = null);

public enum CombatTargetKind
{
    Missing = 0,
    MireBrute = 1,
    Basalt = 2,
}

public sealed record TargetFactsSnapshot(
    bool IsLiving,
    bool IsOrganic,
    bool IsFlammable,
    bool IsMassive,
    bool IsAnchored,
    string Matter,
    string Scale);

public sealed record TargetPreviewSnapshot(
    CombatTargetKind Kind,
    string? Identity,
    string DisplayName,
    WorldAddress Address,
    TargetFactsSnapshot Facts,
    int CardinalDistance,
    int? HitPoints,
    int? MaximumHitPoints,
    bool CanBurn,
    string EligibilityReason,
    int PreparationTicks,
    int ConsequenceTicks,
    int RecoveryTicks,
    bool IsCurrentTarget);

public sealed record EquipmentSnapshot(
    string WeaponName,
    int WeaponDamage,
    int WeaponCadence,
    string ArmorName,
    int ArmorReduction,
    string AccessoryName,
    int MaximumHitPointBonus);

public sealed record IncarnationCombatSnapshot(
    long Identity,
    WorldAddress Address,
    int HitPoints,
    int MaximumHitPoints,
    bool IsLiving);

public sealed record MireBruteSnapshot(
    string Identity,
    WorldAddress OriginAddress,
    WorldAddress Address,
    int HitPoints,
    int MaximumHitPoints,
    bool IsLiving,
    bool IsBurning,
    int SwingTicksRemaining);

public sealed record DangerSnapshot(bool IsImmediate, int? CardinalDistance, string Status);

public sealed record ExpressionSnapshot(
    WordId? Verb,
    IReadOnlyList<WordId> Modifiers,
    int UsedLoad,
    int SharedLoadCapacity,
    int UsedLinks,
    int LinkCapacity,
    int ActiveVerbSlots,
    string DisplayName);

public sealed record PendingActionSnapshot(
    TacticalActionKind Kind,
    string DisplayName,
    WorldAddress? Target,
    int? DeltaX,
    int? DeltaY,
    bool? WeaponStanceActive);

public sealed record PreparationSnapshot(
    string DisplayName,
    string TargetIdentity,
    WorldAddress TargetAddressAtPreparation,
    int RemainingTicks,
    int TotalTicks,
    string InterruptionRisk);

public sealed record BurnConsequenceSnapshot(
    string TargetIdentity,
    int Damage,
    int RemainingTicks);

public sealed record RecoverySnapshot(int RemainingTicks, bool CanSkipSafely);

/// <summary>
/// Read-only presentation data. The message feed is deliberately supplied by
/// the simulation instance and is never part of this durable state shape.
/// </summary>
public sealed record CombatContextSnapshot(
    IncarnationCombatSnapshot Incarnation,
    EquipmentSnapshot Equipment,
    MireBruteSnapshot? MireBrute,
    IReadOnlyList<TargetPreviewSnapshot> Targets,
    DangerSnapshot Danger,
    EngagementPlanState EngagementPlan,
    bool WeaponStanceActive,
    ExpressionSnapshot Expression,
    PendingActionSnapshot? PendingAction,
    PreparationSnapshot? Preparation,
    BurnConsequenceSnapshot? OngoingBurn,
    RecoverySnapshot Recovery,
    ScorchedGroundState? Scorch,
    IReadOnlyList<CombatResultSnapshot> RecentResults,
    IReadOnlyList<CombatForecastEventSnapshot> Forecast);

internal readonly record struct CombatAdvanceResult(
    ChronicleState State,
    IReadOnlyList<CombatResultSnapshot> Results);

internal static class CombatRules
{
    private static readonly TargetFactsSnapshot MireBruteFacts = new(
        IsLiving: true,
        IsOrganic: true,
        IsFlammable: true,
        IsMassive: true,
        IsAnchored: false,
        Matter: "living organic",
        Scale: "massive");

    private static readonly TargetFactsSnapshot BasaltFacts = new(
        IsLiving: false,
        IsOrganic: false,
        IsFlammable: false,
        IsMassive: true,
        IsAnchored: true,
        Matter: "mineral basalt",
        Scale: "anchored place");

    internal static bool IsAvailable(ChronicleState state) =>
        state.WorldGrammarVersion is 4 or 5 or 6 && state.Combat is not null;

    internal static bool IsImmediateDanger(ChronicleState state)
    {
        if (!IsAvailable(state) ||
            !state.HasLivingIncarnation ||
            !state.Combat!.MireBrute.IsLiving ||
            !string.Equals(
                state.Address.Stratum,
                state.Combat.MireBrute.Address.Stratum,
                StringComparison.Ordinal))
        {
            return false;
        }

        return CardinalDistance(state.Address, state.Combat.MireBrute.Address) <= 3;
    }

    internal static TargetPreviewSnapshot PreviewTarget(
        ChronicleState state,
        WorldAddress address)
    {
        if (!IsAvailable(state))
        {
            return MissingTarget(address, "This Chronicle has no combat Targets.");
        }

        var combat = state.Combat!;
        var brute = combat.MireBrute;
        if (address == brute.Address)
        {
            var expression = ActiveExpression(state);
            var distance = CardinalDistance(state.Address, address);
            var canBurn = state.HasLivingIncarnation &&
                          brute.IsLiving &&
                          distance <= CombatState.BurnRange &&
                          expression.Verb == WordIds.Burn &&
                          !HoldingFacts.IsCarrying(state) &&
                          !HoldingFacts.HasCommitment(state) &&
                          combat.RecoveryRemaining == 0 &&
                          combat.PendingAction is null &&
                          combat.Preparation is null;
            var eligibility = !state.HasLivingIncarnation
                ? "A replacement Incarnation is required before Burn can be prepared."
                : HoldingFacts.IsCarrying(state)
                    ? "Set down the Resonant Lode before preparing Burn; carrying occupies both hands and focused Attunement."
                    : HoldingFacts.HasCommitment(state)
                        ? "Finish or cancel the physical commitment before preparing Burn."
                : !brute.IsLiving
                    ? "The Mire Brute is already dead."
                    : distance > CombatState.BurnRange
                        ? "The Mire Brute is outside Burn range."
                        : expression.Verb != WordIds.Burn
                            ? "Attune Burn before preparing this Invocation."
                            : combat.RecoveryRemaining > 0
                                ? $"Burn is recovering for {combat.RecoveryRemaining} more Heartbeats."
                                : combat.PendingAction is not null || combat.Preparation is not null
                                    ? "Finish or cancel the current tactical action before preparing Burn."
                                    : "The Mire Brute is flammable and within Burn range.";
            return new TargetPreviewSnapshot(
                CombatTargetKind.MireBrute,
                brute.Identity,
                "Mire Brute",
                address,
                MireBruteFacts,
                distance,
                brute.HitPoints,
                CombatState.MireBruteMaximumHitPoints,
                canBurn,
                eligibility,
                PreparationFor(expression),
                ConsequenceFor(expression),
                RecoveryFor(expression),
                IsCurrentTarget(state, brute.Identity));
        }

        if (address == WorldArea.GeneratedBasaltAddress(state.Seed))
        {
            return new TargetPreviewSnapshot(
                CombatTargetKind.Basalt,
                WorldArea.GeneratedBasaltIdentity(state.Seed),
                "Basalt",
                address,
                BasaltFacts,
                CardinalDistance(state.Address, address),
                null,
                null,
                false,
                "Basalt is mineral, nonflammable, and anchored.",
                PreparationFor(ActiveExpression(state)),
                ConsequenceFor(ActiveExpression(state)),
                RecoveryFor(ActiveExpression(state)),
                IsCurrentTarget(state, WorldArea.GeneratedBasaltIdentity(state.Seed)));
        }

        return MissingTarget(address, "There is no selectable Target there.");
    }

    internal static IReadOnlyList<TargetPreviewSnapshot> Targets(ChronicleState state)
    {
        if (!IsAvailable(state))
        {
            return [];
        }

        var combat = state.Combat!;
        return
        [
            PreviewTarget(state, combat.MireBrute.Address),
            PreviewTarget(state, WorldArea.GeneratedBasaltAddress(state.Seed)),
        ];
    }

    internal static bool TryStartPreparation(
        ChronicleState state,
        WorldAddress target,
        out ChronicleState updated,
        out string message)
    {
        updated = state;
        if (!IsAvailable(state))
        {
            message = "Burn is available only in a World Grammar v4 Chronicle.";
            return false;
        }

        if (HoldingFacts.IsCarrying(state))
        {
            message = "Set down the Resonant Lode before preparing Burn; carrying occupies both hands and focused Attunement.";
            return false;
        }

        if (HoldingFacts.HasCommitment(state))
        {
            message = "Finish or cancel the physical commitment before preparing Burn.";
            return false;
        }

        var combat = state.Combat!;
        if (combat.RecoveryRemaining > 0)
        {
            message = $"Burn is recovering for {combat.RecoveryRemaining} more Heartbeats.";
            return false;
        }

        var expression = ActiveExpression(state);
        if (expression.Verb != WordIds.Burn)
        {
            message = "Attune Burn before preparing this Invocation.";
            return false;
        }

        var preview = PreviewTarget(state, target);
        if (!preview.CanBurn || preview.Identity is null)
        {
            message = preview.EligibilityReason;
            return false;
        }

        var preparation = new BurnPreparationState(
            state.IncarnationId,
            preview.Identity,
            preview.Address,
            expression,
            PreparationFor(expression));
        updated = state with
        {
            Combat = combat with
            {
                PendingAction = null,
                Preparation = preparation,
            },
        };
        message = $"Preparing {ExpressionName(expression)} against the Mire Brute.";
        return true;
    }

    internal static CombatAdvanceResult Advance(
        ChronicleState state,
        IMaterialCommitments material)
    {
        if (!IsAvailable(state) ||
            !state.HasLivingIncarnation ||
            state.Speed == ChronicleSpeed.Paused)
        {
            return new CombatAdvanceResult(state, []);
        }

        if (state.Tick >= long.MaxValue - 1)
        {
            return new CombatAdvanceResult(state with { Speed = ChronicleSpeed.Paused }, []);
        }

        var results = new List<CombatResultSnapshot>();
        var beforeDanger = IsImmediateDanger(state);
        var next = state with { Tick = checked(state.Tick + 1) };
        var powerAdvance = material.AdvanceAfterTick(next);
        next = powerAdvance.State;
        if (powerAdvance.Message is { } powerMessage)
        {
            results.Add(new CombatResultSnapshot(
                next.Tick,
                CombatResultKind.PowerHome,
                powerMessage,
                Address: powerAdvance.Address));
        }
        var combat = next.Combat!;

        // 1. Resolve exactly one queued movement or stance/preparation command.
        if (combat.PendingAction is { } pending)
        {
            combat = combat with { PendingAction = null };
            next = next with { Combat = combat };
            switch (pending.Kind)
            {
                case TacticalActionKind.Move:
                    var destination = new WorldAddress(
                        next.Address.Stratum,
                        checked(next.Address.X + pending.DeltaX),
                        checked(next.Address.Y + pending.DeltaY));
                    if (IsOccupiedByLivingMireBrute(next, destination) ||
                        HoldingFacts.BlocksMovement(next, destination))
                    {
                        results.Add(Result(
                            next,
                            CombatResultKind.Command,
                            IsOccupiedByLivingMireBrute(next, destination)
                                ? "The living Mire Brute occupies that cell."
                                : "A physical subject occupies that cell.",
                            destination));
                    }
                    else
                    {
                        next = next.TravelTo(destination);
                        results.Add(Result(next, CombatResultKind.Movement, "The Incarnation moves.", next.Address));
                    }
                    break;
                case TacticalActionKind.SetWeaponStance:
                    combat = next.Combat! with { WeaponStanceActive = pending.WeaponStanceActive };
                    next = next with { Combat = combat };
                    results.Add(Result(
                        next,
                        CombatResultKind.Stance,
                        pending.WeaponStanceActive ? "Iron Cleaver stance readied." : "Iron Cleaver stance lowered."));
                    break;
                case TacticalActionKind.PrepareBurn:
                    if (pending.Target is not { } target)
                    {
                        results.Add(Result(
                            next,
                            CombatResultKind.PreparationInterrupted,
                            "Burn preparation lost its selected Target."));
                    }
                    else if (TryStartPreparation(next, target, out var prepared, out var message))
                    {
                        next = prepared;
                        results.Add(Result(next, CombatResultKind.PreparationStarted, message));
                    }
                    else
                    {
                        results.Add(Result(next, CombatResultKind.PreparationInterrupted, message));
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Unknown tactical action '{pending.Kind}'.");
            }
        }

        // Contact must pause before the first hostile stage, even when the
        // just-resolved move was what crossed the threat radius.
        if (!beforeDanger && IsImmediateDanger(next))
        {
            next = ApplyEngagement(next, results);
            return new CombatAdvanceResult(next, results);
        }

        // 2. Advance/release the one prepared Invocation.
        combat = next.Combat!;
        var released = false;
        var preparationInterrupted = false;
        if (combat.Preparation is { } preparation)
        {
            var remaining = preparation.RemainingTicks - 1;
            if (remaining > 0)
            {
                next = next with
                {
                    Combat = combat with { Preparation = preparation with { RemainingTicks = remaining } },
                };
            }
            else if (!TryReleaseBurn(next, preparation, out next, out var releaseMessage))
            {
                preparationInterrupted = true;
                results.Add(Result(next, CombatResultKind.PreparationInterrupted, releaseMessage));
            }
            else
            {
                released = true;
                results.Add(Result(next, CombatResultKind.InvocationReleased, releaseMessage, next.Combat!.Scorch?.Address));
            }
        }

        // 3. Apply an already released burn, including one released above.
        var bruteDied = false;
        if (next.Combat!.OngoingBurn is not null)
        {
            (next, bruteDied) = ApplyBurnTick(next, results);
        }

        // 4. Resolve the ready automatic weapon strike.
        if (!bruteDied)
        {
            (next, bruteDied) = ApplyWeapon(next, results);
        }

        // 5. Resolve the authored Brute's bounded pursuit or cadence.
        if (!bruteDied && next.HasLivingIncarnation && IsImmediateDanger(next))
        {
            (next, preparationInterrupted) = ApplyMireBrute(next, results, preparationInterrupted, material);
        }

        // 6. Recovery and all derived transitions happen after observable
        // action stages so release beats a same-Heartbeat hostile swing.
        next = AdvanceRecovery(next, results);
        var afterDanger = IsImmediateDanger(next);
        var afterCombat = next.Combat!;
        if (!afterDanger && afterCombat.EngagementActive)
        {
            next = next with { Combat = afterCombat with { EngagementActive = false } };
        }

        if (released || preparationInterrupted || bruteDied || !next.HasLivingIncarnation)
        {
            next = next with { Speed = ChronicleSpeed.Paused };
        }

        return new CombatAdvanceResult(next, results);
    }

    internal static CombatContextSnapshot Snapshot(
        ChronicleState state,
        IReadOnlyList<CombatResultSnapshot> recentResults,
        IMaterialCommitments material)
    {
        if (!IsAvailable(state))
        {
            return new CombatContextSnapshot(
                new IncarnationCombatSnapshot(
                    state.IncarnationId,
                    state.Address,
                    0,
                    0,
                    state.HasLivingIncarnation),
                new EquipmentSnapshot("", 0, 0, "", 0, "", 0),
                null,
                [],
                new DangerSnapshot(false, null, "No combat encounter in this World Grammar pin."),
                new EngagementPlanState(false),
                false,
                EmptyExpression(),
                null,
                null,
                null,
                new RecoverySnapshot(0, false),
                null,
                recentResults,
                []);
        }

        var combat = state.Combat!;
        var brute = combat.MireBrute;
        var danger = IsImmediateDanger(state);
        var expression = ExpressionSnapshot(state);
        return new CombatContextSnapshot(
            new IncarnationCombatSnapshot(
                state.IncarnationId,
                state.Address,
                combat.IncarnationHitPoints,
                combat.MaximumHitPoints,
                state.HasLivingIncarnation),
            new EquipmentSnapshot(
                combat.Equipment.WeaponName,
                CombatState.IronCleaverDamage,
                CombatState.IronCleaverCadence,
                combat.Equipment.ArmorName,
                combat.Equipment.PhysicalDamageReduction,
                combat.Equipment.AccessoryName,
                combat.Equipment.MaximumHitPointBonus),
            new MireBruteSnapshot(
                brute.Identity,
                brute.OriginAddress,
                brute.Address,
                brute.HitPoints,
                CombatState.MireBruteMaximumHitPoints,
                brute.IsLiving,
                combat.OngoingBurn?.TargetIdentity == brute.Identity,
                brute.SwingTicksRemaining),
            Targets(state),
            new DangerSnapshot(
                danger,
                string.Equals(state.Address.Stratum, brute.Address.Stratum, StringComparison.Ordinal)
                    ? CardinalDistance(state.Address, brute.Address)
                    : null,
                danger
                    ? "Mire Brute is an immediate threat."
                    : brute.IsLiving
                        ? "Mire Brute is outside immediate threat range."
                        : "Mire Brute defeated."),
            combat.EngagementPlan,
            combat.WeaponStanceActive,
            expression,
            PendingSnapshot(combat.PendingAction),
            PreparationSnapshot(combat.Preparation),
            combat.OngoingBurn is { } burning
                ? new BurnConsequenceSnapshot(burning.TargetIdentity, burning.Damage, burning.RemainingTicks)
                : null,
            new RecoverySnapshot(combat.RecoveryRemaining, CanSkipRecovery(state)),
            combat.Scorch,
            recentResults,
            Forecast(state, material));
    }

    internal static IReadOnlyList<CombatForecastEventSnapshot> Forecast(
        ChronicleState state,
        IMaterialCommitments material)
    {
        if (!IsAvailable(state) || !state.HasLivingIncarnation)
        {
            return [];
        }

        var projected = state with { Speed = ChronicleSpeed.Slow };
        var forecast = new List<CombatForecastEventSnapshot>(4);
        for (var heartbeat = 0; heartbeat < 64 && forecast.Count < 4; heartbeat++)
        {
            var result = Advance(projected, material);
            if (result.State == projected)
            {
                break;
            }

            projected = result.State;
            foreach (var eventResult in result.Results)
            {
                if (ToForecast(eventResult) is { } forecastEvent)
                {
                    forecast.Add(forecastEvent);
                    if (forecast.Count == 4)
                    {
                        break;
                    }
                }
            }

            if (projected.HasLivingIncarnation &&
                projected.Combat!.MireBrute.IsLiving &&
                projected.Speed == ChronicleSpeed.Paused)
            {
                projected = projected with { Speed = ChronicleSpeed.Slow };
            }
        }

        return Array.AsReadOnly(forecast.ToArray());
    }

    internal static bool CanSkipRecovery(ChronicleState state) =>
        IsAvailable(state) &&
        state.Combat!.RecoveryRemaining > 0 &&
        state.Tick < long.MaxValue - state.Combat.RecoveryRemaining &&
        !IsImmediateDanger(state) &&
        state.Combat.Preparation is null &&
        state.Combat.PendingAction is null &&
        state.Combat.OngoingBurn is null &&
        state.HasLivingIncarnation;

    internal static ChronicleState SkipRecovery(ChronicleState state)
    {
        if (!CanSkipRecovery(state))
        {
            return state;
        }

        var combat = state.Combat!;
        return state with
        {
            Tick = checked(state.Tick + combat.RecoveryRemaining),
            Combat = combat with { RecoveryRemaining = 0 },
        };
    }

    internal static bool TryValidateExpression(
        ChronicleState state,
        WordId verbId,
        IReadOnlyList<WordId> requestedModifiers,
        out LoadoutSlot slot,
        out string message)
    {
        slot = default;
        if (!WordCatalogue.TryGet(verbId, out var verb) || verb.Kind != WordKind.Verb)
        {
            message = "That Verb is unknown.";
            return false;
        }

        if (!state.Codex.Contains(verbId))
        {
            message = $"{verb.DisplayName} is not in the Codex.";
            return false;
        }

        if (requestedModifiers.Distinct().Count() != requestedModifiers.Count)
        {
            message = "A Modifier may appear only once in an Expression.";
            return false;
        }

        var modifiers = WordCatalogue.Canonicalize(requestedModifiers);
        if (modifiers.Any(id => !WordCatalogue.TryGet(id, out var definition) || definition.Kind != WordKind.Modifier))
        {
            message = "Expressions may attach only known Modifiers.";
            return false;
        }

        if (modifiers.Any(id => !state.Codex.Contains(id)))
        {
            message = "Every attached Modifier must be in the Codex.";
            return false;
        }

        if (modifiers.Any(id =>
                !WordCatalogue.AreCompatible(verb, WordCatalogue.Get(id))))
        {
            message = $"That Modifier is incompatible with {verb.DisplayName}.";
            return false;
        }

        var load = verb.Load + modifiers.Sum(id => WordCatalogue.Get(id).Load);
        var linkCapacity = HoldingFacts.LinkCapacityFor(state);
        var loadCapacity = HoldingFacts.NextAttunementCapacity(state);
        var exceedsLinks = requestedModifiers.Count > linkCapacity - 1;
        var exceedsLoad = load > loadCapacity;
        if (exceedsLinks && exceedsLoad)
        {
            message = $"That Expression exceeds the {linkCapacity}-Link limit and needs " +
                      $"{load} Load; next Attunement capacity is {loadCapacity}.";
            return false;
        }

        if (exceedsLinks)
        {
            message = $"This Expression exceeds the {linkCapacity}-Link limit.";
            return false;
        }

        if (exceedsLoad)
        {
            message = HoldingFacts.IsAvailable(state) && loadCapacity == HoldingFacts.InherentLoadCapacity
                ? $"Needs {load} Load; next Attunement capacity is {loadCapacity}. " +
                  (state.PowerHome!.Resonator?.Phase switch
                  {
                      HearthResonatorPhase.Destroyed =>
                          "The Hearth Resonator is destroyed; rebuilding it would restore 4.",
                      HearthResonatorPhase.Rebuilding =>
                          "The Hearth Resonator is rebuilding and contributes 0 until complete.",
                      HearthResonatorPhase.UnderConstruction =>
                          "The Hearth Resonator is unfinished and contributes 0 until complete.",
                      _ => "An intact Hearth Resonator at Home would add 4.",
                  })
                : $"That Expression needs {load} Load; next Attunement capacity is {loadCapacity}.";
            return false;
        }

        slot = new LoadoutSlot(
            verbId,
            Modifier: modifiers.Length == 0 ? null : modifiers[0],
            Modifier2: modifiers.Length < 2 ? null : modifiers[1]);
        message = $"Attuned {ExpressionName(slot)} ({load}/{loadCapacity} Load at Heartbeat {state.Tick}).";
        return true;
    }

    internal static ChronicleState ApplyEngagement(
        ChronicleState state,
        ICollection<CombatResultSnapshot>? results = null)
    {
        if (!IsAvailable(state) || !IsImmediateDanger(state))
        {
            return state;
        }

        var combat = state.Combat!;
        if (combat.EngagementActive)
        {
            return state;
        }

        var updated = state with
        {
            Speed = ChronicleSpeed.Paused,
            Combat = combat with
            {
                EngagementActive = true,
                WeaponStanceActive = combat.EngagementPlan.OpenWithWeaponStance,
                WeaponTicksUntilReady = 0,
            },
        };
        results?.Add(Result(
            updated,
            CombatResultKind.Engagement,
            combat.EngagementPlan.OpenWithWeaponStance
                ? "Mire Brute contact: Iron Cleaver stance readied; Chronicle paused."
                : "Mire Brute contact: Chronicle paused before the first hostile Heartbeat."));
        return updated;
    }

    internal static ChronicleState EndIncarnation(
        ChronicleState state,
        IMaterialCommitments material)
    {
        if (!IsAvailable(state))
        {
            return state with
            {
                IncarnationLife = IncarnationLifeState.AwaitingReplacement,
                Study = state.Study.Stop(),
            };
        }

        state = material.EndIncarnation(state);
        var combat = state.Combat!;
        return state with
        {
            Speed = ChronicleSpeed.Paused,
            IncarnationLife = IncarnationLifeState.AwaitingReplacement,
            Study = state.Study.Stop(),
            Loadout = state.WorldGrammarVersion is 5 or 6 ? LoadoutState.Empty : state.Loadout,
            Attunement = state.WorldGrammarVersion is 5 or 6 ? null : state.Attunement,
            Combat = combat with
            {
                IncarnationHitPoints = 0,
                WeaponStanceActive = false,
                PendingAction = null,
                Preparation = null,
                RecoveryRemaining = 0,
                EngagementActive = false,
            },
        };
    }

    internal static ChronicleState CreateReplacement(ChronicleState state)
    {
        if (!IsAvailable(state) || state.IncarnationLife != IncarnationLifeState.AwaitingReplacement)
        {
            return state;
        }

        if (state.IncarnationId >= long.MaxValue - 1)
        {
            return state;
        }

        var combat = state.Combat!;
        return state with
        {
            Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
            Loadout = LoadoutState.Empty,
            Attunement = state.WorldGrammarVersion is 5 or 6 ? null : state.Attunement,
            IncarnationId = checked(state.IncarnationId + 1),
            IncarnationLife = IncarnationLifeState.Alive,
            Study = state.Study.Stop(),
            Speed = ChronicleSpeed.Paused,
            Combat = combat with
            {
                IncarnationHitPoints = combat.MaximumHitPoints,
                WeaponStanceActive = false,
                WeaponTicksUntilReady = 0,
                PendingAction = null,
                Preparation = null,
                RecoveryRemaining = 0,
                EngagementActive = false,
            },
        };
    }

    private static bool TryReleaseBurn(
        ChronicleState state,
        BurnPreparationState preparation,
        out ChronicleState updated,
        out string message)
    {
        updated = state;
        var combat = state.Combat!;
        if (!state.HasLivingIncarnation || preparation.ActorIncarnationId != state.IncarnationId)
        {
            updated = state with { Combat = combat with { Preparation = null } };
            message = "Burn preparation ended because its Incarnation is no longer present.";
            return false;
        }

        if (combat.RecoveryRemaining > 0 || ActiveExpression(state) != preparation.Expression)
        {
            updated = state with { Combat = combat with { Preparation = null } };
            message = "Burn preparation ended because the acting Expression state changed.";
            return false;
        }

        var brute = combat.MireBrute;
        if (!brute.IsLiving || !string.Equals(brute.Identity, preparation.TargetIdentity, StringComparison.Ordinal))
        {
            updated = state with { Combat = combat with { Preparation = null } };
            message = "Burn preparation ended because its Target is no longer a living Mire Brute.";
            return false;
        }

        if (CardinalDistance(state.Address, brute.Address) > CombatState.BurnRange ||
            !string.Equals(state.Address.Stratum, brute.Address.Stratum, StringComparison.Ordinal))
        {
            updated = state with { Combat = combat with { Preparation = null } };
            message = "Burn preparation ended because the Mire Brute left Burn range.";
            return false;
        }

        var duration = ConsequenceFor(preparation.Expression);
        updated = state with
        {
            Combat = combat with
            {
                Preparation = null,
                OngoingBurn = new BurnConsequenceState(
                    brute.Identity,
                    DamageFor(preparation.Expression),
                    duration),
                RecoveryRemaining = RecoveryFor(preparation.Expression),
                Scorch = combat.Scorch ?? new ScorchedGroundState(brute.Address, state.Tick),
            },
        };
        message = $"{ExpressionName(preparation.Expression)} releases against the Mire Brute.";
        return true;
    }

    private static (ChronicleState State, bool BruteDied) ApplyBurnTick(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results)
    {
        var combat = state.Combat!;
        var burning = combat.OngoingBurn!;
        var brute = combat.MireBrute;
        if (!brute.IsLiving || !string.Equals(brute.Identity, burning.TargetIdentity, StringComparison.Ordinal))
        {
            return (state with { Combat = combat with { OngoingBurn = null } }, false);
        }

        var hitPoints = Math.Max(0, brute.HitPoints - burning.Damage);
        var updatedBrute = brute with
        {
            HitPoints = hitPoints,
            DefeatedTick = hitPoints == 0 ? state.Tick : brute.DefeatedTick,
        };
        var remaining = burning.RemainingTicks - 1;
        var updated = state with
        {
            Combat = combat with
            {
                MireBrute = updatedBrute,
                OngoingBurn = remaining > 0
                    ? burning with { RemainingTicks = remaining }
                    : null,
            },
        };
        results.Add(Result(
            updated,
            CombatResultKind.BurnDamage,
            $"Burn deals {burning.Damage} fire damage.",
            brute.Address,
            burning.Damage));
        if (hitPoints > 0)
        {
            return (updated, false);
        }

        results.Add(Result(updated, CombatResultKind.MireBruteDied, "The Mire Brute dies.", brute.Address));
        return (updated, true);
    }

    private static (ChronicleState State, bool BruteDied) ApplyWeapon(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results)
    {
        var combat = state.Combat!;
        var brute = combat.MireBrute;
        if (!combat.WeaponStanceActive ||
            combat.Preparation is not null ||
            !brute.IsLiving ||
            !AreAdjacent(state.Address, brute.Address))
        {
            return (state, false);
        }

        if (combat.WeaponTicksUntilReady > 0)
        {
            return (state with
            {
                Combat = combat with { WeaponTicksUntilReady = combat.WeaponTicksUntilReady - 1 },
            }, false);
        }

        var hitPoints = Math.Max(0, brute.HitPoints - CombatState.IronCleaverDamage);
        var updatedBrute = brute with
        {
            HitPoints = hitPoints,
            DefeatedTick = hitPoints == 0 ? state.Tick : brute.DefeatedTick,
        };
        var updated = state with
        {
            Combat = combat with
            {
                MireBrute = updatedBrute,
                WeaponTicksUntilReady = CombatState.IronCleaverCadence - 1,
                OngoingBurn = hitPoints == 0 ? null : combat.OngoingBurn,
            },
        };
        results.Add(Result(
            updated,
            CombatResultKind.WeaponStrike,
            "Iron Cleaver deals 5 physical damage.",
            brute.Address,
            CombatState.IronCleaverDamage));
        if (hitPoints > 0)
        {
            return (updated, false);
        }

        results.Add(Result(updated, CombatResultKind.MireBruteDied, "The Mire Brute dies.", brute.Address));
        return (updated, true);
    }

    private static (ChronicleState State, bool PreparationInterrupted) ApplyMireBrute(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results,
        bool preparationInterrupted,
        IMaterialCommitments material)
    {
        var combat = state.Combat!;
        var brute = combat.MireBrute;
        if (!AreAdjacent(state.Address, brute.Address))
        {
            var destination = StepToward(brute.Address, state.Address);
            var pursued = state with
            {
                Combat = combat with { MireBrute = brute with { Address = destination } },
            };
            results.Add(Result(pursued, CombatResultKind.MireBruteMove, "The Mire Brute pursues.", destination));
            return (pursued, preparationInterrupted);
        }

        if (brute.SwingTicksRemaining > 1)
        {
            return (state with
            {
                Combat = combat with
                {
                    MireBrute = brute with { SwingTicksRemaining = brute.SwingTicksRemaining - 1 },
                },
            }, preparationInterrupted);
        }

        var damage = Math.Max(0, CombatState.MireBruteSwingDamage - combat.Equipment.PhysicalDamageReduction);
        var hitPoints = Math.Max(0, combat.IncarnationHitPoints - damage);
        var updatedCombat = combat with
        {
            IncarnationHitPoints = hitPoints,
            MireBrute = brute with { SwingTicksRemaining = CombatState.MireBruteSwingCadence },
        };
        var updated = state with { Combat = updatedCombat };
        results.Add(Result(
            updated,
            CombatResultKind.MireBruteSwing,
            $"Mire Brute swing deals {damage} physical damage after Quilted Jack.",
            state.Address,
            damage));
        updated = material.InterruptAfterHostileDamage(updated, results);
        updatedCombat = updated.Combat!;

        if (hitPoints == 0)
        {
            updated = EndIncarnation(updated, material);
            results.Add(Result(updated, CombatResultKind.IncarnationDied, "The Incarnation dies; the Chronicle pauses."));
            return (updated, true);
        }

        if (updatedCombat.Preparation is not null)
        {
            updated = updated with
            {
                Combat = updated.Combat! with { Preparation = null },
            };
            results.Add(Result(
                updated,
                CombatResultKind.PreparationInterrupted,
                "The Mire Brute interrupts Burn preparation."));
            return (updated, true);
        }

        return (updated, preparationInterrupted);
    }

    private static ChronicleState AdvanceRecovery(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results)
    {
        var combat = state.Combat!;
        if (combat.RecoveryRemaining <= 0)
        {
            return state;
        }

        var remaining = combat.RecoveryRemaining - 1;
        var updated = state with { Combat = combat with { RecoveryRemaining = remaining } };
        if (remaining == 0)
        {
            results.Add(Result(updated, CombatResultKind.RecoveryComplete, "Burn Recovery completes."));
        }

        return updated;
    }

    private static TargetPreviewSnapshot MissingTarget(WorldAddress address, string reason) =>
        new(
            CombatTargetKind.Missing,
            null,
            "No Target",
            address,
            new TargetFactsSnapshot(false, false, false, false, false, "unknown", "unknown"),
            0,
            null,
            null,
            false,
            reason,
            0,
            0,
            0,
            false);

    private static bool IsCurrentTarget(ChronicleState state, string identity) =>
        state.Combat?.Preparation?.TargetIdentity == identity ||
        state.Combat?.PendingAction is { Kind: TacticalActionKind.PrepareBurn, Target: not null } pending &&
        PreviewTargetIdentity(state, pending.Target!.Value) == identity;

    private static string? PreviewTargetIdentity(ChronicleState state, WorldAddress address) =>
        address == state.Combat?.MireBrute.Address
            ? state.Combat.MireBrute.Identity
            : address == WorldArea.GeneratedBasaltAddress(state.Seed)
                ? WorldArea.GeneratedBasaltIdentity(state.Seed)
                : null;

    private static LoadoutSlot ActiveExpression(ChronicleState state) =>
        state.ActiveLoadout.Slots.FirstOrDefault(slot => slot.Verb is not null && slot.Noun is null);

    private static ExpressionSnapshot ExpressionSnapshot(ChronicleState state)
    {
        var slot = ActiveExpression(state);
        var modifiers = slot.Modifiers;
        var load = slot.Verb is { } verb
            ? WordCatalogue.Get(verb).Load + modifiers.Sum(id => WordCatalogue.Get(id).Load)
            : 0;
        return new ExpressionSnapshot(
            slot.Verb,
            Array.AsReadOnly(modifiers.ToArray()),
            load,
            state.Attunement?.Capacity ?? HoldingFacts.NextAttunementCapacity(state),
            slot.IsEmpty ? 0 : 1 + modifiers.Count,
            HoldingFacts.LinkCapacityFor(state),
            CombatState.ActiveVerbSlots,
            slot.IsEmpty ? "No Expression attuned" : ExpressionName(slot));
    }

    private static ExpressionSnapshot EmptyExpression() => new(
        null,
        [],
        0,
        CombatState.SharedLoadCapacity,
        0,
        CombatState.LinkCapacity,
        CombatState.ActiveVerbSlots,
        "No Expression attuned");

    private static PendingActionSnapshot? PendingSnapshot(TacticalActionState? pending) => pending switch
    {
        null => null,
        { Kind: TacticalActionKind.Move } => new PendingActionSnapshot(
            TacticalActionKind.Move,
            "Move",
            null,
            pending.DeltaX,
            pending.DeltaY,
            null),
        { Kind: TacticalActionKind.SetWeaponStance } => new PendingActionSnapshot(
            TacticalActionKind.SetWeaponStance,
            pending.WeaponStanceActive ? "Ready Iron Cleaver" : "Lower Iron Cleaver",
            null,
            null,
            null,
            pending.WeaponStanceActive),
        { Kind: TacticalActionKind.PrepareBurn } => new PendingActionSnapshot(
            TacticalActionKind.PrepareBurn,
            "Prepare Burn",
            pending.Target,
            null,
            null,
            null),
        _ => throw new InvalidOperationException($"Unknown tactical action '{pending.Kind}'."),
    };

    private static PreparationSnapshot? PreparationSnapshot(BurnPreparationState? preparation) => preparation is { } value
        ? new PreparationSnapshot(
            ExpressionName(value.Expression),
            value.TargetIdentity,
            value.TargetAddressAtPreparation,
            value.RemainingTicks,
            PreparationFor(value.Expression),
            "A Mire Brute swing interrupts Preparation before release.")
        : null;

    private static int PreparationFor(LoadoutSlot expression) =>
        WordEffects.Compose(expression).Preparation;

    private static int ConsequenceFor(LoadoutSlot expression) =>
        WordEffects.Compose(expression).Consequence;

    private static int RecoveryFor(LoadoutSlot expression) =>
        WordEffects.Compose(expression).Recovery;

    private static int DamageFor(LoadoutSlot expression) =>
        WordEffects.Compose(expression).Damage;

    private static string ExpressionName(LoadoutSlot expression)
    {
        if (expression.Verb is not { } verb)
        {
            return "Expression";
        }

        return string.Join(
            " + ",
            new[] { WordCatalogue.Get(verb).DisplayName }
                .Concat(expression.Modifiers.Select(id => WordCatalogue.Get(id).DisplayName)));
    }

    private static CombatResultSnapshot Result(
        ChronicleState state,
        CombatResultKind kind,
        string text,
        WorldAddress? address = null,
        int? damage = null) =>
        new(state.Tick, kind, text, damage, address);

    private static CombatForecastEventSnapshot? ToForecast(CombatResultSnapshot result) => result.Kind switch
    {
        CombatResultKind.Movement or CombatResultKind.Stance or CombatResultKind.PreparationStarted =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.PendingAction, result.Text, result.Damage, result.Address),
        CombatResultKind.InvocationReleased =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.BurnRelease, result.Text, result.Damage, result.Address),
        CombatResultKind.BurnDamage =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.BurnDamage, result.Text, result.Damage, result.Address),
        CombatResultKind.WeaponStrike =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.WeaponStrike, result.Text, result.Damage, result.Address),
        CombatResultKind.MireBruteMove =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.MireBruteMove, result.Text, result.Damage, result.Address),
        CombatResultKind.MireBruteSwing =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.MireBruteSwing, result.Text, result.Damage, result.Address),
        CombatResultKind.RecoveryComplete =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.RecoveryComplete, result.Text, result.Damage, result.Address),
        CombatResultKind.Engagement =>
            new CombatForecastEventSnapshot(result.Tick, CombatForecastKind.Engagement, result.Text, result.Damage, result.Address),
        _ => null,
    };

    private static WorldAddress StepToward(WorldAddress origin, WorldAddress toward)
    {
        if (origin.X != toward.X)
        {
            return origin with { X = origin.X < toward.X ? origin.X + 1 : origin.X - 1 };
        }

        return origin with { Y = origin.Y < toward.Y ? origin.Y + 1 : origin.Y - 1 };
    }

    private static bool AreAdjacent(WorldAddress first, WorldAddress second) =>
        string.Equals(first.Stratum, second.Stratum, StringComparison.Ordinal) &&
        CardinalDistance(first, second) == 1;

    internal static bool IsOccupiedByLivingMireBrute(ChronicleState state, WorldAddress address) =>
        IsAvailable(state) &&
        state.Combat!.MireBrute.IsLiving &&
        state.Combat.MireBrute.Address == address;

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
}
