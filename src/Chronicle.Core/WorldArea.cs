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

/// <summary>
/// A WorldCell carries whatever durable subjects stand at its Address. The
/// scorch flag is intentionally independent so presentation can show a
/// persistent material delta underneath any subject.
/// </summary>
public readonly record struct WorldCell(
    WorldAddress Address,
    WorldGround Ground,
    WorldFeature? Feature,
    string? DurableIdentity,
    string? MotifIdentity,
    WorldCardinalAdjacency SameFormAdjacency,
    IReadOnlyList<WorldSubject> Subjects,
    bool IsScorched = false)
{
    public WorldSubject? Subject(WorldSubjectKind kind) =>
        Subjects.FirstOrDefault(subject => subject.Kind == kind);

    public WorldSubject? Subject(string archetype) =>
        Subjects.FirstOrDefault(subject =>
            string.Equals(subject.Archetype, archetype, StringComparison.Ordinal));

    public bool Has(WorldSubjectKind kind) => Subject(kind) is not null;
}

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
                var brute = state.WorldGrammarVersion is 4 or 5 or 6 &&
                            state.Combat?.MireBrute.Address == address
                    ? state.Combat.MireBrute
                    : null;
                var scorch = state.WorldGrammarVersion is 4 or 5 or 6 &&
                             state.Combat?.Scorch?.Address == address;
                var target = state.WorldGrammarVersion is 4 or 5 or 6 &&
                             address == GeneratedBasaltAddress(state.Seed)
                    ? new WorldSubject(
                        GeneratedBasaltIdentity(state.Seed),
                        WorldSubjectKind.Target,
                        WorldSubjects.BasaltArchetype,
                        WorldSubjects.Present,
                        displayName: "Basalt")
                    : null;
                var power = state.WorldGrammarVersion is 5 or 6 ? state.PowerHome : null;
                var seam = power is not null && address == HoldingFacts.SingingSeamAddress
                    ? SeamSubject(
                        HoldingRules.SingingSeamIdentity(state.Seed),
                        power.Lode.Disposition == ResonantLodeDisposition.Embedded
                            ? SingingSeamVisualState.Embedded
                            : SingingSeamVisualState.Empty,
                        power.ExtractionProgress)
                    : null;
                var lode = power is not null && HoldingRules.LodeWorldAddress(state) == address
                    ? LodeSubject(
                        power.Lode.Identity,
                        power.Lode.Disposition,
                        power.Lode.CarrierIncarnationId)
                    : null;
                var source = power?.Resonator is { } resonator && resonator.Address == address
                    ? LoadSourceSubject(
                        resonator,
                        resonator.Phase switch
                        {
                            HearthResonatorPhase.UnderConstruction => HoldingRules.BuildTicks,
                            HearthResonatorPhase.Rebuilding => HoldingRules.RebuildTicks,
                            HearthResonatorPhase.Intact => HoldingRules.BuildTicks,
                            _ => HoldingRules.DismantleTicks,
                        })
                    : null;
                var burnPrimer = power is not null && address == HoldingRules.BurnPrimerAddress
                    ? StudySourceSubject(
                        HoldingRules.BurnPrimerIdentity(state.Seed),
                        HoldingRules.HasBurnPrimerKnowledge(state))
                    : null;
                var agents = state.WorldGrammarVersion == 6
                    ? state.Agents.Where(agent => agent.Address == address).ToArray()
                    : [];
                var roadRolls = state.WorldGrammarVersion == 6
                    ? state.Agents.Where(agent => agent.RoadRollAddress == address).ToArray()
                    : [];
                var subjects = new List<WorldSubject>(5);
                if (brute is not null)
                {
                    subjects.Add(CreatureSubject(
                        brute,
                        state.Combat?.OngoingBurn?.TargetIdentity == brute.Identity));
                }

                if (target is not null)
                {
                    subjects.Add(target);
                }

                if (burnPrimer is not null)
                {
                    subjects.Add(burnPrimer);
                }

                if (power is not null && HoldingRules.ResonatorSite(state) == address)
                {
                    subjects.Add(new WorldSubject(
                        HoldingRules.HearthResonatorIdentity(state.Seed) + ".site",
                        WorldSubjectKind.ConstructionSite,
                        WorldSubjects.HearthResonatorSiteArchetype,
                        source is null ? WorldSubjects.Ready : WorldSubjects.Occupied,
                        displayName: "Hearth Resonator Site"));
                }

                if (seam is not null)
                {
                    subjects.Add(seam);
                }

                if (source is not null)
                {
                    subjects.Add(source);
                }

                if (lode is not null)
                {
                    subjects.Add(lode);
                }

                foreach (var agent in agents)
                {
                    subjects.Add(AgentSubject(agent));
                }

                foreach (var owner in roadRolls)
                {
                    subjects.Add(RoadRollSubject(owner));
                }

                cells[index++] = new WorldCell(
                    address,
                    semantics.Ground,
                    semantics.Feature,
                    brute?.Identity ?? semantics.DurableIdentity,
                    semantics.MotifIdentity,
                    new WorldCardinalAdjacency(
                        North: SameForm(state, semantics, address, 0, -1, cairnAddress),
                        East: SameForm(state, semantics, address, 1, 0, cairnAddress),
                        South: SameForm(state, semantics, address, 0, 1, cairnAddress),
                        West: SameForm(state, semantics, address, -1, 0, cairnAddress)),
                    Array.AsReadOnly(subjects.ToArray()),
                    IsScorched: scorch);
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
        if (state.WorldGrammarVersion is not (0 or 1 or 2 or 3 or 4 or 5 or 6))
        {
            throw new InvalidOperationException(
                $"Unsupported World Grammar version '{state.WorldGrammarVersion}'.");
        }

        if (string.Equals(address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal))
        {
            if (state.WorldGrammarVersion is 5 or 6)
            {
                return OverlayDurableSubject(
                    state,
                    address,
                    state.WorldGrammarVersion == 6
                        ? Version6SurfaceAt(state, address)
                        : Version5SurfaceAt(state, address),
                    cairnAddress);
            }

            if (state.WorldGrammarVersion == 4)
            {
                return Version4SurfaceAt(state.Seed, address);
            }

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

        CellSemantics sky = state.WorldGrammarVersion is 1 or 2 or 3 or 4 or 5 or 6
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
        var withoutMovedBell =
            address == SkyStratum.LandmarkAddress &&
            state.CurrentBellAddress != address &&
            string.Equals(
                generated.DurableIdentity,
                SkyStratum.LandmarkName,
                StringComparison.Ordinal)
                ? generated with
                {
                    Feature = null,
                    DurableIdentity = null,
                    MotifIdentity = "sky-open-lane",
                }
                : generated;

        var withCairn =
            state.WorldGrammarVersion == 3 &&
            string.Equals(address.Stratum, SurfacePatch.SurfaceStratum, StringComparison.Ordinal) &&
            address == cairnAddress
                ? withoutMovedBell with
                {
                    DurableIdentity = state.FirstConflict is
                        {
                            Address: var conflictAddress,
                            Outcome: FirstConflictOutcome.Shattered,
                        } && conflictAddress == address
                            ? FirstConflictSubjects.ShatteredCairnIdentity
                            : FirstConflictSubjects.RivenCairnIdentity,
                }
                : withoutMovedBell;

        var withBell =
            state.CurrentBellAddress == address &&
            withCairn.DurableIdentity is null
                ? withCairn with
                {
                    Feature = WorldFeature.Landmark,
                    DurableIdentity = SkyStratum.LandmarkName,
                    MotifIdentity = SkyStratum.LandmarkName,
                }
                : withCairn;

        var withLooseStone =
            state.LooseStoneAddress == address &&
            withBell.Feature != WorldFeature.Landmark &&
            withBell.DurableIdentity is null
                ? withBell with
                {
                    Feature = WorldFeature.Stone,
                    DurableIdentity = ChronicleState.LooseStoneIdentity,
                }
                : withBell;

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

    private static WorldSubject CreatureSubject(MireBruteState brute, bool isBurning)
    {
        var marks = new List<WorldSubjectMark>(2);
        if (brute.IsLiving && brute.HitPoints < CombatState.MireBruteMaximumHitPoints)
        {
            marks.Add(WorldSubjectMark.Wounded);
        }

        if (brute.IsLiving && isBurning)
        {
            marks.Add(WorldSubjectMark.Burning);
        }

        return new WorldSubject(
            brute.Identity,
            WorldSubjectKind.Creature,
            WorldSubjects.MireBruteArchetype,
            brute.IsLiving ? WorldSubjects.Living : WorldSubjects.Dead,
            displayName: "Mire Brute",
            marks: Array.AsReadOnly(marks.ToArray()),
            progress: new WorldSubjectProgress(
                brute.HitPoints,
                CombatState.MireBruteMaximumHitPoints));
    }

    private static WorldSubject StudySourceSubject(string identity, bool isRead) =>
        new(
            identity,
            WorldSubjectKind.StudySource,
            WorldSubjects.BurnPrimerArchetype,
            isRead ? WorldSubjects.Read : WorldSubjects.Unread,
            displayName: "Burn Primer",
            marks: isRead
                ? Array.Empty<WorldSubjectMark>()
                : Array.AsReadOnly(new[] { WorldSubjectMark.Selected }));

    private static WorldSubject SeamSubject(
        string identity,
        SingingSeamVisualState state,
        int extractionProgress) =>
        new(
            identity,
            WorldSubjectKind.MaterialSeam,
            WorldSubjects.SingingSeamArchetype,
            WorldSubjects.Condition(state),
            displayName: "Singing Seam",
            progress: new WorldSubjectProgress(extractionProgress, HoldingRules.ExtractTicks));

    private static WorldSubject LodeSubject(
        string identity,
        ResonantLodeDisposition disposition,
        long? carrierIncarnationId) =>
        new(
            identity,
            WorldSubjectKind.LooseMaterial,
            WorldSubjects.ResonantLodeArchetype,
            WorldSubjects.Condition(disposition),
            displayName: "Resonant Lode",
            holderIncarnationId: carrierIncarnationId);

    private static WorldSubject LoadSourceSubject(
        HearthResonatorState resonator,
        int totalProgress) =>
        new(
            resonator.Identity,
            WorldSubjectKind.LoadSource,
            WorldSubjects.HearthResonatorArchetype,
            WorldSubjects.Condition(resonator.Phase),
            displayName: "Hearth Resonator",
            progress: new WorldSubjectProgress(resonator.Progress, totalProgress));

    private static WorldSubject AgentSubject(AgentState agent) =>
        new(
            agent.Profile.Identity,
            WorldSubjectKind.Agent,
            WorldSubjects.WayfarerListenerArchetype,
            WorldSubjects.Condition(agent),
            displayName: agent.Profile.DisplayName);

    private static WorldSubject RoadRollSubject(AgentState owner) =>
        new(
            AgentRules.RoadRollIdentity(owner.Profile.Identity),
            WorldSubjectKind.PersonalPlace,
            WorldSubjects.WayfarerRoadRollArchetype,
            WorldSubjects.Laid,
            displayName: $"{owner.Profile.DisplayName}'s road-roll",
            ownerIdentity: owner.Profile.Identity);

    public static WorldAddress GeneratedMireBruteAddress(long seed) =>
        new(SurfacePatch.SurfaceStratum, 5, 0);

    public static string GeneratedMireBruteIdentity(long seed) =>
        $"subject.mire-brute.{seed.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public static WorldAddress GeneratedBasaltAddress(long seed) =>
        new(SurfacePatch.SurfaceStratum, 1, 1);

    public static string GeneratedBasaltIdentity(long seed) =>
        $"place.basalt.{seed.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

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

    private static CellSemantics Version4SurfaceAt(long seed, WorldAddress address)
    {
        // The authored acceptance clearing keeps the one bounded opponent in
        // actual map space without introducing general collision or pathing.
        if (address.Y == 0 && address.X is >= 0 and <= 5)
        {
            return new CellSemantics(
                address.Stratum,
                WorldGround.Grass,
                Feature: null,
                DurableIdentity: null,
                MotifIdentity: "surface-combat-clearing");
        }

        if (address == GeneratedBasaltAddress(seed))
        {
            return new CellSemantics(
                address.Stratum,
                WorldGround.Soil,
                WorldFeature.Stone,
                DurableIdentity: null,
                MotifIdentity: "surface-basalt-target");
        }

        return Version1SurfaceAt(seed, address);
    }

    private static CellSemantics Version5SurfaceAt(ChronicleState state, WorldAddress address)
    {
        if (address == HoldingRules.BurnPrimerAddress)
        {
            return new CellSemantics(
                address.Stratum,
                WorldGround.Soil,
                Feature: null,
                DurableIdentity: null,
                MotifIdentity: "surface-burn-primer-clearing");
        }

        if (HoldingRules.ResonatorSite(state) == address)
        {
            return new CellSemantics(
                address.Stratum,
                WorldGround.Soil,
                Feature: null,
                DurableIdentity: null,
                MotifIdentity: "surface-home-source-foundation");
        }

        return Version4SurfaceAt(state.Seed, address);
    }

    private static CellSemantics Version6SurfaceAt(ChronicleState state, WorldAddress address)
    {
        if (state.Home is null)
        {
            return Version5SurfaceAt(state, address);
        }

        var start = AgentRules.ResonanceListenerStartAddress(state);
        var waiting = AgentRules.ResonanceListenerWaitingAddress(state);
        var roadRoll = AgentRules.ResonanceListenerRoadRollAddress(state);
        if ((address.Stratum == start.Stratum &&
             address.Y == start.Y &&
             address.X >= start.X &&
             address.X <= waiting.X) ||
            address == roadRoll)
        {
            return new CellSemantics(
                address.Stratum,
                WorldGround.Grass,
                Feature: null,
                DurableIdentity: null,
                MotifIdentity: address == roadRoll
                    ? "surface-wayfarer-rest"
                    : "surface-wayfarer-route");
        }

        return Version5SurfaceAt(state, address);
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
