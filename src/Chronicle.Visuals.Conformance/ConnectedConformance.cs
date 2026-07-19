using Chronicle.VisualCompiler;
using Chronicle.VisualPack;
using System.Collections.Immutable;

static class ConnectedConformance
{
    private static readonly byte[] Source = """
        {
          "schemaVersion": 1,
          "packId": "chronicle.e2",
          "visualStyleVersion": 1,
          "palettes": [
            {
              "id": "surface",
              "entries": ["00000000", "4f8ca8ff"],
              "roles": { "water.edge": 1 }
            }
          ],
          "families": [
            {
              "id": "feature.grove",
              "seed": 31,
              "variantCount": 1,
              "targets": [
                {
                  "nativeSize": 16,
                  "rows": ["010", "111", "010"],
                  "anchor": [1, 2],
                  "layer": "feature",
                  "paletteRoles": ["water.edge"]
                }
              ]
            }
          ],
          "connectedFamilies": [
            {
              "id": "terrain.water",
              "seed": 41337,
              "variantCount": 2,
              "nativeSizes": [16, 20],
              "layer": "adjacency",
              "paletteRole": "water.edge",
              "requireEdgeContinuity": true
            },
            {
              "id": "terrain.cloud",
              "seed": 41338,
              "variantCount": 2,
              "nativeSizes": [16, 20],
              "layer": "adjacency",
              "paletteRole": "water.edge",
              "requireEdgeContinuity": true
            },
            {
              "id": "structure.wall",
              "seed": 41339,
              "variantCount": 2,
              "nativeSizes": [16, 20],
              "layer": "structure",
              "paletteRole": "water.edge",
              "requireEdgeContinuity": true
            },
            {
              "id": "terrain.path",
              "seed": 41340,
              "variantCount": 2,
              "nativeSizes": [16, 20],
              "layer": "adjacency",
              "paletteRole": "water.edge",
              "requireEdgeContinuity": true
            },
            {
              "id": "transition.surface-water",
              "seed": 41341,
              "variantCount": 1,
              "nativeSizes": [16, 20],
              "layer": "adjacency",
              "paletteRole": "water.edge",
              "requireEdgeContinuity": true,
              "masks": [0, 1, 2, 4, 8, 15],
              "fallbackMask": 0,
              "transitionOwnership": "primary-ground-plus-one-transition"
            }
          ],
          "motifs": [
            {
              "familyId": "motif.grove",
              "footprint": [2, 2],
              "anchorCell": [0, 1],
              "occupancyTags": ["grove"],
              "clippingBehavior": "clip",
              "marks": [
                { "visualId": "feature.grove", "cell": [0, 1], "pixelOffset": [0, 0] },
                { "visualId": "feature.grove", "cell": [1, 0], "pixelOffset": [1, -1] }
              ]
            }
          ]
        }
        """u8.ToArray();

    public static bool Run()
    {
        var result = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(Source),
            new CompilationOptions(ReviewMode.Standard));
        if (!result.Succeeded || result.Pack is null)
        {
            Console.Error.WriteLine("CVC-E2-COMPILE: connected family and motif did not compile.");
            return false;
        }

        var completeFamilies = new[]
        {
            "terrain.water",
            "terrain.cloud",
            "structure.wall",
            "terrain.path"
        };
        if (completeFamilies.Any(familyId =>
                result.Pack.Adjacencies.SingleOrDefault(
                    item => item.FamilyId == familyId) is not
                {
                    RequireEdgeContinuity: true,
                    FallbackMask: null
                } adjacency ||
                !adjacency.RequiredMasks.SequenceEqual(Enumerable.Range(0, 16))))
        {
            Console.Error.WriteLine("CVC-E2-ADJACENCY: masks 0-15 are incomplete.");
            return false;
        }

        foreach (var familyId in completeFamilies)
        {
            foreach (var mask in Enumerable.Range(0, 16))
            {
                foreach (var nativeSize in new[] { 16, 20 })
                {
                    if (!result.Pack.TryResolve(
                            familyId, nativeSize, 0, mask, out var firstHandle) ||
                        !result.Pack.TryResolve(
                            familyId, nativeSize, 1, mask, out var secondHandle))
                    {
                        Console.Error.WriteLine(
                            $"CVC-E2-RESOLVE: missing {familyId} {nativeSize}px mask {mask}.");
                        return false;
                    }
                    var first = result.Pack.GetVisual(firstHandle);
                    var second = result.Pack.GetVisual(secondHandle);
                    if (first.GeometryDigest == second.GeometryDigest ||
                        !EdgesMatchMask(result.Pack, first, mask) ||
                        !EdgesMatchMask(result.Pack, second, mask))
                    {
                        Console.Error.WriteLine(
                            $"CVC-E2-TOPOLOGY: {familyId} {nativeSize}px mask {mask} failed.");
                        return false;
                    }
                }
            }
        }

        var fallbackAdjacency = result.Pack.Adjacencies.SingleOrDefault(
            static item => item.FamilyId == "transition.surface-water");
        if (fallbackAdjacency?.FallbackMask != 0 ||
            !result.Pack.TryResolve(
                "transition.surface-water", 16, 0, 0, out var fallbackHandle) ||
            !result.Pack.GetVisual(fallbackHandle).Tags.Contains(
                "ownership:primary-ground-plus-one-transition",
                StringComparer.Ordinal) ||
            result.Pack.TryResolve(
                "transition.surface-water", 16, 0, 3, out _))
        {
            Console.Error.WriteLine(
                "CVC-E2-FALLBACK: transition ownership/fallback was not compiled.");
            return false;
        }

