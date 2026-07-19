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
}

public enum IncarnationLifeState
{
    Alive = 0,
    AwaitingReplacement = 1,
}

public readonly record struct CodexState(
    bool HasFly = false,
    bool HasStone = false)
{
    internal CodexState LearnFly() => this with { HasFly = true };

    internal CodexState LearnStone() => this with { HasStone = true };
}

public readonly record struct StudyState(
    int StoneUnderstanding = 0,
    bool IsStudyingBell = false)
{
    public const int StoneUnderstandingRequired = 16;

    internal StudyState Stop() => this with { IsStudyingBell = false };
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
    int WorldGrammarVersion = 0)
{
    public static readonly WorldAddress InitialLooseStoneAddress =
        new(SurfacePatch.SurfaceStratum, 1, 0);
    public const string LooseStoneIdentity = "Loose Stone";

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
        WorldGrammarVersion: 1);

    public ChronicleState AdvanceTick()
    {
        if (!HasLivingIncarnation || Speed == ChronicleSpeed.Paused)
        {
            return this;
        }

        var advanced = this with { Tick = Tick + 1 };
        if (!advanced.Study.IsStudyingBell)
        {
            return advanced;
        }

        if (advanced.Address != SkyStratum.LandmarkAddress || advanced.Codex.HasStone)
        {
            return advanced with { Study = advanced.Study.Stop() };
        }

        var understanding = advanced.Study.StoneUnderstanding + 1;
        if (understanding < StudyState.StoneUnderstandingRequired)
        {
            return advanced with
            {
                Study = advanced.Study with { StoneUnderstanding = understanding },
            };
        }

        return advanced with
        {
            Codex = advanced.Codex.LearnStone(),
            Study = new StudyState(
                StoneUnderstanding: StudyState.StoneUnderstandingRequired,
                IsStudyingBell: false),
        };
    }

    public ChronicleState WithSpeed(ChronicleSpeed speed) => this with { Speed = speed };

    internal ChronicleState WithIntent(OpeningIntent intent)
    {
        var codex = intent == OpeningIntent.Up ? Codex.LearnFly() : Codex;
        var loadout = Loadout ?? LoadoutState.Empty;
        if (intent == OpeningIntent.Up &&
            loadout.Slots.All(slot => slot.Verb != ChronicleVerb.Fly))
        {
            loadout = loadout.WithSlot(0, new LoadoutSlot(ChronicleVerb.Fly));
        }

        return this with
        {
            Intent = intent,
            Codex = codex,
            Loadout = loadout,
        };
    }

    internal ChronicleState BeginStoneStudy() =>
        Address == SkyStratum.LandmarkAddress &&
        !Codex.HasStone &&
        Study.StoneUnderstanding < StudyState.StoneUnderstandingRequired
            ? this with { Study = Study with { IsStudyingBell = true } }
            : this;

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
        if (WorldGrammarVersion != 0 && WorldGrammarVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported World Grammar version '{WorldGrammarVersion}'.");
        }

        if (Study.StoneUnderstanding is < 0 or > StudyState.StoneUnderstandingRequired)
        {
            throw new InvalidOperationException(
                $"Stone understanding must be between 0 and {StudyState.StoneUnderstandingRequired}.");
        }

        if (!Enum.IsDefined(IncarnationLife))
        {
            throw new InvalidOperationException($"Unknown Incarnation life state '{IncarnationLife}'.");
        }

        var codex = Intent == OpeningIntent.Up ? Codex.LearnFly() : Codex;
        var study = Study;

        if (codex.HasStone || study.StoneUnderstanding == StudyState.StoneUnderstandingRequired)
        {
            codex = codex.LearnStone();
            study = new StudyState(
                StoneUnderstanding: StudyState.StoneUnderstandingRequired,
                IsStudyingBell: false);
        }
        else if (study.IsStudyingBell && Address != SkyStratum.LandmarkAddress)
        {
            study = study.Stop();
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
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(ChronicleState state) =>
        JsonSerializer.Serialize(
            new ChronicleSaveEnvelope(CurrentVersion, state.MigrateAndValidate()),
            Options);

    public static ChronicleState Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Chronicle save data must be a JSON object.");
        }

        return document.RootElement.TryGetProperty("Version", out var versionElement)
            ? DeserializeEnvelope(document.RootElement, versionElement)
            : DeserializePreEnvelope(json);
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
            _ => throw new InvalidOperationException($"Unsupported Chronicle save version '{version}'."),
        };
    }

    private static ChronicleState DeserializeVersion1(JsonElement root)
    {
        if (!root.TryGetProperty("Chronicle", out var chronicleElement))
        {
            throw new InvalidOperationException("Version 1 Chronicle save data was missing its Chronicle.");
        }

        var state = chronicleElement.Deserialize<ChronicleState>(Options)
            ?? throw new InvalidOperationException("Version 1 Chronicle save data was empty.");
        return state.MigrateAndValidate();
    }

    private static ChronicleState DeserializePreEnvelope(string json)
    {
        var state = JsonSerializer.Deserialize<ChronicleState>(json, Options)
            ?? throw new InvalidOperationException("Pre-envelope Chronicle save data was empty.");
        return MigratePreEnvelopeState(state);
    }

    private static ChronicleState MigratePreEnvelopeState(ChronicleState state) =>
        state.MigrateAndValidate();

    private sealed record ChronicleSaveEnvelope(int Version, ChronicleState Chronicle);
}
