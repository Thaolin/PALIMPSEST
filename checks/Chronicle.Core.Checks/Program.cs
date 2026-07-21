using Chronicle.Core;

VerifyAuthoredWordCatalogue();
VerifyUnderstandingRespectsCatalogueThresholds();
VerifyCurrentSaveShapeIsStrict();
VerifyLanguageSnapshotsAreImmutable();
VerifyGeneratedBellStudySource();
VerifyDeliberateStudyChoiceAdvancesOnlySelectedWord();
VerifySelectedUnderstandingSaveLoad();
VerifyStudySourceReflectsWordSpecificState();
VerifyStudySourceVersionMigrationAndLegacyCompatibility();
VerifyStudyChoiceRejections();
VerifyPartialStudySurvivesDeathReplacementAndReturn();
VerifyIndependentBellStudyCompletion();
VerifyLegacySaveCompatibility();
VerifyWorldGrammarVersionMigrationAndPinning();
VerifyGrammar3CairnIsDeterministicAndOpeningIndependent();
VerifyEnteringRivenCairnPausesAndExposesConflictContext();
VerifyPausedCairnCommandsAndRejectionsStayDeliberate();
VerifyPreparedSmashResolvesOnFirstDeliveredTickAtAllSpeeds();
VerifyUnpreparedCairnTickEndsOnlyOneBodyAndReplacementRetainsSmash();
VerifyFirstConflictSaveV5AndLiteralVersion3Migration();
VerifySaveBoundaryRejectsFutureAndMalformedState();
VerifyCounterTransitionsRejectBeforeUnsavableState();
VerifyCairnRejectsFoundAndFlyStoneCannotOverwriteIt();
VerifySlice1SaveMigratesCodex();
VerifySerializedIntent();
VerifyMovementRequiresIntent();
VerifyUpIntent();
VerifyHereBuildOpeningGrantsFound();
VerifyAgainstOpeningGrantsSmash();
VerifyFoundRejectsUnsupportedUseWithoutMutation();
VerifyHomeSiteEligibility();
VerifyFoundEstablishesSingularHome();
VerifyHomeReturnRouteGuidesPhysicalSteps();
VerifyHomeHearthstoneOverlaysExistingRidge();
VerifyHomeSaveV5AndVersion2Migration();
VerifyNonHereChronicleCanRetainEquipAndFoundHome();
VerifyHomeSurvivesReplacementAndFoundRemainsReequipable();
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
VerifyLoadoutUsesCatalogueWordIdentities();
VerifyLoadoutIdentityPersistenceAndPredecessorCollision();
VerifyOnlyCodexLanguageCanBeEquipped();
VerifyVerbCannotOccupyTwoSlots();
VerifyUnequippedFlyIsUnavailable();
VerifyIntrinsicFlyUsesLoadoutSlot();
VerifyFlyStoneMovesOnlyTheLooseStone();
VerifyFlyStoneReturnsTheLooseStone();
VerifyFlyStoneCannotOverlapHome();
VerifyFlyStoneRejectsInvalidTargets();
VerifyFlyBellMovesDurableSituation();
VerifyFlyBellSaveV5AndLiteralV4Migration();
VerifyLoadoutReplayAndSaveLoad();
VerifyDeathRequiresLivingIncarnationAtBell();
VerifyAwaitingReplacementFreezesChronicle();
VerifyReplacementPreservesChronicleAndResetsBody();
VerifyLifecycleSaveEnvelopeAndMigration();
VerifyLifecycleReplay();

Console.WriteLine(
    "SLICE5 CORE ACCEPTANCE PASS expression=Fly[Bell] durable=Bell+source save=5 migration=4");
Console.WriteLine(
    "PASS: Slice 5 composition proof plus retained Goal 4C and earlier Core regressions verified.");

static void VerifyAuthoredWordCatalogue()
{
    var words = WordCatalogue.Words;

    Assert(
        words.Select(word => word.Id).SequenceEqual(
        [
            WordIds.Fly,
            WordIds.Found,
            WordIds.Smash,
            WordIds.Stone,
            WordIds.Bell,
        ]),
        "The authored Word Catalogue must expose stable Fly, Found, Smash, Stone, and Bell identities in canonical order.");
    Assert(
        words.Select(word => word.Id).Distinct().Count() == 5,
        "The authored Word Catalogue must not contain duplicate Word identities.");

    var fly = WordCatalogue.Get(WordIds.Fly);
    var found = WordCatalogue.Get(WordIds.Found);
    var smash = WordCatalogue.Get(WordIds.Smash);
    var stone = WordCatalogue.Get(WordIds.Stone);
    var bell = WordCatalogue.Get(WordIds.Bell);

    Assert(
        fly.Kind == WordKind.Verb &&
        fly.DisplayName == "Fly" &&
        fly.CompatibleNouns.SequenceEqual([WordIds.Stone, WordIds.Bell]),
        "Fly must be an authored Verb compatible with Stone and Bell.");
    Assert(
        found.Kind == WordKind.Verb &&
        found.DisplayName == "Found" &&
        found.UnderstandingRequired == 0 &&
        found.CompatibleNouns.Count == 0,
        "Found must be an authored intrinsic Verb without a 4B Noun.");
    Assert(
        smash.Kind == WordKind.Verb &&
        smash.DisplayName == "Smash" &&
        smash.UnderstandingRequired == 0 &&
        smash.CompatibleNouns.Count == 0,
        "Smash must be an authored intrinsic Verb without a 4C Noun.");
    Assert(
        stone.Kind == WordKind.Noun &&
        stone.DisplayName == "Stone" &&
        stone.CompatibleNouns.Count == 0,
        "Stone must be an authored Noun.");
    Assert(
        bell.Kind == WordKind.Noun &&
        bell.DisplayName == "Bell" &&
        bell.CompatibleNouns.Count == 0,
        "Bell must be an authored Noun used as a fitted subject, not a Verb.");
    Assert(
        !string.IsNullOrWhiteSpace(fly.Meaning) &&
        !string.IsNullOrWhiteSpace(found.Meaning) &&
        !string.IsNullOrWhiteSpace(smash.Meaning) &&
        !string.IsNullOrWhiteSpace(stone.Meaning) &&
        !string.IsNullOrWhiteSpace(bell.Meaning),
        "Every catalogue Word must have an authored meaning.");
    AssertThrows<KeyNotFoundException>(
        () => WordCatalogue.Get(new WordId("word.unknown")),
        "An unknown Word identity must not resolve through the authored catalogue.");
}

static void VerifyUnderstandingRespectsCatalogueThresholds()
{
    var save = System.Text.Json.Nodes.JsonNode.Parse(
        ChronicleSaveCodec.Serialize(ChronicleState.Begin(41_337)))!;
    save["Chronicle"]!["Study"]!["Understanding"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """[{"Word":"word.fly","Amount":1}]""");

    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(save.ToJsonString()),
        "Current saves must reject Understanding above the authored Word's threshold.");
}

static void VerifyCurrentSaveShapeIsStrict()
{
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize("""{"Version":2,"Chronicle":{}}"""),
        "A version 2 envelope must reject a Chronicle with missing current fields.");

    var currentJson = ChronicleSaveCodec.Serialize(ChronicleState.Begin(41_337));
    var missingVersion = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!.AsObject();
    missingVersion.Remove("Version");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(missingVersion.ToJsonString()),
        "A current envelope missing Version must not fall through predecessor migration.");

    var legacyCodex = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    legacyCodex["Chronicle"]!["Codex"]!["HasFly"] = true;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(legacyCodex.ToJsonString()),
        "Version 5 must reject mixed predecessor Boolean Codex fields.");

    var duplicateCodex = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    duplicateCodex["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """["word.fly","word.fly"]""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(duplicateCodex.ToJsonString()),
        "Version 5 must reject duplicate Codex Words before canonicalization.");

    var outOfOrderCodex = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    outOfOrderCodex["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """["word.found","word.fly"]""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(outOfOrderCodex.ToJsonString()),
        "Version 5 must reject out-of-order Codex Words before canonicalization.");

    var legacyStudy = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    legacyStudy["Chronicle"]!["Study"]!["StoneUnderstanding"] = 7;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(legacyStudy.ToJsonString()),
        "Version 5 must reject mixed predecessor Bell-specific Study fields.");

    var duplicateUnderstanding = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    duplicateUnderstanding["Chronicle"]!["Study"]!["Understanding"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """[{"Word":"word.stone","Amount":1},{"Word":"word.stone","Amount":2}]""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(duplicateUnderstanding.ToJsonString()),
        "Version 5 must reject duplicate Understanding Words before canonicalization.");

    var outOfOrderUnderstanding = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    outOfOrderUnderstanding["Chronicle"]!["Study"]!["Understanding"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """[{"Word":"word.bell","Amount":1},{"Word":"word.stone","Amount":1}]""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(outOfOrderUnderstanding.ToJsonString()),
        "Version 5 must reject out-of-order Understanding Words before canonicalization.");

    var zeroUnderstanding = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    zeroUnderstanding["Chronicle"]!["Study"]!["Understanding"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """[{"Word":"word.stone","Amount":0}]""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(zeroUnderstanding.ToJsonString()),
        "Version 5 must reject zero Understanding entries before canonicalization removes them.");

    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["ReturnRoute"] = new System.Text.Json.Nodes.JsonObject(),
        "Version 5 must reject unexpected envelope properties.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["ReturnRoute"] = new System.Text.Json.Nodes.JsonObject(),
        "Version 5 must reject transient ReturnRoute data on the Chronicle.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["Address"]!["Generated"] = true,
        "Version 5 must reject unexpected Chronicle Address properties.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["LooseStoneAddress"]!["Generated"] = true,
        "Version 5 must reject unexpected loose-Stone Address properties.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["BellAddress"]!["Generated"] = true,
        "Version 5 must reject unexpected Bell Address properties.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["Codex"]!["HasStone"] = false,
        "Version 5 must reject mixed legacy Codex properties.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["Study"]!["IsStudyingBell"] = false,
        "Version 5 must reject mixed legacy Study properties.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["Study"]!["Understanding"] =
            System.Text.Json.Nodes.JsonNode.Parse(
                """[{"Word":"word.stone","Amount":1,"LegacyAmount":1}]"""),
        "Version 5 must reject unexpected Understanding-entry properties.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["Loadout"]!["CurrentSite"] = new System.Text.Json.Nodes.JsonObject(),
        "Version 5 must reject transient CurrentSite data on the Loadout.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        currentJson,
        root => root["Chronicle"]!["Loadout"]!["Slot1"]!["LegacyVerb"] = 1,
        "Version 5 must reject unexpected Loadout-slot properties.");

    var foundedJson = ChronicleSaveCodec.Serialize(FoundedHereHome());
    AssertCurrentSaveRejectsUnexpectedProperty(
        foundedJson,
        root => root["Chronicle"]!["Home"]!["CurrentSite"] = new System.Text.Json.Nodes.JsonObject(),
        "Version 5 must reject transient CurrentSite data on Home.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        foundedJson,
        root => root["Chronicle"]!["Home"]!["Address"]!["ReturnRoute"] =
            new System.Text.Json.Nodes.JsonObject(),
        "Version 5 must reject unexpected Home Address properties.");

    var hereWithoutFound = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    hereWithoutFound["Chronicle"]!["Intent"] = (int)OpeningIntent.Here;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(hereWithoutFound.ToJsonString()),
        "A current HERE Chronicle missing Found must reject instead of being repaired.");

    var upWithoutFly = System.Text.Json.Nodes.JsonNode.Parse(currentJson)!;
    upWithoutFly["Chronicle"]!["Intent"] = (int)OpeningIntent.Up;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(upWithoutFly.ToJsonString()),
        "A current UP Chronicle missing Fly must reject instead of being repaired.");

    var homeWithoutFound = System.Text.Json.Nodes.JsonNode.Parse(foundedJson)!;
    homeWithoutFound["Chronicle"]!["Codex"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Words":[]}""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(homeWithoutFound.ToJsonString()),
        "A current Home-bearing HERE Chronicle missing Found must reject instead of being repaired.");

    var nonHereHomeWithoutFound = System.Text.Json.Nodes.JsonNode.Parse(foundedJson)!;
    nonHereHomeWithoutFound["Chronicle"]!["Intent"] = (int)OpeningIntent.Up;
    nonHereHomeWithoutFound["Chronicle"]!["Codex"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Words":["word.fly"]}""");
    nonHereHomeWithoutFound["Chronicle"]!["Loadout"]!["Slot1"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.fly","Noun":null}""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(nonHereHomeWithoutFound.ToJsonString()),
        "A current non-HERE Home must still reject when Found is absent from the Codex.");
}

static void VerifyLanguageSnapshotsAreImmutable()
{
    var simulation = AtBellWithFly();
    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The immutability fixture must expose the Bell source.");
    simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Stone));
    AdvanceTicks(simulation, 1);
    var state = simulation.State;

    var codexWords = (IList<WordId>)state.Codex.Words;
    var understanding = (IList<WordUnderstanding>)state.Study.Understanding;
    AssertThrows<NotSupportedException>(
        () => codexWords[0] = WordIds.Bell,
        "Public Codex membership must not expose its mutable backing array.");
    AssertThrows<NotSupportedException>(
        () => understanding[0] = new WordUnderstanding(WordIds.Bell, 1),
        "Public Understanding must not expose its mutable backing array.");
    Assert(
        simulation.State == state &&
        simulation.State.Codex.Contains(WordIds.Fly) &&
        simulation.State.Study.UnderstandingFor(WordIds.Stone) == 1,
        "Failed external mutation attempts must leave Chronicle state and equality unchanged.");
}