        var motif = result.Pack.Motifs.SingleOrDefault(
            static item => item.FamilyId == "motif.grove");
        if (motif is null ||
            motif.Footprint != new PixelSize(2, 2) ||
            motif.AnchorCell != new PixelPoint(0, 1) ||
            motif.Marks.Length != 2 ||
            PackValidator.Validate(result.Pack).Length != 0)
        {
            Console.Error.WriteLine("CVC-E2-MOTIF: grove footprint/marks did not validate.");
            return false;
        }

        var evidence = result.ReviewFiles.Select(static file => file.Path).ToHashSet();
        if (!evidence.Contains("review/adjacency-16.png") ||
            !evidence.Contains("review/shifted-overlap-16.png") ||
            !evidence.Contains("review/motifs-16.png") ||
            !evidence.Contains("review/layers-16.png"))
        {
            Console.Error.WriteLine("CVC-E2-REVIEW: connected/motif/layer evidence is absent.");
            return false;
        }

        var reviewContent = result.ReviewFiles
            .Where(static file => file.Path.EndsWith("-16.png", StringComparison.Ordinal))
            .ToDictionary(static file => file.Path, static file => file.Bytes);
        var evidencePaths = new[]
        {
            "review/native-16.png",
            "review/adjacency-16.png",
            "review/shifted-overlap-16.png",
            "review/motifs-16.png",
            "review/layers-16.png"
        };
        if (evidencePaths
            .Select(path => Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(reviewContent[path].AsSpan())))
            .Distinct(StringComparer.Ordinal)
            .Count() != evidencePaths.Length)
        {
            Console.Error.WriteLine(
                "CVC-E2-REVIEW-CONTENT: specialized evidence sheets are aliases.");
            return false;
        }

        var missingVariantMask = CloneWith(
            result.Pack,
            result.Pack.Visuals.Where(static visual =>
                !(visual.FamilyId == "terrain.water" &&
                  visual.NativeSize == 20 &&
                  visual.VariantOrdinal == 1 &&
                  visual.AdjacencyMask == 15)));
        if (!PackValidator.Validate(missingVariantMask)
                .Any(static item => item.Code == "CVP-ADJ-002"))
        {
            Console.Error.WriteLine(
                "CVC-E2-VARIANT-COVERAGE: one variant omitted a required mask.");
            return false;
        }

        var missingWholeSize = CloneWith(
            result.Pack,
            result.Pack.Visuals.Where(static visual =>
                !(visual.FamilyId == "terrain.water" &&
                  visual.NativeSize == 20)));
        if (!PackValidator.Validate(missingWholeSize)
                .Any(static item => item.Code == "CVP-ADJ-002"))
        {
            Console.Error.WriteLine(
                "CVC-E2-SIZE-COVERAGE: a connected family omitted a required size.");
            return false;
        }

        result.Pack.TryResolve("terrain.water", 16, 0, 0, out var isolatedHandle);
        var isolated = result.Pack.GetVisual(isolatedHandle);
        var alteredAtlas = result.Pack.GetAtlasIndices(isolated.AtlasId).ToArray();
        var atlas = result.Pack.Atlases.Single(item => item.Id == isolated.AtlasId);
        var center = isolated.Rectangle.Width / 2;
        for (var y = 0; y <= center; y++)
        {
            alteredAtlas[
                (isolated.Rectangle.Y + y) * atlas.Width +
                isolated.Rectangle.X +
                center] = 1;
        }
        var falseConnection = CloneWith(
            result.Pack,
            result.Pack.Visuals,
            isolated.AtlasId,
            alteredAtlas);
        if (!PackValidator.Validate(falseConnection)
                .Any(static item => item.Code == "CVP-ADJ-004"))
        {
            Console.Error.WriteLine(
                "CVC-E2-UNSET-EDGE: an unset direction acquired a connection.");
            return false;
        }

        return true;
    }

    private static bool EdgesMatchMask(
        CompiledVisualPack pack,
        VisualRecord visual,
        int mask)
    {
        var atlas = pack.Atlases.Single(item => item.Id == visual.AtlasId);
        var pixels = pack.GetAtlasIndices(atlas.Id).ToArray();
        bool Occupied(int startX, int startY, int stepX, int stepY)
        {
            var length = stepX == 0
                ? visual.Rectangle.Height
                : visual.Rectangle.Width;
            for (var index = 0; index < length; index++)
            {
                var x = visual.Rectangle.X + startX + stepX * index;
                var y = visual.Rectangle.Y + startY + stepY * index;
                if (pixels[y * atlas.Width + x] != 0)
                {
                    return true;
                }
            }
            return false;
        }

        return Occupied(0, 0, 1, 0) == ((mask & 1) != 0) &&
               Occupied(visual.Rectangle.Width - 1, 0, 0, 1) ==
               ((mask & 2) != 0) &&
               Occupied(0, visual.Rectangle.Height - 1, 1, 0) ==
               ((mask & 4) != 0) &&
               Occupied(0, 0, 0, 1) == ((mask & 8) != 0);
    }

    private static CompiledVisualPack CloneWith(
        CompiledVisualPack pack,
        IEnumerable<VisualRecord> visuals,
        string? alteredAtlasId = null,
        byte[]? alteredAtlas = null) => new(
        pack.PackId,
        pack.Compatibility,
        pack.Compiler,
        pack.SourceDigest,
        pack.Palettes,
        pack.Atlases,
        visuals.ToImmutableArray(),
        pack.Motifs,
        pack.Adjacencies,
        pack.RequiredMappings,
        pack.RequiredNativeSizes,
        pack.Provenance,
        pack.Atlases.Select(atlas => KeyValuePair.Create(
            atlas.Id,
            atlas.Id == alteredAtlasId
                ? (ReadOnlyMemory<byte>)alteredAtlas!
                : pack.GetAtlasIndices(atlas.Id))),
        pack.PackDigest);
}
