namespace Chronicle.Core;

/// <summary>
/// What a durable Chronicle subject is, independent of any one fixture. World
/// Grammar generates subjects; a cell carries however many it holds. Adding a
/// creature, a Study Source, or a Holding never adds a field to a cell.
/// </summary>
public enum WorldSubjectKind
{
    Creature = 1,
    Target = 2,
    StudySource = 3,
    MaterialSeam = 4,
    LooseMaterial = 5,
    LoadSource = 6,
    ConstructionSite = 7,
    Agent = 8,
    PersonalPlace = 9,
}

/// <summary>
/// A small, authored semantic qualifier. Visual Grammar decides how a mark is
/// drawn; Core never names a pack visual or overlay.
/// </summary>
public enum WorldSubjectMark
{
    Wounded = 1,
    Burning = 2,
    Selected = 3,
}

/// <summary>
/// The one optional bounded quantity a subject exposes to presentation.
/// </summary>
public readonly record struct WorldSubjectProgress
{
    public WorldSubjectProgress(int current, int maximum)
    {
        if (maximum <= 0 || current < 0 || current > maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(current),
                "World subject progress must satisfy 0 <= current <= maximum and maximum > 0.");
        }

        Current = current;
        Maximum = maximum;
    }

    public int Current { get; }

    public int Maximum { get; }
}

/// <summary>
/// One durable semantic subject standing at one World Address.
/// </summary>
public sealed record WorldSubject
{
    public const int MaximumMarks = 4;

    public WorldSubject(
        string identity,
        WorldSubjectKind kind,
        string archetype,
        string condition,
        string displayName = "",
        IReadOnlyList<WorldSubjectMark>? marks = null,
        WorldSubjectProgress? progress = null,
        long? holderIncarnationId = null,
        string? ownerIdentity = null)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("A World subject identity cannot be empty.", nameof(identity));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(archetype) || string.IsNullOrWhiteSpace(condition))
        {
            throw new ArgumentException("A World subject requires an archetype and condition.");
        }

        var boundedMarks = marks?.ToArray() ?? Array.Empty<WorldSubjectMark>();
        if (boundedMarks.Length > MaximumMarks ||
            boundedMarks.Any(mark => !Enum.IsDefined(mark)) ||
            boundedMarks.Distinct().Count() != boundedMarks.Length)
        {
            throw new ArgumentException(
                $"A World subject accepts at most {MaximumMarks} distinct authored marks.",
                nameof(marks));
        }

        Identity = identity;
        Kind = kind;
        Archetype = archetype;
        Condition = condition;
        DisplayName = displayName;
        Marks = Array.AsReadOnly(boundedMarks);
        Progress = progress;
        HolderIncarnationId = holderIncarnationId;
        OwnerIdentity = ownerIdentity;
    }

    public string Identity { get; }

    public WorldSubjectKind Kind { get; }

    public string Archetype { get; }

    public string Condition { get; }

    public string DisplayName { get; }

    public IReadOnlyList<WorldSubjectMark> Marks { get; }

    public WorldSubjectProgress? Progress { get; }

    public long? HolderIncarnationId { get; }

    public string? OwnerIdentity { get; }
}

/// <summary>
/// The authored archetype and condition vocabulary shared by World Grammar,
/// Visual Grammar, and verification.
/// </summary>
public static class WorldSubjects
{
    public const string MireBruteArchetype = "mire-brute";
    public const string BasaltArchetype = "basalt";
    public const string BurnPrimerArchetype = "burn-primer";
    public const string SingingSeamArchetype = "singing-seam";
    public const string ResonantLodeArchetype = "resonant-lode";
    public const string HearthResonatorArchetype = "hearth-resonator";
    public const string HearthResonatorSiteArchetype = "hearth-resonator-site";
    public const string WayfarerListenerArchetype = "wayfarer-listener";
    public const string WayfarerRoadRollArchetype = "wayfarer-road-roll";

    public const string Living = "living";
    public const string Dead = "dead";
    public const string Read = "read";
    public const string Unread = "unread";
    public const string Present = "present";
    public const string Ready = "ready";
    public const string Occupied = "occupied";
    public const string Approaching = "approaching";
    public const string Waiting = "waiting";
    public const string WelcomeOffered = "welcome-offered";
    public const string Guest = "guest";
    public const string Laid = "laid";

    public static string Condition(AgentState agent) => agent switch
    {
        { HomeRelationship.Kind: AgentHomeRelationshipKind.WelcomeOffered } => WelcomeOffered,
        { Presence: AgentPresenceState.ApproachingHome } => Approaching,
        { Presence: AgentPresenceState.WaitingAtHome } => Waiting,
        { Presence: AgentPresenceState.AtHome } => Guest,
        _ => throw new InvalidOperationException(
            $"Unknown Agent presentation state '{agent.Presence}/{agent.HomeRelationship.Kind}'."),
    };

    public static string Condition(SingingSeamVisualState state) => state switch
    {
        SingingSeamVisualState.Embedded => "embedded",
        SingingSeamVisualState.Empty => "empty",
        _ => throw new InvalidOperationException($"Unknown Singing Seam state '{state}'."),
    };

    public static string Condition(ResonantLodeDisposition disposition) => disposition switch
    {
        ResonantLodeDisposition.Embedded => "embedded",
        ResonantLodeDisposition.Loose => "loose",
        ResonantLodeDisposition.Carried => "carried",
        ResonantLodeDisposition.Committed => "committed",
        ResonantLodeDisposition.Installed => "installed",
        _ => throw new InvalidOperationException($"Unknown Resonant Lode state '{disposition}'."),
    };

    public static string Condition(HearthResonatorPhase phase) => phase switch
    {
        HearthResonatorPhase.UnderConstruction => "under-construction",
        HearthResonatorPhase.Intact => "intact",
        HearthResonatorPhase.Damaged => "damaged",
        HearthResonatorPhase.Destroyed => "destroyed",
        HearthResonatorPhase.Rebuilding => "rebuilding",
        _ => throw new InvalidOperationException($"Unknown Hearth Resonator phase '{phase}'."),
    };
}