static void VerifyGeneratedBellStudySource()
{
    var simulation = AtBellWithFly();
    var before = simulation.State;
    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The generated Bell cell must expose its Study Source.");

    Assert(
        simulation.State.WorldGrammarVersion == 3,
        "World Grammar version 3 must retain the generated two-offer Bell Study Source unchanged.");
    Assert(source.Id == StudySourceIds.BellSkyStone, "The Bell source must have one stable identity.");
    Assert(source.Name == "Sky-Stone Clapper", "The Bell source must name the encountered situation.");
    Assert(source.Address == SkyStratum.LandmarkAddress, "The Bell source must retain its generated World Address.");
    Assert(
        source.Rarity == StudySourceRarity.Rare &&
        source.Danger == StudySourceDanger.Lethal &&
        source.Significance == StudySourceSignificance.Landmark,
        "The Bell source must expose its confirmed Rare, Lethal, and Landmark qualities.");
    Assert(
        source.Offers.Select(offer => offer.Word.Id).SequenceEqual([WordIds.Stone, WordIds.Bell]),
        "The Bell source must offer Stone first and Bell second.");
    Assert(
        source.Offers.All(offer =>
            offer.UnderstandingYield == 16 &&
            !string.IsNullOrWhiteSpace(offer.Rationale)),
        "Each Bell offer must explain its contextual relevance and contribute sixteen Understanding.");
    Assert(
        source.Offers[0].Rationale.Contains("dark clapper", StringComparison.Ordinal) &&
        source.Offers[1].Rationale.Contains("gold vessel", StringComparison.Ordinal),
        "Stone and Bell must have distinct authored contextual reasons.");
    Assert(simulation.State == before, "Querying a generated Study Source must not mutate Chronicle state.");

    var repeated = simulation.CurrentStudySource;
    Assert(
        repeated is not null &&
        repeated.Id == source.Id &&
        repeated.Offers.Select(offer => offer.Word.Id).SequenceEqual(
            source.Offers.Select(offer => offer.Word.Id)),
        "Equivalent Study Source queries must reproduce the same ordered offer.");

    simulation.Apply(new MoveIncarnation(0, 1));
    Assert(simulation.CurrentStudySource is null, "An address away from the Bell must expose no Study Source.");
}

static void VerifyDeliberateStudyChoiceAdvancesOnlySelectedWord()
{
    var simulation = AtBellWithFly();
    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The Study choice fixture must expose the Bell source.");

    var chooseStone = simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Stone));
    Assert(chooseStone.Applied, "Choosing the offered Stone word must begin a Core-owned pursuit.");
    Assert(
        simulation.State.Study.ActiveSourceId == source.Id &&
        simulation.State.Study.ActiveWord == WordIds.Stone,
        "Study must retain the exact selected source and Word identities.");

    AdvanceTicks(simulation, 6);
    Assert(
        simulation.State.Study.UnderstandingFor(WordIds.Stone) == 6 &&
        simulation.State.Study.UnderstandingFor(WordIds.Bell) == 0,
        "Six Study ticks must advance only the deliberately selected Stone word.");
    Assert(
        !simulation.State.Codex.Contains(WordIds.Stone) &&
        !simulation.State.Codex.Contains(WordIds.Bell),
        "Partial Understanding must not add either offered word to the Codex.");

    var chooseBell = simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Bell));
    Assert(chooseBell.Applied, "Choosing the other offered word must change the active pursuit.");
    AdvanceTicks(simulation, 3);
    Assert(
        simulation.State.Study.UnderstandingFor(WordIds.Stone) == 6 &&
        simulation.State.Study.UnderstandingFor(WordIds.Bell) == 3,
        "Switching pursuit must preserve Stone progress and advance only Bell.");
    Assert(
        simulation.State.Study.ActiveWord == WordIds.Bell,
        "The active pursuit must identify Bell after the deliberate switch.");
}

static void VerifySelectedUnderstandingSaveLoad()
{
    var simulation = AtBellWithFly();
    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The persistence fixture must expose the Bell source.");
    simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Bell));
    AdvanceTicks(simulation, 6);

    var expected = simulation.State;
    var json = ChronicleSaveCodec.Serialize(expected);
    var restored = ChronicleSaveCodec.Deserialize(json);

    Assert(json.Contains("\"Version\": 5", StringComparison.Ordinal), "Current saves must use envelope version 5.");
    Assert(
        json.Contains("\"word.bell\"", StringComparison.Ordinal) &&
        json.Contains("\"ActiveSourceId\"", StringComparison.Ordinal),
        "The save must inspectably retain Bell-specific Understanding and the active pursuit.");
    Assert(
        !json.Contains("\"Offers\"", StringComparison.Ordinal) &&
        !json.Contains("\"Rationale\"", StringComparison.Ordinal) &&
        !json.Contains("\"Situation\"", StringComparison.Ordinal),
        "Generated Study Source snapshots and catalogue presentation must stay out of Chronicle saves.");
    Assert(restored == expected, "Save/load must restore the exact selected Bell pursuit and partial Understanding.");
    Assert(
        restored.Study.UnderstandingFor(WordIds.Bell) == 6 &&
        restored.Study.ActiveSourceId == source.Id &&
        restored.Study.ActiveWord == WordIds.Bell,
        "Save/load must preserve Bell=6 and the exact active source/Word identities.");
}

static void VerifyStudySourceReflectsWordSpecificState()
{
    var simulation = AtBellWithFly();
    var initial = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The source-state fixture must expose the Bell source.");
    simulation.Apply(new ChooseStudyWord(initial.Id, WordIds.Stone));
    AdvanceTicks(simulation, 6);

    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("Active Study must retain the generated source.");
    var stone = source.Offers.Single(offer => offer.Word.Id == WordIds.Stone);
    var bell = source.Offers.Single(offer => offer.Word.Id == WordIds.Bell);

    Assert(
        stone.CurrentUnderstanding == 6 &&
        stone.UnderstandingRequired == 16 &&
        stone.IsSelected &&
        !stone.IsLearned,
        "The Stone offer must expose its exact selected partial progress.");
    Assert(
        bell.CurrentUnderstanding == 0 &&
        bell.UnderstandingRequired == 16 &&
        !bell.IsSelected &&
        !bell.IsLearned,
        "The unselected Bell offer must remain visibly separate at zero progress.");
}

static void VerifyStudySourceVersionMigrationAndLegacyCompatibility()
{
    const string version1Json =
        """
        {
          "Version": 1,
          "Chronicle": {
            "Seed": 41337,
            "Tick": 7,
            "Address": { "Stratum": "sky", "X": 0, "Y": -4 },
            "Speed": 2,
            "Intent": 1,
            "Codex": { "HasFly": true, "HasStone": false },
            "Study": { "StoneUnderstanding": 7, "IsStudyingBell": true },
            "Loadout": {
              "Slot1": { "Verb": 1, "Noun": null },
              "Slot2": { "Verb": null, "Noun": null },
              "Slot3": { "Verb": null, "Noun": null },
              "Slot4": { "Verb": null, "Noun": null },
              "Slot5": { "Verb": null, "Noun": null },
              "Slot6": { "Verb": null, "Noun": null },
              "Slot7": { "Verb": null, "Noun": null },
              "Slot8": { "Verb": null, "Noun": null }
            },
            "LooseStoneAddress": { "Stratum": "surface", "X": 1, "Y": 0 },
            "IncarnationId": 1,
            "IncarnationLife": 0,
            "WorldGrammarVersion": 1
          }
        }
        """;

    var migrated = ChronicleSaveCodec.Deserialize(version1Json);
    Assert(
        migrated.WorldGrammarVersion == 2 &&
        migrated.Home is null,
        "A version 1 Chronicle must migrate to World Grammar 2 without inventing Home.");
    Assert(
        migrated.Study.UnderstandingFor(WordIds.Stone) == 7 &&
        migrated.Study.ActiveSourceId == StudySourceIds.BellSkyStone &&
        migrated.Study.ActiveWord == WordIds.Stone,
        "Version 1 active Stone Study must migrate exactly to the generated source/Word pursuit.");
    Assert(
        new ChronicleSimulation(migrated).CurrentStudySource?.Offers.Count == 2,
        "The migrated version 2 Chronicle must expose the confirmed two-offer Bell source.");

    foreach (var invalidIntent in new[]
    {
        (Value: (int)OpeningIntent.Here, Name: "HERE"),
        (Value: 99, Name: "undefined"),
    })
    {
        var invalidVersion1 = System.Text.Json.Nodes.JsonNode.Parse(version1Json)!;
        invalidVersion1["Chronicle"]!["Intent"] = invalidIntent.Value;
        AssertThrows<InvalidOperationException>(
            () => ChronicleSaveCodec.Deserialize(invalidVersion1.ToJsonString()),
            $"A literal version-1 save must reject {invalidIntent.Name} Intent before migration.");
    }

    var futureVersion1 = System.Text.Json.Nodes.JsonNode.Parse(version1Json)!;
    futureVersion1["Chronicle"]!["WorldGrammarVersion"] = 3;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(futureVersion1.ToJsonString()),
        "A literal version-1 save must not acquire the later grammar-3 Cairn rule.");

    var version1Physical = migrated with { WorldGrammarVersion = 1 };
    foreach (var stratum in new[] { SurfacePatch.SurfaceStratum, SkyStratum.StratumName })
    {
        var bounds = new WorldRectangle(-16, -16, 32, 32);
        Assert(
            WorldArea.Generate(version1Physical, stratum, bounds).Cells.SequenceEqual(
                WorldArea.Generate(migrated, stratum, bounds).Cells),
            $"Migrating World Grammar 1 to 2 must not change physical {stratum} semantics.");
    }

    const string legacyJson =
        """
        {
          "Seed": 41337,
          "Tick": 7,
          "Address": { "Stratum": "sky", "X": 0, "Y": -4 },
          "Speed": 2,
          "Intent": 1,
          "Codex": { "HasFly": true, "HasStone": false },
          "Study": { "StoneUnderstanding": 7, "IsStudyingBell": true }
        }
        """;
    var legacy = ChronicleSaveCodec.Deserialize(legacyJson);
    var legacySource = new ChronicleSimulation(legacy).CurrentStudySource;
    Assert(
        legacy.WorldGrammarVersion == 0 &&
        legacy.Home is null,
        "A pre-versioned Chronicle must retain World Grammar 0 without inventing Home.");
    Assert(
        legacySource is not null &&
        legacySource.Offers.Select(offer => offer.Word.Id).SequenceEqual([WordIds.Stone]) &&
        legacy.Study.ActiveWord == WordIds.Stone &&
        legacy.Study.UnderstandingFor(WordIds.Stone) == 7,
        "A legacy Chronicle must retain its single Stone offer and exact unfinished Study.");

    foreach (var invalidIntent in new[]
    {
        (Value: (int)OpeningIntent.Here, Name: "HERE"),
        (Value: 99, Name: "undefined"),
    })
    {
        var invalidPreEnvelope = System.Text.Json.Nodes.JsonNode.Parse(legacyJson)!;
        invalidPreEnvelope["Intent"] = invalidIntent.Value;
        AssertThrows<InvalidOperationException>(
            () => ChronicleSaveCodec.Deserialize(invalidPreEnvelope.ToJsonString()),
            $"A literal pre-envelope save must reject {invalidIntent.Name} Intent before migration.");
    }

    var futurePreEnvelope = System.Text.Json.Nodes.JsonNode.Parse(legacyJson)!;
    futurePreEnvelope["WorldGrammarVersion"] = 3;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(futurePreEnvelope.ToJsonString()),
        "A literal pre-envelope save must not acquire the later grammar-3 Cairn rule.");
}

static void VerifyStudyChoiceRejections()
{
    var simulation = AtBellWithFly();
    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The rejection fixture must expose the Bell source.");

    foreach (var rejectedCommand in new ChooseStudyWord[]
    {
        new(new StudySourceId("study-source.stale"), WordIds.Stone),
        new(source.Id, WordIds.Fly),
        new(source.Id, new WordId("word.unknown")),
    })
    {
        var before = simulation.State;
        var result = simulation.Apply(rejectedCommand);
        Assert(!result.Applied, "A stale, unoffered, or unknown Study choice must be rejected.");
        Assert(simulation.State == before, "A rejected Study choice must not mutate Chronicle state.");
    }

    Assert(
        simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Stone)).Applied,
        "The rejection fixture must allow its first valid Stone selection.");
    var active = simulation.State;
    var repeated = simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Stone));
    Assert(!repeated.Applied, "Repeating the already active pursuit must be rejected.");
    Assert(simulation.State == active, "A repeated active pursuit must not hide a state mutation.");

    simulation.Apply(new MoveIncarnation(1, 0));
    var away = simulation.State;
    var absent = simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Bell));
    Assert(!absent.Applied, "A choice must be rejected after leaving its Study Source.");
    Assert(simulation.State == away, "An absent-source rejection must leave state unchanged.");

    var completed = AtBellWithFly();
    var completedSource = completed.CurrentStudySource!;
    completed.Apply(new ChooseStudyWord(completedSource.Id, WordIds.Stone));
    AdvanceTicks(completed, StudyState.UnderstandingRequired);
    var learned = completed.State;
    var learnedChoice = completed.Apply(
        new ChooseStudyWord(completedSource.Id, WordIds.Stone));
    Assert(!learnedChoice.Applied, "A learned word must not begin another pursuit.");
    Assert(completed.State == learned, "Rejecting a learned word must not mutate Chronicle state.");
}

