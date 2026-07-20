namespace Chronicle.Core;

public static class FirstConflictSubjects
{
    public const string RiverWardSubjectId = "subject.river-ward";
    public const string RiverWardIdentity = "The River-Ward";
    public const string RivenCairnIdentity = "The Riven Cairn";
    public const string ShatteredCairnIdentity = "The Shattered Cairn";
    public const string History =
        "A Stone ward split by the river's old flood rises from the ridge. " +
        "It was built to hold the ford; every living body completes its closing circuit.";
    public const string Warning =
        "The next active Chronicle tick will end this Incarnation unless Smash is prepared.";
}

public enum FirstConflictOutcome
{
    Shattered = 1,
}

public sealed record FirstConflictState(
    string SubjectId,
    WorldAddress Address,
    long ThreatenedTick,
    LoadoutSlot? PendingAction = null,
    FirstConflictOutcome? Outcome = null,
    long? ResolvedTick = null,
    long? ResolvingIncarnationId = null);

public sealed record ConflictContextSnapshot(
    string CairnIdentity,
    string SubjectIdentity,
    string History,
    string Warning,
    WorldAddress Address,
    long ThreatenedTick,
    bool IsSmashPrepared,
    LoadoutSlot? PendingAction,
    FirstConflictOutcome? Outcome,
    long? ResolvedTick,
    long? ResolvingIncarnationId)
{
    public bool IsThreatened => Outcome is null;

    public bool IsResolved => Outcome is not null;
}
