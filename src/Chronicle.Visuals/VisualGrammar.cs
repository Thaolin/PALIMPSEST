using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Chronicle.Core;
using Chronicle.VisualPack;

namespace Chronicle.Visuals;

public sealed record VisualCompositionInput(
    WorldArea SemanticArea,
    WorldRectangle VisibleBounds,
    long ChronicleSeed,
    CompiledVisualPack Pack,
    int VisualStyleVersion,
    WorldAddress? IncarnationAddress,
    IReadOnlyList<WorldAddress> TargetAddresses,
    IReadOnlyList<WorldAddress> SelectedAddresses);

public readonly record struct VisualRenderMark(
    WorldAddress Address,
    string VisualId,
    string FamilyId,
    int VariantOrdinal,
    VisualLayerClass Layer,
    AtlasRect AtlasRect,
    PixelAnchor Anchor,
    int OverviewPaletteIndex,
    int Column,
    int Row);

public sealed record VisualRenderPlan(
    string PackId,
    string PackDigest,
    int CellSize,
    WorldRectangle Bounds,
    IReadOnlyList<VisualRenderMark> Marks,
    string Digest);

/// <summary>
/// Maps immutable Chronicle semantics to transient draw marks. This is the one
/// Palimpsest-specific Visual Grammar seam; it owns no gameplay meaning.
/// </summary>
public static class VisualGrammar
{
    private const int SupportedPackFormatVersion = 1;
    private const int SupportedComposerVersion = 1;

    public static VisualRenderPlan Compose(VisualCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.SemanticArea);
        ArgumentNullException.ThrowIfNull(input.Pack);
        ArgumentNullException.ThrowIfNull(input.TargetAddresses);
        ArgumentNullException.ThrowIfNull(input.SelectedAddresses);
        Validate(input);

        var cells = input.SemanticArea.Cells.ToDictionary(cell => cell.Address);
        var targets = input.TargetAddresses.ToHashSet();
        var selections = input.SelectedAddresses.ToHashSet();
        var layers = Enumerable.Range(0, Enum.GetValues<VisualLayerClass>().Length)
            .Select(_ => new List<VisualRenderMark>())
            .ToArray();

        foreach (var cell in input.SemanticArea.Cells
                     .Where(cell => Contains(input.VisibleBounds, cell.Address))
                     .OrderBy(cell => cell.Address.Y)
                     .ThenBy(cell => cell.Address.X))
        {
            AddGround(input, cell, layers);
            AddAdjacency(input, cell, cells, layers);
            AddFeature(input, cell, cells, layers);
            AddDurableSubject(input, cell, layers);

            if (input.IncarnationAddress == cell.Address)
            {
                Add(input, cell.Address, "actor.incarnation", layers);
            }

            if (targets.Contains(cell.Address))
            {
                Add(input, cell.Address, "emphasis.target.valid", layers);
            }

            if (selections.Contains(cell.Address))
            {
                Add(input, cell.Address, "emphasis.selection", layers);
            }
        }