static void VerifyPartialStudySurvivesDeathReplacementAndReturn()
{
    var simulation = AtBellWithFly();
    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The lifecycle Study fixture must expose the Bell source.");
    simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Stone));
    AdvanceTicks(simulation, 6);

    simulation.Apply(new EndIncarnationAtBell());
    Assert(
        simulation.State.IncarnationLife == IncarnationLifeState.AwaitingReplacement &&
        simulation.State.Study.ActiveWord is null &&
        simulation.State.Study.UnderstandingFor(WordIds.Stone) == 6,
        "Death at the source must clear only active Study and retain exact Stone Understanding.");

    var codexBeforeReplacement = simulation.State.Codex;
    simulation.Apply(new CreateReplacementIncarnation());
    Assert(
        simulation.State.Codex == codexBeforeReplacement &&
        simulation.State.Study.UnderstandingFor(WordIds.Stone) == 6 &&
        simulation.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty),
        "A replacement must inherit Codex and Understanding with eight empty Loadout slots.");

    Assert(
        simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly)).Applied,
        "The replacement must deliberately re-equip its inherited Fly.");
    Assert(
        simulation.Apply(new UseLoadoutSlot(0)).Applied,
        "The replacement must use re-equipped Fly to return to the sky.");
    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    var returnedSource = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("Returning to the Bell must regenerate its Study Source.");
    Assert(
        simulation.State.Study.ActiveWord is null &&
        simulation.Apply(new ChooseStudyWord(returnedSource.Id, WordIds.Stone)).Applied,
        "Returning after replacement must require and accept another deliberate Stone selection.");
    AdvanceTicks(simulation, StudyState.UnderstandingRequired - 6);

    Assert(
        simulation.State.Codex.Contains(WordIds.Stone) &&
        simulation.State.Study.UnderstandingFor(WordIds.Stone) == StudyState.UnderstandingRequired &&
        simulation.State.Study.ActiveWord is null,
        "Resumed Stone Study must cap at sixteen, learn Stone exactly once, and clear activity.");
}

static void VerifyIndependentBellStudyCompletion()
{
    var first = AtBellWithFly();
    var source = first.CurrentStudySource
        ?? throw new InvalidOperationException("The alternate Bell fixture must expose the Bell source.");
    Assert(
        first.Apply(new ChooseStudyWord(source.Id, WordIds.Bell)).Applied,
        "The alternate branch must choose Bell through the same Core command.");
    AdvanceTicks(first, 5);

    var restoredState = ChronicleSaveCodec.Deserialize(
        ChronicleSaveCodec.Serialize(first.State));
    var restored = new ChronicleSimulation(restoredState);
    Assert(
        restored.State.Study.ActiveWord == WordIds.Bell &&
        restored.State.Study.UnderstandingFor(WordIds.Bell) == 5,
        "The independent Bell branch must restore its exact active partial pursuit.");
    AdvanceTicks(restored, StudyState.UnderstandingRequired - 5);

    Assert(
        restored.State.Codex.Contains(WordIds.Bell) &&
        !restored.State.Codex.Contains(WordIds.Stone) &&
        restored.State.Study.UnderstandingFor(WordIds.Bell) == StudyState.UnderstandingRequired &&
        restored.State.Study.UnderstandingFor(WordIds.Stone) == 0 &&
        restored.State.Study.ActiveWord is null,
        "Completing Bell must learn only Bell exactly at sixteen and leave Stone untouched.");
    var completed = restored.State;
    var repeat = restored.Apply(new ChooseStudyWord(source.Id, WordIds.Bell));
    Assert(!repeat.Applied && restored.State == completed, "Completed Bell Study must be idempotent.");
    Assert(
        restored.CurrentStudySource?.Offers.Single(offer => offer.Word.Id == WordIds.Bell).IsLearned == true,
        "The regenerated Bell offer must visibly report its learned status.");
}

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
    Assert(restored.Home is null, "A Slice 0 save must not gain Home during migration.");
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
        newChronicle.WorldGrammarVersion == 3,
        "A newly created Chronicle must pin World Grammar version 3.");

    var json = ChronicleSaveCodec.Serialize(newChronicle);
    var restored = ChronicleSaveCodec.Deserialize(json);
    Assert(
        json.Contains("\"WorldGrammarVersion\"", StringComparison.Ordinal),
        "A new Chronicle save must include its pinned World Grammar version.");
    Assert(
        restored.WorldGrammarVersion == 3,
        "Save/load must retain a new Chronicle's pinned World Grammar version.");
}

static void VerifyGrammar3CairnIsDeterministicAndOpeningIndependent()
{
    var baseState = ChronicleState.Begin(41_337);
    var bounds = new WorldRectangle(-96, -96, 193, 193);
    var area = WorldArea.Generate(baseState, SurfacePatch.SurfaceStratum, bounds);
    var cairns = area.Cells
        .Where(cell => cell.DurableIdentity == FirstConflictSubjects.RivenCairnIdentity)
        .ToArray();

    Assert(
        baseState.WorldGrammarVersion == 3 &&
        baseState.FirstConflict is null,
        "New Chronicles must pin World Grammar 3 without manufacturing a conflict delta.");
    Assert(
        cairns.Length == 1 &&
        cairns[0].Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3) &&
        cairns[0].Ground == WorldGround.Soil &&
        cairns[0].Feature == WorldFeature.Stone &&
        cairns[0].MotifIdentity == "surface-ridge-main",
        "Seed 41337 must place one intact Riven Cairn at the authored dry Stone ridge-spur fixture.");
    Assert(
        cairns[0].Address != baseState.Address &&
        cairns[0].Address != ChronicleState.InitialLooseStoneAddress &&
        cairns[0].Address != new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3),
        "The generated Cairn must not overlap the origin, loose Stone, or accepted Home fixture.");

    var v2 = baseState with { WorldGrammarVersion = 2 };
    var v2Area = WorldArea.Generate(v2, SurfacePatch.SurfaceStratum, bounds);
    Assert(
        area.Cells.Zip(v2Area.Cells).All(pair =>
            pair.First.Address == pair.Second.Address &&
            pair.First.Ground == pair.Second.Ground &&
            pair.First.Feature == pair.Second.Feature &&
            pair.First.MotifIdentity == pair.Second.MotifIdentity),
        "Grammar 3 must delegate every version-2 surface ground, feature, and motif unchanged.");

    var skyBounds = new WorldRectangle(-8, -8, 17, 17);
    var v3Sky = WorldArea.Generate(baseState, SkyStratum.StratumName, skyBounds);
    var v2Sky = WorldArea.Generate(v2, SkyStratum.StratumName, skyBounds);
    var v3Bell = new ChronicleSimulation(
        baseState with { Address = SkyStratum.LandmarkAddress }).CurrentStudySource;
    var v2Bell = new ChronicleSimulation(
        v2 with { Address = SkyStratum.LandmarkAddress }).CurrentStudySource;
    Assert(
        v3Sky.Cells.SequenceEqual(v2Sky.Cells) &&
        v3Bell is not null &&
        v2Bell is not null &&
        v3Bell.Id == v2Bell.Id &&
        v3Bell.Offers.Select(offer => offer.Word.Id).SequenceEqual(
            v2Bell.Offers.Select(offer => offer.Word.Id)) &&
        v3Bell.Offers.Select(offer => offer.UnderstandingYield).SequenceEqual(
            v2Bell.Offers.Select(offer => offer.UnderstandingYield)),
        "Grammar 3 must delegate every version-2 Sky cell and the Bell Study Source unchanged.");
    Assert(
        area.Cells.SequenceEqual(
            WorldArea.Generate(baseState, SurfacePatch.SurfaceStratum, bounds).Cells),
        "Grammar-3 Cairn generation must be replay- and query-order-neutral.");

    var openings = new ChronicleSimulation[]
    {
        new(ChronicleState.Begin(41_337)),
        new(ChronicleState.Begin(41_337)),
        new(ChronicleState.Begin(41_337)),
    };
    openings[0].Apply(new ChooseAgainstIntent());
    openings[1].Apply(new ChooseUpIntent());
    openings[2].Apply(new ChooseHereIntent());

    Assert(
        openings.All(simulation =>
        {
            var cell = WorldArea.Generate(
                simulation.State,
                SurfacePatch.SurfaceStratum,
                new WorldRectangle(1, 3, 1, 1)).Cells[0];
            return simulation.State.WorldGrammarVersion == 3 &&
                   cell.DurableIdentity == FirstConflictSubjects.RivenCairnIdentity;
        }),
        "AGAINST, UP, and HERE must expose the same generated intact Cairn rather than private opening content.");
}

static void VerifyEnteringRivenCairnPausesAndExposesConflictContext()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseAgainstIntent());
    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));

    var context = simulation.ConflictContext
        ?? throw new InvalidOperationException("Entering the intact Cairn must expose Core conflict context.");
    var paused = simulation.State;

    Assert(
        paused.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3) &&
        paused.Speed == ChronicleSpeed.Paused &&
        paused.FirstConflict == new FirstConflictState(
            FirstConflictSubjects.RiverWardSubjectId,
            paused.Address,
            ThreatenedTick: 0),
        "Entering the unresolved Riven Cairn must record its threat and pause before any fixed tick advances.");
    Assert(
        context.CairnIdentity == FirstConflictSubjects.RivenCairnIdentity &&
        context.SubjectIdentity == FirstConflictSubjects.RiverWardIdentity &&
        context.History == FirstConflictSubjects.History &&
        context.Warning == FirstConflictSubjects.Warning &&
        !context.IsSmashPrepared &&
        context.IsThreatened &&
        !context.IsResolved,
        "Conflict Context must name the Riven Cairn and River-Ward, explain its history and next-tick warning, and expose preparation.");

    simulation.AdvanceClockPulse();
    Assert(
        simulation.State == paused,
        "A paused Chronicle must freeze the unresolved ward, conflict state, and fixed Chronicle tick.");

    var allOpenings = new[]
    {
        (ChronicleCommand)new ChooseAgainstIntent(),
        new ChooseUpIntent(),
        new ChooseHereIntent(),
    };
    var contexts = allOpenings.Select(opening =>
    {
        var branch = new ChronicleSimulation(ChronicleState.Begin(41_337));
        branch.Apply(opening);
        branch.Apply(new MoveIncarnation(1, 0));
        branch.Apply(new MoveIncarnation(0, 1));
        branch.Apply(new MoveIncarnation(0, 1));
        branch.Apply(new MoveIncarnation(0, 1));
        return branch.ConflictContext
            ?? throw new InvalidOperationException("Every grammar-3 opening must enter the shared Cairn conflict.");
    }).ToArray();
    Assert(
        contexts.All(candidate =>
            candidate.CairnIdentity == FirstConflictSubjects.RivenCairnIdentity &&
            candidate.SubjectIdentity == FirstConflictSubjects.RiverWardIdentity &&
            candidate.History == FirstConflictSubjects.History &&
            candidate.Warning == FirstConflictSubjects.Warning &&
            candidate.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3)),
        "AGAINST, UP, and HERE must expose the same Cairn identity, history, and danger; only their granted language differs.");
}

static void VerifyPausedCairnCommandsAndRejectionsStayDeliberate()
{
    var simulation = AgainstAtRivenCairn();
    var paused = simulation.State;

    var remote = simulation.Apply(
        new UseLoadoutSlot(0, new WorldAddress(SurfacePatch.SurfaceStratum, 1, 2)));
    var absent = simulation.Apply(new UseLoadoutSlot(7));
    Assert(
        !remote.Applied &&
        !absent.Applied &&
        simulation.State == paused,
        "Remote or absent actions must reject without mutating the paused ward exchange.");

    var cleared = simulation.Apply(new ClearLoadoutSlot(0));
    var equipped = simulation.Apply(new ConfigureLoadoutSlot(1, WordIds.Smash));
    Assert(
        cleared.Applied &&
        equipped.Applied &&
        simulation.State.Speed == ChronicleSpeed.Paused &&
        simulation.State.Tick == paused.Tick,
        "Paused Cairn time must allow deliberate Loadout configuration without autonomous change.");

    var prepared = simulation.Apply(new UseLoadoutSlot(1));
    var preparedState = simulation.State;
    var repeated = simulation.Apply(new UseLoadoutSlot(1));
    Assert(
        prepared.Applied &&
        !repeated.Applied &&
        simulation.State == preparedState,
        "Smash must queue once and repeated preparation must reject without changing the pending exchange.");

    var retreat = simulation.Apply(new MoveIncarnation(0, -1));
    Assert(
        retreat.Applied &&
        simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 2) &&
        simulation.State.Speed == ChronicleSpeed.Paused &&
        simulation.State.FirstConflict is null &&
        simulation.ConflictContext is null,
        "Deliberate retreat while paused must clear an unresolved pending exchange without advancing time.");
    Assert(
        WorldArea.Generate(
            simulation.State,
            SurfacePatch.SurfaceStratum,
            new WorldRectangle(1, 3, 1, 1)).Cells.Single().DurableIdentity ==
            FirstConflictSubjects.RivenCairnIdentity,
        "Leaving an unresolved Cairn must leave its generated River-Ward intact.");
}

