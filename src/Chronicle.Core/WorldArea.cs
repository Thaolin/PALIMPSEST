namespace Chronicle.Core;

public readonly record struct WorldRectangle(long MinX, long MinY, int Width, int Height);

public enum WorldGround
{
    Grass,
    Soil,
    Water,
    OpenSky,
}

public enum WorldFeature
{
    Vegetation,
    Stone,
    Cloud,
    Landmark,
}

public readonly record struct WorldCardinalAdjacency(
    bool North,
    bool East,
    bool South,
    bool West);

public readonly record struct WorldCell(
    WorldAddress Address,
    WorldGround Ground,
    WorldFeature? Feature,
    string? DurableIdentity,
    string? MotifIdentity,
    WorldCardinalAdjacency SameFormAdjacency);

public sealed class WorldArea
{
    private static readonly string[] GroveMotifs =
    [
        "surface-grove-0", "surface-grove-1", "surface-grove-2", "surface-grove-3",
        "surface-grove-4", "surface-grove-5", "surface-grove-6", "surface-grove-7",
        "surface-grove-8", "surface-grove-9", "surface-grove-10", "surface-grove-11",
        "surface-grove-12", "surface-grove-13", "surface-grove-14", "surface-grove-15",
    ];

    private static readonly string[] CloudBankMotifs =
    [
        "sky-cloud-bank-0", "sky-cloud-bank-1", "sky-cloud-bank-2", "sky-cloud-bank-3",
        "sky-cloud-bank-4", "sky-cloud-bank-5", "sky-cloud-bank-6", "sky-cloud-bank-7",
        "sky-cloud-bank-8", "sky-cloud-bank-9", "sky-cloud-bank-10", "sky-cloud-bank-11",
        "sky-cloud-bank-12", "sky-cloud-bank-13", "sky-cloud-bank-14", "sky-cloud-bank-15",
    ];

    private WorldArea(string stratum, WorldRectangle bounds, IReadOnlyList<WorldCell> cells)
    {
        Stratum = stratum;
        Bounds = bounds;
        Cells = cells;
    }

    public string Stratum { get; }

    public WorldRectangle Bounds { get; }

    public IReadOnlyList<WorldCell> Cells { get; }

