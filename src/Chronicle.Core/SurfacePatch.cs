namespace Chronicle.Core;

public enum SurfaceTerrain
{
    Grass,
    Forest,
    Stone,
    Water,
}

public readonly record struct SurfaceTile(WorldAddress Address, SurfaceTerrain Terrain);

public sealed class SurfacePatch
{
    public const string SurfaceStratum = "surface";
    public const int Width = 15;
    public const int Height = 11;

    private SurfacePatch(WorldAddress center, IReadOnlyList<SurfaceTile> tiles)
    {
        Center = center;
        Tiles = tiles;
    }

    public WorldAddress Center { get; }

    // Row-major: Y increases between rows, then X increases within each row.
    public IReadOnlyList<SurfaceTile> Tiles { get; }

    public static SurfacePatch Generate(ChronicleState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!string.Equals(state.Address.Stratum, SurfaceStratum, StringComparison.Ordinal))
        {
            throw new ArgumentException("Surface patches require a surface center address.", nameof(state));
        }

        var tiles = new SurfaceTile[Width * Height];
        var index = 0;

        for (var y = -Height / 2; y <= Height / 2; y++)
        {
            for (var x = -Width / 2; x <= Width / 2; x++)
            {
                var address = new WorldAddress(
                    state.Address.Stratum,
                    state.Address.X + x,
                    state.Address.Y + y);
                tiles[index++] = new SurfaceTile(address, TerrainAt(state.Seed, address.X, address.Y));
            }
        }

        return new SurfacePatch(state.Address, Array.AsReadOnly(tiles));
    }

    private static SurfaceTerrain TerrainAt(long seed, long x, long y) =>
        (SurfaceTerrain)(DeterministicHash.Coordinates(seed, x, y) & 3u);
}
