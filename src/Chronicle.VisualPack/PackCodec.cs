using System.Collections.Immutable;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicle.VisualPack;

public static class PackCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Default,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static CanonicalPackOutput WriteCanonical(CompiledVisualPack source)
    {
        var diagnostics = PackValidator.Validate(source);
        var manifest = new ManifestDocument(
            source.PackId,
            source.Compatibility,
            source.Compiler,
            source.SourceDigest,
            source.Palettes.OrderBy(static item => item.Id, StringComparer.Ordinal).ToImmutableArray(),
            source.Atlases.OrderBy(static item => item.Id, StringComparer.Ordinal).ToImmutableArray(),
            source.Visuals
                .OrderBy(static item => item.Id, StringComparer.Ordinal)
                .ThenBy(static item => item.NativeSize)
                .ThenBy(static item => item.VariantOrdinal)
                .ThenBy(static item => item.AdjacencyMask ?? -1)
                .ToImmutableArray(),
            source.Motifs.OrderBy(static item => item.FamilyId, StringComparer.Ordinal).ToImmutableArray(),
            source.Adjacencies.OrderBy(static item => item.FamilyId, StringComparer.Ordinal).ToImmutableArray(),
            source.RequiredMappings.Order(StringComparer.Ordinal).ToImmutableArray(),
            source.RequiredNativeSizes.Order().ToImmutableArray());
        var manifestBytes = Json(manifest);
        var manifestDigest = PackDigests.Bytes(manifestBytes);

        var digestEntries = new List<HashEntry>
        {
            new("catalogue", "normalized", source.SourceDigest),
            new("manifest", "manifest.json", manifestDigest)
        };
        digestEntries.AddRange(source.Palettes
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .Select(static item => new HashEntry("palette", item.Id, item.Digest)));
        digestEntries.AddRange(source.Atlases
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .Select(static item => new HashEntry("atlas", item.Id, item.Digest)));

        var aggregate = PackDigests.Aggregate(digestEntries.Select(
            static entry => (entry.Kind, entry.Id, entry.Digest)));
        var hashes = new HashDocument(
            source.SourceDigest,
            manifestDigest,
            digestEntries.ToImmutableArray(),
            aggregate);

        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["manifest.json"] = manifestBytes,
            ["hashes.json"] = Json(hashes),
            ["validation.json"] = Json(new ValidationDocument(diagnostics)),
            ["provenance.json"] = Json(new ProvenanceDocument(
                source.Provenance.OrderBy(static item => item.FamilyId, StringComparer.Ordinal)
                    .ToImmutableArray()))
        };

        foreach (var atlas in source.Atlases.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            files.Add(atlas.BufferPath, source.GetAtlasIndices(atlas.Id).ToArray());
        }

        return new CanonicalPackOutput(files, aggregate);
    }

    public static CompiledVisualPack ReadCanonical(CanonicalPackOutput input)
    {
        if (!input.CanonicalFiles.TryGetValue("manifest.json", out var manifestBytes) ||
            !input.CanonicalFiles.TryGetValue("hashes.json", out var hashBytes) ||
            !input.CanonicalFiles.TryGetValue("provenance.json", out var provenanceBytes))
        {
            throw new FormatException("CVP-FMT-001: required canonical file is missing.");
        }

        var manifest = Deserialize<ManifestDocument>(manifestBytes.AsSpan());
        var hashes = Deserialize<HashDocument>(hashBytes.AsSpan());
        var provenance = Deserialize<ProvenanceDocument>(provenanceBytes.AsSpan());
        var expectedManifestDigest = PackDigests.Bytes(manifestBytes.AsSpan());
        var expectedEntries = new List<HashEntry>
        {
            new("catalogue", "normalized", manifest.SourceDigest),
            new("manifest", "manifest.json", expectedManifestDigest)
        };
        expectedEntries.AddRange(manifest.Palettes
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .Select(static item => new HashEntry("palette", item.Id, item.Digest)));
        expectedEntries.AddRange(manifest.Atlases
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .Select(static item => new HashEntry("atlas", item.Id, item.Digest)));
        var expectedAggregate = PackDigests.Aggregate(expectedEntries.Select(
            static entry => (entry.Kind, entry.Id, entry.Digest)));

        if (!StringComparer.Ordinal.Equals(
                hashes.NormalizedCatalogueDigest,
                manifest.SourceDigest) ||
            !StringComparer.Ordinal.Equals(
                hashes.NormalizedManifestDigest,
                expectedManifestDigest) ||
            !hashes.Entries.SequenceEqual(expectedEntries) ||
            !StringComparer.Ordinal.Equals(
                hashes.AggregatePackDigest,
                expectedAggregate))
        {
            throw new FormatException("CVP-DIG-006: hashes.json does not match canonical content.");
        }

        var buffers = new List<KeyValuePair<string, ReadOnlyMemory<byte>>>();

        foreach (var atlas in manifest.Atlases)
        {
            if (!input.CanonicalFiles.TryGetValue(atlas.BufferPath, out var bytes))
            {
                throw new FormatException($"CVP-FMT-002: missing atlas '{atlas.BufferPath}'.");
            }

            if (!StringComparer.Ordinal.Equals(PackDigests.Bytes(bytes.AsSpan()), atlas.Digest))
            {
                throw new FormatException($"CVP-DIG-001: atlas digest mismatch for '{atlas.Id}'.");
            }

            buffers.Add(KeyValuePair.Create(
                atlas.Id,
                (ReadOnlyMemory<byte>)bytes.ToArray()));
        }

        var pack = new CompiledVisualPack(
            manifest.PackId,
            manifest.Compatibility,
            manifest.Compiler,
            manifest.SourceDigest,
            manifest.Palettes,
            manifest.Atlases,
            manifest.Visuals,
            manifest.Motifs,
            manifest.Adjacencies,
            manifest.RequiredMappings,
            manifest.RequiredNativeSizes,
            provenance.Families,
            buffers,
            hashes.AggregatePackDigest);
        var rewritten = WriteCanonical(pack);
        var validationErrors = PackValidator.Validate(pack)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        if (validationErrors.Length != 0)
        {
            throw new FormatException(
                $"{validationErrors[0].Code}: loaded pack failed validation.");
        }

        if (!StringComparer.Ordinal.Equals(rewritten.AggregateDigest, expectedAggregate) ||
            !StringComparer.Ordinal.Equals(rewritten.AggregateDigest, input.AggregateDigest))
        {
            throw new FormatException("CVP-DIG-002: aggregate pack digest mismatch.");
        }

        return pack;
    }

    public static CompiledVisualPack ReadCanonical(IEnumerable<PackFile> files)
    {
        var materialized = files.ToArray();
        var duplicate = materialized
            .GroupBy(static file => file.Path, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new FormatException($"CVP-FMT-004: duplicate pack path '{duplicate.Key}'.");
        }

        var dictionary = materialized.ToDictionary(
            static file => file.Path,
            static file => file.Bytes.ToArray(),
            StringComparer.Ordinal);
        if (!dictionary.TryGetValue("hashes.json", out var hashBytes))
        {
            throw new FormatException("CVP-FMT-001: required canonical file is missing.");
        }

        var hashes = Deserialize<HashDocument>(hashBytes);
        return ReadCanonical(new CanonicalPackOutput(dictionary, hashes.AggregatePackDigest));
    }

    private static byte[] Json<T>(T value)
    {
        var encoded = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var result = new byte[encoded.Length + 1];
        encoded.CopyTo(result, 0);
        result[^1] = (byte)'\n';
        return result;
    }

    private static T Deserialize<T>(ReadOnlySpan<byte> bytes) =>
        JsonSerializer.Deserialize<T>(bytes, JsonOptions)
        ?? throw new FormatException($"CVP-JSON-001: invalid {typeof(T).Name}.");

    private sealed record ManifestDocument(
        string PackId,
        CompatibilityRecord Compatibility,
        CompilerRecord Compiler,
        string SourceDigest,
        ImmutableArray<PaletteRecord> Palettes,
        ImmutableArray<AtlasRecord> Atlases,
        ImmutableArray<VisualRecord> Visuals,
        ImmutableArray<MotifRecord> Motifs,
        ImmutableArray<AdjacencyRecord> Adjacencies,
        ImmutableArray<string> RequiredMappings,
        ImmutableArray<int> RequiredNativeSizes);

    private sealed record HashEntry(string Kind, string Id, string Digest);

    private sealed record HashDocument(
        string NormalizedCatalogueDigest,
        string NormalizedManifestDigest,
        ImmutableArray<HashEntry> Entries,
        string AggregatePackDigest);

    private sealed record ValidationDocument(ImmutableArray<PackDiagnostic> Diagnostics);

    private sealed record ProvenanceDocument(ImmutableArray<ProvenanceRecord> Families);
}

