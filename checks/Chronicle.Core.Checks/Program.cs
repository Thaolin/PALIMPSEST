using Chronicle.Core;

VerifyLegacySaveCompatibility();
VerifyWorldGrammarVersionMigrationAndPinning();
VerifySlice1SaveMigratesCodex();
VerifySerializedIntent();
VerifyMovementRequiresIntent();
VerifyUpIntent();
VerifyFlyAvailability();
VerifyFlyRequiresIntent();
VerifyCoordinatePreservingRoundTrip();
VerifyFlyAtSecondAddress();
VerifySkyGeneration();
VerifySkySeedDeterminism();
VerifySkyMovementBeyondFormerBounds();
VerifyWideWorldCoordinates();
VerifyLandmarkJourney();
VerifyReturnJourney();
VerifyInterleavedReplay();
VerifyPause();
VerifyClockSpeeds();
VerifyCardinalSurfaceMovement();
VerifySurfaceGeneration();
VerifySurfaceAreaSnapshotBoundsOrderAndDeterminism();
VerifyLegacyAreaSemanticsAndReadOnly();
VerifyVersion1SurfaceGrammarFixtures();
VerifyVersion1SurfaceQueryInvariance();
VerifyVersion1SkyGrammarAndDurableSubjects();
VerifyVersion1CardinalAdjacencyContext();
VerifyVersion1CoordinateLimitsDoNotWrap();
VerifyAreaQueriesStayOutOfPersistenceAndReplay();
VerifySaveLoad();
VerifySkySaveLoad();
VerifyStudyReplay();
VerifyStudyRequiresBell();
VerifyStudyPause();
VerifyStudyStopsWhenLeavingBell();
VerifyStudySaveLoad();
VerifyStudyCompletionIsIdempotent();
VerifyCodexAndStudySerialization();
VerifySlice2ASaveMigratesLoadoutAndStone();
VerifyLoadoutHasEightSerializableSlots();
VerifyOnlyCodexLanguageCanBeEquipped();
VerifyVerbCannotOccupyTwoSlots();
VerifyUnequippedFlyIsUnavailable();
VerifyIntrinsicFlyUsesLoadoutSlot();
VerifyFlyStoneMovesOnlyTheLooseStone();
VerifyFlyStoneReturnsTheLooseStone();
VerifyFlyStoneRejectsInvalidTargets();
VerifyLoadoutReplayAndSaveLoad();
VerifyDeathRequiresLivingIncarnationAtBell();
VerifyAwaitingReplacementFreezesChronicle();
VerifyReplacementPreservesChronicleAndResetsBody();
VerifyLifecycleSaveEnvelopeAndMigration();
VerifyLifecycleReplay();

Console.WriteLine(
    "PASS: Gate 3A Core World Grammar plus Slice 0 through Slice 2C regression and save compatibility verified.");

static void VerifyLegacySaveCompatibility()
{
    const string slice0Json =
        """
        {
          "Seed": 41337,
          "Tick": 17,
          "Address": {
            "Stratum": "surface",
            "X": 4,
            "Y": -3
          },
          "Speed": 4
        }
        """;

    var restored = ChronicleSaveCodec.Deserialize(slice0Json);

    Assert(restored.Seed == 41_337, "A literal Slice 0 save must preserve its seed.");
    Assert(restored.Tick == 17, "A literal Slice 0 save must preserve its tick.");
    Assert(restored.Address == new WorldAddress("surface", 4, -3), "A literal Slice 0 save must preserve its address.");
    Assert(restored.Speed == ChronicleSpeed.Fast, "A literal Slice 0 save must preserve its speed.");
    Assert(restored.Intent == OpeningIntent.Unchosen, "A Slice 0 save without Intent must load as Unchosen.");
    Assert(!restored.CanFly, "A legacy save must not grant Fly.");
    Assert(
        restored.ActiveLoadout.Slots.Count == LoadoutState.SlotCount &&
        restored.ActiveLoadout.Slots.All(slot => slot.IsEmpty),
        "A Slice 0 save must migrate to eight empty Loadout slots.");
    Assert(
        restored.LooseStoneAddress == ChronicleState.InitialLooseStoneAddress,
        "A Slice 0 save must gain the fixed loose Stone without serializing terrain.");
}

static void VerifyWorldGrammarVersionMigrationAndPinning()
{
    const string predecessorJson =
        """
        {
          "Seed": 41337,
          "Tick": 17,
          "Address": {
            "Stratum": "surface",
            "X": 4,
            "Y": -3
          },
          "Speed": 4
        }
        """;

    var predecessor = ChronicleSaveCodec.Deserialize(predecessorJson);
    Assert(
        predecessor.WorldGrammarVersion == 0,
        "A predecessor save without a World Grammar version must retain legacy version 0.");

    var newChronicle = ChronicleState.Begin(41_337);
    Assert(
        newChronicle.WorldGrammarVersion == 1,
        "A newly created Chronicle must pin World Grammar version 1.");

    var json = ChronicleSaveCodec.Serialize(newChronicle);
    var restored = ChronicleSaveCodec.Deserialize(json);
    Assert(
        json.Contains("\"WorldGrammarVersion\"", StringComparison.Ordinal),
        "A new Chronicle save must include its pinned World Grammar version.");
    Assert(
        restored.WorldGrammarVersion == 1,
        "Save/load must retain a new Chronicle's pinned World Grammar version.");
}

static void VerifySlice1SaveMigratesCodex()
{
    const string slice1Json =
        """
        {
          "Seed": 41337,
          "Tick": 29,
          "Address": {
            "Stratum": "sky",
            "X": 0,
            "Y": -4
          },
          "Speed": 1,
          "Intent": 1
        }
        """;

    var restored = ChronicleSaveCodec.Deserialize(slice1Json);

    Assert(restored.Intent == OpeningIntent.Up, "A literal Slice 1 save must preserve UP.");
    Assert(restored.Codex.HasFly, "A literal Slice 1 UP save must migrate Fly into the Codex.");
    Assert(restored.CanFly, "Migrated Fly must remain available to the Slice 1 journey.");
    Assert(!restored.Codex.HasStone, "A migrated Slice 1 save must not invent Stone.");
    Assert(restored.Study.StoneUnderstanding == 0, "A migrated Slice 1 save must begin with no Study progress.");
    Assert(
        restored.ActiveLoadout[0].IsIntrinsicFly &&
        restored.ActiveLoadout.Slots.Skip(1).All(slot => slot.IsEmpty),
        "A Slice 1 UP save must migrate intrinsic Fly into only the first Loadout slot.");
}

static void VerifySerializedIntent()
{
    var state = ChronicleState.Begin(41_337) with
    {
        Intent = OpeningIntent.Up,
        Codex = new CodexState(HasFly: true, HasStone: false),
        Loadout = IntrinsicFlyLoadout(),
    };
    var json = ChronicleSaveCodec.Serialize(state);

    Assert(json.Contains("\"Intent\"", StringComparison.Ordinal), "Saved JSON must include Intent.");
    Assert(json.Contains("\"Codex\"", StringComparison.Ordinal), "Saved JSON must include the explicit Codex.");
    Assert(!json.Contains("CanFly", StringComparison.Ordinal), "Saved JSON must not include derived CanFly.");
    Assert(!json.Contains("Tiles", StringComparison.Ordinal), "Saved JSON must not contain generated tiles.");
}

static void VerifyMovementRequiresIntent()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    var before = simulation.State;

    simulation.Apply(new MoveIncarnation(1, 0));

    Assert(simulation.State == before, "Movement before choosing UP must leave state unchanged.");
}

static void VerifyUpIntent()
{
    var first = new ChronicleSimulation(ChronicleState.Begin(41_337));
    var second = new ChronicleSimulation(ChronicleState.Begin(41_337));

    first.Apply(new ChooseUpIntent());
    first.Apply(new ChooseUpIntent());
    second.Apply(new ChooseUpIntent());

    Assert(first.State.Intent == OpeningIntent.Up, "ChooseUpIntent must save UP.");
    Assert(first.State.CanFly, "UP must derive the Fly capability.");
    Assert(first.State == second.State, "Repeating ChooseUpIntent must be deterministic and idempotent.");
}

static void VerifyFlyAvailability()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    Assert(simulation.FlyDestination is null, "Fly must be unavailable before UP.");

    simulation.Apply(new ChooseUpIntent());
    Assert(
        simulation.FlyDestination == new WorldAddress(SkyStratum.StratumName, 0, 0),
        "A surface address must expose the matching sky address.");

    simulation.Apply(new UseLoadoutSlot(0));
    Assert(
        simulation.FlyDestination == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        "A sky address must expose the matching surface address.");

    simulation.Apply(new MoveIncarnation(1, 0));
    Assert(
        simulation.FlyDestination == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0),
        "Fly must remain available away from the original address.");

    var unsupportedState = ChronicleState.Begin(41_337) with
    {
        Intent = OpeningIntent.Up,
        Codex = new CodexState(HasFly: true, HasStone: false),
        Loadout = IntrinsicFlyLoadout(),
        Address = new WorldAddress("underworld", 3, 4),
    };
    var unsupported = new ChronicleSimulation(unsupportedState);
    Assert(unsupported.FlyDestination is null, "Fly must not invent a destination in an unsupported Stratum.");
    unsupported.Apply(new UseLoadoutSlot(0));
    Assert(unsupported.State == unsupportedState, "Fly in an unsupported Stratum must leave state unchanged.");
}

