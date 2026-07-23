using Chronicle.Core;

/// <summary>
/// Owns every player-facing sentence for the bounded Agent surface. Core
/// supplies identity, need, relationship, intent, timing, and blockers only.
/// </summary>
internal static class AgentPresentation
{
    internal static string Banner(AgentSnapshot agent) => agent.Presence switch
    {
        AgentPresenceState.ApproachingHome => "SOMEONE APPROACHES HOME",
        AgentPresenceState.WaitingAtHome when
            agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered =>
            "WELCOME OFFER · ANSWER PENDING",
        AgentPresenceState.WaitingAtHome => "SOMEONE WAITS AT HOME",
        AgentPresenceState.AtHome => "HOME REMEMBERS ITS GUEST",
        _ => "SOMEONE IS HERE",
    };

    internal static string Checklist(AgentSnapshot agent, bool paused)
    {
        var lines = agent.Presence switch
        {
            AgentPresenceState.ApproachingHome => new[]
            {
                $"CHECKLIST · {agent.DisplayName.ToUpperInvariant()} APPROACHES",
                $"[ ] NEXT · steps toward Home at {agent.WaitingAddress.X},{agent.WaitingAddress.Y}",
                $"[ ] WHEN · active Heartbeat H{agent.NextHeartbeat}",
                agent.Blocker == AgentBlockerKind.None
                    ? "[ ] INTERRUPTS · pause or a blocked next cell"
                    : $"[!] BLOCKED · {Blocker(agent.Blocker)} at {agent.NextAddress}",
                $"[ ] PREVENTS · nothing; {agent.DisplayName} chooses the route",
            },
            AgentPresenceState.WaitingAtHome when
                agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered => new[]
            {
                "CHECKLIST · WELCOME OFFER OPEN",
                $"[ ] NEXT · {agent.DisplayName} decides whether to stay",
                $"[ ] WHEN · active Heartbeat H{agent.NextHeartbeat}",
                "[ ] INTERRUPTS · P withdraws; death or leaving reach cancels",
                paused ? "[ ] SPACE · resume; another welcome is blocked" : "[ ] PREVENTS · another open welcome",
            },
            AgentPresenceState.WaitingAtHome => new[]
            {
                $"CHECKLIST · {agent.DisplayName.ToUpperInvariant()} WAITS",
                "[x] ARRIVED · west of the Hearth",
                "[ ] P · offer welcome from this cell or beside it",
                "[ ] NEXT · waits until you decide",
                "[ ] COST · Guest only; no job, command, or obedience",
            },
            AgentPresenceState.AtHome => new[]
            {
                $"CHECKLIST · {agent.DisplayName.ToUpperInvariant()} AT HOME",
                "[x] REFUGE · satisfied",
                $"[x] RELATIONSHIP · Guest since H{agent.HomeRelationship.EstablishedTick}",
                $"[x] ROAD-ROLL · {agent.RoadRollAddress}",
                $"[x] COMMAND · none; {agent.DisplayName} keeps their own agency",
            },
            _ => ["CHECKLIST · AGENT STATE UNKNOWN"],
        };
        return string.Join('\n', lines.Take(5));
    }

    internal static string Heading(AgentSnapshot agent) =>
        $"{agent.DisplayName.ToUpperInvariant()} · {Relationship(agent.HomeRelationship.Kind).ToUpperInvariant()}";

    internal static string Facts(AgentSnapshot agent) =>
        $"NEEDS · Refuge — {NeedStatus(agent.Need.Status)}\n" +
        "WHY HERE · Followed your Resonant Lode from the emptied Seam\n" +
        $"ORIGIN · {agent.OriginAddress}";

