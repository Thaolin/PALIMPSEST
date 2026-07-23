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
    LoadAttunementState? Attunement = null,
    AgentCollectionState Agents = default)
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
        !HoldingFacts.IsCarrying(this) &&
        !HoldingFacts.HasCommitment(this) &&
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
        WorldGrammarVersion: 6,
        Home: new HomeState(
            "holding.home",
            "The First Hearth",
            AcceptedHomeFixtureAddress,
            FoundedTick: 0,
            FoundingIncarnationId: 1,
            HomeMaterialState.HearthstoneRaised),
        BellAddress: SkyStratum.LandmarkAddress,
        Combat: CombatState.Create(seed),
        PowerHome: HoldingRules.Create(seed),
        Attunement: new LoadAttunementState(HoldingFacts.InherentLoadCapacity, 0));

    public ChronicleState AdvanceTick()
    {
        if (CombatRules.IsAvailable(this))
        {
            return CombatRules.Advance(this, HoldingCommitments.Instance).State;
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
            OpeningIntent.Against when WorldGrammarVersion is 4 or 5 or 6 => WordIds.Burn,
            OpeningIntent.Against => WordIds.Smash,
            _ => null,
        };
        var codex = firstVerb is { } word ? Codex.Learn(word) : Codex;
        if (intent == OpeningIntent.Against && WorldGrammarVersion is 4 or 5 or 6)
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

        if (CombatRules.IsAvailable(this))
        {
            return CombatRules.EndIncarnation(this, HoldingCommitments.Instance);
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
        if (CombatRules.IsAvailable(this))
        {
            return CombatRules.CreateReplacement(this);
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
        if (WorldGrammarVersion is not (0 or 1 or 2 or 3 or 4 or 5 or 6))
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
            OpeningIntent.Against when WorldGrammarVersion is 4 or 5 or 6 => Codex
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
        if (WorldGrammarVersion is not (4 or 5 or 6))
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
            FirstConflict = WorldGrammarVersion is 4 or 5 or 6
                ? null
                : FirstConflict is { Outcome: FirstConflictOutcome.Shattered }
                    ? FirstConflict
                    : null,
            Combat = WorldGrammarVersion is 4 or 5 or 6
                ? Combat ?? CombatState.Create(Seed)
                : null,
            PowerHome = WorldGrammarVersion is 5 or 6
                ? PowerHome ?? HoldingRules.Create(Seed)
                : null,
            Attunement = WorldGrammarVersion is 5 or 6
                ? Attunement
                : Attunement ?? new LoadAttunementState(
                    HoldingFacts.InherentLoadCapacity,
                    Tick: 0),
            Agents = WorldGrammarVersion == 6 ? Agents : default,
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