static void VerifyPreparedSmashResolvesOnFirstDeliveredTickAtAllSpeeds()
{
    foreach (var speed in new[]
    {
        ChronicleSpeed.Slow,
        ChronicleSpeed.Normal,
        ChronicleSpeed.Fast,
    })
    {
        var simulation = AgainstAtRivenCairn();
        var threatened = simulation.State;
        var prepared = simulation.Apply(new UseLoadoutSlot(0));
        var pending = simulation.State;

        Assert(
            prepared.Applied &&
            pending.Tick == threatened.Tick &&
            pending.FirstConflict?.PendingAction == new LoadoutSlot(WordIds.Smash) &&
            WorldArea.Generate(
                pending,
                SurfacePatch.SurfaceStratum,
                new WorldRectangle(1, 3, 1, 1)).Cells.Single().DurableIdentity ==
                FirstConflictSubjects.RivenCairnIdentity,
            "Prepared intrinsic Smash must record its exact Loadout action without resolving material before a tick.");

        simulation.Apply(new SetChronicleSpeed(speed));
        simulation.AdvanceClockPulse();
        var resolved = simulation.State;
        var context = simulation.ConflictContext
            ?? throw new InvalidOperationException("The Shattered Cairn must retain its read-only conflict provenance.");
        var expectedPulseTicks = speed switch
        {
            ChronicleSpeed.Slow => 1,
            ChronicleSpeed.Normal => 2,
            ChronicleSpeed.Fast => 4,
            _ => throw new InvalidOperationException("The fixture requires an active Chronicle speed."),
        };

        Assert(
            resolved.HasLivingIncarnation &&
            resolved.Address == threatened.Address &&
            resolved.Tick == threatened.Tick + expectedPulseTicks &&
            resolved.FirstConflict == new FirstConflictState(
                FirstConflictSubjects.RiverWardSubjectId,
                threatened.Address,
                threatened.Tick,
                PendingAction: null,
                Outcome: FirstConflictOutcome.Shattered,
                ResolvedTick: threatened.Tick + 1,
                ResolvingIncarnationId: threatened.IncarnationId),
            "At every active speed, the first delivered tick must shatter the ward and later pulse ticks remain ordinary Chronicle ticks.");
        Assert(
            context.CairnIdentity == FirstConflictSubjects.ShatteredCairnIdentity &&
            context.IsResolved &&
            !context.IsThreatened &&
            !context.IsSmashPrepared &&
            WorldArea.Generate(
                resolved,
                SurfacePatch.SurfaceStratum,
                new WorldRectangle(1, 3, 1, 1)).Cells.Single().DurableIdentity ==
                FirstConflictSubjects.ShatteredCairnIdentity,
            "Successful Smash must leave The Shattered Cairn as the durable non-Agent material outcome.");
    }
}

static void VerifyUnpreparedCairnTickEndsOnlyOneBodyAndReplacementRetainsSmash()
{
    foreach (var speed in new[]
    {
        ChronicleSpeed.Slow,
        ChronicleSpeed.Normal,
        ChronicleSpeed.Fast,
    })
    {
        var simulation = AgainstAtRivenCairn();
        var threatened = simulation.State;
        simulation.Apply(new SetChronicleSpeed(speed));
        simulation.AdvanceClockPulse();
        var ended = simulation.State;

        Assert(
            ended.Tick == threatened.Tick + 1 &&
            ended.IncarnationLife == IncarnationLifeState.AwaitingReplacement &&
            ended.FirstConflict is null &&
            WorldArea.Generate(
                ended,
                SurfacePatch.SurfaceStratum,
                new WorldRectangle(1, 3, 1, 1)).Cells.Single().DurableIdentity ==
                FirstConflictSubjects.RivenCairnIdentity,
            "No prepared action must end the body on exactly the first delivered tick and leave the River-Ward unresolved.");

        var replacement = simulation.Apply(new CreateReplacementIncarnation());
        Assert(
            replacement.Applied &&
            simulation.State.HasLivingIncarnation &&
            simulation.State.Codex.Contains(WordIds.Smash) &&
            simulation.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty),
            "A replacement after Cairn failure must retain Smash in the Codex with an empty Loadout.");
    }
}

static void VerifyFirstConflictSaveV5AndLiteralVersion3Migration()
{
    var threatenedSimulation = AgainstAtRivenCairn();
    var threatened = threatenedSimulation.State;
    var threatenedJson = ChronicleSaveCodec.Serialize(threatened);
    var threatenedRestored = ChronicleSaveCodec.Deserialize(threatenedJson);

    Assert(
        threatenedJson.Contains("\"Version\": 5", StringComparison.Ordinal) &&
        threatenedJson.Contains("\"FirstConflict\"", StringComparison.Ordinal) &&
        threatenedRestored == threatened,
        "Strict save envelope v5 must round-trip the threatened River-Ward state exactly.");

    threatenedSimulation.Apply(new UseLoadoutSlot(0));
    var pending = threatenedSimulation.State;
    var pendingJson = ChronicleSaveCodec.Serialize(pending);
    var pendingRestored = ChronicleSaveCodec.Deserialize(pendingJson);
    Assert(
        pendingRestored == pending &&
        pendingRestored.FirstConflict?.PendingAction == new LoadoutSlot(WordIds.Smash),
        "Strict save envelope v5 must retain the exact pending Smash Loadout action.");

    var forgedPendingWithoutSmash = System.Text.Json.Nodes.JsonNode.Parse(pendingJson)!;
    forgedPendingWithoutSmash["Chronicle"]!["Intent"] = (int)OpeningIntent.Here;
    forgedPendingWithoutSmash["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse("""["word.found"]""");
    forgedPendingWithoutSmash["Chronicle"]!["Loadout"]!["Slot1"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.found","Noun":null}""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(forgedPendingWithoutSmash.ToJsonString()),
        "A forged pending Smash action must reject when Smash is absent from the durable Codex.");

    var resolving = new ChronicleSimulation(pendingRestored);
    resolving.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    var resumedBeforeTick = resolving.State;
    var resumedJson = ChronicleSaveCodec.Serialize(resumedBeforeTick);
    var resumedRestored = ChronicleSaveCodec.Deserialize(resumedJson);
    Assert(
        resumedRestored == resumedBeforeTick &&
        resumedRestored.Speed == ChronicleSpeed.Slow &&
        resumedRestored.FirstConflict is { Outcome: null, PendingAction: { IsIntrinsicSmash: true } },
        "The reachable resumed-before-next-tick conflict state must round-trip without losing the pending action.");
    resolving = new ChronicleSimulation(resumedRestored);
    resolving.AdvanceClockPulse();
    var shattered = resolving.State;
    var shatteredJson = ChronicleSaveCodec.Serialize(shattered);
    var shatteredRestored = ChronicleSaveCodec.Deserialize(shatteredJson);
    Assert(
        shatteredRestored == shattered &&
        shatteredRestored.FirstConflict?.Outcome == FirstConflictOutcome.Shattered &&
        WorldArea.Generate(
            shatteredRestored,
            SurfacePatch.SurfaceStratum,
            new WorldRectangle(1, 3, 1, 1)).Cells.Single().DurableIdentity ==
            FirstConflictSubjects.ShatteredCairnIdentity,
        "Strict save envelope v5 must retain the resolved Shattered Cairn outcome and provenance.");

    var forgedShatteredWithoutSmash = System.Text.Json.Nodes.JsonNode.Parse(shatteredJson)!;
    forgedShatteredWithoutSmash["Chronicle"]!["Intent"] = (int)OpeningIntent.Here;
    forgedShatteredWithoutSmash["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse("""["word.found"]""");
    forgedShatteredWithoutSmash["Chronicle"]!["Loadout"]!["Slot1"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.found","Noun":null}""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(forgedShatteredWithoutSmash.ToJsonString()),
        "A forged Shattered Cairn outcome must reject when Smash is absent from the durable Codex.");

    var revisit = new ChronicleSimulation(shatteredRestored);
    revisit.Apply(new MoveIncarnation(0, -1));
    revisit.Apply(new MoveIncarnation(0, 1));
    Assert(
        WorldArea.Generate(
            revisit.State,
            SurfacePatch.SurfaceStratum,
            new WorldRectangle(1, 3, 1, 1)).Cells.Single().DurableIdentity ==
            FirstConflictSubjects.ShatteredCairnIdentity,
        "The Shattered Cairn must survive leaving and revisiting its generated Address.");

    var deathAndReplacement = new ChronicleSimulation(
        shatteredRestored with { Address = SkyStratum.LandmarkAddress });
    deathAndReplacement.Apply(new EndIncarnationAtBell());
    deathAndReplacement.Apply(new CreateReplacementIncarnation());
    Assert(
        WorldArea.Generate(
            deathAndReplacement.State,
            SurfacePatch.SurfaceStratum,
            new WorldRectangle(1, 3, 1, 1)).Cells.Single().DurableIdentity ==
            FirstConflictSubjects.ShatteredCairnIdentity,
        "The Shattered Cairn must survive Incarnation death and replacement.");

    AssertCurrentSaveRejectsUnexpectedProperty(
        threatenedJson,
        root => root["Chronicle"]!["FirstConflict"]!["Unexpected"] = true,
        "Version 5 must reject unexpected First Conflict fields.");
    AssertCurrentSaveRejectsUnexpectedProperty(
        threatenedJson,
        root => root["Chronicle"]!["FirstConflict"] = new System.Text.Json.Nodes.JsonObject(),
        "Version 5 must reject incomplete First Conflict data.");

    var offSiteThreat = System.Text.Json.Nodes.JsonNode.Parse(threatenedJson)!;
    offSiteThreat["Chronicle"]!["Address"]!["X"] = 0;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(offSiteThreat.ToJsonString()),
        "Version 5 must reject an unresolved conflict away from the Cairn.");

    var staleThreatTick = System.Text.Json.Nodes.JsonNode.Parse(threatenedJson)!;
    staleThreatTick["Chronicle"]!["Tick"] = 1;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(staleThreatTick.ToJsonString()),
        "Version 5 must reject stale unresolved threat provenance that cannot resolve on ThreatenedTick + 1.");

    var looseStoneOverlap = System.Text.Json.Nodes.JsonNode.Parse(threatenedJson)!;
    looseStoneOverlap["Chronicle"]!["LooseStoneAddress"]!["Y"] = 3;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(looseStoneOverlap.ToJsonString()),
        "Version 5 must reject a loose Stone overlap with the generated Cairn.");

    var skyLooseStoneOverlap = System.Text.Json.Nodes.JsonNode.Parse(threatenedJson)!;
    skyLooseStoneOverlap["Chronicle"]!["LooseStoneAddress"]!["Stratum"] =
        SkyStratum.StratumName;
    skyLooseStoneOverlap["Chronicle"]!["LooseStoneAddress"]!["Y"] = 3;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(skyLooseStoneOverlap.ToJsonString()),
        "Version 5 must reject a sky loose Stone whose X/Y provenance would let Fly[Stone] overwrite the Cairn.");

    var homeOverlap = System.Text.Json.Nodes.JsonNode.Parse(threatenedJson)!;
    homeOverlap["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse("""["word.found","word.smash"]""");
    homeOverlap["Chronicle"]!["Home"] = System.Text.Json.Nodes.JsonNode.Parse(
        """
        {
          "HoldingId": "holding.home",
          "DisplayName": "The First Hearth",
          "Address": { "Stratum": "surface", "X": 1, "Y": 3 },
          "FoundedTick": 0,
          "FoundingIncarnationId": 1,
          "Material": 1
        }
        """);
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(homeOverlap.ToJsonString()),
        "Version 5 must reject a Home overlap with the generated Cairn.");

    foreach (var grammarPin in new[] { 0, 1, 2 })
    {
        var migrated = ChronicleSaveCodec.Deserialize(LiteralVersion3Fixture(grammarPin));
        var oldWorld = new ChronicleSimulation(migrated);
        var against = oldWorld.Apply(new ChooseAgainstIntent());
        var oldCell = WorldArea.Generate(
            migrated,
            SurfacePatch.SurfaceStratum,
            new WorldRectangle(1, 3, 1, 1)).Cells.Single();

        Assert(
            migrated.WorldGrammarVersion == grammarPin &&
            migrated.FirstConflict is null &&
            oldCell.DurableIdentity is null &&
            !against.Applied &&
            oldWorld.State == migrated,
            "Literal version-3 predecessor pins must survive unchanged without a retroactive Cairn or AGAINST opening.");
    }

    var version3SmashCodex = System.Text.Json.Nodes.JsonNode.Parse(LiteralVersion3Fixture(2))!;
    version3SmashCodex["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse("""["word.smash"]""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(version3SmashCodex.ToJsonString()),
        "Literal version 3 must reject the later Smash Codex identity.");

    var version3SmashLoadout = System.Text.Json.Nodes.JsonNode.Parse(LiteralVersion3Fixture(2))!;
    version3SmashLoadout["Chronicle"]!["Loadout"]!["Slot1"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.smash","Noun":null}""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(version3SmashLoadout.ToJsonString()),
        "Literal version 3 must reject the later Smash Loadout identity.");

    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(LiteralVersion3Fixture(3)),
        "Literal version 3 must reject a grammar-3 pin rather than inventing a conflict state.");
}

