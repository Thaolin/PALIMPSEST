namespace Chronicle.Core;

public enum SkyTerrain
{
    OpenSky,
    Cloud,
    Landmark,
}

public readonly record struct SkyTile(WorldAddress Address, SkyTerrain Terrain);

public sealed class SkyStratum
{
    public const string StratumName = "sky";
    public const int Width = 15;
    public const int Height = 11;
    public const string LandmarkName = "The Bell That Fell Up";
    public const string LandmarkArrivalLine = "It hangs without support. It rings below you.";

    public static readonly WorldAddress LandmarkAddress = new(StratumName, 0, -4);

    private SkyStratum(long seed, WorldAddress center, IReadOnlyList<SkyTile> tiles)
    {
        Seed = seed;
        Center = center;
        Tiles = tiles;
    }

    public long Seed { get; }

    public WorldAddress Center { get; }

    // Row-major: Y increases between rows, then X increases within each row.
    public IReadOnlyList<SkyTile> Tiles { get; }

    public static SkyStratum Generate(ChronicleState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!string.Equals(state.Address.Stratum, StratumName, StringComparison.Ordinal))
        {
            throw new ArgumentException("Sky patches require a sky center address.", nameof(state));
        }

        var tiles = new SkyTile[Width * Height];
        var index = 0;

        for (var y = -Height / 2; y <= Height / 2; y++)
        {
            for (var x = -Width / 2; x <= Width / 2; x++)
            {
                var address = new WorldAddress(
                    StratumName,
                    state.Address.X + x,
                    state.Address.Y + y);
                var terrain = TerrainAt(state.Seed, address.X, address.Y);
                tiles[index++] = new SkyTile(address, terrain);
            }
        }

        return new SkyStratum(state.Seed, state.Address, Array.AsReadOnly(tiles));
    }

    public SkyTile TileAt(WorldAddress address)
    {
        if (!Contains(address))
        {
            throw new ArgumentOutOfRangeException(
                nameof(address),
                address,
                "Address is outside the visible sky patch.");
        }

        var minX = Center.X - Width / 2;
        var minY = Center.Y - Height / 2;
        var index = checked((int)((address.Y - minY) * Width + address.X - minX));
        return Tiles[index];
    }

    public bool Contains(WorldAddress address) =>
        string.Equals(address.Stratum, StratumName, StringComparison.Ordinal) &&
        address.X >= Center.X - Width / 2 &&
        address.X <= Center.X + Width / 2 &&
        address.Y >= Center.Y - Height / 2 &&
        address.Y <= Center.Y + Height / 2;

    internal static SkyTerrain TerrainAt(long seed, long x, long y)
    {
        var address = new WorldAddress(StratumName, x, y);
        return address == LandmarkAddress
            ? SkyTerrain.Landmark
            : CloudAt(seed, x, y)
                ? SkyTerrain.Cloud
                : SkyTerrain.OpenSky;
    }

    private static bool CloudAt(long seed, long x, long y) =>
        DeterministicHash.Coordinates(seed, x, y, 0x51ED270Bu) % 5u == 0;
}
