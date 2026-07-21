using Chronicle.VisualCompiler;
using Chronicle.VisualPack;

static class VocabularyConformance
{
    private static readonly byte[] Source = """
        {
          "schemaVersion": 1,
          "packId": "chronicle.e3.tracer",
          "visualStyleVersion": 1,
          "palettes": [
            {
              "id": "surface",
              "entries": ["00000000", "e8c45cff"],
              "roles": { "figure.ink": 1 }
            },
            {
              "id": "sky",
              "entries": ["00000000", "b9dce8ff"],
              "roles": { "figure.ink": 1 }
            }
          ],
          "families": [
            {
              "id": "actor.incarnation",
              "seed": 703,
              "variantCount": 2,
              "targets": [
                {
                  "nativeSize": 16,
                  "rows": ["010", "111", "010", "111"],
                  "anchor": [1, 3],
                  "layer": "actor",
                  "paletteRoles": ["figure.ink"]
                }
              ]
            },
            {
              "id": "baseline.actor.incarnation",
              "seed": 704,
              "variantCount": 1,
              "targets": [
                {
                  "nativeSize": 16,
                  "rows": ["010", "111", "010", "111"],
                  "anchor": [1, 3],
                  "layer": "actor",
                  "paletteRoles": ["figure.ink"]
                }
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
            Console.Error.WriteLine("CVC-E3-PALETTES: two-palette compile failed.");
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.Error.WriteLine(
                    $"{diagnostic.Code} {diagnostic.Subject}: {diagnostic.Message}");
            }
            return false;
        }

        var paletteIds = result.Pack.Palettes
            .Select(static palette => palette.Id)
            .ToArray();
        if (!paletteIds.SequenceEqual(new[] { "sky", "surface" }) ||
            result.Pack.Atlases.Any(atlas =>
                !atlas.CompatiblePalettes.SequenceEqual(paletteIds)))
        {
            Console.Error.WriteLine(
                "CVC-E3-COMPATIBILITY: atlas is not compatible with both palettes.");
            return false;
        }
        var candidateProvenance = result.Pack.Provenance.Single(
            static item => item.FamilyId == "actor.incarnation");
        if (candidateProvenance.Origin != "authored")
        {
            Console.Error.WriteLine(
                "CVC-E3-PROVENANCE: actor incarnation is not authored.");
            return false;
        }
        var baselineProvenance = result.Pack.Provenance.Single(
            static item => item.FamilyId == "baseline.actor.incarnation");
        if (baselineProvenance.Origin != "authored")
        {
            Console.Error.WriteLine(
                "CVC-E3-PROVENANCE: baseline provenance is not authored.");
            return false;
        }

        var recoloured = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(Source.AsSpan().ToArray()
                .ReplaceUtf8("e8c45cff", "8ac46cff")
                .ReplaceUtf8("b9dce8ff", "d0a8e8ff")),
            new CompilationOptions(ReviewMode.None));
        if (!recoloured.Succeeded ||
            !result.Pack.Visuals.Select(static visual => visual.GeometryDigest)
                .SequenceEqual(recoloured.Pack!.Visuals.Select(
                    static visual => visual.GeometryDigest)))
        {
            Console.Error.WriteLine(
                "CVC-E3-GEOMETRY: palette colour edits changed geometry.");
            return false;
        }

        var reviewPaths = result.ReviewFiles
            .Select(static file => file.Path)
            .ToHashSet(StringComparer.Ordinal);
        if (!reviewPaths.Contains("review/palette-sky-16.png") ||
            !reviewPaths.Contains("review/palette-surface-16.png") ||
            !reviewPaths.Contains("review/variants-16.png") ||
            !reviewPaths.Contains("review/authoring-evidence.json"))
        {
            Console.Error.WriteLine(
                "CVC-E3-REVIEW: palette/variant/manual evidence is incomplete.");
            return false;
        }

        using var specimenStream = typeof(VocabularyConformance).Assembly
            .GetManifestResourceStream("Chronicle.Visuals.Conformance.e3.json");
        if (specimenStream is null)
        {
            Console.Error.WriteLine("CVC-E3-SPECIMENS: embedded E3 catalogue is absent.");
            return false;
        }
        using var specimenBytes = new MemoryStream();
        specimenStream.CopyTo(specimenBytes);
        var specimens = VisualCompiler.Compile(
            VisualCatalogue.ParseJson(specimenBytes.ToArray()),
            new CompilationOptions(ReviewMode.Standard));
        var required = new[]
        {
            "actor.incarnation",
            "actor.incarnation.corpse",
            "subject.loose-stone",
            "landmark.bell-that-fell-up",
            "object.material.wood",
            "object.material.iron",
            "object.material.glass",
            "glyph.codex",
            "glyph.loadout",
            "glyph.map",
            "glyph.status",
            "emphasis.target",
            "emphasis.selection",
            "baseline.actor.incarnation",
            "baseline.subject.loose-stone",
            "baseline.landmark.bell-that-fell-up"
        };
        if (!specimens.Succeeded ||
            specimens.Pack is null ||
            required.Any(id => !specimens.Pack.RequiredMappings.Contains(
                id,
                StringComparer.Ordinal)) ||
            specimens.Pack.Palettes.Select(static item => item.Id)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(new[] { "surface", "sky" }) is false)
        {
            Console.Error.WriteLine(
                "CVC-E3-SPECIMENS: required Palimpsest-shaped vocabulary is incomplete.");
            return false;
        }

        foreach (var group in specimens.Pack.Visuals
                     .Where(static visual =>
                         visual.AdjacencyMask is null &&
                         visual.VariantOrdinal > 0)
                     .Select(static visual => (visual.FamilyId, visual.NativeSize))
                     .Distinct())
        {
            var familyGeometry = specimens.Pack.Visuals
                .Where(visual =>
                    visual.FamilyId == group.FamilyId &&
                    visual.NativeSize == group.NativeSize)
                .Select(static visual => visual.GeometryDigest)
                .ToArray();
            if (familyGeometry.Distinct(StringComparer.Ordinal).Count() !=
                familyGeometry.Length)
            {
                Console.Error.WriteLine(
                    $"CVC-E3-LINEAGE: {group.FamilyId} variants collapsed.");
                return false;
            }
        }

        return true;
    }

    private static byte[] ReplaceUtf8(
        this byte[] source,
        string oldValue,
        string newValue) =>
        System.Text.Encoding.UTF8.GetBytes(
            System.Text.Encoding.UTF8.GetString(source)
                .Replace(oldValue, newValue, StringComparison.Ordinal));
}
