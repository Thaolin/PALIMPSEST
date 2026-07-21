using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Chronicle.VisualPack;

namespace Chronicle.VisualCompiler;

internal static class DeterministicSelection
{
    public static int SelectVariant(
        ulong seed,
        int visualStyleVersion,
        string familyId,
        VisualLayer layer,
        long x,
        long y,
        int variantCount,
        int? adjacencyMask = null,
        ulong salt = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyId);
        if (visualStyleVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(visualStyleVersion));
        }
        if (variantCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(variantCount));
        }
        if (!Enum.IsDefined(layer))
        {
            throw new ArgumentOutOfRangeException(nameof(layer));
        }
        if (adjacencyMask is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(adjacencyMask));
        }

        var framed = string.Format(
            CultureInfo.InvariantCulture,
            "pgen-selection-v1\nseed={0}\nstyle={1}\nfamily={2}\nlayer={3}\nx={4}\ny={5}\nmask={6}\nsalt={7}\n",
            seed,
            visualStyleVersion,
            familyId,
            (int)layer,
            x,
            y,
            adjacencyMask ?? -1,
            salt);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(framed));
        var value = BinaryPrimitives.ReadUInt64LittleEndian(digest);
        return (int)(value % (ulong)variantCount);
    }
}