static void VerifyFlyRequiresIntent()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    var before = simulation.State;

    simulation.Apply(new UseLoadoutSlot(0));

    Assert(simulation.State == before, "Fly before choosing UP must leave state unchanged.");
}

static void VerifyCoordinatePreservingRoundTrip()
{
    var surfaceAddress = new WorldAddress(SurfacePatch.SurfaceStratum, 2, -1);
    var simulation = new ChronicleSimulation(
        ChronicleState.Begin(41_337) with
        {
            Intent = OpeningIntent.Up,
            Codex = new CodexState(HasFly: true, HasStone: false),
            Loadout = IntrinsicFlyLoadout(),
            Address = surfaceAddress,
        });

    simulation.Apply(new UseLoadoutSlot(0));
    Assert(
        simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 2, -1),
        "Fly must preserve coordinates when entering the sky.");

    simulation.Apply(new UseLoadoutSlot(0));
    Assert(simulation.State.Address == surfaceAddress, "Fly must preserve coordinates when returning to the surface.");
}

static void VerifyFlyAtSecondAddress()
{
    var state = ChronicleState.Begin(41_337) with
    {
        Intent = OpeningIntent.Up,
        Codex = new CodexState(HasFly: true, HasStone: false),
        Loadout = IntrinsicFlyLoadout(),
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, -12, 34),
    };
    var simulation = new ChronicleSimulation(state);

    simulation.Apply(new UseLoadoutSlot(0));

    Assert(
        simulation.State.Address == new WorldAddress(SkyStratum.StratumName, -12, 34),
        "Fly must work deterministically at a second arbitrary address.");
}

static void VerifySkyGeneration()
{
    var state = ChronicleState.Begin(41_337) with
    {
        Intent = OpeningIntent.Up,
        Address = new WorldAddress(SkyStratum.StratumName, -1, -8),
    };
    var sky = SkyStratum.Generate(state);

    Assert(sky.Tiles.Count == 165, "The visible sky patch must contain exactly 15x11 tiles.");
    Assert(sky.Center == state.Address, "The visible sky patch must be centered on the Incarnation.");
    Assert(sky.Tiles.Count(tile => tile.Terrain == SkyTerrain.Landmark) == 1, "The sky must contain exactly one Landmark.");
    Assert(sky.TileAt(SkyStratum.LandmarkAddress).Terrain == SkyTerrain.Landmark, "The Bell must occupy its fixed address.");

    for (var index = 0; index < sky.Tiles.Count; index++)
    {
        var expected = new WorldAddress(
            SkyStratum.StratumName,
            state.Address.X - SkyStratum.Width / 2 + index % SkyStratum.Width,
            state.Address.Y - SkyStratum.Height / 2 + index / SkyStratum.Width);
        Assert(sky.Tiles[index].Address == expected, "Sky tiles must use stable row-major ordering.");
    }

    var farState = state with { Address = new WorldAddress(SkyStratum.StratumName, 100, 100) };
    var farSky = SkyStratum.Generate(farState);
    Assert(
        farSky.Tiles.All(tile => tile.Terrain != SkyTerrain.Landmark),
        "A player-centered patch must not duplicate an off-screen Landmark.");
}

static void VerifySkySeedDeterminism()
{
    var state = ChronicleState.Begin(41_337) with
    {
        Intent = OpeningIntent.Up,
        Address = new WorldAddress(SkyStratum.StratumName, 4, -3),
    };
    var first = SkyStratum.Generate(state);
    var second = SkyStratum.Generate(state);
    var otherSeed = SkyStratum.Generate(state with { Seed = state.Seed + 1 });
    var formerlyCollidingLowSeed = SkyStratum.Generate(state with { Seed = 1 });
    var formerlyCollidingHighSeed = SkyStratum.Generate(state with { Seed = 1L << 32 });

    Assert(first.Tiles.SequenceEqual(second.Tiles), "The same seed must generate the same ordered sky tiles.");
    Assert(
        first.Tiles.Zip(otherSeed.Tiles).Any(pair => pair.First.Terrain != pair.Second.Terrain),
        "A different seed must change cloud decoration.");
    Assert(
        formerlyCollidingLowSeed.Tiles
            .Zip(formerlyCollidingHighSeed.Tiles)
            .Any(pair => pair.First.Terrain != pair.Second.Terrain),
        "Distinct low- and high-half seed bits must not collapse to the same sky decoration.");
}

static void VerifySkyMovementBeyondFormerBounds()
{
    var state = ChronicleState.Begin(41_337) with
    {
        Intent = OpeningIntent.Up,
        Address = new WorldAddress(SkyStratum.StratumName, 7, 5),
    };
    var simulation = new ChronicleSimulation(state);

    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.Apply(new MoveIncarnation(0, 1));

    Assert(
        simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 8, 6),
        "Sky movement must continue beyond the former fixed patch boundary.");
}

static void VerifyWideWorldCoordinates()
{
    var center = new WorldAddress(SkyStratum.StratumName, int.MaxValue, int.MaxValue);
    var state = ChronicleState.Begin(41_337) with
    {
        Intent = OpeningIntent.Up,
        Codex = new CodexState(HasFly: true, HasStone: false),
        Address = center,
    };
    var sky = SkyStratum.Generate(state);

    Assert(sky.Center == center, "Sky generation must preserve a center at the former 32-bit boundary.");
    Assert(sky.Contains(center), "A generated sky patch must always contain its own center.");
    Assert(
        sky.Tiles[^1].Address ==
        new WorldAddress(
            SkyStratum.StratumName,
            (long)int.MaxValue + SkyStratum.Width / 2,
            (long)int.MaxValue + SkyStratum.Height / 2),
        "Sky generation must not wrap coordinates at the former 32-bit boundary.");

    var simulation = new ChronicleSimulation(state);
    simulation.Apply(new MoveIncarnation(1, 0));
    Assert(
        simulation.State.Address ==
        new WorldAddress(SkyStratum.StratumName, (long)int.MaxValue + 1, int.MaxValue),
        "Movement must cross the former 32-bit coordinate boundary without wrapping.");

    var restored = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(simulation.State));
    Assert(restored == simulation.State, "Save/load must preserve wide World Address coordinates.");
}

static void VerifyLandmarkJourney()
{
    var simulation = BeginWithUp();
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    Assert(simulation.State.Address == SkyStratum.LandmarkAddress, "Four north moves from the sky anchor must reach the Bell.");
    Assert(
        SkyStratum.Generate(simulation.State).TileAt(simulation.State.Address).Terrain == SkyTerrain.Landmark,
        "The destination tile must be the generated Landmark.");
}

static void VerifyReturnJourney()
{
    var simulation = BeginWithUp();
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, 1));
    }

    simulation.Apply(new UseLoadoutSlot(0));

    Assert(
        simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        "The Bell journey must return exactly to surface (0, 0).");
    Assert(simulation.State.CanFly, "Returning to the surface must retain Fly.");
}

static void VerifyInterleavedReplay()
{
    var first = ReplayInterleaved(41_337);
    var second = ReplayInterleaved(41_337);

    Assert(first == second, "The same seed and interleaved command/pulse stream must replay to the same state.");
}

static void VerifyPause()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Paused));
    var paused = simulation.State;

    simulation.AdvanceOneTick();
    Assert(simulation.State == paused, "AdvanceOneTick must not advance a paused Chronicle.");
    simulation.AdvanceClockPulse();
    Assert(simulation.State == paused, "AdvanceClockPulse must not advance a paused Chronicle.");
}

static void VerifyClockSpeeds()
{
    foreach (var (speed, expectedTicks) in new[]
    {
        (ChronicleSpeed.Slow, 1L),
        (ChronicleSpeed.Normal, 2L),
        (ChronicleSpeed.Fast, 4L),
        (ChronicleSpeed.Paused, 0L),
    })
    {
        var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
        simulation.Apply(new SetChronicleSpeed(speed));
        var beforePulse = simulation.State.Tick;

        Assert(simulation.State.Speed == speed, "A commanded Chronicle speed must remain inspectable.");
        simulation.AdvanceClockPulse();
        Assert(simulation.State.Tick - beforePulse == expectedTicks, $"{speed} must advance {expectedTicks} ticks per pulse.");
    }
}

static void VerifyCardinalSurfaceMovement()
{
    var simulation = BeginWithUp();
    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.Apply(new MoveIncarnation(0, -1));
    simulation.Apply(new MoveIncarnation(-1, 0));

    Assert(
        simulation.State.Address == new WorldAddress("surface", 0, -1),
        "Cardinal surface movement must retain its Slice 0 behavior after choosing Intent.");
    var beforeInvalidMove = simulation.State;
    AssertThrows<ArgumentException>(
        () => simulation.Apply(new MoveIncarnation(1, 1)),
        "A non-cardinal move must be rejected.");
    Assert(simulation.State == beforeInvalidMove, "A rejected move must not change Chronicle state.");
}

