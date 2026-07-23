using Chronicle.Core;

/// <summary>
/// Player-facing language for the bounded Goal 7B social surface. Core owns
/// admissibility and answers; this adapter only explains those facts.
/// </summary>
internal static class DirectivePresentation
{
    internal static string Banner(DirectiveContextSnapshot context) => context.Pending is not null
        ? "DIRECTIVE · CONSIDERATION PENDING"
        : context.Memories.LastOrDefault() is { } memory
            ? $"DIRECTIVE · {Response(memory.Response).ToUpperInvariant()}"
            : "DIRECTIVE · NOT UNIT CONTROL";

    internal static string Heading(AgentSnapshot agent) =>
        $"{agent.DisplayName.ToUpperInvariant()} · GUEST WITH AGENCY";

    internal static string Facts(
        ChronicleState state,
        AgentSnapshot agent,
        DirectiveContextSnapshot context)
    {
        var slot = state.ActiveLoadout[0];
        var active = slot.Verb is { } verb ? WordCatalogue.Get(verb).DisplayName : "none";
        var activeLoad = slot.Verb is { } activeVerb
            ? WordCatalogue.Get(activeVerb).Load.ToString()
            : "—";
        return "RELATIONSHIP · GUEST\n" +
               $"ATTUNED · {active.ToUpperInvariant()} · {activeLoad} LOAD\n" +
               $"DIRECTIVE MEMORY · {context.Memories.Count}";
    }

    internal static string Decision(
        DirectiveContextSnapshot context,
        AgentSnapshot agent,
        bool paused)
    {
        if (context.Pending is { } pending)
        {
            return $"NEXT · {agent.DisplayName} considers {ShortDirective(pending.Directive)}\n" +
                   $"WHEN · active H{pending.ResolvesAtTick}{(paused ? " · SPACE resumes" : string.Empty)}\n" +
                   "INTERRUPTS · P withdraws first\n" +
                   "PREVENTS · another Directive";
        }

        var rest = context.Actions.Single(action => action.Directive == DirectiveKind.RestByRoadRoll);
        var danger = context.Actions.Single(action => action.Directive == DirectiveKind.ApproachMireBrute);
        return $"NEXT · Rest {CompactAvailability(rest)}; Brute {CompactAvailability(danger)}\n" +
               "WHEN · next active H after delivery\n" +
               "INTERRUPTS · P withdraws first\n" +
               "PREVENTS · another Directive";
    }

    internal static string Checklist(
        ChronicleState state,
        DirectiveContextSnapshot context,
        AgentSnapshot agent,
        bool paused)
    {
        if (context.Pending is { } pending)
        {
            return string.Join('\n', new[]
            {
                $"CHECKLIST · {WordName(pending.Verb).ToUpperInvariant()} DELIVERED",
                $"[ ] NEXT · {agent.DisplayName} considers; no obedience promised",
                $"[ ] WHEN · active Heartbeat H{pending.ResolvesAtTick}",
                $"[ ] INTERRUPT · P withdraws{(paused ? "; SPACE resumes" : string.Empty)}",
                "[ ] BLOCKED · another Directive to Tamar",
            });
        }

        if (context.Memories.LastOrDefault() is { } memory)
        {
            return string.Join('\n', new[]
            {
                $"CHECKLIST · {Response(memory.Response).ToUpperInvariant()}",
                $"[x] WHO · {agent.DisplayName} · Incarnation #{memory.IssuingIncarnationId}",
                $"[x] WORD · {WordName(memory.Verb)} · {DirectiveName(memory.Directive)}",
                $"[x] WHY · {Reason(memory)}",
                $"[x] WHEN · H{memory.ResolvedTick} · answer remembered",
            });
        }

        var active = state.ActiveLoadout[0].Verb is { } verb ? WordName(verb) : "none";
        return string.Join('\n', new[]
        {
            "CHECKLIST · CHOOSE ONE DIRECTIVE",
            "[x] CODEX · Suggest + Command",
            $"[x] ATTUNED · {active}",
            "[ ] P · Rest by your road-roll (safe)",
            "[ ] X · Approach the Mire Brute (dangerous)",
        });
    }

    internal static IReadOnlyList<string> Forecast(
        DirectiveContextSnapshot context,
        AgentSnapshot agent,
        bool paused)
    {
        if (context.Pending is { } pending)
        {
            return
            [
                paused
                    ? $"H{pending.ResolvesAtTick} · PAUSED; {agent.DisplayName} has not answered"
                    : $"H{pending.ResolvesAtTick} · {agent.DisplayName} answers once",
                $"ASK · {DirectiveName(pending.Directive)}",
            ];
        }

        if (context.Memories.LastOrDefault() is { } memory)
        {
            return
            [
                $"H{memory.ResolvedTick} · {Response(memory.Response).ToUpperInvariant()} · {Reason(memory)}",
                "NO RETRY · deliver a new Directive to ask again",
            ];
        }

        return ["NO TIMER · select a Directive; delivery itself spends no Heartbeat"];
    }

    internal static string ActionLabel(DirectiveActionSnapshot action) => action.Directive switch
    {
        DirectiveKind.RestByRoadRoll => "REST BY ROAD-ROLL",
        DirectiveKind.ApproachMireBrute => "APPROACH MIRE BRUTE",
        _ => "DIRECTIVE",
    };