    internal static string Decision(AgentSnapshot agent, bool paused) => agent switch
    {
        { Presence: AgentPresenceState.ApproachingHome } =>
            $"STATE  NEXT: one step toward Home{BlockedSuffix(agent)}\n" +
            $"WHEN  Active Heartbeat H{agent.NextHeartbeat}\n" +
            "INTERRUPTS  Pause or a blocked next cell\n" +
            $"PREVENTS  Nothing; movement is {agent.DisplayName}'s",
        { HomeRelationship.Kind: AgentHomeRelationshipKind.WelcomeOffered } =>
            $"STATE  NEXT: {agent.DisplayName} considers a place at Home\n" +
            $"WHEN  Active Heartbeat H{agent.NextHeartbeat}{(paused ? " · SPACE resumes" : string.Empty)}\n" +
            "INTERRUPTS  P withdraws; death or leaving reach cancels\n" +
            "PREVENTS  A second welcome decision",
        { Presence: AgentPresenceState.WaitingAtHome } =>
            "STATE  NEXT: P offers a persistent Guest place\n" +
            "WHEN  Answer on the next active Heartbeat\n" +
            "INTERRUPTS  Withdraw, death, or leave physical reach\n" +
            "PREVENTS  One other open welcome; grants no command",
        { Presence: AgentPresenceState.AtHome } =>
            $"STATE  NEXT: {agent.DisplayName} remains a Guest at Home\n" +
            $"WHEN  Relationship persists from H{agent.HomeRelationship.EstablishedTick}\n" +
            "INTERRUPTS  No Goal 7A dismissal or command exists\n" +
            "PREVENTS  Nothing; Guest is not a worker or follower",
        _ => string.Empty,
    };

    internal static IReadOnlyList<string> Forecast(AgentSnapshot agent, bool paused) => agent switch
    {
        { Presence: AgentPresenceState.ApproachingHome, Blocker: not AgentBlockerKind.None } =>
        [
            $"H{agent.NextHeartbeat} · BLOCKED by {Blocker(agent.Blocker)}",
            $"NEXT CELL · {agent.NextAddress}",
        ],
        { Presence: AgentPresenceState.ApproachingHome } =>
        [
            paused
                ? $"H{agent.NextHeartbeat} · PAUSED; no step"
                : $"H{agent.NextHeartbeat} · steps to {agent.NextAddress}",
        ],
        { HomeRelationship.Kind: AgentHomeRelationshipKind.WelcomeOffered } =>
        [
            paused
                ? $"H{agent.NextHeartbeat} · PAUSED; answer waits"
                : $"H{agent.NextHeartbeat} · {agent.DisplayName} answers",
        ],
        { Presence: AgentPresenceState.WaitingAtHome } =>
            [$"NO TIMER · {agent.DisplayName} waits for your decision"],
        { Presence: AgentPresenceState.AtHome } => ["PERSISTENT · Guest and road-roll remain at Home"],
        _ => [],
    };

    internal static string ActionLabel(AgentSnapshot agent) =>
        agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered
            ? "WITHDRAW WELCOME"
            : "OFFER WELCOME";

    internal static string ActionDetail(AgentActionSnapshot action) => action.AvailabilityReason switch
    {
        AgentActionAvailabilityReason.Available when action.Kind == AgentActionKind.OfferWelcome =>
            $"ANSWERS H{action.ResolvesAtTick}",
        AgentActionAvailabilityReason.Available => "NO HEARTBEAT",
        AgentActionAvailabilityReason.LivingIncarnationRequired => "NEW BODY REQUIRED",
        AgentActionAvailabilityReason.IncarnationMustBeAtHome => "RETURN TO HOME",
        AgentActionAvailabilityReason.AgentMustBeWaiting => "WAIT FOR ARRIVAL",
        AgentActionAvailabilityReason.AgentOutOfReach => "STAND ON OR BESIDE TAMAR",
        AgentActionAvailabilityReason.AnotherWelcomeIsOpen => "ONE OFFER ALREADY OPEN",
        AgentActionAvailabilityReason.NoWelcomeIsOpen => "NO OFFER TO WITHDRAW",
        AgentActionAvailabilityReason.AlreadyGuest => "ALREADY A GUEST",
        _ => "UNAVAILABLE",
    };