static void VerifySurfaceGeneration()
{
    var state = new ChronicleState(41_337, 8, new WorldAddress("surface", 4, -3), ChronicleSpeed.Normal);
    var first = SurfacePatch.Generate(state);
    var second = SurfacePatch.Generate(state);
    var otherSeed = SurfacePatch.Generate(state with { Seed = state.Seed + 1 });

    Assert(first.Tiles.Count == 15 * 11, "A surface patch must contain its 15x11 tile area.");
    Assert(first.Tiles.SequenceEqual(second.Tiles), "The same state must generate the same row-major surface tiles.");
    Assert(
        first.Tiles.Zip(otherSeed.Tiles).Any(pair => pair.First != pair.Second),
        "Changing the seed must change at least one generated surface tile.");
}

static void VerifySurfaceAreaSnapshotBoundsOrderAndDeterminism()
{
    var state = ChronicleState.Begin(41_337);
    var bounds = new WorldRectangle(MinX: -2, MinY: -3, Width: 3, Height: 2);

    var first = WorldArea.Generate(state, SurfacePatch.SurfaceStratum, bounds);
    var second = WorldArea.Generate(state, SurfacePatch.SurfaceStratum, bounds);

    Assert(first.Cells.Count == 6, "A bounded Surface snapshot must contain exactly its requested absolute rectangle.");
    Assert(
        first.Cells[0].Address == new WorldAddress("surface", -2, -3),
        "A bounded Surface snapshot must begin at the rectangle's literal minimum address.");
    Assert(
        first.Cells[1].Address == new WorldAddress("surface", -1, -3),
        "A bounded Surface snapshot must order the next literal address across its first row.");
    Assert(
        first.Cells[^1].Address == new WorldAddress("surface", 0, -2),
        "A bounded Surface snapshot must end at the rectangle's literal final row-major address.");
    Assert(
        first.Cells.SequenceEqual(second.Cells),
        "The same Chronicle, Surface stratum, and absolute rectangle must return equal ordered cells.");
}

static void VerifyLegacyAreaSemanticsAndReadOnly()
{
    var surfaceState = ChronicleState.Begin(41_337) with { WorldGrammarVersion = 0 };
    var skyState = surfaceState with { Address = SkyStratum.LandmarkAddress };
    var surfaceBounds = new WorldRectangle(MinX: -2, MinY: -1, Width: 3, Height: 2);
    var skyBounds = new WorldRectangle(MinX: -1, MinY: -5, Width: 3, Height: 3);
    var surfaceBefore = ChronicleSaveCodec.Serialize(surfaceState);
    var skyBefore = ChronicleSaveCodec.Serialize(skyState);

    var surface = WorldArea.Generate(surfaceState, SurfacePatch.SurfaceStratum, surfaceBounds);
    var sky = WorldArea.Generate(skyState, SkyStratum.StratumName, skyBounds);
    var legacySurface = SurfacePatch.Generate(surfaceState);
    var legacySky = SkyStratum.Generate(skyState);

    foreach (var cell in surface.Cells)
    {
        var legacy = legacySurface.Tiles.Single(tile => tile.Address == cell.Address);
        var expected = LegacySurfaceSemantics(legacy.Terrain);

        Assert(cell.Ground == expected.Ground, "Version 0 Surface ground must reproduce legacy terrain semantics.");
        Assert(cell.Feature == expected.Feature, "Version 0 Surface features must reproduce legacy terrain semantics.");
        Assert(cell.DurableIdentity is null, "Version 0 Surface cells must not invent durable identities.");
        Assert(
            cell.SameFormAdjacency == LegacySurfaceAdjacency(legacySurface, legacy),
            "Version 0 Surface adjacency must match its legacy ground-and-feature form.");
    }

    foreach (var cell in sky.Cells)
    {
        var legacy = legacySky.TileAt(cell.Address);
        var expected = LegacySkySemantics(legacy.Terrain);

        Assert(cell.Ground == expected.Ground, "Version 0 Sky ground must reproduce legacy terrain semantics.");
        Assert(cell.Feature == expected.Feature, "Version 0 Sky features must reproduce legacy terrain semantics.");
        Assert(
            cell.DurableIdentity == expected.DurableIdentity,
            "Version 0 Sky durable identity must reproduce legacy Landmark semantics.");
        Assert(
            cell.SameFormAdjacency == LegacySkyAdjacency(legacySky, legacy),
            "Version 0 Sky adjacency must match its legacy ground-and-feature form.");
    }

    var bell = sky.Cells.Single(cell => cell.Address == SkyStratum.LandmarkAddress);
    Assert(bell.Feature == WorldFeature.Landmark, "The legacy Bell must remain a Landmark feature.");
    Assert(
        bell.DurableIdentity == SkyStratum.LandmarkName,
        "The legacy Bell must retain its durable identity through a version 0 area snapshot.");
    Assert(
        ChronicleSaveCodec.Serialize(surfaceState) == surfaceBefore &&
        ChronicleSaveCodec.Serialize(skyState) == skyBefore,
        "Read-only version 0 area requests must not mutate Chronicle state.");
}

static void VerifyVersion1SurfaceGrammarFixtures()
{
    var bounds = new WorldRectangle(MinX: -128, MinY: -128, Width: 256, Height: 256);
    var fixtures = new[]
    {
        (Seed: 41_337L, Area: WorldArea.Generate(ChronicleState.Begin(41_337), SurfacePatch.SurfaceStratum, bounds)),
        (Seed: 41_338L, Area: WorldArea.Generate(ChronicleState.Begin(41_338), SurfacePatch.SurfaceStratum, bounds)),
        (Seed: 90_421L, Area: WorldArea.Generate(ChronicleState.Begin(90_421), SurfacePatch.SurfaceStratum, bounds)),
    };

    foreach (var fixture in fixtures)
    {
        var cells = fixture.Area.Cells;
        Assert(cells.Any(cell => cell.Ground == WorldGround.Water), $"Fixture {fixture.Seed} must contain water semantics.");
        Assert(
            cells.Any(cell =>
                cell.Feature is null &&
                cell.Ground is WorldGround.Grass or WorldGround.Soil),
            $"Fixture {fixture.Seed} must contain a Grass or Soil clearing.");
        Assert(cells.Any(cell => cell.Feature == WorldFeature.Vegetation), $"Fixture {fixture.Seed} must contain vegetation.");
        Assert(cells.Any(cell => cell.Feature == WorldFeature.Stone), $"Fixture {fixture.Seed} must contain stone.");
        Assert(
            HasVersion1SurfaceInteraction(fixture.Area),
            $"Fixture {fixture.Seed} must expose a named water/ridge or clearing/vegetation interaction.");
        Assert(
            HasNamedMotifSpanningAtLeast45Cells(cells),
            $"Fixture {fixture.Seed} must retain a named motif across at least three 15-cell viewport widths.");
    }

    Assert(
        !fixtures[0].Area.Cells.SequenceEqual(fixtures[1].Area.Cells) &&
        !fixtures[0].Area.Cells.SequenceEqual(fixtures[2].Area.Cells) &&
        !fixtures[1].Area.Cells.SequenceEqual(fixtures[2].Area.Cells),
        "Fixture seeds must differ in ordered Surface semantics, not only Chronicle seed metadata.");
}

static void VerifyVersion1SurfaceQueryInvariance()
{
    var state = ChronicleState.Begin(41_337);
    var largeBounds = new WorldRectangle(MinX: -40, MinY: -30, Width: 24, Height: 16);
    var overlapBounds = new WorldRectangle(MinX: -35, MinY: -25, Width: 18, Height: 10);
    var before = ChronicleSaveCodec.Serialize(state);

    var large = WorldArea.Generate(state, SurfacePatch.SurfaceStratum, largeBounds);
    var overlap = WorldArea.Generate(state, SurfacePatch.SurfaceStratum, overlapBounds);
    var assembled = new[]
        {
            WorldArea.Generate(state, SurfacePatch.SurfaceStratum, new WorldRectangle(-40, -30, 12, 8)),
            WorldArea.Generate(state, SurfacePatch.SurfaceStratum, new WorldRectangle(-28, -30, 12, 8)),
            WorldArea.Generate(state, SurfacePatch.SurfaceStratum, new WorldRectangle(-40, -22, 12, 8)),
            WorldArea.Generate(state, SurfacePatch.SurfaceStratum, new WorldRectangle(-28, -22, 12, 8)),
        }
        .SelectMany(area => area.Cells)
        .OrderBy(cell => cell.Address.Y)
        .ThenBy(cell => cell.Address.X)
        .ToArray();
    var reversedOverlap = WorldArea.Generate(state, SurfacePatch.SurfaceStratum, overlapBounds);
    var reversedLarge = WorldArea.Generate(state, SurfacePatch.SurfaceStratum, largeBounds);
    var largeByAddress = large.Cells.ToDictionary(cell => cell.Address);

    Assert(
        large.Cells.SequenceEqual(assembled),
        "A version 1 Surface rectangle must equal the exact row-major assembly of its four bounded subrequests.");
    Assert(
        overlap.Cells.All(cell => largeByAddress[cell.Address] == cell),
        "Overlapping version 1 Surface requests must agree exactly at every shared World Address.");
    Assert(
        reversedOverlap.Cells.SequenceEqual(overlap.Cells) && reversedLarge.Cells.SequenceEqual(large.Cells),
        "Version 1 Surface semantics must not depend on bounded-query order.");
    Assert(
        ChronicleSaveCodec.Serialize(state) == before,
        "Version 1 Surface area requests must leave Chronicle state byte-for-byte unchanged.");
}

