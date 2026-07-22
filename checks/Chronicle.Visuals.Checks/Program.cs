using Chronicle.Core;
using Chronicle.VisualPack;
using Chronicle.Visuals;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

VerifyCanonicalPGenBundleAndReaderFailures();
VerifyPGenAndManualPacksComposeTheSameSemantics();
VerifyGoal6AVisualVocabularyAndLayerCoexistence();
VerifyGoal6BVisualVocabularyAndSemanticStates();
VerifyConnectedSurfaceFeaturesUseExplicitCardinalMasks();
VerifyGate3BManualPacksResolveRequiredVisualVocabulary();
VerifyGate3BCompositionCropsAndLayersTheSharedSkySnapshot();
VerifyVisualCompositionAtNumericWorldEdges();
VerifyHomeHearthstoneComposesOverItsSurfaceRidge();
VerifyGoal4CManualPackAndStaticDangerSeam();
VerifyGoal4CCairnSubjectsComposeOverCoreSemantics();
VerifyMovedBellComposesAtItsCoreAddress();
Console.WriteLine(
    "PASS: Goal 6B canonical P-GEN reader, power-state vocabulary, retained combat semantics, deterministic composition, and overlap verified.");

static void VerifyCanonicalPGenBundleAndReaderFailures()
{
    var files = LoadCanonicalFiles();
    var pack = CanonicalVisualPackReader.ReadCanonical(files);
    var fromDirectory = LoadPGenPack();
    var requiredIds = new[]
    {
        "terrain.surface.grass",
        "terrain.surface.soil",
        "terrain.surface.water",
        "terrain.sky.open",
        "feature.surface.grove",
        "feature.surface.grove.v2",
        "feature.surface.grove.v3",
        "feature.surface.ridge",
        "feature.surface.ridge.v2",
        "feature.surface.ridge.v3",
        "feature.surface.ridge-water-crossing",
        "terrain.sky.cloud",
        "landmark.bell-that-fell-up",
        "subject.loose-stone",
        "subject.home-hearthstone",
        "subject.riven-cairn-river-ward",
        "subject.shattered-cairn",
        "actor.incarnation",
        "emphasis.danger.river-ward",
        "emphasis.target.valid",
        "emphasis.selection",
        "glyph.codex",
        "glyph.loadout",
        "glyph.codex.fly",
        "glyph.codex.stone",
    }
    .Concat(Goal6AVisualVocabulary().Select(static expected => expected.VisualId))
    .Concat(Goal6BVisualVocabulary().Select(static expected => expected.VisualId))
    .ToArray();

    Assert(
        pack.Digest ==
            "sha256:e3c5871dabefdb5a61078ad0b556e4304e4066b089d0cf50d36e1acbc96f3e71" &&
        fromDirectory.Digest == pack.Digest &&
        pack.Definitions.Count == 276 &&
        pack.StyleVersion == 2 &&
        pack.ComposerVersion == 2 &&
        requiredIds.All(id => pack.Resolve(id).VisualId == id),
        "The packaged P-GEN artifact must load through both reader Interfaces and resolve the exact current vocabulary.");

    ExpectReaderFailure(
        files.Where(file => file.RelativePath != "manifest.json"),
        "PAL20-FMT-002");
    ExpectReaderFailure(
        files.Append(new CanonicalVisualPackFile("unexpected.bin", new byte[] { 1 })),
        "PAL20-FMT-002");
    ExpectReaderFailure(
        files.Append(files[0]),
        "PAL20-FMT-001");
    ExpectReaderFailure(
        files.Append(new CanonicalVisualPackFile("../escape", Array.Empty<byte>())),
        "PAL20-FMT-006");

    var corruptAtlas = CloneFiles(files);
    corruptAtlas[CanonicalVisualPackReader.AtlasPath][0] ^= 1;
    ExpectReaderFailure(ToPackFiles(corruptAtlas), "PAL20-HASH-001");

    var incompatible = CloneFiles(files);
    var validation = JsonNode.Parse(incompatible["validation.json"])!.AsObject();
    validation["packFormatVersion"] = 2;
    incompatible["validation.json"] = CanonicalJson(validation);
    RewriteHashes(incompatible);
    ExpectReaderFailure(ToPackFiles(incompatible), "PAL20-COMPAT-001");

    var futureReader = CloneFiles(files);
    validation = JsonNode.Parse(futureReader["validation.json"])!.AsObject();
    validation["minimumReaderVersion"] = "2.0.1";
    futureReader["validation.json"] = CanonicalJson(validation);
    RewriteHashes(futureReader);
    ExpectReaderFailure(ToPackFiles(futureReader), "PAL20-COMPAT-002");

    var nonCanonical = CloneFiles(files);
    nonCanonical["manifest.json"] = nonCanonical["manifest.json"]
        .Concat(new byte[] { (byte)' ' })
        .ToArray();
    RewriteHashes(nonCanonical);
    ExpectReaderFailure(ToPackFiles(nonCanonical), "PAL20-FMT-004");

    var duplicateJson = CloneFiles(files);
    var manifestText = Encoding.UTF8.GetString(duplicateJson["manifest.json"]);
    duplicateJson["manifest.json"] = Encoding.UTF8.GetBytes(
        manifestText.Replace(
            "{\"packFormatVersion\":1,",
            "{\"packFormatVersion\":1,\"packFormatVersion\":1,",
            StringComparison.Ordinal));
    ExpectReaderFailure(ToPackFiles(duplicateJson), "PAL20-JSON-002");
}

static void VerifyPGenAndManualPacksComposeTheSameSemantics()
{
    var state = ChronicleState.Begin(41_337) with
    {
        WorldGrammarVersion = 3,
        Combat = null,
    };
    var visible = new WorldRectangle(-6, -6, 13, 13);
    var semantic = WorldArea.Generate(
        state,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(-7, -7, 15, 15));
    var pgen = LoadPGenPack();
    var manual = ManualVisualPack.CreateGate3B(20);
    var incarnation = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0);
    var danger = semantic.Cells.Single(cell =>
        cell.DurableIdentity == FirstConflictSubjects.RivenCairnIdentity).Address;

    VisualRenderPlan Compose(CompiledVisualPack pack) => VisualGrammar.Compose(
        new VisualCompositionInput(
            semantic,
            visible,
            state.Seed,
            pack,
            pack.StyleVersion,
            incarnation,
            [danger],
            [incarnation],
            [danger]));

    var pgenPlan = Compose(pgen);
    var manualPlan = Compose(manual);
    static object Projection(VisualRenderMark mark) => new
    {
        mark.Address,
        mark.VisualId,
        mark.FamilyId,
        mark.VariantOrdinal,
        mark.Layer,
        mark.Anchor,
        mark.OverviewPaletteIndex,
        mark.Column,
        mark.Row,
    };

    Assert(
        pgenPlan.Marks.Select(Projection).SequenceEqual(
            manualPlan.Marks.Select(Projection)),
        "P-GEN and the manual golden pack must compose the same semantic marks, variants, layers, and cells.");
    Assert(
        Compose(pgen).Digest == pgenPlan.Digest &&
        Compose(pgen).Marks.SequenceEqual(pgenPlan.Marks),
        "Repeated P-GEN composition must remain deterministic.");
}