static void VerifySaveBoundaryRejectsFutureAndMalformedState()
{
    var version2 = System.Text.Json.Nodes.JsonNode.Parse(LiteralVersion3Fixture(2))!;
    version2["Version"] = 2;
    version2["Chronicle"]!.AsObject().Remove("Home");
    version2["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """["word.fly","word.found","word.smash"]""");
    version2["Chronicle"]!["Loadout"]!["Slot1"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.fly","Noun":null}""");
    version2["Chronicle"]!["Loadout"]!["Slot2"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.found","Noun":null}""");
    version2["Chronicle"]!["Loadout"]!["Slot3"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.smash","Noun":null}""");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(version2.ToJsonString()),
        "Literal version 2 must reject Found and Smash identities introduced by later save envelopes.");

    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize("""{"Version":1,"Chronicle":{}}"""),
        "A malformed empty version-1 predecessor must not migrate into invalid current state.");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize("{}"),
        "A malformed empty pre-envelope predecessor must not migrate into invalid current state.");

    foreach (var futureGrammarPin in new[] { 1, 2 })
    {
        AssertThrows<InvalidOperationException>(
            () => ChronicleSaveCodec.Deserialize(
                LiteralPreEnvelopeFixture(futureGrammarPin)),
            $"A pre-envelope Chronicle must reject later World Grammar pin {futureGrammarPin}.");
    }

    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(LiteralVersion1Fixture(2)),
        "A version-1 Chronicle must reject the later World Grammar 2 pin.");

    var current = System.Text.Json.Nodes.JsonNode.Parse(
        ChronicleSaveCodec.Serialize(ChronicleState.Begin(41_337)))!;
    current["Chronicle"]!["Tick"] = long.MaxValue;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(current.ToJsonString()),
        "A current save must reject an unadvanceable maximum Chronicle tick.");

    current = System.Text.Json.Nodes.JsonNode.Parse(
        ChronicleSaveCodec.Serialize(ChronicleState.Begin(41_337)))!;
    current["Chronicle"]!["IncarnationId"] = long.MaxValue;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(current.ToJsonString()),
        "A current save must reject an unincrementable maximum Incarnation identity.");
}

static void VerifyCairnRejectsFoundAndFlyStoneCannotOverwriteIt()
{
    var mixed = new ChronicleSimulation(ChronicleState.Begin(41_337));
    mixed.Apply(new ChooseHereIntent());
    var mixedJson = System.Text.Json.Nodes.JsonNode.Parse(
        ChronicleSaveCodec.Serialize(mixed.State))!;
    mixedJson["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """["word.fly","word.found","word.smash","word.stone"]""");
    mixedJson["Chronicle"]!["Study"]!["Understanding"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """[{"Word":"word.stone","Amount":16}]""");
    mixedJson["Chronicle"]!["Loadout"]!["Slot2"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.smash","Noun":null}""");
    mixedJson["Chronicle"]!["Loadout"]!["Slot3"] =
        System.Text.Json.Nodes.JsonNode.Parse("""{"Verb":"word.fly","Noun":"word.stone"}""");
    var simulation = new ChronicleSimulation(ChronicleSaveCodec.Deserialize(mixedJson.ToJsonString()));

    Assert(
        simulation.State.Codex.Contains(WordIds.Fly) &&
        simulation.State.Codex.Contains(WordIds.Found) &&
        simulation.State.Codex.Contains(WordIds.Smash) &&
        simulation.State.ActiveLoadout[0] == new LoadoutSlot(WordIds.Found) &&
        simulation.State.ActiveLoadout[1] == new LoadoutSlot(WordIds.Smash) &&
        simulation.State.ActiveLoadout[2] == new LoadoutSlot(WordIds.Fly, WordIds.Stone),
        "A mixed Codex must independently fit Fly, Found, and Smash without treating an opening as a permanent class.");

    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    var intact = simulation.State;
    var found = simulation.Apply(new UseLoadoutSlot(0));
    var flyStone = simulation.Apply(new UseLoadoutSlot(2, intact.Address));

    Assert(
        !found.Applied &&
        !flyStone.Applied &&
        simulation.State == intact &&
        simulation.HomeContext.CurrentSite.IsEligible == false &&
        simulation.HomeContext.CurrentSite.DurableIdentity == FirstConflictSubjects.RivenCairnIdentity,
        "The intact Cairn must reject Found and Fly[Stone] targeting without overwriting the ward or creating Home.");

    simulation.Apply(new UseLoadoutSlot(1));
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    simulation.AdvanceClockPulse();
    var shattered = simulation.State;
    var foundAfterShatter = simulation.Apply(new UseLoadoutSlot(0));
    Assert(
        !foundAfterShatter.Applied &&
        simulation.State == shattered &&
        simulation.HomeContext.CurrentSite.DurableIdentity == FirstConflictSubjects.ShatteredCairnIdentity,
        "The Shattered Cairn must remain ineligible for Found and retain its durable material identity.");
}

static void VerifyCounterTransitionsRejectBeforeUnsavableState()
{
    var maxTickJson = System.Text.Json.Nodes.JsonNode.Parse(
        ChronicleSaveCodec.Serialize(ChronicleState.Begin(41_337)))!;
    maxTickJson["Chronicle"]!["Tick"] = long.MaxValue - 1;
    var maxTick = new ChronicleSimulation(
        ChronicleSaveCodec.Deserialize(maxTickJson.ToJsonString()));
    var beforeTick = maxTick.State;

    maxTick.AdvanceClockPulse();
    var exhaustedTick = beforeTick with { Speed = ChronicleSpeed.Paused };
    Assert(
        maxTick.State == exhaustedTick &&
        ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(maxTick.State)) == exhaustedTick,
        "A maximum-minus-one clock pulse must pause before publishing an unsaveable tick and remain exactly saveable.");
    maxTick.AdvanceClockPulse();
    Assert(
        maxTick.State == exhaustedTick,
        "An exhausted paused Chronicle must remain inert without throwing on later frame pulses.");

    var awaitingReplacement = ChronicleState.Begin(41_337) with
    {
        IncarnationId = long.MaxValue - 1,
        IncarnationLife = IncarnationLifeState.AwaitingReplacement,
    };
    var maxIncarnation = new ChronicleSimulation(
        ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(awaitingReplacement)));
    var beforeReplacement = maxIncarnation.State;

    var replacement = maxIncarnation.Apply(new CreateReplacementIncarnation());
    Assert(
        !replacement.Applied &&
        maxIncarnation.State == beforeReplacement &&
        ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(maxIncarnation.State)) ==
        beforeReplacement,
        "A maximum-minus-one replacement request must reject without throwing and leave the Chronicle exactly saveable.");
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

static void VerifyHereBuildOpeningGrantsFound()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));

    var found = WordCatalogue.Get(WordIds.Found);
    Assert(
        found.Kind == WordKind.Verb &&
        found.UnderstandingRequired == 0 &&
        found.CompatibleNouns.Count == 0,
        "Found must be the authored intrinsic zero-Understanding Build Verb.");

    var result = simulation.Apply(new ChooseHereIntent());
    Assert(result.Applied, "HERE must select the Build Starting Vector.");
    Assert(
        simulation.State.Intent == OpeningIntent.Here,
        "HERE must persist as the selected opening Intent.");
    Assert(
        simulation.State.Codex.Contains(WordIds.Found),
        "HERE must add Found to the durable Codex.");
    Assert(
        simulation.State.ActiveLoadout[0] == new LoadoutSlot(WordIds.Found),
        "HERE must equip intrinsic Found in Loadout slot 1.");
    Assert(
        simulation.State.ActiveLoadout.Slots.Count(slot => slot.Verb == WordIds.Found) == 1,
        "HERE must equip Found exactly once.");
    Assert(
        !simulation.State.CanFly &&
        simulation.State.Codex.Words.SequenceEqual([WordIds.Found]),
        "The Build opening must grant only Found and must not silently grant Explore's Fly.");

    var selected = simulation.State;
    var repeated = simulation.Apply(new ChooseHereIntent());
    Assert(
        !repeated.Applied && simulation.State == selected,
        "Repeating HERE must not duplicate Found or mutate the Chronicle.");
}

static void VerifyAgainstOpeningGrantsSmash()
{
    var smash = WordCatalogue.Get(WordIds.Smash);
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));

    var result = simulation.Apply(new ChooseAgainstIntent());

    Assert(
        smash.Kind == WordKind.Verb &&
        smash.DisplayName == "Smash" &&
        smash.UnderstandingRequired == 0 &&
        smash.CompatibleNouns.Count == 0,
        "Smash must be the authored intrinsic zero-Understanding Combat Verb.");
    Assert(
        result.Applied &&
        simulation.State.Intent == OpeningIntent.Against &&
        simulation.State.Codex.Contains(WordIds.Smash) &&
        simulation.State.ActiveLoadout[0] == new LoadoutSlot(WordIds.Smash) &&
        simulation.State.ActiveLoadout.Slots.Count(slot => slot.Verb == WordIds.Smash) == 1,
        "AGAINST must add Smash to the durable Codex and equip it exactly once in slot 1.");
    Assert(
        !simulation.State.Codex.Contains(WordIds.Fly) &&
        !simulation.State.Codex.Contains(WordIds.Found),
        "The Combat opening must grant only Smash and remain independent from Explore and Build.");

    var repeated = simulation.Apply(new ChooseAgainstIntent());
    Assert(
        !repeated.Applied,
        "Repeating AGAINST must not duplicate Smash or mutate the Chronicle.");
}

static void VerifyFoundRejectsUnsupportedUseWithoutMutation()
{
    var build = new ChronicleSimulation(ChronicleState.Begin(41_337));
    build.Apply(new ChooseHereIntent());

    var duplicateBefore = build.State;
    var duplicate = build.Apply(new ConfigureLoadoutSlot(1, WordIds.Found));
    Assert(
        !duplicate.Applied &&
        duplicate.Message == "Found already occupies another Loadout slot." &&
        build.State == duplicateBefore,
        "Found must retain the generic duplicate-Verb invariant with exact player-facing feedback.");

    build.Apply(new MoveIncarnation(0, 1));
    build.Apply(new MoveIncarnation(0, 1));
    build.Apply(new MoveIncarnation(0, 1));
    var validSite = build.State;
    var remote = build.Apply(
        new UseLoadoutSlot(
            0,
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 4)));
    Assert(
        !remote.Applied &&
        remote.Message == "Intrinsic Found acts on the current site and takes no target." &&
        build.State == validSite,
        "Intrinsic Found must reject remote targeting without mutation.");

    var empty = new ChronicleSimulation(
        validSite with
        {
            Loadout = LoadoutState.Empty,
        });
    var emptyBefore = empty.State;
    var emptyResult = empty.Apply(new UseLoadoutSlot(0));
    Assert(
        !emptyResult.Applied &&
        emptyResult.Message == "Loadout slot 1 is empty." &&
        empty.State == emptyBefore,
        "A valid site without equipped Found must reject through the existing Loadout seam.");

    var unsupportedSurface = new ChronicleSimulation(
        validSite with
        {
            Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, -5),
        });
    var unsupportedBefore = unsupportedSurface.State;
    var unsupported = unsupportedSurface.Apply(new UseLoadoutSlot(0));
    Assert(
        !unsupported.Applied &&
        unsupported.Message == "Home requires an existing Stone feature." &&
        unsupportedSurface.State == unsupportedBefore,
        "Found must reject an unsupported surface clearing without changing terrain.");

    var sky = new ChronicleSimulation(
        validSite with
        {
            Address = SkyStratum.LandmarkAddress,
        });
    var skyBefore = sky.State;
    var skyResult = sky.Apply(new UseLoadoutSlot(0));
    Assert(
        !skyResult.Applied &&
        skyResult.Message == "Home must be founded on the surface." &&
        sky.State == skyBefore,
        "Found must reject the Bell and every sky site in the first fixture.");

    var dead = new ChronicleSimulation(
        validSite with
        {
            IncarnationLife = IncarnationLifeState.AwaitingReplacement,
        });
    var deadBefore = dead.State;
    var deadResult = dead.Apply(new UseLoadoutSlot(0));
    Assert(
        !deadResult.Applied &&
        deadResult.Message == "The Chronicle is awaiting a replacement Incarnation." &&
        dead.State == deadBefore,
        "Found must reject when there is no living Incarnation.");

    var explore = new ChronicleSimulation(ChronicleState.Begin(41_337));
    explore.Apply(new ChooseUpIntent());
    var exploreBefore = explore.State;
    var exploreFound = explore.Apply(new ConfigureLoadoutSlot(1, WordIds.Found));
    Assert(
        !exploreFound.Applied &&
        exploreFound.Message == "Found is not in the Codex." &&
        explore.State == exploreBefore,
        "The Explore opening must not receive Build's Found for free.");
}

static void VerifyHomeSiteEligibility()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseHereIntent());

    var start = simulation.State;
    var flooded = simulation.HomeContext;
    Assert(
        flooded.Home is null &&
        flooded.ReturnRoute is null &&
        flooded.CurrentSite.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0) &&
        flooded.CurrentSite.Ground == WorldGround.Water &&
        flooded.CurrentSite.Feature == WorldFeature.Stone &&
        !flooded.CurrentSite.IsEligible &&
        flooded.CurrentSite.Reason == "The Stone here is under water.",
        "The seed-41337 start must expose the exact invalid water-covered Stone fixture.");
    Assert(simulation.State == start, "Querying flooded Home eligibility must not mutate the Chronicle.");

    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    var supportedState = simulation.State;
    var supported = simulation.HomeContext;
    Assert(
        supported.CurrentSite.Address ==
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
        supported.CurrentSite.Ground == WorldGround.Soil &&
        supported.CurrentSite.Feature == WorldFeature.Stone &&
        supported.CurrentSite.DurableIdentity is null &&
        supported.CurrentSite.IsEligible &&
        supported.CurrentSite.Reason == "The supported Stone here can become Home.",
        "Three south steps must expose the exact eligible soil-supported Stone fixture.");
    Assert(simulation.State == supportedState, "Querying eligible Home context must not mutate the Chronicle.");
}

