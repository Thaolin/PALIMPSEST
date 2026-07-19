using Chronicle.Core;

VerifyLegacySaveCompatibility();
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
    "PASS: Slice 0 through Slice 2C deterministic generation, Codex, Study, Loadout, Expressions, Incarnation replacement, replay, and save compatibility verified.");

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