    public static WorldArea Generate(ChronicleState state, string stratum, WorldRectangle bounds)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!string.Equals(stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal) &&
            !string.Equals(stratum, SkyStratum.StratumName, StringComparison.Ordinal))
        {
            throw new ArgumentException("World areas require an existing stratum.", nameof(stratum));
        }

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds), "World area dimensions must be positive.");
        }

        var cells = new WorldCell[checked(bounds.Width * bounds.Height)];
        var cairnAddress = state.WorldGrammarVersion == 3
            ? GeneratedCairnAddress(state.Seed)
            : (WorldAddress?)null;
        var index = 0;

        for (var y = 0; y < bounds.Height; y++)
        {
            for (var x = 0; x < bounds.Width; x++)
            {
                var address = new WorldAddress(
                    stratum,
                    checked(bounds.MinX + x),
                    checked(bounds.MinY + y));
                var semantics = SemanticsAt(state, address, cairnAddress);
                cells[index++] = new WorldCell(
                    address,
                    semantics.Ground,
                    semantics.Feature,
                    semantics.DurableIdentity,
                    semantics.MotifIdentity,
                    new WorldCardinalAdjacency(
                        North: SameForm(state, semantics, address, 0, -1, cairnAddress),
                        East: SameForm(state, semantics, address, 1, 0, cairnAddress),
                        South: SameForm(state, semantics, address, 0, 1, cairnAddress),
                        West: SameForm(state, semantics, address, -1, 0, cairnAddress)));
            }
        }

        return new WorldArea(stratum, bounds, Array.AsReadOnly(cells));
    }

    private static bool SameForm(
        ChronicleState state,
        CellSemantics cell,
        WorldAddress address,
        int deltaX,
        int deltaY,
        WorldAddress? cairnAddress)
    {
        if ((deltaX < 0 && address.X == long.MinValue) ||
            (deltaX > 0 && address.X == long.MaxValue) ||
            (deltaY < 0 && address.Y == long.MinValue) ||
            (deltaY > 0 && address.Y == long.MaxValue))
        {
            return false;
        }

        var neighbor = SemanticsAt(
            state,
            new WorldAddress(cell.Stratum, address.X + deltaX, address.Y + deltaY),
            cairnAddress);
        return cell.Ground == neighbor.Ground && cell.Feature == neighbor.Feature;
    }

    private static CellSemantics SemanticsAt(
        ChronicleState state,
        WorldAddress address,
        WorldAddress? cairnAddress)
    {
        if (state.WorldGrammarVersion is not (0 or 1 or 2 or 3))
        {
            throw new InvalidOperationException(
                $"Unsupported World Grammar version '{state.WorldGrammarVersion}'.");
        }

        if (string.Equals(address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal))
        {
            if (state.WorldGrammarVersion is 1 or 2 or 3)
            {
                return OverlayDurableSubject(
                    state,
                    address,
                    Version1SurfaceAt(state.Seed, address),
                    cairnAddress);
            }

            CellSemantics legacySurface =
                SurfacePatch.TerrainAt(state.Seed, address.X, address.Y) switch
            {
                SurfaceTerrain.Grass => new(address.Stratum, WorldGround.Grass, null, null, null),
                SurfaceTerrain.Forest => new(
                    address.Stratum,
                    WorldGround.Grass,
                    WorldFeature.Vegetation,
                    null,
                    null),
                SurfaceTerrain.Stone => new(
                    address.Stratum,
                    WorldGround.Soil,
                    WorldFeature.Stone,
                    null,
                    null),
                SurfaceTerrain.Water => new(address.Stratum, WorldGround.Water, null, null, null),
                _ => throw new InvalidOperationException("Unknown legacy Surface terrain."),
            };
            return OverlayDurableSubject(state, address, legacySurface, cairnAddress);
        }

        CellSemantics sky = state.WorldGrammarVersion is 1 or 2 or 3
            ? Version1SkyAt(state.Seed, address)
            : SkyStratum.TerrainAt(state.Seed, address.X, address.Y) switch
            {
                SkyTerrain.OpenSky => new(address.Stratum, WorldGround.OpenSky, null, null, null),
                SkyTerrain.Cloud => new(
                    address.Stratum,
                    WorldGround.OpenSky,
                    WorldFeature.Cloud,
                    null,
                    null),
                SkyTerrain.Landmark => new(
                    address.Stratum,
                    WorldGround.OpenSky,
                    WorldFeature.Landmark,
                    SkyStratum.LandmarkName,
                    SkyStratum.LandmarkName),
                _ => throw new InvalidOperationException("Unknown legacy Sky terrain."),
            };
        return OverlayDurableSubject(state, address, sky, cairnAddress);
    }

    private static CellSemantics OverlayDurableSubject(
        ChronicleState state,
        WorldAddress address,
        CellSemantics generated,
        WorldAddress? cairnAddress)
    {
        var withCairn =
            state.WorldGrammarVersion == 3 &&
            string.Equals(address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal) &&
            address == cairnAddress
                ? generated with
                {
                    DurableIdentity = state.FirstConflict is
                        {
                            Address: var conflictAddress,
                            Outcome: FirstConflictOutcome.Shattered,
                        } && conflictAddress == address
                            ? FirstConflictSubjects.ShatteredCairnIdentity
                            : FirstConflictSubjects.RivenCairnIdentity,
                }
                : generated;

        var withLooseStone =
            state.LooseStoneAddress == address &&
            withCairn.Feature != WorldFeature.Landmark &&
            withCairn.DurableIdentity is null
                ? withCairn with
                {
                    Feature = WorldFeature.Stone,
                    DurableIdentity = ChronicleState.LooseStoneIdentity,
                }
                : withCairn;

        return withLooseStone.DurableIdentity is null &&
               state.Home?.Address == address
            ? withLooseStone with
            {
                DurableIdentity = ChronicleState.HomeHearthstoneIdentity,
            }
            : withLooseStone;
    }

    internal static WorldAddress GeneratedCairnAddress(long seed)
    {
        for (var distance = 1; distance <= 96; distance++)
        {
            // Enumerate the accepted selector tuple directly:
            // (distance, east/axis/west, absolute X, negative Y).
            for (var absoluteX = 1; absoluteX <= distance; absoluteX++)
            {
                var absoluteY = distance - absoluteX;
                if (TryCairnCandidate(seed, absoluteX, absoluteY, out var east))
                {
                    return east;
                }

                if (absoluteY != 0 &&
                    TryCairnCandidate(seed, absoluteX, -absoluteY, out east))
                {
                    return east;
                }
            }

            if (TryCairnCandidate(seed, 0, distance, out var axis))
            {
                return axis;
            }

            if (TryCairnCandidate(seed, 0, -distance, out axis))
            {
                return axis;
            }

            for (var absoluteX = 1; absoluteX <= distance; absoluteX++)
            {
                var absoluteY = distance - absoluteX;
                if (TryCairnCandidate(seed, -absoluteX, absoluteY, out var west))
                {
                    return west;
                }

                if (absoluteY != 0 &&
                    TryCairnCandidate(seed, -absoluteX, -absoluteY, out west))
                {
                    return west;
                }
            }
        }

        throw new InvalidOperationException(
            "World Grammar v3 could not find a dry Stone Cairn site within its bounded selector.");
    }

    private static bool TryCairnCandidate(
        long seed,
        int x,
        int y,
        out WorldAddress address)
    {
        address = new WorldAddress(SurfacePatch.SurfaceStratum, x, y);
        if (address == ChronicleState.InitialLooseStoneAddress ||
            address == ChronicleState.AcceptedHomeFixtureAddress)
        {
            return false;
        }

        var semantics = Version1SurfaceAt(seed, address);
        return semantics.Ground != WorldGround.Water &&
               semantics.Feature == WorldFeature.Stone;
    }

    private static CellSemantics Version1SkyAt(long seed, WorldAddress address)
    {
        if (address == SkyStratum.LandmarkAddress)
        {
            return new CellSemantics(
                address.Stratum,
                WorldGround.OpenSky,
                WorldFeature.Landmark,
                SkyStratum.LandmarkName,
                SkyStratum.LandmarkName);
        }

        var layout = DeterministicHash.Coordinates(seed, 0, 0, 0xC10D8A7Eu);
        var horizontalBanks = (layout & 1u) == 0;
        var along = horizontalBanks ? address.X : address.Y;
        var across = horizontalBanks ? address.Y : address.X;
        var phase = (long)((layout >> 1) % 48u);
        var offset = (long)((layout >> 8) % 61u) - 30;
        var bankCoordinate = (Int128)across - Triangle((Int128)along + phase, 48, 14) - offset;
        var bandIndex = FloorDiv(bankCoordinate + 36, 72);
        var withinBand = FloorMod(bankCoordinate + 36, 72) - 36;
        var nearBell =
            Distance(address.X, SkyStratum.LandmarkAddress.X) <= 2 &&
            Distance(address.Y, SkyStratum.LandmarkAddress.Y) <= 2;
        var cloud = !nearBell && Math.Abs(withinBand) <= 10;

        return new CellSemantics(
            address.Stratum,
            WorldGround.OpenSky,
            cloud ? WorldFeature.Cloud : null,
            DurableIdentity: null,
            cloud ? CloudBankMotifs[(int)FloorMod(bandIndex, CloudBankMotifs.Length)] : "sky-open-lane");
    }

    private static CellSemantics Version1SurfaceAt(long seed, WorldAddress address)
    {
        var layout = DeterministicHash.Coordinates(seed, 0, 0, 0x3A51C3A1u);
        var horizontalRiver = (layout & 1u) == 0;
        var riverOffset = (long)((layout >> 1) % 41u) - 20;
        var riverPhase = (long)((layout >> 7) % 48u);
        var alongRiver = horizontalRiver ? address.X : address.Y;
        var acrossRiver = horizontalRiver ? address.Y : address.X;
        var riverAxis = riverOffset + Triangle((Int128)alongRiver + riverPhase, 48, 12);
        var water = Distance(acrossRiver, riverAxis) <= 3;

        var ridgeOffset = (long)((layout >> 13) % 31u) - 15;
        var ridgePhase = (long)((layout >> 18) % 48u);
        var alongRidge = horizontalRiver ? address.Y : address.X;
        var acrossRidge = horizontalRiver ? address.X : address.Y;
        var ridgeAxis = ridgeOffset + Triangle((Int128)alongRidge + ridgePhase, 48, 10);
        var ridge = Distance(acrossRidge, ridgeAxis) <= 2;

        var groveCellX = FloorDiv((Int128)address.X + (layout & 31u), 64);
        var groveCellY = FloorDiv((Int128)address.Y + ((layout >> 5) & 31u), 64);
        var groveHash = DeterministicHash.Coordinates(
            seed,
            groveCellX,
            groveCellY,
            0x6A09E667u);
        var groveCenterX =
            (Int128)groveCellX * 64 - (long)(layout & 31u) + 32 + (long)(groveHash % 17u) - 8;
        var groveCenterY =
            (Int128)groveCellY * 64 - (long)((layout >> 5) & 31u) + 32 +
            (long)((groveHash >> 8) % 17u) - 8;
        var groveX = (Int128)address.X - groveCenterX;
        var groveY = (Int128)address.Y - groveCenterY;
        var grove = groveX * groveX + groveY * groveY <= 15 * 15;

        var ground = water
            ? WorldGround.Water
            : ridge || FloorMod((Int128)address.X + address.Y + (layout >> 20), 96) < 30
                ? WorldGround.Soil
                : WorldGround.Grass;
        WorldFeature? feature = ridge
            ? WorldFeature.Stone
            : grove && !water
                ? WorldFeature.Vegetation
                : null;
        var motif = water && ridge
            ? "surface-water-ridge-crossing"
            : water
                ? "surface-water-main"
                : ridge
                    ? "surface-ridge-main"
                    : grove
                        ? GroveMotifs[(int)FloorMod(groveCellX ^ groveCellY, GroveMotifs.Length)]
                        : "surface-clearing-main";

        return new CellSemantics(
            address.Stratum,
            ground,
            feature,
            DurableIdentity: null,
            motif);
    }

    private static long Triangle(Int128 value, int period, int amplitude)
    {
        var position = FloorMod(value, period);
        var half = period / 2;
        var distanceFromEnd = position <= half ? position : period - position;
        return distanceFromEnd * amplitude * 2 / period - amplitude;
    }

    private static long FloorDiv(Int128 value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return checked((long)(remainder < 0 ? quotient - 1 : quotient));
    }

    private static long FloorMod(Int128 value, int divisor)
    {
        var remainder = value % divisor;
        return checked((long)(remainder < 0 ? remainder + divisor : remainder));
    }

    private static Int128 Distance(long left, long right) =>
        Int128.Abs((Int128)left - right);

    private readonly record struct CellSemantics(
        string Stratum,
        WorldGround Ground,
        WorldFeature? Feature,
        string? DurableIdentity,
        string? MotifIdentity);

}
