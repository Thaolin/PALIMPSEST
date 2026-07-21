using System.Text.Json;
using Chronicle.VisualPack;

namespace Chronicle.VisualPreview;

internal sealed record FixtureEntry(
    string VisualId,
    int NativeSize,
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
                entry.NativeSize != Palimpsest20Pack.NativeCellSize ||
                entry.Scale is < 1 or > 64))
        {
            throw new FormatException("CVG-PLAN-002: fixture plan is invalid.");
        }
        return plan;
    }
}