public static class PackDigests
{
    public static string Bytes(ReadOnlySpan<byte> bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    public static string Palette(ImmutableArray<Rgba8> entries) =>
        Bytes(PaletteBytes(entries));

    public static string Geometry(
        PixelRect rectangle,
        PixelSize logicalSize,
        PixelPoint anchor,
        int nativeSize,
        int? adjacencyMask,
        TransformFlags transforms,
        bool requireConnected,
        ReadOnlySpan<byte> atlasIndices,
        int atlasWidth)
    {
        using var stream = new MemoryStream();
        stream.Write("chronicle.geometry.v1\0"u8);
        Write(rectangle.Width);
        Write(rectangle.Height);
        Write(logicalSize.Width);
        Write(logicalSize.Height);
        Write(anchor.X);
        Write(anchor.Y);
        Write(nativeSize);
        Write(adjacencyMask ?? -1);
        Write((int)transforms);
        Write(requireConnected ? 1 : 0);
        for (var y = 0; y < rectangle.Height; y++)
        {
            for (var x = 0; x < rectangle.Width; x++)
            {
                var index = (rectangle.Y + y) * atlasWidth + rectangle.X + x;
                stream.WriteByte(
                    (uint)index < (uint)atlasIndices.Length && atlasIndices[index] != 0
                        ? (byte)1
                        : (byte)0);
            }
        }

        return Bytes(stream.ToArray());

        void Write(int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            stream.Write(bytes);
        }
    }

    internal static byte[] PaletteBytes(ImmutableArray<Rgba8> entries)
    {
        var bytes = new byte[entries.Length * 4];
        for (var index = 0; index < entries.Length; index++)
        {
            var offset = index * 4;
            bytes[offset] = entries[index].R;
            bytes[offset + 1] = entries[index].G;
            bytes[offset + 2] = entries[index].B;
            bytes[offset + 3] = entries[index].A;
        }

        return bytes;
    }

    public static string Aggregate(
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

        return Bytes(stream.ToArray());
    }
}
