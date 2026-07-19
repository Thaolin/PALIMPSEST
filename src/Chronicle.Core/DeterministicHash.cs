using System.Numerics;

namespace Chronicle.Core;

internal static class DeterministicHash
{
    public static uint Coordinates(long seed, long x, long y, uint salt = 0)
    {
        unchecked
        {
            var hash = (uint)seed;
            hash = (hash ^ MixHigh(seed, 13, 0xD6E8FEB9u)) * 0x9E3779B9u;
            hash = (hash ^ (uint)x ^ MixWideCoordinate(x, 11, 0xA5A3564Du)) * 0x85EBCA6Bu;
            hash = (hash ^ (uint)y ^ MixWideCoordinate(y, 17, 0x9E3779B1u)) * 0xC2B2AE35u;
            hash ^= salt;
            return hash ^ (hash >> 16);
        }
    }

    private static uint MixHigh(long value, int rotation, uint multiplier) =>
        BitOperations.RotateLeft((uint)((ulong)value >> 32), rotation) * multiplier;

    private static uint MixWideCoordinate(long value, int rotation, uint multiplier) =>
        value is >= int.MinValue and <= int.MaxValue
            ? 0
            : MixHigh(value, rotation, multiplier);
}
