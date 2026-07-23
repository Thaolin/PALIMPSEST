using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Chronicle.Core;

public enum AgentPresenceState
{
    ApproachingHome = 1,
    WaitingAtHome = 2,
    AtHome = 3,
}

public enum AgentNeedKind
{
    Refuge = 1,
}

public enum AgentNeedStatus
{
    Seeking = 1,
    Offered = 2,
    Satisfied = 3,
}

public enum AgentHomeRelationshipKind
{
    Unfamiliar = 1,
    WelcomeOffered = 2,
    Guest = 3,
}

public enum AgentIntentKind
{
    ApproachHome = 1,
    WaitForWelcome = 2,
    ConsiderWelcome = 3,
    RemainAtHome = 4,
    ConsiderDirective = 5,
}

public enum AgentActionKind
{
    OfferWelcome = 1,
    WithdrawWelcome = 2,
}

public enum AgentActionAvailabilityReason
{
    Available = 1,
    NoConsequentialAgent = 2,
    LivingIncarnationRequired = 3,
    IncarnationMustBeAtHome = 4,
    AgentMustBeWaiting = 5,
    AgentOutOfReach = 6,
    AnotherWelcomeIsOpen = 7,
    NoWelcomeIsOpen = 8,
    AlreadyGuest = 9,
}

public enum AgentBlockerKind
{
    None = 0,
    Incarnation = 1,
    Creature = 2,
    PhysicalSubject = 3,
    Agent = 4,
}

public enum AgentEventKind
{
    Promoted = 1,
    Moved = 2,
    Blocked = 3,
    Arrived = 4,
    WelcomeOffered = 5,
    WelcomeWithdrawn = 6,
    WelcomeAccepted = 7,
    WelcomeInterrupted = 8,
}

public sealed record AgentProfile(
    string Identity,
    string DisplayName,
    string Archetype,
    string ProvenanceIdentity,
    WorldAddress OriginAddress,
    int Ordinal,
    int WorldGrammarVersion);

public sealed record AgentNeedState(AgentNeedKind Kind, AgentNeedStatus Status);

public sealed record AgentHomeRelationshipState(
    string HomeIdentity,
    AgentHomeRelationshipKind Kind,
    long? EstablishedTick = null,
    long? WelcomingIncarnationId = null);

public sealed record AgentState(
    AgentProfile Profile,
    WorldAddress Address,
    WorldAddress WaitingAddress,
    AgentPresenceState Presence,
    AgentNeedState Need,
    AgentHomeRelationshipState HomeRelationship,
    AgentIntentKind Intent,
    long PromotedTick,
    long? ArrivalTick = null,
    long? WelcomeOfferedTick = null,
    WorldAddress? RoadRollAddress = null,
    PendingDirectiveState? PendingDirective = null,
    DirectiveMemoryCollectionState DirectiveMemories = default);

