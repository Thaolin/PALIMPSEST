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
    public static readonly WordId Burn = new("word.burn");
    public static readonly WordId Quickly = new("word.quickly");
    public static readonly WordId Lasting = new("word.lasting");

    // These identities remain parseable solely for explicit v6 retirement of
    // literal predecessor saves. They are not successor vocabulary.
    public static readonly WordId Stone = new("word.stone");
    public static readonly WordId Bell = new("word.bell");
}

public enum WordKind
{
    Verb = 1,
    Noun = 2,
    Modifier = 3,
}

/// <summary>
/// The authored Chronicle-time and material effect of one Word. A Verb carries
/// its own timing and damage; a Modifier carries the deltas it contributes to
/// whatever compatible Verb it is linked to. Deltas are order-independent, so
/// resolution never needs to know which Words exist.
/// </summary>
public sealed record WordEffect(
    int Preparation = 0,
    int Consequence = 0,
    int Recovery = 0,
    int Damage = 0)
{
    public static readonly WordEffect None = new();
}

public sealed record WordDefinition(
    WordId Id,
    string DisplayName,
    WordKind Kind,
    string Meaning,
    int UnderstandingRequired,
    IReadOnlyList<WordId> CompatibleNouns,
    int Load = 0,
    WordEffect? Effect = null,
    IReadOnlyList<WordId>? CompatibleVerbs = null)
{
    public WordEffect AuthoredEffect => Effect ?? WordEffect.None;

    public IReadOnlyList<WordId> SupportedVerbs =>
        CompatibleVerbs ?? Array.Empty<WordId>();
}

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
            WordIds.Burn,
            "Burn",
            WordKind.Verb,
            "Scorch a flammable Chronicle subject through interruptible preparation.",
            0,
            Array.Empty<WordId>(),
            Load: 1,
            Effect: new WordEffect(
                Preparation: 3,
                Consequence: 3,
                Recovery: 8,
                Damage: 4)),
        new(
            WordIds.Quickly,
            "Quickly",
            WordKind.Modifier,
            "Shorten the exposed preparation for an authored compatible Verb.",
            0,
            Array.Empty<WordId>(),
            Load: 6,
            Effect: new WordEffect(Preparation: -2),
            CompatibleVerbs: Array.AsReadOnly([WordIds.Burn])),
        new(
            WordIds.Lasting,
            "Lasting",
            WordKind.Modifier,
            "Extend the authored consequence for an authored compatible Verb.",
            0,
            Array.Empty<WordId>(),
            Load: 5,
            Effect: new WordEffect(Consequence: 3),
            CompatibleVerbs: Array.AsReadOnly([WordIds.Burn])),
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

    private static readonly AsyncLocal<WordDefinition[]?> VerificationWords = new();

    public static IReadOnlyList<WordDefinition> Words =>
        Array.AsReadOnly(ActiveWords);

    public static WordDefinition Get(WordId id) =>
        ActiveWords.FirstOrDefault(word => word.Id == id) is { } word
            ? word
            : throw new KeyNotFoundException($"Unknown Word identity '{id}'.");

    internal static bool TryGet(WordId id, out WordDefinition definition)
    {
        var word = ActiveWords.FirstOrDefault(candidate => candidate.Id == id);
        if (word is not null)
        {
            definition = word;
            return true;
        }

        definition = null!;
        return false;
    }

    internal static bool AreCompatible(WordDefinition verb, WordDefinition modifier) =>
        verb.Kind == WordKind.Verb &&
        modifier.Kind == WordKind.Modifier &&
        modifier.SupportedVerbs.Contains(verb.Id);

    /// <summary>
    /// Test-only authoring seam. The production catalogue remains immutable;
    /// verification can add one definition and drive the same resolver,
    /// validation, and save/load path without changing those rules.
    /// </summary>
    internal static IDisposable UseDefinitionsForVerification(
        IEnumerable<WordDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var supplied = definitions.ToArray();
        if (supplied.Length == 0 || supplied.Select(word => word.Id).Distinct().Count() != supplied.Length)
        {
            throw new ArgumentException("Verification Word definitions require unique identities.", nameof(definitions));
        }

        var previous = VerificationWords.Value;
        VerificationWords.Value = supplied;
        return new VerificationScope(previous);
    }

    internal static WordId[] Canonicalize(IEnumerable<WordId> words)
    {
        var requested = words.Distinct().ToHashSet();
        foreach (var word in requested)
        {
            Get(word);
        }

        return ActiveWords
            .Where(word => requested.Contains(word.Id))
            .Select(word => word.Id)
            .ToArray();
    }

    private static WordDefinition[] ActiveWords =>
        VerificationWords.Value ?? AuthoredWords;

    private sealed class VerificationScope(WordDefinition[]? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            VerificationWords.Value = previous;
            _disposed = true;
        }
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
