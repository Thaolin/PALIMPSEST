using Chronicle.VisualPack;

internal static class PackagedVisualPackLoader
{
    internal const string ManualComparisonArgument = "--manual-visual-pack";
    private const string PackDirectoryName = "palimpsest20";

    internal static CompiledVisualPack Load(
        IReadOnlyCollection<string> arguments,
        int cellSize)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (cellSize == 16 || arguments.Contains(
                ManualComparisonArgument,
                StringComparer.Ordinal))
        {
            return ManualVisualPack.CreateGate3B(cellSize);
        }

        if (cellSize != CanonicalVisualPackReader.RequiredCellSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellSize),
                "The packaged P-GEN pack supports the accepted 20-pixel view.");
        }

        var directory = Path.Combine(
            AppContext.BaseDirectory,
            "visual-packs",
            PackDirectoryName);
        return CanonicalVisualPackReader.ReadDirectory(directory);
    }

    internal static string ReviewTag(CompiledVisualPack pack) =>
        string.Equals(
            pack.PackId,
            CanonicalVisualPackReader.RequiredPackId,
            StringComparison.Ordinal)
                ? "pgen"
                : "manual";
}
