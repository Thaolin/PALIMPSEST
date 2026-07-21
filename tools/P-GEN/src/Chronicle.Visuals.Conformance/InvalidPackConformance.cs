using System.Collections.Immutable;
using Chronicle.VisualPack;

static class InvalidPackConformance
{
    // Keep this list in lockstep with fixtures/e0/invalid/cases.json.
    private static readonly ImmutableArray<string> ExpectedCodes =
    [
        "CVP-FMT-003", "CVP-ID-001", "CVP-ID-002", "CVP-PAL-001", "CVP-PAL-002",
        "CVP-PAL-003", "CVP-ATL-001", "CVP-VIS-001", "CVP-ADJ-001", "CVP-MOT-001",
        "CVP-MAP-001", "CVP-FMT-004", "ArgumentException", "CVP-PAL-004", "CVP-PAL-005",
        "CVP-PAL-006", "CVP-DIG-003", "CVP-DIG-004", "CVP-DIG-005", "CVP-ADJ-003",
        "CVP-ADJ-004", "CVP-MOT-002", "CVP-MOT-003", "CVP-PAL-005",
        "CVP-ADJ-002", "CVP-ATL-002", "CVP-OCC-001", "CVP-VAR-001",
        "CVP-VIS-002", "CVP-VIS-003", "CVP-VIS-004", "CVP-VIS-005", "CVP-VIS-006"
    ];

    public static bool Run(CompiledVisualPack reference, CanonicalPackOutput canonical)
    {
        var assertions = new (string Name, CompiledVisualPack Pack, string Code)[]
        {
            ("format version", Copy(reference, compatibility: reference.Compatibility with
            {
                PackFormatVersion = PackVersions.PackFormatVersion + 1
            }), "CVP-FMT-003"),
            ("invalid identifier", Copy(reference, palettes: reference.Palettes.SetItem(0,
                reference.Palettes[0] with { Id = "invalid_identifier" })), "CVP-ID-001"),
            ("duplicate identifier", Copy(reference, palettes: reference.Palettes.Add(
                reference.Palettes[0])), "CVP-ID-002"),
            ("palette count", Copy(reference, palettes: reference.Palettes.SetItem(0,
                reference.Palettes[0] with { Entries = ImmutableArray<Rgba8>.Empty })), "CVP-PAL-001"),
            ("palette transparency", Copy(reference, palettes: reference.Palettes.SetItem(0,
                reference.Palettes[0] with { TransparentIndex = 1 })), "CVP-PAL-002"),
            ("palette role range", Copy(reference, palettes: reference.Palettes.SetItem(0,
                reference.Palettes[0] with
                {
                    Roles = ImmutableArray.Create(new PaletteRole("landmark.gold", 2))
                })), "CVP-PAL-003"),
            ("atlas bytes", Copy(reference, atlasBuffers:
            [
                KeyValuePair.Create(reference.Atlases[0].Id,
                    (ReadOnlyMemory<byte>)reference.GetAtlasIndices(reference.Atlases[0].Id)
                        .Span[..^1].ToArray())
            ]), "CVP-ATL-001"),
            ("visual bounds", Copy(reference, visuals: reference.Visuals.SetItem(0,
                reference.Visuals[0] with { Rectangle = new PixelRect(0, 0, 5, 4) })), "CVP-VIS-001"),
            ("visual native atlas", Copy(reference, visuals: reference.Visuals.SetItem(0,
                reference.Visuals[0] with { NativeSize = 20 })), "CVP-VIS-006"),
            ("adjacency mask", Copy(reference, visuals: reference.Visuals.SetItem(0,
                reference.Visuals[0] with { AdjacencyMask = 16 })), "CVP-ADJ-001"),
            ("motif footprint", Copy(reference, motifs: reference.Motifs.SetItem(0,
                reference.Motifs[0] with { Footprint = new PixelSize(0, 1) })), "CVP-MOT-001"),
            ("required mapping", Copy(reference, requiredMappings:
                ImmutableArray.Create("landmark.missing")), "CVP-MAP-001"),
            ("indexed colour", Copy(reference, atlasBuffers:
            [
                KeyValuePair.Create(reference.Atlases[0].Id,
                    (ReadOnlyMemory<byte>)reference.GetAtlasIndices(reference.Atlases[0].Id)
                        .ToArray().Select(static value => value == 1 ? (byte)2 : value).ToArray())
            ]), "CVP-PAL-004"),
            ("atlas palette", Copy(reference, atlases: reference.Atlases.SetItem(0,
                reference.Atlases[0] with
                {
                    CompatiblePalettes = ImmutableArray.Create("palette.missing")
                })), "CVP-PAL-005"),
            ("empty atlas palette", Copy(reference, atlases: reference.Atlases.SetItem(0,
                reference.Atlases[0] with
                {
                    CompatiblePalettes = ImmutableArray<string>.Empty
                })), "CVP-PAL-005"),
            ("visual palette role", Copy(reference, visuals: reference.Visuals.SetItem(0,
                reference.Visuals[0] with
                {
                    PaletteRoles = ImmutableArray.Create("missing.role")
                })), "CVP-PAL-006"),
            ("palette digest", Copy(reference, palettes: reference.Palettes.SetItem(0,
                reference.Palettes[0] with { Digest = ZeroDigest })), "CVP-DIG-003"),
            ("atlas digest", Copy(reference, atlases: reference.Atlases.SetItem(0,
                reference.Atlases[0] with { Digest = ZeroDigest })), "CVP-DIG-004"),
            ("geometry digest", Copy(reference, visuals: reference.Visuals.SetItem(0,
                reference.Visuals[0] with { GeometryDigest = ZeroDigest })), "CVP-DIG-005"),
            ("adjacency fallback", Copy(reference, adjacencies:
                ImmutableArray.Create(new AdjacencyRecord(
                    reference.Visuals[0].FamilyId,
                    ImmutableArray.Create(0),
                    16,
                    false))), "CVP-ADJ-003"),
            ("edge continuity", Copy(reference,
                visuals: reference.Visuals.SetItem(0, reference.Visuals[0] with
                {
                    AdjacencyMask = 1
                }),
                adjacencies: ImmutableArray.Create(new AdjacencyRecord(
                    reference.Visuals[0].FamilyId,
                    ImmutableArray.Create(1),
                    null,
                    true))), "CVP-ADJ-004"),
            ("motif visual", Copy(reference, motifs: reference.Motifs.SetItem(0,
                reference.Motifs[0] with
                {
                    Marks = ImmutableArray.Create(new MotifMark(
                        "visual.missing",
                        new PixelPoint(0, 0),
                        new PixelPoint(0, 0)))
                })), "CVP-MOT-002"),
            ("motif mark cell", Copy(reference, motifs: reference.Motifs.SetItem(0,
                reference.Motifs[0] with
                {
                    Marks = ImmutableArray.Create(new MotifMark(
                        reference.Visuals[0].Id,
                        new PixelPoint(1, 0),
                        new PixelPoint(0, 0)))
                })), "CVP-MOT-003")
        };

        var passed = true;
        foreach (var assertion in assertions)
        {
            passed &= AssertDiagnostic(assertion.Name, assertion.Pack, assertion.Code);
        }
        passed &= AssertFormatException(
            "duplicate pack path",
            "CVP-FMT-004",
            () => PackCodec.ReadCanonical(canonical.Files.Add(
                canonical.Files.Single(static file => file.Path == "manifest.json"))));
        passed &= AssertArgumentException(
            "invalid pack path",
            () => _ = new PackFile("../manifest.json", Array.Empty<byte>()));

        var assertedCodes = assertions.Select(static assertion => assertion.Code)
            .Append("CVP-FMT-004")
            .Append("ArgumentException")
            .Concat([
                "CVP-ADJ-002", "CVP-ATL-002", "CVP-OCC-001", "CVP-VAR-001",
                "CVP-VIS-002", "CVP-VIS-003", "CVP-VIS-004", "CVP-VIS-005"
            ])
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (!assertedCodes.SequenceEqual(ExpectedCodes.Order(StringComparer.Ordinal)))
        {
            Console.Error.WriteLine("CVP-E0-MATRIX: asserted codes no longer match cases.json.");
            passed = false;
        }

        return passed;
    }

