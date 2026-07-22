using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicle.Core;

public readonly record struct WorldAddress(string Stratum, long X, long Y)
{
    public override string ToString() => $"{Stratum} ({X}, {Y})";
}

public enum ChronicleSpeed
{
    Paused = 0,
    Slow = 1,
    Normal = 2,
    Fast = 4,
}

public enum OpeningIntent
{
    Unchosen = 0,
    Up = 1,
    Here = 2,
    Against = 3,
}

public enum IncarnationLifeState
{
    Alive = 0,
    AwaitingReplacement = 1,
}

public sealed record ChronicleState(
    long Seed,
    long Tick,
    WorldAddress Address,
    ChronicleSpeed Speed,
    OpeningIntent Intent = OpeningIntent.Unchosen,
    CodexState Codex = default,
    StudyState Study = default,
    LoadoutState? Loadout = null,
    WorldAddress? LooseStoneAddress = null,
    long IncarnationId = 1,
    IncarnationLifeState IncarnationLife = IncarnationLifeState.Alive,
    int WorldGrammarVersion = 0,
    HomeState? Home = null,
    FirstConflictState? FirstConflict = null,
    WorldAddress? BellAddress = null,
    CombatState? Combat = null,
    PowerHomeState? PowerHome = null,
    LoadAttunementState? Attunement = null)
{
    public static readonly WorldAddress InitialLooseStoneAddress =
        new(SurfacePatch.SurfaceStratum, 1, 0);
    public static readonly WorldAddress AcceptedHomeFixtureAddress =
        new(SurfacePatch.SurfaceStratum, 0, 3);
    public const string LooseStoneIdentity = "Loose Stone";
    public const string HomeHearthstoneIdentity = "The First Hearthstone";

    [JsonIgnore]
    public LoadoutState ActiveLoadout => Loadout ?? LoadoutState.InitialFor(Codex);

    [JsonIgnore]
    public bool HasLivingIncarnation => IncarnationLife == IncarnationLifeState.Alive;

    [JsonIgnore]
    public WorldAddress CurrentBellAddress => BellAddress ?? SkyStratum.LandmarkAddress;

    [JsonIgnore]
    public bool CanFly =>
        HasLivingIncarnation &&
        !Goal6BPowerComesHome.IsCarrying(this) &&
        !Goal6BPowerComesHome.HasCommitment(this) &&
        ActiveLoadout.Slots.Any(slot => slot.IsIntrinsicFly);

    public static ChronicleState Begin(long seed) => new(
        seed,
        Tick: 0,
        Address: AcceptedHomeFixtureAddress,
        Speed: ChronicleSpeed.Normal,
        Intent: OpeningIntent.Unchosen,
        Codex: new CodexState([WordIds.Found]),
        Study: new StudyState(),
        Loadout: LoadoutState.Empty,
        LooseStoneAddress: InitialLooseStoneAddress,
        IncarnationId: 1,
        IncarnationLife: IncarnationLifeState.Alive,
        WorldGrammarVersion: 5,
        Home: new HomeState(
            "holding.home",
            "The First Hearth",
            AcceptedHomeFixtureAddress,
            FoundedTick: 0,
            FoundingIncarnationId: 1,
            HomeMaterialState.HearthstoneRaised),
        BellAddress: SkyStratum.LandmarkAddress,
        Combat: CombatState.Create(seed),
        PowerHome: Goal6BPowerComesHome.Create(seed),
        Attunement: new LoadAttunementState(Goal6BPowerComesHome.InherentLoadCapacity, 0));

    public ChronicleState AdvanceTick()
    {
        if (Goal6AActionPlanning.IsAvailable(this))
        {
            return Goal6AActionPlanning.Advance(this).State;
        }

        if (!HasLivingIncarnation || Speed == ChronicleSpeed.Paused)
        {
            return this;
        }

        if (Tick >= long.MaxValue - 1)
        {
            return this with { Speed = ChronicleSpeed.Paused };
        }

        var advanced = this with { Tick = checked(Tick + 1) };
        if (advanced.FirstConflict is { Outcome: null } conflict)
        {
            return conflict.PendingAction == new LoadoutSlot(WordIds.Smash)
                ? advanced with
                {
                    FirstConflict = conflict with
                    {
                        PendingAction = null,
                        Outcome = FirstConflictOutcome.Shattered,
                        ResolvedTick = advanced.Tick,
                        ResolvingIncarnationId = advanced.IncarnationId,
                    },
                }
                : advanced with
                {
                    IncarnationLife = IncarnationLifeState.AwaitingReplacement,
                    Study = advanced.Study.Stop(),
                    FirstConflict = null,
                };
        }

        if (advanced.Study.ActiveWord is not { } activeWord ||
            advanced.Study.ActiveSourceId is not { } activeSourceId)
        {
            return advanced;
        }

        var source = StudySourceGrammar.At(advanced, advanced.Address);
        var offer = source?.Offers.FirstOrDefault(candidate => candidate.Word.Id == activeWord);
        if (source is null ||
            source.Id != activeSourceId ||
            offer is null ||
            advanced.Codex.Contains(activeWord))
        {
            return advanced with { Study = advanced.Study.Stop() };
        }

        var understanding = advanced.Study.UnderstandingFor(activeWord) + 1;
        var required = offer.Word.UnderstandingRequired;
        var capped = Math.Min(understanding, offer.UnderstandingYield);
        var study = advanced.Study.WithUnderstanding(activeWord, capped);
        if (capped < required)
        {
            return advanced with { Study = study };
        }

        return advanced with
        {
            Codex = advanced.Codex.Learn(activeWord),
            Study = study.Stop(),
        };
    }

    public ChronicleState WithSpeed(ChronicleSpeed speed) => this with { Speed = speed };

    internal ChronicleState WithIntent(OpeningIntent intent)
    {
        WordId? firstVerb = intent switch
        {
            OpeningIntent.Up => WordIds.Fly,
            OpeningIntent.Here => WordIds.Found,
            OpeningIntent.Against when WorldGrammarVersion is 4 or 5 => WordIds.Burn,
            OpeningIntent.Against => WordIds.Smash,
            _ => null,
        };
        var codex = firstVerb is { } word ? Codex.Learn(word) : Codex;
        if (intent == OpeningIntent.Against && WorldGrammarVersion is 4 or 5)
        {
            codex = codex.Learn(WordIds.Quickly).Learn(WordIds.Lasting);
        }
        var loadout = Loadout ?? LoadoutState.Empty;
        if (firstVerb is { } verb &&
            loadout.Slots.All(slot => slot.Verb != verb))
        {
            loadout = loadout.WithSlot(0, new LoadoutSlot(verb));
        }

        return this with
        {
            Intent = intent,
            Codex = codex,
            Loadout = loadout,
        };
    }

    internal ChronicleState BeginStudy(StudySourceId sourceId, WordId wordId) =>
        this with { Study = Study.Begin(sourceId, wordId) };

    internal ChronicleState TravelTo(WorldAddress address)
    {
        var moved = this with
        {
            Address = address,
            Study = address == Address ? Study : Study.Stop(),
        };

        return moved.FirstConflict is { Outcome: null } conflict && address != conflict.Address
            ? moved with { FirstConflict = null }
            : moved;
    }

    internal ChronicleState EndIncarnationAtBell()
    {
        if (!HasLivingIncarnation || Address != CurrentBellAddress)
        {
            return this;
        }

        if (Goal6AActionPlanning.IsAvailable(this))
        {
            return Goal6AActionPlanning.EndIncarnation(this);
        }

        return this with
        {
            IncarnationLife = IncarnationLifeState.AwaitingReplacement,
            Study = Study.Stop(),
            FirstConflict = FirstConflict is { Outcome: null } ? null : FirstConflict,
        };
    }

    internal ChronicleState CreateReplacementIncarnation()
    {
        if (Goal6AActionPlanning.IsAvailable(this))
        {
            return Goal6AActionPlanning.CreateReplacement(this);
        }

        if (IncarnationLife != IncarnationLifeState.AwaitingReplacement)
        {
            return this;
        }

        if (IncarnationId >= long.MaxValue - 1)
        {
            return this;
        }

        return this with
        {
            Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
            Loadout = LoadoutState.Empty,
            IncarnationId = checked(IncarnationId + 1),
            IncarnationLife = IncarnationLifeState.Alive,
            Study = Study.Stop(),
            FirstConflict = FirstConflict is { Outcome: null } ? null : FirstConflict,
        };
    }

    internal ChronicleState MigrateAndValidate()
    {
        if (WorldGrammarVersion is not (0 or 1 or 2 or 3 or 4 or 5))
        {
            throw new InvalidOperationException($"Unsupported World Grammar version '{WorldGrammarVersion}'.");
        }

        if (!Enum.IsDefined(IncarnationLife))
        {
            throw new InvalidOperationException($"Unknown Incarnation life state '{IncarnationLife}'.");
        }

        var codex = Intent switch
        {
            OpeningIntent.Up => Codex.Learn(WordIds.Fly),
            OpeningIntent.Here => Codex.Learn(WordIds.Found),
            OpeningIntent.Against when WorldGrammarVersion is 4 or 5 => Codex
                .Learn(WordIds.Burn)
                .Learn(WordIds.Quickly)
                .Learn(WordIds.Lasting),
            OpeningIntent.Against => Codex.Learn(WordIds.Smash),
            _ => Codex,
        };
        var study = Study;

        foreach (var word in WordCatalogue.Words.Where(word => word.UnderstandingRequired > 0))
        {
            if (!codex.Contains(word.Id) &&
                study.UnderstandingFor(word.Id) != word.UnderstandingRequired)
            {
                continue;
            }

            codex = codex.Learn(word.Id);
            study = study
                .WithUnderstanding(word.Id, word.UnderstandingRequired)
                .StopIfActive(word.Id);
        }

        if (study.ActiveSourceId is { } activeSourceId &&
            study.ActiveWord is { } activeWord)
        {
            var source = StudySourceGrammar.At(
                this with { Codex = codex, Study = study },
                Address);
            if (source is null ||
                source.Id != activeSourceId ||
                source.Offers.All(offer => offer.Word.Id != activeWord) ||
                codex.Contains(activeWord))
            {
                study = study.Stop();
            }
        }

        if (IncarnationLife == IncarnationLifeState.AwaitingReplacement)
        {
            study = study.Stop();
        }

        var loadout = Loadout ?? LoadoutState.InitialFor(codex);
        if (WorldGrammarVersion is not (4 or 5))
        {
            (codex, study, loadout) = RetirePredecessorNouns(codex, study, loadout);
        }

        loadout.Validate(codex);

        var looseStoneAddress = LooseStoneAddress ?? InitialLooseStoneAddress;
        var bellAddress = BellAddress ?? SkyStratum.LandmarkAddress;
        if (!string.Equals(
                looseStoneAddress.Stratum,
                SurfacePatch.SurfaceStratum,
                StringComparison.Ordinal) &&
            !string.Equals(
                looseStoneAddress.Stratum,
                SkyStratum.StratumName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The loose Stone must be in the surface or sky Stratum.");
        }

        return this with
        {
            Codex = codex,
            Study = study,
            Loadout = loadout,
            LooseStoneAddress = looseStoneAddress,
            BellAddress = bellAddress,
            IncarnationId = IncarnationId <= 0 ? 1 : IncarnationId,
            FirstConflict = WorldGrammarVersion is 4 or 5
                ? null
                : FirstConflict is { Outcome: FirstConflictOutcome.Shattered }
                    ? FirstConflict
                    : null,
            Combat = WorldGrammarVersion is 4 or 5
                ? Combat ?? CombatState.Create(Seed)
                : null,
            PowerHome = WorldGrammarVersion == 5
                ? PowerHome ?? Goal6BPowerComesHome.Create(Seed)
                : null,
            Attunement = WorldGrammarVersion == 5
                ? Attunement
                : Attunement ?? new LoadAttunementState(
                    Goal6BPowerComesHome.InherentLoadCapacity,
                    Tick: 0),
        };
    }

    private static (CodexState Codex, StudyState Study, LoadoutState Loadout) RetirePredecessorNouns(
        CodexState codex,
        StudyState study,
        LoadoutState loadout)
    {
        var successorCodex = new CodexState(codex.Words.Where(word =>
            word != WordIds.Stone && word != WordIds.Bell));
        var successorStudy = new StudyState(
            study.Understanding.Where(entry =>
                entry.Word != WordIds.Stone && entry.Word != WordIds.Bell));
        var successorLoadout = LoadoutState.Empty;
        for (var index = 0; index < LoadoutState.SlotCount; index++)
        {
            var slot = loadout[index];
            if (slot.Verb is null)
            {
                continue;
            }

            // The only accepted fitted predecessor expression is Fly[Noun].
            // It retains Fly as an intrinsic successor capability; the Noun is
            // deliberately not remapped to a Modifier.
            successorLoadout = successorLoadout.WithSlot(
                index,
                new LoadoutSlot(slot.Verb, Modifier: slot.Modifier));
        }

        return (successorCodex, successorStudy, successorLoadout);
    }
}

public static class ChronicleSaveCodec
{
    public const int CurrentVersion = 7;

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
        var current = chronicleElement.Deserialize<CurrentChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 7 Chronicle save data was empty.");
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
            retained);
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
            current.Attunement);
    }

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
            Attunement: new LoadAttunementState(Goal6BPowerComesHome.InherentLoadCapacity, 0));
    }

    private static bool NeedsRetainedDurables(ChronicleState state) =>
        state.WorldGrammarVersion != 4 ||
        state.Home is not null ||
        state.FirstConflict is not null ||
        state.LooseStoneAddress != ChronicleState.InitialLooseStoneAddress ||
        state.BellAddress != SkyStratum.LandmarkAddress;

    private static ChronicleState MigratePredecessor(
        PredecessorChronicleState predecessor,
        bool migrateWorldGrammarOne)
    {
        if (predecessor.Intent is not (OpeningIntent.Unchosen or OpeningIntent.Up))
        {
            throw new InvalidOperationException(
                "Predecessor Chronicle saves only support Unchosen or UP Intent.");
        }

        if (predecessor.WorldGrammarVersion is not (0 or 1 or 2))
        {
            throw new InvalidOperationException(
                "Predecessor Chronicle saves only support World Grammar pins 0, 1, or 2.");
        }

        var codex = new CodexState(
            predecessor.Codex?.HasFly ?? false,
            predecessor.Codex?.HasStone ?? false);
        var study = new StudyState(
            predecessor.Study?.StoneUnderstanding ?? 0,
            predecessor.Study?.IsStudyingBell ?? false);
        var loadout = predecessor.Loadout is null
            ? (LoadoutState?)null
            : new LoadoutState(
                MigrateSlot(predecessor.Loadout.Slot1),
                MigrateSlot(predecessor.Loadout.Slot2),
                MigrateSlot(predecessor.Loadout.Slot3),
                MigrateSlot(predecessor.Loadout.Slot4),
                MigrateSlot(predecessor.Loadout.Slot5),
                MigrateSlot(predecessor.Loadout.Slot6),
                MigrateSlot(predecessor.Loadout.Slot7),
                MigrateSlot(predecessor.Loadout.Slot8));
        var worldGrammarVersion =
            migrateWorldGrammarOne && predecessor.WorldGrammarVersion == 1
                ? 2
                : predecessor.WorldGrammarVersion;

        return new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            codex,
            study,
            loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            worldGrammarVersion).MigrateAndValidate();
    }

    private static ChronicleState MigrateVersion2(Version2ChronicleState predecessor)
    {
        if (predecessor.Intent is not (OpeningIntent.Unchosen or OpeningIntent.Up))
        {
            throw new InvalidOperationException(
                "Version 2 Chronicle saves only support Unchosen or UP Intent.");
        }

        if (predecessor.WorldGrammarVersion is not (0 or 1 or 2))
        {
            throw new InvalidOperationException(
                "Version 2 Chronicle saves only support World Grammar pins 0, 1, or 2.");
        }

        var state = new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            predecessor.Codex,
            predecessor.Study,
            predecessor.Loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            predecessor.WorldGrammarVersion,
            Home: null);
        var migrated = state.MigrateAndValidate();
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static ChronicleState MigrateVersion3(Version3ChronicleState predecessor)
    {
        if (predecessor.Intent == OpeningIntent.Against ||
            predecessor.WorldGrammarVersion is not (0 or 1 or 2))
        {
            throw new InvalidOperationException(
                "Version 3 Chronicle saves only support World Grammar pins 0, 1, or 2 without AGAINST.");
        }

        var migrated = new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            predecessor.Codex,
            predecessor.Study,
            predecessor.Loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            predecessor.WorldGrammarVersion,
            predecessor.Home,
            FirstConflict: null,
            BellAddress: SkyStratum.LandmarkAddress).MigrateAndValidate();
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static ChronicleState MigrateVersion4(Version4ChronicleState predecessor)
    {
        var migrated = new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            predecessor.Codex,
            predecessor.Study,
            predecessor.Loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            predecessor.WorldGrammarVersion,
            predecessor.Home,
            predecessor.FirstConflict,
            BellAddress: SkyStratum.LandmarkAddress).MigrateAndValidate();
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static LoadoutSlot MigrateSlot(PredecessorLoadoutSlot? slot)
    {
        if (slot is null)
        {
            return new LoadoutSlot();
        }

        var verb = slot.Verb switch
        {
            null => (WordId?)null,
            1 => WordIds.Fly,
            _ => throw new InvalidOperationException(
                $"Unknown predecessor Loadout Verb value '{slot.Verb}'."),
        };
        var noun = slot.Noun switch
        {
            null => (WordId?)null,
            1 => WordIds.Stone,
            _ => throw new InvalidOperationException(
                $"Unknown predecessor Loadout Noun value '{slot.Noun}'."),
        };
        return new LoadoutSlot(verb, noun);
    }

    private static void ValidateVersion3Document(JsonElement chronicle)
    {
        ValidateCurrentDocument(chronicle);
        ValidateAllowedWordIdentities(
            chronicle,
            "Version 3",
            WordIds.Fly,
            WordIds.Found,
            WordIds.Stone,
            WordIds.Bell);
    }

    private static void ValidateCurrentDocument(JsonElement chronicle)
    {
        RequireObjectWithProperties(
            chronicle,
            "Chronicle",
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
        RequireExactObjectWithProperties(
            chronicle.GetProperty("Address"),
            "Chronicle Address",
            "Stratum",
            "X",
            "Y");
        RequireExactObjectWithProperties(
            chronicle.GetProperty("LooseStoneAddress"),
            "loose-Stone Address",
            "Stratum",
            "X",
            "Y");

        var codex = chronicle.GetProperty("Codex");
        RequireExactObjectWithProperties(codex, "Codex", "Words");
        ValidateCodexWords(codex.GetProperty("Words"));
        var study = chronicle.GetProperty("Study");
        RequireExactObjectWithProperties(
            study,
            "Study",
            "Understanding",
            "ActiveSourceId",
            "ActiveWord");
        var understanding = study.GetProperty("Understanding");
        ValidateUnderstandingEntries(understanding);

        var loadout = chronicle.GetProperty("Loadout");
        RequireExactObjectWithProperties(
            loadout,
            "Loadout",
            "Slot1",
            "Slot2",
            "Slot3",
            "Slot4",
            "Slot5",
            "Slot6",
            "Slot7",
            "Slot8");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            RequireExactObjectWithProperties(
                loadout.GetProperty($"Slot{index}"),
                $"Loadout slot {index}",
                "Verb",
                "Noun");
        }

        var home = chronicle.GetProperty("Home");

        if (home.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            home,
            "Home",
            "HoldingId",
            "DisplayName",
            "Address",
            "FoundedTick",
            "FoundingIncarnationId",
            "Material");
        RequireExactObjectWithProperties(
            home.GetProperty("Address"),
            "Home Address",
            "Stratum",
            "X",
            "Y");
    }

    private static void ValidateVersion4Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Chronicle",
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
            "Home",
            "FirstConflict");
        ValidateCurrentDocument(chronicle);
        ValidateConflictDocument(chronicle.GetProperty("FirstConflict"));
    }

    private static void ValidateVersion5Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Chronicle",
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
            "Home",
            "FirstConflict",
            "BellAddress");
        ValidateCurrentDocument(chronicle);
        ValidateConflictDocument(chronicle.GetProperty("FirstConflict"));
        ValidateAllowedWordIdentities(
            chronicle,
            "Version 5",
            WordIds.Fly,
            WordIds.Found,
            WordIds.Smash,
            WordIds.Stone,
            WordIds.Bell);
        if (!chronicle.GetProperty("WorldGrammarVersion").TryGetInt32(out var grammarVersion) ||
            grammarVersion is < 0 or > 3)
        {
            throw new InvalidOperationException("Version 5 saves support only predecessor World Grammar pins 0 through 3.");
        }
        RequireExactObjectWithProperties(
            chronicle.GetProperty("BellAddress"),
            "Bell Address",
            "Stratum",
            "X",
            "Y");
    }

    private static void ValidateVersion6Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Version 6 Chronicle",
            "Seed",
            "Tick",
            "Address",
            "Speed",
            "Intent",
            "Codex",
            "Loadout",
            "IncarnationId",
            "IncarnationLife",
            "WorldGrammarVersion",
            "Combat",
            "RetainedDurables");
        ValidateV6Address(chronicle.GetProperty("Address"), "Chronicle Address");

        var codex = chronicle.GetProperty("Codex");
        RequireExactObjectWithProperties(codex, "Codex", "Words");
        ValidateCodexWords(codex.GetProperty("Words"));
        foreach (var wordElement in codex.GetProperty("Words").EnumerateArray())
        {
            var word = ReadKnownWordId(wordElement, "Version 6 Codex Word");
            if (WordCatalogue.Get(word).Kind == WordKind.Noun)
            {
                throw new InvalidOperationException(
                    $"Version 6 Codex cannot retain predecessor Noun '{word}'.");
            }
        }

        ValidateV6Loadout(chronicle.GetProperty("Loadout"), "Loadout");
        ValidateV6Combat(chronicle.GetProperty("Combat"), expressionHasSecondModifier: false);
        ValidateV6RetainedDurables(chronicle.GetProperty("RetainedDurables"));
    }

    private static void ValidateVersion7Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Version 7 Chronicle",
            "Seed", "Tick", "Address", "Speed", "Intent", "Codex", "Loadout",
            "Attunement", "IncarnationId", "IncarnationLife", "WorldGrammarVersion",
            "Combat", "PowerHome", "RetainedDurables");
        ValidateV6Address(chronicle.GetProperty("Address"), "Chronicle Address");

        var codex = chronicle.GetProperty("Codex");
        RequireExactObjectWithProperties(codex, "Codex", "Words");
        ValidateCodexWords(codex.GetProperty("Words"));
        foreach (var wordElement in codex.GetProperty("Words").EnumerateArray())
        {
            var word = ReadKnownWordId(wordElement, "Version 7 Codex Word");
            if (WordCatalogue.Get(word).Kind == WordKind.Noun)
            {
                throw new InvalidOperationException(
                    $"Version 7 Codex cannot retain predecessor Noun '{word}'.");
            }
        }

        var loadout = chronicle.GetProperty("Loadout");
        RequireExactObjectWithProperties(loadout, "Version 7 Loadout", "Verb", "Modifiers");
        RequireArray(loadout.GetProperty("Modifiers"), "Version 7 Loadout Modifiers");
        if (loadout.GetProperty("Modifiers").GetArrayLength() > 2)
        {
            throw new InvalidOperationException("Version 7 Loadout supports at most two Modifiers.");
        }

        var modifierIds = loadout.GetProperty("Modifiers")
            .EnumerateArray()
            .Select(element => ReadKnownWordId(element, "Version 7 Loadout Modifier"))
            .ToArray();
        if (modifierIds.Any(id => WordCatalogue.Get(id).Kind != WordKind.Modifier) ||
            modifierIds.Distinct().Count() != modifierIds.Length ||
            !WordCatalogue.Canonicalize(modifierIds).SequenceEqual(modifierIds))
        {
            throw new InvalidOperationException(
                "Version 7 Loadout Modifiers must be known, unique, and in canonical order.");
        }

        var attunement = chronicle.GetProperty("Attunement");
        if (attunement.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(attunement, "Attunement", "Capacity", "Tick");
        }

        ValidateV6Combat(chronicle.GetProperty("Combat"), expressionHasSecondModifier: true);
        ValidateV7PowerHome(chronicle.GetProperty("PowerHome"));
        ValidateV6RetainedDurables(chronicle.GetProperty("RetainedDurables"));
    }

    private static void ValidateV7PowerHome(JsonElement power)
    {
        if (power.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            power,
            "Power Comes Home state",
            "Lode", "ExtractionProgress", "Resonator", "Commitment");
        var lode = power.GetProperty("Lode");
        RequireExactObjectWithProperties(
            lode,
            "Resonant Lode",
            "Identity", "OriginAddress", "Disposition", "Address", "CarrierIncarnationId");
        ValidateV6Address(lode.GetProperty("OriginAddress"), "Resonant Lode origin Address");
        if (lode.GetProperty("Address").ValueKind != JsonValueKind.Null)
        {
            ValidateV6Address(lode.GetProperty("Address"), "Resonant Lode Address");
        }

        var source = power.GetProperty("Resonator");
        if (source.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                source,
                "Hearth Resonator",
                "Identity", "Address", "Phase", "Progress");
            ValidateV6Address(source.GetProperty("Address"), "Hearth Resonator Address");
        }

        var commitment = power.GetProperty("Commitment");
        if (commitment.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                commitment,
                "Power Comes Home commitment",
                "Kind", "ActorIncarnationId", "SubjectIdentity", "Address", "CompletedTicks", "TotalTicks");
            ValidateV6Address(commitment.GetProperty("Address"), "Power commitment Address");
        }
    }

    private static void ValidateV6Loadout(JsonElement loadout, string name)
    {
        RequireExactObjectWithProperties(
            loadout,
            name,
            "Verb",
            "Modifier");
    }

    private static void ValidateV6Address(JsonElement address, string name)
    {
        RequireExactObjectWithProperties(address, name, "Stratum", "X", "Y");
    }

    private static void ValidateV6RetainedDurables(JsonElement retained)
    {
        if (retained.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            retained,
            "retained predecessor durables",
            "LooseStoneAddress",
            "BellAddress",
            "Home",
            "RivenCairn");
        ValidateV6Address(retained.GetProperty("LooseStoneAddress"), "retained loose-Stone Address");
        ValidateV6Address(retained.GetProperty("BellAddress"), "retained Bell Address");
        var home = retained.GetProperty("Home");
        if (home.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                home,
                "retained Home",
                "HoldingId",
                "DisplayName",
                "Address",
                "FoundedTick",
                "FoundingIncarnationId",
                "Material");
            ValidateV6Address(home.GetProperty("Address"), "retained Home Address");
        }

        var cairn = retained.GetProperty("RivenCairn");
        if (cairn.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                cairn,
                "retained Riven Cairn",
                "Address",
                "ResolvedTick",
                "ResolvingIncarnationId");
            ValidateV6Address(cairn.GetProperty("Address"), "retained Riven Cairn Address");
        }
    }

    private static void ValidateV6Combat(JsonElement combat, bool expressionHasSecondModifier)
    {
        if (combat.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            combat,
            "Goal 6A Combat",
            "IncarnationHitPoints",
            "Equipment",
            "EngagementPlan",
            "WeaponStanceActive",
            "WeaponTicksUntilReady",
            "EngagementActive",
            "MireBrute",
            "PendingAction",
            "Preparation",
            "OngoingBurn",
            "RecoveryRemaining",
            "Scorch");
        RequireExactObjectWithProperties(
            combat.GetProperty("Equipment"),
            "Goal 6A Equipment",
            "WeaponIdentity",
            "WeaponName",
            "ArmorIdentity",
            "ArmorName",
            "AccessoryIdentity",
            "AccessoryName",
            "MaximumHitPointBonus",
            "PhysicalDamageReduction");
        RequireExactObjectWithProperties(
            combat.GetProperty("EngagementPlan"),
            "Engagement Plan",
            "OpenWithWeaponStance");
        var brute = combat.GetProperty("MireBrute");
        RequireExactObjectWithProperties(
            brute,
            "Mire Brute",
            "Identity",
            "OriginAddress",
            "Address",
            "HitPoints",
            "SwingTicksRemaining",
            "DefeatedTick");
        ValidateV6Address(brute.GetProperty("OriginAddress"), "Mire Brute origin Address");
        ValidateV6Address(brute.GetProperty("Address"), "Mire Brute Address");

        var pending = combat.GetProperty("PendingAction");
        if (pending.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                pending,
                "pending tactical action",
                "Kind",
                "DeltaX",
                "DeltaY",
                "WeaponStanceActive",
                "Target");
            if (pending.GetProperty("Target").ValueKind != JsonValueKind.Null)
            {
                ValidateV6Address(pending.GetProperty("Target"), "pending tactical Target");
            }
        }

        var preparation = combat.GetProperty("Preparation");
        if (preparation.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                preparation,
                "Burn Preparation",
                "ActorIncarnationId",
                "TargetIdentity",
                "TargetAddressAtPreparation",
                "Expression",
                "RemainingTicks");
            ValidateV6Address(
                preparation.GetProperty("TargetAddressAtPreparation"),
                "Burn Preparation Target Address");
            if (expressionHasSecondModifier)
            {
                RequireExactObjectWithProperties(
                    preparation.GetProperty("Expression"),
                    "Burn Preparation Expression",
                    "Verb",
                    "Noun",
                    "Modifier",
                    "Modifier2");
            }
            else
            {
                RequireExactObjectWithProperties(
                    preparation.GetProperty("Expression"),
                    "Burn Preparation Expression",
                    "Verb",
                    "Noun",
                    "Modifier");
            }
        }

        var burn = combat.GetProperty("OngoingBurn");
        if (burn.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                burn,
                "ongoing Burn",
                "TargetIdentity",
                "Damage",
                "RemainingTicks");
        }

        var scorch = combat.GetProperty("Scorch");
        if (scorch.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(scorch, "scorched ground", "Address", "CreatedTick");
            ValidateV6Address(scorch.GetProperty("Address"), "scorched ground Address");
        }
    }

    private static void ValidatePreVersion5LoadoutCompatibility(
        JsonElement chronicle,
        string saveName)
    {
        var loadout = chronicle.GetProperty("Loadout");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            var slot = loadout.GetProperty($"Slot{index}");
            var verb = slot.GetProperty("Verb");
            var noun = slot.GetProperty("Noun");
            if (verb.ValueKind == JsonValueKind.String &&
                noun.ValueKind == JsonValueKind.String)
            {
                var verbId = new WordId(verb.GetString()!);
                var nounId = new WordId(noun.GetString()!);
                if (!PreVersion5CompatibleNouns.TryGetValue(verbId, out var compatibleNouns) ||
                    !compatibleNouns.Contains(nounId))
                {
                    throw new InvalidOperationException(
                        $"{saveName} saves cannot contain later fitted compatibility '{verbId}[{nounId}]'.");
                }
            }
        }
    }

    private static void ValidateConflictDocument(JsonElement conflict)
    {
        if (conflict.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            conflict,
            "First Conflict",
            "SubjectId",
            "Address",
            "ThreatenedTick",
            "PendingAction",
            "Outcome",
            "ResolvedTick",
            "ResolvingIncarnationId");
        RequireExactObjectWithProperties(
            conflict.GetProperty("Address"),
            "First Conflict Address",
            "Stratum",
            "X",
            "Y");

        var pendingAction = conflict.GetProperty("PendingAction");
        if (pendingAction.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                pendingAction,
                "First Conflict pending Loadout action",
                "Verb",
                "Noun");
        }

        RequireNullableNumber(conflict.GetProperty("Outcome"), "First Conflict outcome");
        RequireNullableNumber(conflict.GetProperty("ResolvedTick"), "First Conflict resolution tick");
        RequireNullableNumber(
            conflict.GetProperty("ResolvingIncarnationId"),
            "First Conflict resolving Incarnation identity");
    }

    private static void ValidateAllowedWordIdentities(
        JsonElement chronicle,
        string saveName,
        params WordId[] allowedWords)
    {
        void ValidateWord(JsonElement element, string field)
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                return;
            }

            var word = ReadKnownWordId(element, $"{saveName} {field}");
            if (!allowedWords.Contains(word))
            {
                throw new InvalidOperationException(
                    $"{saveName} saves cannot contain later Word identity '{word}'.");
            }
        }

        foreach (var word in chronicle
                     .GetProperty("Codex")
                     .GetProperty("Words")
                     .EnumerateArray())
        {
            ValidateWord(word, "Codex Word");
        }

        var study = chronicle.GetProperty("Study");
        foreach (var entry in study.GetProperty("Understanding").EnumerateArray())
        {
            ValidateWord(entry.GetProperty("Word"), "Study Understanding Word");
        }

        ValidateWord(study.GetProperty("ActiveWord"), "active Study Word");

        var loadout = chronicle.GetProperty("Loadout");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            var slot = loadout.GetProperty($"Slot{index}");
            foreach (var property in new[] { "Verb", "Noun" })
            {
                ValidateWord(slot.GetProperty(property), $"Loadout {property}");
            }
        }
    }

    private static void ValidateVersion2Document(JsonElement chronicle)
    {
        RequireObjectWithProperties(
            chronicle,
            "Chronicle",
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
            "WorldGrammarVersion");
        RequireObjectWithProperties(
            chronicle.GetProperty("Address"),
            "Chronicle Address",
            "Stratum",
            "X",
            "Y");
        RequireObjectWithProperties(
            chronicle.GetProperty("LooseStoneAddress"),
            "loose-Stone Address",
            "Stratum",
            "X",
            "Y");

        var loadout = chronicle.GetProperty("Loadout");
        RequireObjectWithProperties(
            loadout,
            "Loadout",
            "Slot1",
            "Slot2",
            "Slot3",
            "Slot4",
            "Slot5",
            "Slot6",
            "Slot7",
            "Slot8");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            RequireObjectWithProperties(
                loadout.GetProperty($"Slot{index}"),
                $"Loadout slot {index}",
                "Verb",
                "Noun");
        }

        ValidateAllowedWordIdentities(
            chronicle,
            "Version 2",
            WordIds.Fly,
            WordIds.Stone,
            WordIds.Bell);
    }

    private static void ValidatePredecessorDocument(
        JsonElement chronicle,
        string name,
        bool requireIntentAndGrammar)
    {
        RequireObjectWithProperties(
            chronicle,
            name,
            "Seed",
            "Tick",
            "Address",
            "Speed");
        if (requireIntentAndGrammar)
        {
            RequireObjectWithProperties(
                chronicle,
                name,
                "Intent",
                "WorldGrammarVersion");
        }

        RequireObjectWithProperties(
            chronicle.GetProperty("Address"),
            $"{name} Address",
            "Stratum",
            "X",
            "Y");

        if (chronicle.TryGetProperty("LooseStoneAddress", out var looseStone) &&
            looseStone.ValueKind != JsonValueKind.Null)
        {
            RequireObjectWithProperties(
                looseStone,
                $"{name} loose-Stone Address",
                "Stratum",
                "X",
                "Y");
        }
    }

    private static void RequireObjectWithProperties(
        JsonElement element,
        string name,
        params string[] properties)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{name} must be a JSON object.");
        }

        foreach (var property in properties)
        {
            if (!element.TryGetProperty(property, out _))
            {
                throw new InvalidOperationException(
                    $"{name} was missing required field '{property}'.");
            }
        }
    }

    private static void RequireExactObjectWithProperties(
        JsonElement element,
        string name,
        params string[] properties)
    {
        RequireObjectWithProperties(element, name, properties);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!properties.Contains(property.Name, StringComparer.Ordinal) ||
                !seen.Add(property.Name))
            {
                throw new InvalidOperationException(
                    $"{name} contained unexpected field '{property.Name}'.");
            }
        }
    }

    private static void RequireArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"{name} must be a JSON array.");
        }
    }

    private static void RequireNullableNumber(JsonElement element, string name)
    {
        if (element.ValueKind is not (JsonValueKind.Null or JsonValueKind.Number))
        {
            throw new InvalidOperationException($"{name} must be a number or null.");
        }
    }

    private static void ValidateCodexWords(JsonElement words)
    {
        RequireArray(words, "Codex Words");
        var seen = new HashSet<WordId>();
        var canonicalWords = WordCatalogue.Words.Select(word => word.Id).ToArray();
        var previousCatalogueIndex = -1;
        foreach (var element in words.EnumerateArray())
        {
            var word = ReadKnownWordId(element, "Codex Word");
            if (!seen.Add(word))
            {
                throw new InvalidOperationException(
                    $"Codex Words contained duplicate identity '{word}'.");
            }

            var catalogueIndex = Array.IndexOf(canonicalWords, word);
            if (catalogueIndex <= previousCatalogueIndex)
            {
                throw new InvalidOperationException(
                    "Codex Words must follow Word Catalogue canonical order.");
            }

            previousCatalogueIndex = catalogueIndex;
        }
    }

    private static WordId ReadKnownWordId(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(element.GetString()))
        {
            throw new InvalidOperationException($"{name} must be a non-empty Word identity string.");
        }

        var word = new WordId(element.GetString()!);
        if (!WordCatalogue.TryGet(word, out _))
        {
            throw new InvalidOperationException($"{name} used unknown identity '{word}'.");
        }

        return word;
    }

    private static void ValidateUnderstandingEntries(JsonElement understanding)
    {
        RequireArray(understanding, "Study Understanding");
        var seen = new HashSet<WordId>();
        var canonicalWords = WordCatalogue.Words.Select(word => word.Id).ToArray();
        var previousCatalogueIndex = -1;
        foreach (var entry in understanding.EnumerateArray())
        {
            RequireExactObjectWithProperties(
                entry,
                "Study Understanding entry",
                "Word",
                "Amount");
            var word = ReadKnownWordId(
                entry.GetProperty("Word"),
                "Study Understanding Word");
            if (!seen.Add(word))
            {
                throw new InvalidOperationException(
                    $"Study Understanding contained duplicate identity '{word}'.");
            }

            var catalogueIndex = Array.IndexOf(canonicalWords, word);
            if (catalogueIndex <= previousCatalogueIndex)
            {
                throw new InvalidOperationException(
                    "Study Understanding must follow Word Catalogue canonical order.");
            }

            previousCatalogueIndex = catalogueIndex;

            var amountElement = entry.GetProperty("Amount");
            var definition = WordCatalogue.Get(word);
            if (!amountElement.TryGetInt32(out var amount) ||
                amount <= 0 ||
                amount > definition.UnderstandingRequired)
            {
                throw new InvalidOperationException(
                    $"{definition.DisplayName} Understanding must be positive and no greater than " +
                    $"its authored threshold of {definition.UnderstandingRequired} in a current save.");
            }
        }
    }

    private static void ValidateCurrentState(ChronicleState state)
    {
        if (state.Tick < 0 || state.Tick == long.MaxValue)
        {
            throw new InvalidOperationException(
                "A Chronicle tick must be non-negative and leave room for another fixed tick.");
        }

        if (!Enum.IsDefined(state.Speed))
        {
            throw new InvalidOperationException($"Unknown Chronicle speed '{state.Speed}'.");
        }

        if (!Enum.IsDefined(state.Intent))
        {
            throw new InvalidOperationException($"Unknown opening Intent '{state.Intent}'.");
        }

        if (state.WorldGrammarVersion is not (0 or 1 or 2 or 3 or 4 or 5))
        {
            throw new InvalidOperationException(
                $"Unsupported World Grammar version '{state.WorldGrammarVersion}'.");
        }

        if (!Enum.IsDefined(state.IncarnationLife))
        {
            throw new InvalidOperationException(
                $"Unknown Incarnation life state '{state.IncarnationLife}'.");
        }

        if (state.IncarnationId <= 0 || state.IncarnationId == long.MaxValue)
        {
            throw new InvalidOperationException(
                "A current Incarnation identity must be positive and leave room for replacement.");
        }

        ValidateOpeningIntentProvenance(state);
        ValidateCurrentAddress(state.Address, "Incarnation");
        var looseStoneAddress = state.LooseStoneAddress
            ?? throw new InvalidOperationException("Current saves require the loose-Stone Address.");
        ValidateCurrentAddress(looseStoneAddress, "Loose Stone");
        if (looseStoneAddress.X != ChronicleState.InitialLooseStoneAddress.X ||
            looseStoneAddress.Y != ChronicleState.InitialLooseStoneAddress.Y)
        {
            throw new InvalidOperationException(
                "The loose Stone must retain its fixed X/Y provenance across Strata.");
        }
        var bellAddress = state.BellAddress
            ?? throw new InvalidOperationException("Current saves require the Bell Address.");
        ValidateCurrentAddress(bellAddress, "Bell");
        if (bellAddress.X != SkyStratum.LandmarkAddress.X ||
            bellAddress.Y != SkyStratum.LandmarkAddress.Y)
        {
            throw new InvalidOperationException(
                "The Bell must retain its fixed X/Y provenance across Strata.");
        }
        if (bellAddress == looseStoneAddress || bellAddress == state.Home?.Address)
        {
            throw new InvalidOperationException(
                "The Bell cannot overlap the loose Stone or Home.");
        }
        if (state.Loadout is null)
        {
            throw new InvalidOperationException("Current saves require all eight Loadout slots.");
        }

        state.Loadout.Value.Validate(state.Codex);
        ValidateCurrentStudy(state);
        ValidateHome(state);
        ValidateGeneratedCairnNonOverlap(state);
        ValidateFirstConflict(state);
        ValidateGoal6ACombat(state);
        ValidateGoal6BPowerHome(state);
    }

    private static void ValidateOpeningIntentProvenance(ChronicleState state)
    {
        if (state.Intent == OpeningIntent.Up && !state.Codex.Contains(WordIds.Fly))
        {
            throw new InvalidOperationException("UP Chronicles must retain Fly in the Codex.");
        }

        if (state.Intent == OpeningIntent.Here && !state.Codex.Contains(WordIds.Found))
        {
            throw new InvalidOperationException("HERE Chronicles must retain Found in the Codex.");
        }

        if (state.Intent == OpeningIntent.Against)
        {
            if (state.WorldGrammarVersion is not (3 or 4 or 5))
            {
                throw new InvalidOperationException("AGAINST is only available in World Grammar version 3, 4, or 5.");
            }

            var firstVerb = state.WorldGrammarVersion is 4 or 5 ? WordIds.Burn : WordIds.Smash;
            if (!state.Codex.Contains(firstVerb))
            {
                throw new InvalidOperationException(
                    $"AGAINST Chronicles must retain {WordCatalogue.Get(firstVerb).DisplayName} in the Codex.");
            }
        }
    }

    private static void ValidateCurrentStudy(ChronicleState state)
    {
        foreach (var word in WordCatalogue.Words.Where(word => word.UnderstandingRequired > 0))
        {
            var understanding = state.Study.UnderstandingFor(word.Id);
            if (state.Codex.Contains(word.Id) && understanding != word.UnderstandingRequired)
            {
                throw new InvalidOperationException(
                    $"Learned {word.DisplayName} must retain complete Understanding.");
            }

            if (!state.Codex.Contains(word.Id) && understanding == word.UnderstandingRequired)
            {
                throw new InvalidOperationException(
                    $"Complete {word.DisplayName} Understanding must be retained in the Codex.");
            }
        }

        if (state.Study.ActiveSourceId is not { } activeSourceId ||
            state.Study.ActiveWord is not { } activeWord)
        {
            return;
        }

        if (!state.HasLivingIncarnation)
        {
            throw new InvalidOperationException("Awaiting-replacement Chronicles cannot retain an active Study pursuit.");
        }

        var source = StudySourceGrammar.At(state, state.Address);
        if (source is null ||
            source.Id != activeSourceId ||
            source.Offers.All(offer => offer.Word.Id != activeWord) ||
            state.Codex.Contains(activeWord))
        {
            throw new InvalidOperationException("Current Study pursuit does not match the Chronicle state.");
        }
    }

    private static void ValidateHome(ChronicleState state)
    {
        if (state.Home is not { } home)
        {
            return;
        }

        if (!state.Codex.Contains(WordIds.Found))
        {
            throw new InvalidOperationException("Home requires Found in the Codex.");
        }

        if (!string.Equals(home.HoldingId, "holding.home", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Home must use the stable holding.home identity.");
        }

        if (!string.Equals(home.DisplayName, "The First Hearth", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Home must retain the display name The First Hearth.");
        }

        if (home.Material != HomeMaterialState.HearthstoneRaised)
        {
            throw new InvalidOperationException("Home must retain its HearthstoneRaised material state.");
        }

        if (home.FoundedTick < 0 || home.FoundedTick > state.Tick)
        {
            throw new InvalidOperationException("Home founding tick must be within Chronicle time.");
        }

        if (home.FoundingIncarnationId <= 0 ||
            home.FoundingIncarnationId > state.IncarnationId)
        {
            throw new InvalidOperationException(
                "Home founding Incarnation must be positive and cannot be newer than the current Incarnation.");
        }

        if (!string.Equals(
                home.Address.Stratum,
                SurfacePatch.SurfaceStratum,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Home must occupy the surface Stratum.");
        }

        var cell = WorldArea.Generate(
            state with { Home = null },
            home.Address.Stratum,
            new WorldRectangle(home.Address.X, home.Address.Y, 1, 1)).Cells[0];
        if (cell.Feature != WorldFeature.Stone ||
            cell.Ground == WorldGround.Water ||
            cell.DurableIdentity is not null)
        {
            throw new InvalidOperationException(
                "Home must occupy an unmarked, supported Stone on non-water surface ground.");
        }
    }

    private static void ValidateGeneratedCairnNonOverlap(ChronicleState state)
    {
        if (state.WorldGrammarVersion != 3)
        {
            return;
        }

        var cairnAddress = WorldArea.GeneratedCairnAddress(state.Seed);
        if (state.LooseStoneAddress == cairnAddress)
        {
            throw new InvalidOperationException("The loose Stone cannot overlap the generated Riven Cairn.");
        }

        if (state.Home?.Address == cairnAddress)
        {
            throw new InvalidOperationException("Home cannot overlap the generated Riven Cairn.");
        }

        if (state.CurrentBellAddress == cairnAddress)
        {
            throw new InvalidOperationException("The Bell cannot overlap the generated Riven Cairn.");
        }
    }

    private static void ValidateFirstConflict(ChronicleState state)
    {
        if (state.FirstConflict is not { } conflict)
        {
            if (state.WorldGrammarVersion == 3 &&
                state.HasLivingIncarnation &&
                state.Address == WorldArea.GeneratedCairnAddress(state.Seed))
            {
                throw new InvalidOperationException(
                    "A living Incarnation at the generated Riven Cairn must retain its conflict state.");
            }

            return;
        }

        if (state.WorldGrammarVersion != 3)
        {
            throw new InvalidOperationException("First Conflict state requires World Grammar version 3.");
        }

        if (!string.Equals(
                conflict.SubjectId,
                FirstConflictSubjects.RiverWardSubjectId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("First Conflict must retain the stable River-Ward subject identity.");
        }

        var cairnAddress = WorldArea.GeneratedCairnAddress(state.Seed);
        if (conflict.Address != cairnAddress)
        {
            throw new InvalidOperationException(
                "First Conflict must retain the generated Riven Cairn Address.");
        }

        if (conflict.Address == state.LooseStoneAddress ||
            conflict.Address == state.Home?.Address ||
            conflict.Address == state.CurrentBellAddress)
        {
            throw new InvalidOperationException(
                "First Conflict cannot overlap the loose Stone, Home, or Bell.");
        }

        if (conflict.ThreatenedTick < 0 || conflict.ThreatenedTick > state.Tick)
        {
            throw new InvalidOperationException("First Conflict threat tick must be within Chronicle time.");
        }

        if (conflict.PendingAction is { } pendingAction &&
            pendingAction != new LoadoutSlot(WordIds.Smash))
        {
            throw new InvalidOperationException(
                "First Conflict may retain only the exact intrinsic Smash Loadout action.");
        }

        if (conflict.PendingAction is not null && !state.Codex.Contains(WordIds.Smash))
        {
            throw new InvalidOperationException(
                "A pending First Conflict Smash action requires Smash in the Codex.");
        }

        if (conflict.Outcome is null)
        {
            if (!state.HasLivingIncarnation)
            {
                throw new InvalidOperationException(
                    "Awaiting-replacement Chronicles cannot retain an unresolved First Conflict.");
            }

            if (conflict.ResolvedTick is not null ||
                conflict.ResolvingIncarnationId is not null)
            {
                throw new InvalidOperationException(
                    "An unresolved First Conflict cannot retain resolution provenance.");
            }

            if (state.Address != conflict.Address)
            {
                throw new InvalidOperationException(
                    "An unresolved First Conflict must retain the living Incarnation at the Riven Cairn.");
            }

            if (conflict.ThreatenedTick != state.Tick)
            {
                throw new InvalidOperationException(
                    "An unresolved First Conflict must remain on its exact threatened tick until resolution.");
            }

            return;
        }

        if (conflict.Outcome != FirstConflictOutcome.Shattered)
        {
            throw new InvalidOperationException("First Conflict has an unknown outcome.");
        }

        if (!state.Codex.Contains(WordIds.Smash))
        {
            throw new InvalidOperationException(
                "A Shattered First Conflict requires Smash in the durable Codex.");
        }

        if (conflict.PendingAction is not null)
        {
            throw new InvalidOperationException(
                "A resolved First Conflict cannot retain a pending action.");
        }

        if (conflict.ResolvedTick is not { } resolvedTick ||
            conflict.ThreatenedTick == long.MaxValue ||
            resolvedTick != conflict.ThreatenedTick + 1 ||
            resolvedTick > state.Tick)
        {
            throw new InvalidOperationException(
                "First Conflict resolution tick must follow the threat within Chronicle time.");
        }

        if (conflict.ResolvingIncarnationId is not { } resolvingIncarnationId ||
            resolvingIncarnationId <= 0 ||
            resolvingIncarnationId > state.IncarnationId)
        {
            throw new InvalidOperationException(
                "First Conflict must retain a valid resolving Incarnation identity.");
        }
    }

    private static void ValidateGoal6ACombat(ChronicleState state)
    {
        if (state.WorldGrammarVersion is not (4 or 5))
        {
            if (state.Combat is not null)
            {
                throw new InvalidOperationException(
                    "Goal 6A combat state requires World Grammar version 4 or 5.");
            }

            return;
        }

        if (state.Combat is not { } combat)
        {
            throw new InvalidOperationException(
                "World Grammar version 4 or 5 requires its authored Goal 6A combat state.");
        }

        if (state.FirstConflict is not null)
        {
            throw new InvalidOperationException(
                "World Grammar version 4 or 5 does not retain the predecessor First Conflict.");
        }

        if (combat.Equipment != EquipmentState.Fixed)
        {
            throw new InvalidOperationException(
                "Goal 6A requires the authored Iron Cleaver, Quilted Jack, and Copper Ward equipment.");
        }

        if (combat.IncarnationHitPoints < 0 ||
            combat.IncarnationHitPoints > combat.MaximumHitPoints)
        {
            throw new InvalidOperationException("Goal 6A Incarnation HP is outside its authored bounds.");
        }

        if (combat.WeaponTicksUntilReady is < 0 or >= CombatState.IronCleaverCadence)
        {
            throw new InvalidOperationException("Iron Cleaver cadence must remain within its authored bounds.");
        }

        if (combat.RecoveryRemaining is < 0 or > CombatState.BurnRecovery)
        {
            throw new InvalidOperationException("Burn Recovery is outside its authored bounds.");
        }

        var brute = combat.MireBrute;
        if (!string.Equals(
                brute.Identity,
                WorldArea.GeneratedMireBruteIdentity(state.Seed),
                StringComparison.Ordinal) ||
            brute.OriginAddress != WorldArea.GeneratedMireBruteAddress(state.Seed))
        {
            throw new InvalidOperationException(
                "Goal 6A must retain the generated Mire Brute's stable identity and origin.");
        }

        ValidateCurrentAddress(brute.Address, "Mire Brute");
        if (brute.HitPoints is < 0 or > CombatState.MireBruteMaximumHitPoints ||
            brute.SwingTicksRemaining is < 1 or > CombatState.MireBruteSwingCadence)
        {
            throw new InvalidOperationException("Mire Brute state is outside its authored bounds.");
        }

        if (brute.IsLiving == (brute.DefeatedTick is not null) ||
            brute.DefeatedTick is { } defeatedTick && (defeatedTick < 0 || defeatedTick > state.Tick))
        {
            throw new InvalidOperationException("Mire Brute outcome provenance is inconsistent.");
        }

        if (combat.PendingAction is not null && combat.Preparation is not null)
        {
            throw new InvalidOperationException(
                "Only one Goal 6A tactical action may be pending at a time.");
        }

        ValidateGoal6APendingAction(state, combat.PendingAction);
        ValidateGoal6APreparation(state, combat.Preparation);
        ValidateGoal6ABurn(state, combat.OngoingBurn);

        if (combat.Scorch is { } scorch)
        {
            ValidateCurrentAddress(scorch.Address, "scorched ground");
            if (scorch.CreatedTick < 0 || scorch.CreatedTick > state.Tick)
            {
                throw new InvalidOperationException("Scorched ground creation must remain within Chronicle time.");
            }
        }

        ValidateGoal6ALoadout(state);
        if (!state.HasLivingIncarnation)
        {
            if (combat.IncarnationHitPoints != 0 ||
                combat.PendingAction is not null ||
                combat.Preparation is not null ||
                combat.RecoveryRemaining != 0 ||
                combat.WeaponStanceActive ||
                combat.EngagementActive)
            {
                throw new InvalidOperationException(
                    "A dead Goal 6A Incarnation cannot retain body-bound combat state.");
            }
        }

        if (Goal6AActionPlanning.IsImmediateDanger(state) &&
            state.Speed is ChronicleSpeed.Normal or ChronicleSpeed.Fast)
        {
            throw new InvalidOperationException(
                "Immediate Goal 6A danger may run only at Slow speed or while paused.");
        }
    }

    private static void ValidateGoal6ALoadout(ChronicleState state)
    {
        var occupied = state.ActiveLoadout.Slots
            .Select((slot, index) => (slot, index))
            .Where(pair => !pair.slot.IsEmpty)
            .ToArray();
        if (occupied.Length > CombatState.ActiveVerbSlots)
        {
            throw new InvalidOperationException("Goal 6A supports exactly one active Verb slot.");
        }

        foreach (var (slot, _) in occupied)
        {
            if (slot.Noun is not null || slot.Verb is not { } verbId)
            {
                throw new InvalidOperationException(
                    "Goal 6A Loadouts use Verbs and Modifiers, never fitted Nouns.");
            }

            if (!WordCatalogue.TryGet(verbId, out var verb) || verb.Kind != WordKind.Verb ||
                !state.Codex.Contains(verbId))
            {
                throw new InvalidOperationException("Goal 6A Loadout Verb is not an attuned Codex Verb.");
            }

            foreach (var modifierId in slot.Modifiers)
            {
                if (!WordCatalogue.TryGet(modifierId, out var modifier) ||
                    modifier.Kind != WordKind.Modifier ||
                    !verb.SupportedModifiers.Contains(modifierId) ||
                    !state.Codex.Contains(modifierId))
                {
                    throw new InvalidOperationException(
                        "Goal 6A Loadout Modifier is not an attuned compatible Codex Modifier.");
                }
            }

            var load = verb.Load + slot.Modifiers.Sum(id => WordCatalogue.Get(id).Load);
            var recordedCapacity = state.Attunement?.Capacity ?? Goal6BPowerComesHome.InherentLoadCapacity;
            if (load > recordedCapacity)
            {
                throw new InvalidOperationException(
                    "The current Loadout exceeds its recorded Attunement capacity.");
            }
        }
    }

    private static void ValidateGoal6APendingAction(
        ChronicleState state,
        TacticalActionState? action)
    {
        if (action is null)
        {
            return;
        }

        if (!Enum.IsDefined(action.Kind))
        {
            throw new InvalidOperationException("Goal 6A pending action has an unknown kind.");
        }

        if (action.Kind == TacticalActionKind.Move &&
            !((action.DeltaX is -1 or 1 && action.DeltaY == 0) ||
              (action.DeltaX == 0 && action.DeltaY is -1 or 1)))
        {
            throw new InvalidOperationException("Goal 6A pending movement must be one cardinal step.");
        }

        if (action.Kind == TacticalActionKind.PrepareBurn)
        {
            if (action.Target is not { } target)
            {
                throw new InvalidOperationException("Goal 6A pending Burn requires a Target.");
            }

            ValidateCurrentAddress(target, "Goal 6A pending Burn Target");
        }
    }

    private static void ValidateGoal6APreparation(
        ChronicleState state,
        BurnPreparationState? preparation)
    {
        if (preparation is null)
        {
            return;
        }

        var modifiers = preparation.Expression.Modifiers;
        if (preparation.ActorIncarnationId <= 0 ||
            preparation.ActorIncarnationId > state.IncarnationId ||
            preparation.RemainingTicks is < 1 or > 3 ||
            preparation.Expression.Verb != WordIds.Burn ||
            preparation.Expression.Noun is not null ||
            modifiers.Any(modifier => modifier != WordIds.Quickly && modifier != WordIds.Lasting) ||
            modifiers.Distinct().Count() != modifiers.Count)
        {
            throw new InvalidOperationException("Goal 6A Burn Preparation is invalid.");
        }

        ValidateCurrentAddress(preparation.TargetAddressAtPreparation, "Burn Preparation Target");
    }

    private static void ValidateGoal6ABurn(ChronicleState state, BurnConsequenceState? burn)
    {
        if (burn is null)
        {
            return;
        }

        if (!string.Equals(
                burn.TargetIdentity,
                state.Combat!.MireBrute.Identity,
                StringComparison.Ordinal) ||
            burn.Damage != CombatState.BurnDamage ||
            burn.RemainingTicks is < 1 or > 6)
        {
            throw new InvalidOperationException("Goal 6A ongoing Burn is invalid.");
        }
    }

    private static void ValidateGoal6BPowerHome(ChronicleState state)
    {
        if (state.WorldGrammarVersion != 5)
        {
            if (state.PowerHome is not null)
            {
                throw new InvalidOperationException(
                    "Power Comes Home state requires World Grammar version 5.");
            }

            if (state.Attunement is { Capacity: not Goal6BPowerComesHome.InherentLoadCapacity })
            {
                throw new InvalidOperationException(
                    "Older World Grammar pins retain only the inherent Load capacity.");
            }

            return;
        }

        var power = state.PowerHome
            ?? throw new InvalidOperationException("World Grammar version 5 requires one Resonant Lode.");
        var lode = power.Lode;
        if (!string.Equals(lode.Identity, Goal6BPowerComesHome.ResonantLodeIdentity(state.Seed), StringComparison.Ordinal) ||
            lode.OriginAddress != Goal6BPowerComesHome.SingingSeamAddress)
        {
            throw new InvalidOperationException("The Resonant Lode must retain its generated identity and origin.");
        }

        if (!Enum.IsDefined(lode.Disposition) ||
            power.ExtractionProgress is < 0 or > Goal6BPowerComesHome.ExtractTicks)
        {
            throw new InvalidOperationException("Resonant Lode extraction state is outside its authored bounds.");
        }

        switch (lode.Disposition)
        {
            case ResonantLodeDisposition.Embedded:
                if (lode.Address != lode.OriginAddress || lode.CarrierIncarnationId is not null || power.Resonator is not null)
                {
                    throw new InvalidOperationException("An embedded Resonant Lode exists only at its persistent origin.");
                }
                break;
            case ResonantLodeDisposition.Loose:
                if (lode.Address is null || lode.CarrierIncarnationId is not null)
                {
                    throw new InvalidOperationException("A loose Resonant Lode requires exactly one world Address.");
                }
                ValidateCurrentAddress(lode.Address.Value, "Resonant Lode");
                break;
            case ResonantLodeDisposition.Carried:
                if (!state.HasLivingIncarnation || lode.Address is not null ||
                    lode.CarrierIncarnationId != state.IncarnationId)
                {
                    throw new InvalidOperationException(
                        "A carried Resonant Lode must belong exclusively to the living current Incarnation.");
                }
                if (state.Combat!.WeaponStanceActive)
                {
                    throw new InvalidOperationException("A Resonant Lode carrier cannot retain Iron Cleaver stance.");
                }
                break;
            case ResonantLodeDisposition.Committed:
            case ResonantLodeDisposition.Installed:
                if (power.Resonator is null || lode.Address != power.Resonator.Address || lode.CarrierIncarnationId is not null)
                {
                    throw new InvalidOperationException("Committed or installed Lode matter must remain at its Hearth Resonator.");
                }
                break;
        }

        if (power.ExtractionProgress < Goal6BPowerComesHome.ExtractTicks &&
            lode.Disposition != ResonantLodeDisposition.Embedded)
        {
            throw new InvalidOperationException("The Resonant Lode cannot leave its Seam before extraction completes.");
        }
        if (power.ExtractionProgress == Goal6BPowerComesHome.ExtractTicks &&
            lode.Disposition == ResonantLodeDisposition.Embedded)
        {
            throw new InvalidOperationException("Completed extraction must leave the Singing Seam visibly empty.");
        }

        if (power.Resonator is { } source)
        {
            var site = Goal6BPowerComesHome.ResonatorSite(state);
            if (site is null || source.Address != site.Value ||
                !string.Equals(source.Identity, Goal6BPowerComesHome.HearthResonatorIdentity(state.Seed), StringComparison.Ordinal) ||
                !Enum.IsDefined(source.Phase))
            {
                throw new InvalidOperationException("The sole Hearth Resonator must remain at Home's exact eligible site.");
            }

            var sourceStateValid = source.Phase switch
            {
                HearthResonatorPhase.UnderConstruction => source.Progress is >= 0 and < Goal6BPowerComesHome.BuildTicks &&
                                                          lode.Disposition == ResonantLodeDisposition.Committed,
                HearthResonatorPhase.Intact => source.Progress == Goal6BPowerComesHome.BuildTicks &&
                                               lode.Disposition == ResonantLodeDisposition.Installed,
                HearthResonatorPhase.Damaged => source.Progress == 1 &&
                                                lode.Disposition == ResonantLodeDisposition.Installed,
                HearthResonatorPhase.Destroyed => source.Progress == Goal6BPowerComesHome.DismantleTicks &&
                                                  lode.Disposition == ResonantLodeDisposition.Loose &&
                                                  lode.Address == source.Address,
                HearthResonatorPhase.Rebuilding => source.Progress is >= 0 and < Goal6BPowerComesHome.RebuildTicks &&
                                                   lode.Disposition == ResonantLodeDisposition.Committed,
                _ => false,
            };
            if (!sourceStateValid)
            {
                throw new InvalidOperationException("Hearth Resonator phase, progress, and Lode matter disagree.");
            }
        }

        if (power.Commitment is { } commitment)
        {
            if (!state.HasLivingIncarnation || commitment.ActorIncarnationId != state.IncarnationId ||
                !Enum.IsDefined(commitment.Kind) || commitment.CompletedTicks < 0 ||
                commitment.CompletedTicks >= commitment.TotalTicks || state.Combat!.PendingAction is not null ||
                state.Combat.Preparation is not null || lode.Disposition == ResonantLodeDisposition.Carried)
            {
                throw new InvalidOperationException("Power Comes Home commitment is not owned exclusively by the current body.");
            }

            var expectedTotal = commitment.Kind switch
            {
                PowerCommitmentKind.Extract => Goal6BPowerComesHome.ExtractTicks,
                PowerCommitmentKind.Build => Goal6BPowerComesHome.BuildTicks,
                PowerCommitmentKind.Dismantle => Goal6BPowerComesHome.DismantleTicks,
                PowerCommitmentKind.Rebuild => Goal6BPowerComesHome.RebuildTicks,
                _ => 0,
            };
            var expectedAddress = commitment.Kind == PowerCommitmentKind.Extract
                ? Goal6BPowerComesHome.SingingSeamAddress
                : power.Resonator?.Address;
            var expectedSubject = commitment.Kind == PowerCommitmentKind.Extract
                ? lode.Identity
                : power.Resonator?.Identity;
            var representedProgressMatches = commitment.Kind switch
            {
                PowerCommitmentKind.Extract =>
                    lode.Disposition == ResonantLodeDisposition.Embedded &&
                    power.ExtractionProgress == commitment.CompletedTicks,
                PowerCommitmentKind.Build =>
                    power.Resonator is { Phase: HearthResonatorPhase.UnderConstruction } building &&
                    building.Progress == commitment.CompletedTicks &&
                    lode.Disposition == ResonantLodeDisposition.Committed,
                PowerCommitmentKind.Dismantle when commitment.CompletedTicks == 0 =>
                    power.Resonator is { Phase: HearthResonatorPhase.Intact },
                PowerCommitmentKind.Dismantle when commitment.CompletedTicks == 1 =>
                    power.Resonator is { Phase: HearthResonatorPhase.Damaged, Progress: 1 },
                PowerCommitmentKind.Rebuild =>
                    power.Resonator is { Phase: HearthResonatorPhase.Rebuilding } rebuilding &&
                    rebuilding.Progress == commitment.CompletedTicks &&
                    lode.Disposition == ResonantLodeDisposition.Committed,
                _ => false,
            };
            if (commitment.TotalTicks != expectedTotal || commitment.Address != expectedAddress ||
                !string.Equals(commitment.SubjectIdentity, expectedSubject, StringComparison.Ordinal) ||
                !Goal6BPowerComesHome.AreAdjacent(state.Address, commitment.Address) ||
                !representedProgressMatches)
            {
                throw new InvalidOperationException(
                    "Power commitment timing, subject, represented progress, or physical adjacency is invalid.");
            }
        }

        var usedLoad = Goal6BPowerComesHome.CurrentUsedLoad(state);
        if (state.Attunement is null)
        {
            if (usedLoad != 0)
            {
                throw new InvalidOperationException("A body awaiting fresh Attunement cannot retain an active Loadout.");
            }
        }
        else if (state.Attunement is { } attunement)
        {
            if (attunement.Capacity is not (Goal6BPowerComesHome.InherentLoadCapacity or
                                            Goal6BPowerComesHome.InherentLoadCapacity + Goal6BPowerComesHome.SourceLoadContribution) ||
                attunement.Tick < 0 || attunement.Tick > state.Tick || usedLoad > attunement.Capacity)
            {
                throw new InvalidOperationException("Recorded Attunement capacity, tick, or active Load is invalid.");
            }

            if (attunement.Capacity > Goal6BPowerComesHome.InherentLoadCapacity &&
                power.Resonator is null or { Phase: HearthResonatorPhase.UnderConstruction })
            {
                throw new InvalidOperationException("A missing or unfinished first Source cannot explain a twelve-Load Attunement.");
            }
        }
    }

    private static void ValidateCurrentAddress(WorldAddress address, string subject)
    {
        if (!string.Equals(
                address.Stratum,
                SurfacePatch.SurfaceStratum,
                StringComparison.Ordinal) &&
            !string.Equals(
                address.Stratum,
                SkyStratum.StratumName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{subject} occupies unsupported Stratum '{address.Stratum}'.");
        }
    }

    private sealed record CurrentSaveEnvelope(int Version, CurrentChronicleState Chronicle);

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