static void VerifyGoal6AVisualVocabularyAndLayerCoexistence()
{
    var initial = Goal6AFixture();
    var initialCombat = initial.Combat ?? throw new InvalidOperationException(
        "Goal 6A visual proof requires the Core-owned combat snapshot.");
    var brute = initialCombat.MireBrute;
    var bruteAddress = brute.Address;
    var basaltAddress = WorldArea.GeneratedBasaltAddress(initial.Seed);
    var visibleBounds = new WorldRectangle(MinX: 0, MinY: 0, Width: 6, Height: 2);
    var semanticBounds = VisualViewportBounds.WithOneCellSemanticHalo(visibleBounds);
    var state = initial with
    {
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, 2, 0),
        Speed = ChronicleSpeed.Paused,
        Combat = initialCombat with
        {
            MireBrute = brute with
            {
                HitPoints = CombatState.MireBruteMaximumHitPoints - 8,
            },
            OngoingBurn = new BurnConsequenceState(
                brute.Identity,
                CombatState.BurnDamage,
                RemainingTicks: 2),
            Scorch = new ScorchedGroundState(bruteAddress, initial.Tick),
        },
    };
    var semantic = WorldArea.Generate(
        state,
        SurfacePatch.SurfaceStratum,
        semanticBounds);
    var semanticBeforeComposition = semantic.Cells.ToArray();
    var bruteCell = semantic.Cells.Single(cell => cell.Address == bruteAddress);
    var basaltCell = semantic.Cells.Single(cell => cell.Address == basaltAddress);

    Assert(
        bruteCell.MireBrute is
        {
            Identity: var bruteIdentity,
            HitPoints: CombatState.MireBruteMaximumHitPoints - 8,
            MaximumHitPoints: CombatState.MireBruteMaximumHitPoints,
            IsLiving: true,
            IsBurning: true,
        } &&
        bruteIdentity == brute.Identity &&
        bruteCell.IsScorched,
        "WorldArea must project the Core-owned wounded, burning Mire Brute and independent scorch delta without inventing presentation state.");
    Assert(
        basaltCell.Target is
        {
            Identity: var basaltIdentity,
            Kind: CombatTargetKind.Basalt,
            DisplayName: "Basalt",
        } &&
        basaltIdentity == WorldArea.GeneratedBasaltIdentity(state.Seed),
        "WorldArea must expose the authored basalt Target as Core-owned target semantics.");

    var actionEmphases = new[]
    {
        new VisualPresentationEmphasis(bruteAddress, VisualPresentationEmphasisKind.PendingAction),
        new VisualPresentationEmphasis(bruteAddress, VisualPresentationEmphasisKind.Preparation),
        new VisualPresentationEmphasis(bruteAddress, VisualPresentationEmphasisKind.Recovery),
    };

    VisualCompositionInput InputFor(CompiledVisualPack pack) => new(
        semantic,
        visibleBounds,
        state.Seed,
        pack,
        pack.StyleVersion,
        state.Address,
        [bruteAddress, basaltAddress],
        [bruteAddress, basaltAddress],
        [bruteAddress],
        actionEmphases);

    var pgen = LoadPGenPack();
    var manual = ManualVisualPack.CreateGate3B(20);
    var pgenInput = InputFor(pgen);
    var pgenPlan = VisualGrammar.Compose(pgenInput);
    var manualPlan = VisualGrammar.Compose(InputFor(manual));

    Assert(
        pgenPlan.Marks.Select(mark => (
                mark.Address,
                mark.VisualId,
                mark.FamilyId,
                mark.VariantOrdinal,
                mark.Layer,
                mark.Anchor,
                mark.OverviewPaletteIndex,
                mark.Column,
                mark.Row))
            .SequenceEqual(manualPlan.Marks.Select(mark => (
                mark.Address,
                mark.VisualId,
                mark.FamilyId,
                mark.VariantOrdinal,
                mark.Layer,
                mark.Anchor,
                mark.OverviewPaletteIndex,
                mark.Column,
                mark.Row))),
        "The P-GEN and manual packs must render the same Goal 6A Core semantics at equivalent layers and cells.");

    var expectedBruteMarks = new (string VisualId, VisualLayerClass Layer)[]
    {
        ("terrain.surface.scorched-ground", VisualLayerClass.GroundField),
        ("subject.mire-brute.living", VisualLayerClass.LandmarkOrSubject),
        ("emphasis.mire-brute.wounded", VisualLayerClass.TemporaryAction),
        ("effect.mire-brute.burning", VisualLayerClass.TemporaryAction),
        ("emphasis.danger.mire-brute", VisualLayerClass.TemporaryAction),
        ("emphasis.target.selected", VisualLayerClass.TargetOrSelection),
        ("emphasis.action.pending", VisualLayerClass.TemporaryAction),
        ("emphasis.action.preparation", VisualLayerClass.TemporaryAction),
        ("emphasis.action.recovery", VisualLayerClass.TemporaryAction),
    };
    Assert(
        expectedBruteMarks.All(expected => pgenPlan.Marks.Any(mark =>
            mark.Address == bruteAddress &&
            mark.VisualId == expected.VisualId &&
            mark.Layer == expected.Layer)),
        "A wounded burning Mire Brute must retain ground, subject, danger, selected Target, and action-state marks together rather than replacing one another.");
    Assert(
        pgenPlan.Marks.Any(mark =>
            mark.Address == basaltAddress &&
            mark.VisualId == "emphasis.target.selected" &&
            mark.Layer == VisualLayerClass.TargetOrSelection),
        "The authored basalt Target must receive the selected-Target emphasis without becoming a collectible Word or subject glyph.");
    Assert(
        pgenPlan.Digest == VisualGrammar.Compose(pgenInput).Digest &&
        pgenPlan.Marks.SequenceEqual(VisualGrammar.Compose(pgenInput).Marks) &&
        pgenPlan.Digest == VisualGrammar.Compose(pgenInput with
        {
            ActionEmphases = actionEmphases.Reverse().ToArray(),
        }).Digest,
        "Goal 6A composition must be deterministic and independent of presentation-emphasis input order.");

    var unhurtPlan = VisualGrammar.Compose(pgenInput with
    {
        SemanticArea = WorldArea.Generate(
            initial,
            SurfacePatch.SurfaceStratum,
            semanticBounds),
        DangerAddresses = [],
        ActionEmphases = [],
    });
    var deadState = state with
    {
        Combat = state.Combat! with
        {
            MireBrute = state.Combat!.MireBrute with
            {
                HitPoints = 0,
                DefeatedTick = state.Tick,
            },
            OngoingBurn = null,
        },
    };
    var deadPlan = VisualGrammar.Compose(pgenInput with
    {
        SemanticArea = WorldArea.Generate(
            deadState,
            SurfacePatch.SurfaceStratum,
            semanticBounds),
        DangerAddresses = [],
        ActionEmphases = [],
    });
    Assert(
        unhurtPlan.Marks.Any(mark =>
            mark.Address == bruteAddress &&
            mark.VisualId == "subject.mire-brute.living") &&
        !unhurtPlan.Marks.Any(mark =>
            mark.Address == bruteAddress &&
            (mark.VisualId == "emphasis.mire-brute.wounded" ||
             mark.VisualId == "effect.mire-brute.burning" ||
             mark.VisualId == "subject.mire-brute.dead")) &&
        deadPlan.Marks.Any(mark =>
            mark.Address == bruteAddress &&
            mark.VisualId == "subject.mire-brute.dead") &&
        !deadPlan.Marks.Any(mark =>
            mark.Address == bruteAddress &&
            (mark.VisualId == "subject.mire-brute.living" ||
             mark.VisualId == "emphasis.mire-brute.wounded" ||
             mark.VisualId == "effect.mire-brute.burning")),
        "Mire Brute living, wounded/burning, and dead visual states must remain mutually legible from the Core snapshot.");

    foreach (var pack in new[] { pgen, manual })
    {
        foreach (var expected in Goal6AVisualVocabulary())
        {
            var definition = pack.Resolve(expected.VisualId);
            Assert(
                definition.LayerClass == expected.Layer &&
                ReadTilePixels(pack, definition).Any(index => pack.Palette[index].Alpha != 0),
                $"Goal 6A visual '{expected.VisualId}' must resolve to its authored layer with visible raster content.");
        }
    }

    Assert(
        semantic.Cells.SequenceEqual(semanticBeforeComposition),
        "Goal 6A visual composition must not mutate the Core-generated area snapshot.");
}

static (string VisualId, VisualLayerClass Layer)[] Goal6AVisualVocabulary() =>
[
    ("terrain.surface.scorched-ground", VisualLayerClass.GroundField),
    ("subject.mire-brute.living", VisualLayerClass.LandmarkOrSubject),
    ("emphasis.mire-brute.wounded", VisualLayerClass.TemporaryAction),
    ("effect.mire-brute.burning", VisualLayerClass.TemporaryAction),
    ("subject.mire-brute.dead", VisualLayerClass.LandmarkOrSubject),
    ("emphasis.target.selected", VisualLayerClass.TargetOrSelection),
    ("emphasis.danger.mire-brute", VisualLayerClass.TemporaryAction),
    ("emphasis.action.pending", VisualLayerClass.TemporaryAction),
    ("emphasis.action.preparation", VisualLayerClass.TemporaryAction),
    ("emphasis.action.recovery", VisualLayerClass.TemporaryAction),
    ("glyph.equipment.iron-cleaver", VisualLayerClass.UiGlyph),
    ("glyph.equipment.quilted-jack", VisualLayerClass.UiGlyph),
    ("glyph.equipment.copper-ward", VisualLayerClass.UiGlyph),
    ("glyph.word.burn", VisualLayerClass.UiGlyph),
    ("glyph.modifier.quickly", VisualLayerClass.UiGlyph),
    ("glyph.modifier.lasting", VisualLayerClass.UiGlyph),
];

