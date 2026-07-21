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
    WorldAddress? BellAddress = null)
{
    public static readonly WorldAddress InitialLooseStoneAddress =
        new(SurfacePatch.SurfaceStratum, 1, 0);
    internal static readonly WorldAddress AcceptedHomeFixtureAddress =
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
        ActiveLoadout.Slots.Any(slot => slot.IsIntrinsicFly);

    public static ChronicleState Begin(long seed) => new(
        seed,
        Tick: 0,
        Address: new WorldAddress("surface", 0, 0),
        Speed: ChronicleSpeed.Normal,
        Intent: OpeningIntent.Unchosen,
        Codex: new CodexState(),
        Study: new StudyState(),
        Loadout: LoadoutState.Empty,
        LooseStoneAddress: InitialLooseStoneAddress,
        IncarnationId: 1,
        IncarnationLife: IncarnationLifeState.Alive,
        WorldGrammarVersion: 3,
        BellAddress: SkyStratum.LandmarkAddress);

    public ChronicleState AdvanceTick()
    {
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
            OpeningIntent.Against => WordIds.Smash,
            _ => null,
        };
        var codex = firstVerb is { } word ? Codex.Learn(word) : Codex;
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

    internal ChronicleState EndIncarnationAtBell() =>
        HasLivingIncarnation &&
        Address == CurrentBellAddress
            ? this with
            {
                IncarnationLife = IncarnationLifeState.AwaitingReplacement,
                Study = Study.Stop(),
                FirstConflict = FirstConflict is { Outcome: null } ? null : FirstConflict,
            }
            : this;

    internal ChronicleState CreateReplacementIncarnation()
    {
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
        if (WorldGrammarVersion is not (0 or 1 or 2 or 3))
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
        };
    }
}

public static class ChronicleSaveCodec
{
    public const int CurrentVersion = 5;

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
            new ChronicleSaveEnvelope(CurrentVersion, current),
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
            BellAddress: SkyStratum.LandmarkAddress);
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
            BellAddress: SkyStratum.LandmarkAddress);
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
        RequireExactObjectWithProperties(
            chronicle.GetProperty("BellAddress"),
            "Bell Address",
            "Stratum",
            "X",
            "Y");
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

        if (state.WorldGrammarVersion is not (0 or 1 or 2 or 3))
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
            if (state.WorldGrammarVersion != 3)
            {
                throw new InvalidOperationException("AGAINST is only available in World Grammar version 3.");
            }

            if (!state.Codex.Contains(WordIds.Smash))
            {
                throw new InvalidOperationException("AGAINST Chronicles must retain Smash in the Codex.");
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

    private sealed record ChronicleSaveEnvelope(int Version, ChronicleState Chronicle);

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
