using Chronicle.Core;

/// <summary>
/// Compact read-only presentation of one visible semantic World cell.
/// </summary>
internal static class InspectionPresentation
{
    internal static string Heading(WorldCell cell) =>
        $"INSPECT · {cell.Address.Stratum.ToUpperInvariant()} {cell.Address.X},{cell.Address.Y}";

    internal static string Facts(WorldCell cell)
    {
        var feature = cell.Feature is null ? "none" : cell.Feature.ToString()!.ToLowerInvariant();
        if (cell.Subjects.Count == 0)
        {
            return $"TERRAIN · {cell.Ground.ToString().ToUpperInvariant()} · {feature.ToUpperInvariant()}\n" +
                   "SUBJECTS · NONE";
        }

        var subject = cell.Subjects[0];
        return $"TERRAIN · {cell.Ground.ToString().ToUpperInvariant()} · {feature.ToUpperInvariant()}\n" +
               $"{subject.Kind.ToString().ToUpperInvariant()} · {DisplayName(subject)}\n" +
               $"STATE · {subject.Condition.ToUpperInvariant()}{OwnerLabel(subject)}";
    }

    internal static string Decision(WorldCell cell, bool selected) =>
        $"NEXT · {(selected ? "cell selected" : "Enter selects cell")}\n" +
        "WHEN · now; no Heartbeat\n" +
        "INTERRUPTS · Escape\n" +
        "PREVENTS · WASD Incarnation movement";

    internal static string Checklist(WorldCell cell, bool selected) => string.Join('\n', new[]
    {
        "CHECKLIST · VISIBLE CELL",
        $"[x] ADDRESS · {cell.Address}",
        $"[x] TERRAIN · {cell.Ground}{(cell.Feature is null ? string.Empty : $" + {cell.Feature}")}",
        $"[{(selected ? "x" : " ")}] ENTER · select · {cell.Subjects.Count} subject(s)",
        "[ ] WASD cursor · ESC exit · no time passes",
    });

    internal static IReadOnlyList<string> Forecast(WorldCell cell) =>
        cell.Subjects.Count == 0
            ? ["READ ONLY · ordinary terrain; no contextual action"]
            : cell.Subjects.Select(subject =>
                    $"{DisplayName(subject).ToUpperInvariant()} · {subject.Condition} · ID {ShortIdentity(subject.Identity)}")
                .Take(3)
                .ToArray();

    private static string DisplayName(WorldSubject subject) =>
        string.IsNullOrWhiteSpace(subject.DisplayName) ? subject.Archetype : subject.DisplayName;

    private static string OwnerLabel(WorldSubject subject)
    {
        if (string.IsNullOrWhiteSpace(subject.OwnerIdentity))
        {
            return subject.Progress is { } progress
                ? $" · {progress.Current}/{progress.Maximum}"
                : string.Empty;
        }

        var display = DisplayName(subject);
        var owner = display.EndsWith("'s road-roll", StringComparison.OrdinalIgnoreCase)
            ? display[..^"'s road-roll".Length]
            : ShortIdentity(subject.OwnerIdentity);
        return $" · OWNER {owner.ToUpperInvariant()}";
    }

    private static string ShortIdentity(string identity) => identity.Length <= 18
        ? identity
        : $"{identity[..14]}…";
}