static void VerifyGoal6BVisualVocabularyAndSemanticStates()
{
    var pack = LoadPGenPack();
    foreach (var expected in Goal6BVisualVocabulary())
    {
        var definition = pack.Resolve(expected.VisualId);
        Assert(
            definition.LayerClass == expected.Layer &&
            ReadTilePixels(pack, definition).Any(index => pack.Palette[index].Alpha != 0),
            $"Goal 6B visual '{expected.VisualId}' must resolve through the runtime reader with visible authored pixels.");
    }

    var legibilityMinimums = new Dictionary<string, (int Width, int Height)>(StringComparer.Ordinal)
    {
        ["place.singing-seam.embedded"] = (15, 15),
        ["resource.resonant-lode.loose"] = (13, 13),
        ["resource.resonant-lode.carried"] = (15, 11),
        ["source.hearth-resonator.construction"] = (15, 15),
        ["source.hearth-resonator.intact"] = (15, 15),
        ["source.hearth-resonator.damaged"] = (15, 15),
        ["source.hearth-resonator.destroyed"] = (15, 11),
        ["source.hearth-resonator.rebuilding"] = (15, 15),
    };
    foreach (var (visualId, minimum) in legibilityMinimums)
    {
        var occupied = OpaqueBounds(pack, pack.Resolve(visualId));
        Assert(
            occupied.Width >= minimum.Width && occupied.Height >= minimum.Height,
            $"Goal 6B visual '{visualId}' must occupy at least {minimum.Width}x{minimum.Height} native pixels so it cannot disappear into ridge terrain; actual {occupied.Width}x{occupied.Height}.");
    }

    var embedded = ChronicleState.Begin(41_337);
    var primer = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 2);
    var seam = new WorldAddress(SurfacePatch.SurfaceStratum, 8, 3);
    var site = new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3);
    VisualRenderPlan Compose(ChronicleState state, WorldRectangle visible)
    {
        var semanticBounds = VisualViewportBounds.WithOneCellSemanticHalo(visible);
        return VisualGrammar.Compose(new VisualCompositionInput(
            WorldArea.Generate(state, SurfacePatch.SurfaceStratum, semanticBounds),
            visible,
            state.Seed,
            pack,
            pack.StyleVersion,
            state.Address,
            [],
            []));
    }

    var embeddedPlan = Compose(embedded, new WorldRectangle(0, 2, 10, 3));
    Assert(
        embeddedPlan.Marks.Any(mark => mark.Address == primer && mark.VisualId == "glyph.codex") &&
        embeddedPlan.Marks.Any(mark => mark.Address == primer && mark.VisualId == "emphasis.target.selected") &&
        embeddedPlan.Marks.Any(mark => mark.Address == seam && mark.VisualId == "place.singing-seam.embedded") &&
        embeddedPlan.Marks.Any(mark => mark.Address == seam && mark.VisualId == "resource.resonant-lode.embedded") &&
        embeddedPlan.Marks.Any(mark => mark.Address == site && mark.VisualId == "emphasis.home-source-site"),
        "The generated map must simultaneously identify the unread Burn Primer, embedded resource, Singing Seam origin, Home, and eligible site.");

    var readPrimer = new ChronicleSimulation(embedded);
    Assert(readPrimer.Apply(new ChooseHereIntent()).Applied && readPrimer.Apply(new ReadBurnPrimer()).Applied,
        "The visual fixture must read the Burn Primer through the real Core command.");
    var readPrimerPlan = Compose(readPrimer.State, new WorldRectangle(0, 2, 2, 2));
    Assert(
        readPrimerPlan.Marks.Any(mark => mark.Address == primer && mark.VisualId == "glyph.codex") &&
        !readPrimerPlan.Marks.Any(mark => mark.Address == primer && mark.VisualId == "emphasis.target.selected"),
        "The read Burn Primer must remain visible while losing its unread selection brackets.");

    var loose = embedded with
    {
        PowerHome = embedded.PowerHome! with
        {
            ExtractionProgress = 2,
            Lode = embedded.PowerHome.Lode with
            {
                Disposition = ResonantLodeDisposition.Loose,
                Address = seam,
            },
        },
    };
    var loosePlan = Compose(loose, new WorldRectangle(7, 2, 3, 3));
    Assert(
        loosePlan.Marks.Any(mark => mark.VisualId == "place.singing-seam.empty") &&
        loosePlan.Marks.Any(mark => mark.VisualId == "resource.resonant-lode.loose"),
        "Extraction must replace the embedded visual with an empty persistent Seam plus one loose Lode.");

    var carried = loose with
    {
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3),
        PowerHome = loose.PowerHome! with
        {
            Lode = loose.PowerHome.Lode with
            {
                Disposition = ResonantLodeDisposition.Carried,
                Address = null,
                CarrierIncarnationId = loose.IncarnationId,
            },
        },
    };
    var carriedPlan = Compose(carried, new WorldRectangle(6, 2, 3, 3));
    Assert(
        carriedPlan.Marks.Any(mark => mark.Address == carried.Address && mark.VisualId == "actor.incarnation") &&
        carriedPlan.Marks.Any(mark => mark.Address == carried.Address && mark.VisualId == "resource.resonant-lode.carried"),
        "The Lode carrier overlay must remain attached to the same map cell as the Incarnation.");

    ChronicleState WithSource(HearthResonatorPhase phase, int progress, ResonantLodeDisposition disposition) =>
        embedded with
        {
            PowerHome = embedded.PowerHome! with
            {
                ExtractionProgress = 2,
                Lode = embedded.PowerHome.Lode with
                {
                    Disposition = disposition,
                    Address = site,
                    CarrierIncarnationId = null,
                },
                Resonator = new HearthResonatorState(
                    "source.hearth-resonator.41337",
                    site,
                    phase,
                    progress),
            },
        };

    var sourceCases = new[]
    {
        (WithSource(HearthResonatorPhase.UnderConstruction, 1, ResonantLodeDisposition.Committed), "source.hearth-resonator.construction"),
        (WithSource(HearthResonatorPhase.Intact, 3, ResonantLodeDisposition.Installed), "source.hearth-resonator.intact"),
        (WithSource(HearthResonatorPhase.Damaged, 1, ResonantLodeDisposition.Installed), "source.hearth-resonator.damaged"),
        (WithSource(HearthResonatorPhase.Destroyed, 2, ResonantLodeDisposition.Loose), "source.hearth-resonator.destroyed"),
        (WithSource(HearthResonatorPhase.Rebuilding, 1, ResonantLodeDisposition.Committed), "source.hearth-resonator.rebuilding"),
    };
    foreach (var (state, visualId) in sourceCases)
    {
        var broad = Compose(state, new WorldRectangle(0, 2, 4, 3));
        var overlap = Compose(state, new WorldRectangle(1, 3, 1, 1));
        Assert(
            broad.Marks.Any(mark => mark.Address == site && mark.VisualId == visualId) &&
            overlap.Marks.Where(mark => mark.Address == site).Select(mark => mark.VisualId)
                .SequenceEqual(broad.Marks.Where(mark => mark.Address == site).Select(mark => mark.VisualId)),
            $"The map must render '{visualId}' identically in broad and overlapping deterministic requests.");
    }
}

static (string VisualId, VisualLayerClass Layer)[] Goal6BVisualVocabulary() =>
[
    ("emphasis.home-source-site", VisualLayerClass.TargetOrSelection),
    ("place.singing-seam.embedded", VisualLayerClass.LandmarkOrSubject),
    ("place.singing-seam.empty", VisualLayerClass.LandmarkOrSubject),
    ("resource.resonant-lode.embedded", VisualLayerClass.TemporaryAction),
    ("resource.resonant-lode.loose", VisualLayerClass.LandmarkOrSubject),
    ("resource.resonant-lode.carried", VisualLayerClass.Actor),
    ("source.hearth-resonator.construction", VisualLayerClass.LandmarkOrSubject),
    ("source.hearth-resonator.intact", VisualLayerClass.LandmarkOrSubject),
    ("source.hearth-resonator.damaged", VisualLayerClass.LandmarkOrSubject),
    ("source.hearth-resonator.destroyed", VisualLayerClass.LandmarkOrSubject),
    ("source.hearth-resonator.rebuilding", VisualLayerClass.LandmarkOrSubject),
];

static ChronicleState Goal6AFixture() => ChronicleState.Begin(41_337) with
{
    Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
    WorldGrammarVersion = 4,
    Home = null,
    PowerHome = null,
    Attunement = new LoadAttunementState(8, 0),
};

static CompiledVisualPack LoadPGenPack() =>
    CanonicalVisualPackReader.ReadDirectory(Path.Combine(
        AppContext.BaseDirectory,
        "visual-packs",
        "palimpsest20"));

static CanonicalVisualPackFile[] LoadCanonicalFiles()
{
    var root = Path.Combine(
        AppContext.BaseDirectory,
        "visual-packs",
        "palimpsest20");
    return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Select(path => new CanonicalVisualPackFile(
            Path.GetRelativePath(root, path).Replace('\\', '/'),
            File.ReadAllBytes(path)))
        .OrderBy(static file => file.RelativePath, StringComparer.Ordinal)
        .ToArray();
}

static Dictionary<string, byte[]> CloneFiles(
    IEnumerable<CanonicalVisualPackFile> files) =>
    files.ToDictionary(
        static file => file.RelativePath,
        static file => file.Bytes.ToArray(),
        StringComparer.Ordinal);

