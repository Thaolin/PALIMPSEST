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
    HomeState? Home = null)
{
    public static readonly WorldAddress InitialLooseStoneAddress =
        new(SurfacePatch.SurfaceStratum, 1, 0);
    public const string LooseStoneIdentity = "Loose Stone";
    public const string HomeHearthstoneIdentity = "The First Hearthstone";

    [JsonIgnore]
    public LoadoutState ActiveLoadout => Loadout ?? LoadoutState.InitialFor(Codex);

    [JsonIgnore]
    public bool HasLivingIncarnation => IncarnationLife == IncarnationLifeState.Alive;

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
        WorldGrammarVersion: 2);

    public ChronicleState AdvanceTick()
    {
        if (!HasLivingIncarnation || Speed == ChronicleSpeed.Paused)
        {
            return this;
        }

        var advanced = this with { Tick = Tick + 1 };
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

    internal ChronicleState TravelTo(WorldAddress address) => this with
    {
        Address = address,
        Study = address == Address ? Study : Study.Stop(),
    };

    internal ChronicleState EndIncarnationAtBell() =>
        HasLivingIncarnation &&
        Address == SkyStratum.LandmarkAddress
            ? this with
            {
                IncarnationLife = IncarnationLifeState.AwaitingReplacement,
                Study = Study.Stop(),
            }
            : this;

    internal ChronicleState CreateReplacementIncarnation() =>
        IncarnationLife == IncarnationLifeState.AwaitingReplacement
            ? this with
            {
                Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
                Loadout = LoadoutState.Empty,
                IncarnationId = checked(IncarnationId + 1),
                IncarnationLife = IncarnationLifeState.Alive,
                Study = Study.Stop(),
            }
            : this;

    internal ChronicleState MigrateAndValidate()
    {
        if (WorldGrammarVersion is not (0 or 1 or 2))
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
            IncarnationId = IncarnationId <= 0 ? 1 : IncarnationId,
        };
    }
}

public static class ChronicleSaveCodec
{
    public const int CurrentVersion = 3;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

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

        return DeserializePreEnvelope(json);
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
            _ => throw new InvalidOperationException($"Unsupported Chronicle save version '{version}'."),
        };
    }

    private static ChronicleState DeserializeVersion1(JsonElement root)
    {
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 1 Chronicle save data was missing its Chronicle.");
        }

        var predecessor = chronicleElement.Deserialize<PredecessorChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 1 Chronicle save data was empty.");
        return MigratePredecessor(predecessor, migrateWorldGrammarOne: true);
    }

    private static ChronicleState DeserializeVersion2(JsonElement root)
    {
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 2 Chronicle save data was missing its Chronicle.");
        }

        ValidateVersion2Document(chronicleElement);
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

        ValidateVersion3Document(chronicleElement);
        var state = chronicleElement.Deserialize<ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 3 Chronicle save data was empty.");
        ValidateCurrentState(state);
        return state;
    }

    private static ChronicleState DeserializePreEnvelope(string json)
    {
        var predecessor = JsonSerializer.Deserialize<PredecessorChronicleState>(json, Options)
            ?? throw new InvalidOperationException("Pre-envelope Chronicle save data was empty.");
        return MigratePredecessor(predecessor, migrateWorldGrammarOne: false);
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
        if (state.Tick < 0)
        {
            throw new InvalidOperationException("A Chronicle tick cannot be negative.");
        }

        if (!Enum.IsDefined(state.Speed))
        {
            throw new InvalidOperationException($"Unknown Chronicle speed '{state.Speed}'.");
        }

        if (!Enum.IsDefined(state.Intent))
        {
            throw new InvalidOperationException($"Unknown opening Intent '{state.Intent}'.");
        }

        if (state.WorldGrammarVersion is not (0 or 1 or 2))
        {
            throw new InvalidOperationException(
                $"Unsupported World Grammar version '{state.WorldGrammarVersion}'.");
        }

        if (!Enum.IsDefined(state.IncarnationLife))
        {
            throw new InvalidOperationException(
                $"Unknown Incarnation life state '{state.IncarnationLife}'.");
        }

        if (state.IncarnationId <= 0)
        {
            throw new InvalidOperationException("A current Incarnation identity must be positive.");
        }

        ValidateOpeningIntentProvenance(state);
        ValidateCurrentAddress(state.Address, "Incarnation");
        ValidateCurrentAddress(
            state.LooseStoneAddress
                ?? throw new InvalidOperationException("Current saves require the loose-Stone Address."),
            "Loose Stone");
        if (state.Loadout is null)
        {
            throw new InvalidOperationException("Current saves require all eight Loadout slots.");
        }

        state.Loadout.Value.Validate(state.Codex);
        ValidateCurrentStudy(state);
        ValidateHome(state);
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