public readonly struct AgentCollectionState :
    IReadOnlyList<AgentState>,
    IEquatable<AgentCollectionState>
{
    private readonly AgentState[]? _agents;

    public AgentCollectionState(IEnumerable<AgentState> agents)
    {
        ArgumentNullException.ThrowIfNull(agents);
        _agents = agents.ToArray();
    }

    public int Count => _agents?.Length ?? 0;

    public AgentState this[int index] => (_agents ?? [])[index];

    public AgentCollectionState Add(AgentState agent) =>
        new(this.Append(agent));

    public AgentCollectionState Replace(AgentState agent) =>
        new(this.Select(existing =>
            string.Equals(existing.Profile.Identity, agent.Profile.Identity, StringComparison.Ordinal)
                ? agent
                : existing));

    public AgentState? Find(string identity) =>
        this.FirstOrDefault(agent =>
            string.Equals(agent.Profile.Identity, identity, StringComparison.Ordinal));

    public bool Equals(AgentCollectionState other) => this.SequenceEqual(other);

    public override bool Equals(object? obj) =>
        obj is AgentCollectionState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var agent in this)
        {
            hash.Add(agent);
        }

        return hash.ToHashCode();
    }

    public IEnumerator<AgentState> GetEnumerator() =>
        ((IEnumerable<AgentState>)(_agents ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool operator ==(AgentCollectionState left, AgentCollectionState right) =>
        left.Equals(right);

    public static bool operator !=(AgentCollectionState left, AgentCollectionState right) =>
        !left.Equals(right);
}

public sealed record AgentEventSnapshot(
    long Tick,
    AgentEventKind Kind,
    string AgentIdentity,
    WorldAddress Address,
    AgentBlockerKind Blocker = AgentBlockerKind.None);

public sealed record AgentActionSnapshot(
    AgentActionKind Kind,
    string AgentIdentity,
    bool Available,
    AgentActionAvailabilityReason AvailabilityReason,
    long? ResolvesAtTick = null);

public sealed record AgentSnapshot(
    string Identity,
    string DisplayName,
    string Archetype,
    string ProvenanceIdentity,
    WorldAddress OriginAddress,
    WorldAddress Address,
    WorldAddress WaitingAddress,
    AgentPresenceState Presence,
    AgentNeedState Need,
    AgentHomeRelationshipState HomeRelationship,
    AgentIntentKind Intent,
    long PromotedTick,
    long? ArrivalTick,
    long? WelcomeOfferedTick,
    WorldAddress? RoadRollAddress,
    WorldAddress? NextAddress,
    long? NextHeartbeat,
    AgentBlockerKind Blocker);

public sealed record AgentContextSnapshot(
    IReadOnlyList<AgentSnapshot> Agents,
    AgentSnapshot? PrimaryAgent,
    IReadOnlyList<AgentActionSnapshot> Actions,
    IReadOnlyList<AgentEventSnapshot> RecentEvents);

internal sealed record AgentAdvanceResult(
    ChronicleState State,
    IReadOnlyList<AgentEventSnapshot> Events);

public static class AgentGrammar
{
    public const string WayfarerListenerArchetype = "wayfarer-listener";
    public const int FirstResonanceListenerOrdinal = 0;

    private static readonly string[] GivenNames =
    [
        "Aven", "Iria", "Tamar", "Sera", "Noll", "Vey", "Orra", "Cair",
        "Mara", "Edrin", "Lio", "Ressa", "Damar", "Kest", "Nara", "Pell",
    ];

    private static readonly string[] FamilyNames =
    [
        "Tern", "Morrow", "Quill", "Reed", "Venn", "Carrow", "Vale", "Sorn",
        "Dusk", "Thorn", "Fallow", "Rill", "Kern", "Aster", "Bracken", "Wey",
    ];

    public static AgentProfile Generate(
        long seed,
        int worldGrammarVersion,
        string provenanceIdentity,
        WorldAddress originAddress,
        int ordinal)
    {
        if (worldGrammarVersion < 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(worldGrammarVersion),
                "Generated Agents require World Grammar version 6 or later.");
        }

        if (string.IsNullOrWhiteSpace(provenanceIdentity))
        {
            throw new ArgumentException("Agent provenance cannot be empty.", nameof(provenanceIdentity));
        }

        if (string.IsNullOrWhiteSpace(originAddress.Stratum))
        {
            throw new ArgumentException("Agent origin must name a Stratum.", nameof(originAddress));
        }

        if (ordinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        var provenanceHash = StableTextHash(provenanceIdentity);
        var provenanceIdentityHash = StableIdentityHash(provenanceIdentity);
        var originStratumIdentityHash = StableIdentityHash(originAddress.Stratum);
        var givenIndex = (int)StableIndex(seed, provenanceHash, ordinal, GivenNames.Length, 0xA7C15F39u);
        var familyIndex = (int)StableIndex(seed, provenanceHash, ordinal, FamilyNames.Length, 0x4D2B91E5u);
        var identity = string.Create(
            CultureInfo.InvariantCulture,
            $"agent.{seed}.{worldGrammarVersion}.{provenanceIdentityHash}." +
            $"{originStratumIdentityHash}.{originAddress.X}.{originAddress.Y}.{ordinal}");

        return new AgentProfile(
            identity,
            $"{GivenNames[givenIndex]} {FamilyNames[familyIndex]}",
            WayfarerListenerArchetype,
            provenanceIdentity,
            originAddress,
            ordinal,
            worldGrammarVersion);
    }

    private static uint StableIndex(
        long seed,
        uint provenanceHash,
        int ordinal,
        int length,
        uint salt)
    {
        var mixed = DeterministicHash.Coordinates(
            seed,
            unchecked((long)provenanceHash),
            ordinal,
            salt);
        return mixed % (uint)length;
    }

    private static uint StableTextHash(string value)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= 16777619u;
            }

            return hash;
        }
    }

    private static string StableIdentityHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
}