    internal static string ActionDetail(DirectiveActionSnapshot action) => action.AvailabilityReason switch
    {
        DirectiveAvailabilityReason.Available =>
            $"{Force(action.ActiveForce)} OK · H{action.ResolvesAtTick}",
        DirectiveAvailabilityReason.RecipientOutOfReach => "MOVE BESIDE TAMAR",
        DirectiveAvailabilityReason.InsufficientForce =>
            $"NEED {Force(action.MinimumForce)}",
        DirectiveAvailabilityReason.AnotherDirectivePending => "ONE ALREADY PENDING",
        DirectiveAvailabilityReason.ObjectiveAlreadySatisfied => "ALREADY AT ROAD-ROLL",
        DirectiveAvailabilityReason.ObjectiveUnavailable => "OBJECTIVE MISSING OR DEAD",
        DirectiveAvailabilityReason.ActiveSocialVerbRequired => "ATTUNE SUGGEST OR COMMAND",
        _ => "UNAVAILABLE",
    };

    internal static string ActionUnavailable(DirectiveActionSnapshot action) =>
        action.AvailabilityReason switch
        {
            DirectiveAvailabilityReason.RecipientOutOfReach =>
                "Delivery requires standing on or cardinally beside Tamar; inspection remains remote.",
            DirectiveAvailabilityReason.InsufficientForce =>
                "This dangerous Directive requires Command. Suggest is insufficient; Tamar has not considered it.",
            DirectiveAvailabilityReason.AnotherDirectivePending =>
                "Tamar can consider only one Directive at a time. Withdraw it or permit the listed Heartbeat.",
            DirectiveAvailabilityReason.ObjectiveAlreadySatisfied =>
                "Tamar is already at the owned road-roll.",
            DirectiveAvailabilityReason.ObjectiveUnavailable =>
                "That objective is missing or no longer living.",
            DirectiveAvailabilityReason.ActiveSocialVerbRequired =>
                "Attune Suggest or Command before delivering a Directive.",
            _ => "That Directive is unavailable right now.",
        };

    internal static string Log(DirectiveEventSnapshot item, string displayName) => item.Kind switch
    {
        DirectiveEventKind.Delivered =>
            $"H{item.Tick}: {WordName(item.Verb)} delivered to {displayName}; answer at H{item.Tick + 1}.",
        DirectiveEventKind.Withdrawn =>
            $"H{item.Tick}: Directive withdrawn before {displayName} answered.",
        DirectiveEventKind.Accepted =>
            $"H{item.Tick}: {displayName} accepts and acts on the request.",
        DirectiveEventKind.Delayed =>
            $"H{item.Tick}: {displayName} delays; {Blocker(item.Blocker)} blocks the way.",
        DirectiveEventKind.Refused =>
            $"H{item.Tick}: {displayName} refuses; Guest status grants no violent commitment.",
        _ => $"H{item.Tick}: {displayName}'s Directive state changed.",
    };

    private static string Preview(DirectiveActionSnapshot action) => action.AvailabilityReason switch
    {
        DirectiveAvailabilityReason.Available =>
            $"AVAILABLE · {Force(action.ActiveForce)} meets {Force(action.MinimumForce)}",
        DirectiveAvailabilityReason.InsufficientForce =>
            $"BLOCKED · needs {Force(action.MinimumForce)}; no consideration",
        DirectiveAvailabilityReason.RecipientOutOfReach => "BLOCKED · stand on or beside Tamar",
        _ => $"BLOCKED · {ActionDetail(action).ToLowerInvariant()}",
    };

    private static string CompactAvailability(DirectiveActionSnapshot action) => action.AvailabilityReason switch
    {
        DirectiveAvailabilityReason.Available => "YES",
        DirectiveAvailabilityReason.InsufficientForce => "NO—COMMAND",
        DirectiveAvailabilityReason.RecipientOutOfReach => "NO—REACH",
        DirectiveAvailabilityReason.ObjectiveAlreadySatisfied => "DONE",
        _ => "NO",
    };

    private static string WordName(WordId verb) => WordCatalogue.Get(verb).DisplayName;

    private static string DirectiveName(DirectiveKind directive) => directive switch
    {
        DirectiveKind.RestByRoadRoll => "Rest by your road-roll",
        DirectiveKind.ApproachMireBrute => "Approach the Mire Brute",
        _ => "Unknown Directive",
    };

    private static string ShortDirective(DirectiveKind directive) => directive switch
    {
        DirectiveKind.RestByRoadRoll => "rest by the road-roll",
        DirectiveKind.ApproachMireBrute => "approaching the Brute",
        _ => "the Directive",
    };

    private static string Response(DirectiveResponseKind response) => response switch
    {
        DirectiveResponseKind.Accepted => "accepted",
        DirectiveResponseKind.Delayed => "delayed",
        DirectiveResponseKind.Refused => "refused",
        _ => "unknown",
    };

    private static string Reason(DirectiveMemoryState memory) => memory.Reason switch
    {
        DirectiveResponseReason.RestAccepted => "chose to rest by the owned road-roll",
        DirectiveResponseReason.DestinationBlocked => $"destination blocked by {Blocker(memory.Blocker)}",
        DirectiveResponseReason.GuestHasNoViolentCommitment => "Guest relationship includes no violent commitment",
        _ => "unknown reason",
    };

    private static string Force(SocialVerbForce? force) => force switch
    {
        SocialVerbForce.Suggest => "SUGGEST",
        SocialVerbForce.Command => "COMMAND",
        _ => "NO SOCIAL VERB",
    };

    private static string Blocker(AgentBlockerKind blocker) => blocker switch
    {
        AgentBlockerKind.Incarnation => "the Incarnation",
        AgentBlockerKind.Creature => "a creature",
        AgentBlockerKind.PhysicalSubject => "a physical subject",
        AgentBlockerKind.Agent => "another person",
        _ => "nothing",
    };
}
