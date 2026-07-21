using Chronicle.VisualCompiler;
using Chronicle.VisualPack;

static class CompilerConformance
{
    private static readonly byte[] CatalogueJson = """
        {
          "schemaVersion": 1,
          "packId": "chronicle.e1",
          "visualStyleVersion": 1,
          "palettes": [
            {
              "id": "surface",
              "entries": ["00000000", "e8c45cff"],
              "roles": { "surface.ink": 1 }
            }
          ],
          "families": [
            {
              "id": "glyph.test",
              "seed": 7,
              "variantCount": 2,
              "targets": [
                {
                  "nativeSize": 16,
                  "rows": ["0000", "0110", "0110", "0000"],
                  "anchor": [2, 3],
                  "layer": "overlay",
                  "paletteRoles": ["surface.ink"]
                },
                {
                  "nativeSize": 20,
                  "rows": ["00000", "00100", "01110", "00100", "00000"],
                  "anchor": [2, 4],
                  "layer": "overlay",
                  "paletteRoles": ["surface.ink"]
                }
              ]
            },
            {
              "id": "glyph.control",
              "seed": 11,
              "variantCount": 1,
              "targets": [
                {
                  "nativeSize": 16,
                  "rows": ["010", "111", "010"],
                  "anchor": [1, 2],
                  "layer": "overlay",
                  "paletteRoles": ["surface.ink"]
                },
                {
                  "nativeSize": 20,
                  "rows": ["00100", "01110", "11111", "01110", "00100"],
                  "anchor": [2, 4],
                  "layer": "overlay",
                  "paletteRoles": ["surface.ink"]
                }
              ]
            }
          ]
        }
        """u8.ToArray();

    public static bool Run()
    {
        var catalogue = VisualCatalogue.ParseJson(CatalogueJson);
        var result = VisualCompiler.Compile(
            catalogue,
            new CompilationOptions(ReviewMode.None));
        if (!result.Succeeded || result.Pack is null)
        {
            Console.Error.WriteLine("CVC-E1-COMPILE: known silhouette did not compile.");
            return false;
        }

        if (!result.Pack.TryResolve("glyph.test", 16, 0, null, out var handle))
        {
            Console.Error.WriteLine("CVC-E1-RESOLVE: compiled visual did not resolve.");
            return false;
        }

        var visual = result.Pack.GetVisual(handle);
        var expected = new byte[]
        {
            0, 0, 0, 0,
            0, 1, 1, 0,
            0, 1, 1, 0,
            0, 0, 0, 0
        };
        if (visual.NativeSize != 16 ||
            !Extract(result.Pack, visual).SequenceEqual(expected) ||
            PackValidator.Validate(result.Pack).Length != 0)
        {
            Console.Error.WriteLine("CVC-E1-PIXELS: compiled public pack differs from known silhouette.");
            return false;
        }

        if (!result.Pack.TryResolve("glyph.test", 16, 1, null, out var variantHandle) ||
            result.Pack.GetVisual(variantHandle).FamilyId != visual.FamilyId ||
            result.Pack.GetVisual(variantHandle).GeometryDigest == visual.GeometryDigest)
        {
            Console.Error.WriteLine("CVC-E1-VARIANT: seeded variant is absent or lost family lineage.");
            return false;
        }

        if (!result.Pack.TryResolve("glyph.test", 20, 0, null, out var twentyHandle) ||
            !Extract(result.Pack, result.Pack.GetVisual(twentyHandle)).SequenceEqual(new byte[]
            {
                0, 0, 0, 0, 0,
                0, 0, 1, 0, 0,
                0, 1, 1, 1, 0,
                0, 0, 1, 0, 0,
                0, 0, 0, 0, 0
            }))
        {
            Console.Error.WriteLine("CVC-E1-NATIVE-20: independent 20-pixel target differs.");
            return false;
        }

        var repeat = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(CatalogueJson),
            new CompilationOptions(ReviewMode.None));
        if (!repeat.Succeeded || repeat.PackDigest != result.PackDigest)
        {
            Console.Error.WriteLine("CVC-E1-REPRO: repeated in-memory compile changed digest.");
            return false;
        }

