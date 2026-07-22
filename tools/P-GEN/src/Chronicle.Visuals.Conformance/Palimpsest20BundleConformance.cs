using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicle.VisualCompiler;
using Chronicle.VisualPack;

static class Palimpsest20BundleConformance
{
    private const string CatalogueResource =
        "Chronicle.Visuals.Conformance.palimpsest20.catalogue.json";
    private const string InvalidCasesResource =
        "Chronicle.Visuals.Conformance.palimpsest20.invalid.cases.json";
    private const string ExpectedHashesResource =
        "Chronicle.Visuals.Conformance.palimpsest20.expected-hashes.json";

    private static readonly JsonSerializerOptions FixtureJson = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static bool Run()
    {
        var passed = RejectMalformedBundles();
        return CompileTwiceProducesIdenticalCanonicalFiles() && passed;
    }

    private static ImmutableArray<string> RegisteredMutations { get; } =
        ImmutableArray.Create(
            "missing",
            "extra",
            "duplicate",
            "unmapped-json",
            "duplicate-json",
            "missing-validation-member",
            "null-manifest-atlas",
            "null-manifest-definition",
            "null-hash-files",
            "null-minimum-reader",
            "oversized-atlas-dimensions",
            "manifest-atlas-path",
            "malformed-json",
            "empty-json",
            "invalid-cell-size",
            "unaligned-atlas-dimension",
            "atlas-length",
            "empty-palette",
            "oversized-palette",
            "opaque-transparent-index",
            "invalid-atlas-index",
            "invalid-palette-role-name",
            "duplicate-palette-role-name",
            "palette-role-index-range",
            "invalid-visual-id",
            "duplicate-visual-id",
            "invalid-family-id",
            "negative-variant",
            "invalid-layer",
            "invalid-rectangle-size",
            "unaligned-rectangle",
            "out-of-bounds-rectangle",
            "overlapping-rectangles",
            "invalid-anchor",
            "invalid-adjacency",
            "overview-index-range",
            "definition-role-index-range",
            "invisible-definition",
            "hash-algorithm",
            "hash-file-digest",
            "hash-file-order",
            "hash-aggregate",
            "manifest-palimpsest-digest",
            "hashes-palimpsest-digest",
            "noncanonical-json",
            "hash-mismatch",
            "format-boundary",
            "composer-boundary",
            "style-boundary",
            "minimum-reader-boundary");

    private static bool RejectMalformedBundles()
    {
        var cases = ReadInvalidCases();
        ValidateCaseRegistry(cases);
        var canonical = CreateCanonicalFixture();
        var passed = true;
        foreach (var testCase in cases)
        {
            passed &= Rejects(
                testCase,
                Mutate(canonical.Files, testCase.Mutation));
        }

        return passed;
    }

    private static void ValidateCaseRegistry(ImmutableArray<InvalidCase> fixtureCases)
    {
        var fixtureIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var testCase in fixtureCases)
        {
            if (string.IsNullOrWhiteSpace(testCase.Id))
            {
                throw new InvalidOperationException(
                    "PAL20-BUNDLE-FIXTURE: invalid case has a blank ID.");
            }
            if (!fixtureIds.Add(testCase.Id))
            {
                throw new InvalidOperationException(
                    $"PAL20-BUNDLE-FIXTURE: duplicate case ID '{testCase.Id}'.");
            }
            if (!RegisteredMutations.Contains(testCase.Mutation))
            {
                throw new InvalidOperationException(
                    $"PAL20-BUNDLE-FIXTURE: case '{testCase.Id}' references unknown mutation '{testCase.Mutation}'.");
            }
        }