        var marks = layers
            .SelectMany(layer => layer
                .OrderBy(mark => mark.Row)
                .ThenBy(mark => mark.Column)
                .ThenBy(mark => mark.VisualId, StringComparer.Ordinal))
            .ToArray();
        var digest = ComputeDigest(input, marks);
        return new VisualRenderPlan(
            input.Pack.PackId,
            input.Pack.Digest,
            input.Pack.CellSize,
            input.VisibleBounds,
            Array.AsReadOnly(marks),
            digest);
    }

    private static void Validate(VisualCompositionInput input)
    {
        if (input.Pack.FormatVersion != SupportedPackFormatVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported visual pack format '{input.Pack.FormatVersion}'.");
        }

        if (input.Pack.ComposerVersion != SupportedComposerVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported composer contract '{input.Pack.ComposerVersion}'.");
        }

        if (input.VisualStyleVersion != input.Pack.StyleVersion)
        {
            throw new InvalidOperationException(
                "The requested visual style does not match the compiled pack.");
        }

        if (input.VisibleBounds.Width <= 0 || input.VisibleBounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "Visual composition bounds must be positive.");
        }

        var semantic = input.SemanticArea.Bounds;
        var requiredSemantic = VisualViewportBounds.WithOneCellSemanticHalo(
            input.VisibleBounds);
        var hasOneCellHalo =
            semantic.MinX <= requiredSemantic.MinX &&
            semantic.MinY <= requiredSemantic.MinY &&
            (Int128)semantic.MinX + semantic.Width >=
            (Int128)requiredSemantic.MinX + requiredSemantic.Width &&
            (Int128)semantic.MinY + semantic.Height >=
            (Int128)requiredSemantic.MinY + requiredSemantic.Height;
        if (!hasOneCellHalo)
        {
            throw new ArgumentException(
                "Visual composition requires every representable cell in the one-cell semantic halo.",
                nameof(input));
        }
    }

    private static void AddGround(
        VisualCompositionInput input,
        WorldCell cell,
        IReadOnlyList<List<VisualRenderMark>> layers)
    {
        var visualId = cell.Ground switch
        {
            WorldGround.Grass => VariantId(
                "terrain.surface.grass",
                SelectVariant(input, cell.Address, "terrain.surface.grass", VisualLayerClass.GroundField, 4)),
            WorldGround.Soil => VariantId(
                "terrain.surface.soil",
                SelectVariant(input, cell.Address, "terrain.surface.soil", VisualLayerClass.GroundField, 4)),
            WorldGround.Water => "terrain.surface.water",
            WorldGround.OpenSky => VariantId(
                "terrain.sky.open",
                SelectVariant(input, cell.Address, "terrain.sky.open", VisualLayerClass.GroundField, 3)),
            _ => throw new InvalidOperationException($"No ground visual maps '{cell.Ground}'."),
        };
        Add(input, cell.Address, visualId, layers);
    }

    private static void AddAdjacency(
        VisualCompositionInput input,
        WorldCell cell,
        IReadOnlyDictionary<WorldAddress, WorldCell> cells,
        IReadOnlyList<List<VisualRenderMark>> layers)
    {
        if (cell.Ground == WorldGround.Water)
        {
            var mask = CardinalMask(
                cell.Address,
                cells,
                neighbor => neighbor.Ground == WorldGround.Water);
            Add(input, cell.Address, $"terrain.surface.water.edge.{(int)mask:00}", layers);
        }

        if (cell.Feature == WorldFeature.Cloud)
        {
            var mask = CardinalMask(
                cell.Address,
                cells,
                neighbor => neighbor.Feature == WorldFeature.Cloud);
            Add(
                input,
                cell.Address,
                mask == CardinalAdjacencyMask.None
                    ? "terrain.sky.cloud"
                    : $"terrain.sky.cloud.mask.{(int)mask:00}",
                layers);
        }
    }

    private static void AddFeature(
        VisualCompositionInput input,
        WorldCell cell,
        IReadOnlyDictionary<WorldAddress, WorldCell> cells,
        IReadOnlyList<List<VisualRenderMark>> layers)
    {
        var familyId = (cell.Ground, cell.Feature) switch
        {
            (_, WorldFeature.Vegetation) => "feature.surface.grove",
            (WorldGround.Water, WorldFeature.Stone) =>
                "feature.surface.ridge-water-crossing",
            (_, WorldFeature.Stone) => "feature.surface.ridge",
            _ => null,
        };
        if (familyId is not null)
        {
            var mask = CardinalMask(
                cell.Address,
                cells,
                neighbor => neighbor.Feature == cell.Feature);
            var variant = SelectVariant(
                input,
                cell.Address,
                familyId,
                VisualLayerClass.EnvironmentalFeature,
                variantCount: familyId == "feature.surface.ridge-water-crossing" ? 4 : 2);
            Add(input, cell.Address, MaskedFeatureId(familyId, mask, variant), layers);
        }
    }

    private static void AddDurableSubject(
        VisualCompositionInput input,
        WorldCell cell,
        IReadOnlyList<List<VisualRenderMark>> layers)
    {
        if (string.Equals(
                cell.DurableIdentity,
                ChronicleState.LooseStoneIdentity,
                StringComparison.Ordinal))
        {
            Add(input, cell.Address, "subject.loose-stone", layers);
        }
        else if (string.Equals(
                     cell.DurableIdentity,
                     SkyStratum.LandmarkName,
                     StringComparison.Ordinal))
        {
            Add(input, cell.Address, "landmark.bell-that-fell-up", layers);
        }
    }

    private static void Add(
        VisualCompositionInput input,
        WorldAddress address,
        string visualId,
        IReadOnlyList<List<VisualRenderMark>> layers)
    {
        var definition = input.Pack.Resolve(visualId);
        var column = checked((int)(address.X - input.VisibleBounds.MinX));
        var row = checked((int)(address.Y - input.VisibleBounds.MinY));
        layers[(int)definition.LayerClass].Add(
            new VisualRenderMark(
                address,
                definition.VisualId,
                definition.FamilyId,
                definition.VariantOrdinal,
                definition.LayerClass,
                definition.AtlasRect,
                definition.Anchor,
                definition.OverviewPaletteIndex,
                column,
                row));
    }

    private static CardinalAdjacencyMask CardinalMask(
        WorldAddress address,
        IReadOnlyDictionary<WorldAddress, WorldCell> cells,
        Func<WorldCell, bool> connects)
    {
        var mask = CardinalAdjacencyMask.None;
        Add(CardinalAdjacencyMask.North, 0, -1);
        Add(CardinalAdjacencyMask.East, 1, 0);
        Add(CardinalAdjacencyMask.South, 0, 1);
        Add(CardinalAdjacencyMask.West, -1, 0);
        return mask;

        void Add(CardinalAdjacencyMask bit, int deltaX, int deltaY)
        {
            if ((deltaX < 0 && address.X == long.MinValue) ||
                (deltaX > 0 && address.X == long.MaxValue) ||
                (deltaY < 0 && address.Y == long.MinValue) ||
                (deltaY > 0 && address.Y == long.MaxValue))
            {
                return;
            }

            var neighborAddress = new WorldAddress(
                address.Stratum,
                address.X + deltaX,
                address.Y + deltaY);
            if (cells.TryGetValue(neighborAddress, out var neighbor) && connects(neighbor))
            {
                mask |= bit;
            }
        }
    }

    private static string VariantId(string familyId, int variant) =>
        variant == 0 ? familyId : $"{familyId}.v{variant}";

    private static string MaskedFeatureId(
        string familyId,
        CardinalAdjacencyMask mask,
        int variant)
    {
        var masked = mask == CardinalAdjacencyMask.None
            ? familyId
            : $"{familyId}.mask.{(int)mask:00}";
        return variant == 0 ? masked : $"{masked}.v{variant}";
    }

    private static int SelectVariant(
        VisualCompositionInput input,
        WorldAddress address,
        string familyId,
        VisualLayerClass layer,
        int variantCount)
    {
        var hash = 14_695_981_039_346_656_037UL;
        MixInt64(ref hash, input.Pack.ComposerVersion);
        MixInt64(ref hash, input.VisualStyleVersion);
        MixInt64(ref hash, input.ChronicleSeed);
        MixString(ref hash, address.Stratum);
        MixInt64(ref hash, address.X);
        MixInt64(ref hash, address.Y);
        MixString(ref hash, familyId);
        MixInt64(ref hash, (int)layer);
        MixInt64(ref hash, 0);
        return (int)(Avalanche(hash) % (uint)variantCount);
    }

    private static ulong Avalanche(ulong hash)
    {
        hash ^= hash >> 33;
        hash *= 0xff51afd7ed558ccdUL;
        hash ^= hash >> 33;
        hash *= 0xc4ceb9fe1a85ec53UL;
        return hash ^ (hash >> 33);
    }

    private static void MixString(ref ulong hash, string value)
    {
        MixInt64(ref hash, value.Length);
        foreach (var character in value)
        {
            MixByte(ref hash, (byte)character);
            MixByte(ref hash, (byte)(character >> 8));
        }
    }

    private static void MixInt64(ref ulong hash, long value)
    {
        var bits = unchecked((ulong)value);
        for (var shift = 0; shift < 64; shift += 8)
        {
            MixByte(ref hash, (byte)(bits >> shift));
        }
    }

    private static void MixByte(ref ulong hash, byte value)
    {
        hash ^= value;
        hash *= 1_099_511_628_211UL;
    }

    private static string ComputeDigest(
        VisualCompositionInput input,
        IReadOnlyList<VisualRenderMark> marks)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var stringBytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        AppendString("chronicle.visual-render-plan/v1");
        AppendString(input.Pack.PackId);
        AppendString(input.Pack.Digest);
        AppendInt32(input.Pack.CellSize);
        AppendInt32(input.VisualStyleVersion);
        AppendInt64(input.VisibleBounds.MinX);
        AppendInt64(input.VisibleBounds.MinY);
        AppendInt32(input.VisibleBounds.Width);
        AppendInt32(input.VisibleBounds.Height);
        AppendInt32(marks.Count);

        foreach (var mark in marks)
        {
            AppendString(mark.Address.Stratum);
            AppendInt64(mark.Address.X);
            AppendInt64(mark.Address.Y);
            AppendString(mark.VisualId);
            AppendString(mark.FamilyId);
            AppendInt32(mark.VariantOrdinal);
            AppendInt32((int)mark.Layer);
            AppendInt32(mark.AtlasRect.X);
            AppendInt32(mark.AtlasRect.Y);
            AppendInt32(mark.AtlasRect.Width);
            AppendInt32(mark.AtlasRect.Height);
            AppendInt32(mark.Anchor.X);
            AppendInt32(mark.Anchor.Y);
            AppendInt32(mark.OverviewPaletteIndex);
            AppendInt32(mark.Column);
            AppendInt32(mark.Row);
        }

        return "sha256:" + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();

        void AppendString(string value)
        {
            if (!stringBytes.TryGetValue(value, out var bytes))
            {
                bytes = Encoding.UTF8.GetBytes(value);
                stringBytes.Add(value, bytes);
            }

            AppendInt32(bytes.Length);
            hash.AppendData(bytes);
        }

        void AppendInt32(int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            hash.AppendData(bytes);
        }

        void AppendInt64(long value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
            hash.AppendData(bytes);
        }
    }

    private static bool Contains(WorldRectangle bounds, WorldAddress address) =>
        address.X >= bounds.MinX &&
        (Int128)address.X < (Int128)bounds.MinX + bounds.Width &&
        address.Y >= bounds.MinY &&
        (Int128)address.Y < (Int128)bounds.MinY + bounds.Height;
}