static void VerifyFoundEstablishesSingularHome()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseHereIntent());

    var floodedState = simulation.State;
    var flooded = simulation.Apply(new UseLoadoutSlot(0));
    Assert(
        !flooded.Applied &&
        flooded.Message == "The Stone here is under water." &&
        simulation.State == floodedState &&
        simulation.State.Home is null,
        "Found at the water-covered start must explain the invalid site and leave the Chronicle unchanged.");

    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    var foundationAddress = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3);
    var before = simulation.State;

    var founded = simulation.Apply(new UseLoadoutSlot(0));
    Assert(
        founded.Applied &&
        founded.Message == "Founded The First Hearth at surface (0, 3)." &&
        simulation.State.Address == foundationAddress,
        "Intrinsic Found must establish Home at the current eligible address without moving.");
    Assert(
        simulation.State.Home == new HomeState(
            "holding.home",
            "The First Hearth",
            foundationAddress,
            before.Tick,
            before.IncarnationId,
            HomeMaterialState.HearthstoneRaised),
        "Found must save the exact singular Home identity, founding facts, and material state.");
    Assert(
        simulation.HomeContext.Home == simulation.State.Home,
        "HomeContext must present the canonical saved Home.");

    var established = simulation.State;
    var repeated = simulation.Apply(new UseLoadoutSlot(0));
    Assert(
        !repeated.Applied &&
        repeated.Message == "The Chronicle already has its singular Home." &&
        simulation.State == established,
        "A repeated Found use must preserve the singular Home without mutation.");
}

static void VerifyHomeReturnRouteGuidesPhysicalSteps()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseHereIntent());
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new UseLoadoutSlot(0));

    var foundedState = simulation.State;
    var arrived = simulation.HomeContext.ReturnRoute;
    Assert(
        arrived == new ReturnRouteSnapshot(
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3),
            IsTraversable: true,
            Arrived: true,
            NextAddress: null,
            RemainingSteps: 0),
        "The Return Route must report exact arrival at Home.");
    Assert(simulation.State == foundedState, "Arrival route queries must not mutate the Chronicle.");

    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.Apply(new MoveIncarnation(0, -1));
    simulation.Apply(new MoveIncarnation(0, -1));
    simulation.Apply(new MoveIncarnation(0, -1));
    var away = simulation.State;
    var route = simulation.HomeContext.ReturnRoute;
    Assert(
        route == new ReturnRouteSnapshot(
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3),
            IsTraversable: true,
            Arrived: false,
            NextAddress: new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0),
            RemainingSteps: 5),
        "The Return Route must resolve X before Y and expose the exact remaining physical step count.");
    Assert(
        simulation.State == away &&
        simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 2, 0),
        "Reading a Return Route must never move the Incarnation.");

    var physicalReturn =
        new[]
        {
            new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0),
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 1),
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 2),
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3),
        };
    for (var index = 0; index < physicalReturn.Length; index++)
    {
        var expectedNext = physicalReturn[index];
        var currentRoute = simulation.HomeContext.ReturnRoute;
        Assert(
            currentRoute?.NextAddress == expectedNext &&
            currentRoute.Value.RemainingSteps == (UInt128)(physicalReturn.Length - index),
            "Every Return Route step must expose the next exact ordinary movement and remaining count.");
        var current = simulation.State.Address;
        simulation.Apply(
            new MoveIncarnation(
                checked((int)(expectedNext.X - current.X)),
                checked((int)(expectedNext.Y - current.Y))));
    }
    Assert(
        simulation.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
        simulation.HomeContext.ReturnRoute?.Arrived == true,
        "Following only displayed cardinal steps must physically arrive at Home.");

    var offSurface = new ChronicleSimulation(
        simulation.State with
        {
            Address = new WorldAddress(SkyStratum.StratumName, 2, 0),
        });
    Assert(
        offSurface.HomeContext.ReturnRoute == new ReturnRouteSnapshot(
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3),
            IsTraversable: false,
            Arrived: false,
            NextAddress: null,
            RemainingSteps: 0),
        "A 4B Return Route outside Home's surface Stratum must remain known but untraversable.");
}

static void VerifyHomeHearthstoneOverlaysExistingRidge()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseHereIntent());
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));

    var homeBounds = new WorldRectangle(0, 3, 1, 1);
    var before = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        homeBounds).Cells.Single();
    Assert(
        before.Ground == WorldGround.Soil &&
        before.Feature == WorldFeature.Stone &&
        before.DurableIdentity is null,
        "The foundation fixture must begin as generated soil-supported Stone without a durable identity.");

    simulation.Apply(new UseLoadoutSlot(0));
    var foundedState = simulation.State;
    var after = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        homeBounds).Cells.Single();
    Assert(
        after == before with
        {
            DurableIdentity = ChronicleState.HomeHearthstoneIdentity,
        } &&
        after.DurableIdentity == "The First Hearthstone",
        "Found must add only The First Hearthstone identity over the unchanged generated ridge.");

    var looseStone = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(1, 0, 1, 1)).Cells.Single();
    Assert(
        looseStone.DurableIdentity == ChronicleState.LooseStoneIdentity &&
        simulation.State.LooseStoneAddress ==
            new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0),
        "Founding Home must not consume, move, or rename the separate loose Stone.");
    Assert(
        WorldArea.Generate(
            simulation.State,
            SurfacePatch.SurfaceStratum,
            homeBounds).Cells.Single() == after &&
        simulation.State == foundedState,
        "Hearthstone area queries must be deterministic and read-only.");
}

static void VerifyHomeSaveV5AndVersion2Migration()
{
    const string goal4AVersion2Json =
        """
        {
          "Version": 2,
          "Chronicle": {
            "Seed": 41337,
            "Tick": 600,
            "Address": {
              "Stratum": "surface",
              "X": 0,
              "Y": 0
            },
            "Speed": 2,
            "Intent": 1,
            "Codex": {
              "Words": [
                "word.fly"
              ]
            },
            "Study": {
              "Understanding": [],
              "ActiveSourceId": null,
              "ActiveWord": null
            },
            "Loadout": {
              "Slot1": {
                "Verb": null,
                "Noun": null
              },
              "Slot2": {
                "Verb": null,
                "Noun": null
              },
              "Slot3": {
                "Verb": null,
                "Noun": null
              },
              "Slot4": {
                "Verb": null,
                "Noun": null
              },
              "Slot5": {
                "Verb": null,
                "Noun": null
              },
              "Slot6": {
                "Verb": null,
                "Noun": null
              },
              "Slot7": {
                "Verb": null,
                "Noun": null
              },
              "Slot8": {
                "Verb": null,
                "Noun": null
              }
            },
            "LooseStoneAddress": {
              "Stratum": "surface",
              "X": 1,
              "Y": 0
            },
            "IncarnationId": 1,
            "IncarnationLife": 0,
            "WorldGrammarVersion": 2
          }
        }
        """;

    var migrated = ChronicleSaveCodec.Deserialize(goal4AVersion2Json);
    Assert(
        migrated.Home is null &&
        migrated.Seed == 41_337 &&
        migrated.Tick == 600 &&
        migrated.Intent == OpeningIntent.Up &&
        migrated.Codex.Words.SequenceEqual([WordIds.Fly]) &&
        migrated.ActiveLoadout.Slots.All(slot => slot.IsEmpty),
        "A literal Goal-4A version-2 save must migrate exactly without inventing Home.");

    var illegalVersion2 = System.Text.Json.Nodes.JsonNode.Parse(goal4AVersion2Json)!;
    illegalVersion2["Chronicle"]!["Intent"] = 2;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(illegalVersion2.ToJsonString()),
        "A version-2 predecessor save must reject the later HERE Intent.");

    var futureVersion2 = System.Text.Json.Nodes.JsonNode.Parse(goal4AVersion2Json)!;
    futureVersion2["Chronicle"]!["WorldGrammarVersion"] = 3;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(futureVersion2.ToJsonString()),
        "A version-2 predecessor save must not acquire the later grammar-3 Cairn rule.");

    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseHereIntent());
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new UseLoadoutSlot(0));
    var founded = simulation.State;

    var json = ChronicleSaveCodec.Serialize(founded);
    var document = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
    var chronicle = document["Chronicle"]!.AsObject();
    var home = chronicle["Home"]!.AsObject();
    Assert(
        document["Version"]!.GetValue<int>() == 5 &&
        home["HoldingId"]!.GetValue<string>() == "holding.home" &&
        home["DisplayName"]!.GetValue<string>() == "The First Hearth" &&
        home["Address"]!["Stratum"]!.GetValue<string>() == SurfacePatch.SurfaceStratum &&
        home["Address"]!["X"]!.GetValue<long>() == 0 &&
        home["Address"]!["Y"]!.GetValue<long>() == 3 &&
        home["FoundedTick"]!.GetValue<long>() == founded.Tick &&
        home["FoundingIncarnationId"]!.GetValue<long>() == founded.IncarnationId &&
        home["Material"]!.GetValue<int>() == (int)HomeMaterialState.HearthstoneRaised,
        "Version 5 must explicitly serialize the exact singular Home state.");
    Assert(
        !json.Contains("ReturnRoute", StringComparison.Ordinal) &&
        !json.Contains("CurrentSite", StringComparison.Ordinal),
        "Current saves must derive Home context and route facts instead of serializing query snapshots.");

    var restored = ChronicleSaveCodec.Deserialize(json);
    Assert(
        restored == founded &&
        new ChronicleSimulation(restored).HomeContext.ReturnRoute?.Arrived == true,
        "Version 5 must round-trip Home exactly and regenerate route knowledge.");
    var explorerSave = ChronicleSaveCodec.Serialize(
        founded with
        {
            Intent = OpeningIntent.Up,
        });
    var explorerRestored = ChronicleSaveCodec.Deserialize(explorerSave);
    Assert(
        explorerRestored.Intent == OpeningIntent.Up &&
        explorerRestored.Codex.Contains(WordIds.Found) &&
        explorerRestored.Home == founded.Home,
        "A current Home must persist for a non-HERE Chronicle that durably knows Found.");

    var noHomeJson = ChronicleSaveCodec.Serialize(ChronicleState.Begin(41_337));
    var noHomeChronicle =
        System.Text.Json.Nodes.JsonNode.Parse(noHomeJson)!["Chronicle"]!.AsObject();
    Assert(
        noHomeChronicle.ContainsKey("Home") &&
        noHomeChronicle["Home"] is null,
        "Version 5 must serialize an explicit nullable Home.");
    noHomeChronicle.Remove("Home");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(
            new System.Text.Json.Nodes.JsonObject
            {
                ["Version"] = 5,
                ["Chronicle"] = noHomeChronicle,
            }.ToJsonString()),
        "A version-5 Chronicle missing its explicit Home field must be rejected.");
}

static void VerifyNonHereChronicleCanRetainEquipAndFoundHome()
{
    var build = new ChronicleSimulation(ChronicleState.Begin(41_337));
    Assert(build.Apply(new ChooseHereIntent()).Applied, "The retained-Found fixture must first learn Found.");

    var retained = System.Text.Json.Nodes.JsonNode.Parse(
        ChronicleSaveCodec.Serialize(build.State))!;
    retained["Chronicle"]!["Intent"] = (int)OpeningIntent.Up;
    retained["Chronicle"]!["Codex"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """{"Words":["word.fly","word.found"]}""");
    retained["Chronicle"]!["Loadout"]!["Slot1"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """{"Verb":"word.fly","Noun":null}""");

    var simulation = new ChronicleSimulation(
        ChronicleSaveCodec.Deserialize(retained.ToJsonString()));
    Assert(
        simulation.State.Intent == OpeningIntent.Up &&
        simulation.State.Codex.Contains(WordIds.Fly) &&
        simulation.State.Codex.Contains(WordIds.Found) &&
        simulation.State.ActiveLoadout[0].IsIntrinsicFly,
        "A non-HERE Chronicle may retain learned Found alongside the Explore opening without becoming a class.");
    Assert(
        simulation.Apply(new ClearLoadoutSlot(0)).Applied &&
        simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Found)).Applied,
        "A non-HERE Incarnation retaining Found must re-equip it through the public Loadout seam.");

    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    Assert(
        simulation.Apply(new UseLoadoutSlot(0)).Applied &&
        simulation.State.Home?.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3),
        "A non-HERE Incarnation with equipped Found must establish Home through the public command seam.");

    var restored = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(simulation.State));
    Assert(
        restored.Intent == OpeningIntent.Up &&
        restored.Codex.Contains(WordIds.Found) &&
        restored.Home == simulation.State.Home,
        "A non-HERE Chronicle must save and restore the Home founded through retained Found.");

    var freshExplore = BeginWithUp();
    Assert(
        !freshExplore.State.Codex.Contains(WordIds.Found),
        "UP must not grant Found merely because another Chronicle can retain it.");
}