        var swapped = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(System.Text.Encoding.UTF8.GetBytes(
                System.Text.Encoding.UTF8.GetString(CatalogueJson)
                    .Replace("e8c45cff", "3c78b4ff", StringComparison.Ordinal))),
            new CompilationOptions(ReviewMode.None));
        if (!swapped.Succeeded ||
            result.Pack.Visuals.Select(static visual => visual.GeometryDigest)
                .SequenceEqual(swapped.Pack!.Visuals.Select(static visual => visual.GeometryDigest)) is false ||
            result.PackDigest == swapped.PackDigest)
        {
            Console.Error.WriteLine("CVC-E1-PALETTE: palette swap changed geometry or not pack identity.");
            return false;
        }

        var review = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(CatalogueJson),
            new CompilationOptions(ReviewMode.Standard));
        var reviewRepeat = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(CatalogueJson),
            new CompilationOptions(ReviewMode.Standard));
        var expectedReviewPaths = new[]
        {
            "review/native-16.png",
            "review/native-20.png",
            "review/nearest-16.png",
            "review/nearest-20.png"
        };
        if (!review.Succeeded ||
            !expectedReviewPaths.All(path =>
                review.ReviewFiles.Any(file => file.Path == path)) ||
            !review.ReviewFiles.Zip(reviewRepeat.ReviewFiles)
                .All(static pair =>
                    pair.First.Path == pair.Second.Path &&
                    pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes.AsSpan())) ||
            review.ReviewFiles
                .Where(static file => file.Path.EndsWith(".png", StringComparison.Ordinal))
                .Any(static file =>
                !file.Bytes.AsSpan()[..8].SequenceEqual(
                    new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })))
        {
            Console.Error.WriteLine("CVC-E1-REVIEW: deterministic native/nearest PNG evidence is absent.");
            return false;
        }

        var editedBytes = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Encoding.UTF8.GetString(CatalogueJson).Replace(
                "\"rows\": [\"0000\", \"0110\", \"0110\", \"0000\"]",
                "\"rows\": [\"00000\", \"01000\", \"01100\", \"00000\"]",
                StringComparison.Ordinal));
        var edited = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(editedBytes),
            new CompilationOptions(ReviewMode.None));
        var originalControl = result.Pack.Visuals
            .Where(static visual => visual.Id == "glyph.control")
            .Select(static visual => visual.GeometryDigest);
        var editedControl = edited.Pack!.Visuals
            .Where(static visual => visual.Id == "glyph.control")
            .Select(static visual => visual.GeometryDigest);
        var originalControlRecords = result.Pack.Visuals
            .Where(static visual => visual.Id == "glyph.control").ToArray();
        var editedControlRecords = edited.Pack.Visuals
            .Where(static visual => visual.Id == "glyph.control").ToArray();
        var originalEdited = result.Pack.Visuals
            .Where(static visual => visual.Id == "glyph.test")
            .Select(static visual => visual.GeometryDigest);
        var changedEdited = edited.Pack.Visuals
            .Where(static visual => visual.Id == "glyph.test")
            .Select(static visual => visual.GeometryDigest);
        if (!edited.Succeeded ||
            !originalControl.SequenceEqual(editedControl) ||
            !originalControlRecords.Zip(editedControlRecords).All(pair =>
                pair.First.Rectangle == pair.Second.Rectangle &&
                Extract(result.Pack, pair.First).SequenceEqual(
                    Extract(edited.Pack, pair.Second))) ||
            originalEdited.SequenceEqual(changedEdited))
        {
            Console.Error.WriteLine("CVC-E1-LOCALITY: local definition edit changed the wrong family.");
            return false;
        }

        try
        {
            _ = VisualCatalogue.ParseJson("{}"u8);
            Console.Error.WriteLine("CVC-E1-CATALOGUE: missing members were accepted.");
            return false;
        }
        catch (FormatException exception) when (exception.Message.StartsWith(
                   "CVC-CAT-004",
                   StringComparison.Ordinal))
        {
            // Expected normalized input rejection.
        }

        var zeroVariants = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(System.Text.Encoding.UTF8.GetBytes(
                System.Text.Encoding.UTF8.GetString(CatalogueJson).Replace(
                    "\"variantCount\": 2",
                    "\"variantCount\": 0",
                    StringComparison.Ordinal))),
            new CompilationOptions(ReviewMode.None));
        var duplicateVariants = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(System.Text.Encoding.UTF8.GetBytes(
                System.Text.Encoding.UTF8.GetString(CatalogueJson).Replace(
                    "\"variantCount\": 2",
                    "\"variantCount\": 16",
                    StringComparison.Ordinal))),
            new CompilationOptions(ReviewMode.None));
        if (!zeroVariants.Diagnostics.Any(static item => item.Code == "CVC-VAR-002") ||
            !duplicateVariants.Diagnostics.Any(static item => item.Code == "CVC-VAR-003"))
        {
            Console.Error.WriteLine("CVC-E1-VARIANT-VALIDATION: invalid variants were accepted.");
            return false;
        }

        return true;
    }

    private static byte[] Extract(CompiledVisualPack pack, VisualRecord visual)
    {
        var atlas = pack.Atlases.Single(item => item.Id == visual.AtlasId);
        var source = pack.GetAtlasIndices(atlas.Id).Span;
        var result = new byte[visual.Rectangle.Width * visual.Rectangle.Height];
        for (var y = 0; y < visual.Rectangle.Height; y++)
        {
            source.Slice(
                    (visual.Rectangle.Y + y) * atlas.Width + visual.Rectangle.X,
                    visual.Rectangle.Width)
                .CopyTo(result.AsSpan(y * visual.Rectangle.Width));
        }
        return result;
    }
}
