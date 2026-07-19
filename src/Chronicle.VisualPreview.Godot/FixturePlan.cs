using System.Text.Json;

namespace Chronicle.VisualPreview;

internal sealed record FixtureEntry(
    string VisualId,
    int NativeSize,
    int Variant,
    int? AdjacencyMask,
    int X,
    int Y,
    int Scale);

internal sealed record FixturePlan(
    string Id,
    int Width,
    int Height,
    string Background,
    string PaletteId,
    FixtureEntry[] Entries)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = System.Text.Json.Serialization
            .JsonUnmappedMemberHandling.Disallow
    };

    public static FixturePlan Parse(ReadOnlySpan<byte> bytes)
    {
        var plan = JsonSerializer.Deserialize<FixturePlan>(bytes, JsonOptions)
            ?? throw new FormatException("CVG-PLAN-001: fixture plan is empty.");
        if (string.IsNullOrWhiteSpace(plan.Id) ||
            plan.Width is < 1 or > 4096 ||
            plan.Height is < 1 or > 4096 ||
            string.IsNullOrWhiteSpace(plan.PaletteId) ||
            plan.Entries is null ||
            plan.Entries.Any(static entry =>
                entry is null ||
                string.IsNullOrWhiteSpace(entry.VisualId) ||
                entry.NativeSize <= 0 ||
                entry.Variant < 0 ||
                entry.Scale is not (1 or 2 or 4 or 8)))
        {
            throw new FormatException("CVG-PLAN-002: fixture plan is invalid.");
        }
        return plan;
    }
}
