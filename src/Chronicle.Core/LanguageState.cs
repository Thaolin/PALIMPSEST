using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicle.Core;

[JsonConverter(typeof(CodexStateJsonConverter))]
public readonly struct CodexState : IEquatable<CodexState>
{
    private readonly WordId[]? _words;

    public CodexState(bool HasFly, bool HasStone)
        : this(
            new[]
            {
                HasFly ? WordIds.Fly : default,
                HasStone ? WordIds.Stone : default,
            }.Where(word => !string.IsNullOrWhiteSpace(word.Value)))
    {
    }

    internal CodexState(IEnumerable<WordId> words)
    {
        _words = WordCatalogue.Canonicalize(words);
    }

    [JsonIgnore]
    public IReadOnlyList<WordId> Words =>
        _words is null ? Array.Empty<WordId>() : Array.AsReadOnly(_words);

    [JsonIgnore]
    public bool HasFly => Contains(WordIds.Fly);

    [JsonIgnore]
    public bool HasStone => Contains(WordIds.Stone);

    [JsonIgnore]
    public bool HasBell => Contains(WordIds.Bell);

    public bool Contains(WordId word) => Words.Contains(word);

    internal CodexState Learn(WordId word) =>
        Contains(word) ? this : new CodexState(Words.Append(word));

    internal CodexState LearnFly() => Learn(WordIds.Fly);

    internal CodexState LearnStone() => Learn(WordIds.Stone);

    public bool Equals(CodexState other) => Words.SequenceEqual(other.Words);

    public override bool Equals(object? obj) => obj is CodexState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var word in Words)
        {
            hash.Add(word);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(CodexState left, CodexState right) => left.Equals(right);

    public static bool operator !=(CodexState left, CodexState right) => !left.Equals(right);
}

public readonly record struct WordUnderstanding(WordId Word, int Amount);

[JsonConverter(typeof(StudyStateJsonConverter))]
public readonly struct StudyState : IEquatable<StudyState>
{
    public const int UnderstandingRequired = 16;
    public const int StoneUnderstandingRequired = UnderstandingRequired;

    private readonly WordUnderstanding[]? _understanding;

    public StudyState(int StoneUnderstanding, bool IsStudyingBell)
        : this(
            StoneUnderstanding > 0
                ? [new WordUnderstanding(WordIds.Stone, StoneUnderstanding)]
                : [],
            IsStudyingBell ? StudySourceIds.BellSkyStone : null,
            IsStudyingBell ? WordIds.Stone : null)
    {
    }

    internal StudyState(
        IEnumerable<WordUnderstanding> understanding,
        StudySourceId? activeSourceId = null,
        WordId? activeWord = null)
    {
        _understanding = Canonicalize(understanding);
        if ((activeSourceId is null) != (activeWord is null))
        {
            throw new InvalidOperationException("Active Study requires both a source and a Word.");
        }

        ActiveSourceId = activeSourceId;
        ActiveWord = activeWord;
    }

    [JsonIgnore]
    public IReadOnlyList<WordUnderstanding> Understanding =>
        _understanding is null
            ? Array.Empty<WordUnderstanding>()
            : Array.AsReadOnly(_understanding);

    public StudySourceId? ActiveSourceId { get; }

    public WordId? ActiveWord { get; }

    [JsonIgnore]
    public int StoneUnderstanding => UnderstandingFor(WordIds.Stone);

    [JsonIgnore]
    public bool IsStudyingBell =>
        ActiveSourceId == StudySourceIds.BellSkyStone &&
        ActiveWord == WordIds.Stone;

    public int UnderstandingFor(WordId word) =>
        Understanding.FirstOrDefault(entry => entry.Word == word).Amount;

    internal StudyState Begin(StudySourceId sourceId, WordId wordId) =>
        new(Understanding, sourceId, wordId);

    internal StudyState Stop() => new(Understanding);

    internal StudyState StopIfActive(WordId wordId) =>
        ActiveWord == wordId ? Stop() : this;

    internal StudyState WithUnderstanding(WordId wordId, int amount)
    {
        var word = WordCatalogue.Get(wordId);
        if (amount < 0 || amount > word.UnderstandingRequired)
        {
            throw new InvalidOperationException(
                $"{word.DisplayName} understanding must be between 0 and {word.UnderstandingRequired}.");
        }

        return new StudyState(
            Understanding
                .Where(entry => entry.Word != wordId)
                .Append(new WordUnderstanding(wordId, amount)),
            ActiveSourceId,
            ActiveWord);
    }

    public bool Equals(StudyState other) =>
        ActiveSourceId == other.ActiveSourceId &&
        ActiveWord == other.ActiveWord &&
        Understanding.SequenceEqual(other.Understanding);

    public override bool Equals(object? obj) => obj is StudyState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ActiveSourceId);
        hash.Add(ActiveWord);
        foreach (var entry in Understanding)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(StudyState left, StudyState right) => left.Equals(right);

    public static bool operator !=(StudyState left, StudyState right) => !left.Equals(right);

    private static WordUnderstanding[] Canonicalize(IEnumerable<WordUnderstanding> entries)
    {
        var amounts = new Dictionary<WordId, int>();
        foreach (var entry in entries)
        {
            var word = WordCatalogue.Get(entry.Word);
            if (entry.Amount < 0 || entry.Amount > word.UnderstandingRequired)
            {
                throw new InvalidOperationException(
                    $"{word.DisplayName} understanding must be between 0 and {word.UnderstandingRequired}.");
            }

            if (entry.Amount > 0)
            {
                amounts[entry.Word] = entry.Amount;
            }
        }

        return WordCatalogue.Words
            .Where(word => amounts.ContainsKey(word.Id))
            .Select(word => new WordUnderstanding(word.Id, amounts[word.Id]))
            .ToArray();
    }
}