internal static class AgentRules
{
    public const string RoadRollArchetype = "wayfarer-road-roll";
    public const string RoadRollCondition = "laid";

    public static AgentContextSnapshot Snapshot(
        ChronicleState state,
        IReadOnlyList<AgentEventSnapshot> recentEvents)
    {
        var agents = state.Agents
            .Select(agent => SnapshotAgent(state, agent))
            .ToArray();
        var primary = agents.FirstOrDefault();
        IReadOnlyList<AgentActionSnapshot> actions = primary is null
            ? []
            :
            [
                Action(state, primary.Identity, AgentActionKind.OfferWelcome),
                Action(state, primary.Identity, AgentActionKind.WithdrawWelcome),
            ];
        return new AgentContextSnapshot(
            Array.AsReadOnly(agents),
            primary,
            actions,
            Array.AsReadOnly(recentEvents.ToArray()));
    }

    public static AgentAdvanceResult Advance(ChronicleState state)
    {
        if (state.WorldGrammarVersion != 6)
        {
            return new AgentAdvanceResult(state, []);
        }

        if (ShouldPromoteResonanceListener(state))
        {
            var promoted = PromoteResonanceListener(state);
            var listener = promoted.Agents.Find(ResonanceListenerProfile(promoted).Identity)!;
            return new AgentAdvanceResult(
                promoted,
                [new AgentEventSnapshot(
                    promoted.Tick,
                    AgentEventKind.Promoted,
                    listener.Profile.Identity,
                    listener.Address)]);
        }

        var next = state;
        var events = new List<AgentEventSnapshot>();
        // Consequential records with no timed local intent remain durable data;
        // they are not a world-scale per-Heartbeat population simulation.
        foreach (var agent in state.Agents.Where(candidate =>
                     candidate.Intent is AgentIntentKind.ApproachHome or
                         AgentIntentKind.ConsiderWelcome))
        {
            var updated = agent;
            if (agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered)
            {
                (next, updated) = ResolveWelcome(next, agent, events);
            }
            else if (agent.Presence == AgentPresenceState.ApproachingHome)
            {
                (next, updated) = AdvanceApproach(next, agent, events);
            }

            next = next with { Agents = next.Agents.Replace(updated) };
        }

        return new AgentAdvanceResult(next, Array.AsReadOnly(events.ToArray()));
    }

    public static bool TryOfferWelcome(
        ChronicleState state,
        string agentIdentity,
        out ChronicleState updated,
        out AgentActionAvailabilityReason reason,
        out AgentEventSnapshot? @event)
    {
        var action = Action(state, agentIdentity, AgentActionKind.OfferWelcome);
        reason = action.AvailabilityReason;
        @event = null;
        if (!action.Available || state.Agents.Find(agentIdentity) is not { } agent)
        {
            updated = state;
            return false;
        }

        var offered = agent with
        {
            Need = agent.Need with { Status = AgentNeedStatus.Offered },
            HomeRelationship = agent.HomeRelationship with
            {
                Kind = AgentHomeRelationshipKind.WelcomeOffered,
                EstablishedTick = null,
                WelcomingIncarnationId = state.IncarnationId,
            },
            Intent = AgentIntentKind.ConsiderWelcome,
            WelcomeOfferedTick = state.Tick,
        };
        updated = state with { Agents = state.Agents.Replace(offered) };
        @event = new AgentEventSnapshot(
            state.Tick,
            AgentEventKind.WelcomeOffered,
            agentIdentity,
            agent.Address);
        return true;
    }