    internal static string ActionUnavailable(
        AgentSnapshot agent,
        AgentActionSnapshot action) => action.AvailabilityReason switch
    {
        AgentActionAvailabilityReason.LivingIncarnationRequired => "A living Incarnation must offer welcome.",
        AgentActionAvailabilityReason.IncarnationMustBeAtHome => "Return to Home before offering welcome.",
        AgentActionAvailabilityReason.AgentMustBeWaiting =>
            $"{agent.DisplayName} must reach the waiting place first.",
        AgentActionAvailabilityReason.AgentOutOfReach =>
            $"Stand on or cardinally beside {agent.DisplayName}.",
        AgentActionAvailabilityReason.AnotherWelcomeIsOpen => "Only one welcome decision may be open.",
        AgentActionAvailabilityReason.NoWelcomeIsOpen => "There is no welcome to withdraw.",
        AgentActionAvailabilityReason.AlreadyGuest => $"{agent.DisplayName} is already a Guest at Home.",
        _ => "No contextual Agent action is available.",
    };

    internal static string Log(AgentEventSnapshot item, string displayName) => item.Kind switch
    {
        AgentEventKind.Promoted => $"H{item.Tick}: {displayName} follows the Resonant Lode toward Home.",
        AgentEventKind.Moved => $"H{item.Tick}: {displayName} steps to {item.Address}.",
        AgentEventKind.Blocked => $"H{item.Tick}: {displayName} is blocked by {Blocker(item.Blocker)} at {item.Address}.",
        AgentEventKind.Arrived => $"H{item.Tick}: {displayName} arrives at Home and waits.",
        AgentEventKind.WelcomeOffered => $"H{item.Tick}: Welcome offered; {displayName} answers next active Heartbeat.",
        AgentEventKind.WelcomeWithdrawn => $"H{item.Tick}: Welcome withdrawn before {displayName} answered.",
        AgentEventKind.WelcomeAccepted => $"H{item.Tick}: {displayName} accepts Refuge as Home's Guest; road-roll placed.",
        AgentEventKind.WelcomeInterrupted => $"H{item.Tick}: {displayName}'s pending welcome was interrupted.",
        _ => $"H{item.Tick}: {displayName}'s state changed.",
    };

    internal static string ReplacementStatus(AgentSnapshot? agent) => agent is null
        ? "BODY ENDED · CHRONICLE HELD\nChoose NEW BODY below."
        : $"BODY ENDED · HOME REMEMBERS\n{agent.DisplayName} remains Home's " +
          $"{Relationship(agent.HomeRelationship.Kind)}. Choose NEW BODY, then return physically.";

    private static string NeedStatus(AgentNeedStatus status) => status switch
    {
        AgentNeedStatus.Seeking => "seeking",
        AgentNeedStatus.Offered => "place offered",
        AgentNeedStatus.Satisfied => "satisfied",
        _ => "unknown",
    };

    private static string Relationship(AgentHomeRelationshipKind relationship) => relationship switch
    {
        AgentHomeRelationshipKind.Unfamiliar => "Stranger",
        AgentHomeRelationshipKind.WelcomeOffered => "Welcome offered",
        AgentHomeRelationshipKind.Guest => "Guest",
        _ => "Unknown",
    };

    private static string BlockedSuffix(AgentSnapshot agent) => agent.Blocker == AgentBlockerKind.None
        ? string.Empty
        : $"; blocked by {Blocker(agent.Blocker)}";

    private static string Blocker(AgentBlockerKind blocker) => blocker switch
    {
        AgentBlockerKind.Incarnation => "the Incarnation",
        AgentBlockerKind.Creature => "a creature",
        AgentBlockerKind.PhysicalSubject => "a physical subject",
        AgentBlockerKind.Agent => "another person",
        _ => "nothing",
    };
}