internal sealed class CodexStateJsonConverter : JsonConverter<CodexState>
{
    public override CodexState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("Words", out var wordsElement) ||
            wordsElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("A current Codex must contain a Words array.");
        }

        return new CodexState(
            wordsElement.EnumerateArray().Select(element => new WordId(element.GetString()!)));
    }

    public override void Write(Utf8JsonWriter writer, CodexState value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Words");
        writer.WriteStartArray();
        foreach (var word in value.Words)
        {
            writer.WriteStringValue(word.Value);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

internal sealed class StudyStateJsonConverter : JsonConverter<StudyState>
{
    public override StudyState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("Understanding", out var understandingElement) ||
            understandingElement.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("ActiveSourceId", out var sourceElement) ||
            !root.TryGetProperty("ActiveWord", out var wordElement))
        {
            throw new JsonException(
                "Current Study must contain Understanding, ActiveSourceId, and ActiveWord.");
        }

        var entries = understandingElement
            .EnumerateArray()
            .Select(element => new WordUnderstanding(
                new WordId(element.GetProperty("Word").GetString()!),
                element.GetProperty("Amount").GetInt32()))
            .ToArray();
        var source = sourceElement.ValueKind switch
        {
            JsonValueKind.Null => (StudySourceId?)null,
            JsonValueKind.String => new StudySourceId(sourceElement.GetString()!),
            _ => throw new JsonException("ActiveSourceId must be a string or null."),
        };
        var word = wordElement.ValueKind switch
        {
            JsonValueKind.Null => (WordId?)null,
            JsonValueKind.String => new WordId(wordElement.GetString()!),
            _ => throw new JsonException("ActiveWord must be a string or null."),
        };
        return new StudyState(entries, source, word);
    }

    public override void Write(Utf8JsonWriter writer, StudyState value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Understanding");
        writer.WriteStartArray();
        foreach (var entry in value.Understanding)
        {
            writer.WriteStartObject();
            writer.WriteString("Word", entry.Word.Value);
            writer.WriteNumber("Amount", entry.Amount);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        if (value.ActiveSourceId is { } source)
        {
            writer.WriteString("ActiveSourceId", source.Value);
            writer.WriteString("ActiveWord", value.ActiveWord!.Value.Value);
        }
        else
        {
            writer.WriteNull("ActiveSourceId");
            writer.WriteNull("ActiveWord");
        }

        writer.WriteEndObject();
    }
}
