namespace Chronicle.Core;

public readonly record struct StudySourceId
{
    public StudySourceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A Study Source identity cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public static class StudySourceIds
{
    public static readonly StudySourceId BellSkyStone =
        new("study-source.bell-that-fell-up.sky-stone");
}

public enum StudySourceRarity
{
    Rare = 1,
}

public enum StudySourceDanger
{
    Lethal = 1,
}

public enum StudySourceSignificance
{
    Landmark = 1,
}

public sealed record StudyOfferSnapshot(
    WordDefinition Word,
    string Rationale,
    int UnderstandingYield,
    int CurrentUnderstanding,
    int UnderstandingRequired,
    bool IsSelected,
    bool IsLearned);

public sealed record StudySourceSnapshot(
    StudySourceId Id,
    string Name,
    WorldAddress Address,
    string Situation,
    StudySourceRarity Rarity,
    StudySourceDanger Danger,
    StudySourceSignificance Significance,
    IReadOnlyList<StudyOfferSnapshot> Offers);

internal static class StudySourceGrammar
{
    private const string StoneRationale =
        "Its dark clapper is stone veined with open sky and rises against the curve that contains it.";
    private const string BellRationale =
        "The gold vessel, clapper, and impossible fall make its identity legible as a Bell.";

    internal static StudySourceSnapshot? At(ChronicleState state, WorldAddress address)
    {
        if ((state.WorldGrammarVersion != 0 &&
             state.WorldGrammarVersion != 2 &&
             state.WorldGrammarVersion != 3) ||
            !string.Equals(address.Stratum, SkyStratum.StratumName, StringComparison.Ordinal))
        {
            return null;
        }

        var cell = WorldArea.Generate(
            state,
            address.Stratum,
            new WorldRectangle(address.X, address.Y, Width: 1, Height: 1)).Cells[0];
        if (cell.Feature != WorldFeature.Landmark ||
            !string.Equals(cell.DurableIdentity, SkyStratum.LandmarkName, StringComparison.Ordinal))
        {
            return null;
        }

        var rarity = StudySourceRarity.Rare;
        var danger = StudySourceDanger.Lethal;
        var significance = StudySourceSignificance.Landmark;
        var yield = UnderstandingYield(rarity, danger, significance);

        return new StudySourceSnapshot(
            StudySourceIds.BellSkyStone,
            "Sky-Stone Clapper",
            address,
            "A dark stone clapper veined with open sky rises inside a gold bell that fell upward.",
            rarity,
            danger,
            significance,
            state.WorldGrammarVersion == 0
                ? Array.AsReadOnly(
                [
                    Offer(state, WordIds.Stone, StoneRationale, yield),
                ])
                : Array.AsReadOnly(
                [
                    Offer(state, WordIds.Stone, StoneRationale, yield),
                    Offer(state, WordIds.Bell, BellRationale, yield),
                ]));
    }

    private static StudyOfferSnapshot Offer(
        ChronicleState state,
        WordId wordId,
        string rationale,
        int yield)
    {
        var word = WordCatalogue.Get(wordId);
        return new StudyOfferSnapshot(
            word,
            rationale,
            yield,
            state.Study.UnderstandingFor(wordId),
            word.UnderstandingRequired,
            state.Study.ActiveWord == wordId,
            state.Codex.Contains(wordId));
    }

    private static int UnderstandingYield(
        StudySourceRarity rarity,
        StudySourceDanger danger,
        StudySourceSignificance significance) =>
        4 +
        (rarity == StudySourceRarity.Rare ? 4 : 0) +
        (danger == StudySourceDanger.Lethal ? 4 : 0) +
        (significance == StudySourceSignificance.Landmark ? 4 : 0);
}