    public static bool TryWithdrawWelcome(
        ChronicleState state,
        string agentIdentity,
        out ChronicleState updated,
        out AgentActionAvailabilityReason reason,
        out AgentEventSnapshot? @event)
    {
        var action = Action(state, agentIdentity, AgentActionKind.WithdrawWelcome);
        reason = action.AvailabilityReason;
        @event = null;
        if (!action.Available || state.Agents.Find(agentIdentity) is not { } agent)
        {
            updated = state;
            return false;
        }

        var withdrawn = ReturnToWaiting(agent);
        updated = state with { Agents = state.Agents.Replace(withdrawn) };
        @event = new AgentEventSnapshot(
            state.Tick,
            AgentEventKind.WelcomeWithdrawn,
            agentIdentity,
            agent.Address);
        return true;
    }

    public static bool IsOccupied(ChronicleState state, WorldAddress address) =>
        state.Agents.Any(agent => agent.Address == address);

    public static AgentActionSnapshot Action(
        ChronicleState state,
        string agentIdentity,
        AgentActionKind kind)
    {
        if (state.Agents.Find(agentIdentity) is not { } agent)
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.NoConsequentialAgent);
        }

        if (kind == AgentActionKind.WithdrawWelcome)
        {
            return agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered
                ? Available(kind, agentIdentity)
                : Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.NoWelcomeIsOpen);
        }

        if (!state.HasLivingIncarnation)
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.LivingIncarnationRequired);
        }

        if (agent.HomeRelationship.Kind == AgentHomeRelationshipKind.Guest)
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.AlreadyGuest);
        }

        if (state.Agents.Any(other =>
                other.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered &&
                !string.Equals(other.Profile.Identity, agentIdentity, StringComparison.Ordinal)))
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.AnotherWelcomeIsOpen);
        }

        if (agent.HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered)
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.AnotherWelcomeIsOpen);
        }

        if (agent.Presence != AgentPresenceState.WaitingAtHome)
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.AgentMustBeWaiting);
        }

        if (state.Home is not { } home || !IsWithinInteractionReach(state.Address, home.Address))
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.IncarnationMustBeAtHome);
        }

        if (!IsWithinInteractionReach(state.Address, agent.Address))
        {
            return Unavailable(kind, agentIdentity, AgentActionAvailabilityReason.AgentOutOfReach);
        }

        return Available(kind, agentIdentity, checked(state.Tick + 1));
    }

    internal static bool IsWithinInteractionReach(WorldAddress actor, WorldAddress subject) =>
        string.Equals(actor.Stratum, subject.Stratum, StringComparison.Ordinal) &&
        CardinalDistance(actor, subject) <= 1;

    internal static AgentProfile ResonanceListenerProfile(ChronicleState state) =>
        AgentGrammar.Generate(
            state.Seed,
            state.WorldGrammarVersion,
            HoldingRules.ResonantLodeIdentity(state.Seed),
            HoldingFacts.SingingSeamAddress,
            AgentGrammar.FirstResonanceListenerOrdinal);

    internal static WorldAddress ResonanceListenerWaitingAddress(ChronicleState state)
    {
        var home = state.Home ?? throw new InvalidOperationException("A resonance listener requires Home.");
        return home.Address with { X = checked(home.Address.X - 1) };
    }

    internal static WorldAddress ResonanceListenerStartAddress(ChronicleState state) =>
        ResonanceListenerWaitingAddress(state) with
        {
            X = checked(ResonanceListenerWaitingAddress(state).X - 3),
        };

    internal static WorldAddress ResonanceListenerRoadRollAddress(ChronicleState state)
    {
        var waiting = ResonanceListenerWaitingAddress(state);
        return waiting with { Y = checked(waiting.Y + 1) };
    }

    internal static string RoadRollIdentity(string agentIdentity) =>
        $"personal.road-roll.{agentIdentity}";

    private static bool ShouldPromoteResonanceListener(ChronicleState state) =>
        state.Agents.Find(ResonanceListenerProfile(state).Identity) is null &&
        state.PowerHome is
        {
            Lode.Disposition: ResonantLodeDisposition.Installed,
            Resonator.Phase: HearthResonatorPhase.Intact,
        };

    private static ChronicleState PromoteResonanceListener(ChronicleState state)
    {
        var profile = ResonanceListenerProfile(state);
        var agent = new AgentState(
            profile,
            ResonanceListenerStartAddress(state),
            ResonanceListenerWaitingAddress(state),
            AgentPresenceState.ApproachingHome,
            new AgentNeedState(AgentNeedKind.Refuge, AgentNeedStatus.Seeking),
            new AgentHomeRelationshipState(
                state.Home!.HoldingId,
                AgentHomeRelationshipKind.Unfamiliar),
            AgentIntentKind.ApproachHome,
            state.Tick);
        return state with { Agents = state.Agents.Add(agent) };
    }

    private static (ChronicleState State, AgentState Agent) AdvanceApproach(
        ChronicleState state,
        AgentState agent,
        List<AgentEventSnapshot> events)
    {
        var destination = NextStep(agent.Address, agent.WaitingAddress);
        var blocker = BlockerAt(state, agent.Profile.Identity, destination);
        if (blocker != AgentBlockerKind.None)
        {
            events.Add(new AgentEventSnapshot(
                state.Tick,
                AgentEventKind.Blocked,
                agent.Profile.Identity,
                destination,
                blocker));
            return (state, agent);
        }

        var arrived = destination == agent.WaitingAddress;
        var updated = agent with
        {
            Address = destination,
            Presence = arrived ? AgentPresenceState.WaitingAtHome : agent.Presence,
            Intent = arrived ? AgentIntentKind.WaitForWelcome : agent.Intent,
            ArrivalTick = arrived ? state.Tick : agent.ArrivalTick,
        };
        events.Add(new AgentEventSnapshot(
            state.Tick,
            arrived ? AgentEventKind.Arrived : AgentEventKind.Moved,
            agent.Profile.Identity,
            destination));
        return (arrived ? state with { Speed = ChronicleSpeed.Paused } : state, updated);
    }

    private static (ChronicleState State, AgentState Agent) ResolveWelcome(
        ChronicleState state,
        AgentState agent,
        List<AgentEventSnapshot> events)
    {
        var valid = state.HasLivingIncarnation &&
                    state.Home is { } home &&
                    IsWithinInteractionReach(state.Address, home.Address) &&
                    agent.Presence == AgentPresenceState.WaitingAtHome &&
                    IsWithinInteractionReach(state.Address, agent.Address) &&
                    agent.WelcomeOfferedTick is { } offeredTick &&
                    offeredTick < state.Tick;
        if (!valid)
        {
            var waiting = ReturnToWaiting(agent);
            events.Add(new AgentEventSnapshot(
                state.Tick,
                AgentEventKind.WelcomeInterrupted,
                agent.Profile.Identity,
                agent.Address));
            return (state, waiting);
        }

        var accepted = agent with
        {
            Presence = AgentPresenceState.AtHome,
            Need = agent.Need with { Status = AgentNeedStatus.Satisfied },
            HomeRelationship = agent.HomeRelationship with
            {
                Kind = AgentHomeRelationshipKind.Guest,
                EstablishedTick = state.Tick,
            },
            Intent = AgentIntentKind.RemainAtHome,
            RoadRollAddress = ResonanceListenerRoadRollAddress(state),
        };
        events.Add(new AgentEventSnapshot(
            state.Tick,
            AgentEventKind.WelcomeAccepted,
            agent.Profile.Identity,
            agent.Address));
        return (state with { Speed = ChronicleSpeed.Paused }, accepted);
    }

    private static AgentState ReturnToWaiting(AgentState agent) => agent with
    {
        Need = agent.Need with { Status = AgentNeedStatus.Seeking },
        HomeRelationship = agent.HomeRelationship with
        {
            Kind = AgentHomeRelationshipKind.Unfamiliar,
            EstablishedTick = null,
            WelcomingIncarnationId = null,
        },
        Intent = AgentIntentKind.WaitForWelcome,
        WelcomeOfferedTick = null,
    };

    private static AgentSnapshot SnapshotAgent(ChronicleState state, AgentState agent)
    {
        long? nextHeartbeat = agent.Intent is AgentIntentKind.ApproachHome or AgentIntentKind.ConsiderWelcome
            ? checked(state.Tick + 1)
            : null;
        var destination = agent.Intent == AgentIntentKind.ApproachHome
            ? NextStep(agent.Address, agent.WaitingAddress)
            : agent.Address;
        return new AgentSnapshot(
            agent.Profile.Identity,
            agent.Profile.DisplayName,
            agent.Profile.Archetype,
            agent.Profile.ProvenanceIdentity,
            agent.Profile.OriginAddress,
            agent.Address,
            agent.WaitingAddress,
            agent.Presence,
            agent.Need,
            agent.HomeRelationship,
            agent.Intent,
            agent.PromotedTick,
            agent.ArrivalTick,
            agent.WelcomeOfferedTick,
            agent.RoadRollAddress,
            agent.Intent == AgentIntentKind.ApproachHome ? destination : null,
            nextHeartbeat,
            agent.Intent == AgentIntentKind.ApproachHome
                ? BlockerAt(state, agent.Profile.Identity, destination)
                : AgentBlockerKind.None);
    }

    internal static AgentBlockerKind BlockerAt(
        ChronicleState state,
        string movingAgentIdentity,
        WorldAddress address)
    {
        if (state.HasLivingIncarnation && state.Address == address)
        {
            return AgentBlockerKind.Incarnation;
        }

        if (CombatRules.IsOccupiedByLivingMireBrute(state, address))
        {
            return AgentBlockerKind.Creature;
        }

        if (HoldingFacts.BlocksMovement(state, address))
        {
            return AgentBlockerKind.PhysicalSubject;
        }

        return state.Agents.Any(agent =>
                !string.Equals(agent.Profile.Identity, movingAgentIdentity, StringComparison.Ordinal) &&
                agent.Address == address)
            ? AgentBlockerKind.Agent
            : AgentBlockerKind.None;
    }

    private static WorldAddress NextStep(WorldAddress current, WorldAddress destination)
    {
        if (!string.Equals(current.Stratum, destination.Stratum, StringComparison.Ordinal))
        {
            return current;
        }

        if (current.X != destination.X)
        {
            return current with { X = checked(current.X + Math.Sign(destination.X - current.X)) };
        }

        if (current.Y != destination.Y)
        {
            return current with { Y = checked(current.Y + Math.Sign(destination.Y - current.Y)) };
        }

        return current;
    }

    private static ulong CardinalDistance(WorldAddress first, WorldAddress second)
    {
        if (!string.Equals(first.Stratum, second.Stratum, StringComparison.Ordinal))
        {
            return ulong.MaxValue;
        }

        var x = first.X >= second.X
            ? (ulong)((Int128)first.X - second.X)
            : (ulong)((Int128)second.X - first.X);
        var y = first.Y >= second.Y
            ? (ulong)((Int128)first.Y - second.Y)
            : (ulong)((Int128)second.Y - first.Y);
        return x > ulong.MaxValue - y ? ulong.MaxValue : x + y;
    }

    private static AgentActionSnapshot Available(
        AgentActionKind kind,
        string identity,
        long? resolvesAtTick = null) =>
        new(kind, identity, true, AgentActionAvailabilityReason.Available, resolvesAtTick);

    private static AgentActionSnapshot Unavailable(
        AgentActionKind kind,
        string identity,
        AgentActionAvailabilityReason reason) =>
        new(kind, identity, false, reason);
}
