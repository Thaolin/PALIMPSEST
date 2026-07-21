using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicle.VisualCompiler;
using Chronicle.VisualPack;

static class AcceptedReferenceConformance
{
    private const string FixtureResource =
        "Chronicle.Visuals.Conformance.palimpsest20.accepted-reference.json";
    private const string CatalogueResource =
        "Chronicle.Visuals.Conformance.palimpsest20.catalogue.json";

    private static readonly JsonSerializerOptions FixtureJson = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static bool Run()
    {
        var passed = true;

        var fixture = LoadCommittedFixture();
        var pack = CompileCandidatePack();
        var errors = AcceptedReferenceFixture.Validate(fixture);
        if (errors.Length > 0)
        {
            Console.Error.WriteLine(
                $"CVC-E45-REF-POSITIVE: committed fixture failed validation: {errors[0]}");
            passed = false;
        }

        passed &= RejectWrongIndexedBuffer(fixture);
        passed &= RejectWrongIndexedDigest(fixture);
        passed &= RejectWrongRgbaDigest(fixture);
        passed &= RejectDuplicateVisualId(fixture);
        passed &= RejectMissingVisualId(fixture);
        passed &= RejectChangedPalette(fixture);
        passed &= RejectWrongProvenance(fixture);
        passed &= RejectWrongAggregateDigest(fixture);
        passed &= CompareAllAcceptedVisuals(fixture, pack);
        passed &= RejectRenamedVisualId(fixture, pack);

        return passed;
    }

    private static AcceptedReferenceFixture LoadCommittedFixture()
    {
        using var stream = typeof(AcceptedReferenceConformance).Assembly
            .GetManifestResourceStream(FixtureResource)
            ?? throw new InvalidOperationException(
                "PAL20-CONFORMANCE: accepted-reference fixture is missing from the conformance assembly.");
        return JsonSerializer.Deserialize<AcceptedReferenceFixture>(stream, FixtureJson)
            ?? throw new FormatException(
                "PAL20-CONFORMANCE: failed to deserialize accepted-reference fixture.");
    }

