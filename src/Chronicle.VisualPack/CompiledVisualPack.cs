using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace Chronicle.VisualPack;

/// <summary>
/// One RGBA palette entry used by an indexed visual atlas.
/// </summary>
public readonly record struct PaletteColor(byte Red, byte Green, byte Blue, byte Alpha = byte.MaxValue);

/// <summary>
/// An integer pixel rectangle within a compiled atlas.
/// </summary>
public readonly record struct AtlasRect(int X, int Y, int Width, int Height);

/// <summary>
/// An integer pixel anchor measured from the top-left of a visual's atlas rectangle.
/// </summary>
public readonly record struct PixelAnchor(int X, int Y);

/// <summary>
/// The four cardinal semantic neighbours used by connected visual families.
/// </summary>
[Flags]
public enum CardinalAdjacencyMask
{
    None = 0,
    North = 1,
    East = 2,
    South = 4,
    West = 8,
}

/// <summary>
/// The fixed rendering strata understood by the Gate 3B pack seam.
/// </summary>
public enum VisualLayerClass
{
    GroundField = 0,
    Adjacency = 1,
    EnvironmentalFeature = 2,
    LandmarkOrSubject = 3,
    Actor = 4,
    TemporaryAction = 5,
    TargetOrSelection = 6,
    UiGlyph = 7,
}