static IEnumerable<CanonicalVisualPackFile> ToPackFiles(
    IReadOnlyDictionary<string, byte[]> files) =>
    files.Select(static pair => new CanonicalVisualPackFile(pair.Key, pair.Value));

static void ExpectReaderFailure(
    IEnumerable<CanonicalVisualPackFile> files,
    string expectedCode)
{
    try
    {
        _ = CanonicalVisualPackReader.ReadCanonical(files);
        throw new InvalidOperationException(
            $"Canonical reader accepted input expected to fail with {expectedCode}.");
    }
    catch (FormatException exception) when (exception.Message.StartsWith(
               expectedCode,
               StringComparison.Ordinal))
    {
    }
}

static byte[] CanonicalJson(JsonNode node) =>
    Encoding.UTF8.GetBytes(node.ToJsonString(new JsonSerializerOptions
    {
        WriteIndented = false,
    }) + "\n");

static void RewriteHashes(IDictionary<string, byte[]> files)
{
    var hashes = JsonNode.Parse(files["hashes.json"])!.AsObject();
    var entries = hashes["files"]!.AsArray();
    foreach (var entryNode in entries)
    {
        var entry = entryNode!.AsObject();
        var path = entry["path"]!.GetValue<string>();
        entry["digest"] = Sha256(files[path]);
    }

    hashes["aggregateDigest"] = AggregateDigest(entries.Select(entryNode =>
    {
        var entry = entryNode!.AsObject();
        return (
            "file",
            entry["path"]!.GetValue<string>(),
            entry["digest"]!.GetValue<string>());
    }));
    files["hashes.json"] = CanonicalJson(hashes);
}

static string AggregateDigest(
    IEnumerable<(string Kind, string Id, string Digest)> entries)
{
    using var stream = new MemoryStream();
    Span<byte> length = stackalloc byte[sizeof(int)];
    foreach (var entry in entries)
    {
        var bytes = Encoding.UTF8.GetBytes(
            $"chronicle.visual-pack.v1\0{entry.Kind}\0{entry.Id}\0{entry.Digest}");
        BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
        stream.Write(length);
        stream.Write(bytes);
    }

    return Sha256(stream.ToArray());
}

static string Sha256(ReadOnlySpan<byte> bytes) =>
    $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

static void VerifyMovedBellComposesAtItsCoreAddress()
{
    var oldAddress = SkyStratum.LandmarkAddress;
    var movedAddress = new WorldAddress(
        SurfacePatch.SurfaceStratum,
        oldAddress.X,
        oldAddress.Y);
    var state = ChronicleState.Begin(41_337) with
    {
        WorldGrammarVersion = 3,
        Combat = null,
        BellAddress = movedAddress,
    };
    var movedBounds = new WorldRectangle(movedAddress.X, movedAddress.Y, Width: 1, Height: 1);
    var oldBounds = new WorldRectangle(oldAddress.X, oldAddress.Y, Width: 1, Height: 1);
    var movedArea = WorldArea.Generate(
        state,
        movedAddress.Stratum,
        new WorldRectangle(movedAddress.X - 1, movedAddress.Y - 1, Width: 3, Height: 3));
    var oldArea = WorldArea.Generate(
        state,
        oldAddress.Stratum,
        new WorldRectangle(oldAddress.X - 1, oldAddress.Y - 1, Width: 3, Height: 3));

    Assert(
        movedArea.Cells.Single(cell => cell.Address == movedAddress).DurableIdentity ==
            SkyStratum.LandmarkName &&
        oldArea.Cells.Single(cell => cell.Address == oldAddress).DurableIdentity !=
            SkyStratum.LandmarkName,
        "WorldArea must move Bell identity to its Core address and remove it from the old sky address.");

    var pack = ManualVisualPack.CreateGate3B(cellSize: 20);
    VisualRenderPlan Compose(WorldArea area, WorldRectangle bounds) => VisualGrammar.Compose(
        new VisualCompositionInput(
            area,
            bounds,
            state.Seed,
            pack,
            VisualStyleVersion: pack.StyleVersion,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: []));

    var movedPlan = Compose(movedArea, movedBounds);
    var oldPlan = Compose(oldArea, oldBounds);
    Assert(
        movedPlan.Marks.Any(mark =>
            mark.Address == movedAddress &&
            mark.VisualId == "landmark.bell-that-fell-up" &&
            mark.Layer == VisualLayerClass.LandmarkOrSubject) &&
        !oldPlan.Marks.Any(mark => mark.VisualId == "landmark.bell-that-fell-up"),
        "VisualGrammar must render moved Bell at its Core address only.");
}

static void VerifyGoal4CManualPackAndStaticDangerSeam()
{
    var state = ChronicleState.Begin(41_337);
    var semanticBounds = new WorldRectangle(MinX: 18, MinY: 18, Width: 5, Height: 5);
    var visibleBounds = new WorldRectangle(MinX: 19, MinY: 19, Width: 3, Height: 3);
    var dangerAddress = new WorldAddress(SkyStratum.StratumName, 20, 20);
    var semanticArea = WorldArea.Generate(state, SkyStratum.StratumName, semanticBounds);
    var semanticBeforeComposition = semanticArea.Cells.ToArray();

    foreach (var cellSize in new[] { 16, 20 })
    {
        var pack = ManualVisualPack.CreateGate3B(cellSize);
        var rivenCairn = pack.Resolve("subject.riven-cairn-river-ward");
        var shatteredCairn = pack.Resolve("subject.shattered-cairn");
        var danger = pack.Resolve("emphasis.danger.river-ward");

        Assert(
            rivenCairn.LayerClass == VisualLayerClass.LandmarkOrSubject &&
            shatteredCairn.LayerClass == VisualLayerClass.LandmarkOrSubject &&
            danger.LayerClass == VisualLayerClass.TemporaryAction,
            "Goal 4C pack marks must keep both Cairn identities beneath actors and danger as transient emphasis.");
        Assert(
            rivenCairn.AtlasRect != shatteredCairn.AtlasRect &&
            rivenCairn.AtlasRect != danger.AtlasRect &&
            shatteredCairn.AtlasRect != danger.AtlasRect,
            "Goal 4C intact, shattered, and danger marks must use distinct authored atlas tiles.");
        Assert(
            new[] { rivenCairn, shatteredCairn, danger }.All(definition =>
                ReadTilePixels(pack, definition).Any(index => pack.Palette[index].Alpha != 0)),
            "Every Goal 4C manual-pack mark must contain visible pixels at both native sizes.");

        var input = new VisualCompositionInput(
            semanticArea,
            visibleBounds,
            state.Seed,
            pack,
            VisualStyleVersion: pack.StyleVersion,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: [],
            DangerAddresses: [dangerAddress, dangerAddress]);
        var withDanger = VisualGrammar.Compose(input);
        var reorderedDanger = VisualGrammar.Compose(input with { DangerAddresses = [dangerAddress] });
        var withoutDanger = VisualGrammar.Compose(input with { DangerAddresses = [] });

        var dangerMarks = withDanger.Marks
            .Where(mark => mark.VisualId == "emphasis.danger.river-ward")
            .ToArray();
        Assert(
            dangerMarks.Length == 1 &&
            dangerMarks[0].Address == dangerAddress &&
            dangerMarks[0].Layer == VisualLayerClass.TemporaryAction,
            "A Core-derived danger address must compose exactly one transient River-Ward emphasis mark.");
        Assert(
            withDanger.Digest == reorderedDanger.Digest &&
            withDanger.Marks.SequenceEqual(reorderedDanger.Marks),
            "Duplicate or caller-order variation in danger addresses must not change a render plan.");
        Assert(
            withDanger.Marks
                .Where(mark => mark.VisualId != "emphasis.danger.river-ward")
                .SequenceEqual(withoutDanger.Marks),
            "Static danger emphasis must add presentation only; it cannot replace semantic terrain or subjects.");

        var laterTickArea = WorldArea.Generate(
            state with { Tick = state.Tick + 1 },
            SkyStratum.StratumName,
            semanticBounds);
        var laterTickPlan = VisualGrammar.Compose(input with { SemanticArea = laterTickArea });
        Assert(
            semanticArea.Cells.SequenceEqual(laterTickArea.Cells) &&
            withDanger.Digest == laterTickPlan.Digest &&
            withDanger.Marks.SequenceEqual(laterTickPlan.Marks),
            "Without a Core state transition, danger emphasis remains static across Chronicle ticks and has no wall-clock animation path.");
    }

    Assert(
        semanticArea.Cells.SequenceEqual(semanticBeforeComposition),
        "Goal 4C visual composition must not mutate its Core-generated semantic snapshot.");
}

