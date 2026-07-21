using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicle.Core;

[JsonConverter(typeof(WordIdJsonConverter))]
public readonly record struct WordId
{
    public WordId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A Word identity cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public static class WordIds
{
    public static readonly WordId Fly = new("word.fly");
    public static readonly WordId Found = new("word.found");
    public static readonly WordId Smash = new("word.smash");
    public static readonly WordId Stone = new("word.stone");
    public static readonly WordId Bell = new("word.bell");
}

public enum WordKind
{
    Verb = 1,
    Noun = 2,
}

public sealed record WordDefinition(
    WordId Id,
    string DisplayName,
    WordKind Kind,
    string Meaning,
    int UnderstandingRequired,
    IReadOnlyList<WordId> CompatibleNouns);

public static class WordCatalogue
{
    private static readonly WordDefinition[] AuthoredWords =
    [
        new(
            WordIds.Fly,
            "Fly",
            WordKind.Verb,
            "Move the acting subject between matching surface and sky coordinates.",
            0,
            Array.AsReadOnly([WordIds.Stone, WordIds.Bell])),
        new(
            WordIds.Found,
            "Found",
            WordKind.Verb,
            "Establish one place as Home by giving its matter a durable mark.",
            0,
            Array.Empty<WordId>()),
        new(
            WordIds.Smash,
            "Smash",
            WordKind.Verb,
            "Break a resisting material at the current site by direct force.",
            0,
            Array.Empty<WordId>()),
        new(
            WordIds.Stone,
            "Stone",
            WordKind.Noun,
            "Stone matter or one discrete Stone subject.",
            StudyState.UnderstandingRequired,
            Array.Empty<WordId>()),
        new(
            WordIds.Bell,
            "Bell",
            WordKind.Noun,
            "A bell as an object and continuing identity.",
            StudyState.UnderstandingRequired,
            Array.Empty<WordId>()),
    ];

    private static readonly IReadOnlyDictionary<WordId, WordDefinition> ById =
        AuthoredWords.ToDictionary(word => word.Id);

    public static IReadOnlyList<WordDefinition> Words { get; } =
        Array.AsReadOnly(AuthoredWords);

    public static WordDefinition Get(WordId id) =>
        ById.TryGetValue(id, out var word)
            ? word
            : throw new KeyNotFoundException($"Unknown Word identity '{id}'.");

    internal static bool TryGet(WordId id, out WordDefinition definition)
    {
        if (ById.TryGetValue(id, out var word))
        {
            definition = word;
            return true;
        }

        definition = null!;
        return false;
    }

    internal static WordId[] Canonicalize(IEnumerable<WordId> words)
    {
        var requested = words.Distinct().ToHashSet();
        foreach (var word in requested)
        {
            Get(word);
        }

        return AuthoredWords
            .Where(word => requested.Contains(word.Id))
            .Select(word => word.Id)
            .ToArray();
    }
}

internal sealed class WordIdJsonConverter : JsonConverter<WordId>
{
    public override WordId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? new WordId(reader.GetString()!)
            : throw new JsonException("A Word identity must be a string.");

    public override void Write(
        Utf8JsonWriter writer,
        WordId value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
