using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Chronicle.VisualCompiler;
using Chronicle.VisualPack;

static class Palimpsest20CompilerConformance
{
    private const string CatalogueResource =
        "Chronicle.Visuals.Conformance.palimpsest20.catalogue.json";
    private const string ContractResource =
        "Chronicle.Visuals.Conformance.palimpsest20.contract.json";

    private static readonly JsonSerializerOptions ContractJson = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static bool Run()
    {
        var contract = ReadContract();
        var catalogueBytes = ReadEmbeddedBytes(CatalogueResource);
        if (!DuplicateFamilyIdentifiersAreRejected(catalogueBytes))
        {
            return false;
        }
        if (!InvalidMotifsAreRejected(catalogueBytes))
        {
            return false;
        }
        var catalogue = VisualCatalogue.ParseJson(catalogueBytes);

        var result = VisualCompiler.CompilePalimpsest20(
            catalogue,
            new CompilationOptions(ReviewMode.None));
        if (!result.Succeeded || result.Pack is null || result.Validation is null)
        {
            Console.Error.WriteLine(
                "PAL20-COMPILER-COMPILE: the explicit Palimpsest20 profile did not produce a validated pack.");
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.Error.WriteLine(
                    $"{diagnostic.Code} {diagnostic.Subject}: {diagnostic.Message}");
            }
            return false;
        }

        var pack = result.Pack;
        if (pack.CellSize != contract.CellSize ||
            pack.AtlasWidth != contract.AtlasWidth ||
            pack.AtlasHeight != contract.AtlasHeight ||
            pack.Definitions.Count != contract.DefinitionCount)
        {
            Console.Error.WriteLine(
                "PAL20-COMPILER-DIMENSIONS: compiled pack dimensions or definition count differ from the independent contract.");
            return false;
        }

        foreach (var expected in contract.Definitions)
        {
            try
            {
                _ = pack.Resolve(expected.Id);
            }
            catch (KeyNotFoundException)
            {
                Console.Error.WriteLine(
                    $"PAL20-COMPILER-RESOLVE: expected ID '{expected.Id}' did not resolve.");
                return false;
            }
        }

        ImmutableArray<MetadataDescriptor> actual;
        try
        {
            actual = pack.Definitions.Select(ToMetadata).ToImmutableArray();
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return false;
        }

        if (actual.Select(static definition => definition.Id)
                .Distinct(StringComparer.Ordinal)
                .Count() != actual.Length)
        {
            Console.Error.WriteLine(
                "PAL20-COMPILER-IDS: compiled definition IDs are not unique.");
            return false;
        }

        if (!MetadataMatchesContract(contract.Definitions, actual, out var mismatch))
        {
            Console.Error.WriteLine($"PAL20-COMPILER-METADATA: {mismatch}");
            return false;
        }

        var missing = actual.Skip(1).ToImmutableArray();
        var extra = actual.Add(new MetadataDescriptor(
            "test.extra-definition",
            "test.extra-family",
            0,
            Palimpsest20LayerClass.GroundField,
            null,
            0,
            ImmutableArray<int>.Empty));
        var wrongLayer = actual.SetItem(0, actual[0] with
        {
            Layer = actual[0].Layer == Palimpsest20LayerClass.GroundField
                ? Palimpsest20LayerClass.Actor
                : Palimpsest20LayerClass.GroundField
        });
        if (MetadataMatchesContract(contract.Definitions, missing, out _) ||
            MetadataMatchesContract(contract.Definitions, extra, out _) ||
            MetadataMatchesContract(contract.Definitions, wrongLayer, out _))
        {
            Console.Error.WriteLine(
                "PAL20-COMPILER-COMPARATOR: metadata comparison accepted a missing, extra, or wrong-layer descriptor.");
            return false;
        }

        var duplicateFixture = contract with
        {
            Definitions = contract.Definitions.Add(contract.Definitions[0])
        };
        if (TryValidateContract(duplicateFixture, out _))
        {
            Console.Error.WriteLine(
                "PAL20-COMPILER-FIXTURE: duplicate contract IDs were accepted.");
            return false;
        }

