using System;

namespace CIS580.Simulation;

/// <summary>
/// A random number generator that produces the same sequence of numbers on every machine
/// for a given seed, and whose state can be saved and restored.
/// </summary>
/// <remarks>
/// System.Random does not guarantee the same sequence across .NET versions, and its state cannot
/// be saved, so it is unsuitable for a simulation that must be reproducible (for networking or
/// replays). This class implements the PCG32 algorithm (https://www.pcg-random.org/).
/// </remarks>
public sealed class DeterministicRandom
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong Increment = 1442695040888963407UL;

    /// <summary>
    /// The generator's complete internal state. Save this to save the generator,
    /// and set it to restore the generator to that point in its sequence.
    /// </summary>
    public ulong State { get; set; }

    /// <summary>
    /// Creates a generator with the given seed
    /// </summary>
    public DeterministicRandom(ulong seed)
    {
        State = 0;
        NextUInt();
        State += seed;
        NextUInt();
    }

    /// <summary>
    /// Returns a random 32-bit unsigned integer
    /// </summary>
    public uint NextUInt()
    {
        ulong old = State;
        State = unchecked(old * Multiplier + Increment);
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rotation = (int)(old >> 59);
        return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
    }

    /// <summary>
    /// Returns a random integer from 0 up to, but not including, <paramref name="maxExclusive"/>
    /// </summary>
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than zero.");

        // Reject values from the incomplete final "bucket" so every result is equally likely
        uint bound = (uint)maxExclusive;
        uint threshold = unchecked(0u - bound) % bound;   // (2^32 - bound) % bound
        uint value;
        do value = NextUInt(); while (value < threshold);
        return (int)(value % bound);
    }

    /// <summary>
    /// Returns a random integer from <paramref name="minInclusive"/> up to, but not including,
    /// <paramref name="maxExclusive"/>
    /// </summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than minInclusive.");
        return minInclusive + NextInt(maxExclusive - minInclusive);
    }

    /// <summary>
    /// Returns a random float from 0 up to, but not including, 1
    /// </summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / (1 << 24));

    /// <summary>
    /// Returns a random float from <paramref name="min"/> up to, but not including, <paramref name="max"/>
    /// </summary>
    public float NextFloat(float min, float max) => min + NextFloat() * (max - min);

    /// <summary>
    /// Returns true with the given probability (0 to 1)
    /// </summary>
    public bool Chance(float probability) => NextFloat() < probability;
}