static void VerifyVersion1SkyGrammarAndDurableSubjects()
{
    var bounds = new WorldRectangle(MinX: -64, MinY: -64, Width: 128, Height: 128);
    var overlapBounds = new WorldRectangle(MinX: -32, MinY: -28, Width: 48, Height: 48);
    var fixtures = new[]
    {
        (Seed: 41_337L, Area: WorldArea.Generate(ChronicleState.Begin(41_337), SkyStratum.StratumName, bounds), Overlap: WorldArea.Generate(ChronicleState.Begin(41_337), SkyStratum.StratumName, overlapBounds)),
        (Seed: 41_338L, Area: WorldArea.Generate(ChronicleState.Begin(41_338), SkyStratum.StratumName, bounds), Overlap: WorldArea.Generate(ChronicleState.Begin(41_338), SkyStratum.StratumName, overlapBounds)),
        (Seed: 90_421L, Area: WorldArea.Generate(ChronicleState.Begin(90_421), SkyStratum.StratumName, bounds), Overlap: WorldArea.Generate(ChronicleState.Begin(90_421), SkyStratum.StratumName, overlapBounds)),
    };

    foreach (var fixture in fixtures)
    {
        var cells = fixture.Area.Cells;
        var byAddress = cells.ToDictionary(cell => cell.Address);
        var bell = cells.Single(cell => cell.Address == SkyStratum.LandmarkAddress);

        Assert(cells.Any(cell => cell.Feature == WorldFeature.Cloud), $"Sky fixture {fixture.Seed} must contain cloud-bank cells.");
        Assert(
            cells.Any(cell => cell.Ground == WorldGround.OpenSky && cell.Feature is null),
            $"Sky fixture {fixture.Seed} must contain open-lane cells.");
        Assert(
            HasNamedCloudBankSpanningAtLeast45Cells(cells),
            $"Sky fixture {fixture.Seed} must retain a named cloud bank across at least three 15-cell viewport widths.");
        Assert(
            cells
                .Where(cell =>
                    Math.Abs(cell.Address.X - SkyStratum.LandmarkAddress.X) <= 2 &&
                    Math.Abs(cell.Address.Y - SkyStratum.LandmarkAddress.Y) <= 2 &&
                    cell.Address != SkyStratum.LandmarkAddress)
                .All(cell => cell.Feature != WorldFeature.Cloud),
            $"Sky fixture {fixture.Seed} must keep a cloud-free two-cell approach around the Bell.");
        Assert(
            bell.Address == SkyStratum.LandmarkAddress &&
            bell.Feature == WorldFeature.Landmark &&
            bell.DurableIdentity == SkyStratum.LandmarkName,
            "The Bell must remain the established Landmark at its exact durable address.");
        Assert(
            fixture.Overlap.Cells.All(cell => byAddress[cell.Address] == cell),
            $"Overlapping Sky requests for fixture {fixture.Seed} must agree at every shared World Address.");
    }

    Assert(
        !fixtures[0].Area.Cells.SequenceEqual(fixtures[1].Area.Cells) &&
        !fixtures[0].Area.Cells.SequenceEqual(fixtures[2].Area.Cells) &&
        !fixtures[1].Area.Cells.SequenceEqual(fixtures[2].Area.Cells),
        "Sky fixture seeds must differ in ordered spatial semantics, not only Chronicle seed metadata.");

    var stoneSimulation = new ChronicleSimulation(LearnedAtSurface());
    Assert(
        stoneSimulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone)).Applied,
        "The durable-subject fixture must equip Fly[Stone].");
    Assert(
        stoneSimulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress)).Applied,
        "The durable-subject fixture must move the loose Stone into the sky.");
    var restoredStoneState = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(stoneSimulation.State));
    var durableArea = WorldArea.Generate(
        restoredStoneState,
        SkyStratum.StratumName,
        new WorldRectangle(MinX: -2, MinY: -6, Width: 5, Height: 8));
    var stone = durableArea.Cells.Single(cell => cell.Address == new WorldAddress(SkyStratum.StratumName, 1, 0));
    var durableBell = durableArea.Cells.Single(cell => cell.Address == SkyStratum.LandmarkAddress);

    Assert(ChronicleState.LooseStoneIdentity == "Loose Stone", "The loose Stone must have one stable durable identity.");
    Assert(
        stone.Ground == WorldGround.OpenSky &&
        stone.Feature == WorldFeature.Stone &&
        stone.DurableIdentity == ChronicleState.LooseStoneIdentity,
        "The moved loose Stone must overlay generated Sky semantics after save/load.");
    Assert(
        durableBell.Feature == WorldFeature.Landmark &&
        durableBell.DurableIdentity == SkyStratum.LandmarkName,
        "Overlaying the loose Stone must not displace the Bell's durable identity.");
}

static void VerifyVersion1CardinalAdjacencyContext()
{
    var state = ChronicleState.Begin(41_337);
    var surface = WorldArea.Generate(
        state,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(MinX: -16, MinY: -16, Width: 33, Height: 33));
    var sky = WorldArea.Generate(
        state,
        SkyStratum.StratumName,
        new WorldRectangle(MinX: -16, MinY: -20, Width: 33, Height: 33));

    Assert(surface.Cells.Any(cell => cell.Ground == WorldGround.Water), "The adjacency fixture must include water.");
    Assert(sky.Cells.Any(cell => cell.Feature == WorldFeature.Cloud), "The adjacency fixture must include cloud.");
    AssertVersion1AdjacencyContext(surface, "Surface");
    AssertVersion1AdjacencyContext(sky, "Sky");
}

static void VerifyVersion1CoordinateLimitsDoNotWrap()
{
    var state = ChronicleState.Begin(41_338);

    foreach (var stratum in new[] { SurfacePatch.SurfaceStratum, SkyStratum.StratumName })
    {
        var maximum = WorldArea.Generate(
            state,
            stratum,
            new WorldRectangle(long.MaxValue, 0, 1, 1)).Cells.Single();
        var minimum = WorldArea.Generate(
            state,
            stratum,
            new WorldRectangle(long.MinValue, 0, 1, 1)).Cells.Single();

        Assert(
            !maximum.SameFormAdjacency.East,
            $"The {stratum} cell at long.MaxValue must not wrap its east adjacency to long.MinValue.");
        Assert(
            !minimum.SameFormAdjacency.West,
            $"The {stratum} cell at long.MinValue must not wrap its west adjacency to long.MaxValue.");
        Assert(
            WorldArea.Generate(
                state,
                stratum,
                new WorldRectangle(long.MaxValue, 0, 1, 1)).Cells.Single() == maximum,
            $"The {stratum} grammar must remain deterministic at the maximum representable address.");
    }
}

static void VerifyAreaQueriesStayOutOfPersistenceAndReplay()
{
    var movedStone = new ChronicleSimulation(LearnedAtSurface());
    movedStone.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    movedStone.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    var json = ChronicleSaveCodec.Serialize(movedStone.State);

    Assert(json.Contains("\"WorldGrammarVersion\"", StringComparison.Ordinal), "A v1 save must preserve its World Grammar version.");
    Assert(json.Contains("\"LooseStoneAddress\"", StringComparison.Ordinal), "A v1 save must preserve the moved loose Stone's durable address.");

    foreach (var generatedOrPresentationConcept in new[]
    {
        "Cells",
        "Tiles",
        "Motif",
        "Adjacency",
        "Inspector",
        "Overlay",
        "Zoom",
        "Render",
        "Capture",
    })
    {
        Assert(
            !json.Contains(generatedOrPresentationConcept, StringComparison.OrdinalIgnoreCase),
            $"A Chronicle save must not persist generated or presentation concept '{generatedOrPresentationConcept}'.");
    }

    Assert(
        ReplayWithOptionalAreaQueries(interleaveAreaQueries: false) ==
        ReplayWithOptionalAreaQueries(interleaveAreaQueries: true),
        "Interleaved Surface and Sky area queries must not affect an otherwise identical Chronicle replay.");
}