/// <summary>
/// A stable visual identifier and its compact manifest data.
/// </summary>
public sealed class VisualDefinition
{
    internal VisualDefinition(
        string visualId,
        AtlasRect atlasRect,
        string familyId,
        int variantOrdinal,
        VisualLayerClass layerClass,
        PixelAnchor anchor,
        CardinalAdjacencyMask? adjacencyMask,
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

    public AtlasRect AtlasRect { get; }

    public string FamilyId { get; }

    public int VariantOrdinal { get; }

    public VisualLayerClass LayerClass { get; }

    public PixelAnchor Anchor { get; }

    public CardinalAdjacencyMask? AdjacencyMask { get; }

    public int OverviewPaletteIndex { get; }

    /// <summary>
    /// Palette indices used by this visual, in the pack's fixed role order.
    /// </summary>
    public IReadOnlyList<int> PaletteRoleIndexes { get; }
}

/// <summary>
/// Immutable, indexed-pixel data at the compiler-neutral visual-pack seam.
/// </summary>
public sealed class CompiledVisualPack
{
    public CompiledVisualPack(
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
        IReadOnlyList<PaletteColor> palette,
        IReadOnlyDictionary<string, int> paletteRoleIndexes,
        IReadOnlyList<VisualDefinition> definitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        ArgumentException.ThrowIfNullOrWhiteSpace(atlasId);
        ArgumentException.ThrowIfNullOrWhiteSpace(paletteId);
        ArgumentNullException.ThrowIfNull(atlasIndices);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(paletteRoleIndexes);
        ArgumentNullException.ThrowIfNull(definitions);

        PackId = packId;
        FormatVersion = formatVersion;
        StyleVersion = styleVersion;
        ComposerVersion = composerVersion;
        CellSize = cellSize;
        AtlasId = atlasId;
        PaletteId = paletteId;
        AtlasWidth = atlasWidth;
        AtlasHeight = atlasHeight;

        var frozenIndices = atlasIndices.ToArray();
        var frozenPalette = palette.ToArray();
        var frozenRoles = paletteRoleIndexes
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray();
        var frozenDefinitions = definitions
            .OrderBy(definition => definition.VisualId, StringComparer.Ordinal)
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

        AtlasIndices = Array.AsReadOnly(frozenIndices);
        Palette = Array.AsReadOnly(frozenPalette);
        PaletteRoleIndexes = new ReadOnlyDictionary<string, int>(
            frozenRoles.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        PaletteRoles = PaletteRoleIndexes;
        Definitions = Array.AsReadOnly(frozenDefinitions);
        _definitionsById = frozenDefinitions.ToDictionary(
            definition => definition.VisualId,
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

    private readonly IReadOnlyDictionary<string, VisualDefinition> _definitionsById;

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

    public IReadOnlyList<PaletteColor> Palette { get; }

    /// <summary>
    /// Stable role names and their palette indices.
    /// </summary>
    public IReadOnlyDictionary<string, int> PaletteRoleIndexes { get; }

    /// <summary>
    /// Alias retained for concise pack consumers.
    /// </summary>
    public IReadOnlyDictionary<string, int> PaletteRoles { get; }

    public IReadOnlyList<VisualDefinition> Definitions { get; }

    /// <summary>
    /// A SHA-256 digest over normalized metadata, palette, definitions, and indexed pixels.
    /// </summary>
    public string Digest { get; }

    public VisualDefinition Resolve(string visualId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visualId);

        if (_definitionsById.TryGetValue(visualId, out var definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Visual pack '{PackId}' does not contain '{visualId}'.");
    }

    private static void Validate(
        int formatVersion,
        int styleVersion,
        int composerVersion,
        int cellSize,
        int atlasWidth,
        int atlasHeight,
        IReadOnlyList<byte> atlasIndices,
        IReadOnlyList<PaletteColor> palette,
        IReadOnlyList<KeyValuePair<string, int>> paletteRoles,
        IReadOnlyList<VisualDefinition> definitions)
    {
        if (formatVersion <= 0 || styleVersion <= 0 || composerVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(formatVersion), "Pack versions must be positive integers.");
        }

        if (cellSize <= 0 || atlasWidth <= 0 || atlasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell and atlas dimensions must be positive integers.");
        }

        var pixelCount = checked(atlasWidth * atlasHeight);
        if (atlasIndices.Count != pixelCount)
        {
            throw new ArgumentException("The indexed atlas length must equal width multiplied by height.", nameof(atlasIndices));
        }

        if (palette.Count is < 1 or > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(palette), "An indexed palette must contain one to 256 entries.");
        }

        if (palette[0].Alpha != 0)
        {
            throw new ArgumentException("Palette index 0 must be transparent.", nameof(palette));
        }

        foreach (var index in atlasIndices)
        {
            if (index >= palette.Count)
            {
                throw new ArgumentException("Every atlas index must resolve through the palette.", nameof(atlasIndices));
            }
        }

        foreach (var role in paletteRoles)
        {
            if (string.IsNullOrWhiteSpace(role.Key) || role.Value < 0 || role.Value >= palette.Count)
            {
                throw new ArgumentException("Palette roles must have non-empty names and valid indices.", nameof(paletteRoles));
            }
        }

        var occupied = new bool[pixelCount];
        var visualIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.VisualId) || !visualIds.Add(definition.VisualId))
            {
                throw new ArgumentException("Visual identifiers must be unique and non-empty.", nameof(definitions));
            }

            if (string.IsNullOrWhiteSpace(definition.FamilyId) || definition.VariantOrdinal < 0)
            {
                throw new ArgumentException("Visual families require stable IDs and non-negative variant ordinals.", nameof(definitions));
            }

            var rect = definition.AtlasRect;
            if (rect.X < 0 || rect.Y < 0 || rect.Width <= 0 || rect.Height <= 0 ||
                rect.X + rect.Width > atlasWidth || rect.Y + rect.Height > atlasHeight)
            {
                throw new ArgumentException($"Visual '{definition.VisualId}' has an out-of-bounds atlas rectangle.", nameof(definitions));
            }

            if (definition.Anchor.X < 0 || definition.Anchor.Y < 0 ||
                definition.Anchor.X >= rect.Width || definition.Anchor.Y >= rect.Height)
            {
                throw new ArgumentException($"Visual '{definition.VisualId}' has an invalid integer anchor.", nameof(definitions));
            }

            if (definition.AdjacencyMask is { } mask && ((int)mask < 0 || (int)mask > 15))
            {
                throw new ArgumentException($"Visual '{definition.VisualId}' has an invalid cardinal adjacency mask.", nameof(definitions));
            }

            if (definition.OverviewPaletteIndex < 0 || definition.OverviewPaletteIndex >= palette.Count ||
                definition.PaletteRoleIndexes.Any(index => index < 0 || index >= palette.Count))
            {
                throw new ArgumentException($"Visual '{definition.VisualId}' references an invalid palette index.", nameof(definitions));
            }

            var hasVisiblePixel = false;
            for (var y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                for (var x = rect.X; x < rect.X + rect.Width; x++)
                {
                    var atlasIndex = y * atlasWidth + x;
                    if (occupied[atlasIndex])
                    {
                        throw new ArgumentException("Atlas rectangles may not overlap.", nameof(definitions));
                    }

                    occupied[atlasIndex] = true;
                    hasVisiblePixel |= atlasIndices[atlasIndex] != 0;
                }
            }

            if (!hasVisiblePixel)
            {
                throw new ArgumentException($"Visual '{definition.VisualId}' has no visible indexed pixels.", nameof(definitions));
            }
        }
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
        IReadOnlyList<PaletteColor> palette,
        IReadOnlyList<KeyValuePair<string, int>> paletteRoles,
        IReadOnlyList<VisualDefinition> definitions)
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
            WriteInt32(bytes, definition.AdjacencyMask is null ? -1 : (int)definition.AdjacencyMask.Value);
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

        return "sha256:" + Convert.ToHexString(SHA256.HashData(bytes.ToArray())).ToLowerInvariant();
    }

    private static void WriteString(Stream stream, string value)
    {
        var utf8 = Encoding.UTF8.GetBytes(value);
        WriteInt32(stream, utf8.Length);
        stream.Write(utf8, 0, utf8.Length);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        buffer[0] = (byte)value;
        buffer[1] = (byte)(value >> 8);
        buffer[2] = (byte)(value >> 16);
        buffer[3] = (byte)(value >> 24);
        stream.Write(buffer);
    }
}