        var registeredSet = RegisteredMutations.ToHashSet(StringComparer.Ordinal);
        var missingFromFixture = registeredSet
            .Where(mutation => !fixtureCases.Any(
                testCase => testCase.Mutation == mutation))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (missingFromFixture.Length > 0)
        {
            throw new InvalidOperationException(
                $"PAL20-BUNDLE-FIXTURE: registered mutations missing from fixture: {string.Join(", ", missingFromFixture)}.");
        }
    }

    private static bool CompileTwiceProducesIdenticalCanonicalFiles()
    {
        var catalogue = VisualCatalogue.ParseJson(ReadEmbeddedBytes(CatalogueResource));
        var first = VisualCompiler.CompilePalimpsest20(
            catalogue,
            new CompilationOptions(ReviewMode.None));
        var second = VisualCompiler.CompilePalimpsest20(
            catalogue,
            new CompilationOptions(ReviewMode.None));
        if (!first.Succeeded || first.Pack is null || first.Validation is null ||
            !second.Succeeded || second.Pack is null || second.Validation is null)
        {
            Console.Error.WriteLine(
                "PAL20-BUNDLE-COMPILE: repeat compilation did not produce two validated Palimpsest20 packs.");
            return false;
        }

        var firstBundle = Palimpsest20Codec.WriteCanonical(first.Pack, first.Validation);
        var secondBundle = Palimpsest20Codec.WriteCanonical(second.Pack, second.Validation);
        if (!FilesEqual(firstBundle.CanonicalFiles, secondBundle.CanonicalFiles))
        {
            Console.Error.WriteLine(
                "PAL20-BUNDLE-DETERMINISM: repeat compilation changed one or more canonical bundle bytes.");
            return false;
        }

        var expected = JsonSerializer.Deserialize<ExpectedHashesDocument>(
            ReadEmbeddedBytes(ExpectedHashesResource),
            FixtureJson)
            ?? throw new InvalidOperationException(
                "PAL20-BUNDLE-FIXTURE: expected-hashes resource is empty.");
        var actualFiles = firstBundle.Files.ToDictionary(
            static file => file.Path,
            static file => PackDigests.Bytes(file.Bytes.AsSpan()),
            StringComparer.Ordinal);
        var expectedFiles = expected.Files.ToDictionary(
            static file => file.Path,
            static file => file.Digest,
            StringComparer.Ordinal);
        if (expected.Profile != "Palimpsest20" ||
            !actualFiles.OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .SequenceEqual(
                    expectedFiles.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) ||
            first.Pack.Digest != expected.PalimpsestDigest ||
            firstBundle.AggregateDigest != expected.AggregateDigest)
        {
            Console.Error.WriteLine(
                "PAL20-BUNDLE-PINNED: canonical output no longer matches committed expected hashes.");
            return false;
        }

        return true;
    }

    private static CanonicalPackOutput CreateCanonicalFixture()
    {
        var pack = new Palimpsest20Pack(
            packId: "palimpsest20.bundle-tracer",
            formatVersion: 1,
            styleVersion: Palimpsest20Pack.SupportedStyleVersion,
            composerVersion: Palimpsest20Pack.SupportedComposerVersion,
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
        return Palimpsest20Codec.WriteCanonical(pack, new Palimpsest20Validation(
            PackFormatVersion: 1,
            ComposerContractVersion: Palimpsest20Pack.SupportedComposerVersion,
            VisualStyleVersion: Palimpsest20Pack.SupportedStyleVersion,
            MinimumReaderVersion: Palimpsest20Codec.ReaderVersion));
    }

    private static ImmutableArray<PackFile> Mutate(
        ImmutableArray<PackFile> canonical,
        string mutation) => mutation switch
    {
        "missing" => canonical.Where(
                static file => file.Path != "validation.json")
            .ToImmutableArray(),
        "extra" => canonical.Add(new PackFile("unexpected.json", "{}\n"u8)),
        "duplicate" => canonical.Add(canonical.Single(
            static file => file.Path == "manifest.json")),
        "unmapped-json" => ReplaceFile(
            canonical,
            "validation.json",
            "{\"packFormatVersion\":1,\"composerContractVersion\":1,\"visualStyleVersion\":1,\"minimumReaderVersion\":\"1.0.0\",\"unexpected\":true}\n"u8),
        "duplicate-json" => ReplaceFile(
            canonical,
            "validation.json",
            "{\"packFormatVersion\":1,\"packFormatVersion\":1,\"composerContractVersion\":1,\"visualStyleVersion\":1,\"minimumReaderVersion\":\"1.0.0\"}\n"u8),
        "missing-validation-member" => ReplaceFile(
            canonical,
            "validation.json",
            "{\"packFormatVersion\":1,\"composerContractVersion\":1,\"visualStyleVersion\":1}\n"u8),
        "null-manifest-atlas" => RewriteManifest(
            canonical,
            static manifest => manifest["atlas"] = null),
        "null-manifest-definition" => RewriteManifest(
            canonical,
            static manifest =>
                manifest["definitions"]!.AsArray()[0] = null),
        "null-hash-files" => RewriteJson(
            canonical,
            "hashes.json",
            static hashes => hashes["files"] = null,
            rewriteHashes: false),
        "null-minimum-reader" => RewriteValidation(
            canonical,
            static validation => validation["minimumReaderVersion"] = null),
        "oversized-atlas-dimensions" => RewriteManifest(
            canonical,
            static manifest =>
            {
                const int alignedOverflowingDimension = 2_147_483_640;
                manifest["atlas"]!["width"] = alignedOverflowingDimension;
                manifest["atlas"]!["height"] = alignedOverflowingDimension;
            }),
        "manifest-atlas-path" => RewriteManifest(
            canonical,
            static manifest => manifest["atlas"]!["path"] = "atlas.indices"),
        "malformed-json" => ReplaceFile(canonical, "validation.json", "{"u8),
        "empty-json" => ReplaceFile(
            canonical,
            "validation.json",
            Array.Empty<byte>()),
        "invalid-cell-size" => RewriteManifest(
            canonical,
            static manifest => manifest["cellSize"] = 10),
        "unaligned-atlas-dimension" => RewriteManifest(
            canonical,
            static manifest => manifest["atlas"]!["width"] = 21),
        "atlas-length" => RewriteHashes(ReplaceFile(
            canonical,
            Palimpsest20Codec.AtlasPath,
            new byte[399])),
        "empty-palette" => RewriteManifest(
            canonical,
            static manifest => manifest["palette"]!["entries"] = new JsonArray()),
        "oversized-palette" => RewriteManifest(
            canonical,
            static manifest =>
            {
                var entries = manifest["palette"]!["entries"]!.AsArray();
                while (entries.Count <= 256)
                {
                    entries.Add(new JsonObject
                    {
                        ["red"] = 1,
                        ["green"] = 1,
                        ["blue"] = 1,
                        ["alpha"] = 255,
                    });
                }
            }),
        "opaque-transparent-index" => RewriteManifest(
            canonical,
            static manifest => manifest["palette"]!["entries"]!
                .AsArray()[0]!["alpha"] = 255),
        "invalid-atlas-index" => RewriteHashes(ReplaceFile(
            canonical,
            Palimpsest20Codec.AtlasPath,
            Enumerable.Repeat((byte)2, 400).ToArray())),
        "invalid-palette-role-name" => RewriteManifest(
            canonical,
            static manifest => manifest["palette"]!["roleIndexes"]!
                .AsArray()[0]!["name"] = "Surface.Grass"),
        "duplicate-palette-role-name" => RewriteManifest(
            canonical,
            static manifest => manifest["palette"]!["roleIndexes"]!.AsArray().Add(
                manifest["palette"]!["roleIndexes"]!.AsArray()[0]!.DeepClone())),
        "palette-role-index-range" => RewriteManifest(
            canonical,
            static manifest => manifest["palette"]!["roleIndexes"]!
                .AsArray()[0]!["index"] = 2),
        "invalid-visual-id" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["visualId"] = "Terrain.Surface.Grass"),
        "duplicate-visual-id" => RewriteManifest(
            canonical,
            static manifest => AddDefinition(manifest, static _ => { })),
        "invalid-family-id" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["familyId"] = "Terrain.Surface.Grass"),
        "negative-variant" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["variantOrdinal"] = -1),
        "invalid-layer" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["layerClass"] = 99),
        "invalid-rectangle-size" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["atlasRect"]!["width"] = 19),
        "unaligned-rectangle" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["atlasRect"]!["x"] = 1),
        "out-of-bounds-rectangle" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["atlasRect"]!["x"] = 20),
        "overlapping-rectangles" => RewriteManifest(
            canonical,
            static manifest => AddDefinition(
                manifest,
                static definition => definition["visualId"] = "terrain.surface.moss")),
        "invalid-anchor" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["anchor"]!["x"] = 9),
        "invalid-adjacency" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["adjacencyMask"] = 16),
        "overview-index-range" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["overviewPaletteIndex"] = 2),
        "definition-role-index-range" => RewriteManifest(
            canonical,
            static manifest => FirstDefinition(manifest)["paletteRoleIndexes"]!
                .AsArray()[0] = 2),
        "invisible-definition" => RewriteHashes(ReplaceFile(
            canonical,
            Palimpsest20Codec.AtlasPath,
            new byte[400])),
        "hash-algorithm" => RewriteJson(
            canonical,
            "hashes.json",
            static hashes => hashes["algorithm"] = "sha512",
            rewriteHashes: false),
        "hash-file-digest" => RewriteJson(
            canonical,
            "hashes.json",
            static hashes => hashes["files"]!.AsArray()[0]!["digest"] =
                "sha256:0000000000000000000000000000000000000000000000000000000000000000",
            rewriteHashes: false),
        "hash-file-order" => RewriteJson(
            canonical,
            "hashes.json",
            static hashes =>
            {
                var files = hashes["files"]!.AsArray();
                var first = files[0]!.DeepClone();
                files[0] = files[1]!.DeepClone();
                files[1] = first;
            },
            rewriteHashes: false),
        "hash-aggregate" => RewriteJson(
            canonical,
            "hashes.json",
            static hashes => hashes["aggregateDigest"] =
                "sha256:0000000000000000000000000000000000000000000000000000000000000000",
            rewriteHashes: false),
        "manifest-palimpsest-digest" => RewriteManifest(
            canonical,
            static manifest => manifest["palimpsestDigest"] =
                "sha256:0000000000000000000000000000000000000000000000000000000000000000"),
        "hashes-palimpsest-digest" => RewriteJson(
            canonical,
            "hashes.json",
            static hashes => hashes["palimpsestDigest"] =
                "sha256:0000000000000000000000000000000000000000000000000000000000000000",
            rewriteHashes: false),
        "noncanonical-json" => RewriteHashes(ReplaceFile(
            canonical,
            "manifest.json",
            " "u8.ToArray().Concat(canonical.Single(
                static file => file.Path == "manifest.json").Bytes).ToArray())),
        "hash-mismatch" => ReplaceFile(
            canonical,
            Palimpsest20Codec.AtlasPath,
            new byte[400]),
        "format-boundary" => RewriteValidation(
            canonical,
            static validation => validation["packFormatVersion"] = 2),
        "composer-boundary" => RewriteValidation(
            canonical,
            static validation => validation["composerContractVersion"] = 3),
        "style-boundary" => RewriteValidation(
            canonical,
            static validation => validation["visualStyleVersion"] = 3),
        "minimum-reader-boundary" => RewriteValidation(
            canonical,
            static validation => validation["minimumReaderVersion"] = "2.0.1"),
        _ => throw new InvalidOperationException(
            $"PAL20-BUNDLE-FIXTURE: unknown mutation '{mutation}'.")
    };

    private static JsonObject FirstDefinition(JsonObject manifest) =>
        manifest["definitions"]!.AsArray()[0]!.AsObject();

    private static void AddDefinition(
        JsonObject manifest,
        Action<JsonObject> mutation)
    {
        var definition = FirstDefinition(manifest).DeepClone().AsObject();
        mutation(definition);
        manifest["definitions"]!.AsArray().Add(definition);
    }

    private static ImmutableArray<PackFile> RewriteManifest(
        ImmutableArray<PackFile> canonical,
        Action<JsonObject> mutation) =>
        RewriteJson(canonical, "manifest.json", mutation, rewriteHashes: true);

    private static ImmutableArray<PackFile> RewriteValidation(
        ImmutableArray<PackFile> canonical,
        Action<JsonObject> mutation) =>
        RewriteJson(canonical, "validation.json", mutation, rewriteHashes: true);

    private static ImmutableArray<PackFile> RewriteJson(
        ImmutableArray<PackFile> canonical,
        string path,
        Action<JsonObject> mutation,
        bool rewriteHashes)
    {
        var validation = JsonNode.Parse(canonical.Single(
            file => file.Path == path).Bytes.AsSpan())
            as JsonObject
            ?? throw new InvalidOperationException(
                $"PAL20-BUNDLE-FIXTURE: '{path}' fixture is not an object.");
        mutation(validation);
        var rewritten = ReplaceFile(
            canonical,
            path,
            Encoding.UTF8.GetBytes(validation.ToJsonString() + "\n"));
        return rewriteHashes ? RewriteHashes(rewritten) : rewritten;
    }

    private static ImmutableArray<PackFile> RewriteHashes(
        ImmutableArray<PackFile> files)
    {
        var manifest = files.Single(static file => file.Path == "manifest.json");
        var atlas = files.Single(static file => file.Path == Palimpsest20Codec.AtlasPath);
        var validation = files.Single(static file => file.Path == "validation.json");
        var originalHashes = JsonNode.Parse(files.Single(
            static file => file.Path == "hashes.json").Bytes.AsSpan())
            as JsonObject
            ?? throw new InvalidOperationException(
                "PAL20-BUNDLE-FIXTURE: hashes fixture is not an object.");
        var hashFiles = new[]
        {
            (atlas.Path, PackDigests.Bytes(atlas.Bytes.AsSpan())),
            (manifest.Path, PackDigests.Bytes(manifest.Bytes.AsSpan())),
            (validation.Path, PackDigests.Bytes(validation.Bytes.AsSpan()))
        };
        var hashes = new JsonObject
        {
            ["algorithm"] = "sha256",
            ["files"] = new JsonArray(hashFiles.Select(static file =>
                (JsonNode)new JsonObject
                {
                    ["path"] = file.Path,
                    ["digest"] = file.Item2,
                }).ToArray()),
            ["palimpsestDigest"] = originalHashes["palimpsestDigest"]?.GetValue<string>(),
            ["aggregateDigest"] = PackDigests.Aggregate(hashFiles.Select(
                static file => ("file", file.Path, file.Item2)))
        };
        return ReplaceFile(
            files,
            "hashes.json",
            Encoding.UTF8.GetBytes(hashes.ToJsonString() + "\n"));
    }

    private static ImmutableArray<PackFile> ReplaceFile(
        ImmutableArray<PackFile> files,
        string path,
        ReadOnlySpan<byte> bytes)
    {
        var copy = bytes.ToArray();
        return files.Select(file => file.Path == path
            ? new PackFile(path, copy)
            : file).ToImmutableArray();
    }

    private static bool Rejects(InvalidCase testCase, ImmutableArray<PackFile> files)
    {
        try
        {
            _ = Palimpsest20Codec.ReadCanonical(files);
            Console.Error.WriteLine(
                $"PAL20-BUNDLE-{testCase.Id}: expected {testCase.ExpectedCode} rejection was absent.");
            return false;
        }
        catch (FormatException exception) when (exception.Message.StartsWith(
                   testCase.ExpectedCode,
                   StringComparison.Ordinal))
        {
            return true;
        }
    }

    private static ImmutableArray<InvalidCase> ReadInvalidCases()
    {
        var document = JsonSerializer.Deserialize<InvalidCasesDocument>(
            ReadEmbeddedBytes(InvalidCasesResource),
            FixtureJson)
            ?? throw new InvalidOperationException(
                "PAL20-BUNDLE-FIXTURE: invalid-cases resource is empty.");
        if (document.Cases.IsDefaultOrEmpty ||
            document.Cases.Any(static testCase =>
                string.IsNullOrWhiteSpace(testCase.Id) ||
                string.IsNullOrWhiteSpace(testCase.Mutation) ||
                string.IsNullOrWhiteSpace(testCase.ExpectedCode)))
        {
            throw new InvalidOperationException(
                "PAL20-BUNDLE-FIXTURE: invalid-cases resource is malformed.");
        }

        return document.Cases;
    }

    private static byte[] ReadEmbeddedBytes(string logicalName)
    {
        using var stream = typeof(Palimpsest20BundleConformance).Assembly
            .GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException(
                $"PAL20-BUNDLE-FIXTURE: embedded resource '{logicalName}' is absent.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }

    private static bool FilesEqual(
        IReadOnlyDictionary<string, ImmutableArray<byte>> left,
        IReadOnlyDictionary<string, ImmutableArray<byte>> right) =>
        left.Count == right.Count &&
        left.All(pair => right.TryGetValue(pair.Key, out var bytes) &&
            pair.Value.AsSpan().SequenceEqual(bytes.AsSpan()));

    private sealed record InvalidCasesDocument(ImmutableArray<InvalidCase> Cases);
    private sealed record InvalidCase(string Id, string Mutation, string ExpectedCode);
    private sealed record ExpectedHashesDocument(
        string Profile,
        ImmutableArray<ExpectedHashFile> Files,
        string PalimpsestDigest,
        string AggregateDigest);
    private sealed record ExpectedHashFile(string Path, string Digest);
}