static ChronicleState ReplayWithOptionalAreaQueries(bool interleaveAreaQueries)
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseUpIntent());

    if (interleaveAreaQueries)
    {
        WorldArea.Generate(simulation.State, SkyStratum.StratumName, new WorldRectangle(-3, -7, 7, 7));
        WorldArea.Generate(simulation.State, SurfacePatch.SurfaceStratum, new WorldRectangle(-3, -3, 7, 7));
    }

    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Fast));
    simulation.AdvanceClockPulse();
    simulation.Apply(new UseLoadoutSlot(0));

    if (interleaveAreaQueries)
    {
        WorldArea.Generate(simulation.State, SurfacePatch.SurfaceStratum, new WorldRectangle(-6, -4, 9, 5));
        WorldArea.Generate(simulation.State, SkyStratum.StratumName, new WorldRectangle(-6, -8, 9, 9));
    }

    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.AdvanceClockPulse();
    return simulation.State;
}

static void AssertVersion1AdjacencyContext(WorldArea area, string stratumName)
{
    var cells = area.Cells.ToDictionary(cell => cell.Address);

    foreach (var cell in area.Cells.Where(cell =>
                 cell.Address.X > area.Bounds.MinX &&
                 cell.Address.X < area.Bounds.MinX + area.Bounds.Width - 1 &&
                 cell.Address.Y > area.Bounds.MinY &&
                 cell.Address.Y < area.Bounds.MinY + area.Bounds.Height - 1))
    {
        Assert(
            cell.SameFormAdjacency.North == HasSameGroundAndFeature(cells, cell, 0, -1),
            $"{stratumName} North adjacency must match the neighboring semantic form.");
        Assert(
            cell.SameFormAdjacency.East == HasSameGroundAndFeature(cells, cell, 1, 0),
            $"{stratumName} East adjacency must match the neighboring semantic form.");
        Assert(
            cell.SameFormAdjacency.South == HasSameGroundAndFeature(cells, cell, 0, 1),
            $"{stratumName} South adjacency must match the neighboring semantic form.");
        Assert(
            cell.SameFormAdjacency.West == HasSameGroundAndFeature(cells, cell, -1, 0),
            $"{stratumName} West adjacency must match the neighboring semantic form.");
    }
}

static bool HasSameGroundAndFeature(
    IReadOnlyDictionary<WorldAddress, WorldCell> cells,
    WorldCell cell,
    int deltaX,
    int deltaY)
{
    var neighbor = cells[cell.Address with
    {
        X = cell.Address.X + deltaX,
        Y = cell.Address.Y + deltaY,
    }];
    return cell.Ground == neighbor.Ground && cell.Feature == neighbor.Feature;
}

static bool HasNamedCloudBankSpanningAtLeast45Cells(IReadOnlyList<WorldCell> cells) =>
    HasConnectedNamedFormSpanning(
        cells,
        cell => cell.Feature == WorldFeature.Cloud,
        minimumSpan: 45);

static bool HasVersion1SurfaceInteraction(WorldArea area)
{
    var cells = area.Cells.ToDictionary(cell => cell.Address);

    return cells.Values.Any(cell =>
        IsVersion1SurfaceInteraction(cells, cell, 1, 0) ||
        IsVersion1SurfaceInteraction(cells, cell, 0, 1));
}

static bool IsVersion1SurfaceInteraction(
    IReadOnlyDictionary<WorldAddress, WorldCell> cells,
    WorldCell cell,
    int deltaX,
    int deltaY) =>
    cells.TryGetValue(
        cell.Address with { X = cell.Address.X + deltaX, Y = cell.Address.Y + deltaY },
        out var neighbor) &&
    !string.IsNullOrWhiteSpace(cell.MotifIdentity) &&
    !string.IsNullOrWhiteSpace(neighbor.MotifIdentity) &&
    !string.Equals(cell.MotifIdentity, neighbor.MotifIdentity, StringComparison.Ordinal) &&
    ((cell.Ground == WorldGround.Water && neighbor.Feature == WorldFeature.Stone) ||
     (neighbor.Ground == WorldGround.Water && cell.Feature == WorldFeature.Stone) ||
     (IsClearing(cell) && neighbor.Feature == WorldFeature.Vegetation) ||
     (IsClearing(neighbor) && cell.Feature == WorldFeature.Vegetation));

static bool HasNamedMotifSpanningAtLeast45Cells(IReadOnlyList<WorldCell> cells) =>
    HasConnectedNamedFormSpanning(cells, _ => true, minimumSpan: 45);

static bool HasConnectedNamedFormSpanning(
    IReadOnlyList<WorldCell> cells,
    Func<WorldCell, bool> include,
    long minimumSpan)
{
    var candidates = cells
        .Where(cell => include(cell) && !string.IsNullOrWhiteSpace(cell.MotifIdentity))
        .ToDictionary(cell => cell.Address);
    var visited = new HashSet<WorldAddress>();

    foreach (var start in candidates.Values)
    {
        if (!visited.Add(start.Address))
        {
            continue;
        }

        var motif = start.MotifIdentity;
        var queue = new Queue<WorldCell>();
        queue.Enqueue(start);
        var minX = start.Address.X;
        var maxX = start.Address.X;
        var minY = start.Address.Y;
        var maxY = start.Address.Y;

        while (queue.TryDequeue(out var cell))
        {
            minX = Math.Min(minX, cell.Address.X);
            maxX = Math.Max(maxX, cell.Address.X);
            minY = Math.Min(minY, cell.Address.Y);
            maxY = Math.Max(maxY, cell.Address.Y);
            if (maxX - minX >= minimumSpan || maxY - minY >= minimumSpan)
            {
                return true;
            }

            EnqueueConnected(candidates, visited, queue, motif, cell.Address with { Y = cell.Address.Y - 1 });
            EnqueueConnected(candidates, visited, queue, motif, cell.Address with { X = cell.Address.X + 1 });
            EnqueueConnected(candidates, visited, queue, motif, cell.Address with { Y = cell.Address.Y + 1 });
            EnqueueConnected(candidates, visited, queue, motif, cell.Address with { X = cell.Address.X - 1 });
        }
    }

    return false;
}

static void EnqueueConnected(
    IReadOnlyDictionary<WorldAddress, WorldCell> candidates,
    ISet<WorldAddress> visited,
    Queue<WorldCell> queue,
    string? motif,
    WorldAddress neighbor)
{
    if (candidates.TryGetValue(neighbor, out var next) &&
        string.Equals(next.MotifIdentity, motif, StringComparison.Ordinal) &&
        visited.Add(neighbor))
    {
        queue.Enqueue(next);
    }
}

static bool IsClearing(WorldCell cell) =>
    cell.Feature is null && cell.Ground is WorldGround.Grass or WorldGround.Soil;

static (WorldGround Ground, WorldFeature? Feature) LegacySurfaceSemantics(SurfaceTerrain terrain) => terrain switch
{
    SurfaceTerrain.Grass => (WorldGround.Grass, null),
    SurfaceTerrain.Forest => (WorldGround.Grass, WorldFeature.Vegetation),
    SurfaceTerrain.Stone => (WorldGround.Soil, WorldFeature.Stone),
    SurfaceTerrain.Water => (WorldGround.Water, null),
    _ => throw new ArgumentOutOfRangeException(nameof(terrain)),
};

static (WorldGround Ground, WorldFeature? Feature, string? DurableIdentity) LegacySkySemantics(SkyTerrain terrain) => terrain switch
{
    SkyTerrain.OpenSky => (WorldGround.OpenSky, null, null),
    SkyTerrain.Cloud => (WorldGround.OpenSky, WorldFeature.Cloud, null),
    SkyTerrain.Landmark => (WorldGround.OpenSky, WorldFeature.Landmark, SkyStratum.LandmarkName),
    _ => throw new ArgumentOutOfRangeException(nameof(terrain)),
};

static WorldCardinalAdjacency LegacySurfaceAdjacency(SurfacePatch patch, SurfaceTile cell) =>
    new(
        North: LegacySurfaceFormMatches(patch, cell, 0, -1),
        East: LegacySurfaceFormMatches(patch, cell, 1, 0),
        South: LegacySurfaceFormMatches(patch, cell, 0, 1),
        West: LegacySurfaceFormMatches(patch, cell, -1, 0));

static WorldCardinalAdjacency LegacySkyAdjacency(SkyStratum sky, SkyTile cell) =>
    new(
        North: LegacySkyFormMatches(sky, cell, 0, -1),
        East: LegacySkyFormMatches(sky, cell, 1, 0),
        South: LegacySkyFormMatches(sky, cell, 0, 1),
        West: LegacySkyFormMatches(sky, cell, -1, 0));

static bool LegacySurfaceFormMatches(SurfacePatch patch, SurfaceTile cell, int deltaX, int deltaY) =>
    LegacySurfaceSemantics(cell.Terrain) == LegacySurfaceSemantics(
        patch.Tiles.Single(tile => tile.Address == new WorldAddress(
            SurfacePatch.SurfaceStratum,
            cell.Address.X + deltaX,
            cell.Address.Y + deltaY)).Terrain);

