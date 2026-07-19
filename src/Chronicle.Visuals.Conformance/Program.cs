using Chronicle.VisualPack;

// TDD E0 tracer: this intentionally does not compile until the public pack seam
// can round-trip a manually constructed version-1 pack deterministically.
return ReferencePackConformance.Run();

static class ReferencePackConformance
{
    public static int Run()
    {
        var pack = CompiledVisualPack.ReferenceFixture();
        var first = PackCodec.WriteCanonical(pack);
        var loaded = PackCodec.ReadCanonical(first);
        var second = PackCodec.WriteCanonical(loaded);

        if (!FilesEqual(first.CanonicalFiles, second.CanonicalFiles))
        {
            Console.Error.WriteLine("CVP-E0-REPRO: reference pack bytes changed after round-trip.");
            return 1;
        }

        if (PackValidator.Validate(loaded).Length != 0)
        {
            Console.Error.WriteLine("CVP-E0-VALIDATION: reference pack did not validate.");
            return 1;
        }

        if (!loaded.TryResolve("landmark.reference", 16, 0, null, out var handle) ||
            loaded.GetVisual(handle).Id != "landmark.reference")
        {
            Console.Error.WriteLine("CVP-E0-HANDLE: stable visual key did not resolve.");
            return 1;
        }

        var overlap = CloneWithOverlappingVisual(pack);
        if (!PackValidator.Validate(overlap).Any(static diagnostic => diagnostic.Code == "CVP-ATL-002"))
        {
            Console.Error.WriteLine("CVP-E0-OVERLAP: expected diagnostic CVP-ATL-002 was absent.");
            return 1;
        }

        var invalidTransform = CloneWithVisual(
            pack,
            pack.Visuals[0] with { AllowedTransforms = (TransformFlags)8 });
        if (!PackValidator.Validate(invalidTransform)
                .Any(static diagnostic => diagnostic.Code == "CVP-VIS-002"))
        {
            Console.Error.WriteLine("CVP-E0-TRANSFORM: expected diagnostic CVP-VIS-002 was absent.");
            return 1;
        }

        var duplicateVariant = CloneWithOverlappingVisual(pack);
        if (!PackValidator.Validate(duplicateVariant)
                .Any(static diagnostic => diagnostic.Code == "CVP-VAR-001"))
        {
            Console.Error.WriteLine("CVP-E0-VARIANT: expected diagnostic CVP-VAR-001 was absent.");
            return 1;
        }

        var missingNativeSize = CloneWithNativeSizes(
            pack,
            System.Collections.Immutable.ImmutableArray.Create(16, 20));
        if (!PackValidator.Validate(missingNativeSize)
                .Any(static diagnostic => diagnostic.Code == "CVP-VIS-003"))
        {
            Console.Error.WriteLine("CVP-E0-NATIVE-SIZE: expected diagnostic CVP-VIS-003 was absent.");
            return 1;
        }

        var missingAdjacency = CloneWithAdjacency(
            pack,
            System.Collections.Immutable.ImmutableArray.Create(
                new AdjacencyRecord(
                    "landmark.reference",
                    System.Collections.Immutable.ImmutableArray.Create(0, 1),
                    null,
                    false)));
        if (!PackValidator.Validate(missingAdjacency)
                .Any(static diagnostic => diagnostic.Code == "CVP-ADJ-002"))
        {
            Console.Error.WriteLine("CVP-E0-ADJACENCY: expected diagnostic CVP-ADJ-002 was absent.");
            return 1;
        }

        var disconnected = CloneWithBuffer(
            pack,
            pack.Visuals[0] with { RequireConnected = true },
            new byte[]
            {
                1, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 1
            });
        if (!PackValidator.Validate(disconnected)
                .Any(static diagnostic => diagnostic.Code == "CVP-OCC-001"))
        {
            Console.Error.WriteLine("CVP-E0-OCCUPANCY: expected diagnostic CVP-OCC-001 was absent.");
            return 1;
        }

        var invalidAnchor = CloneWithVisual(
            pack,
            pack.Visuals[0] with { Anchor = new PixelPoint(-1, 0) });
        if (!PackValidator.Validate(invalidAnchor)
                .Any(static diagnostic => diagnostic.Code == "CVP-VIS-004"))
        {
            Console.Error.WriteLine("CVP-E0-ANCHOR: expected diagnostic CVP-VIS-004 was absent.");
            return 1;
        }

        var invalidLayer = CloneWithVisual(
            pack,
            pack.Visuals[0] with { Layer = (VisualLayer)999 });
        if (!PackValidator.Validate(invalidLayer)
                .Any(static diagnostic => diagnostic.Code == "CVP-VIS-005"))
        {
            Console.Error.WriteLine("CVP-E0-LAYER: expected diagnostic CVP-VIS-005 was absent.");
            return 1;
        }

        var tamperedFiles = first.Files.Select(file =>
            file.Path == "atlases/reference-16.indices"
                ? new PackFile(file.Path, new byte[file.Bytes.Length])
                : file);
        try
        {
            _ = PackCodec.ReadCanonical(tamperedFiles);
            Console.Error.WriteLine("CVP-E0-DIGEST: tampered atlas was accepted.");
            return 1;
        }
        catch (FormatException exception) when (exception.Message.StartsWith(
                   "CVP-DIG-001",
                   StringComparison.Ordinal))
        {
            // Expected public reader rejection.
        }

        try
        {
            _ = PackCodec.ReadCanonical(TamperManifestHash(first));
            Console.Error.WriteLine("CVP-E0-HASH-DOCUMENT: tampered hashes.json was accepted.");
            return 1;
        }
        catch (FormatException exception) when (exception.Message.StartsWith(
                   "CVP-DIG-006",
                   StringComparison.Ordinal))
        {
            // Expected public reader rejection.
        }

        if (!InvalidPackConformance.Run(pack, first))
        {
            return 1;
        }

        if (!CompilerConformance.Run())
        {
            return 1;
        }

        if (!ConnectedConformance.Run())
        {
            return 1;
        }

        if (!VocabularyConformance.Run())
        {
            return 1;
        }

        if (!Palimpsest20PackConformance.Run())
        {
            return 1;
        }

        if (!Palimpsest20CompilerConformance.Run())
        {
            return 1;
        }

        if (!Palimpsest20BundleConformance.Run())
        {
            return 1;
        }

        if (!MotifDeterminismConformance.Run())
        {
            return 1;
        }

        Console.WriteLine(first.AggregateDigest);
        return 0;
    }