        return true;
    }

    private static bool InvalidMotifsAreRejected(byte[] source)
    {
        foreach (var mutation in new Action<JsonObject>[]
        {
            static motif => motif["footprint"] = new JsonArray(1_000_000, 1_000_000),
            static motif => motif["occupancyTags"] = new JsonArray(),
            static motif => motif["occupancyTags"] = new JsonArray("grove", " ")
        })
        {
            var root = JsonNode.Parse(source)?.AsObject()
                ?? throw new InvalidOperationException(
                    "PAL20-COMPILER-FIXTURE: catalogue resource is not an object.");
            var motif = root["motifs"]?.AsArray()[0]?.AsObject()
                ?? throw new InvalidOperationException(
                    "PAL20-COMPILER-FIXTURE: first motif is absent.");
            mutation(motif);
            try
            {
                _ = VisualCatalogue.ParseJson(
                    Encoding.UTF8.GetBytes(root.ToJsonString()));
                Console.Error.WriteLine(
                    "PAL20-COMPILER-MOTIF-BOUNDS: invalid motif bounds or occupancy tags were accepted.");
                return false;
            }
            catch (FormatException exception) when (exception.Message.StartsWith(
                       "CVC-CAT-005",
                       StringComparison.Ordinal))
            {
            }
        }

        return true;
    }

    private static bool DuplicateFamilyIdentifiersAreRejected(byte[] source)
    {
        foreach (var propertyName in new[] { "families", "connectedFamilies", "motifs" })
        {
            var root = JsonNode.Parse(source)?.AsObject()
                ?? throw new InvalidOperationException(
                    "PAL20-COMPILER-FIXTURE: catalogue resource is not an object.");
            var declarations = root[propertyName]?.AsArray()
                ?? throw new InvalidOperationException(
                    $"PAL20-COMPILER-FIXTURE: '{propertyName}' is absent.");
            declarations.Add(declarations[0]?.DeepClone());
            try
            {
                _ = VisualCatalogue.ParseJson(
                    Encoding.UTF8.GetBytes(root.ToJsonString()));
                Console.Error.WriteLine(
                    $"PAL20-COMPILER-CATALOGUE: duplicate '{propertyName}' ID was accepted.");
                return false;
            }
            catch (FormatException exception) when (exception.Message.StartsWith(
                       "CVC-CAT-005",
                       StringComparison.Ordinal))
            {
                // Expected schema-v2 rejection.
            }
        }

        return true;
    }

    private static ContractDocument ReadContract()
    {
        var contract = JsonSerializer.Deserialize<ContractDocument>(
            ReadEmbeddedBytes(ContractResource),
            ContractJson) ?? throw new InvalidOperationException(
                "PAL20-COMPILER-FIXTURE: contract resource is empty.");
        if (!TryValidateContract(contract, out var error))
        {
            throw new InvalidOperationException($"PAL20-COMPILER-FIXTURE: {error}");
        }

        return contract;
    }

    private static bool TryValidateContract(
        ContractDocument contract,
        out string error)
    {
        if (contract.CellSize != Palimpsest20Pack.NativeCellSize ||
            contract.AtlasWidth <= 0 ||
            contract.AtlasHeight <= 0 ||
            contract.AtlasWidth % contract.CellSize != 0 ||
            contract.AtlasHeight % contract.CellSize != 0 ||
            contract.DefinitionCount != contract.Definitions.Length)
        {
            error = "dimensions or definition count are invalid.";
            return false;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in contract.Definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) ||
                string.IsNullOrWhiteSpace(definition.FamilyId) ||
                !ids.Add(definition.Id) ||
                definition.VariantOrdinal < 0 ||
                !Enum.IsDefined(definition.Layer) ||
                definition.Mask is < 0 or > 15 ||
                definition.OverviewPaletteIndex < 0 ||
                definition.PaletteRoleIndexes.IsDefault ||
                definition.PaletteRoleIndexes.Any(static index => index < 0))
            {
                error = $"definition '{definition.Id}' is invalid or duplicates an ID.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool MetadataMatchesContract(
        ImmutableArray<ContractDefinition> expected,
        ImmutableArray<MetadataDescriptor> actual,
        out string mismatch)
    {
        var expectedById = expected.ToDictionary(
            static definition => definition.Id,
            StringComparer.Ordinal);
        var actualById = actual.ToDictionary(
            static definition => definition.Id,
            StringComparer.Ordinal);
        if (!expectedById.Keys.ToHashSet(StringComparer.Ordinal)
                .SetEquals(actualById.Keys))
        {
            mismatch = "compiled ID set differs from the contract.";
            return false;
        }

        foreach (var expectedDefinition in expected)
        {
            var actualDefinition = actualById[expectedDefinition.Id];
            if (actualDefinition.FamilyId != expectedDefinition.FamilyId ||
                actualDefinition.VariantOrdinal != expectedDefinition.VariantOrdinal ||
                actualDefinition.Layer != expectedDefinition.Layer ||
                actualDefinition.Mask != expectedDefinition.Mask ||
                actualDefinition.OverviewPaletteIndex !=
                    expectedDefinition.OverviewPaletteIndex ||
                !actualDefinition.PaletteRoleIndexes.SequenceEqual(
                    expectedDefinition.PaletteRoleIndexes))
            {
                mismatch = $"metadata differs for '{expectedDefinition.Id}'.";
                return false;
            }
        }

        mismatch = string.Empty;
        return true;
    }

    private static MetadataDescriptor ToMetadata(
        Palimpsest20Definition definition)
    {
        if (definition.AtlasRect.Width != Palimpsest20Pack.NativeCellSize ||
            definition.AtlasRect.Height != Palimpsest20Pack.NativeCellSize ||
            definition.AtlasRect.X < 0 ||
            definition.AtlasRect.Y < 0 ||
            definition.AtlasRect.X % Palimpsest20Pack.NativeCellSize != 0 ||
            definition.AtlasRect.Y % Palimpsest20Pack.NativeCellSize != 0 ||
            definition.Anchor != new Palimpsest20PixelAnchor(10, 10))
        {
            throw new InvalidOperationException(
                $"PAL20-COMPILER-GEOMETRY: '{definition.VisualId}' is not a centered 20x20 cell.");
        }

        return new MetadataDescriptor(
            definition.VisualId,
            definition.FamilyId,
            definition.VariantOrdinal,
            definition.LayerClass,
            definition.AdjacencyMask is null ? null : (int)definition.AdjacencyMask.Value,
            definition.OverviewPaletteIndex,
            definition.PaletteRoleIndexes.ToImmutableArray());
    }

    private static byte[] ReadEmbeddedBytes(string logicalName)
    {
        using var stream = typeof(Palimpsest20CompilerConformance).Assembly
            .GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException(
                $"PAL20-COMPILER-FIXTURE: embedded resource '{logicalName}' is absent.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }

    private sealed record ContractDocument(
        [property: JsonRequired, JsonPropertyName("cellSize")] int CellSize,
        [property: JsonRequired, JsonPropertyName("atlasWidth")] int AtlasWidth,
        [property: JsonRequired, JsonPropertyName("atlasHeight")] int AtlasHeight,
        [property: JsonRequired, JsonPropertyName("definitionCount")] int DefinitionCount,
        [property: JsonRequired, JsonPropertyName("definitions")]
        ImmutableArray<ContractDefinition> Definitions);

    private sealed record ContractDefinition(
        [property: JsonRequired, JsonPropertyName("id")] string Id,
        [property: JsonRequired, JsonPropertyName("familyId")] string FamilyId,
        [property: JsonRequired, JsonPropertyName("variantOrdinal")] int VariantOrdinal,
        [property: JsonRequired, JsonPropertyName("layer")] Palimpsest20LayerClass Layer,
        [property: JsonRequired, JsonPropertyName("mask")] int? Mask,
        [property: JsonRequired, JsonPropertyName("overviewPaletteIndex")]
        int OverviewPaletteIndex,
        [property: JsonRequired, JsonPropertyName("paletteRoleIndexes")]
        ImmutableArray<int> PaletteRoleIndexes);

    private sealed record MetadataDescriptor(
        string Id,
        string FamilyId,
        int VariantOrdinal,
        Palimpsest20LayerClass Layer,
        int? Mask,
        int OverviewPaletteIndex,
        ImmutableArray<int> PaletteRoleIndexes);
}
