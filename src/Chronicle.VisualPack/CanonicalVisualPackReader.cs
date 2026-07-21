using System.Collections.Immutable;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicle.VisualPack;

/// <summary>
/// One canonical relative path and byte payload supplied to the compiled-pack reader.
/// </summary>
public sealed record CanonicalVisualPackFile(string RelativePath, ReadOnlyMemory<byte> Bytes);

/// <summary>
/// Palimpsest-owned reader for P-GEN's canonical four-file Palimpsest20 bundle.
/// </summary>
public static class CanonicalVisualPackReader
{
    public const string AtlasPath = "atlases/palimpsest20.indices";
    public const string ReaderVersion = "1.0.0";
    public const string RequiredPackId = "chronicle.palimpsest20";
    public const int RequiredCellSize = 20;

    private static readonly string[] RequiredPaths =
    [
        AtlasPath,
        "hashes.json",
        "manifest.json",
        "validation.json",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Default,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false,
    };

    public static CompiledVisualPack ReadDirectory(string bundleDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleDirectory);
        var root = Path.GetFullPath(bundleDirectory);
        if (!Directory.Exists(root))
        {
            throw new FormatException(
                $"PAL20-IO-001: compiled visual bundle directory '{root}' does not exist.");
        }

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new CanonicalVisualPackFile(
                Path.GetRelativePath(root, path).Replace('\\', '/'),
                File.ReadAllBytes(path)))
            .ToArray();
        return ReadCanonical(files);
    }

    public static CompiledVisualPack ReadCanonical(
        IEnumerable<CanonicalVisualPackFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var materialized = files.ToArray();
        foreach (var file in materialized)
        {
            if (!IsCanonicalRelativePath(file.RelativePath))
            {
                throw new FormatException(
                    $"PAL20-FMT-006: invalid pack path '{file.RelativePath}'.");
            }
        }

        var duplicate = materialized
            .GroupBy(static file => file.RelativePath, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new FormatException(
                $"PAL20-FMT-001: duplicate pack path '{duplicate.Key}'.");
        }

        var byPath = materialized.ToDictionary(
            static file => file.RelativePath,
            static file => file.Bytes.ToArray(),
            StringComparer.Ordinal);
        if (byPath.Count != RequiredPaths.Length ||
            RequiredPaths.Any(path => !byPath.ContainsKey(path)))
        {
            throw new FormatException(
                "PAL20-FMT-002: canonical Palimpsest20 file set is incomplete or unexpected.");
        }

        var manifest = Deserialize<ManifestDocument>(
            byPath["manifest.json"],
            "manifest.json");
        var validation = Deserialize<ValidationDocument>(
            byPath["validation.json"],
            "validation.json");
        var hashes = Deserialize<HashDocument>(
            byPath["hashes.json"],
            "hashes.json");
        ValidateDocuments(manifest, validation, hashes);

        if (!StringComparer.Ordinal.Equals(manifest.Atlas.Path, AtlasPath))
        {
            throw new FormatException(
                "PAL20-FMT-003: manifest atlas path is not canonical.");
        }

        var expectedHashFiles = new[]
        {
            new HashFile(AtlasPath, BytesDigest(byPath[AtlasPath])),
            new HashFile("manifest.json", BytesDigest(byPath["manifest.json"])),
            new HashFile("validation.json", BytesDigest(byPath["validation.json"])),
        };
        var expectedAggregate = AggregateDigest(expectedHashFiles.Select(
            static file => ("file", file.Path, file.Digest)));
        if (!StringComparer.Ordinal.Equals(hashes.Algorithm, "sha256") ||
            !hashes.Files.SequenceEqual(expectedHashFiles) ||
            !StringComparer.Ordinal.Equals(hashes.AggregateDigest, expectedAggregate))
        {
            throw new FormatException(
                "PAL20-HASH-001: hashes.json does not match canonical files.");
        }

        CompiledVisualPack pack;
        try
        {
            pack = new CompiledVisualPack(
                manifest.PackId,
                manifest.PackFormatVersion,
                manifest.VisualStyleVersion,
                manifest.ComposerContractVersion,
                manifest.CellSize,
                manifest.Atlas.Id,
                manifest.Palette.Id,
                manifest.Atlas.Width,
                manifest.Atlas.Height,
                byPath[AtlasPath],
                manifest.Palette.Entries,
                manifest.Palette.RoleIndexes.ToDictionary(
                    static role => role.Name,
                    static role => role.Index,
                    StringComparer.Ordinal),
                manifest.Definitions.Select(static definition =>
                    new VisualDefinition(
                        definition.VisualId,
                        definition.AtlasRect,
                        definition.FamilyId,
                        definition.VariantOrdinal,
                        definition.LayerClass,
                        definition.Anchor,
                        definition.AdjacencyMask,
                        definition.OverviewPaletteIndex,
                        definition.PaletteRoleIndexes)).ToArray());
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            OverflowException)
        {
            throw new FormatException(
                "PAL20-FMT-005: manifest data is invalid.",
                exception);
        }

        ValidateCompatibility(pack, validation);
        if (!StringComparer.Ordinal.Equals(pack.Digest, manifest.PalimpsestDigest) ||
            !StringComparer.Ordinal.Equals(pack.Digest, hashes.PalimpsestDigest))
        {
            throw new FormatException(
                "PAL20-HASH-002: PALIMPSEST-compatible pack digest mismatch.");
        }

        var rewritten = WriteCanonical(pack, validation);
        if (!FilesEqual(rewritten, byPath))
        {
            throw new FormatException(
                "PAL20-FMT-004: pack files are not in canonical form.");
        }

        return pack;
    }

    private static IReadOnlyDictionary<string, byte[]> WriteCanonical(
        CompiledVisualPack pack,
        ValidationDocument validation)
    {
        var manifest = new ManifestDocument(
            pack.FormatVersion,
            pack.StyleVersion,
            pack.ComposerVersion,
            pack.PackId,
            pack.CellSize,
            new AtlasDocument(
                pack.AtlasId,
                AtlasPath,
                pack.AtlasWidth,
                pack.AtlasHeight),
            new PaletteDocument(
                pack.PaletteId,
                pack.Palette.ToImmutableArray(),
                pack.PaletteRoleIndexes
                    .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                    .Select(static pair => new PaletteRoleDocument(pair.Key, pair.Value))
                    .ToImmutableArray()),
            pack.Definitions
                .OrderBy(static definition => definition.VisualId, StringComparer.Ordinal)
                .Select(static definition => new DefinitionDocument(
                    definition.VisualId,
                    definition.AtlasRect,
                    definition.FamilyId,
                    definition.VariantOrdinal,
                    definition.LayerClass,
                    definition.Anchor,
                    definition.AdjacencyMask,
                    definition.OverviewPaletteIndex,
                    definition.PaletteRoleIndexes.ToImmutableArray()))
                .ToImmutableArray(),
            pack.Digest);
        var manifestBytes = Json(manifest);
        var validationBytes = Json(validation);
        var atlasBytes = pack.AtlasIndices.ToArray();
        var hashedFiles = new[]
        {
            new HashFile(AtlasPath, BytesDigest(atlasBytes)),
            new HashFile("manifest.json", BytesDigest(manifestBytes)),
            new HashFile("validation.json", BytesDigest(validationBytes)),
        };
        var hashes = new HashDocument(
            "sha256",
            hashedFiles.ToImmutableArray(),
            pack.Digest,
            AggregateDigest(hashedFiles.Select(
                static file => ("file", file.Path, file.Digest))));

        return new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [AtlasPath] = atlasBytes,
            ["hashes.json"] = Json(hashes),
            ["manifest.json"] = manifestBytes,
            ["validation.json"] = validationBytes,
        };
    }

    private static void ValidateDocuments(
        ManifestDocument manifest,
        ValidationDocument validation,
        HashDocument hashes)
    {
        if (manifest.PackId is null ||
            manifest.Atlas is null ||
            manifest.Atlas.Id is null ||
            manifest.Atlas.Path is null ||
            manifest.Palette is null ||
            manifest.Palette.Id is null ||
            manifest.Palette.Entries.IsDefault ||
            manifest.Palette.RoleIndexes.IsDefault ||
            manifest.Palette.RoleIndexes.Any(static role =>
                role is null || role.Name is null) ||
            manifest.Definitions.IsDefault ||
            manifest.Definitions.Any(static definition =>
                definition is null ||
                definition.VisualId is null ||
                definition.FamilyId is null ||
                definition.PaletteRoleIndexes.IsDefault) ||
            manifest.PalimpsestDigest is null ||
            validation.MinimumReaderVersion is null ||
            hashes.Algorithm is null ||
            hashes.Files.IsDefault ||
            hashes.Files.Any(static file =>
                file is null || file.Path is null || file.Digest is null) ||
            hashes.PalimpsestDigest is null ||
            hashes.AggregateDigest is null)
        {
            throw new FormatException(
                "PAL20-JSON-003: required JSON members cannot be null.");
        }
    }

    private static void ValidateCompatibility(
        CompiledVisualPack pack,
        ValidationDocument validation)
    {
        if (!StringComparer.Ordinal.Equals(pack.PackId, RequiredPackId) ||
            pack.CellSize != RequiredCellSize ||
            validation.PackFormatVersion != 1 ||
            validation.ComposerContractVersion != 1 ||
            validation.VisualStyleVersion != 1 ||
            validation.PackFormatVersion != pack.FormatVersion ||
            validation.ComposerContractVersion != pack.ComposerVersion ||
            validation.VisualStyleVersion != pack.StyleVersion)
        {
            throw new FormatException(
                "PAL20-COMPAT-001: unsupported or inconsistent compatibility versions.");
        }

        if (!Version.TryParse(validation.MinimumReaderVersion, out var minimum) ||
            !Version.TryParse(ReaderVersion, out var reader) ||
            minimum > reader)
        {
            throw new FormatException(
                "PAL20-COMPAT-002: minimum reader version is unsupported.");
        }
    }

    private static bool IsCanonicalRelativePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        !Path.IsPathRooted(path) &&
        !path.Contains('\\', StringComparison.Ordinal) &&
        !path.StartsWith("/", StringComparison.Ordinal) &&
        path.Split('/').All(static segment =>
            segment.Length > 0 && segment is not "." and not "..");

    private static bool FilesEqual(
        IReadOnlyDictionary<string, byte[]> expected,
        IReadOnlyDictionary<string, byte[]> actual) =>
        expected.Count == actual.Count &&
        expected.All(pair =>
            actual.TryGetValue(pair.Key, out var bytes) &&
            pair.Value.AsSpan().SequenceEqual(bytes));

    private static byte[] Json<T>(T value)
    {
        var encoded = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var result = new byte[encoded.Length + 1];
        encoded.CopyTo(result, 0);
        result[^1] = (byte)'\n';
        return result;
    }

    private static T Deserialize<T>(byte[] bytes, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            RejectDuplicateProperties(document.RootElement);
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions)
                ?? throw new FormatException($"PAL20-JSON-001: '{path}' is empty.");
        }
        catch (JsonException exception)
        {
            throw new FormatException(
                $"PAL20-JSON-001: invalid '{path}'.",
                exception);
        }
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new FormatException(
                        $"PAL20-JSON-002: duplicate property '{property.Name}'.");
                }

                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicateProperties(item);
            }
        }
    }

    private static string BytesDigest(ReadOnlySpan<byte> bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static string AggregateDigest(
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

        return BytesDigest(stream.ToArray());
    }

    private sealed record ManifestDocument(
        [property: JsonRequired] int PackFormatVersion,
        [property: JsonRequired] int VisualStyleVersion,
        [property: JsonRequired] int ComposerContractVersion,
        [property: JsonRequired] string PackId,
        [property: JsonRequired] int CellSize,
        [property: JsonRequired] AtlasDocument Atlas,
        [property: JsonRequired] PaletteDocument Palette,
        [property: JsonRequired] ImmutableArray<DefinitionDocument> Definitions,
        [property: JsonRequired] string PalimpsestDigest);

    private sealed record AtlasDocument(
        [property: JsonRequired] string Id,
        [property: JsonRequired] string Path,
        [property: JsonRequired] int Width,
        [property: JsonRequired] int Height);

    private sealed record PaletteDocument(
        [property: JsonRequired] string Id,
        [property: JsonRequired] ImmutableArray<PaletteColor> Entries,
        [property: JsonRequired] ImmutableArray<PaletteRoleDocument> RoleIndexes);

    private sealed record PaletteRoleDocument(
        [property: JsonRequired] string Name,
        [property: JsonRequired] int Index);

    private sealed record DefinitionDocument(
        [property: JsonRequired] string VisualId,
        [property: JsonRequired] AtlasRect AtlasRect,
        [property: JsonRequired] string FamilyId,
        [property: JsonRequired] int VariantOrdinal,
        [property: JsonRequired] VisualLayerClass LayerClass,
        [property: JsonRequired] PixelAnchor Anchor,
        [property: JsonRequired] CardinalAdjacencyMask? AdjacencyMask,
        [property: JsonRequired] int OverviewPaletteIndex,
        [property: JsonRequired] ImmutableArray<int> PaletteRoleIndexes);

    private sealed record HashFile(
        [property: JsonRequired] string Path,
        [property: JsonRequired] string Digest);

    private sealed record HashDocument(
        [property: JsonRequired] string Algorithm,
        [property: JsonRequired] ImmutableArray<HashFile> Files,
        [property: JsonRequired] string PalimpsestDigest,
        [property: JsonRequired] string AggregateDigest);

    private sealed record ValidationDocument(
        [property: JsonRequired] int PackFormatVersion,
        [property: JsonRequired] int ComposerContractVersion,
        [property: JsonRequired] int VisualStyleVersion,
        [property: JsonRequired] string MinimumReaderVersion);
}