    private static bool AssertDiagnostic(string name, CompiledVisualPack pack, string expectedCode)
    {
        if (PackValidator.Validate(pack).Any(diagnostic => diagnostic.Code == expectedCode))
        {
            return true;
        }

        Console.Error.WriteLine($"CVP-E0-{name}: expected diagnostic {expectedCode} was absent.");
        return false;
    }

    private static bool AssertFormatException(string name, string expectedCode, Action action)
    {
        try
        {
            action();
            Console.Error.WriteLine($"CVP-E0-{name}: expected {expectedCode} rejection was absent.");
            return false;
        }
        catch (FormatException exception) when (exception.Message.StartsWith(
                   expectedCode,
                   StringComparison.Ordinal))
        {
            return true;
        }
    }

    private static bool AssertArgumentException(string name, Action action)
    {
        try
        {
            action();
            Console.Error.WriteLine($"CVP-E0-{name}: expected ArgumentException was absent.");
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static CompiledVisualPack Copy(
        CompiledVisualPack source,
        CompatibilityRecord? compatibility = null,
        ImmutableArray<PaletteRecord>? palettes = null,
        ImmutableArray<AtlasRecord>? atlases = null,
        ImmutableArray<VisualRecord>? visuals = null,
        ImmutableArray<MotifRecord>? motifs = null,
        ImmutableArray<AdjacencyRecord>? adjacencies = null,
        ImmutableArray<string>? requiredMappings = null,
        ImmutableArray<int>? requiredNativeSizes = null,
        IEnumerable<KeyValuePair<string, ReadOnlyMemory<byte>>>? atlasBuffers = null) => new(
        source.PackId,
        compatibility ?? source.Compatibility,
        source.Compiler,
        source.SourceDigest,
        palettes ?? source.Palettes,
        atlases ?? source.Atlases,
        visuals ?? source.Visuals,
        motifs ?? source.Motifs,
        adjacencies ?? source.Adjacencies,
        requiredMappings ?? source.RequiredMappings,
        requiredNativeSizes ?? source.RequiredNativeSizes,
        source.Provenance,
        atlasBuffers ?? source.Atlases.Select(atlas =>
            KeyValuePair.Create(atlas.Id, source.GetAtlasIndices(atlas.Id))),
        source.PackDigest);

    private const string ZeroDigest =
        "sha256:0000000000000000000000000000000000000000000000000000000000000000";
}
