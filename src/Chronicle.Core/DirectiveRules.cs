using System.Collections;

namespace Chronicle.Core;

public enum SocialVerbForce
{
    Suggest = 1,
    Command = 2,
}

public enum DirectiveKind
{
    RestByRoadRoll = 1,
    ApproachMireBrute = 2,
}

public enum DirectiveResponseKind
{
    Accepted = 1,
    Delayed = 2,
    Refused = 3,
}

public enum DirectiveResponseReason
{
    RestAccepted = 1,
    DestinationBlocked = 2,
    GuestHasNoViolentCommitment = 3,
}

public enum DirectiveAvailabilityReason
{
    Available = 1,
    NoConsequentialAgent = 2,
    LivingIncarnationRequired = 3,
    RecipientNotLocal = 4,
    RecipientOutOfReach = 5,
    ActiveSocialVerbRequired = 6,
    SocialModifiersUnsupported = 7,
    ObjectiveUnavailable = 8,
    ObjectiveAlreadySatisfied = 9,
    InsufficientForce = 10,
    AnotherDirectivePending = 11,
    NoDirectivePending = 12,
    OriginalIssuerRequired = 13,
}

public enum DirectiveEventKind
{
    Delivered = 1,
    Withdrawn = 2,
    Accepted = 3,
    Delayed = 4,
    Refused = 5,
}

public sealed record DirectiveDefinition(
    DirectiveKind Kind,
    string Identity,
    SocialVerbForce MinimumForce);

public sealed record PendingDirectiveState(
    string AgentIdentity,
    long IssuingIncarnationId,
    WordId Verb,
    DirectiveKind Directive,
    string ObjectiveIdentity,
    WorldAddress ObjectiveAddress,
    long IssuedTick,
    long ResolvesAtTick,
    WorldAddress DeliveryAddress);

public sealed record DirectiveMemoryState(
    string AgentIdentity,
    long IssuingIncarnationId,
    WordId Verb,
    DirectiveKind Directive,
    string ObjectiveIdentity,
    WorldAddress ObjectiveAddress,
    long IssuedTick,
    long ResolvedTick,
    DirectiveResponseKind Response,
    DirectiveResponseReason Reason,
    AgentBlockerKind Blocker,
    WorldAddress ResultingAddress);