static void VerifyHomeSurvivesReplacementAndFoundRemainsReequipable()
{
    var unfounded = AtBellWithFound();
    Assert(
        unfounded.Apply(new EndIncarnationAtBell()).Applied,
        "The unfounded replacement fixture must end the first Incarnation through the public Bell command.");
    Assert(
        unfounded.Apply(new CreateReplacementIncarnation()).Applied &&
        unfounded.State.Home is null &&
        unfounded.State.IncarnationId == 2 &&
        unfounded.State.Codex.Contains(WordIds.Found) &&
        unfounded.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty),
        "A replacement must retain Found in the Codex but begin empty when Home was not established.");
    Assert(
        unfounded.Apply(new ConfigureLoadoutSlot(0, WordIds.Found)).Applied,
        "A later living Incarnation must be able to re-equip retained Found.");
    unfounded.Apply(new MoveIncarnation(0, 1));
    unfounded.Apply(new MoveIncarnation(0, 1));
    unfounded.Apply(new MoveIncarnation(0, 1));
    Assert(
        unfounded.Apply(new UseLoadoutSlot(0)).Applied &&
        unfounded.State.Home?.FoundingIncarnationId == 2,
        "A replacement must be able to establish Home when the earlier body did not.");

    var founded = FoundedHereHome();
    var homeBearingAtBell = new ChronicleSimulation(
        founded with { Address = SkyStratum.LandmarkAddress });
    var firstHome = homeBearingAtBell.State.Home;
    Assert(
        homeBearingAtBell.Apply(new EndIncarnationAtBell()).Applied,
        "The Home-bearing fixture must end the first Incarnation through the public Bell command.");
    Assert(
        homeBearingAtBell.Apply(new CreateReplacementIncarnation()).Applied,
        "The Home-bearing fixture must create its replacement through the public command.");
    var replacement = homeBearingAtBell;
    Assert(
        replacement.State.Home == firstHome &&
        replacement.State.IncarnationId == 2 &&
        replacement.State.Codex.Contains(WordIds.Found) &&
        replacement.State.ActiveLoadout.Slots.All(slot => slot.IsEmpty),
        "Home and Found must survive replacement while the new body's Loadout resets.");
    Assert(
        replacement.HomeContext.ReturnRoute == new ReturnRouteSnapshot(
            new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3),
            IsTraversable: true,
            Arrived: false,
            NextAddress: new WorldAddress(SurfacePatch.SurfaceStratum, 0, 1),
            RemainingSteps: 3),
        "The replacement must regain exact physical route knowledge from the surface origin.");
    var hearthstone = WorldArea.Generate(
        replacement.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(0, 3, 1, 1)).Cells.Single();
    Assert(
        hearthstone.DurableIdentity == ChronicleState.HomeHearthstoneIdentity,
        "The First Hearthstone must remain materially present after replacement.");
    Assert(replacement.Apply(new ConfigureLoadoutSlot(0, WordIds.Found)).Applied, "The replacement must re-equip Found.");
    var beforeRepeatedFound = replacement.State;
    var repeatedFound = replacement.Apply(new UseLoadoutSlot(0));
    Assert(
        !repeatedFound.Applied &&
        repeatedFound.Message == "The Chronicle already has its singular Home." &&
        replacement.State == beforeRepeatedFound,
        "A replacement must not use retained Found to create a second Home.");
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
        stoneSimulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone)).Applied,
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
    movedStone.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
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
        BellAddress = SkyStratum.LandmarkAddress,
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

    ChooseStone(simulation);

    Assert(simulation.State == before, "Study away from the Bell must leave state unchanged.");
}

static void VerifyStudyPause()
{
    var simulation = AtBellWithFly();
    ChooseStone(simulation);
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
    ChooseStone(simulation);
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
    ChooseStone(simulation);
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
    ChooseStone(simulation);
    AdvanceTicks(simulation, StudyState.StoneUnderstandingRequired);

    Assert(simulation.State.Codex.HasStone, "Sixteen Chronicle ticks of Study must learn Stone.");
    Assert(
        simulation.State.Study.StoneUnderstanding == StudyState.StoneUnderstandingRequired,
        "Completed Study must retain its full understanding threshold.");
    Assert(!simulation.State.Study.IsStudyingBell, "Completing Study must stop its active state.");

    var completed = simulation.State;
    ChooseStone(simulation);
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
    ChooseStone(simulation);
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
        restored.ActiveLoadout[0] == new LoadoutSlot(WordIds.Fly),
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

static void VerifyLoadoutUsesCatalogueWordIdentities()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    var configured = simulation.Apply(
        new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));

    Assert(configured.Applied, "Catalogue-authored Fly and Stone identities must configure Fly[Stone].");
    Assert(
        simulation.State.ActiveLoadout[0] == new LoadoutSlot(WordIds.Fly, WordIds.Stone),
        "Loadout state must retain the same stable Word identities used by the Catalogue, Codex, and Study.");

    var bellStudy = AtBellWithFly();
    var source = bellStudy.CurrentStudySource
        ?? throw new InvalidOperationException("The Bell compatibility fixture must expose its Study Source.");
    bellStudy.Apply(new ChooseStudyWord(source.Id, WordIds.Bell));
    AdvanceTicks(bellStudy, StudyState.UnderstandingRequired);
    var beforeBell = bellStudy.State;
    var bellResult = bellStudy.Apply(
        new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Bell));

    Assert(bellStudy.State.Codex.Contains(WordIds.Bell), "The compatibility fixture must first learn Bell.");
    Assert(
        bellResult.Applied &&
        bellStudy.State != beforeBell &&
        bellStudy.State.ActiveLoadout[0] == new LoadoutSlot(WordIds.Fly, WordIds.Bell),
        "Known Bell must fit into Fly through the authored Slice 5 compatibility.");
}

static void VerifyLoadoutIdentityPersistenceAndPredecessorCollision()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    var json = ChronicleSaveCodec.Serialize(simulation.State);
    using var document = System.Text.Json.JsonDocument.Parse(json);
    var slot = document.RootElement
        .GetProperty("Chronicle")
        .GetProperty("Loadout")
        .GetProperty("Slot1");

    Assert(
        slot.GetProperty("Verb").GetString() == WordIds.Fly.Value &&
        slot.GetProperty("Noun").GetString() == WordIds.Stone.Value,
        "Version 5 must serialize Loadout words as their exact stable string identities.");
    Assert(
        !json.Contains("\"Value\"", StringComparison.Ordinal),
        "Version 5 must not leak WordId implementation objects into saved Loadout state.");
    Assert(
        ChronicleSaveCodec.Deserialize(json) == simulation.State,
        "A string-identity Loadout must round-trip exactly through version 5.");

    const string version1FittedJson =
        """
        {
          "Version": 1,
          "Chronicle": {
            "Seed": 41337,
            "Tick": 16,
            "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
            "Speed": 2,
            "Intent": 1,
            "Codex": { "HasFly": true, "HasStone": true },
            "Study": { "StoneUnderstanding": 16, "IsStudyingBell": false },
            "Loadout": {
              "Slot1": { "Verb": 1, "Noun": 1 }
            },
            "WorldGrammarVersion": 1
          }
        }
        """;
    var fitted = ChronicleSaveCodec.Deserialize(version1FittedJson);
    Assert(
        fitted.WorldGrammarVersion == 2 &&
        fitted.ActiveLoadout[0] == new LoadoutSlot(WordIds.Fly, WordIds.Stone),
        "Version 1 must map its colliding Verb=1 and Noun=1 fields independently to Fly[Stone].");

    const string absentLoadoutJson =
        """
        {
          "Seed": 41337,
          "Tick": 0,
          "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
          "Speed": 2,
          "Intent": 1,
          "Codex": { "HasFly": true, "HasStone": false }
        }
        """;
    const string emptyLoadoutJson =
        """
        {
          "Seed": 41337,
          "Tick": 0,
          "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
          "Speed": 2,
          "Intent": 1,
          "Codex": { "HasFly": true, "HasStone": false },
          "Loadout": {}
        }
        """;
    Assert(
        ChronicleSaveCodec.Deserialize(absentLoadoutJson).ActiveLoadout[0].IsIntrinsicFly,
        "A predecessor save with no Loadout field must receive its historical intrinsic Fly slot.");
    Assert(
        ChronicleSaveCodec.Deserialize(emptyLoadoutJson).ActiveLoadout.Slots.All(candidate => candidate.IsEmpty),
        "A predecessor save with an explicit empty Loadout must remain empty.");

    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(
            """
            {
              "Seed": 41337,
              "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
              "Speed": 2,
              "Codex": { "HasFly": true },
              "Loadout": { "Slot1": { "Verb": 2, "Noun": null } }
            }
            """),
        "An unknown predecessor Verb number must be rejected independently.");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(
            """
            {
              "Seed": 41337,
              "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
              "Speed": 2,
              "Codex": { "HasFly": true, "HasStone": true },
              "Loadout": { "Slot1": { "Verb": 1, "Noun": 2 } }
            }
            """),
        "An unknown predecessor Noun number must be rejected independently.");
}

static void VerifyOnlyCodexLanguageCanBeEquipped()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    var beforeFly = simulation.State;
    var flyResult = simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly));

    Assert(!flyResult.Applied && simulation.State == beforeFly, "Unknown Fly must not be equipped.");
    Assert(flyResult.Message.Contains("Codex", StringComparison.Ordinal), "Unknown language must return a legible Core rejection.");

    simulation.Apply(new ChooseUpIntent());
    var beforeStone = simulation.State;
    var stoneResult = simulation.Apply(
        new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));

    Assert(!stoneResult.Applied && simulation.State == beforeStone, "Unknown Stone must not be fitted into Fly.");
    Assert(stoneResult.Message.Contains("Stone", StringComparison.Ordinal), "Unknown Stone must report why fitting failed.");

    var invalidResult = simulation.Apply(
        new ConfigureLoadoutSlot(0, new WordId("word.unknown")));
    Assert(!invalidResult.Applied && simulation.State == beforeStone, "An invalid Verb value must leave state unchanged.");
}

static void VerifyVerbCannotOccupyTwoSlots()
{
    var simulation = BeginWithUp();
    var before = simulation.State;
    var result = simulation.Apply(new ConfigureLoadoutSlot(1, WordIds.Fly));

    Assert(!result.Applied, "A learned Verb already in one slot must not be duplicated.");
    Assert(simulation.State == before, "Rejected duplicate configuration must leave every slot unchanged.");
    Assert(
        simulation.State.ActiveLoadout.Slots.Count(slot => slot.Verb == WordIds.Fly) == 1,
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
        new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
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
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly));
    simulation.Apply(new UseLoadoutSlot(0));
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));

    var incarnationAddress = simulation.State.Address;
    var result = simulation.Apply(
        new UseLoadoutSlot(0, new WorldAddress(SkyStratum.StratumName, 1, 0)));

    Assert(result.Applied, "Fly[Stone] must act on the same loose Stone in the sky.");
    Assert(simulation.State.Address == incarnationAddress, "Returning Stone must not move the Incarnation.");
    Assert(
        simulation.State.LooseStoneAddress == ChronicleState.InitialLooseStoneAddress,
        "The same Expression must return Stone to its matching surface address.");
}

static void VerifyFlyStoneCannotOverlapHome()
{
    var fixture = new ChronicleSimulation(ChronicleState.Begin(15));
    fixture.Apply(new ChooseHereIntent());
    var json = System.Text.Json.Nodes.JsonNode.Parse(
        ChronicleSaveCodec.Serialize(fixture.State))!;
    json["Chronicle"]!["Address"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """{"Stratum":"sky","X":0,"Y":0}""");
    json["Chronicle"]!["Codex"]!["Words"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """["word.fly","word.found","word.stone"]""");
    json["Chronicle"]!["Study"]!["Understanding"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """[{"Word":"word.stone","Amount":16}]""");
    json["Chronicle"]!["Loadout"]!["Slot1"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """{"Verb":"word.fly","Noun":"word.stone"}""");
    json["Chronicle"]!["LooseStoneAddress"] =
        System.Text.Json.Nodes.JsonNode.Parse(
            """{"Stratum":"sky","X":1,"Y":0}""");
    json["Chronicle"]!["Home"] = System.Text.Json.Nodes.JsonNode.Parse(
        """
        {
          "HoldingId": "holding.home",
          "DisplayName": "The First Hearth",
          "Address": { "Stratum": "surface", "X": 1, "Y": 0 },
          "FoundedTick": 0,
          "FoundingIncarnationId": 1,
          "Material": 1
        }
        """);

    var simulation = new ChronicleSimulation(
        ChronicleSaveCodec.Deserialize(json.ToJsonString()));
    var before = simulation.State;
    Assert(
        simulation.ValidTargetsForSlot(0).Count == 0,
        "Core must not advertise a Fly[Stone] target whose destination would overlap Home.");
    var result = simulation.Apply(
        new UseLoadoutSlot(
            0,
            new WorldAddress(SkyStratum.StratumName, 1, 0)));

    Assert(
        !result.Applied &&
        simulation.State == before &&
        ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(simulation.State)) == before,
        "Fly[Stone] must reject before returning the loose Stone onto Home and preserve a saveable state.");
}

static void VerifyFlyStoneRejectsInvalidTargets()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));

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

