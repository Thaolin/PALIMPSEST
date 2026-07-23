using Chronicle.Core;

public enum TamarReactionIntent
{
    ArrivalNeedsRefuge,
    WelcomeAccepted,
    SuggestAccepted,
    CommandRefused,
    RememberedExchange,
}

public sealed record TamarSpokenBeat(
    TamarReactionIntent Intent,
    string Speaker,
    string Relation,
    string Text,
    int Cadence);

/// <summary>
/// The bounded wayfarer-listener voice kit. Selection uses stable identity
/// only; every inserted fact comes from the current Core snapshots.
/// </summary>
public static class TamarVoice
{
    public static TamarSpokenBeat? Present(
        AgentSnapshot? agent,
        DirectiveContextSnapshot directives)
    {
        if (agent is null)
        {
            return null;
        }

        var cadence = StableCadence(agent.Identity);
        var latestEvent = directives.RecentEvents.LastOrDefault();
        var latestMemory = directives.Memories.LastOrDefault();
        if (latestEvent is
            {
                Kind: DirectiveEventKind.Refused,
                Reason: DirectiveResponseReason.GuestHasNoViolentCommitment,
            })
        {
            return Compose(TamarReactionIntent.CommandRefused, agent, cadence);
        }

        if (latestEvent is { Kind: DirectiveEventKind.Accepted })
        {
            return Compose(TamarReactionIntent.SuggestAccepted, agent, cadence);
        }

        if (latestMemory is { } memory)
        {
            return Compose(
                TamarReactionIntent.RememberedExchange,
                agent,
                cadence,
                memory);
        }

        if (agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Guest)
        {
            return Compose(TamarReactionIntent.WelcomeAccepted, agent, cadence);
        }

        if (agent.Presence == AgentPresenceState.WaitingAtHome)
        {
            return Compose(TamarReactionIntent.ArrivalNeedsRefuge, agent, cadence);
        }

        return null;
    }

    internal static IReadOnlyList<TamarSpokenBeat> ComposeKitForVerification(
        AgentSnapshot agent)
    {
        var alternate = agent with { Identity = AlternateIdentity(agent.Identity) };
        return new[] { agent, alternate }
            .SelectMany(identity => Enum.GetValues<TamarReactionIntent>()
                .Select(intent => Compose(
                    intent,
                    identity,
                    StableCadence(identity.Identity))))
            .ToArray();
    }

    private static TamarSpokenBeat Compose(
        TamarReactionIntent intent,
        AgentSnapshot agent,
        int cadence,
        DirectiveMemoryState? memory = null)
    {
        var text = intent switch
        {
            TamarReactionIntent.ArrivalNeedsRefuge => cadence == 0
                ? "The singing carried farther than you know.\nI need refuge, if refuge is what you mean to offer."
                : "I followed the resonance here.\nMay I rest beneath this roof without owing my life for it?",
            TamarReactionIntent.WelcomeAccepted => cadence == 0
                ? "Then I accept your shelter.\nThis road-roll is mine; I will keep it close."
                : "A roof, warmth, and no claim upon my hands.\nI can live with that.",
            TamarReactionIntent.SuggestAccepted => cadence == 0
                ? "A quiet place by my road-roll? Yes.\nThat is a kindness I can accept."
                : "I hear the sense in that.\nI'll rest beside what I brought.",
            TamarReactionIntent.CommandRefused => cadence == 0
                ? "I accepted a roof, not an oath to bleed for it.\nI will not be sent into danger."
                : "Your fire can carry the order. It cannot make the choice mine.\nGuest is not soldier.",
            TamarReactionIntent.RememberedExchange => RememberedExchange(memory, cadence),
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, null),
        };
        return Beat(intent, agent, cadence, text);
    }

    private static string RememberedExchange(
        DirectiveMemoryState? memory,
        int cadence)
    {
        var exchange = memory?.Directive == DirectiveKind.RestByRoadRoll
            ? "suggestion"
            : "order";
        var answer = memory?.Response switch
        {
            DirectiveResponseKind.Accepted => "yes",
            DirectiveResponseKind.Delayed => "not yet",
            _ => "no",
        };
        return cadence == 0
            ? $"I remember the {exchange}, and my answer: {answer}.\nA Guest still keeps their own will."
            : $"We spoke. I considered the {exchange}.\nMy answer remains {answer}.";
    }

    private static TamarSpokenBeat Beat(
        TamarReactionIntent intent,
        AgentSnapshot agent,
        int cadence,
        string text) =>
        new(
            intent,
            agent.DisplayName,
            agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Guest
                ? "Guest of the First Hearth"
                : "Wayfarer seeking refuge",
            text,
            cadence);

    private static int StableCadence(string identity)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in identity)
            {
                hash ^= character;
                hash *= 16777619;
            }
            return (int)(hash & 1);
        }
    }

    private static string AlternateIdentity(string identity)
    {
        for (var ordinal = 1; ordinal <= 16; ordinal++)
        {
            var candidate = $"{identity}.alternate-{ordinal}";
            if (StableCadence(candidate) != StableCadence(identity))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not derive Tamar's alternate cadence identity.");
    }
}
