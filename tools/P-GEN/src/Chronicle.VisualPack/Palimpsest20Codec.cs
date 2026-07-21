using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicle.VisualPack;

public static class Palimpsest20Codec
{
    public const string AtlasPath = "atlases/palimpsest20.indices";
    public const string ReaderVersion = "1.0.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Default,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = false
    };

    public static CanonicalPackOutput WriteCanonical(
        Palimpsest20Pack pack,
        Palimpsest20Validation validation)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(validation);
        ValidateCompatibility(pack, validation);

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
            new HashFile(AtlasPath, PackDigests.Bytes(atlasBytes)),
            new HashFile("manifest.json", PackDigests.Bytes(manifestBytes)),
            new HashFile("validation.json", PackDigests.Bytes(validationBytes))
        };
        var aggregate = PackDigests.Aggregate(hashedFiles.Select(
            static file => ("file", file.Path, file.Digest)));
        var hashes = new HashDocument(
            "sha256",
            hashedFiles.ToImmutableArray(),
            pack.Digest,
            aggregate);

        return new CanonicalPackOutput(
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [AtlasPath] = atlasBytes,
                ["hashes.json"] = Json(hashes),
                ["manifest.json"] = manifestBytes,
                ["validation.json"] = validationBytes
            },
            aggregate);
    }

    public static Palimpsest20Bundle ReadCanonical(IEnumerable<PackFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var materialized = files.ToArray();
        var duplicate = materialized
            .GroupBy(static file => file.Path, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new FormatException(
                $"PAL20-FMT-001: duplicate pack path '{duplicate.Key}'.");
        }

        var byPath = materialized.ToDictionary(
            static file => file.Path,
            static file => file.Bytes.ToArray(),
            StringComparer.Ordinal);
        var required = new[]
        {
            AtlasPath,
            "hashes.json",
            "manifest.json",
            "validation.json"
        };
        if (byPath.Count != required.Length ||
            required.Any(path => !byPath.ContainsKey(path)))
        {
            throw new FormatException(
                "PAL20-FMT-002: canonical Palimpsest20 file set is incomplete or unexpected.");
        }

        var manifest = Deserialize<ManifestDocument>(
            byPath["manifest.json"],
            "manifest.json");
        var validation = Deserialize<Palimpsest20Validation>(
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
            new HashFile(AtlasPath, PackDigests.Bytes(byPath[AtlasPath])),
            new HashFile(
                "manifest.json",
                PackDigests.Bytes(byPath["manifest.json"])),
            new HashFile(
                "validation.json",
                PackDigests.Bytes(byPath["validation.json"]))
        };
        var expectedAggregate = PackDigests.Aggregate(expectedHashFiles.Select(
            static file => ("file", file.Path, file.Digest)));
        if (!StringComparer.Ordinal.Equals(hashes.Algorithm, "sha256") ||
            !hashes.Files.SequenceEqual(expectedHashFiles) ||
            !StringComparer.Ordinal.Equals(hashes.AggregateDigest, expectedAggregate))
        {
            throw new FormatException(
                "PAL20-HASH-001: hashes.json does not match canonical files.");
        }

        Palimpsest20Pack pack;
        try
        {
            pack = new Palimpsest20Pack(
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
                    new Palimpsest20Definition(
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
        if (!FilesEqual(rewritten.CanonicalFiles, byPath))
        {
            throw new FormatException(
                "PAL20-FMT-004: pack files are not in canonical form.");
        }

        return new Palimpsest20Bundle(pack, validation);
    }

    private static void ValidateDocuments(
        ManifestDocument manifest,
        Palimpsest20Validation validation,
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
        Palimpsest20Pack pack,
        Palimpsest20Validation validation)
    {
        if (validation.PackFormatVersion != Palimpsest20Pack.SupportedFormatVersion ||
            validation.ComposerContractVersion !=
                Palimpsest20Pack.SupportedComposerVersion ||
            validation.VisualStyleVersion != Palimpsest20Pack.SupportedStyleVersion ||
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

    private static bool FilesEqual(
        IReadOnlyDictionary<string, ImmutableArray<byte>> expected,
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
                ?? throw new FormatException(
                    $"PAL20-JSON-001: '{path}' is empty.");
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
        [property: JsonRequired] ImmutableArray<Palimpsest20PaletteColor> Entries,
        [property: JsonRequired] ImmutableArray<PaletteRoleDocument> RoleIndexes);

    private sealed record PaletteRoleDocument(
        [property: JsonRequired] string Name,
        [property: JsonRequired] int Index);

    private sealed record DefinitionDocument(
        [property: JsonRequired] string VisualId,
        [property: JsonRequired] Palimpsest20AtlasRect AtlasRect,
        [property: JsonRequired] string FamilyId,
        [property: JsonRequired] int VariantOrdinal,
        [property: JsonRequired] Palimpsest20LayerClass LayerClass,
        [property: JsonRequired] Palimpsest20PixelAnchor Anchor,
        [property: JsonRequired] Palimpsest20AdjacencyMask? AdjacencyMask,
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
}