static bool LegacySkyFormMatches(SkyStratum sky, SkyTile cell, int deltaX, int deltaY) =>
    LegacySkySemantics(cell.Terrain) == LegacySkySemantics(
        sky.TileAt(new WorldAddress(
            SkyStratum.StratumName,
            cell.Address.X + deltaX,
            cell.Address.Y + deltaY)).Terrain);

static void VerifySaveLoad()
{
    var state = new ChronicleState(
        41_337,
        17,
        new WorldAddress("surface", 4, -3),
        ChronicleSpeed.Fast,
        OpeningIntent.Up)
    {
        Codex = new CodexState(HasFly: true, HasStone: false),
        Loadout = IntrinsicFlyLoadout(),
        LooseStoneAddress = ChronicleState.InitialLooseStoneAddress,
    };
    var restored = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(state));

    Assert(restored == state, "JSON save/load must restore the complete Chronicle state by value.");
    var originalPatch = SurfacePatch.Generate(state);
    var restoredPatch = SurfacePatch.Generate(restored);
    Assert(restoredPatch.Center == originalPatch.Center, "Surface patch regeneration after load must preserve its center.");
    Assert(restoredPatch.Tiles.SequenceEqual(originalPatch.Tiles), "Surface patch regeneration after load must preserve its tiles.");
}

static void VerifySkySaveLoad()
{
    var state = ChronicleState.Begin(41_337) with
    {
        Tick = 29,
        Speed = ChronicleSpeed.Slow,
        Intent = OpeningIntent.Up,
        Codex = new CodexState(HasFly: true, HasStone: false),
        Address = SkyStratum.LandmarkAddress,
    };
    var restored = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(state));
    var originalSky = SkyStratum.Generate(state);
    var restoredSky = SkyStratum.Generate(restored);

    Assert(restored == state, "Save/load in the sky must restore complete Chronicle state.");
    Assert(restoredSky.Center == originalSky.Center, "Sky regeneration after load must preserve its center.");
    Assert(restoredSky.Tiles.SequenceEqual(originalSky.Tiles), "Sky regeneration after load must preserve every tile.");
}

static void VerifyStudyReplay()
{
    var first = ReplayStudy(41_337);
    var second = ReplayStudy(41_337);

    Assert(first == second, "The same Study command/tick stream must replay to the same complete state.");
    Assert(first.Study.StoneUnderstanding == 7, "The deterministic Study replay must retain its expected partial understanding.");
    Assert(first.Study.IsStudyingBell, "The deterministic Study replay must remain active at the Bell.");
    Assert(!first.Codex.HasStone, "Partial Study replay must not learn Stone early.");
}

static void VerifyStudyRequiresBell()
{
    var simulation = BeginWithUp();
    var before = simulation.State;

    simulation.Apply(new StudySkyStone());

    Assert(simulation.State == before, "Study away from the Bell must leave state unchanged.");
}

static void VerifyStudyPause()
{
    var simulation = AtBellWithFly();
    simulation.Apply(new StudySkyStone());
    simulation.AdvanceOneTick();
    var beforePause = simulation.State;

    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Paused));
    var paused = simulation.State;
    simulation.AdvanceOneTick();
    simulation.AdvanceClockPulse();

    Assert(simulation.State.Tick == paused.Tick, "Pause must prevent Chronicle tick advancement during Study.");
    Assert(
        simulation.State.Study.StoneUnderstanding == beforePause.Study.StoneUnderstanding,
        "Pause must prevent Study understanding advancement.");
    Assert(simulation.State.Study.IsStudyingBell, "Pause must not silently cancel an active Study.");
}

static void VerifyStudyStopsWhenLeavingBell()
{
    var simulation = AtBellWithFly();
    simulation.Apply(new StudySkyStone());
    AdvanceTicks(simulation, 3);
    var understanding = simulation.State.Study.StoneUnderstanding;

    simulation.Apply(new MoveIncarnation(1, 0));
    Assert(!simulation.State.Study.IsStudyingBell, "Leaving the Bell must stop active Study.");
    Assert(
        simulation.State.Study.StoneUnderstanding == understanding,
        "Leaving the Bell must retain accumulated understanding.");

    simulation.AdvanceOneTick();
    Assert(
        simulation.State.Study.StoneUnderstanding == understanding,
        "Ticks after leaving the Bell must not advance Study understanding.");
}

static void VerifyStudySaveLoad()
{
    var simulation = AtBellWithFly();
    simulation.Apply(new StudySkyStone());
    AdvanceTicks(simulation, 7);
    var state = simulation.State;

    var restored = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(state));

    Assert(restored == state, "Save/load must restore exact partial Study state and Codex state.");
    Assert(restored.Study.IsStudyingBell, "Save/load must preserve an active Study at the Bell.");
    Assert(restored.Study.StoneUnderstanding == 7, "Save/load must preserve partial Study understanding exactly.");
}

static void VerifyStudyCompletionIsIdempotent()
{
    var simulation = AtBellWithFly();
    simulation.Apply(new StudySkyStone());
    AdvanceTicks(simulation, StudyState.StoneUnderstandingRequired);

    Assert(simulation.State.Codex.HasStone, "Sixteen Chronicle ticks of Study must learn Stone.");
    Assert(
        simulation.State.Study.StoneUnderstanding == StudyState.StoneUnderstandingRequired,
        "Completed Study must retain its full understanding threshold.");
    Assert(!simulation.State.Study.IsStudyingBell, "Completing Study must stop its active state.");

    var completed = simulation.State;
    simulation.Apply(new StudySkyStone());
    Assert(simulation.State == completed, "Repeating completed Study must leave state unchanged.");

    simulation.AdvanceOneTick();
    Assert(simulation.State.Codex.HasStone, "Further ticks after completion must not remove Stone from the Codex.");
    Assert(
        simulation.State.Study.StoneUnderstanding == StudyState.StoneUnderstandingRequired,
        "Further ticks after completion must not regress understanding.");
}

static void VerifyCodexAndStudySerialization()
{
    var simulation = AtBellWithFly();
    simulation.Apply(new StudySkyStone());
    AdvanceTicks(simulation, 5);
    var json = ChronicleSaveCodec.Serialize(simulation.State);
    var restored = ChronicleSaveCodec.Deserialize(json);

    Assert(json.Contains("\"Codex\"", StringComparison.Ordinal), "Saved JSON must include Codex state.");
    Assert(json.Contains("\"Study\"", StringComparison.Ordinal), "Saved JSON must include Study state.");
    Assert(!json.Contains("Tiles", StringComparison.Ordinal), "Saved JSON must not serialize generated tiles.");
    Assert(restored.Codex == simulation.State.Codex, "Save/load must preserve each Codex word exactly.");
    Assert(restored.Study == simulation.State.Study, "Save/load must preserve Study progress and activity exactly.");
}

static void VerifySlice2ASaveMigratesLoadoutAndStone()
{
    const string slice2AJson =
        """
        {
          "Seed": 41337,
          "Tick": 16,
          "Address": {
            "Stratum": "surface",
            "X": 0,
            "Y": 0
          },
          "Speed": 2,
          "Intent": 1,
          "Codex": {
            "HasFly": true,
            "HasStone": true
          },
          "Study": {
            "StoneUnderstanding": 16,
            "IsStudyingBell": false
          }
        }
        """;

    var restored = ChronicleSaveCodec.Deserialize(slice2AJson);

    Assert(restored.ActiveLoadout.Slots.Count == LoadoutState.SlotCount, "A Slice 2A save must gain eight Loadout slots.");
    Assert(
        restored.ActiveLoadout[0] == new LoadoutSlot(ChronicleVerb.Fly),
        "A Slice 2A save with Fly must migrate intrinsic Fly into slot one.");
    Assert(
        restored.ActiveLoadout.Slots.Skip(1).All(slot => slot.IsEmpty),
        "Migration must not invent entries for the other seven slots.");
    Assert(
        restored.LooseStoneAddress == ChronicleState.InitialLooseStoneAddress,
        "A Slice 2A save must gain the loose Stone at its fixed initial address.");
}

static void VerifyLoadoutHasEightSerializableSlots()
{
    var state = LearnedAtSurface();
    var json = ChronicleSaveCodec.Serialize(state);
    var restored = ChronicleSaveCodec.Deserialize(json);

    Assert(state.ActiveLoadout.Slots.Count == 8, "A Loadout must always expose exactly eight ordered slots.");
    Assert(restored.ActiveLoadout == state.ActiveLoadout, "Save/load must preserve all eight Loadout slots by value.");
    Assert(json.Contains("\"Loadout\"", StringComparison.Ordinal), "Saved JSON must contain the serialized Loadout.");
    Assert(json.Contains("\"LooseStoneAddress\"", StringComparison.Ordinal), "Saved JSON must contain the loose Stone delta.");
    Assert(!json.Contains("Tiles", StringComparison.Ordinal), "Loadout saves must not contain generated tiles.");
}