static void VerifyFlyBellMovesDurableSituation()
{
    var simulation = LearnedBellAdjacent();
    var originalBell = SkyStratum.LandmarkAddress;
    var movedBell = originalBell with { Stratum = SurfacePatch.SurfaceStratum };
    var incarnationAddress = simulation.State.Address;

    Assert(
        simulation.ValidTargetsForSlot(0).SequenceEqual([originalBell]),
        "The shared fitted-Fly query must expose the adjacent Bell subject.");
    var result = simulation.Apply(new UseLoadoutSlot(0, originalBell));

    Assert(result.Applied, "Fly[Bell] must move the authored Bell subject.");
    Assert(simulation.State.Address == incarnationAddress, "Fly[Bell] must not move the Incarnation.");
    Assert(
        simulation.State.CurrentBellAddress == movedBell,
        "Fly[Bell] must preserve Bell coordinates while moving it to the matching surface site.");

    var oldCell = WorldArea.Generate(
        simulation.State,
        SkyStratum.StratumName,
        new WorldRectangle(originalBell.X, originalBell.Y, 1, 1)).Cells.Single();
    var movedCell = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(movedBell.X, movedBell.Y, 1, 1)).Cells.Single();
    Assert(
        oldCell.DurableIdentity != SkyStratum.LandmarkName &&
        movedCell.DurableIdentity == SkyStratum.LandmarkName,
        "World queries must expose exactly one Bell at its durable moved Address.");
    Assert(
        WorldArea.Generate(
            simulation.State,
            SurfacePatch.SurfaceStratum,
            new WorldRectangle(movedBell.X, movedBell.Y, 1, 1)).Cells.Single() == movedCell &&
        WorldArea.Generate(
            simulation.State,
            SkyStratum.StratumName,
            new WorldRectangle(originalBell.X, originalBell.Y, 1, 1)).Cells.Single() == oldCell,
        "Moved-Bell World queries must be deterministic and independent of query order.");

    var oldSource = new ChronicleSimulation(simulation.State with { Address = originalBell });
    var movedSource = new ChronicleSimulation(simulation.State with { Address = movedBell });
    Assert(oldSource.CurrentStudySource is null, "The Bell Study Source must leave the old sky Address.");
    Assert(
        movedSource.CurrentStudySource is { Address: var sourceAddress } && sourceAddress == movedBell,
        "The same Bell Study Source must follow the durable Bell to the surface.");

    var ended = movedSource.Apply(new EndIncarnationAtBell());
    Assert(
        ended.Applied && movedSource.State.IncarnationLife == IncarnationLifeState.AwaitingReplacement,
        "The Bell's retained death affordance must follow its durable moved Address.");
}

static void VerifyFlyBellSaveV5AndLiteralV4Migration()
{
    var moved = ReplayFlyBell();
    Assert(ReplayFlyBell() == moved, "The same Fly[Bell] command stream must replay identically.");
    var json = ChronicleSaveCodec.Serialize(moved);
    var restored = ChronicleSaveCodec.Deserialize(json);

    Assert(
        json.Contains("\"Version\": 5", StringComparison.Ordinal) &&
        json.Contains("\"BellAddress\"", StringComparison.Ordinal) &&
        restored == moved,
        "Save v5 must round-trip the exact moved Bell and fitted Fly[Bell] Loadout.");

    var missingBell = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
    missingBell["Chronicle"]!.AsObject().Remove("BellAddress");
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(missingBell.ToJsonString()),
        "Save v5 must reject a missing durable Bell Address.");

    var malformedBell = System.Text.Json.Nodes.JsonNode.Parse(json)!;
    malformedBell["Chronicle"]!["BellAddress"]!["Y"] = -3;
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(malformedBell.ToJsonString()),
        "Save v5 must reject Bell coordinates outside its authored provenance.");

    const string version4Json =
        """
        {
          "Version": 4,
          "Chronicle": {
            "Seed": 41337,
            "Tick": 7,
            "Address": { "Stratum": "sky", "X": 0, "Y": -4 },
            "Speed": 2,
            "Intent": 1,
            "Codex": { "Words": ["word.fly"] },
            "Study": {
              "Understanding": [{ "Word": "word.stone", "Amount": 7 }],
              "ActiveSourceId": "study-source.bell-that-fell-up.sky-stone",
              "ActiveWord": "word.stone"
            },
            "Loadout": {
              "Slot1": { "Verb": "word.fly", "Noun": null },
              "Slot2": { "Verb": null, "Noun": null },
              "Slot3": { "Verb": null, "Noun": null },
              "Slot4": { "Verb": null, "Noun": null },
              "Slot5": { "Verb": null, "Noun": null },
              "Slot6": { "Verb": null, "Noun": null },
              "Slot7": { "Verb": null, "Noun": null },
              "Slot8": { "Verb": null, "Noun": null }
            },
            "LooseStoneAddress": { "Stratum": "surface", "X": 1, "Y": 0 },
            "IncarnationId": 1,
            "IncarnationLife": 0,
            "WorldGrammarVersion": 3,
            "Home": null,
            "FirstConflict": null
          }
        }
        """;

    var migrated = ChronicleSaveCodec.Deserialize(version4Json);
    Assert(
        migrated.BellAddress == SkyStratum.LandmarkAddress &&
        migrated.Study.ActiveSourceId == StudySourceIds.BellSkyStone &&
        migrated.Study.UnderstandingFor(WordIds.Stone) == 7,
        "A literal v4 save must gain the fixed Bell Address without losing active Study.");

    foreach (var predecessorVersion in new[] { 2, 3, 4 })
    {
        var forged = System.Text.Json.Nodes.JsonNode.Parse(version4Json)!;
        var chronicle = forged["Chronicle"]!.AsObject();
        forged["Version"] = predecessorVersion;
        chronicle["WorldGrammarVersion"] = predecessorVersion == 4 ? 3 : 2;
        chronicle["Codex"]!["Words"] =
            System.Text.Json.Nodes.JsonNode.Parse("""["word.fly","word.bell"]""");
        chronicle["Study"] = System.Text.Json.Nodes.JsonNode.Parse(
            """{"Understanding":[{"Word":"word.bell","Amount":16}],"ActiveSourceId":null,"ActiveWord":null}""");
        chronicle["Loadout"]!["Slot1"]!["Noun"] = WordIds.Bell.Value;
        if (predecessorVersion < 4)
        {
            chronicle.Remove("FirstConflict");
        }

        if (predecessorVersion < 3)
        {
            chronicle.Remove("Home");
        }

        AssertThrows<InvalidOperationException>(
            () => ChronicleSaveCodec.Deserialize(forged.ToJsonString()),
            $"A forged v{predecessorVersion} save must not acquire the later Fly[Bell] compatibility.");
    }
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
        new ChooseStudyWord(StudySourceIds.BellSkyStone, WordIds.Stone),
        new ConfigureLoadoutSlot(0, WordIds.Fly),
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
    Assert(
        migrated.ActiveLoadout[0] == new LoadoutSlot(WordIds.Fly, WordIds.Stone),
        "Slice 2B migration must preserve its fitted Loadout.");
    Assert(
        migrated.LooseStoneAddress == new WorldAddress(SkyStratum.StratumName, 1, 0),
        "Slice 2B migration must preserve its moved Stone.");

    var awaiting = EndedChronicle().State;
    var awaitingJson = ChronicleSaveCodec.Serialize(awaiting);
    var restoredAwaiting = ChronicleSaveCodec.Deserialize(awaitingJson);
    Assert(awaitingJson.Contains("\"Version\": 5", StringComparison.Ordinal), "Current saves must use version 5.");
    Assert(awaitingJson.Contains("\"Chronicle\"", StringComparison.Ordinal), "Current saves must wrap Chronicle state.");
    Assert(restoredAwaiting == awaiting, "Save/load before replacement must preserve the awaiting Chronicle exactly.");

    var simulation = new ChronicleSimulation(restoredAwaiting);
    simulation.Apply(new CreateReplacementIncarnation());
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly));
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
        Slot1: new LoadoutSlot(WordIds.Fly)),
    LooseStoneAddress = ChronicleState.InitialLooseStoneAddress,
};

static LoadoutState IntrinsicFlyLoadout() => new(
    Slot1: new LoadoutSlot(WordIds.Fly));

static ChronicleState ReplayFlyStone()
{
    var simulation = new ChronicleSimulation(LearnedAtSurface());
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly));
    simulation.Apply(new UseLoadoutSlot(0));
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    simulation.Apply(new UseLoadoutSlot(0, new WorldAddress(SkyStratum.StratumName, 1, 0)));
    return simulation.State;
}

static ChronicleState ReplayFlyBell()
{
    var simulation = LearnedBellAdjacent();
    simulation.Apply(new UseLoadoutSlot(0, SkyStratum.LandmarkAddress));
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
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly));
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    var death = simulation.Apply(new EndIncarnationAtBell());
    Assert(death.Applied, "Lifecycle fixture must end the first Incarnation at the Bell.");
    return simulation;
}

static ChronicleState ReplayLifecycle()
{
    var simulation = AtBellWithFly();
    ChooseStone(simulation);
    AdvanceTicks(simulation, StudyState.StoneUnderstandingRequired);

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, 1));
    }

    simulation.Apply(new UseLoadoutSlot(0));
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    simulation.Apply(new UseLoadoutSlot(0, ChronicleState.InitialLooseStoneAddress));
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly));
    simulation.Apply(new UseLoadoutSlot(0));

    for (var step = 0; step < 4; step++)
    {
        simulation.Apply(new MoveIncarnation(0, -1));
    }

    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Stone));
    simulation.Apply(new EndIncarnationAtBell());
    simulation.Apply(new CreateReplacementIncarnation());
    simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly));
    simulation.Apply(new UseLoadoutSlot(0));
    return simulation.State;
}

static ChronicleState FoundedHereHome()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    Assert(simulation.Apply(new ChooseHereIntent()).Applied, "The Home fixture must choose HERE.");
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    Assert(
        simulation.Apply(new UseLoadoutSlot(0)).Applied,
        "The Home fixture must establish The First Hearth through intrinsic Found.");
    return simulation.State;
}

static ChronicleSimulation AtBellWithFound()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    Assert(
        simulation.Apply(new ChooseHereIntent()).Applied,
        "The retained-Found Bell fixture must learn Found through HERE.");
    return new ChronicleSimulation(
        simulation.State with { Address = SkyStratum.LandmarkAddress });
}

static ChronicleSimulation AgainstAtRivenCairn()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(41_337));
    simulation.Apply(new ChooseAgainstIntent());
    simulation.Apply(new MoveIncarnation(1, 0));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    simulation.Apply(new MoveIncarnation(0, 1));
    return simulation;
}

static string LiteralVersion3Fixture(int grammarPin) =>
    """
    {
      "Version": 3,
      "Chronicle": {
        "Seed": 41337,
        "Tick": 0,
        "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
        "Speed": 2,
        "Intent": 0,
        "Codex": { "Words": [] },
        "Study": {
          "Understanding": [],
          "ActiveSourceId": null,
          "ActiveWord": null
        },
        "Loadout": {
          "Slot1": { "Verb": null, "Noun": null },
          "Slot2": { "Verb": null, "Noun": null },
          "Slot3": { "Verb": null, "Noun": null },
          "Slot4": { "Verb": null, "Noun": null },
          "Slot5": { "Verb": null, "Noun": null },
          "Slot6": { "Verb": null, "Noun": null },
          "Slot7": { "Verb": null, "Noun": null },
          "Slot8": { "Verb": null, "Noun": null }
        },
        "LooseStoneAddress": { "Stratum": "surface", "X": 1, "Y": 0 },
        "IncarnationId": 1,
        "IncarnationLife": 0,
        "WorldGrammarVersion": __GRAMMAR_PIN__,
        "Home": null
      }
    }
    """.Replace("__GRAMMAR_PIN__", grammarPin.ToString(), StringComparison.Ordinal);

static string LiteralVersion1Fixture(int grammarPin) =>
    """
    {
      "Version": 1,
      "Chronicle": {
        "Seed": 41337,
        "Tick": 0,
        "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
        "Speed": 2,
        "Intent": 0,
        "Codex": { "HasFly": false, "HasStone": false },
        "Study": { "StoneUnderstanding": 0, "IsStudyingBell": false },
        "WorldGrammarVersion": __GRAMMAR_PIN__
      }
    }
    """.Replace("__GRAMMAR_PIN__", grammarPin.ToString(), StringComparison.Ordinal);

static string LiteralPreEnvelopeFixture(int grammarPin) =>
    """
    {
      "Seed": 41337,
      "Tick": 0,
      "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
      "Speed": 2,
      "WorldGrammarVersion": __GRAMMAR_PIN__
    }
    """.Replace("__GRAMMAR_PIN__", grammarPin.ToString(), StringComparison.Ordinal);

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

static ChronicleSimulation LearnedBellAdjacent()
{
    var simulation = AtBellWithFly();
    var source = simulation.CurrentStudySource
        ?? throw new InvalidOperationException("The Fly[Bell] fixture must expose the Bell Study Source.");
    simulation.Apply(new ChooseStudyWord(source.Id, WordIds.Bell));
    AdvanceTicks(simulation, StudyState.UnderstandingRequired);
    Assert(simulation.State.Codex.Contains(WordIds.Bell), "The Fly[Bell] fixture must learn Bell.");
    Assert(
        simulation.Apply(new ConfigureLoadoutSlot(0, WordIds.Fly, WordIds.Bell)).Applied,
        "The Fly[Bell] fixture must equip the authored compatible Expression.");
    simulation.Apply(new MoveIncarnation(0, 1));
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

    ChooseStone(simulation);
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

static ChronicleCommandResult ChooseStone(ChronicleSimulation simulation)
{
    var sourceId = simulation.CurrentStudySource?.Id ?? StudySourceIds.BellSkyStone;
    return simulation.Apply(new ChooseStudyWord(sourceId, WordIds.Stone));
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

static void AssertCurrentSaveRejectsUnexpectedProperty(
    string json,
    Action<System.Text.Json.Nodes.JsonObject> addUnexpectedProperty,
    string message)
{
    var root = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
    addUnexpectedProperty(root);
    AssertThrows<InvalidOperationException>(
        () => ChronicleSaveCodec.Deserialize(root.ToJsonString()),
        message);
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