static void VerifyGoal4CCairnSubjectsComposeOverCoreSemantics()
{
    var intactState = ChronicleState.Begin(41_337) with
    {
        WorldGrammarVersion = 3,
        Combat = null,
    };
    var semanticBounds = new WorldRectangle(MinX: -1, MinY: 1, Width: 5, Height: 5);
    var visibleBounds = new WorldRectangle(MinX: 0, MinY: 2, Width: 3, Height: 3);
    var intactArea = WorldArea.Generate(
        intactState,
        SurfacePatch.SurfaceStratum,
        semanticBounds);
    var intactBeforeComposition = intactArea.Cells.ToArray();
    var rivenCairn = intactArea.Cells.Single(cell =>
        string.Equals(
            cell.DurableIdentity,
            FirstConflictSubjects.RivenCairnIdentity,
            StringComparison.Ordinal));
    var grammarTwoCell = WorldArea.Generate(
            intactState with { WorldGrammarVersion = 2 },
            SurfacePatch.SurfaceStratum,
            semanticBounds)
        .Cells.Single(cell => cell.Address == rivenCairn.Address);

    Assert(
        intactState.WorldGrammarVersion == 3 &&
        rivenCairn.Ground == grammarTwoCell.Ground &&
        rivenCairn.Feature == grammarTwoCell.Feature &&
        rivenCairn.MotifIdentity == grammarTwoCell.MotifIdentity,
        "The generated Riven Cairn must preserve its prior ground, feature, and motif semantics.");

    var shatteredState = intactState with
    {
        FirstConflict = new FirstConflictState(
            FirstConflictSubjects.RiverWardSubjectId,
            rivenCairn.Address,
            ThreatenedTick: 3,
            PendingAction: new LoadoutSlot(WordIds.Smash),
            Outcome: FirstConflictOutcome.Shattered,
            ResolvedTick: 4,
            ResolvingIncarnationId: intactState.IncarnationId),
    };
    var shatteredArea = WorldArea.Generate(
        shatteredState,
        SurfacePatch.SurfaceStratum,
        semanticBounds);
    var shatteredBeforeComposition = shatteredArea.Cells.ToArray();
    var shatteredCairn = shatteredArea.Cells.Single(cell => cell.Address == rivenCairn.Address);

    Assert(
        string.Equals(
            shatteredCairn.DurableIdentity,
            FirstConflictSubjects.ShatteredCairnIdentity,
            StringComparison.Ordinal) &&
        shatteredCairn.Ground == rivenCairn.Ground &&
        shatteredCairn.Feature == rivenCairn.Feature &&
        shatteredCairn.MotifIdentity == rivenCairn.MotifIdentity,
        "The Core-resolved Shattered Cairn must replace only the Cairn identity over unchanged material semantics.");

    var overlapBounds = new WorldRectangle(
        rivenCairn.Address.X,
        rivenCairn.Address.Y,
        Width: 1,
        Height: 1);

    foreach (var cellSize in new[] { 16, 20 })
    {
        var pack = ManualVisualPack.CreateGate3B(cellSize);
        var intactInput = new VisualCompositionInput(
            intactArea,
            visibleBounds,
            intactState.Seed,
            pack,
            VisualStyleVersion: pack.StyleVersion,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: [],
            DangerAddresses: [rivenCairn.Address]);
        var intactPlan = VisualGrammar.Compose(intactInput);
        var intactRepeat = VisualGrammar.Compose(intactInput);
        var intactOverlap = VisualGrammar.Compose(intactInput with { VisibleBounds = overlapBounds });
        var shatteredInput = intactInput with
        {
            SemanticArea = shatteredArea,
            DangerAddresses = [],
        };
        var shatteredPlan = VisualGrammar.Compose(shatteredInput);
        var shatteredRepeat = VisualGrammar.Compose(shatteredInput);
        var shatteredOverlap = VisualGrammar.Compose(
            shatteredInput with { VisibleBounds = overlapBounds });

        Assert(
            intactPlan.Marks.Any(mark =>
                mark.Address == rivenCairn.Address &&
                mark.VisualId == "subject.riven-cairn-river-ward" &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject) &&
            intactPlan.Marks.Any(mark =>
                mark.Address == rivenCairn.Address &&
                mark.VisualId == "emphasis.danger.river-ward" &&
                mark.Layer == VisualLayerClass.TemporaryAction),
            "An intact Core Riven Cairn must compose its authored River-Ward subject and its Core-fed danger emphasis.");
        Assert(
            shatteredPlan.Marks.Any(mark =>
                mark.Address == rivenCairn.Address &&
                mark.VisualId == "subject.shattered-cairn" &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject) &&
            !shatteredPlan.Marks.Any(mark =>
                mark.Address == rivenCairn.Address &&
                (mark.VisualId == "subject.riven-cairn-river-ward" ||
                 mark.VisualId == "emphasis.danger.river-ward")),
            "A resolved Core Shattered Cairn must replace the intact subject mark and remove transient danger emphasis.");

        var intactFeature = intactPlan.Marks.Single(mark =>
            mark.Address == rivenCairn.Address &&
            mark.Layer == VisualLayerClass.EnvironmentalFeature);
        var shatteredFeature = shatteredPlan.Marks.Single(mark =>
            mark.Address == rivenCairn.Address &&
            mark.Layer == VisualLayerClass.EnvironmentalFeature);
        Assert(
            intactPlan.Marks.Any(mark =>
                mark.Address == rivenCairn.Address &&
                mark.Layer == VisualLayerClass.GroundField) &&
            intactFeature.VisualId == shatteredFeature.VisualId &&
            intactFeature.AtlasRect == shatteredFeature.AtlasRect,
            "Cairn subject visuals must layer over, never replace, the stable underlying terrain and ridge mark.");
        Assert(
            intactPlan.Digest == intactRepeat.Digest &&
            intactPlan.Marks.SequenceEqual(intactRepeat.Marks) &&
            shatteredPlan.Digest == shatteredRepeat.Digest &&
            shatteredPlan.Marks.SequenceEqual(shatteredRepeat.Marks),
            "Intact and shattered Cairn composition must produce stable deterministic render plans.");
        Assert(
            VisualIdsAt(intactPlan, rivenCairn.Address)
                .SequenceEqual(VisualIdsAt(intactOverlap, rivenCairn.Address)) &&
            VisualIdsAt(shatteredPlan, rivenCairn.Address)
                .SequenceEqual(VisualIdsAt(shatteredOverlap, rivenCairn.Address)),
            "Overlapping visible queries must preserve every intact and shattered Cairn mark at its absolute address.");
    }

    Assert(
        intactArea.Cells.SequenceEqual(intactBeforeComposition) &&
        shatteredArea.Cells.SequenceEqual(shatteredBeforeComposition),
        "Cairn composition must not mutate either Core-generated semantic area.");
}

static void VerifyVisualCompositionAtNumericWorldEdges()
{
    var state = ChronicleState.Begin(41_337);
    var pack = ManualVisualPack.CreateGate3B(cellSize: 20);
    var cases = new[]
    {
        (
            Name: "minimum",
            SemanticBounds: new WorldRectangle(long.MinValue, long.MinValue, Width: 2, Height: 2),
            VisibleBounds: new WorldRectangle(long.MinValue, long.MinValue, Width: 1, Height: 1)),
        (
            Name: "maximum",
            SemanticBounds: new WorldRectangle(long.MaxValue - 1, long.MaxValue - 1, Width: 2, Height: 2),
            VisibleBounds: new WorldRectangle(long.MaxValue, long.MaxValue, Width: 1, Height: 1)),
    };

    foreach (var edge in cases)
    {
        Assert(
            VisualViewportBounds.Centered(
                edge.VisibleBounds.MinX,
                edge.VisibleBounds.MinY,
                width: 1,
                height: 1) == edge.VisibleBounds &&
            VisualViewportBounds.WithOneCellSemanticHalo(edge.VisibleBounds) ==
            edge.SemanticBounds,
            $"The {edge.Name} numeric edge must clamp its viewport and semantic halo without inventing a World edge.");

        var semanticArea = WorldArea.Generate(state, SkyStratum.StratumName, edge.SemanticBounds);
        var visibleAddress = new WorldAddress(
            SkyStratum.StratumName,
            edge.VisibleBounds.MinX,
            edge.VisibleBounds.MinY);
        var plan = VisualGrammar.Compose(new VisualCompositionInput(
            semanticArea,
            edge.VisibleBounds,
            state.Seed,
            pack,
            VisualStyleVersion: pack.StyleVersion,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: []));

        Assert(
            semanticArea.Cells.Count == 4 &&
            semanticArea.Cells.All(cell =>
                cell.Address.X >= edge.SemanticBounds.MinX &&
                cell.Address.X <= edge.VisibleBounds.MinX + (edge.Name == "minimum" ? 1 : 0) &&
                cell.Address.Y >= edge.SemanticBounds.MinY &&
                cell.Address.Y <= edge.VisibleBounds.MinY + (edge.Name == "minimum" ? 1 : 0)),
            $"The {edge.Name} numeric edge must use the largest representable one-cell semantic context without wrapping addresses.");
        Assert(
            plan.Marks.Count > 0 &&
            plan.Marks.All(mark =>
                mark.Address == visibleAddress &&
                mark.Column == 0 &&
                mark.Row == 0),
            $"The {edge.Name} numeric edge must compose only the visible cell at column/row zero.");
    }

    Assert(
        VisualViewportBounds.OffsetClamped(long.MinValue, -1) == long.MinValue &&
        VisualViewportBounds.OffsetClamped(long.MaxValue, 1) == long.MaxValue,
        "Visual panning must stop at the numeric storage limit without wrapping.");
}