static void VerifyOnlyCodexLanguageCanBeEquipped()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    var beforeFly = simulation.State;
    var flyResult = simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly));

    Assert(!flyResult.Applied && simulation.State == beforeFly, "Unknown Fly must not be equipped.");
    Assert(flyResult.Message.Contains("Codex", StringComparison.Ordinal), "Unknown language must return a legible Core rejection.");

    simulation.Apply(new ChooseUpIntent());
    var beforeStone = simulation.State;
    var stoneResult = simulation.Apply(
        new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));

    Assert(!stoneResult.Applied && simulation.State == beforeStone, "Unknown Stone must not be fitted into Fly.");
    Assert(stoneResult.Message.Contains("Stone", StringComparison.Ordinal), "Unknown Stone must report why fitting failed.");

    var invalidResult = simulation.Apply(
        new ConfigureLoadoutSlot(0, (ChronicleVerb)999));
    Assert(!invalidResult.Applied && simulation.State == beforeStone, "An invalid Verb value must leave state unchanged.");
}

static void VerifyVerbCannotOccupyTwoSlots()
{
    var simulation = BeginWithUp();
    var before = simulation.State;
    var result = simulation.Apply(new ConfigureLoadoutSlot(1, ChronicleVerb.Fly));

    Assert(!result.Applied, "A learned Verb already in one slot must not be duplicated.");
    Assert(simulation.State == before, "Rejected duplicate configuration must leave every slot unchanged.");
    Assert(
        simulation.State.ActiveLoadout.Slots.Count(slot => slot.Verb == ChronicleVerb.Fly) == 1,
        "Fly must occupy at most one Loadout slot.");
}

static void VerifyUnequippedFlyIsUnavailable()
{
    var simulation = BeginWithUp();
    var clear = simulation.Apply(new ClearLoadoutSlot(0));
    var address = simulation.State.Address;

    Assert(clear.Applied, "Clearing occupied slot one must change the Loadout.");
    Assert(simulation.State.ActiveLoadout[0].IsEmpty, "Cleared slot one must be inspectably empty.");
    Assert(!simulation.State.CanFly && simulation.FlyDestination is null, "Unequipped Fly must disable self-flight.");

    var use = simulation.Apply(new UseLoadoutSlot(0));
    Assert(!use.Applied && simulation.State.Address == address, "Using an empty slot must not move the Incarnation.");
}

static void VerifyIntrinsicFlyUsesLoadoutSlot()
{
    var simulation = BeginWithUp();
    var beforeInvalidTarget = simulation.State;
    var invalidTarget = simulation.Apply(
        new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    Assert(
        !invalidTarget.Applied && simulation.State == beforeInvalidTarget,
        "Intrinsic Fly must reject a target instead of silently applying the wrong subject.");

    var upward = simulation.Apply(new UseLoadoutSlot(0));
    Assert(upward.Applied, "Using intrinsic Fly from its Loadout slot must succeed.");
    Assert(
        simulation.State.Address == new WorldAddress(SkyStratum.StratumName, 0, 0),
        "Intrinsic Fly must preserve coordinates when entering the sky.");

    var downward = simulation.Apply(new UseLoadoutSlot(0));
    Assert(downward.Applied, "The same intrinsic Fly slot must return from the sky.");
    Assert(
        simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        "Intrinsic Fly must preserve coordinates when returning to the surface.");
}

static void VerifyFlyStoneMovesOnlyTheLooseStone()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    var configured = simulation.Apply(
        new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    var incarnationAddress = simulation.State.Address;
    var target = simulation.State.LooseStoneAddress;

    Assert(configured.Applied, "Known Stone must fit into known Fly.");
    Assert(
        simulation.ValidTargetsForSlot(0).SequenceEqual(new[] { ChronicleState.InitialLooseStoneAddress }),
        "Core must expose the adjacent loose Stone as the only valid fitted target.");

    var use = simulation.Apply(new UseLoadoutSlot(0, target));

    Assert(use.Applied, "Fly[Stone] must move its adjacent loose Stone target.");
    Assert(simulation.State.Address == incarnationAddress, "Fly[Stone] must not move the Incarnation.");
    Assert(
        simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
        "Fly[Stone] must preserve coordinates while moving Stone into the sky.");
    Assert(!simulation.State.CanFly, "Fitting Stone into Fly must replace intrinsic self-flight.");
}

static void VerifyFlyStoneReturnsTheLooseStone()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly));
    simulation.Apply(new UseLoadoutSlot(0));
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));

    var incarnationAddress = simulation.State.Address;
    var result = simulation.Apply(
        new UseLoadoutSlot(0, new WorldAddress(SkyStratum.StratumName, 1, 0)));

    Assert(result.Applied, "Fly[Stone] must act on the same loose Stone in the sky.");
    Assert(simulation.State.Address == incarnationAddress, "Returning Stone must not move the Incarnation.");
    Assert(
        simulation.State.LooseStoneAddress == ChronicleState.InitialLooseStoneAddress,
        "The same Expression must return Stone to its matching surface address.");
}

static void VerifyFlyStoneRejectsInvalidTargets()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));

    foreach (var target in new WorldAddress?[]
    {
        null,
        new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        new WorldAddress(SurfacePatch.SurfaceStratum, 2, 0),
        new WorldAddress(SkyStratum.StratumName, 1, 0),
    })
    {
        var before = simulation.State;
        var result = simulation.Apply(new UseLoadoutSlot(0, target));
        Assert(!result.Applied, "Missing or incorrectly typed fitted targets must be rejected.");
        Assert(simulation.State == before, "An invalid fitted target must leave all Chronicle state unchanged.");
        Assert(!string.IsNullOrWhiteSpace(result.Message), "Invalid fitted targets must return a legible Core rejection.");
    }

    var emptyBefore = simulation.State;
    var empty = simulation.Apply(new UseLoadoutSlot(1));
    Assert(!empty.Applied && simulation.State == emptyBefore, "Using an empty Loadout slot must leave state unchanged.");

    var distantState = simulation.State with
    {
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, -1, 0),
    };
    var distant = new ChronicleSimulation(distantState);
    var distantBefore = distant.State;
    var distantResult = distant.Apply(
        new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    Assert(!distantResult.Applied && distant.State == distantBefore, "A distant loose Stone must not be a valid target.");
}

static void VerifyLoadoutReplayAndSaveLoad()
{
    var first = ReplayFlyStone();
    var second = ReplayFlyStone();

    Assert(first == second, "The same Loadout configuration and target stream must replay to the same state.");
    var restored = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(first));
    Assert(restored == first, "Save/load must restore the Loadout and moved loose Stone exactly.");
    Assert(
        restored.LooseStoneAddress == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0),
        "Replay must finish with the loose Stone returned to the surface.");
}

static void VerifyDeathRequiresLivingIncarnationAtBell()
{
    var awayFromBell = new ChronicleSimulation(LearnedAtSurface());
    var awayState = awayFromBell.State;
    var rejectedAway = awayFromBell.Apply(new EndIncarnationAtBell());

    Assert(!rejectedAway.Applied, "Bell death must be rejected away from the Bell.");
    Assert(awayFromBell.State == awayState, "Rejected death away from the Bell must leave state unchanged.");

    var atBell = AtBellWithFly();
    var ended = atBell.Apply(new EndIncarnationAtBell());

    Assert(ended.Applied, "A living Incarnation at the Bell must be able to end deliberately.");
    Assert(
        atBell.State.IncarnationLife == IncarnationLifeState.AwaitingReplacement,
        "Bell death must enter the explicit awaiting-replacement state.");

    var secondAtBell = AtBellWithFly();
    secondAtBell.Apply(new EndIncarnationAtBell());
    Assert(
        secondAtBell.State == atBell.State,
        "The same Bell death command stream must produce the same awaiting-replacement state.");

    var awaitingState = atBell.State;
    var repeated = atBell.Apply(new EndIncarnationAtBell());
    Assert(!repeated.Applied, "An ended Incarnation must not die again.");
    Assert(atBell.State == awaitingState, "Repeated death while awaiting replacement must leave state unchanged.");
}

static void VerifyAwaitingReplacementFreezesChronicle()
{
    var simulation = EndedChronicle();
    var awaiting = simulation.State;

    Assert(
        awaiting.IncarnationLife == IncarnationLifeState.AwaitingReplacement,
        "The lifecycle fixture must begin while awaiting replacement.");
    Assert(simulation.FlyDestination is null, "An ended body must not expose an intrinsic Fly destination.");
    Assert(
        simulation.ValidTargetsForSlot(0).Count == 0,
        "An ended body must not expose fitted Expression targets.");

    ChronicleCommand[] rejectedCommands =
    [
        new MoveIncarnation(0, 1),
        new StudySkyStone(),
        new ConfigureLoadoutSlot(0, ChronicleVerb.Fly),
        new ClearLoadoutSlot(0),
        new UseLoadoutSlot(0, awaiting.LooseStoneAddress),
        new EndIncarnationAtBell(),
        new SetChronicleSpeed(ChronicleSpeed.Paused),
        new ChooseUpIntent(),
    ];

    foreach (var command in rejectedCommands)
    {
        var result = simulation.Apply(command);
        Assert(!result.Applied, $"{command.GetType().Name} must be unavailable while awaiting replacement.");
        Assert(simulation.State == awaiting, "Only replacement may change an awaiting Chronicle.");
    }

    simulation.AdvanceOneTick();
    Assert(simulation.State == awaiting, "A direct tick must not advance while awaiting replacement.");
    simulation.AdvanceClockPulse();
    Assert(simulation.State == awaiting, "A clock pulse must not advance while awaiting replacement.");
}

