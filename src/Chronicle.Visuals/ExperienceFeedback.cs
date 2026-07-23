using Chronicle.Core;

namespace Chronicle.Visuals;

public enum FeedbackTimingBand
{
    Routine,
    DecisionConsequence,
    ChronicleConsequence,
}

public enum SemanticCueFamily
{
    Movement,
    CleaverContact,
    ArmorAnswer,
    BurnPreparation,
    BurnRelease,
    BurnImpact,
    BurnConsequence,
    LodeExtraction,
    LodeLift,
    LodeBurden,
    LodeSetDown,
    ResonatorConstruction,
    ResonatorDamage,
    ResonatorDestruction,
    ResonatorRebuild,
    WordDiscovery,
    AttunementSuccess,
    AttunementRejected,
    TamarArrival,
    TamarWelcome,
    TamarSuggestAccepted,
    TamarCommandRefused,
}

public enum CausalFeedbackRole
{
    Selection,
    Origin,
    Contact,
    Recipient,
    PersistentConsequence,
}

public sealed record ExperienceFeedbackPlan(
    FeedbackTimingBand Band,
    int MaximumMilliseconds,
    CausalFeedbackRole Role,
    WorldAddress Origin,
    WorldAddress Recipient,
    IReadOnlyList<SemanticCueFamily> Cues,
    bool TravelsInFullMotion,
    bool EmphasizesCellsInReducedMotion);

/// <summary>
/// Bounded semantic feedback over accepted commands. It changes no rule and
/// never delays Chronicle time.
/// </summary>
public static class ExperienceFeedback
{
    public const int RoutineMilliseconds = 120;
    public const int DecisionMilliseconds = 300;
    public const int ChronicleMilliseconds = 450;