static void VerifyHomeHearthstoneComposesOverItsSurfaceRidge()
{
    var homeAddress = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3);
    var looseStoneAddress = ChronicleState.InitialLooseStoneAddress;
    var visibleBounds = new WorldRectangle(MinX: 0, MinY: 0, Width: 2, Height: 4);
    var semanticHaloBounds = new WorldRectangle(MinX: -1, MinY: -1, Width: 4, Height: 6);
    var state = ChronicleState.Begin(41_337) with
    {
        WorldGrammarVersion = 3,
        Combat = null,
        Address = homeAddress,
        Home = new HomeState(
            "holding.home",
            "The First Hearth",
            homeAddress,
            FoundedTick: 0,
            FoundingIncarnationId: 1,
            HomeMaterialState.HearthstoneRaised),
    };
    var stateBeforeComposition = state;
    var semanticArea = WorldArea.Generate(
        state,
        SurfacePatch.SurfaceStratum,
        semanticHaloBounds);
    var homeCell = semanticArea.Cells.Single(cell => cell.Address == homeAddress);

    Assert(
        homeCell.Feature == WorldFeature.Stone &&
        homeCell.DurableIdentity == "The First Hearthstone",
        "The confirmed Home fixture must retain its generated Stone ridge and add only The First Hearthstone identity.");

    foreach (var cellSize in new[] { 16, 20 })
    {
        var pack = ManualVisualPack.CreateGate3B(cellSize);
        var input = new VisualCompositionInput(
            semanticArea,
            visibleBounds,
            state.Seed,
            pack,
            VisualStyleVersion: pack.StyleVersion,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: []);
        var first = VisualGrammar.Compose(input);
        var second = VisualGrammar.Compose(input);

        Assert(
            first.Marks.Any(mark =>
                mark.Address == homeAddress &&
                mark.FamilyId == "feature.surface.ridge" &&
                mark.Layer == VisualLayerClass.EnvironmentalFeature),
            "The Hearthstone cell must retain its ridge environmental-feature mark.");
        Assert(
            first.Marks.Any(mark =>
                mark.Address == looseStoneAddress &&
                mark.VisualId == "subject.loose-stone" &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject),
            "The separate loose Stone must retain its subject.loose-stone mapping.");
        Assert(
            first.Digest == second.Digest &&
            first.Marks.SequenceEqual(second.Marks) &&
            state == stateBeforeComposition,
            "Hearthstone composition must be deterministic and read-only.");

        var hearthstone = pack.Resolve("subject.home-hearthstone");
        Assert(
            hearthstone.VisualId == "subject.home-hearthstone" &&
            hearthstone.LayerClass == VisualLayerClass.LandmarkOrSubject,
            "Each compiled pack must provide subject.home-hearthstone at the LandmarkOrSubject layer.");
        Assert(
            first.Marks.Any(mark =>
                mark.Address == homeAddress &&
                mark.VisualId == "subject.home-hearthstone" &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject),
            "The Hearthstone must compose as subject.home-hearthstone over its existing ridge.");
    }
}

static void VerifyGate3BManualPacksResolveRequiredVisualVocabulary()
{
    const int expectedFormatVersion = 1;
    const int expectedStyleVersion = 2;
    const int expectedComposerVersion = 2;
    var requiredVisualIds = new[]
    {
        "terrain.surface.grass",
        "terrain.surface.soil",
        "terrain.surface.water",
        "terrain.sky.open",
        "feature.surface.grove",
        "feature.surface.ridge",
        "feature.surface.ridge-water-crossing",
        "terrain.sky.cloud",
        "landmark.bell-that-fell-up",
        "subject.loose-stone",
        "subject.riven-cairn-river-ward",
        "subject.shattered-cairn",
        "actor.incarnation",
        "emphasis.danger.river-ward",
        "emphasis.target.valid",
        "emphasis.selection",
        "glyph.codex",
        "glyph.loadout",
        "glyph.codex.fly",
        "glyph.codex.stone",
    }
    .Concat(Goal6AVisualVocabulary().Select(static expected => expected.VisualId))
    .ToArray();

    foreach (var cellSize in new[] { 16, 20 })
    {
        var pack = ManualVisualPack.CreateGate3B(cellSize);

        Assert(pack.PackId == "chronicle.gate3b.manual", "The manual Gate 3B pack needs one stable identity.");
        Assert(pack.FormatVersion == expectedFormatVersion, "The Gate 3B pack format must be version 1.");
        Assert(pack.StyleVersion == expectedStyleVersion, "The Gate 3B visual style must be version 2.");
        Assert(pack.ComposerVersion == expectedComposerVersion, "The Gate 3B composer contract must be version 2.");
        Assert(pack.CellSize == cellSize, "The pack must retain the requested native cell size.");
        Assert(pack.AtlasWidth > 0 && pack.AtlasHeight > 0, "The pack must declare a non-empty atlas.");
        Assert(
            pack.AtlasIndices.Count == pack.AtlasWidth * pack.AtlasHeight,
            "The indexed atlas must contain exactly one palette index per atlas pixel.");
        Assert(pack.Palette.Count is > 0 and <= 256, "The pack palette must use between one and 256 entries.");
        Assert(
            pack.AtlasIndices.All(index => index >= 0 && index < pack.Palette.Count),
            "Every atlas pixel index must resolve through the declared palette.");
        Assert(
            pack.Definitions.Select(definition => definition.VisualId).Distinct(StringComparer.Ordinal).Count() == pack.Definitions.Count,
            "Stable visual identifiers must be unique within a compiled pack.");
        Assert(!string.IsNullOrWhiteSpace(pack.Digest), "The compiled pack must expose a deterministic digest.");

        foreach (var visualId in requiredVisualIds)
        {
            var definition = pack.Resolve(visualId);
            Assert(definition.VisualId == visualId, $"The compiled pack must resolve '{visualId}' by stable identifier.");
            Assert(
                definition.AtlasRect.X >= 0 &&
                definition.AtlasRect.Y >= 0 &&
                definition.AtlasRect.Width > 0 &&
                definition.AtlasRect.Height > 0 &&
                definition.AtlasRect.X + definition.AtlasRect.Width <= pack.AtlasWidth &&
                definition.AtlasRect.Y + definition.AtlasRect.Height <= pack.AtlasHeight,
                $"The resolved '{visualId}' atlas rectangle must fit completely inside the atlas.");
        }

        foreach (var expected in Goal6AVisualVocabulary())
        {
            var definition = pack.Resolve(expected.VisualId);
            Assert(
                definition.LayerClass == expected.Layer &&
                ReadTilePixels(pack, definition).Any(index => pack.Palette[index].Alpha != 0),
                $"The Goal 6A manual visual '{expected.VisualId}' must retain its authored layer and visible raster.");
        }
    }
}

