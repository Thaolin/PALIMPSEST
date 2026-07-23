using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicle.Core;

public static partial class ChronicleSaveCodec
{
    public const int CurrentVersion = 9;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly IReadOnlyDictionary<WordId, IReadOnlyList<WordId>>
        PreVersion5CompatibleNouns = new Dictionary<WordId, IReadOnlyList<WordId>>
        {
            [WordIds.Fly] = [WordIds.Stone],
        };

    public static string Serialize(ChronicleState state)
    {
        var current = state.MigrateAndValidate();
        ValidateCurrentState(current);
        return JsonSerializer.Serialize(
            new CurrentSaveEnvelope(CurrentVersion, ToCurrentSave(current)),
            Options);
    }

    public static ChronicleState Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Chronicle save data must be a JSON object.");
        }

        if (document.RootElement.TryGetProperty("Version", out var versionElement))
        {
            return DeserializeEnvelope(document.RootElement, versionElement);
        }

        if (document.RootElement.TryGetProperty("Chronicle", out _))
        {
            throw new InvalidOperationException(
                "A Chronicle save envelope was missing its Version.");
        }

        return DeserializePreEnvelope(document.RootElement, json);
    }

    private static ChronicleState DeserializeEnvelope(
        JsonElement root,
        JsonElement versionElement)
    {
        if (!versionElement.TryGetInt32(out var version))
        {
            throw new InvalidOperationException("Chronicle save version must be an integer.");
        }

        return version switch
        {
            1 => DeserializeVersion1(root),
            2 => DeserializeVersion2(root),
            3 => DeserializeVersion3(root),
            4 => DeserializeVersion4(root),
            5 => DeserializeVersion5(root),
            6 => DeserializeVersion6(root),
            7 => DeserializeVersion7(root),
            8 => DeserializeVersion8(root),
            9 => DeserializeVersion9(root),
            _ => throw new InvalidOperationException($"Unsupported Chronicle save version '{version}'."),
        };
    }

    private static ChronicleState DeserializeVersion1(JsonElement root)
    {
        RequireObjectWithProperties(
            root,
            "Version 1 envelope",
            "Version",
            "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 1 Chronicle save data was missing its Chronicle.");
        }

        ValidatePredecessorDocument(
            chronicleElement,
            "Version 1 Chronicle",
            requireIntentAndGrammar: true);
        var predecessor = chronicleElement.Deserialize<PredecessorChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 1 Chronicle save data was empty.");
        if (predecessor.WorldGrammarVersion is not (0 or 1))
        {
            throw new InvalidOperationException(
                "Version 1 Chronicle saves only support World Grammar pins 0 or 1.");
        }

        var migrated = MigratePredecessor(predecessor, migrateWorldGrammarOne: true);
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static ChronicleState DeserializeVersion2(JsonElement root)
    {
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 2 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion2Document(chronicleElement);
        ValidatePreVersion5LoadoutCompatibility(chronicleElement, "Version 2");
        var predecessor = chronicleElement.Deserialize<Version2ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 2 Chronicle save data was empty.");
        return MigrateVersion2(predecessor);
    }

    private static ChronicleState DeserializeVersion3(JsonElement root)
    {
        RequireExactObjectWithProperties(
            root,
            "Version 3 envelope",
            "Version",
            "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 3 Chronicle save data was missing its Chronicle.");
        }

        RequireExactObjectWithProperties(
            chronicleElement,
            "Version 3 Chronicle",
            "Seed",
            "Tick",
            "Address",
            "Speed",
            "Intent",
            "Codex",
            "Study",
            "Loadout",
            "LooseStoneAddress",
            "IncarnationId",
            "IncarnationLife",
            "WorldGrammarVersion",
            "Home");
        ValidateVersion3Document(chronicleElement);
        ValidatePreVersion5LoadoutCompatibility(chronicleElement, "Version 3");
        var predecessor = chronicleElement.Deserialize<Version3ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 3 Chronicle save data was empty.");
        return MigrateVersion3(predecessor);
    }

    private static ChronicleState DeserializeVersion4(JsonElement root)
    {
        RequireExactObjectWithProperties(
            root,
            "Version 4 envelope",
            "Version",
            "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 4 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion4Document(chronicleElement);
        ValidatePreVersion5LoadoutCompatibility(chronicleElement, "Version 4");
        var predecessor = chronicleElement.Deserialize<Version4ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 4 Chronicle save data was empty.");
        return MigrateVersion4(predecessor);
    }

    private static ChronicleState DeserializeVersion5(JsonElement root)
    {
        RequireExactObjectWithProperties(
            root,
            "Version 5 envelope",
            "Version",
            "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 5 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion5Document(chronicleElement);
        var state = chronicleElement.Deserialize<ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 5 Chronicle save data was empty.");
        var migrated = state.MigrateAndValidate();
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static ChronicleState DeserializeVersion6(JsonElement root)
    {
        RequireExactObjectWithProperties(
            root,
            "Version 6 envelope",
            "Version",
            "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 6 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion6Document(chronicleElement);
        var successor = chronicleElement.Deserialize<SuccessorChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 6 Chronicle save data was empty.");
        var state = FromSuccessorSave(successor);
        ValidateCurrentState(state);
        return state;
    }

    private static ChronicleState DeserializeVersion7(JsonElement root)
    {
        RequireExactObjectWithProperties(root, "Version 7 envelope", "Version", "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 7 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion7Document(chronicleElement);
        var current = chronicleElement.Deserialize<Version7ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 7 Chronicle save data was empty.");
        var state = FromVersion7Save(current).MigrateAndValidate();
        ValidateCurrentState(state);
        return state;
    }

    private static ChronicleState DeserializeVersion8(JsonElement root)
    {
        RequireExactObjectWithProperties(root, "Version 8 envelope", "Version", "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 8 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion8Document(chronicleElement);
        var current = chronicleElement.Deserialize<Version8ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 8 Chronicle save data was empty.");
        var state = FromVersion8Save(current).MigrateAndValidate();
        ValidateCurrentState(state);
        return state;
    }

    private static ChronicleState DeserializeVersion9(JsonElement root)
    {
        RequireExactObjectWithProperties(root, "Version 9 envelope", "Version", "Chronicle");
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 9 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion9Document(chronicleElement);
        var current = chronicleElement.Deserialize<CurrentChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 9 Chronicle save data was empty.");
        var state = FromCurrentSave(current).MigrateAndValidate();
        ValidateCurrentState(state);
        return state;
    }

    private static ChronicleState DeserializePreEnvelope(
        JsonElement root,
        string json)
    {
        ValidatePredecessorDocument(
            root,
            "Pre-envelope Chronicle",
            requireIntentAndGrammar: false);
        var predecessor = JsonSerializer.Deserialize<PredecessorChronicleState>(json, Options)
            ?? throw new InvalidOperationException("Pre-envelope Chronicle save data was empty.");
        if (predecessor.WorldGrammarVersion != 0)
        {
            throw new InvalidOperationException(
                "Pre-envelope Chronicle saves support only World Grammar pin 0.");
        }

        var migrated = MigratePredecessor(predecessor, migrateWorldGrammarOne: false);
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static CurrentChronicleState ToCurrentSave(ChronicleState state)
    {
        var slot = state.ActiveLoadout.Slots.FirstOrDefault(candidate => candidate.Verb is not null);
        var retained = ToRetainedDurables(state);
        return new CurrentChronicleState(
            state.Seed,
            state.Tick,
            state.Address,
            state.Speed,
            state.Intent,
            state.Codex,
            new CurrentLoadoutState(slot.Verb, Array.AsReadOnly(slot.Modifiers.ToArray())),
            state.Attunement,
            state.IncarnationId,
            state.IncarnationLife,
            state.WorldGrammarVersion,
            state.Combat,
            state.PowerHome,
            retained,
            Array.AsReadOnly(state.Agents.Select(ToCurrentAgent).ToArray()));
    }

    private static ChronicleState FromCurrentSave(CurrentChronicleState current)
    {
        var retained = current.RetainedDurables;
        var firstConflict = retained?.RivenCairn is { } cairn
            ? new FirstConflictState(
                FirstConflictSubjects.RiverWardSubjectId,
                cairn.Address,
                checked(cairn.ResolvedTick - 1),
                PendingAction: null,
                Outcome: FirstConflictOutcome.Shattered,
                ResolvedTick: cairn.ResolvedTick,
                ResolvingIncarnationId: cairn.ResolvingIncarnationId)
            : null;
        var modifiers = current.Loadout.Modifiers ?? [];
        if (modifiers.Count > 2)
        {
            throw new InvalidOperationException("Version 7 supports at most two Modifiers in its one active Expression.");
        }

        return new ChronicleState(
            current.Seed,
            current.Tick,
            current.Address,
            current.Speed,
            current.Intent,
            current.Codex,
            new StudyState(),
            new LoadoutState(new LoadoutSlot(
                current.Loadout.Verb,
                Modifier: modifiers.Count > 0 ? modifiers[0] : null,
                Modifier2: modifiers.Count > 1 ? modifiers[1] : null)),
            retained?.LooseStoneAddress ?? ChronicleState.InitialLooseStoneAddress,
            current.IncarnationId,
            current.IncarnationLife,
            current.WorldGrammarVersion,
            retained?.Home,
            firstConflict,
            retained?.BellAddress ?? SkyStratum.LandmarkAddress,
            current.Combat,
            current.PowerHome,
            current.Attunement,
            new AgentCollectionState((current.Agents ?? []).Select(FromCurrentAgent)));
    }

    private static ChronicleState FromVersion8Save(Version8ChronicleState current)
    {
        var retained = current.RetainedDurables;
        var firstConflict = retained?.RivenCairn is { } cairn
            ? new FirstConflictState(
                FirstConflictSubjects.RiverWardSubjectId,
                cairn.Address,
                checked(cairn.ResolvedTick - 1),
                PendingAction: null,
                Outcome: FirstConflictOutcome.Shattered,
                ResolvedTick: cairn.ResolvedTick,
                ResolvingIncarnationId: cairn.ResolvingIncarnationId)
            : null;
        var modifiers = current.Loadout.Modifiers ?? [];
        if (modifiers.Count > 2)
        {
            throw new InvalidOperationException("Version 8 supports at most two Modifiers in its one active Expression.");
        }

        return new ChronicleState(
            current.Seed,
            current.Tick,
            current.Address,
            current.Speed,
            current.Intent,
            current.Codex,
            new StudyState(),
            new LoadoutState(new LoadoutSlot(
                current.Loadout.Verb,
                Modifier: modifiers.Count > 0 ? modifiers[0] : null,
                Modifier2: modifiers.Count > 1 ? modifiers[1] : null)),
            retained?.LooseStoneAddress ?? ChronicleState.InitialLooseStoneAddress,
            current.IncarnationId,
            current.IncarnationLife,
            current.WorldGrammarVersion,
            retained?.Home,
            firstConflict,
            retained?.BellAddress ?? SkyStratum.LandmarkAddress,
            current.Combat,
            current.PowerHome,
            current.Attunement,
            new AgentCollectionState((current.Agents ?? []).Select(FromVersion8Agent)));
    }

    private static ChronicleState FromVersion7Save(Version7ChronicleState current)
    {
        var retained = current.RetainedDurables;
        var firstConflict = retained?.RivenCairn is { } cairn
            ? new FirstConflictState(
                FirstConflictSubjects.RiverWardSubjectId,
                cairn.Address,
                checked(cairn.ResolvedTick - 1),
                PendingAction: null,
                Outcome: FirstConflictOutcome.Shattered,
                ResolvedTick: cairn.ResolvedTick,
                ResolvingIncarnationId: cairn.ResolvingIncarnationId)
            : null;
        var modifiers = current.Loadout.Modifiers ?? [];
        if (modifiers.Count > 2)
        {
            throw new InvalidOperationException("Version 7 supports at most two Modifiers in its one active Expression.");
        }

        return new ChronicleState(
            current.Seed,
            current.Tick,
            current.Address,
            current.Speed,
            current.Intent,
            current.Codex,
            new StudyState(),
            new LoadoutState(new LoadoutSlot(
                current.Loadout.Verb,
                Modifier: modifiers.Count > 0 ? modifiers[0] : null,
                Modifier2: modifiers.Count > 1 ? modifiers[1] : null)),
            retained?.LooseStoneAddress ?? ChronicleState.InitialLooseStoneAddress,
            current.IncarnationId,
            current.IncarnationLife,
            current.WorldGrammarVersion,
            retained?.Home,
            firstConflict,
            retained?.BellAddress ?? SkyStratum.LandmarkAddress,
            current.Combat,
            current.PowerHome,
            current.Attunement,
            Agents: default);
    }

    private static CurrentAgentState ToCurrentAgent(AgentState agent) => new(
        agent.Profile,
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
        agent.PendingDirective,
        Array.AsReadOnly(agent.DirectiveMemories.ToArray()));

    private static AgentState FromCurrentAgent(CurrentAgentState agent) => new(
        agent.Profile,
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
        agent.PendingDirective,
        new DirectiveMemoryCollectionState(agent.DirectiveMemories ?? []));

    private static Version8AgentState ToVersion8Agent(AgentState agent) => new(
        agent.Profile,
        agent.Address,
        agent.WaitingAddress,
        agent.Presence,
        agent.Need,
        agent.HomeRelationship,
        agent.Intent,
        agent.PromotedTick,
        agent.ArrivalTick,
        agent.WelcomeOfferedTick,
        agent.RoadRollAddress);

    private static AgentState FromVersion8Agent(Version8AgentState agent) => new(
        agent.Profile,
        agent.Address,
        agent.WaitingAddress,
        agent.Presence,
        agent.Need,
        agent.HomeRelationship,
        agent.Intent,
        agent.PromotedTick,
        agent.ArrivalTick,
        agent.WelcomeOfferedTick,
        agent.RoadRollAddress);

    private static RetainedDurablesState? ToRetainedDurables(ChronicleState state)
    {
        if (!NeedsRetainedDurables(state))
        {
            return null;
        }

        return new RetainedDurablesState(
            state.LooseStoneAddress!.Value,
            state.BellAddress!.Value,
            state.Home,
            state.FirstConflict is
            {
                Outcome: FirstConflictOutcome.Shattered,
                ResolvedTick: { } resolvedTick,
                ResolvingIncarnationId: { } resolvingIncarnationId,
            } conflict
                ? new RivenCairnDurableState(
                    conflict.Address,
                    resolvedTick,
                    resolvingIncarnationId)
                : null);
    }

    private static SuccessorChronicleState ToSuccessorSave(ChronicleState state)
    {
        var slot = state.ActiveLoadout.Slots.FirstOrDefault(candidate => candidate.Verb is not null);
        var retained = ToRetainedDurables(state);
        return new SuccessorChronicleState(
            state.Seed,
            state.Tick,
            state.Address,
            state.Speed,
            state.Intent,
            state.Codex,
            new SuccessorLoadoutState(slot.Verb, slot.Modifier),
            state.IncarnationId,
            state.IncarnationLife,
            state.WorldGrammarVersion,
            state.Combat,
            retained);
    }

    private static ChronicleState FromSuccessorSave(SuccessorChronicleState successor)
    {
        var retained = successor.RetainedDurables;
        var firstConflict = retained?.RivenCairn is { } cairn
            ? new FirstConflictState(
                FirstConflictSubjects.RiverWardSubjectId,
                cairn.Address,
                checked(cairn.ResolvedTick - 1),
                PendingAction: null,
                Outcome: FirstConflictOutcome.Shattered,
                ResolvedTick: cairn.ResolvedTick,
                ResolvingIncarnationId: cairn.ResolvingIncarnationId)
            : null;
        return new ChronicleState(
            successor.Seed,
            successor.Tick,
            successor.Address,
            successor.Speed,
            successor.Intent,
            successor.Codex,
            new StudyState(),
            new LoadoutState(new LoadoutSlot(successor.Loadout.Verb, Modifier: successor.Loadout.Modifier)),
            retained?.LooseStoneAddress ?? ChronicleState.InitialLooseStoneAddress,
            successor.IncarnationId,
            successor.IncarnationLife,
            successor.WorldGrammarVersion,
            retained?.Home,
            firstConflict,
            retained?.BellAddress ?? SkyStratum.LandmarkAddress,
            successor.Combat,
            PowerHome: null,
            Attunement: new LoadAttunementState(HoldingFacts.InherentLoadCapacity, 0));
    }

    private static bool NeedsRetainedDurables(ChronicleState state) =>
        state.WorldGrammarVersion != 4 ||
        state.Home is not null ||
        state.FirstConflict is not null ||
        state.LooseStoneAddress != ChronicleState.InitialLooseStoneAddress ||
        state.BellAddress != SkyStratum.LandmarkAddress;

    internal static string SerializeVersion7ForVerification(ChronicleState state)
    {
        var current = state.MigrateAndValidate();
        ValidateCurrentState(current);
        var slot = current.ActiveLoadout.Slots.FirstOrDefault(candidate => candidate.Verb is not null);
        var version7 = new Version7ChronicleState(
            current.Seed,
            current.Tick,
            current.Address,
            current.Speed,
            current.Intent,
            current.Codex,
            new CurrentLoadoutState(slot.Verb, Array.AsReadOnly(slot.Modifiers.ToArray())),
            current.Attunement,
            current.IncarnationId,
            current.IncarnationLife,
            current.WorldGrammarVersion,
            current.Combat,
            current.PowerHome,
            ToRetainedDurables(current));
        return JsonSerializer.Serialize(new Version7SaveEnvelope(7, version7), Options);
    }

    internal static string SerializeVersion8ForVerification(ChronicleState state)
    {
        var current = state.MigrateAndValidate();
        ValidateCurrentState(current);
        var slot = current.ActiveLoadout.Slots.FirstOrDefault(candidate => candidate.Verb is not null);
        var version8 = new Version8ChronicleState(
            current.Seed,
            current.Tick,
            current.Address,
            current.Speed,
            current.Intent,
            current.Codex,
            new CurrentLoadoutState(slot.Verb, Array.AsReadOnly(slot.Modifiers.ToArray())),
            current.Attunement,
            current.IncarnationId,
            current.IncarnationLife,
            current.WorldGrammarVersion,
            current.Combat,
            current.PowerHome,
            ToRetainedDurables(current),
            Array.AsReadOnly(current.Agents.Select(ToVersion8Agent).ToArray()));
        return JsonSerializer.Serialize(new Version8SaveEnvelope(8, version8), Options);
    }

    private sealed record CurrentSaveEnvelope(int Version, CurrentChronicleState Chronicle);

    private sealed record Version8SaveEnvelope(int Version, Version8ChronicleState Chronicle);

    private sealed record Version7SaveEnvelope(int Version, Version7ChronicleState Chronicle);

    private sealed record CurrentChronicleState(
        long Seed,
        long Tick,
        WorldAddress Address,
        ChronicleSpeed Speed,
        OpeningIntent Intent,
        CodexState Codex,
        CurrentLoadoutState Loadout,
        LoadAttunementState? Attunement,
        long IncarnationId,
        IncarnationLifeState IncarnationLife,
        int WorldGrammarVersion,
        CombatState? Combat,
        PowerHomeState? PowerHome,
        RetainedDurablesState? RetainedDurables,
        IReadOnlyList<CurrentAgentState> Agents);

    private sealed record Version8ChronicleState(
        long Seed,
        long Tick,
        WorldAddress Address,
        ChronicleSpeed Speed,
        OpeningIntent Intent,
        CodexState Codex,
        CurrentLoadoutState Loadout,
        LoadAttunementState? Attunement,
        long IncarnationId,
        IncarnationLifeState IncarnationLife,
        int WorldGrammarVersion,
        CombatState? Combat,
        PowerHomeState? PowerHome,
        RetainedDurablesState? RetainedDurables,
        IReadOnlyList<Version8AgentState> Agents);

    private sealed record CurrentAgentState(
        AgentProfile Profile,
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
        PendingDirectiveState? PendingDirective,
        IReadOnlyList<DirectiveMemoryState> DirectiveMemories);

    private sealed record Version8AgentState(
        AgentProfile Profile,
        WorldAddress Address,
        WorldAddress WaitingAddress,
        AgentPresenceState Presence,
        AgentNeedState Need,
        AgentHomeRelationshipState HomeRelationship,
        AgentIntentKind Intent,
        long PromotedTick,
        long? ArrivalTick,
        long? WelcomeOfferedTick,
        WorldAddress? RoadRollAddress);

    private sealed record Version7ChronicleState(
        long Seed,
        long Tick,
        WorldAddress Address,
        ChronicleSpeed Speed,
        OpeningIntent Intent,
        CodexState Codex,
        CurrentLoadoutState Loadout,
        LoadAttunementState? Attunement,
        long IncarnationId,
        IncarnationLifeState IncarnationLife,
        int WorldGrammarVersion,
        CombatState? Combat,
        PowerHomeState? PowerHome,
        RetainedDurablesState? RetainedDurables);

    private sealed record CurrentLoadoutState(
        WordId? Verb,
        IReadOnlyList<WordId> Modifiers);

    private sealed record SuccessorSaveEnvelope(int Version, SuccessorChronicleState Chronicle);

    private sealed record SuccessorChronicleState(
        long Seed,
        long Tick,
        WorldAddress Address,
        ChronicleSpeed Speed,
        OpeningIntent Intent,
        CodexState Codex,
        SuccessorLoadoutState Loadout,
        long IncarnationId,
        IncarnationLifeState IncarnationLife,
        int WorldGrammarVersion,
        CombatState? Combat,
        RetainedDurablesState? RetainedDurables);

    private sealed record SuccessorLoadoutState(WordId? Verb, WordId? Modifier);

    private sealed record RetainedDurablesState(
        WorldAddress LooseStoneAddress,
        WorldAddress BellAddress,
        HomeState? Home,
        RivenCairnDurableState? RivenCairn);

    private sealed record RivenCairnDurableState(
        WorldAddress Address,
        long ResolvedTick,
        long ResolvingIncarnationId);

    private sealed class Version2ChronicleState
    {
        public long Seed { get; init; }
        public long Tick { get; init; }
        public WorldAddress Address { get; init; }
        public ChronicleSpeed Speed { get; init; }
        public OpeningIntent Intent { get; init; }
        public CodexState Codex { get; init; }
        public StudyState Study { get; init; }
        public LoadoutState? Loadout { get; init; }
        public WorldAddress? LooseStoneAddress { get; init; }
        public long IncarnationId { get; init; }
        public IncarnationLifeState IncarnationLife { get; init; }
        public int WorldGrammarVersion { get; init; }
    }

    private sealed class Version3ChronicleState
    {
        public long Seed { get; init; }
        public long Tick { get; init; }
        public WorldAddress Address { get; init; }
        public ChronicleSpeed Speed { get; init; }
        public OpeningIntent Intent { get; init; }
        public CodexState Codex { get; init; }
        public StudyState Study { get; init; }
        public LoadoutState? Loadout { get; init; }
        public WorldAddress? LooseStoneAddress { get; init; }
        public long IncarnationId { get; init; }
        public IncarnationLifeState IncarnationLife { get; init; }
        public int WorldGrammarVersion { get; init; }
        public HomeState? Home { get; init; }
    }

    private sealed class Version4ChronicleState
    {
        public long Seed { get; init; }
        public long Tick { get; init; }
        public WorldAddress Address { get; init; }
        public ChronicleSpeed Speed { get; init; }
        public OpeningIntent Intent { get; init; }
        public CodexState Codex { get; init; }
        public StudyState Study { get; init; }
        public LoadoutState? Loadout { get; init; }
        public WorldAddress? LooseStoneAddress { get; init; }
        public long IncarnationId { get; init; }
        public IncarnationLifeState IncarnationLife { get; init; }
        public int WorldGrammarVersion { get; init; }
        public HomeState? Home { get; init; }
        public FirstConflictState? FirstConflict { get; init; }
    }

    private sealed class PredecessorChronicleState
    {
        public long Seed { get; init; }
        public long Tick { get; init; }
        public WorldAddress Address { get; init; }
        public ChronicleSpeed Speed { get; init; }
        public OpeningIntent Intent { get; init; }
        public PredecessorCodexState? Codex { get; init; }
        public PredecessorStudyState? Study { get; init; }
        public PredecessorLoadoutState? Loadout { get; init; }
        public WorldAddress? LooseStoneAddress { get; init; }
        public long IncarnationId { get; init; }
        public IncarnationLifeState IncarnationLife { get; init; }
        public int WorldGrammarVersion { get; init; }
    }

    private sealed class PredecessorCodexState
    {
        public bool HasFly { get; init; }
        public bool HasStone { get; init; }
    }

    private sealed class PredecessorStudyState
    {
        public int StoneUnderstanding { get; init; }
        public bool IsStudyingBell { get; init; }
    }

    private sealed class PredecessorLoadoutState
    {
        public PredecessorLoadoutSlot? Slot1 { get; init; }
        public PredecessorLoadoutSlot? Slot2 { get; init; }
        public PredecessorLoadoutSlot? Slot3 { get; init; }
        public PredecessorLoadoutSlot? Slot4 { get; init; }
        public PredecessorLoadoutSlot? Slot5 { get; init; }
        public PredecessorLoadoutSlot? Slot6 { get; init; }
        public PredecessorLoadoutSlot? Slot7 { get; init; }
        public PredecessorLoadoutSlot? Slot8 { get; init; }
    }

    private sealed class PredecessorLoadoutSlot
    {
        public int? Verb { get; init; }
        public int? Noun { get; init; }
    }
}
