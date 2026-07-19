using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Chronicle.VisualPack;

internal static partial class PackValidator
{
    public static ImmutableArray<PackDiagnostic> Validate(CompiledVisualPack pack)
    {
        var diagnostics = new List<PackDiagnostic>();

        if (pack.Compatibility.PackFormatVersion != PackVersions.PackFormatVersion)
        {
            Error("CVP-FMT-003", pack.PackId, "Unsupported pack format version.");
        }

        CheckIdentifiers(pack.Palettes.Select(static item => item.Id), "palette");
        CheckIdentifiers(pack.Atlases.Select(static item => item.Id), "atlas");
        foreach (var identifier in pack.Visuals.Select(static item => item.Id))
        {
            if (!IdentifierPattern().IsMatch(identifier))
            {
                Error("CVP-ID-001", identifier, "Invalid visual identifier.");
            }
        }
        foreach (var duplicate in pack.Visuals
                     .GroupBy(static visual => new VisualKey(
                         visual.Id,
                         visual.NativeSize,
                         visual.VariantOrdinal,
                         visual.AdjacencyMask))
                     .Where(static group => group.Count() > 1))
        {
            Error("CVP-ID-002", duplicate.Key.Id,
                "Duplicate visual key for native size, variant, and adjacency mask.");
        }

        foreach (var palette in pack.Palettes)
        {
            if (palette.Entries is { Length: < 1 or > 256 })
            {
                Error("CVP-PAL-001", palette.Id, "Palette must contain 1 through 256 entries.");
            }
            else if (palette.TransparentIndex != 0 || palette.Entries[0].A != 0)
            {
                Error("CVP-PAL-002", palette.Id, "Palette index 0 must be transparent.");
            }

            foreach (var role in palette.Roles)
            {
                if (role.Index >= palette.Entries.Length)
                {
                    Error("CVP-PAL-003", palette.Id, $"Role '{role.Name}' index is out of range.");
                }
            }

            if (!StringComparer.Ordinal.Equals(
                    palette.Digest,
                    PackDigests.Palette(palette.Entries)))
            {
                Error("CVP-DIG-003", palette.Id, "Palette digest does not match its entries.");
            }
        }

        foreach (var atlas in pack.Atlases)
        {
            var indices = pack.GetAtlasIndices(atlas.Id);
            if (atlas.Width <= 0 || atlas.Height <= 0 ||
                indices.Length != atlas.Width * atlas.Height)
            {
                Error("CVP-ATL-001", atlas.Id, "Atlas dimensions do not match indexed buffer.");
            }

            if (!StringComparer.Ordinal.Equals(atlas.Digest, PackDigests.Bytes(indices.Span)))
            {
                Error("CVP-DIG-004", atlas.Id, "Atlas digest does not match its indexed buffer.");
            }

            if (atlas.CompatiblePalettes.IsEmpty)
            {
                Error("CVP-PAL-005", atlas.Id, "Atlas declares no compatible palette.");
            }

            foreach (var paletteId in atlas.CompatiblePalettes)
            {
                var palette = pack.Palettes.FirstOrDefault(item => item.Id == paletteId);
                if (palette is null)
                {
                    Error("CVP-PAL-005", $"{atlas.Id}:{paletteId}",
                        "Atlas references an unknown compatible palette.");
                    continue;
                }

                if (palette.Entries.Length < 256 &&
                    indices.Span.IndexOfAnyInRange(
                        (byte)palette.Entries.Length,
                        byte.MaxValue) >= 0)
                {
                    Error("CVP-PAL-004", $"{atlas.Id}:{paletteId}",
                        "Atlas contains an index outside a compatible palette.");
                }
            }
        }

        foreach (var visual in pack.Visuals)
        {
            var atlas = pack.Atlases.FirstOrDefault(item => item.Id == visual.AtlasId);
            if (atlas is null ||
                visual.Rectangle.X < 0 ||
                visual.Rectangle.Y < 0 ||
                visual.Rectangle.Width <= 0 ||
                visual.Rectangle.Height <= 0 ||
                visual.Rectangle.X + visual.Rectangle.Width > atlas.Width ||
                visual.Rectangle.Y + visual.Rectangle.Height > atlas.Height)
            {
                Error("CVP-VIS-001", visual.Id, "Visual rectangle is outside its atlas.");
            }

            if (atlas is not null && visual.NativeSize != atlas.NativeSize)
            {
                Error("CVP-VIS-006", visual.Id,
                    "Visual native size differs from its atlas.");
            }

            if (visual.AdjacencyMask is < 0 or > 15)
            {
                Error("CVP-ADJ-001", visual.Id, "Adjacency mask must be in the range 0 through 15.");
            }

            if (visual.Anchor.X < 0 ||
                visual.Anchor.Y < 0 ||
                visual.Anchor.X >= visual.LogicalSize.Width ||
                visual.Anchor.Y >= visual.LogicalSize.Height)
            {
                Error("CVP-VIS-004", visual.Id, "Visual anchor is outside its logical size.");
            }

            if (!Enum.IsDefined(visual.Layer))
            {
                Error("CVP-VIS-005", visual.Id, "Visual layer is unsupported.");
            }

            const TransformFlags supported =
                TransformFlags.FlipHorizontal |
                TransformFlags.FlipVertical |
                TransformFlags.RotateQuarter;
            if ((visual.AllowedTransforms & ~supported) != 0)
            {
                Error("CVP-VIS-002", visual.Id, "Visual declares an unsupported transform.");
            }

            if (visual.RequireConnected &&
                atlas is not null &&
                !PixelConnectivity.IsFourConnected(
                    pack.GetAtlasIndices(atlas.Id).Span,
                    atlas.Width,
                    visual.Rectangle))
            {
                Error("CVP-OCC-001", visual.Id, "Occupied pixels are disconnected.");
            }

            if (atlas is not null)
            {
                foreach (var role in visual.PaletteRoles)
                {
                    if (atlas.CompatiblePalettes
                        .Select(id => pack.Palettes.FirstOrDefault(palette => palette.Id == id))
                        .Where(static palette => palette is not null)
                        .Any(palette => !palette!.Roles.Any(item => item.Name == role)))
                    {
                        Error("CVP-PAL-006", $"{visual.Id}:{role}",
                            "Visual palette role is missing from a compatible palette.");
                    }
                }

                var expectedGeometry = PackDigests.Geometry(
                    visual.Rectangle,
                    visual.LogicalSize,
                    visual.Anchor,
                    visual.NativeSize,
                    visual.AdjacencyMask,
                    visual.AllowedTransforms,
                    visual.RequireConnected,
                    pack.GetAtlasIndices(atlas.Id).Span,
                    atlas.Width);
                if (!StringComparer.Ordinal.Equals(visual.GeometryDigest, expectedGeometry))
                {
                    Error("CVP-DIG-005", visual.Id,
                        "Visual geometry digest does not match its occupied pixels.");
                }
            }
        }

        foreach (var atlasVisuals in pack.Visuals
                     .OrderBy(static visual => visual.Id, StringComparer.Ordinal)
                     .GroupBy(static visual => visual.AtlasId, StringComparer.Ordinal))
        {
            var visuals = atlasVisuals.ToArray();
            // ponytail: specimen atlases are small; replace with a sweep only if profiling requires it.
            for (var left = 0; left < visuals.Length; left++)
            {
                for (var right = left + 1; right < visuals.Length; right++)
                {
                    if (Overlaps(visuals[left].Rectangle, visuals[right].Rectangle))
                    {
                        Error(
                            "CVP-ATL-002",
                            $"{visuals[left].Id}|{visuals[right].Id}",
                            "Visual rectangles overlap.");
                    }
                }
            }
        }

        foreach (var duplicate in pack.Visuals
                     .GroupBy(static visual => (
                         visual.FamilyId,
                         visual.NativeSize,
                         visual.VariantOrdinal,
                         visual.AdjacencyMask))
                     .Where(static group => group.Count() > 1))
        {
            Error(
                "CVP-VAR-001",
                duplicate.Key.FamilyId,
                "Family contains a duplicate variant ordinal for the same native size and adjacency mask.");
        }

        foreach (var motif in pack.Motifs)
        {
            if (motif.Footprint.Width <= 0 ||
                motif.Footprint.Height <= 0 ||
                motif.VariantCount <= 0 ||
                !Enum.IsDefined(motif.ClippingBehavior) ||
                motif.AnchorCell.X < 0 ||
                motif.AnchorCell.Y < 0 ||
                motif.AnchorCell.X >= motif.Footprint.Width ||
                motif.AnchorCell.Y >= motif.Footprint.Height)
            {
                Error("CVP-MOT-001", motif.FamilyId, "Motif footprint or anchor is invalid.");
            }

            foreach (var mark in motif.Marks)
            {
                if (mark.VariantOrdinal < 0 ||
                    mark.VariantOrdinal >= motif.VariantCount)
                {
                    Error(
                        "CVP-MOT-004",
                        motif.FamilyId,
                        "Motif mark variant is outside the declared range.");
                }

                if (!pack.Visuals.Any(visual =>
                        visual.Id == mark.VisualId &&
                        visual.VariantOrdinal == mark.VariantOrdinal))
                {
                    Error("CVP-MOT-002", $"{motif.FamilyId}:{mark.VisualId}",
                        "Motif mark references a missing visual variant.");
                }

                if (mark.Cell.X < 0 ||
                    mark.Cell.Y < 0 ||
                    mark.Cell.X >= motif.Footprint.Width ||
                    mark.Cell.Y >= motif.Footprint.Height)
                {
                    Error("CVP-MOT-003", motif.FamilyId,
                        "Motif mark cell is outside the footprint.");
                }
            }

            for (var variant = 0; variant < motif.VariantCount; variant++)
            {
                if (!motif.Marks.Any(mark => mark.VariantOrdinal == variant))
                {
                    Error(
                        "CVP-MOT-004",
                        motif.FamilyId,
                        $"Motif variant {variant} has no ordered marks.");
                }
            }
        }

        foreach (var adjacency in pack.Adjacencies)
        {
            if (adjacency.RequiredMasks.Any(static mask => mask is < 0 or > 15) ||
                adjacency.FallbackMask is < 0 or > 15)
            {
                Error("CVP-ADJ-003", adjacency.FamilyId,
                    "Adjacency required or fallback mask is outside 0 through 15.");
            }

            var familyVisuals = pack.Visuals
                .Where(visual => visual.FamilyId == adjacency.FamilyId)
                .ToArray();
            var expectedVariants = familyVisuals
                .Select(static visual => visual.VariantOrdinal)
                .Distinct()
                .Order()
                .DefaultIfEmpty(0)
                .ToArray();
            foreach (var nativeSize in pack.RequiredNativeSizes.Order())
            {
                foreach (var variant in expectedVariants)
                {
                    var available = familyVisuals
                        .Where(visual =>
                            visual.NativeSize == nativeSize &&
                            visual.VariantOrdinal == variant)
                        .Select(static visual => visual.AdjacencyMask)
                        .Where(static mask => mask.HasValue)
                        .Select(static mask => mask!.Value)
                        .ToHashSet();
                    foreach (var mask in adjacency.RequiredMasks)
                    {
                        if (!available.Contains(mask) &&
                            adjacency.FallbackMask is null)
                        {
                            Error(
                                "CVP-ADJ-002",
                                $"{adjacency.FamilyId}:{nativeSize}:{variant}:{mask}",
                                "Required adjacency mask has no visual or fallback.");
                        }
                    }

                    if (adjacency.FallbackMask is int fallback &&
                        !available.Contains(fallback))
                    {
                        Error(
                            "CVP-ADJ-003",
                            $"{adjacency.FamilyId}:{nativeSize}:{variant}",
                            "Adjacency fallback mask has no visual.");
                    }
                }
            }

            if (adjacency.RequireEdgeContinuity &&
                !HasContinuousEdges(pack, adjacency.FamilyId))
            {
                Error("CVP-ADJ-004", adjacency.FamilyId,
                    "Declared adjacency edge signatures do not continue.");
            }
        }

        foreach (var mapping in pack.RequiredMappings)
        {
            if (!pack.Visuals.Any(visual => visual.Id == mapping))
            {
                Error("CVP-MAP-001", mapping, "Required mapping has no visual.");
            }
        }

        foreach (var nativeSize in pack.RequiredNativeSizes)
        {
            if (!pack.Visuals.Any(visual => visual.NativeSize == nativeSize))
            {
                Error("CVP-VIS-003", nativeSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "Required native size has no visual.");
            }
        }

        return diagnostics
            .OrderBy(static item => item.Code, StringComparer.Ordinal)
            .ThenBy(static item => item.Subject, StringComparer.Ordinal)
            .ThenBy(static item => item.Message, StringComparer.Ordinal)
            .ToImmutableArray();

        void Error(string code, string subject, string message) =>
            diagnostics.Add(new PackDiagnostic(code, DiagnosticSeverity.Error, subject, message));

        void CheckIdentifiers(IEnumerable<string> identifiers, string category)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var identifier in identifiers)
            {
                if (!IdentifierPattern().IsMatch(identifier))
                {
                    Error("CVP-ID-001", identifier, $"Invalid {category} identifier.");
                }
                else if (!seen.Add(identifier))
                {
                    Error("CVP-ID-002", identifier, $"Duplicate {category} identifier.");
                }
            }
        }

        static bool Overlaps(PixelRect left, PixelRect right) =>
            left.X < right.X + right.Width &&
            right.X < left.X + left.Width &&
            left.Y < right.Y + right.Height &&
            right.Y < left.Y + left.Height;

        static bool HasContinuousEdges(CompiledVisualPack pack, string familyId)
        {
            var byNativeSize = pack.Visuals
                .Where(visual =>
                    visual.FamilyId == familyId && visual.AdjacencyMask.HasValue)
                .GroupBy(static visual => visual.NativeSize);
            foreach (var group in byNativeSize)
            {
                var horizontal = new List<string>();
                var vertical = new List<string>();
                foreach (var visual in group)
                {
                    var atlas = pack.Atlases.First(item => item.Id == visual.AtlasId);
                    var pixels = pack.GetAtlasIndices(atlas.Id).Span;
                    var north = Edge(visual, atlas, pixels, 0, 0, 1, 0);
                    var east = Edge(
                        visual,
                        atlas,
                        pixels,
                        visual.Rectangle.Width - 1,
                        0,
                        0,
                        1);
                    var south = Edge(
                        visual,
                        atlas,
                        pixels,
                        0,
                        visual.Rectangle.Height - 1,
                        1,
                        0);
                    var west = Edge(visual, atlas, pixels, 0, 0, 0, 1);
                    if ((visual.AdjacencyMask!.Value & 1) != 0)
                    {
                        horizontal.Add(north);
                    }
                    else if (north.Contains('1'))
                    {
                        return false;
                    }
                    if ((visual.AdjacencyMask.Value & 4) != 0)
                    {
                        horizontal.Add(south);
                    }
                    else if (south.Contains('1'))
                    {
                        return false;
                    }
                    if ((visual.AdjacencyMask.Value & 2) != 0)
                    {
                        vertical.Add(east);
                    }
                    else if (east.Contains('1'))
                    {
                        return false;
                    }
                    if ((visual.AdjacencyMask.Value & 8) != 0)
                    {
                        vertical.Add(west);
                    }
                    else if (west.Contains('1'))
                    {
                        return false;
                    }
                }

                if (!Matches(horizontal) || !Matches(vertical))
                {
                    return false;
                }
            }

            return true;

            static bool Matches(List<string> signatures) =>
                signatures.Count == 0 ||
                signatures.Count >= 2 &&
                signatures.All(signature =>
                    signature.Contains('1') &&
                    signature == signatures[0]);

            static string Edge(
                VisualRecord visual,
                AtlasRecord atlas,
                ReadOnlySpan<byte> pixels,
                int startX,
                int startY,
                int stepX,
                int stepY)
            {
                var length = stepX == 0
                    ? visual.Rectangle.Height
                    : visual.Rectangle.Width;
                var chars = new char[length];
                for (var index = 0; index < length; index++)
                {
                    var x = visual.Rectangle.X + startX + stepX * index;
                    var y = visual.Rectangle.Y + startY + stepY * index;
                    chars[index] = pixels[y * atlas.Width + x] == 0 ? '0' : '1';
                }

                return new string(chars);
            }
        }
    }

    [GeneratedRegex("^[a-z0-9]+(?:[.-][a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();
}
