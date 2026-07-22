namespace Chronicle.Core;

/// <summary>
/// Pure facts about Holdings that both the combat rulebook and the Holding
/// rulebook read. Keeping them here lets <see cref="CombatRules"/> depend on
/// nothing in <see cref="HoldingRules"/>, so the dependency between the two
/// rulebooks runs in exactly one direction.
/// </summary>
internal static class HoldingFacts
{
    internal const int InherentLoadCapacity = 8;
    internal const int SourceLoadContribution = 4;
    internal const int LinkCapacity = 3;

    internal static readonly WorldAddress SingingSeamAddress =
        new(SurfacePatch.SurfaceStratum, 8, 3);

    internal static bool IsAvailable(ChronicleState state) =>
        state.WorldGrammarVersion == 5 && state.PowerHome is not null;

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
        IsAvailable(state) ? LinkCapacity : CombatState.GrammarFourLinkCapacity;

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
}

/// <summary>
/// The material commitment hooks the combat rulebook must run at fixed points
/// in a Heartbeat. <see cref="HoldingRules"/> supplies the only production
/// implementation; the combat rulebook never names it.
/// </summary>
internal interface IMaterialCommitments
{
    PowerAdvanceResult AdvanceAfterTick(ChronicleState state);

    ChronicleState InterruptAfterHostileDamage(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results);

    ChronicleState EndIncarnation(ChronicleState state);
}

/// <summary>
/// A Chronicle without Holdings: every hook is the identity.
/// </summary>
internal sealed class NoMaterialCommitments : IMaterialCommitments
{
    internal static readonly NoMaterialCommitments Instance = new();

    private NoMaterialCommitments()
    {
    }

    public PowerAdvanceResult AdvanceAfterTick(ChronicleState state) =>
        new(state, null, null);

    public ChronicleState InterruptAfterHostileDamage(
        ChronicleState state,
        ICollection<CombatResultSnapshot> results) => state;

    public ChronicleState EndIncarnation(ChronicleState state) => state;
}
