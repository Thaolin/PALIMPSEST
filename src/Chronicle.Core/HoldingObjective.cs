namespace Chronicle.Core;

/// <summary>
/// The one material objective a Chronicle currently offers. Core states the
/// facts; presentation owns every sentence, every key label, and every glyph.
/// </summary>
public enum HoldingObjectiveKind
{
    ReturnTheLode = 1,
    Commitment = 2,
    LearnBurn = 3,
    GetTheLode = 4,
    LiftTheLode = 5,
    CarryLodeHome = 6,
    FinishConstruction = 7,
    UseNewLoad = 8,
    TestSourceLoss = 9,
    FinishDismantling = 10,
    RebuildPower = 11,
    FinishRebuild = 12,
}

/// <summary>
/// A durable Chronicle subject an objective step refers to.
/// </summary>
public enum HoldingSubject
{
    None = 0,
    BurnPrimer = 1,
    SingingSeam = 2,
    ResonantLode = 3,
    Home = 4,
    ResonatorSite = 5,
    HearthResonator = 6,
    DestroyedHearthResonator = 7,
}

/// <summary>
/// The physical act an objective invites next.
/// </summary>
public enum HoldingActionKind
{
    None = 0,
    Read = 1,
    Extract = 2,
    Lift = 3,
    Build = 4,
    ResumeBuild = 5,
    Dismantle = 6,
    Destroy = 7,
    Rebuild = 8,
    ResumeRebuild = 9,
    Attune = 10,
    AdvanceHeartbeat = 11,
}

/// <summary>
/// The next material change an objective produces.
/// </summary>
public enum HoldingOutcome
{
    None = 0,
    BurnWordsEnterCodex = 1,
    ExtractionProgressRemains = 2,
    LodeLooseAndSeamEmpty = 3,
    ConstructionAdvances = 4,
    ResonatorIntactOffersFourNext = 5,
    ResonatorDamagedStillContributes = 6,
    ResonatorDestroyedNextFallsToInherent = 7,
    RebuildAdvances = 8,
    ResonatorIntactRestoresFullNext = 9,
    LoadoutChangesAtAttunement = 10,
    DamagedThenDestroyedNextFallsToInherent = 11,
    WorkAdvances = 12,
    LodeCarried = 13,
    LodeSetDown = 14,
}

/// <summary>
/// A material fact already established when the objective is offered.
/// </summary>
public enum HoldingEstablishedFact
{
    None = 0,
    CurrentLoadoutSurvivesSourceLoss = 1,
    SourceContributesAtNextAttunement = 2,
    SourceDamagedStillContributes = 3,
    SourceDestroyedNextIsInherent = 4,
}

/// <summary>
/// What can stop the objective, and what it forbids while it runs.
/// </summary>
public enum HoldingConstraint
{
    NothingStopsOrLocks = 1,
    HostileInterruptionKeepsProgress = 2,
    HostileInterruptionKeepsWorkProgress = 3,
    LocksAllOtherActionsWhileActive = 4,
    CarryingLocksWeaponInvocationFlightAttunement = 5,
    BlockedByCarryingWorkOrDanger = 6,
    LocksMovementFightInvocationAttunement = 7,
}

/// <summary>
/// One relative journey, expressed as Chronicle geometry rather than as text.
/// </summary>
public readonly record struct HoldingOffsetSnapshot(
    bool SameStratum,
    string Stratum,
    long DeltaX,
    long DeltaY);

/// <summary>
/// The structured objective the HUD renders as a checklist.
/// </summary>
public sealed record HoldingObjectiveSnapshot(
    HoldingObjectiveKind Kind,
    HoldingSubject TravelSubject,
    bool TravelSubjectLocated,
    bool TravelSubjectInReach,
    HoldingOffsetSnapshot? TravelOffset,
    HoldingActionKind Action,
    int ActionHeartbeats,
    HoldingEstablishedFact EstablishedFact,
    HoldingOutcome NextOutcome,
    bool ShowsCarryHomeStep,
    int CommitmentCompletedTicks,
    int CommitmentTotalTicks,
    bool WaitingForHeartbeat,
    long NextTick,
    IReadOnlyList<HoldingConstraint> Constraints);