static void VerifyGate3BCompositionCropsAndLayersTheSharedSkySnapshot()
{
    var skyOrigin = new WorldAddress(SkyStratum.StratumName, 0, 0);
    var looseStoneAddress = new WorldAddress(SkyStratum.StratumName, 1, 0);
    var bellAddress = SkyStratum.LandmarkAddress;
    var semanticHaloBounds = new WorldRectangle(MinX: -2, MinY: -6, Width: 5, Height: 8);
    var visibleBounds = new WorldRectangle(MinX: -1, MinY: -5, Width: 3, Height: 6);
    var state = ChronicleState.Begin(41_337) with
    {
        Address = skyOrigin,
        LooseStoneAddress = looseStoneAddress,
    };
    var semanticArea = WorldArea.Generate(state, SkyStratum.StratumName, semanticHaloBounds);
    var pack = ManualVisualPack.CreateGate3B(cellSize: 20);
    var input = new VisualCompositionInput(
        semanticArea,
        visibleBounds,
        state.Seed,
        pack,
        VisualStyleVersion: pack.StyleVersion,
        IncarnationAddress: skyOrigin,
        TargetAddresses: [looseStoneAddress],
        SelectedAddresses: [looseStoneAddress]);

    var first = VisualGrammar.Compose(input);
    var second = VisualGrammar.Compose(input);
    var visibleCells = semanticArea.Cells.Where(cell => IsWithin(cell.Address, visibleBounds)).ToArray();
    var visibleAddresses = visibleCells.Select(cell => cell.Address).ToHashSet();

    Assert(first.PackId == pack.PackId, "A render plan must identify the compiled pack it consumed.");
    Assert(first.PackDigest == pack.Digest, "A render plan must identify the exact compiled-pack digest it consumed.");
    Assert(first.CellSize == 20, "A render plan must retain its accepted native cell size.");
    Assert(first.Bounds == visibleBounds, "A render plan must crop exactly to its visible bounds.");
    Assert(
        first.Marks.All(mark => visibleAddresses.Contains(mark.Address)),
        "A render plan must not emit marks for semantic-halo-only addresses.");
    Assert(
        visibleCells.All(cell => first.Marks.Any(mark =>
            mark.Address == cell.Address && mark.Layer == VisualLayerClass.GroundField)),
        "Every visible semantic cell must contribute a ground mark.");

    Assert(
        first.Marks.Any(mark => mark.Address == bellAddress &&
            mark.VisualId == "landmark.bell-that-fell-up" &&
            mark.Layer == VisualLayerClass.LandmarkOrSubject),
        "The Bell must compose as its durable Landmark visual.");
    Assert(
        first.Marks.Any(mark => mark.Address == looseStoneAddress &&
            mark.VisualId == "subject.loose-stone" &&
            mark.Layer == VisualLayerClass.LandmarkOrSubject),
        "The moved loose Stone must compose as its durable-subject visual.");
    Assert(
        first.Marks.Any(mark => mark.Address == skyOrigin &&
            mark.VisualId == "actor.incarnation" &&
            mark.Layer == VisualLayerClass.Actor),
        "The Incarnation must compose above the semantic sky cell at its Core address.");
    Assert(
        first.Marks.Any(mark => mark.Address == looseStoneAddress &&
            mark.VisualId == "emphasis.target.valid" &&
            mark.Layer == VisualLayerClass.TemporaryAction),
        "A Core-valid target must compose as temporary target emphasis.");
    Assert(
        first.Marks.Any(mark => mark.Address == looseStoneAddress &&
            mark.VisualId == "emphasis.selection" &&
            mark.Layer == VisualLayerClass.TargetOrSelection),
        "The selected target must compose as the top selection mark.");

    foreach (var mark in first.Marks)
    {
        var definition = pack.Resolve(mark.VisualId);
        Assert(mark.FamilyId == definition.FamilyId, "Every plan mark must retain its pack-resolved family identity.");
        Assert(mark.VariantOrdinal == definition.VariantOrdinal, "Every plan mark must use a declared pack variant.");
        Assert(mark.AtlasRect == definition.AtlasRect, "Every plan mark must retain its resolved atlas rectangle.");
        Assert(mark.Anchor == definition.Anchor, "Every plan mark must retain its resolved integer anchor.");
        Assert(
            mark.OverviewPaletteIndex == definition.OverviewPaletteIndex,
            "Every plan mark must retain its resolved overview palette index.");
        Assert(
            mark.Column == checked((int)(mark.Address.X - visibleBounds.MinX)) &&
            mark.Row == checked((int)(mark.Address.Y - visibleBounds.MinY)),
            "Every plan mark must place its absolute address at the visible-bounds-relative cell.");
    }

    Assert(
        first.Marks.Zip(first.Marks.Skip(1)).All(pair =>
            Comparer<(VisualLayerClass Layer, int Row, int Column, string VisualId)>.Default.Compare(
                (pair.First.Layer, pair.First.Row, pair.First.Column, pair.First.VisualId),
                (pair.Second.Layer, pair.Second.Row, pair.Second.Column, pair.Second.VisualId)) <= 0),
        "Render-plan marks must use deterministic layer, row, column, and visual-identifier order.");
    Assert(first.Digest == second.Digest, "Repeated composition of one input must return the same plan digest.");
    Assert(
        first.Marks.Select(mark => (
                mark.Address,
                mark.VisualId,
                mark.FamilyId,
                mark.VariantOrdinal,
                mark.Layer,
                mark.AtlasRect,
                mark.Anchor,
                mark.OverviewPaletteIndex,
                mark.Column,
                mark.Row))
            .SequenceEqual(second.Marks.Select(mark => (
                mark.Address,
                mark.VisualId,
                mark.FamilyId,
                mark.VariantOrdinal,
                mark.Layer,
                mark.AtlasRect,
                mark.Anchor,
                mark.OverviewPaletteIndex,
                mark.Column,
                mark.Row))),
        "Repeated composition of one input must return the same canonical mark projection.");
}

static void VerifyConnectedSurfaceFeaturesUseExplicitCardinalMasks()
{
    foreach (var cellSize in new[] { 16, 20 })
    {
        var pack = ManualVisualPack.CreateGate3B(cellSize);
        var expectedMasks = Enumerable.Range(0, 16)
            .Select(rawMask => (Raw: rawMask, Mask: (CardinalAdjacencyMask)rawMask))
            .ToArray();
        var groveDefinitions = expectedMasks
            .SelectMany(expected => Enumerable.Range(0, 2).Select(variant => (
                expected.Mask,
                Variant: variant,
                Definition: pack.Resolve(
                    MaskedFeatureId("feature.surface.grove", expected.Raw, variant)))))
            .ToArray();
        var ridgeDefinitions = expectedMasks
            .SelectMany(expected => Enumerable.Range(0, 2).Select(variant => (
                expected.Mask,
                Variant: variant,
                Definition: pack.Resolve(
                    MaskedFeatureId("feature.surface.ridge", expected.Raw, variant)))))
            .ToArray();
        var crossingDefinitions = expectedMasks
            .SelectMany(expected => Enumerable.Range(0, 4).Select(variant => (
                expected.Mask,
                Variant: variant,
                Definition: pack.Resolve(
                    MaskedFeatureId(
                        "feature.surface.ridge-water-crossing",
                        expected.Raw,
                        variant)))))
            .ToArray();

        foreach (var expected in groveDefinitions
                     .Concat(ridgeDefinitions)
                     .Concat(crossingDefinitions))
        {
            Assert(
                expected.Definition.AdjacencyMask == expected.Mask,
                $"Connected Surface definition '{expected.Definition.VisualId}' must declare its exact cardinal mask.");
            Assert(
                expected.Definition.LayerClass == VisualLayerClass.EnvironmentalFeature,
                $"Connected Surface definition '{expected.Definition.VisualId}' must remain an environmental feature.");
            Assert(
                expected.Definition.VariantOrdinal == expected.Variant,
                $"Connected Surface definition '{expected.Definition.VisualId}' must declare variant {expected.Variant}.");
        }

        foreach (var crossing in crossingDefinitions)
        {
            var dryRidge = pack.Resolve(
                MaskedFeatureId(
                    "feature.surface.ridge",
                    (int)crossing.Mask,
                    crossing.Variant % 2));
            foreach (var direction in new[]
                     {
                         CardinalAdjacencyMask.North,
                         CardinalAdjacencyMask.East,
                         CardinalAdjacencyMask.South,
                         CardinalAdjacencyMask.West,
                     })
            {
                var crossingEdge = ReadEdgePixels(pack, crossing.Definition, direction);
                if (crossing.Mask.HasFlag(direction))
                {
                    Assert(
                        crossingEdge.SequenceEqual(ReadEdgePixels(pack, dryRidge, direction)),
                        $"Water-crossing '{crossing.Definition.VisualId}' must exactly meet the dry ridge on its {direction} edge.");
                }
                else
                {
                    Assert(
                        crossingEdge.All(index => pack.Palette[index].Alpha == 0),
                        $"Water-crossing '{crossing.Definition.VisualId}' must leave its disconnected {direction} edge transparent.");
                }
            }
        }
    }

    var state = ChronicleState.Begin(41_337) with
    {
        WorldGrammarVersion = 3,
        Combat = null,
    };
    var semanticHaloBounds = new WorldRectangle(MinX: -17, MinY: -12, Width: 35, Height: 25);
    var visibleBounds = new WorldRectangle(MinX: -16, MinY: -11, Width: 33, Height: 23);
    var overlapBounds = new WorldRectangle(MinX: -4, MinY: -4, Width: 8, Height: 8);
    var semanticArea = WorldArea.Generate(state, SurfacePatch.SurfaceStratum, semanticHaloBounds);
    var cellsByAddress = semanticArea.Cells.ToDictionary(cell => cell.Address);
    var visibleFeatures = semanticArea.Cells
        .Where(cell =>
            IsWithin(cell.Address, visibleBounds) &&
            cell.DurableIdentity is null &&
            cell.Feature is WorldFeature.Vegetation or WorldFeature.Stone)
        .ToArray();

    Assert(
        visibleFeatures.Any(cell => cell.Feature == WorldFeature.Vegetation) &&
        visibleFeatures.Any(cell => cell.Feature == WorldFeature.Stone),
        "The fixed seed-41337 Surface crop must contain both vegetation and stone semantics.");
    Assert(
        visibleFeatures.Any(cell =>
            cell.Ground == WorldGround.Water &&
            cell.Feature == WorldFeature.Stone),
        "The fixed seed-41337 Surface crop must contain a ridge/water interaction.");

    foreach (var cellSize in new[] { 16, 20 })
    {
        var pack = ManualVisualPack.CreateGate3B(cellSize);
        var full = VisualGrammar.Compose(new VisualCompositionInput(
            semanticArea,
            visibleBounds,
            state.Seed,
            pack,
            VisualStyleVersion: pack.StyleVersion,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: []));
        var overlap = VisualGrammar.Compose(new VisualCompositionInput(
            semanticArea,
            overlapBounds,
            state.Seed,
            pack,
            VisualStyleVersion: pack.StyleVersion,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: []));

        foreach (var cell in visibleFeatures)
        {
            var familyId = (cell.Ground, cell.Feature) switch
            {
                (_, WorldFeature.Vegetation) => "feature.surface.grove",
                (WorldGround.Water, WorldFeature.Stone) =>
                    "feature.surface.ridge-water-crossing",
                _ => "feature.surface.ridge",
            };
            var expectedMask = ExpectedFeatureMask(cell, cellsByAddress);
            var featureMarks = full.Marks
                .Where(mark =>
                    mark.Address == cell.Address &&
                    mark.Layer == VisualLayerClass.EnvironmentalFeature)
                .ToArray();

            Assert(
                featureMarks.Length == 1,
                $"Surface {cell.Feature} at {cell.Address} must use exactly one connected feature visual.");
            var expectedVariantCount = familyId == "feature.surface.ridge-water-crossing"
                ? 4
                : 2;
            Assert(
                featureMarks[0].VariantOrdinal >= 0 &&
                featureMarks[0].VariantOrdinal < expectedVariantCount &&
                featureMarks[0].VisualId ==
                MaskedFeatureId(familyId, (int)expectedMask, featureMarks[0].VariantOrdinal),
                $"Surface {cell.Feature} at {cell.Address} must combine its exact cardinal mask with a declared cosmetic variant.");
        }

        var fullFeatureMarks = full.Marks
            .Where(mark => mark.Layer == VisualLayerClass.EnvironmentalFeature)
            .ToDictionary(mark => mark.Address);
        var looseStoneCell = cellsByAddress[ChronicleState.InitialLooseStoneAddress];
        Assert(
            looseStoneCell.Feature == WorldFeature.Stone,
            "The fixed Surface fixture must place the durable loose Stone inside the semantic ridge.");
        var looseStoneFamily = looseStoneCell.Ground == WorldGround.Water
            ? "feature.surface.ridge-water-crossing"
            : "feature.surface.ridge";
        var looseStoneMask = ExpectedFeatureMask(looseStoneCell, cellsByAddress);
        Assert(
            fullFeatureMarks.TryGetValue(looseStoneCell.Address, out var looseStoneFeature) &&
            looseStoneFeature.FamilyId == looseStoneFamily &&
            looseStoneFeature.VisualId ==
            MaskedFeatureId(
                looseStoneFamily,
                (int)looseStoneMask,
                looseStoneFeature.VariantOrdinal) &&
            full.Marks.Any(mark =>
                mark.Address == looseStoneCell.Address &&
                mark.VisualId == "subject.loose-stone" &&
                mark.Layer == VisualLayerClass.LandmarkOrSubject),
            "A durable loose Stone must layer over, not remove, its connected environmental feature.");
        foreach (var expectedFamily in new[]
                 {
                     (Id: "feature.surface.grove", VariantCount: 2),
                     (Id: "feature.surface.ridge", VariantCount: 2),
                     (Id: "feature.surface.ridge-water-crossing", VariantCount: 4),
                 })
        {
            Assert(
                fullFeatureMarks.Values
                    .Where(mark => mark.FamilyId == expectedFamily.Id)
                    .Select(mark => mark.VariantOrdinal)
                    .Distinct()
                    .Count() == expectedFamily.VariantCount,
                $"The fixed Surface fixture must exercise all {expectedFamily.VariantCount} address-stable variants for '{expectedFamily.Id}'.");
        }
        foreach (var overlapMark in overlap.Marks.Where(mark =>
                     mark.Layer == VisualLayerClass.EnvironmentalFeature))
        {
            Assert(
                fullFeatureMarks.TryGetValue(overlapMark.Address, out var fullMark) &&
                fullMark.VisualId == overlapMark.VisualId &&
                fullMark.AtlasRect == overlapMark.AtlasRect,
                $"Overlapping composition must preserve the connected feature visual at {overlapMark.Address}.");
        }
    }
}