    public static ExperienceFeedbackPlan? ForCommand(
        ChronicleCommand command,
        ChronicleState before,
        ChronicleState after,
        bool applied)
    {
        if (!applied)
        {
            return command is AttuneExpression
                ? Plan(
                    FeedbackTimingBand.DecisionConsequence,
                    CausalFeedbackRole.Origin,
                    before.Address,
                    before.Address,
                    [SemanticCueFamily.AttunementRejected])
                : null;
        }

        return command switch
        {
            MoveIncarnation => Plan(
                FeedbackTimingBand.Routine,
                CausalFeedbackRole.Origin,
                before.Address,
                after.Address,
                after.PowerHome?.Lode.Disposition == ResonantLodeDisposition.Carried
                    ? [SemanticCueFamily.Movement, SemanticCueFamily.LodeBurden]
                    : [SemanticCueFamily.Movement]),
            SetWeaponStance { Active: true } => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.Contact,
                before.Address,
                after.Combat?.MireBrute.Address ?? after.Address,
                [SemanticCueFamily.CleaverContact, SemanticCueFamily.ArmorAnswer]),
            PrepareBurn burn => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.Recipient,
                before.Address,
                burn.Target,
                [SemanticCueFamily.BurnPreparation]),
            ReadBurnPrimer => Plan(
                FeedbackTimingBand.ChronicleConsequence,
                CausalFeedbackRole.PersistentConsequence,
                before.Address,
                before.Address,
                [SemanticCueFamily.WordDiscovery]),
            AttuneExpression => Plan(
                FeedbackTimingBand.ChronicleConsequence,
                CausalFeedbackRole.Origin,
                before.Address,
                before.Address,
                [SemanticCueFamily.AttunementSuccess]),
            LiftResonantLode => Plan(
                FeedbackTimingBand.Routine,
                CausalFeedbackRole.Origin,
                before.PowerHome?.Lode.Address ?? before.Address,
                after.Address,
                [SemanticCueFamily.LodeLift, SemanticCueFamily.LodeBurden]),
            SetDownResonantLode => Plan(
                FeedbackTimingBand.Routine,
                CausalFeedbackRole.Recipient,
                before.Address,
                after.PowerHome?.Lode.Address ?? after.Address,
                [SemanticCueFamily.LodeSetDown]),
            BeginPowerCommitment work => PowerWork(work.Kind, before, after),
            OfferWelcome => Plan(
                FeedbackTimingBand.ChronicleConsequence,
                CausalFeedbackRole.Recipient,
                before.Address,
                after.Agents.FirstOrDefault()?.Address ?? before.Address,
                [SemanticCueFamily.TamarWelcome]),
            DeliverDirective directive => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.Recipient,
                before.Address,
                after.Agents.Find(directive.AgentIdentity)?.Address ?? before.Address,
                directive.Directive == DirectiveKind.RestByRoadRoll
                    ? [SemanticCueFamily.TamarSuggestAccepted]
                    : [SemanticCueFamily.TamarCommandRefused]),
            _ => null,
        };
    }

    public static ExperienceFeedbackPlan ForResolvedCombat(
        CombatResultSnapshot result,
        WorldAddress actor,
        WorldAddress recipient) =>
        result.Kind switch
        {
            CombatResultKind.InvocationReleased => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.Contact,
                actor,
                recipient,
                [SemanticCueFamily.BurnRelease]),
            CombatResultKind.BurnDamage => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.PersistentConsequence,
                actor,
                recipient,
                [SemanticCueFamily.BurnImpact, SemanticCueFamily.BurnConsequence]),
            CombatResultKind.WeaponStrike => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.Contact,
                actor,
                recipient,
                [SemanticCueFamily.CleaverContact, SemanticCueFamily.ArmorAnswer]),
            _ => Plan(
                FeedbackTimingBand.Routine,
                CausalFeedbackRole.Selection,
                recipient,
                recipient,
                []),
        };

    public static ExperienceFeedbackPlan? ForAgentEvent(
        AgentEventSnapshot result,
        WorldAddress playerAddress) =>
        result.Kind switch
        {
            AgentEventKind.Arrived => Plan(
                FeedbackTimingBand.ChronicleConsequence,
                CausalFeedbackRole.Recipient,
                result.Address,
                playerAddress,
                [SemanticCueFamily.TamarArrival]),
            AgentEventKind.WelcomeAccepted => Plan(
                FeedbackTimingBand.ChronicleConsequence,
                CausalFeedbackRole.PersistentConsequence,
                playerAddress,
                result.Address,
                [SemanticCueFamily.TamarWelcome]),
            _ => null,
        };

    public static ExperienceFeedbackPlan? ForDirectiveEvent(
        DirectiveEventSnapshot result,
        WorldAddress playerAddress,
        WorldAddress agentAddress) =>
        result.Kind switch
        {
            DirectiveEventKind.Accepted => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.Recipient,
                playerAddress,
                agentAddress,
                [SemanticCueFamily.TamarSuggestAccepted]),
            DirectiveEventKind.Refused => Plan(
                FeedbackTimingBand.DecisionConsequence,
                CausalFeedbackRole.Recipient,
                playerAddress,
                agentAddress,
                [SemanticCueFamily.TamarCommandRefused]),
            _ => null,
        };

    private static ExperienceFeedbackPlan PowerWork(
        PowerCommitmentKind kind,
        ChronicleState before,
        ChronicleState after)
    {
        var address = after.PowerHome?.Commitment?.Address ??
                      before.PowerHome?.Commitment?.Address ??
                      before.Address;
        return Plan(
            kind is PowerCommitmentKind.Build or PowerCommitmentKind.Rebuild
                ? FeedbackTimingBand.ChronicleConsequence
                : FeedbackTimingBand.DecisionConsequence,
            CausalFeedbackRole.Recipient,
            before.Address,
            address,
            kind switch
            {
                PowerCommitmentKind.Extract => [SemanticCueFamily.LodeExtraction],
                PowerCommitmentKind.Build => [SemanticCueFamily.ResonatorConstruction],
                PowerCommitmentKind.Dismantle => [SemanticCueFamily.ResonatorDamage],
                PowerCommitmentKind.Rebuild => [SemanticCueFamily.ResonatorRebuild],
                _ => [],
            });
    }

    private static ExperienceFeedbackPlan Plan(
        FeedbackTimingBand band,
        CausalFeedbackRole role,
        WorldAddress origin,
        WorldAddress recipient,
        IReadOnlyList<SemanticCueFamily> cues) =>
        new(
            band,
            band switch
            {
                FeedbackTimingBand.Routine => RoutineMilliseconds,
                FeedbackTimingBand.DecisionConsequence => DecisionMilliseconds,
                _ => ChronicleMilliseconds,
            },
            role,
            origin,
            recipient,
            cues,
            TravelsInFullMotion: origin != recipient,
            EmphasizesCellsInReducedMotion: true);
}
