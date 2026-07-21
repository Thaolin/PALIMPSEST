using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Chronicle.VisualPack;

public readonly record struct Palimpsest20PaletteColor(
    byte Red,
    byte Green,
    byte Blue,
    byte Alpha = byte.MaxValue);

public readonly record struct Palimpsest20AtlasRect(
    int X,
    int Y,
    int Width,
    int Height);

public readonly record struct Palimpsest20PixelAnchor(int X, int Y);

[Flags]
public enum Palimpsest20AdjacencyMask
{
    None = 0,
    North = 1,
    East = 2,
    South = 4,
    West = 8
}

public enum Palimpsest20LayerClass
{
    GroundField = 0,
    Adjacency = 1,
    EnvironmentalFeature = 2,
    LandmarkOrSubject = 3,
    Actor = 4,
    TemporaryAction = 5,
    TargetOrSelection = 6,
    UiGlyph = 7
}

public sealed class Palimpsest20Definition
{
    public Palimpsest20Definition(
        string visualId,
        Palimpsest20AtlasRect atlasRect,
        string familyId,
        int variantOrdinal,
        Palimpsest20LayerClass layerClass,
        Palimpsest20PixelAnchor anchor,
        Palimpsest20AdjacencyMask? adjacencyMask,
        int overviewPaletteIndex,
        IReadOnlyList<int> paletteRoleIndexes)
    {
        VisualId = visualId;
        AtlasRect = atlasRect;
        FamilyId = familyId;
        VariantOrdinal = variantOrdinal;
        LayerClass = layerClass;
        Anchor = anchor;
        AdjacencyMask = adjacencyMask;
        OverviewPaletteIndex = overviewPaletteIndex;
        PaletteRoleIndexes = Array.AsReadOnly(paletteRoleIndexes.ToArray());
    }

    public string VisualId { get; }
    public Palimpsest20AtlasRect AtlasRect { get; }
    public string FamilyId { get; }
    public int VariantOrdinal { get; }
    public Palimpsest20LayerClass LayerClass { get; }
    public Palimpsest20PixelAnchor Anchor { get; }
    public Palimpsest20AdjacencyMask? AdjacencyMask { get; }
    public int OverviewPaletteIndex { get; }
    public IReadOnlyList<int> PaletteRoleIndexes { get; }
}

public sealed class Palimpsest20Pack
{
    public const int SupportedFormatVersion = 1;
    public const int SupportedStyleVersion = 1;
    public const int SupportedComposerVersion = 1;
    public const int NativeCellSize = 20;

    private readonly IReadOnlyDictionary<string, Palimpsest20Definition> _definitionsById;

