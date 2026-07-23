using Chronicle.Core;

/// <summary>
/// Compact read-only presentation of one visible semantic World cell.
/// </summary>
internal static class InspectionPresentation
{
    internal static string Heading(WorldCell cell) =>
        cell.Subjects.FirstOrDefault() is { } subject
            ? $"LOOK · {DisplayName(subject).ToUpperInvariant()}"
            : $"LOOK · {Readable(cell.Ground.ToString()).ToUpperInvariant()}";

    internal static string Facts(WorldCell cell)
    {
        var place = Readable(cell.Ground.ToString());
        var feature = cell.Feature is null
            ? string.Empty
            : $" · {Readable(cell.Feature.ToString()!)}";
        if (cell.Subjects.Count == 0)
        {
            return $"{place}{feature}\nNothing else occupies this cell.";
        }

        var subject = cell.Subjects[0];
        return $"{place}{feature}\n" +
               $"{DisplayName(subject)} · {Readable(subject.Condition)}{OwnerLabel(subject)}";
    }

    internal static string Decision(WorldCell cell, bool selected) => selected
        ? "This cell is pinned.\nMove to return to the world, or press I to look around."
        : "Arrow keys move the look cursor.\nEnter pins this cell. I or Escape closes.";

    internal static string Checklist(WorldCell cell, bool selected) =>
        selected
            ? $"PINNED · {cell.Address.X}, {cell.Address.Y}\nMove or Escape to return."
            : $"LOOKING · {cell.Address.X}, {cell.Address.Y}\nArrows move · Enter pin · Escape close";

    internal static IReadOnlyList<string> Forecast(WorldCell cell) =>
        cell.Subjects.Count == 0
            ? ["READ ONLY · no contextual action here"]
            : cell.Subjects
                .Take(3)
                .Select(subject =>
                    $"{DisplayName(subject).ToUpperInvariant()} · {Readable(subject.Condition).ToUpperInvariant()}")
                .ToArray();

    private static string DisplayName(WorldSubject subject) =>
        string.IsNullOrWhiteSpace(subject.DisplayName) ? Readable(subject.Archetype) : subject.DisplayName;

    private static string OwnerLabel(WorldSubject subject)
    {
        if (!string.IsNullOrWhiteSpace(subject.OwnerIdentity))
        {
            return " · owned";
        }

        return subject.Progress is { } progress
            ? $" · {progress.Current}/{progress.Maximum}"
            : string.Empty;
    }

    private static string Readable(string value) =>
        string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) && char.IsLower(value[index - 1])
                ? $" {character}"
                : character.ToString())).ToLowerInvariant();
}
