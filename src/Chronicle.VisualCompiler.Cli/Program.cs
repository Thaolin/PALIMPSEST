using Chronicle.VisualCompiler;
using Chronicle.VisualPack;
using System.Text.Json;

if (!TryArguments(args, out var cataloguePath, out var outputPath))
{
    Console.Error.WriteLine(
        "usage: chronicle-visuals build --profile Palimpsest20 --catalogue <path> --output <directory>");
    return 2;
}

string? stagingPath = null;
string? destinationPath = null;
string? backupPath = null;
try
{
    destinationPath = Path.GetFullPath(outputPath);
    if (Path.GetPathRoot(destinationPath) == destinationPath)
    {
        throw new InvalidOperationException("CVC-IO-002: output cannot be a filesystem root.");
    }
    stagingPath = destinationPath + ".staging";
    backupPath = destinationPath + ".backup";
    var ownershipMarker = Path.Combine(
        destinationPath,
        ".chronicle-visual-compiler-output");
    if (Directory.Exists(destinationPath) && !File.Exists(ownershipMarker))
    {
        throw new InvalidOperationException(
            "CVC-IO-003: refusing to replace an output directory not owned by this compiler.");
    }
    var stagingMarker = Path.Combine(
        stagingPath,
        ".chronicle-visual-compiler-output");
    if (Directory.Exists(stagingPath))
    {
        if (!File.Exists(stagingMarker))
        {
            throw new InvalidOperationException(
                "CVC-IO-004: refusing to replace an unowned staging directory.");
        }
        Directory.Delete(stagingPath, recursive: true);
    }
    var backupMarker = Path.Combine(
        backupPath,
        ".chronicle-visual-compiler-output");
    if (Directory.Exists(backupPath))
    {
        if (!File.Exists(backupMarker))
        {
            throw new InvalidOperationException(
                "CVC-IO-005: refusing to replace an unowned backup directory.");
        }
        if (Directory.Exists(destinationPath))
        {
            Directory.Delete(backupPath, recursive: true);
        }
        else
        {
            Directory.Move(backupPath, destinationPath);
        }
    }

    var source = File.ReadAllBytes(cataloguePath);
    var first = VisualCompiler.CompilePalimpsest20(
        VisualCatalogue.ParseJson(source),
        new CompilationOptions(ReviewMode.Standard));
    var second = VisualCompiler.CompilePalimpsest20(
        VisualCatalogue.ParseJson(source),
        new CompilationOptions(ReviewMode.Standard));
    if (!first.Succeeded || first.Pack is null || first.Validation is null)
    {
        foreach (var diagnostic in first.Diagnostics)
        {
            Console.Error.WriteLine(
                $"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Subject}: {diagnostic.Message}");
        }
        return 1;
    }
    if (!second.Succeeded ||
        second.Pack is null ||
        second.Validation is null)
    {
        Console.Error.WriteLine("CVC-DET-001: repeated compilation changed output.");
        return 1;
    }

    var canonical = Palimpsest20Codec.WriteCanonical(first.Pack, first.Validation);
    var repeatedCanonical =
        Palimpsest20Codec.WriteCanonical(second.Pack, second.Validation);
    if (!CanonicalFilesEqual(canonical.Files, repeatedCanonical.Files) ||
        !FilesEqual(first.ReviewFiles, second.ReviewFiles))
    {
        Console.Error.WriteLine("CVC-DET-001: repeated compilation changed output.");
        return 1;
    }

    Directory.CreateDirectory(stagingPath);
    File.WriteAllText(
        stagingMarker,
        "chronicle.visual-compiler-output.v1\n",
        new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    foreach (var file in canonical.Files)
    {
        Write($"pack/{file.Path}", file.Bytes.AsSpan());
    }
    foreach (var file in first.ReviewFiles)
    {
        Write(file.Path, file.Bytes.AsSpan());
    }
    if (Directory.Exists(destinationPath))
    {
        Directory.Move(destinationPath, backupPath);
    }
    try
    {
        Directory.Move(stagingPath, destinationPath);
        stagingPath = null;
    }
    catch
    {
        if (!Directory.Exists(destinationPath) &&
            Directory.Exists(backupPath))
        {
            Directory.Move(backupPath, destinationPath);
            backupPath = null;
        }
        throw;
    }
    if (Directory.Exists(backupPath))
    {
        Directory.Delete(backupPath, recursive: true);
    }
    backupPath = null;

    Console.WriteLine(canonical.AggregateDigest);
    return 0;

    void Write(string relativePath, ReadOnlySpan<byte> bytes)
    {
        var path = Path.GetFullPath(
            Path.Combine(stagingPath!, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(stagingPath!) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("CVC-IO-001: output path escaped its directory.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes.ToArray());
    }
}
catch (Exception exception) when (
    exception is IOException or
    UnauthorizedAccessException or
    ArgumentException or
    FormatException or
    JsonException or
    InvalidOperationException)
{
    string? rollbackFailure = null;
    if (backupPath is not null &&
        destinationPath is not null &&
        Directory.Exists(backupPath) &&
        !Directory.Exists(destinationPath) &&
        File.Exists(Path.Combine(
            backupPath,
            ".chronicle-visual-compiler-output")))
    {
        try
        {
            Directory.Move(backupPath, destinationPath);
            backupPath = null;
        }
        catch (Exception rollbackException)
        {
            rollbackFailure = rollbackException.Message;
        }
    }
    if (stagingPath is not null &&
        Directory.Exists(stagingPath) &&
        File.Exists(Path.Combine(stagingPath, ".chronicle-visual-compiler-output")))
    {
        Directory.Delete(stagingPath, recursive: true);
    }
    Console.Error.WriteLine(exception.Message);
    if (rollbackFailure is not null)
    {
        Console.Error.WriteLine($"CVC-IO-006: output rollback failed: {rollbackFailure}");
    }
    return 1;
}

static bool TryArguments(
    string[] arguments,
    out string cataloguePath,
    out string outputPath)
{
    cataloguePath = "";
    outputPath = "";
    var hasCatalogue = false;
    var hasOutput = false;
    var hasProfile = false;
    if (arguments.Length != 7 || arguments[0] != "build")
    {
        return false;
    }
    for (var index = 1; index < arguments.Length; index += 2)
    {
        if (arguments[index] == "--catalogue" && !hasCatalogue)
        {
            cataloguePath = arguments[index + 1];
            hasCatalogue = true;
        }
        else if (arguments[index] == "--output" && !hasOutput)
        {
            outputPath = arguments[index + 1];
            hasOutput = true;
        }
        else if (arguments[index] == "--profile" &&
                 arguments[index + 1] == "Palimpsest20" &&
                 !hasProfile)
        {
            hasProfile = true;
        }
        else
        {
            return false;
        }
    }
    return hasCatalogue &&
        hasOutput &&
        hasProfile &&
        cataloguePath.Length != 0 &&
        outputPath.Length != 0;
}

static bool FilesEqual(
    IReadOnlyList<ReviewFile> left,
    IReadOnlyList<ReviewFile> right) =>
    left.Count == right.Count &&
    left.Zip(right).All(static pair =>
        pair.First.Path == pair.Second.Path &&
        pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes.AsSpan()));

static bool CanonicalFilesEqual(
    IReadOnlyList<PackFile> left,
    IReadOnlyList<PackFile> right) =>
    left.Count == right.Count &&
    left.Zip(right).All(static pair =>
        pair.First.Path == pair.Second.Path &&
        pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes.AsSpan()));