    public Palimpsest20Pack(
        string packId,
        int formatVersion,
        int styleVersion,
        int composerVersion,
        int cellSize,
        string atlasId,
        string paletteId,
        int atlasWidth,
        int atlasHeight,
        IReadOnlyList<byte> atlasIndices,
        IReadOnlyList<Palimpsest20PaletteColor> palette,
        IReadOnlyDictionary<string, int> paletteRoleIndexes,
        IReadOnlyList<Palimpsest20Definition> definitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        ArgumentException.ThrowIfNullOrWhiteSpace(atlasId);
        ArgumentException.ThrowIfNullOrWhiteSpace(paletteId);
        ArgumentNullException.ThrowIfNull(atlasIndices);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(paletteRoleIndexes);
        ArgumentNullException.ThrowIfNull(definitions);

        var frozenIndices = atlasIndices.ToArray();
        var frozenPalette = palette.ToArray();
        var frozenRoles = paletteRoleIndexes
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .ToArray();
        var frozenDefinitions = definitions
            .OrderBy(static definition => definition.VisualId, StringComparer.Ordinal)
            .ToArray();

        Validate(
            formatVersion,
            styleVersion,
            composerVersion,
            cellSize,
            atlasWidth,
            atlasHeight,
            frozenIndices,
            frozenPalette,
            frozenRoles,
            frozenDefinitions);

        PackId = packId;
        FormatVersion = formatVersion;
        StyleVersion = styleVersion;
        ComposerVersion = composerVersion;
        CellSize = cellSize;
        AtlasId = atlasId;
        PaletteId = paletteId;
        AtlasWidth = atlasWidth;
        AtlasHeight = atlasHeight;
        AtlasIndices = Array.AsReadOnly(frozenIndices);
        Palette = Array.AsReadOnly(frozenPalette);
        PaletteRoleIndexes = new ReadOnlyDictionary<string, int>(
            frozenRoles.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value,
                StringComparer.Ordinal));
        Definitions = Array.AsReadOnly(frozenDefinitions);
        _definitionsById = frozenDefinitions.ToDictionary(
            static definition => definition.VisualId,
            StringComparer.Ordinal);
        Digest = ComputeDigest(
            packId,
            formatVersion,
            styleVersion,
            composerVersion,
            cellSize,
            atlasId,
            paletteId,
            atlasWidth,
            atlasHeight,
            frozenIndices,
            frozenPalette,
            frozenRoles,
            frozenDefinitions);
    }

    public string PackId { get; }
    public int FormatVersion { get; }
    public int StyleVersion { get; }
    public int ComposerVersion { get; }
    public int CellSize { get; }
    public string AtlasId { get; }
    public string PaletteId { get; }
    public int AtlasWidth { get; }
    public int AtlasHeight { get; }
    public IReadOnlyList<byte> AtlasIndices { get; }
    public IReadOnlyList<Palimpsest20PaletteColor> Palette { get; }
    public IReadOnlyDictionary<string, int> PaletteRoleIndexes { get; }
    public IReadOnlyList<Palimpsest20Definition> Definitions { get; }
    public string Digest { get; }

    public Palimpsest20Definition Resolve(string visualId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visualId);
        return _definitionsById.TryGetValue(visualId, out var definition)
            ? definition
            : throw new KeyNotFoundException(
                $"Visual pack '{PackId}' does not contain '{visualId}'.");
    }

    private static void Validate(
        int formatVersion,
        int styleVersion,
        int composerVersion,
        int cellSize,
        int atlasWidth,
        int atlasHeight,
        IReadOnlyList<byte> atlasIndices,
        IReadOnlyList<Palimpsest20PaletteColor> palette,
        IReadOnlyList<KeyValuePair<string, int>> paletteRoles,
        IReadOnlyList<Palimpsest20Definition> definitions)
    {
        if (formatVersion != SupportedFormatVersion ||
            styleVersion != SupportedStyleVersion ||
            composerVersion != SupportedComposerVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                "Palimpsest20 supports pack, style, and composer version 1.");
        }

        if (cellSize != NativeCellSize ||
            atlasWidth <= 0 ||
            atlasHeight <= 0 ||
            atlasWidth % cellSize != 0 ||
            atlasHeight % cellSize != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellSize),
                "Palimpsest20 requires a 20px grid-aligned atlas.");
        }

        var pixelCount = checked(atlasWidth * atlasHeight);
        if (atlasIndices.Count != pixelCount)
        {
            throw new ArgumentException(
                "The indexed atlas length must equal width multiplied by height.",
                nameof(atlasIndices));
        }

        if (palette.Count is < 1 or > 256 || palette[0].Alpha != 0)
        {
            throw new ArgumentException(
                "The palette requires one to 256 entries and transparent index 0.",
                nameof(palette));
        }

        if (atlasIndices.Any(index => index >= palette.Count))
        {
            throw new ArgumentException(
                "Every atlas index must resolve through the palette.",
                nameof(atlasIndices));
        }

        foreach (var role in paletteRoles)
        {
            if (!IsIdentifier(role.Key) || role.Value < 0 || role.Value >= palette.Count)
            {
                throw new ArgumentException(
                    "Palette roles require stable names and valid indexes.",
                    nameof(paletteRoles));
            }
        }

        var occupied = new bool[pixelCount];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (!IsIdentifier(definition.VisualId) || !ids.Add(definition.VisualId))
            {
                throw new ArgumentException(
                    "Visual identifiers must be unique lowercase namespaced ASCII.",
                    nameof(definitions));
            }

            if (!IsIdentifier(definition.FamilyId) ||
                definition.VariantOrdinal < 0 ||
                !Enum.IsDefined(definition.LayerClass))
            {
                throw new ArgumentException(
                    $"Visual '{definition.VisualId}' has invalid family metadata.",
                    nameof(definitions));
            }

            var rect = definition.AtlasRect;
            if (rect.Width != NativeCellSize ||
                rect.Height != NativeCellSize ||
                rect.X < 0 ||
                rect.Y < 0 ||
                rect.X % NativeCellSize != 0 ||
                rect.Y % NativeCellSize != 0 ||
                rect.X + rect.Width > atlasWidth ||
                rect.Y + rect.Height > atlasHeight)
            {
                throw new ArgumentException(
                    $"Visual '{definition.VisualId}' must occupy one aligned 20px cell.",
                    nameof(definitions));
            }

            if (definition.Anchor != new Palimpsest20PixelAnchor(10, 10))
            {
                throw new ArgumentException(
                    $"Visual '{definition.VisualId}' must use the centered 20px anchor.",
                    nameof(definitions));
            }

            if (definition.AdjacencyMask is { } mask &&
                (int)mask is < 0 or > 15)
            {
                throw new ArgumentException(
                    $"Visual '{definition.VisualId}' has an invalid cardinal mask.",
                    nameof(definitions));
            }

            if (definition.OverviewPaletteIndex < 0 ||
                definition.OverviewPaletteIndex >= palette.Count ||
                definition.PaletteRoleIndexes.Any(index =>
                    index < 0 || index >= palette.Count))
            {
                throw new ArgumentException(
                    $"Visual '{definition.VisualId}' references an invalid palette index.",
                    nameof(definitions));
            }

            var visible = false;
            for (var y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                for (var x = rect.X; x < rect.X + rect.Width; x++)
                {
                    var index = y * atlasWidth + x;
                    if (occupied[index])
                    {
                        throw new ArgumentException(
                            "Atlas rectangles may not overlap.",
                            nameof(definitions));
                    }

                    occupied[index] = true;
                    visible |= atlasIndices[index] != 0;
                }
            }

            if (!visible)
            {
                throw new ArgumentException(
                    $"Visual '{definition.VisualId}' has no visible indexed pixels.",
                    nameof(definitions));
            }
        }
    }

    private static bool IsIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hasNamespaceSeparator = false;
        var previousWasSeparator = true;
        foreach (var character in value)
        {
            if (character is >= 'a' and <= 'z' ||
                character is >= '0' and <= '9')
            {
                previousWasSeparator = false;
                continue;
            }
            if (character is not ('.' or '-') || previousWasSeparator)
            {
                return false;
            }

            hasNamespaceSeparator |= character == '.';
            previousWasSeparator = true;
        }

        return hasNamespaceSeparator && !previousWasSeparator;
    }

    private static string ComputeDigest(
        string packId,
        int formatVersion,
        int styleVersion,
        int composerVersion,
        int cellSize,
        string atlasId,
        string paletteId,
        int atlasWidth,
        int atlasHeight,
        IReadOnlyList<byte> atlasIndices,
        IReadOnlyList<Palimpsest20PaletteColor> palette,
        IReadOnlyList<KeyValuePair<string, int>> paletteRoles,
        IReadOnlyList<Palimpsest20Definition> definitions)
    {
        using var bytes = new MemoryStream();
        WriteString(bytes, "chronicle.visual-pack/v1");
        WriteString(bytes, packId);
        WriteInt32(bytes, formatVersion);
        WriteInt32(bytes, styleVersion);
        WriteInt32(bytes, composerVersion);
        WriteInt32(bytes, cellSize);
        WriteString(bytes, atlasId);
        WriteString(bytes, paletteId);
        WriteInt32(bytes, atlasWidth);
        WriteInt32(bytes, atlasHeight);

        WriteInt32(bytes, palette.Count);
        foreach (var color in palette)
        {
            bytes.WriteByte(color.Red);
            bytes.WriteByte(color.Green);
            bytes.WriteByte(color.Blue);
            bytes.WriteByte(color.Alpha);
        }

        WriteInt32(bytes, paletteRoles.Count);
        foreach (var role in paletteRoles)
        {
            WriteString(bytes, role.Key);
            WriteInt32(bytes, role.Value);
        }

        WriteInt32(bytes, definitions.Count);
        foreach (var definition in definitions)
        {
            WriteString(bytes, definition.VisualId);
            WriteString(bytes, definition.FamilyId);
            WriteInt32(bytes, definition.VariantOrdinal);
            WriteInt32(bytes, (int)definition.LayerClass);
            WriteInt32(bytes, definition.AtlasRect.X);
            WriteInt32(bytes, definition.AtlasRect.Y);
            WriteInt32(bytes, definition.AtlasRect.Width);
            WriteInt32(bytes, definition.AtlasRect.Height);
            WriteInt32(bytes, definition.Anchor.X);
            WriteInt32(bytes, definition.Anchor.Y);
            WriteInt32(
                bytes,
                definition.AdjacencyMask is null
                    ? -1
                    : (int)definition.AdjacencyMask.Value);
            WriteInt32(bytes, definition.OverviewPaletteIndex);
            WriteInt32(bytes, definition.PaletteRoleIndexes.Count);
            foreach (var paletteIndex in definition.PaletteRoleIndexes)
            {
                WriteInt32(bytes, paletteIndex);
            }
        }

        WriteInt32(bytes, atlasIndices.Count);
        foreach (var index in atlasIndices)
        {
            bytes.WriteByte(index);
        }

        return "sha256:" +
            Convert.ToHexString(SHA256.HashData(bytes.ToArray())).ToLowerInvariant();
    }

    private static void WriteString(Stream stream, string value)
    {
        var utf8 = Encoding.UTF8.GetBytes(value);
        WriteInt32(stream, utf8.Length);
        stream.Write(utf8);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        bytes[0] = (byte)value;
        bytes[1] = (byte)(value >> 8);
        bytes[2] = (byte)(value >> 16);
        bytes[3] = (byte)(value >> 24);
        stream.Write(bytes);
    }
}

public sealed record Palimpsest20Validation(
    [property: JsonRequired] int PackFormatVersion,
    [property: JsonRequired] int ComposerContractVersion,
    [property: JsonRequired] int VisualStyleVersion,
    [property: JsonRequired] string MinimumReaderVersion);

public sealed record Palimpsest20Bundle(
    Palimpsest20Pack Pack,
    Palimpsest20Validation Validation);
