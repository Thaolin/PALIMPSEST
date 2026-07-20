using Chronicle.Core;
using Chronicle.VisualPack;
using Chronicle.Visuals;

VerifyConnectedSurfaceFeaturesUseExplicitCardinalMasks();
VerifyGate3BManualPacksResolveRequiredVisualVocabulary();
VerifyGate3BCompositionCropsAndLayersTheSharedSkySnapshot();
VerifyVisualCompositionAtNumericWorldEdges();
VerifyHomeHearthstoneComposesOverItsSurfaceRidge();
Console.WriteLine(
    "PASS: Gate 3B compiled visual packs, deterministic composition, connected features, and overlap verified.");

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
            VisualStyleVersion: 1,
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
            VisualStyleVersion: 1,
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
    const int expectedStyleVersion = 1;
    const int expectedComposerVersion = 1;
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
        "actor.incarnation",
        "emphasis.target.valid",
        "emphasis.selection",
        "glyph.codex",
        "glyph.loadout",
        "glyph.codex.fly",
        "glyph.codex.stone",
    };

    foreach (var cellSize in new[] { 16, 20 })
    {
        var pack = ManualVisualPack.CreateGate3B(cellSize);

        Assert(pack.PackId == "chronicle.gate3b.manual", "The manual Gate 3B pack needs one stable identity.");
        Assert(pack.FormatVersion == expectedFormatVersion, "The Gate 3B pack format must be version 1.");
        Assert(pack.StyleVersion == expectedStyleVersion, "The Gate 3B visual style must be version 1.");
        Assert(pack.ComposerVersion == expectedComposerVersion, "The Gate 3B composer contract must be version 1.");
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
        VisualStyleVersion: 1,
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

    var state = ChronicleState.Begin(41_337);
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
            VisualStyleVersion: 1,
            IncarnationAddress: null,
            TargetAddresses: [],
            SelectedAddresses: []));
        var overlap = VisualGrammar.Compose(new VisualCompositionInput(
            semanticArea,
            overlapBounds,
            state.Seed,
            pack,
            VisualStyleVersion: 1,
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