public readonly struct DirectiveMemoryCollectionState :
    IReadOnlyList<DirectiveMemoryState>,
    IEquatable<DirectiveMemoryCollectionState>
{
    private readonly DirectiveMemoryState[]? _memories;

    public DirectiveMemoryCollectionState(IEnumerable<DirectiveMemoryState> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);
        _memories = memories.ToArray();
    }

    public int Count => _memories?.Length ?? 0;

    public DirectiveMemoryState this[int index] => (_memories ?? [])[index];

    public DirectiveMemoryCollectionState Add(DirectiveMemoryState memory) =>
        new(this.Append(memory));

    public bool Equals(DirectiveMemoryCollectionState other) => this.SequenceEqual(other);

    public override bool Equals(object? obj) =>
        obj is DirectiveMemoryCollectionState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var memory in this)
        {
            hash.Add(memory);
        }

        return hash.ToHashCode();
    }

    public IEnumerator<DirectiveMemoryState> GetEnumerator() =>
        ((IEnumerable<DirectiveMemoryState>)(_memories ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool operator ==(
        DirectiveMemoryCollectionState left,
        DirectiveMemoryCollectionState right) => left.Equals(right);

    public static bool operator !=(
        DirectiveMemoryCollectionState left,
        DirectiveMemoryCollectionState right) => !left.Equals(right);
}

public sealed record DirectiveActionSnapshot(
    DirectiveKind Directive,
    string DirectiveIdentity,
    string AgentIdentity,
    string? ObjectiveIdentity,
    WorldAddress? ObjectiveAddress,
    int SlotIndex,
    WordId? ActiveVerb,
    SocialVerbForce? ActiveForce,
    SocialVerbForce MinimumForce,
    bool Available,
    DirectiveAvailabilityReason AvailabilityReason,
    long? ResolvesAtTick);

public sealed record DirectiveEventSnapshot(
    long Tick,
    DirectiveEventKind Kind,
    string AgentIdentity,
    DirectiveKind Directive,
    string ObjectiveIdentity,
    WorldAddress ObjectiveAddress,
    long IssuingIncarnationId,
    WordId Verb,
    DirectiveResponseKind? Response = null,
    DirectiveResponseReason? Reason = null,
    AgentBlockerKind Blocker = AgentBlockerKind.None);

public sealed record DirectiveContextSnapshot(
    string? PrimaryAgentIdentity,
    IReadOnlyList<DirectiveActionSnapshot> Actions,
    PendingDirectiveState? Pending,
    IReadOnlyList<DirectiveMemoryState> Memories,
    IReadOnlyList<DirectiveEventSnapshot> RecentEvents);

internal sealed record DirectiveAdvanceResult(
    ChronicleState State,
    IReadOnlyList<DirectiveEventSnapshot> Events);

internal static class DirectiveRules
{
    public static readonly DirectiveDefinition RestByRoadRoll = new(
        DirectiveKind.RestByRoadRoll,
        "directive.rest-by-road-roll",
        SocialVerbForce.Suggest);

    public static readonly DirectiveDefinition ApproachMireBrute = new(
        DirectiveKind.ApproachMireBrute,
        "directive.approach-mire-brute",
        SocialVerbForce.Command);

    public static IReadOnlyList<DirectiveDefinition> Definitions { get; } =
        Array.AsReadOnly([RestByRoadRoll, ApproachMireBrute]);

    public static DirectiveContextSnapshot Snapshot(
        ChronicleState state,
        IReadOnlyList<DirectiveEventSnapshot> recentEvents)
    {
        var primary = state.Agents.FirstOrDefault();
        IReadOnlyList<DirectiveActionSnapshot> actions = primary is null
            ? []
            : Array.AsReadOnly(Definitions
                .Select(definition => Action(state, primary.Profile.Identity, 0, definition.Kind))
                .ToArray());
        return new DirectiveContextSnapshot(
            primary?.Profile.Identity,
            actions,
            primary?.PendingDirective,
            Array.AsReadOnly(primary?.DirectiveMemories.ToArray() ?? []),
            Array.AsReadOnly(recentEvents.ToArray()));
    }

    public static DirectiveActionSnapshot Action(
        ChronicleState state,
        string agentIdentity,
        int slotIndex,
        DirectiveKind directive)
    {
        var definition = Definition(directive);
        var slot = slotIndex is >= 0 and < LoadoutState.SlotCount
            ? state.ActiveLoadout[slotIndex]
            : default;
        var activeVerb = slot.Verb;
        var force = activeVerb is { } verb ? ForceFor(verb) : null;
        var objective = Objective(state, agentIdentity, directive);

        DirectiveAvailabilityReason reason;
        if (state.Agents.Find(agentIdentity) is not { } agent)
        {
            reason = DirectiveAvailabilityReason.NoConsequentialAgent;
        }
        else if (!state.HasLivingIncarnation)
        {
            reason = DirectiveAvailabilityReason.LivingIncarnationRequired;
        }
        else if (!string.Equals(state.Address.Stratum, agent.Address.Stratum, StringComparison.Ordinal))
        {
            reason = DirectiveAvailabilityReason.RecipientNotLocal;
        }
        else if (!AgentRules.IsWithinInteractionReach(state.Address, agent.Address))
        {
            reason = DirectiveAvailabilityReason.RecipientOutOfReach;
        }
        else if (activeVerb is null || force is null)
        {
            reason = DirectiveAvailabilityReason.ActiveSocialVerbRequired;
        }
        else if (slot.Modifiers.Count > 0 || slot.Noun is not null)
        {
            reason = DirectiveAvailabilityReason.SocialModifiersUnsupported;
        }
        else if (agent.PendingDirective is not null)
        {
            reason = DirectiveAvailabilityReason.AnotherDirectivePending;
        }
        else if (objective is null)
        {
            reason = DirectiveAvailabilityReason.ObjectiveUnavailable;
        }
        else if (directive == DirectiveKind.RestByRoadRoll && agent.Address == objective.Value.Address)
        {
            reason = DirectiveAvailabilityReason.ObjectiveAlreadySatisfied;
        }
        else if (force < definition.MinimumForce)
        {
            reason = DirectiveAvailabilityReason.InsufficientForce;
        }
        else
        {
            reason = DirectiveAvailabilityReason.Available;
        }

        return new DirectiveActionSnapshot(
            directive,
            definition.Identity,
            agentIdentity,
            objective?.Identity,
            objective?.Address,
            slotIndex,
            activeVerb,
            force,
            definition.MinimumForce,
            reason == DirectiveAvailabilityReason.Available,
            reason,
            reason == DirectiveAvailabilityReason.Available ? checked(state.Tick + 1) : null);
    }

    public static bool TryDeliver(
        ChronicleState state,
        string agentIdentity,
        int slotIndex,
        DirectiveKind directive,
        out ChronicleState updated,
        out DirectiveAvailabilityReason reason,
        out DirectiveEventSnapshot? @event)
    {
        var action = Action(state, agentIdentity, slotIndex, directive);
        reason = action.AvailabilityReason;
        @event = null;
        if (!action.Available ||
            action.ActiveVerb is not { } verb ||
            action.ObjectiveIdentity is not { } objectiveIdentity ||
            action.ObjectiveAddress is not { } objectiveAddress ||
            state.Agents.Find(agentIdentity) is not { } agent)
        {
            updated = state;
            return false;
        }

        var pending = new PendingDirectiveState(
            agentIdentity,
            state.IncarnationId,
            verb,
            directive,
            objectiveIdentity,
            objectiveAddress,
            state.Tick,
            checked(state.Tick + 1),
            state.Address);
        var considering = agent with
        {
            Intent = AgentIntentKind.ConsiderDirective,
            PendingDirective = pending,
        };
        updated = state with
        {
            Speed = ChronicleSpeed.Paused,
            Agents = state.Agents.Replace(considering),
        };
        @event = Event(state.Tick, DirectiveEventKind.Delivered, pending);
        return true;
    }

    public static bool TryWithdraw(
        ChronicleState state,
        string agentIdentity,
        out ChronicleState updated,
        out DirectiveAvailabilityReason reason,
        out DirectiveEventSnapshot? @event)
    {
        @event = null;
        if (state.Agents.Find(agentIdentity) is not { } agent)
        {
            reason = DirectiveAvailabilityReason.NoConsequentialAgent;
            updated = state;
            return false;
        }

        if (agent.PendingDirective is not { } pending)
        {
            reason = DirectiveAvailabilityReason.NoDirectivePending;
            updated = state;
            return false;
        }

        if (!state.HasLivingIncarnation || state.IncarnationId != pending.IssuingIncarnationId)
        {
            reason = DirectiveAvailabilityReason.OriginalIssuerRequired;
            updated = state;
            return false;
        }

        if (!AgentRules.IsWithinInteractionReach(state.Address, agent.Address))
        {
            reason = DirectiveAvailabilityReason.RecipientOutOfReach;
            updated = state;
            return false;
        }

        var withdrawn = agent with
        {
            Intent = AgentIntentKind.RemainAtHome,
            PendingDirective = null,
        };
        updated = state with { Agents = state.Agents.Replace(withdrawn) };
        reason = DirectiveAvailabilityReason.Available;
        @event = Event(state.Tick, DirectiveEventKind.Withdrawn, pending);
        return true;
    }

    public static DirectiveAdvanceResult Advance(ChronicleState state)
    {
        var next = state;
        var events = new List<DirectiveEventSnapshot>();
        foreach (var original in state.Agents.Where(agent =>
                     agent.PendingDirective is { } pending && pending.ResolvesAtTick <= state.Tick))
        {
            var agent = next.Agents.Find(original.Profile.Identity)!;
            var pending = agent.PendingDirective!;
            var response = Resolve(next, agent, pending);
            next = response.State;
            events.Add(response.Event);
        }

        return new DirectiveAdvanceResult(next, Array.AsReadOnly(events.ToArray()));
    }

    public static SocialVerbForce? ForceFor(WordId verb) => verb == WordIds.Suggest
        ? SocialVerbForce.Suggest
        : verb == WordIds.Command
            ? SocialVerbForce.Command
            : null;

    public static DirectiveDefinition Definition(DirectiveKind kind) =>
        Definitions.FirstOrDefault(definition => definition.Kind == kind)
        ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Directive kind.");

    private static (ChronicleState State, DirectiveEventSnapshot Event) Resolve(
        ChronicleState state,
        AgentState agent,
        PendingDirectiveState pending)
    {
        DirectiveResponseKind response;
        DirectiveResponseReason reason;
        AgentBlockerKind blocker;
        var resultingAddress = agent.Address;

        if (pending.Directive == DirectiveKind.RestByRoadRoll)
        {
            blocker = AgentRules.BlockerAt(state, agent.Profile.Identity, pending.ObjectiveAddress);
            if (blocker == AgentBlockerKind.None)
            {
                response = DirectiveResponseKind.Accepted;
                reason = DirectiveResponseReason.RestAccepted;
                resultingAddress = pending.ObjectiveAddress;
            }
            else
            {
                response = DirectiveResponseKind.Delayed;
                reason = DirectiveResponseReason.DestinationBlocked;
            }
        }
        else
        {
            response = DirectiveResponseKind.Refused;
            reason = DirectiveResponseReason.GuestHasNoViolentCommitment;
            blocker = AgentBlockerKind.None;
        }

        var memory = new DirectiveMemoryState(
            pending.AgentIdentity,
            pending.IssuingIncarnationId,
            pending.Verb,
            pending.Directive,
            pending.ObjectiveIdentity,
            pending.ObjectiveAddress,
            pending.IssuedTick,
            state.Tick,
            response,
            reason,
            blocker,
            resultingAddress);
        var resolved = agent with
        {
            Address = resultingAddress,
            Intent = AgentIntentKind.RemainAtHome,
            PendingDirective = null,
            DirectiveMemories = agent.DirectiveMemories.Add(memory),
        };
        var updated = state with
        {
            Speed = ChronicleSpeed.Paused,
            Agents = state.Agents.Replace(resolved),
        };
        var eventKind = response switch
        {
            DirectiveResponseKind.Accepted => DirectiveEventKind.Accepted,
            DirectiveResponseKind.Delayed => DirectiveEventKind.Delayed,
            DirectiveResponseKind.Refused => DirectiveEventKind.Refused,
            _ => throw new ArgumentOutOfRangeException(nameof(response), response, null),
        };
        return (updated, Event(state.Tick, eventKind, pending, response, reason, blocker));
    }

    private static (string Identity, WorldAddress Address)? Objective(
        ChronicleState state,
        string agentIdentity,
        DirectiveKind directive)
    {
        var agent = state.Agents.Find(agentIdentity);
        return directive switch
        {
            DirectiveKind.RestByRoadRoll when agent?.RoadRollAddress is { } address =>
                (AgentRules.RoadRollIdentity(agentIdentity), address),
            DirectiveKind.ApproachMireBrute when state.Combat?.MireBrute is { IsLiving: true } brute =>
                (brute.Identity, brute.Address),
            _ => null,
        };
    }

    private static DirectiveEventSnapshot Event(
        long tick,
        DirectiveEventKind kind,
        PendingDirectiveState pending,
        DirectiveResponseKind? response = null,
        DirectiveResponseReason? reason = null,
        AgentBlockerKind blocker = AgentBlockerKind.None) =>
        new(
            tick,
            kind,
            pending.AgentIdentity,
            pending.Directive,
            pending.ObjectiveIdentity,
            pending.ObjectiveAddress,
            pending.IssuingIncarnationId,
            pending.Verb,
            response,
            reason,
            blocker);
}
