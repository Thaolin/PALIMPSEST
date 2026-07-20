namespace Chronicle.VisualPack;

/// <summary>
/// The compact, manually authored Gate 3B packager. It is intentionally not a
/// catalogue compiler; it emits the same immutable runtime seam a later
/// authoring tool may target.
/// </summary>
public static class ManualVisualPack
{
    private const byte Transparent = 0;
    private const byte SurfaceDark = 1;
    private const byte Grass = 2;
    private const byte GrassLight = 3;
    private const byte SoilDark = 4;
    private const byte Soil = 5;
    private const byte WaterDeep = 6;
    private const byte Water = 7;
    private const byte WaterLight = 8;
    private const byte GroveDark = 9;
    private const byte Grove = 10;
    private const byte GroveLight = 11;
    private const byte StoneDark = 12;
    private const byte Stone = 13;
    private const byte StoneLight = 14;
    private const byte SkyDeep = 15;
    private const byte Sky = 16;
    private const byte CloudShadow = 17;
    private const byte Cloud = 18;
    private const byte GoldDark = 19;
    private const byte Gold = 20;
    private const byte GoldBright = 21;
    private const byte ActorDark = 22;
    private const byte Actor = 23;
    private const byte ActorLight = 24;
    private const byte Cyan = 25;
    private const byte UiDark = 26;
    private const byte UiLight = 27;

