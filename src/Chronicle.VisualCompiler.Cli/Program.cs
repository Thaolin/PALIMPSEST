using Chronicle.VisualCompiler;
using Chronicle.VisualPack;
using System.Text.Json;

if (!TryArguments(args, out var cataloguePath, out var outputPath))
{
    Console.Error.WriteLine(
        "usage: chronicle-visuals build --catalogue <path> --output <directory>");
    return 2;
}

string? stagingPath = null;
try
{
    var destinationPath = Path.GetFullPath(outputPath);
    if (Path.GetPathRoot(destinationPath) == destinationPath)
    {
        throw new InvalidOperationException("CVC-IO-002: output cannot be a filesystem root.");
    }
    stagingPath = destinationPath + ".staging";
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

    var source = File.ReadAllBytes(cataloguePath);
    var first = VisualCompiler.Compile(
        VisualCatalogue.ParseJson(source),
        new CompilationOptions(ReviewMode.Standard));
    var second = VisualCompiler.Compile(
        VisualCatalogue.ParseJson(source),
        new CompilationOptions(ReviewMode.Standard));
    if (!first.Succeeded || first.Pack is null)
    {
        foreach (var diagnostic in first.Diagnostics)
        {
            Console.Error.WriteLine(
                $"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Subject}: {diagnostic.Message}");
        }
        return 1;
    }
    if (!second.Succeeded ||
        first.PackDigest != second.PackDigest ||
        !FilesEqual(first.ReviewFiles, second.ReviewFiles))
    {
        Console.Error.WriteLine("CVC-DET-001: repeated compilation changed output.");
        return 1;
    }

    var canonical = PackCodec.WriteCanonical(first.Pack);
    Directory.CreateDirectory(stagingPath);
    File.WriteAllText(
        stagingMarker,
        "chronicle.visual-compiler-output.v1\n",
        new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    foreach (var file in canonical.Files)
    {
        Write(file.Path, file.Bytes.AsSpan());
    }
    foreach (var file in first.ReviewFiles)
    {
        Write(file.Path, file.Bytes.AsSpan());
    }
    if (Directory.Exists(destinationPath))
    {
        Directory.Delete(destinationPath, recursive: true);
    }
    Directory.Move(stagingPath, destinationPath);
    stagingPath = null;

    Console.WriteLine(first.PackDigest);
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
    FormatException or
    JsonException or
    InvalidOperationException)
{
    if (stagingPath is not null &&
        Directory.Exists(stagingPath) &&
        File.Exists(Path.Combine(stagingPath, ".chronicle-visual-compiler-output")))
    {
        Directory.Delete(stagingPath, recursive: true);
    }
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static bool TryArguments(
    string[] arguments,
    out string cataloguePath,
    out string outputPath)
{
    cataloguePath = "";
    outputPath = "";
    if (arguments.Length != 5 || arguments[0] != "build")
    {
        return false;
    }
    for (var index = 1; index < arguments.Length; index += 2)
    {
        if (arguments[index] == "--catalogue")
        {
            cataloguePath = arguments[index + 1];
        }
        else if (arguments[index] == "--output")
        {
            outputPath = arguments[index + 1];
        }
        else
        {
            return false;
        }
    }
    return cataloguePath.Length != 0 && outputPath.Length != 0;
}

static bool FilesEqual(
    IReadOnlyList<ReviewFile> left,
    IReadOnlyList<ReviewFile> right) =>
    left.Count == right.Count &&
    left.Zip(right).All(static pair =>
        pair.First.Path == pair.Second.Path &&
        pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes.AsSpan()));