    private static CompiledVisualPack CloneWithOverlappingVisual(CompiledVisualPack pack) => new(
        pack.PackId,
        pack.Compatibility,
        pack.Compiler,
        pack.SourceDigest,
        pack.Palettes,
        pack.Atlases,
        pack.Visuals.Add(pack.Visuals[0] with { Id = "landmark.reference.overlap" }),
        pack.Motifs,
        pack.Adjacencies,
        pack.RequiredMappings,
        pack.RequiredNativeSizes,
        pack.Provenance,
        pack.Atlases.Select(atlas =>
            KeyValuePair.Create(atlas.Id, pack.GetAtlasIndices(atlas.Id))),
        pack.PackDigest);

    private static CompiledVisualPack CloneWithVisual(
        CompiledVisualPack pack,
        VisualRecord visual) => new(
        pack.PackId,
        pack.Compatibility,
        pack.Compiler,
        pack.SourceDigest,
        pack.Palettes,
        pack.Atlases,
        System.Collections.Immutable.ImmutableArray.Create(visual),
        pack.Motifs,
        pack.Adjacencies,
        pack.RequiredMappings,
        pack.RequiredNativeSizes,
        pack.Provenance,
        pack.Atlases.Select(atlas =>
            KeyValuePair.Create(atlas.Id, pack.GetAtlasIndices(atlas.Id))),
        pack.PackDigest);

    private static CompiledVisualPack CloneWithNativeSizes(
        CompiledVisualPack pack,
        System.Collections.Immutable.ImmutableArray<int> requiredNativeSizes) => new(
        pack.PackId,
        pack.Compatibility,
        pack.Compiler,
        pack.SourceDigest,
        pack.Palettes,
        pack.Atlases,
        pack.Visuals,
        pack.Motifs,
        pack.Adjacencies,
        pack.RequiredMappings,
        requiredNativeSizes,
        pack.Provenance,
        pack.Atlases.Select(atlas =>
            KeyValuePair.Create(atlas.Id, pack.GetAtlasIndices(atlas.Id))),
        pack.PackDigest);

    private static CompiledVisualPack CloneWithAdjacency(
        CompiledVisualPack pack,
        System.Collections.Immutable.ImmutableArray<AdjacencyRecord> adjacencies) => new(
        pack.PackId,
        pack.Compatibility,
        pack.Compiler,
        pack.SourceDigest,
        pack.Palettes,
        pack.Atlases,
        pack.Visuals,
        pack.Motifs,
        adjacencies,
        pack.RequiredMappings,
        pack.RequiredNativeSizes,
        pack.Provenance,
        pack.Atlases.Select(atlas =>
            KeyValuePair.Create(atlas.Id, pack.GetAtlasIndices(atlas.Id))),
        pack.PackDigest);

    private static CompiledVisualPack CloneWithBuffer(
        CompiledVisualPack pack,
        VisualRecord visual,
        byte[] buffer) => new(
        pack.PackId,
        pack.Compatibility,
        pack.Compiler,
        pack.SourceDigest,
        pack.Palettes,
        pack.Atlases,
        System.Collections.Immutable.ImmutableArray.Create(visual),
        pack.Motifs,
        pack.Adjacencies,
        pack.RequiredMappings,
        pack.RequiredNativeSizes,
        pack.Provenance,
        new[]
        {
            KeyValuePair.Create(
                pack.Atlases[0].Id,
                (ReadOnlyMemory<byte>)buffer)
        },
        pack.PackDigest);

    private static bool FilesEqual(
        IReadOnlyDictionary<string, System.Collections.Immutable.ImmutableArray<byte>> left,
        IReadOnlyDictionary<string, System.Collections.Immutable.ImmutableArray<byte>> right) =>
        left.Count == right.Count &&
        left.OrderBy(static item => item.Key, StringComparer.Ordinal)
            .Zip(right.OrderBy(static item => item.Key, StringComparer.Ordinal))
            .All(static pair =>
                pair.First.Key == pair.Second.Key &&
                pair.First.Value.AsSpan().SequenceEqual(pair.Second.Value.AsSpan()));

    private static IEnumerable<PackFile> TamperManifestHash(CanonicalPackOutput canonical)
    {
        const string zero =
            "sha256:0000000000000000000000000000000000000000000000000000000000000000";
        foreach (var file in canonical.Files)
        {
            if (file.Path != "hashes.json")
            {
                yield return file;
                continue;
            }

            var node = System.Text.Json.Nodes.JsonNode.Parse(file.Bytes.AsSpan())
                ?? throw new InvalidOperationException("Reference hashes.json could not be parsed.");
            node["normalizedManifestDigest"] = zero;
            yield return new PackFile(
                file.Path,
                System.Text.Encoding.UTF8.GetBytes(node.ToJsonString() + "\n"));
        }
    }
}