    private static CompiledVisualPack CompileCandidatePack()
    {
        using var stream = typeof(AcceptedReferenceConformance).Assembly
            .GetManifestResourceStream(CatalogueResource)
            ?? throw new InvalidOperationException(
                "PAL20-CONFORMANCE: E4.5 catalogue is missing from the conformance assembly.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var catalogue = VisualCatalogue.ParseJson(bytes.ToArray());
        var result = VisualCompiler.Compile(
            catalogue,
            new CompilationOptions(ReviewMode.None));
        if (!result.Succeeded || result.Pack is null)
        {
            throw new InvalidOperationException(
                "PAL20-CONFORMANCE: E4.5 candidate pack did not compile.");
        }
        return result.Pack;
    }

    private static bool RejectWrongIndexedBuffer(AcceptedReferenceFixture fixture)
    {
        var visual = fixture.Visuals[0];
        var corrupted = new byte[400];
        Array.Copy(visual.IndexedBuffer!, corrupted, 400);
        corrupted[0] = (byte)(corrupted[0] == 0 ? 1 : 0);
        var modified = fixture with
        {
            Visuals = fixture.Visuals.SetItem(
                0,
                visual with { IndexedBuffer = corrupted })
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("indexedDigest mismatch", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-INDEXED-BUFFER: changed indexed buffer was not rejected.");
            return false;
        }
        return true;
    }

    private static bool RejectWrongIndexedDigest(AcceptedReferenceFixture fixture)
    {
        var visual = fixture.Visuals[0];
        var modified = fixture with
        {
            Visuals = fixture.Visuals.SetItem(
                0,
                visual with
                {
                    IndexedDigest =
                        "sha256:0000000000000000000000000000000000000000000000000000000000000000"
                })
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("indexedDigest mismatch", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-INDEXED-DIGEST: wrong indexed digest was not rejected.");
            return false;
        }
        return true;
    }

    private static bool RejectWrongRgbaDigest(AcceptedReferenceFixture fixture)
    {
        var visual = fixture.Visuals[0];
        var modified = fixture with
        {
            Visuals = fixture.Visuals.SetItem(
                0,
                visual with
                {
                    RgbaDigest =
                        "sha256:0000000000000000000000000000000000000000000000000000000000000000"
                })
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("rgbaDigest mismatch", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-RGBA-DIGEST: wrong rgba digest was not rejected.");
            return false;
        }
        return true;
    }

    private static bool RejectDuplicateVisualId(AcceptedReferenceFixture fixture)
    {
        var visual = fixture.Visuals[0];
        var modified = fixture with
        {
            Visuals = fixture.Visuals.SetItem(
                0,
                visual with { VisualId = fixture.Visuals[1].VisualId })
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("Duplicate visual ID", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-DUPLICATE-ID: duplicate visual ID was not rejected.");
            return false;
        }
        return true;
    }

    private static bool RejectMissingVisualId(AcceptedReferenceFixture fixture)
    {
        var visual = fixture.Visuals[0];
        var modified = fixture with
        {
            Visuals = fixture.Visuals.SetItem(0, visual with { VisualId = "" })
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("blank ID", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-MISSING-ID: missing visual ID was not rejected.");
            return false;
        }
        return true;
    }

    private static bool RejectChangedPalette(AcceptedReferenceFixture fixture)
    {
        var palette = fixture.Palette.ToArray();
        palette[0] = palette[0] with { Rgba = "FF0000FF" };
        var modified = fixture with
        {
            Palette = ImmutableArray.Create(palette)
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("rgbaDigest mismatch", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-PALETTE: changed palette was not rejected.");
            return false;
        }
        return true;
    }

    private static bool RejectWrongProvenance(AcceptedReferenceFixture fixture)
    {
        var modified = fixture with
        {
            Provenance = fixture.Provenance with { SourceCommit = "" }
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("SourceCommit", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-PROVENANCE: wrong provenance was not rejected.");
            return false;
        }
        return true;
    }

    private static bool RejectWrongAggregateDigest(AcceptedReferenceFixture fixture)
    {
        var modified = fixture with
        {
            AggregateDigest =
                "sha256:0000000000000000000000000000000000000000000000000000000000000000"
        };
        var errors = AcceptedReferenceFixture.Validate(modified);
        if (!errors.Any(e => e.Contains("Aggregate digest mismatch", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-AGGREGATE: wrong aggregate digest was not rejected.");
            return false;
        }
        return true;
    }

    private static bool CompareAllAcceptedVisuals(
        AcceptedReferenceFixture fixture,
        CompiledVisualPack pack)
    {
        var comparisons = ReviewRenderer.BuildReferenceComparisons(
            pack,
            fixture,
            fixture.Provenance.NativeSize);
        if (comparisons.Length != 64 ||
            !comparisons.Select(static comparison => comparison.Reference.VisualId)
                .SequenceEqual(
                    fixture.Visuals.Select(static visual => visual.VisualId),
                    StringComparer.Ordinal))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-COVERAGE: comparison output does not cover all 64 accepted visuals in fixture order.");
            return false;
        }
        return true;
    }

    private static bool RejectRenamedVisualId(
        AcceptedReferenceFixture fixture,
        CompiledVisualPack pack)
    {
        var index = Array.FindIndex(
            fixture.Visuals.ToArray(),
            static visual => visual.VisualId == "actor.incarnation");
        var renamed = fixture with
        {
            Visuals = fixture.Visuals.SetItem(
                index,
                fixture.Visuals[index] with
                {
                    VisualId = "actor.incarnation-renamed"
                })
        };

        var errors = AcceptedReferenceFixture.Validate(renamed);
        if (!errors.Any(static error =>
                error.Contains("Visual ID set digest mismatch", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "CVC-E45-REF-ID-SET: renamed visual ID was not rejected by fixture validation.");
            return false;
        }

        try
        {
            _ = ReviewRenderer.BuildReferenceComparisons(
                pack,
                renamed,
                renamed.Provenance.NativeSize);
        }
        catch (FormatException exception) when (
            exception.Message.Contains("no candidate matches", StringComparison.Ordinal))
        {
            return true;
        }

        Console.Error.WriteLine(
            "CVC-E45-REF-COMPARE-ID: renamed visual ID was not rejected by comparison construction.");
        return false;
    }
}