static void VerifyReplacementPreservesChronicleAndResetsBody()
{
    var alive = new ChronicleSimulation(LearnedAtSurface());
    var rejected = alive.Apply(new CreateReplacementIncarnation());
    Assert(!rejected.Applied, "Replacement must be rejected while an Incarnation is alive.");

    var simulation = EndedChronicle();
    var awaiting = simulation.State;
    var replacement = simulation.Apply(new CreateReplacementIncarnation());

    Assert(replacement.Applied, "The replacement command must leave awaiting-replacement state.");
    Assert(simulation.State.IncarnationLife == IncarnationLifeState.Alive, "Replacement must create a living body.");
    Assert(
        simulation.State.IncarnationId == awaiting.IncarnationId + 1,
        "Replacement identity must increment deterministically.");
    Assert(
        simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        "Replacement must begin at the fixed surface origin.");
    Assert(
        simulation.State.ActiveLoadout.Slots.Count == LoadoutState.SlotCount &&
        simulation.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty),
        "Replacement must begin with exactly eight empty Loadout slots.");
    Assert(!simulation.State.CanFly, "The dead body's active Fly must not leak into the replacement.");

    Assert(simulation.State.Seed == awaiting.Seed, "Replacement must preserve the Chronicle seed.");
    Assert(simulation.State.Tick == awaiting.Tick, "Replacement must preserve the frozen Chronicle tick.");
    Assert(simulation.State.Speed == awaiting.Speed, "Replacement must restore play at the selected speed.");
    Assert(simulation.State.Intent == awaiting.Intent, "Replacement must preserve opening Intent.");
    Assert(simulation.State.Codex == awaiting.Codex, "Replacement must preserve the complete Codex.");
    Assert(simulation.State.Study == awaiting.Study, "Replacement must preserve complete Understanding.");
    Assert(
        simulation.State.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
        "Replacement must preserve the loose Stone's changed World Address.");

    var repeated = simulation.Apply(new CreateReplacementIncarnation());
    Assert(!repeated.Applied, "A living replacement must not be replaced again.");
}

static void VerifyLifecycleSaveEnvelopeAndMigration()
{
    const string slice2BJson =
        """
        {
          "Seed": 41337,
          "Tick": 16,
          "Address": {
            "Stratum": "surface",
            "X": 0,
            "Y": 0
          },
          "Speed": 2,
          "Intent": 1,
          "Codex": {
            "HasFly": true,
            "HasStone": true
          },
          "Study": {
            "StoneUnderstanding": 16,
            "IsStudyingBell": false
          },
          "Loadout": {
            "Slot1": {
              "Verb": 1,
              "Noun": 1
            }
          },
          "LooseStoneAddress": {
            "Stratum": "sky",
            "X": 1,
            "Y": 0
          }
        }
        """;

    var migrated = ChronicleSaveCodec.Deserialize(slice2BJson);
    Assert(migrated.IncarnationId == 1, "A literal Slice 2B save must gain first-Incarnation identity.");
    Assert(
        migrated.IncarnationLife == IncarnationLifeState.Alive,
        "A literal Slice 2B save must gain a living first Incarnation.");
    Assert(migrated.ActiveLoadout[0].IsFlyStone, "Slice 2B migration must preserve its fitted Loadout.");
    Assert(
        migrated.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
        "Slice 2B migration must preserve its moved Stone.");

    var awaiting = EndedChronicle().State;
    var awaitingJson = ChronicleSaveCodec.Serialize(awaiting);
    var restoredAwaiting = ChronicleSaveCodec.Deserialize(awaitingJson);
    Assert(awaitingJson.Contains("\"Version\": 1", StringComparison.Ordinal), "Current saves must use version 1.");
    Assert(awaitingJson.Contains("\"Chronicle\"", StringComparison.Ordinal), "Current saves must wrap Chronicle state.");
    Assert(restoredAwaiting == awaiting, "Save/load before replacement must preserve the awaiting Chronicle exactly.");

    var simulation = new ChronicleSimulation(restoredAwaiting);
    simulation.Apply(new CreateReplacementIncarnation());
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly));
    var replacement = simulation.State;
    var replacementJson = ChronicleSaveCodec.Serialize(replacement);
    var restoredReplacement = ChronicleSaveCodec.Deserialize(replacementJson);
    Assert(restoredReplacement == replacement, "Save/load after replacement must preserve the new body exactly.");

    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize("""{"Version":999,"Chronicle":{}}"""),
        "Unknown save-envelope versions must be rejected explicitly.");
}

static void VerifyLifecycleReplay()
{
    var first = ReplayLifecycle();
    var second = ReplayLifecycle();

    Assert(first == second, "The same Study, Expression, death, and replacement stream must replay identically.");
    Assert(first.IncarnationId == 2, "Lifecycle replay must finish with the deterministic replacement identity.");
    Assert(first.IncarnationLife == IncarnationLifeState.Alive, "Lifecycle replay must finish with a living replacement.");
    Assert(first.Codex.HasFly && first.Codex.HasStone, "Lifecycle replay must preserve learned language.");
    Assert(
        first.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
        "Lifecycle replay must preserve the material Stone change.");
}

static ChronicleState LearnedAtSurface() => ChronicleState.Begin(41_337) with
{
    Intent = OpeningIntent.Up,
    Codex = new CodexState(HasFly: true, HasStone: true),
    Study = new StudyState(
        StoneUnderstanding: StudyState.StoneUnderstandingRequired,
        IsStudyingBell: false),
    Loadout = new LoadoutState(
        Slot1: new LoadoutSlot(ChronicleVerb.Fly)),
    LooseStoneAddress = ChronicleState.InitialLooseStoneAddress,
};

static LoadoutState IntrinsicFlyLoadout() => new(
    Slot1: new LoadoutSlot(ChronicleVerb.Fly));

static ChronicleState ReplayFlyStone()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly));
    simulation.Apply(new UseLoadoutSlot(0));
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    simulation.Apply(new UseLoadoutSlot(0, new WorldAddress(SkyStratum.StratumName, 1, 0)));
    return simulation.State;
}

static ChronicleSimulation EndedChronicle()
{
    var state = LearnedAtSurface() with
    {
        Tick = 23,
        Speed = ChronicleSpeed.Fast,
    };
    var simulation = new ChronicleSimulation(state);
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly));
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    var death = simulation.Apply(new EndIncarnationAtBell());
    Assert(death.Applied, "Lifecycle fixture must end the first Incarnation at the Bell.");
    return simulation;
}

static ChronicleState ReplayLifecycle()
{
    var simulation = AtBellWithFly();
    simulation.Apply(new StudySkyStone());
    AdvanceTicks(simulation, StudyState.StoneUnderstandingRequired);

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, 1));
    }

    simulation.Apply(new UseLoadoutSlot(0));
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly));
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly, ChronicleNoun.Stone));
    simulation.Apply(new EndIncarnationAtBell());
    simulation.Apply(new CreateReplacementIncarnation());
    simulation.Apply(new ConfigureLoadoutSlot(0, ChronicleVerb.Fly));
    simulation.Apply(new UseLoadoutSlot(0));
    return simulation.State;
}

static ChronicleSimulation BeginWithUp()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseUpIntent());
    return simulation;
}

static ChronicleSimulation AtBellWithFly()
{
    var simulation = BeginWithUp();
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    Assert(simulation.State.Address == SkyStratum.LandmarkAddress, "Study fixture must arrive at the Bell.");
    return simulation;
}

static ChronicleState ReplayStudy(long seed)
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(seed));
    simulation.Apply(new ChooseUpIntent());
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    simulation.Apply(new StudySkyStone());
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Fast));
    simulation.AdvanceClockPulse();
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Paused));
    simulation.AdvanceClockPulse();
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    simulation.AdvanceClockPulse();
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Normal));
    simulation.AdvanceClockPulse();
    return simulation.State;
}

static void AdvanceTicks(ChronicleSimulation simulation, int count)
{
    for (var tick = 0; tick < count; tick++)
    {
        simulation.AdvanceOneTick();
    }
}

static ChronicleState ReplayInterleaved(long seed)
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(seed));
    simulation.Apply(new ChooseUpIntent());
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Fast));
    simulation.AdvanceClockPulse();
    simulation.Apply(new UseLoadoutSlot(0));
    simulation.Apply(new MoveIncarnation(0, -1));
    simulation.AdvanceClockPulse();
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    simulation.Apply(new MoveIncarnation(0, -1));
    simulation.AdvanceClockPulse();
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Paused));
    simulation.AdvanceClockPulse();
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Normal));
    simulation.AdvanceClockPulse();
    return simulation.State;
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
