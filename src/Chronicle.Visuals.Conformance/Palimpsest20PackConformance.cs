using Chronicle.VisualPack;

static class Palimpsest20PackConformance
{
    public static bool Run()
    {
        var pack = new Palimpsest20Pack(
            packId: "palimpsest20.tracer",
            formatVersion: 1,
            styleVersion: 1,
            composerVersion: 1,
            cellSize: 20,
            atlasId: "palimpsest20",
            paletteId: "surface",
            atlasWidth: 20,
            atlasHeight: 20,
            atlasIndices: Enumerable.Repeat((byte)1, 400).ToArray(),
            palette:
            [
                new Palimpsest20PaletteColor(0, 0, 0, 0),
                new Palimpsest20PaletteColor(47, 91, 51),
            ],
            paletteRoleIndexes: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["surface.grass"] = 1,
            },
            definitions:
            [
                new Palimpsest20Definition(
                    visualId: "terrain.surface.grass",
                    atlasRect: new Palimpsest20AtlasRect(0, 0, 20, 20),
                    familyId: "terrain.surface.grass",
                    variantOrdinal: 0,
                    layerClass: Palimpsest20LayerClass.GroundField,
                    anchor: new Palimpsest20PixelAnchor(10, 10),
                    adjacencyMask: null,
                    overviewPaletteIndex: 1,
                    paletteRoleIndexes: [1]),
            ]);
        var validation = new Palimpsest20Validation(
            PackFormatVersion: 1,
            ComposerContractVersion: 1,
            VisualStyleVersion: 1,
            MinimumReaderVersion: "1.0.0");

        var first = Palimpsest20Codec.WriteCanonical(pack, validation);
        var expectedFiles = new[]
        {
            "atlases/palimpsest20.indices",
            "hashes.json",
            "manifest.json",
            "validation.json",
        };
        if (!first.Files.Select(static file => file.Path)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .SequenceEqual(expectedFiles, StringComparer.Ordinal))
        {
            Console.Error.WriteLine("PAL20-E0-FILES: canonical file set was not exact.");
            return false;
        }

        var loaded = Palimpsest20Codec.ReadCanonical(first.Files);
        var grass = loaded.Pack.Resolve("terrain.surface.grass");
        if (loaded.Pack.CellSize != 20 ||
            loaded.Validation.MinimumReaderVersion != "1.0.0" ||
            grass.AtlasRect != new Palimpsest20AtlasRect(0, 0, 20, 20) ||
            grass.LayerClass != Palimpsest20LayerClass.GroundField ||
            grass.OverviewPaletteIndex != 1)
        {
            Console.Error.WriteLine("PAL20-E0-RESOLVE: grass definition changed after round-trip.");
            return false;
        }

        var second = Palimpsest20Codec.WriteCanonical(loaded.Pack, loaded.Validation);
        if (!FilesEqual(first.CanonicalFiles, second.CanonicalFiles))
        {
            Console.Error.WriteLine("PAL20-E0-ROUNDTRIP: canonical bytes changed after round-trip.");
            return false;
        }

        foreach (var invalidId in new[] { ".foo", "foo..bar", "foo.-bar" })
        {
            try
            {
                _ = new Palimpsest20Pack(
                    pack.PackId,
                    pack.FormatVersion,
                    pack.StyleVersion,
                    pack.ComposerVersion,
                    pack.CellSize,
                    pack.AtlasId,
                    pack.PaletteId,
                    pack.AtlasWidth,
                    pack.AtlasHeight,
                    pack.AtlasIndices,
                    pack.Palette,
                    pack.PaletteRoleIndexes,
                    [
                        new Palimpsest20Definition(
                            invalidId,
                            new Palimpsest20AtlasRect(0, 0, 20, 20),
                            "terrain.surface.grass",
                            0,
                            Palimpsest20LayerClass.GroundField,
                            new Palimpsest20PixelAnchor(10, 10),
                            null,
                            1,
                            [1]),
                    ]);
                Console.Error.WriteLine(
                    $"PAL20-E0-ID: malformed identifier '{invalidId}' was accepted.");
                return false;
            }
            catch (ArgumentException)
            {
            }
        }

        try
        {
            _ = Palimpsest20Codec.ReadCanonical(first.Files.Where(
                static file => file.Path != "validation.json"));
            Console.Error.WriteLine("PAL20-E0-VALIDATION: missing validation.json was accepted.");
            return false;
        }
        catch (FormatException)
        {
            return true;
        }
    }

    private static bool FilesEqual(
        IReadOnlyDictionary<string, System.Collections.Immutable.ImmutableArray<byte>> left,
        IReadOnlyDictionary<string, System.Collections.Immutable.ImmutableArray<byte>> right) =>
        left.Count == right.Count &&
        left.OrderBy(static item => item.Key, StringComparer.Ordinal)
            .Zip(right.OrderBy(static item => item.Key, StringComparer.Ordinal))
            .All(static pair =>
                pair.First.Key == pair.Second.Key &&
                pair.First.Value.AsSpan().SequenceEqual(pair.Second.Value.AsSpan()));
}
