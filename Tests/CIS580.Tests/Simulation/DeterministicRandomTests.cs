using System;
using System.Linq;
using CIS580.Simulation;

namespace CIS580.Tests.Simulation;

public class DeterministicRandomTests
{
    private static uint[] Sequence(DeterministicRandom random, int count)
        => Enumerable.Range(0, count).Select(_ => random.NextUInt()).ToArray();

    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        Assert.Equal(Sequence(new DeterministicRandom(42), 100), Sequence(new DeterministicRandom(42), 100));
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        Assert.NotEqual(Sequence(new DeterministicRandom(1), 10), Sequence(new DeterministicRandom(2), 10));
    }

    [Fact]
    public void Sequence_IsStableAcrossVersions()
    {
        // If this fails, the algorithm changed, which would break saved replays and
        // networked games between different builds.
        var random = new DeterministicRandom(42);
        // (Values from an independent implementation of the PCG32 reference algorithm.)
        Assert.Equal(new uint[] { 0xC2F57BD6, 0x6B07C4A9, 0x72B7B29B }, Sequence(random, 3));
    }

    [Fact]
    public void RestoringState_RepeatsTheSequence()
    {
        var random = new DeterministicRandom(7);
        random.NextUInt();
        ulong saved = random.State;
        var expected = Sequence(random, 20);

        random.State = saved;

        Assert.Equal(expected, Sequence(random, 20));
    }

    [Fact]
    public void NextInt_StaysInRange()
    {
        var random = new DeterministicRandom(3);
        for (int i = 0; i < 10_000; i++)
        {
            int value = random.NextInt(-5, 5);
            Assert.InRange(value, -5, 4);
        }
    }

    [Fact]
    public void NextInt_ProducesEveryValue()
    {
        var random = new DeterministicRandom(3);
        var seen = new bool[6];
        for (int i = 0; i < 1_000; i++)
            seen[random.NextInt(6)] = true;

        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void NextInt_RejectsEmptyRanges()
    {
        var random = new DeterministicRandom(3);
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(5, 5));
    }

    [Fact]
    public void NextFloat_IsFromZeroUpToOne()
    {
        var random = new DeterministicRandom(9);
        for (int i = 0; i < 10_000; i++)
        {
            float value = random.NextFloat();
            Assert.True(value >= 0f && value < 1f, $"{value} is out of range");
        }
    }
}