static string MaskedFeatureId(string familyId, int rawMask, int variant = 0)
{
    var masked = rawMask == 0 ? familyId : $"{familyId}.mask.{rawMask:00}";
    return variant == 0 ? masked : $"{masked}.v{variant}";
}

static CardinalAdjacencyMask ExpectedFeatureMask(
    WorldCell cell,
    IReadOnlyDictionary<WorldAddress, WorldCell> cells)
{
    var mask = CardinalAdjacencyMask.None;
    Include(CardinalAdjacencyMask.North, 0, -1);
    Include(CardinalAdjacencyMask.East, 1, 0);
    Include(CardinalAdjacencyMask.South, 0, 1);
    Include(CardinalAdjacencyMask.West, -1, 0);
    return mask;

    void Include(CardinalAdjacencyMask direction, int deltaX, int deltaY)
    {
        var neighborAddress = new WorldAddress(
            cell.Address.Stratum,
            checked(cell.Address.X + deltaX),
            checked(cell.Address.Y + deltaY));
        if (cells.TryGetValue(neighborAddress, out var neighbor) &&
            neighbor.Feature == cell.Feature)
        {
            mask |= direction;
        }
    }
}

static IReadOnlyList<byte> ReadEdgePixels(
    CompiledVisualPack pack,
    VisualDefinition definition,
    CardinalAdjacencyMask direction)
{
    var rect = definition.AtlasRect;
    return direction switch
    {
        CardinalAdjacencyMask.North => Enumerable.Range(0, rect.Width)
            .Select(offset => pack.AtlasIndices[rect.Y * pack.AtlasWidth + rect.X + offset])
            .ToArray(),
        CardinalAdjacencyMask.East => Enumerable.Range(0, rect.Height)
            .Select(offset =>
                pack.AtlasIndices[
                    (rect.Y + offset) * pack.AtlasWidth + rect.X + rect.Width - 1])
            .ToArray(),
        CardinalAdjacencyMask.South => Enumerable.Range(0, rect.Width)
            .Select(offset =>
                pack.AtlasIndices[
                    (rect.Y + rect.Height - 1) * pack.AtlasWidth + rect.X + offset])
            .ToArray(),
        CardinalAdjacencyMask.West => Enumerable.Range(0, rect.Height)
            .Select(offset => pack.AtlasIndices[(rect.Y + offset) * pack.AtlasWidth + rect.X])
            .ToArray(),
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };
}

static IReadOnlyList<byte> ReadTilePixels(
    CompiledVisualPack pack,
    VisualDefinition definition) =>
    Enumerable.Range(0, definition.AtlasRect.Height)
        .SelectMany(y => Enumerable.Range(0, definition.AtlasRect.Width)
            .Select(x => pack.AtlasIndices[
                (definition.AtlasRect.Y + y) * pack.AtlasWidth + definition.AtlasRect.X + x]))
        .ToArray();

static (int Width, int Height) OpaqueBounds(
    CompiledVisualPack pack,
    VisualDefinition definition)
{
    var minimumX = definition.AtlasRect.Width;
    var minimumY = definition.AtlasRect.Height;
    var maximumX = -1;
    var maximumY = -1;
    for (var y = 0; y < definition.AtlasRect.Height; y++)
    {
        for (var x = 0; x < definition.AtlasRect.Width; x++)
        {
            var paletteIndex = pack.AtlasIndices[
                (definition.AtlasRect.Y + y) * pack.AtlasWidth + definition.AtlasRect.X + x];
            if (pack.Palette[paletteIndex].Alpha == 0)
            {
                continue;
            }

            minimumX = Math.Min(minimumX, x);
            minimumY = Math.Min(minimumY, y);
            maximumX = Math.Max(maximumX, x);
            maximumY = Math.Max(maximumY, y);
        }
    }

    return maximumX < minimumX || maximumY < minimumY
        ? (0, 0)
        : (maximumX - minimumX + 1, maximumY - minimumY + 1);
}

static IReadOnlyList<string> VisualIdsAt(
    VisualRenderPlan plan,
    WorldAddress address) =>
    plan.Marks
        .Where(mark => mark.Address == address)
        .Select(mark => mark.VisualId)
        .ToArray();

static bool IsWithin(WorldAddress address, WorldRectangle bounds) =>
    address.X >= bounds.MinX &&
    address.X < bounds.MinX + bounds.Width &&
    address.Y >= bounds.MinY &&
    address.Y < bounds.MinY + bounds.Height;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