    public static CompiledVisualPack CreateGate3B(int cellSize)
    {
        if (cellSize is not (16 or 20))
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellSize),
                "The Gate 3B comparison has exactly two native cell sizes: 16 and 20.");
        }

        var builder = new AtlasBuilder(cellSize);
        AddGroundFamilies(builder);
        AddWaterAdjacency(builder);
        AddSurfaceFeatures(builder);
        AddCloudAdjacency(builder);
        AddSubjectsAndActor(builder);
        AddEmphasis(builder);
        AddGlyphs(builder);
        AddHearthstone(builder);

        return builder.Build(
            packId: "chronicle.gate3b.manual",
            atlasId: $"chronicle.world-{cellSize}",
            paletteId: "chronicle.gate3b.palette",
            Palette,
            PaletteRoles);
    }

    private static IReadOnlyList<PaletteColor> Palette { get; } =
        Array.AsReadOnly(
        [
            new PaletteColor(0, 0, 0, 0),
            new PaletteColor(20, 31, 25),
            new PaletteColor(47, 91, 51),
            new PaletteColor(82, 126, 70),
            new PaletteColor(65, 45, 29),
            new PaletteColor(112, 78, 48),
            new PaletteColor(18, 48, 70),
            new PaletteColor(27, 91, 117),
            new PaletteColor(83, 166, 166),
            new PaletteColor(17, 48, 29),
            new PaletteColor(37, 96, 52),
            new PaletteColor(83, 132, 72),
            new PaletteColor(47, 51, 60),
            new PaletteColor(103, 108, 119),
            new PaletteColor(177, 183, 190),
            new PaletteColor(12, 31, 53),
            new PaletteColor(20, 61, 88),
            new PaletteColor(104, 134, 148),
            new PaletteColor(173, 198, 204),
            new PaletteColor(117, 76, 20),
            new PaletteColor(225, 164, 45),
            new PaletteColor(255, 226, 112),
            new PaletteColor(39, 29, 51),
            new PaletteColor(214, 61, 88),
            new PaletteColor(255, 243, 211),
            new PaletteColor(112, 222, 220),
            new PaletteColor(9, 15, 24),
            new PaletteColor(223, 233, 235),
        ]);

    private static IReadOnlyDictionary<string, int> PaletteRoles { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["actor.dark"] = ActorDark,
            ["actor.primary"] = Actor,
            ["actor.read"] = ActorLight,
            ["cloud.primary"] = Cloud,
            ["cloud.shadow"] = CloudShadow,
            ["emphasis.active"] = Cyan,
            ["landmark.bright"] = GoldBright,
            ["landmark.gold"] = Gold,
            ["sky.deep"] = SkyDeep,
            ["sky.ground"] = Sky,
            ["stone.dark"] = StoneDark,
            ["stone.primary"] = Stone,
            ["surface.dark"] = SurfaceDark,
            ["surface.grass"] = Grass,
            ["surface.soil"] = Soil,
            ["ui.light"] = UiLight,
            ["water.deep"] = WaterDeep,
            ["water.primary"] = Water,
            ["water.shine"] = WaterLight,
        };

    private static void AddGroundFamilies(AtlasBuilder builder)
    {
        for (var variant = 0; variant < 4; variant++)
        {
            var captured = variant;
            builder.Add(
                VariantId("terrain.surface.grass", variant),
                "terrain.surface.grass",
                variant,
                VisualLayerClass.GroundField,
                Grass,
                [Grass, GrassLight, SurfaceDark],
                canvas => PaintGround(canvas, Grass, GrassLight, SurfaceDark, captured));
            builder.Add(
                VariantId("terrain.surface.soil", variant),
                "terrain.surface.soil",
                variant,
                VisualLayerClass.GroundField,
                Soil,
                [Soil, SoilDark, Stone],
                canvas => PaintGround(canvas, Soil, SoilDark, Stone, captured + 1));
        }

        builder.Add(
            "terrain.surface.water",
            "terrain.surface.water",
            0,
            VisualLayerClass.GroundField,
            Water,
            [WaterDeep, Water, WaterLight],
            canvas =>
            {
                canvas.Fill(Water);
                var y = canvas.Size / 3;
                canvas.Horizontal(2, canvas.Size - 4, y, WaterLight);
                canvas.Horizontal(canvas.Size / 2, canvas.Size - 3, y + canvas.Size / 3, WaterDeep);
            });

        for (var variant = 0; variant < 3; variant++)
        {
            var captured = variant;
            builder.Add(
                VariantId("terrain.sky.open", variant),
                "terrain.sky.open",
                variant,
                VisualLayerClass.GroundField,
                Sky,
                [SkyDeep, Sky],
                canvas => PaintSky(canvas, captured));
        }
    }

    private static void AddWaterAdjacency(AtlasBuilder builder)
    {
        for (var rawMask = 0; rawMask < 16; rawMask++)
        {
            var mask = (CardinalAdjacencyMask)rawMask;
            builder.Add(
                $"terrain.surface.water.edge.{rawMask:00}",
                "terrain.surface.water.edge",
                rawMask,
                VisualLayerClass.Adjacency,
                Water,
                [SoilDark, WaterDeep, WaterLight],
                canvas => PaintWaterEdge(canvas, mask),
                mask);
        }
    }

    private static void AddSurfaceFeatures(AtlasBuilder builder)
    {
        for (var rawMask = 0; rawMask < 16; rawMask++)
        {
            var mask = (CardinalAdjacencyMask)rawMask;
            for (var variant = 0; variant < 2; variant++)
            {
                var stableVariant = variant;
                builder.Add(
                    MaskedVariantId("feature.surface.grove", rawMask, stableVariant),
                    "feature.surface.grove",
                    stableVariant,
                    VisualLayerClass.EnvironmentalFeature,
                    Grove,
                    [GroveDark, Grove, GroveLight, SoilDark],
                    canvas => PaintGrove(canvas, mask, stableVariant),
                    mask);
                builder.Add(
                    MaskedVariantId("feature.surface.ridge", rawMask, stableVariant),
                    "feature.surface.ridge",
                    stableVariant,
                    VisualLayerClass.EnvironmentalFeature,
                    Stone,
                    [StoneDark, Stone, StoneLight],
                    canvas => PaintRidge(canvas, mask, stableVariant),
                    mask);
            }

            for (var variant = 0; variant < 4; variant++)
            {
                var stableVariant = variant;
                builder.Add(
                    MaskedVariantId(
                        "feature.surface.ridge-water-crossing",
                        rawMask,
                        stableVariant),
                    "feature.surface.ridge-water-crossing",
                    stableVariant,
                    VisualLayerClass.EnvironmentalFeature,
                    StoneLight,
                    [StoneDark, Stone, StoneLight, WaterDeep, WaterLight],
                    canvas => PaintRidgeWaterCrossing(canvas, mask, stableVariant),
                    mask);
            }
        }
    }

    private static void AddCloudAdjacency(AtlasBuilder builder)
    {
        for (var rawMask = 0; rawMask < 16; rawMask++)
        {
            var mask = (CardinalAdjacencyMask)rawMask;
            builder.Add(
                rawMask == 0 ? "terrain.sky.cloud" : $"terrain.sky.cloud.mask.{rawMask:00}",
                "terrain.sky.cloud",
                rawMask,
                VisualLayerClass.Adjacency,
                Cloud,
                [CloudShadow, Cloud],
                canvas => PaintCloud(canvas, mask),
                mask);
        }
    }

    private static void AddSubjectsAndActor(AtlasBuilder builder)
    {
        builder.Add(
            "landmark.bell-that-fell-up",
            "landmark.bell-that-fell-up",
            0,
            VisualLayerClass.LandmarkOrSubject,
            Gold,
            [GoldDark, Gold, GoldBright, UiDark],
            PaintBell);
        builder.Add(
            "subject.loose-stone",
            "subject.loose-stone",
            0,
            VisualLayerClass.LandmarkOrSubject,
            StoneLight,
            [StoneDark, Stone, StoneLight, ActorLight],
            PaintLooseStone);
        builder.Add(
            "actor.incarnation",
            "actor.incarnation",
            0,
            VisualLayerClass.Actor,
            Actor,
            [ActorDark, Actor, ActorLight],
            PaintActor);
    }

    private static void AddHearthstone(AtlasBuilder builder)
    {
        builder.Add(
            "subject.home-hearthstone",
            "subject.home-hearthstone",
            0,
            VisualLayerClass.LandmarkOrSubject,
            Gold,
            [StoneDark, Stone, StoneLight, GoldDark, Gold, GoldBright],
            PaintHearthstone);
    }

    private static void AddEmphasis(AtlasBuilder builder)
    {
        builder.Add(
            "emphasis.target.valid",
            "emphasis.target.valid",
            0,
            VisualLayerClass.TemporaryAction,
            Cyan,
            [Cyan],
            canvas => PaintCorners(canvas, Cyan, inset: 2, arm: Math.Max(3, canvas.Size / 4)));
        builder.Add(
            "emphasis.selection",
            "emphasis.selection",
            0,
            VisualLayerClass.TargetOrSelection,
            GoldBright,
            [GoldBright],
            canvas => PaintCorners(canvas, GoldBright, inset: 0, arm: Math.Max(4, canvas.Size / 3)));
    }

    private static void AddGlyphs(AtlasBuilder builder)
    {
        builder.Add(
            "glyph.codex",
            "glyph.codex",
            0,
            VisualLayerClass.UiGlyph,
            UiLight,
            [UiDark, UiLight, Gold],
            PaintCodex);
        builder.Add(
            "glyph.loadout",
            "glyph.loadout",
            0,
            VisualLayerClass.UiGlyph,
            UiLight,
            [UiDark, UiLight, Cyan],
            PaintLoadout);
        builder.Add(
            "glyph.codex.fly",
            "glyph.codex.fly",
            0,
            VisualLayerClass.UiGlyph,
            Cyan,
            [UiDark, Cyan, UiLight],
            PaintFly);
        builder.Add(
            "glyph.codex.stone",
            "glyph.codex.stone",
            0,
            VisualLayerClass.UiGlyph,
            StoneLight,
            [StoneDark, Stone, StoneLight],
            PaintStoneGlyph);
    }

    private static string VariantId(string baseId, int variant) =>
        variant == 0 ? baseId : $"{baseId}.v{variant}";

    private static string MaskedId(string baseId, int mask) =>
        mask == 0 ? baseId : $"{baseId}.mask.{mask:00}";

    private static string MaskedVariantId(string baseId, int mask, int variant)
    {
        var masked = MaskedId(baseId, mask);
        return variant == 0 ? masked : $"{masked}.v{variant}";
    }

    private static void PaintGround(
        PixelCanvas canvas,
        byte fill,
        byte light,
        byte dark,
        int variant)
    {
        canvas.Fill(fill);
        var inset = canvas.Size == 20 ? 3 : 2;
        for (var i = 0; i < 4; i++)
        {
            var x = inset + (i * 5 + variant * 3) % (canvas.Size - inset * 2);
            var y = inset + (i * 3 + variant * 5) % (canvas.Size - inset * 2);
            canvas.Pixel(x, y, i % 3 == 0 ? dark : light);
            if (canvas.Size == 20 && i % 2 == 0)
            {
                canvas.Pixel(x + 1, y, light);
            }
        }
    }

    private static void PaintSky(PixelCanvas canvas, int variant)
    {
        canvas.Fill(Sky);
        if (variant > 0)
        {
            var x = 3 + variant * 5;
            var y = 4 + variant * 5;
            canvas.Horizontal(x, x + variant - 1, y, SkyDeep);
        }
    }

    private static void PaintWaterEdge(PixelCanvas canvas, CardinalAdjacencyMask mask)
    {
        var last = canvas.Size - 1;
        var bank = canvas.Size == 20 ? 2 : 1;
        if (!mask.HasFlag(CardinalAdjacencyMask.North))
        {
            canvas.Rect(0, 0, canvas.Size, bank, SoilDark);
            canvas.Horizontal(1, canvas.Size - 2, bank, WaterLight);
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.East))
        {
            canvas.Rect(last - bank + 1, 0, bank, canvas.Size, SoilDark);
            canvas.Vertical(last - bank, 1, canvas.Size - 2, WaterLight);
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.South))
        {
            canvas.Rect(0, last - bank + 1, canvas.Size, bank, SoilDark);
            canvas.Horizontal(1, canvas.Size - 2, last - bank, WaterDeep);
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.West))
        {
            canvas.Rect(0, 0, bank, canvas.Size, SoilDark);
            canvas.Vertical(bank, 1, canvas.Size - 2, WaterLight);
        }

        canvas.Horizontal(canvas.Size / 3, canvas.Size * 2 / 3, canvas.Size / 2, WaterLight);
    }

    private static void PaintGrove(
        PixelCanvas canvas,
        CardinalAdjacencyMask mask,
        int variant)
    {
        var s = canvas.Size;
        var cut = s == 20 ? 4 : 3;
        canvas.Fill(GroveDark);
        CarveMissingEdges(canvas, mask, cut);

        var center = s / 2;
        var primaryX = center + (variant == 0 ? -2 : 2);
        var primaryY = center + (variant == 0 ? 1 : -1);
        canvas.Diamond(primaryX, primaryY, s == 20 ? 3 : 2, Grove);
        canvas.Horizontal(primaryX - 1, primaryX + 1, primaryY - 2, GroveLight);
        canvas.Rect(primaryX, primaryY + 3, 1, Math.Max(2, s / 5), SoilDark);
        if (variant == 1)
        {
            var secondaryX = center - 4;
            canvas.Diamond(secondaryX, center - 4, 2, Grove);
            canvas.Pixel(secondaryX, center - 5, GroveLight);
        }
    }

    private static void CarveMissingEdges(
        PixelCanvas canvas,
        CardinalAdjacencyMask mask,
        int cut)
    {
        var s = canvas.Size;
        if (!mask.HasFlag(CardinalAdjacencyMask.North))
        {
            for (var x = 0; x < s; x++)
            {
                canvas.Rect(x, 0, 1, cut - (x % 5 == 2 ? 1 : 0), Transparent);
            }
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.East))
        {
            for (var y = 0; y < s; y++)
            {
                canvas.Rect(s - cut + (y % 5 == 2 ? 1 : 0), y, cut, 1, Transparent);
            }
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.South))
        {
            for (var x = 0; x < s; x++)
            {
                canvas.Rect(x, s - cut + (x % 5 == 2 ? 1 : 0), 1, cut, Transparent);
            }
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.West))
        {
            for (var y = 0; y < s; y++)
            {
                canvas.Rect(0, y, cut - (y % 5 == 2 ? 1 : 0), 1, Transparent);
            }
        }
    }

    private static void PaintRidge(
        PixelCanvas canvas,
        CardinalAdjacencyMask mask,
        int variant)
    {
        var s = canvas.Size;
        var center = s / 2;
        var width = s == 20 ? 5 : 4;
        var half = width / 2;

        if (mask.HasFlag(CardinalAdjacencyMask.North))
        {
            canvas.Rect(center - half, 0, width, center + 1, StoneDark);
            canvas.Rect(center - half + 1, 0, width - 2, center + 1, Stone);
        }

        if (mask.HasFlag(CardinalAdjacencyMask.East))
        {
            canvas.Rect(center, center - half, s - center, width, StoneDark);
            canvas.Rect(center, center - half + 1, s - center, width - 2, Stone);
        }

        if (mask.HasFlag(CardinalAdjacencyMask.South))
        {
            canvas.Rect(center - half, center, width, s - center, StoneDark);
            canvas.Rect(center - half + 1, center, width - 2, s - center, Stone);
        }

        if (mask.HasFlag(CardinalAdjacencyMask.West))
        {
            canvas.Rect(0, center - half, center, width, StoneDark);
            canvas.Rect(0, center - half + 1, center, width - 2, Stone);
        }

        var peakX = center + (variant == 0 ? -2 : 2);
        var peakOffset = variant == 0
            ? (s == 20 ? 6 : 5)
            : (s == 20 ? 4 : 3);
        var peakY = center - peakOffset;
        var baseY = center + peakOffset;
        var baseHalf = variant == 0
            ? (s == 20 ? 7 : 6)
            : (s == 20 ? 5 : 4);
        canvas.Triangle(center - baseHalf, baseY, peakX, peakY, center + baseHalf, baseY, StoneDark);
        canvas.Triangle(
            center - baseHalf + 2,
            baseY - 1,
            peakX,
            peakY + 3,
            center + baseHalf - 2,
            baseY - 1,
            Stone);
        canvas.Line(peakX, peakY + 2, center + (variant == 0 ? 2 : -2), center + 2, StoneLight);
        canvas.Pixel(center + (variant == 0 ? 4 : -4), baseY - 2, StoneLight);
        if (variant == 1)
        {
            canvas.Triangle(
                center - 7,
                center + 5,
                center - 4,
                center,
                center - 1,
                center + 5,
                StoneDark);
            canvas.Line(center - 4, center + 1, center - 2, center + 4, StoneLight);
        }
    }

    private static void PaintRidgeWaterCrossing(
        PixelCanvas canvas,
        CardinalAdjacencyMask mask,
        int variant)
    {
        var s = canvas.Size;
        var center = s / 2;
        var width = s == 20 ? 5 : 4;
        var half = width / 2;
        var depth = s == 20 ? 4 : 3;

        if (mask.HasFlag(CardinalAdjacencyMask.North))
        {
            canvas.Rect(center - half, 0, width, depth, StoneDark);
            canvas.Rect(center - half + 1, 0, width - 2, depth, Stone);
        }

        if (mask.HasFlag(CardinalAdjacencyMask.East))
        {
            canvas.Rect(s - depth, center - half, depth, width, StoneDark);
            canvas.Rect(s - depth, center - half + 1, depth, width - 2, Stone);
        }

        if (mask.HasFlag(CardinalAdjacencyMask.South))
        {
            canvas.Rect(center - half, s - depth, width, depth, StoneDark);
            canvas.Rect(center - half + 1, s - depth, width - 2, depth, Stone);
        }

        if (mask.HasFlag(CardinalAdjacencyMask.West))
        {
            canvas.Rect(0, center - half, depth, width, StoneDark);
            canvas.Rect(0, center - half + 1, depth, width - 2, Stone);
        }

        var directionBias =
            (mask.HasFlag(CardinalAdjacencyMask.East) ? 1 : 0) -
            (mask.HasFlag(CardinalAdjacencyMask.West) ? 1 : 0);
        var (firstOffsetX, firstOffsetY, secondOffsetX, secondOffsetY) = variant switch
        {
            0 => (-4, -2, 3, 4),
            1 => (4, -4, -3, 2),
            2 => (-1, -5, 5, 1),
            _ => (2, 0, -5, 5),
        };
        var radius = s == 20 && variant % 2 == 0 ? 2 : 1;
        var firstX = Math.Clamp(center + firstOffsetX, radius + 2, s - radius - 3);
        var firstY = Math.Clamp(
            center + firstOffsetY + directionBias,
            radius + 2,
            s - radius - 3);
        var secondX = Math.Clamp(center + secondOffsetX, radius + 1, s - radius - 2);
        var secondY = Math.Clamp(
            center + secondOffsetY - directionBias,
            radius + 1,
            s - radius - 2);

        canvas.Diamond(firstX, firstY, radius + 1, StoneDark);
        canvas.Diamond(firstX, firstY, radius, Stone);
        canvas.Diamond(secondX, secondY, radius, StoneDark);
        canvas.Pixel(secondX, secondY, Stone);
        canvas.Pixel(firstX - 1, firstY - 1, StoneLight);
        canvas.Pixel(secondX, secondY - 1, StoneLight);
    }

    private static void PaintCloud(PixelCanvas canvas, CardinalAdjacencyMask mask)
    {
        var s = canvas.Size;
        canvas.Fill(Cloud);
        var cut = s == 20 ? 4 : 3;

        if (!mask.HasFlag(CardinalAdjacencyMask.North))
        {
            for (var x = 0; x < s; x++)
            {
                canvas.Rect(x, 0, 1, cut - (x % 5 == 2 ? 1 : 0), Transparent);
            }

            canvas.Horizontal(cut, s - cut - 1, cut, UiLight);
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.East))
        {
            for (var y = 0; y < s; y++)
            {
                canvas.Rect(
                    s - cut + (y % 5 == 2 ? 1 : 0),
                    y,
                    cut,
                    1,
                    Transparent);
            }

            canvas.Vertical(s - cut - 1, cut, s - cut - 1, CloudShadow);
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.South))
        {
            for (var x = 0; x < s; x++)
            {
                canvas.Rect(
                    x,
                    s - cut + (x % 5 == 2 ? 1 : 0),
                    1,
                    cut,
                    Transparent);
            }

            canvas.Horizontal(cut, s - cut - 1, s - cut - 1, CloudShadow);
        }

        if (!mask.HasFlag(CardinalAdjacencyMask.West))
        {
            for (var y = 0; y < s; y++)
            {
                canvas.Rect(0, y, cut - (y % 5 == 2 ? 1 : 0), 1, Transparent);
            }

            canvas.Vertical(cut, cut, s - cut - 1, UiLight);
        }
    }

    private static void PaintBell(PixelCanvas canvas)
    {
        var s = canvas.Size;
        var center = s / 2;
        canvas.Diamond(center, center, s / 2 - 1, GoldDark);
        canvas.Diamond(center, center, s / 2 - 3, Gold);
        canvas.Rect(center - s / 3, center - s / 4, s * 2 / 3 + 1, s / 2, UiDark);
        canvas.Rect(center - s / 3 + 1, center - s / 4 + 1, s * 2 / 3 - 1, s / 2 - 2, Gold);
        canvas.Rect(center - s / 4, center - s / 5, s / 2 + 1, s / 3, UiDark);
        canvas.Horizontal(center - s / 2 + 2, center + s / 2 - 2, center + s / 3, GoldBright);
        canvas.Rect(center - 1, 1, 3, 3, GoldBright);
        canvas.Rect(center - 1, center + s / 3 + 1, 3, 3, Gold);
        canvas.Pixel(1, center, GoldBright);
        canvas.Pixel(s - 2, center, GoldBright);
    }

    private static void PaintLooseStone(PixelCanvas canvas)
    {
        var s = canvas.Size;
        canvas.Diamond(s / 2, s / 2 + 2, s / 3, StoneDark);
        canvas.Diamond(s / 2, s / 2 + 1, s / 3 - 2, Stone);
        canvas.Triangle(s / 3, s * 2 / 3, s / 2, s / 3, s * 3 / 4, s * 2 / 3, StoneLight);
        canvas.Line(s / 2, s / 3, s * 2 / 3, s / 2, ActorLight);
    }

    private static void PaintHearthstone(PixelCanvas canvas)
    {
        var s = canvas.Size;
        var center = s / 2;
        var baseY = s - (s == 20 ? 3 : 2);
        var baseHalf = s == 20 ? 6 : 5;
        var peakY = s == 20 ? 5 : 4;
        canvas.Triangle(center - baseHalf, baseY, center, peakY, center + baseHalf, baseY, StoneDark);
        canvas.Triangle(
            center - baseHalf + 2,
            baseY - 1,
            center,
            peakY + 2,
            center + baseHalf - 2,
            baseY - 1,
            Stone);
        canvas.Horizontal(center - baseHalf + 2, center + baseHalf - 2, baseY - 2, StoneLight);

        var hearthRadius = s == 20 ? 3 : 2;
        canvas.Diamond(center, center, hearthRadius, GoldDark);
        canvas.Diamond(center, center, hearthRadius - 1, Gold);
        canvas.Vertical(center, center - 1, center + 1, GoldBright);
    }

    private static void PaintActor(PixelCanvas canvas)
    {
        var s = canvas.Size;
        var center = s / 2;
        canvas.Rect(center - 2, 2, 5, 6, ActorLight);
        canvas.Rect(center - 1, 3, 3, 4, Actor);
        canvas.Triangle(center - s / 3, s - 2, center, s / 3, center + s / 3, s - 2, ActorLight);
        canvas.Triangle(center - s / 4, s - 3, center, s / 2 - 1, center + s / 4, s - 3, Actor);
        canvas.Vertical(center, s / 2, s - 4, ActorLight);
        canvas.Pixel(center - 1, 4, ActorDark);
        canvas.Pixel(center + 1, 4, ActorDark);
    }

    private static void PaintCorners(PixelCanvas canvas, byte color, int inset, int arm)
    {
        var last = canvas.Size - 1 - inset;
        canvas.Horizontal(inset, inset + arm, inset, color);
        canvas.Vertical(inset, inset, inset + arm, color);
        canvas.Horizontal(last - arm, last, inset, color);
        canvas.Vertical(last, inset, inset + arm, color);
        canvas.Horizontal(inset, inset + arm, last, color);
        canvas.Vertical(inset, last - arm, last, color);
        canvas.Horizontal(last - arm, last, last, color);
        canvas.Vertical(last, last - arm, last, color);
    }

    private static void PaintCodex(PixelCanvas canvas)
    {
        var s = canvas.Size;
        canvas.Rect(2, 3, s / 2 - 2, s - 6, Gold);
        canvas.Rect(s / 2, 3, s / 2 - 2, s - 6, UiLight);
        canvas.Vertical(s / 2 - 1, 3, s - 4, UiDark);
        canvas.Horizontal(4, s / 2 - 3, 6, UiDark);
        canvas.Horizontal(s / 2 + 2, s - 4, 6, UiDark);
    }

    private static void PaintLoadout(PixelCanvas canvas)
    {
        var s = canvas.Size;
        var cell = Math.Max(3, s / 4);
        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var px = 2 + x * (cell + 2);
                var py = 2 + y * (cell + 2);
                canvas.Rect(px, py, cell, cell, UiLight);
                canvas.Rect(px + 1, py + 1, cell - 2, cell - 2, x == 0 && y == 0 ? Cyan : UiDark);
            }
        }
    }

    private static void PaintFly(PixelCanvas canvas)
    {
        var s = canvas.Size;
        var center = s / 2;
        canvas.Line(2, center, center, center - 4, Cyan);
        canvas.Line(2, center, center, center + 2, UiLight);
        canvas.Line(s - 3, center, center, center - 4, Cyan);
        canvas.Line(s - 3, center, center, center + 2, UiLight);
        canvas.Vertical(center, center - 4, s - 3, Cyan);
        canvas.Line(center, center - 5, center - 3, center - 1, UiLight);
        canvas.Line(center, center - 5, center + 3, center - 1, UiLight);
    }

    private static void PaintStoneGlyph(PixelCanvas canvas)
    {
        var s = canvas.Size;
        canvas.Diamond(s / 2, s / 2 + 1, s / 3, StoneDark);
        canvas.Triangle(s / 4, s * 2 / 3, s / 2, s / 3, s * 3 / 4, s * 2 / 3, Stone);
        canvas.Line(s / 2, s / 3, s * 2 / 3, s / 2, StoneLight);
    }

    private sealed class AtlasBuilder(int cellSize)
    {
        private readonly List<Tile> _tiles = [];

        public void Add(
            string visualId,
            string familyId,
            int variantOrdinal,
            VisualLayerClass layerClass,
            int overviewPaletteIndex,
            IReadOnlyList<int> paletteRoleIndexes,
            Action<PixelCanvas> paint,
            CardinalAdjacencyMask? adjacencyMask = null)
        {
            var pixels = new byte[checked(cellSize * cellSize)];
            paint(new PixelCanvas(cellSize, pixels));
            _tiles.Add(
                new Tile(
                    visualId,
                    familyId,
                    variantOrdinal,
                    layerClass,
                    adjacencyMask,
                    overviewPaletteIndex,
                    paletteRoleIndexes.ToArray(),
                    pixels));
        }

        public CompiledVisualPack Build(
            string packId,
            string atlasId,
            string paletteId,
            IReadOnlyList<PaletteColor> palette,
            IReadOnlyDictionary<string, int> paletteRoles)
        {
            const int columns = 8;
            var rows = (_tiles.Count + columns - 1) / columns;
            var atlasWidth = checked(columns * cellSize);
            var atlasHeight = checked(rows * cellSize);
            var atlas = new byte[checked(atlasWidth * atlasHeight)];
            var definitions = new List<VisualDefinition>(_tiles.Count);

            for (var index = 0; index < _tiles.Count; index++)
            {
                var tile = _tiles[index];
                var atlasX = index % columns * cellSize;
                var atlasY = index / columns * cellSize;
                for (var y = 0; y < cellSize; y++)
                {
                    Array.Copy(
                        tile.Pixels,
                        y * cellSize,
                        atlas,
                        (atlasY + y) * atlasWidth + atlasX,
                        cellSize);
                }

                definitions.Add(
                    new VisualDefinition(
                        tile.VisualId,
                        new AtlasRect(atlasX, atlasY, cellSize, cellSize),
                        tile.FamilyId,
                        tile.VariantOrdinal,
                        tile.LayerClass,
                        new PixelAnchor(cellSize / 2, cellSize / 2),
                        tile.AdjacencyMask,
                        tile.OverviewPaletteIndex,
                        tile.PaletteRoleIndexes));
            }

            return new CompiledVisualPack(
                packId,
                formatVersion: 1,
                styleVersion: 1,
                composerVersion: 1,
                cellSize,
                atlasId,
                paletteId,
                atlasWidth,
                atlasHeight,
                atlas,
                palette,
                paletteRoles,
                definitions);
        }

        private sealed record Tile(
            string VisualId,
            string FamilyId,
            int VariantOrdinal,
            VisualLayerClass LayerClass,
            CardinalAdjacencyMask? AdjacencyMask,
            int OverviewPaletteIndex,
            int[] PaletteRoleIndexes,
            byte[] Pixels);
    }

    private sealed class PixelCanvas
    {
        private readonly byte[] _pixels;

        public PixelCanvas(int size, byte[] pixels)
        {
            Size = size;
            _pixels = pixels;
        }

        public int Size { get; }

        public void Fill(byte color) => Array.Fill(_pixels, color);

        public void Pixel(int x, int y, byte color)
        {
            if (x >= 0 && y >= 0 && x < Size && y < Size)
            {
                _pixels[y * Size + x] = color;
            }
        }

        public void Rect(int x, int y, int width, int height, byte color)
        {
            for (var row = y; row < y + height; row++)
            {
                for (var column = x; column < x + width; column++)
                {
                    Pixel(column, row, color);
                }
            }
        }

        public void Horizontal(int x1, int x2, int y, byte color)
        {
            for (var x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            {
                Pixel(x, y, color);
            }
        }

        public void Vertical(int x, int y1, int y2, byte color)
        {
            for (var y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            {
                Pixel(x, y, color);
            }
        }

        public void Diamond(int centerX, int centerY, int radius, byte color)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var half = radius - Math.Abs(y);
                Horizontal(centerX - half, centerX + half, centerY + y, color);
            }
        }

        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, byte color)
        {
            var minX = Math.Min(x1, Math.Min(x2, x3));
            var maxX = Math.Max(x1, Math.Max(x2, x3));
            var minY = Math.Min(y1, Math.Min(y2, y3));
            var maxY = Math.Max(y1, Math.Max(y2, y3));
            var area = Edge(x1, y1, x2, y2, x3, y3);
            if (area == 0)
            {
                return;
            }

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var a = Edge(x1, y1, x2, y2, x, y);
                    var b = Edge(x2, y2, x3, y3, x, y);
                    var c = Edge(x3, y3, x1, y1, x, y);
                    if ((a >= 0 && b >= 0 && c >= 0) || (a <= 0 && b <= 0 && c <= 0))
                    {
                        Pixel(x, y, color);
                    }
                }
            }
        }

        public void Line(int x1, int y1, int x2, int y2, byte color)
        {
            var dx = Math.Abs(x2 - x1);
            var sx = x1 < x2 ? 1 : -1;
            var dy = -Math.Abs(y2 - y1);
            var sy = y1 < y2 ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                Pixel(x1, y1, color);
                if (x1 == x2 && y1 == y2)
                {
                    break;
                }

                var doubled = error * 2;
                if (doubled >= dy)
                {
                    error += dy;
                    x1 += sx;
                }

                if (doubled <= dx)
                {
                    error += dx;
                    y1 += sy;
                }
            }
        }

        private static int Edge(int ax, int ay, int bx, int by, int px, int py) =>
            (px - ax) * (by - ay) - (py - ay) * (bx - ax);
    }
}
