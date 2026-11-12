using System;
using System.Numerics;
using CIS580.Simulation;
using MechaHorde.Simulation;

namespace MechaHorde.Tests;

public class GameSessionTests
{
    private static readonly PlayerId One = new(1);
    private static readonly PlayerId Two = new(2);

    /// <summary>
    /// A scripted player who moves in a circle
    /// </summary>
    private static ICommandSource Circling(float speed) => new DelegateCommandSource(tick =>
        new PlayerCommand { Move = new Vector2(MathF.Cos(tick * speed), MathF.Sin(tick * speed)) });

    private static ICommandSource Constant(Vector2 move) =>
        new DelegateCommandSource(_ => new PlayerCommand { Move = move });

    private static (GameSession session, SessionRunner runner) Start(ulong seed)
    {
        var session = new GameSession(seed);
        var runner = new SessionRunner(session);
        return (session, runner);
    }

    [Fact]
    public void SameSeedAndCommands_PlayOutIdentically()
    {
        // This is the property networking and replays depend upon
        var (a, runnerA) = Start(seed: 1234);
        var (b, runnerB) = Start(seed: 1234);
        runnerA.AddPlayer(One, Circling(0.05f));
        runnerA.AddPlayer(Two, Circling(0.11f));
        runnerB.AddPlayer(One, Circling(0.05f));
        runnerB.AddPlayer(Two, Circling(0.11f));

        for (int i = 0; i < 600; i++)
        {
            runnerA.Step();
            runnerB.Step();
        }

        Assert.Equal(a.World.Wanderer.Position, b.World.Wanderer.Position);
        Assert.Equal(a.World.FindPlayer(One).Position, b.World.FindPlayer(One).Position);
        Assert.Equal(a.World.FindPlayer(Two).Position, b.World.FindPlayer(Two).Position);
        Assert.Equal(a.World.Random.State, b.World.Random.State);
    }

    [Fact]
    public void DifferentSeeds_PlayOutDifferently()
    {
        var (a, runnerA) = Start(seed: 1);
        var (b, runnerB) = Start(seed: 2);

        for (int i = 0; i < 60; i++)
        {
            runnerA.Step();
            runnerB.Step();
        }

        Assert.NotEqual(a.World.Wanderer.Position, b.World.Wanderer.Position);
    }

    [Fact]
    public void Player_MovesAtTheirSpeed()
    {
        var (session, runner) = Start(seed: 1);
        runner.AddPlayer(One, Constant(Vector2.UnitX));
        runner.Step();      // The player joins, and moves for one tick
        var start = session.World.FindPlayer(One).Position;

        for (int i = 0; i < runner.TicksPerSecond; i++)
            runner.Step();

        var moved = session.World.FindPlayer(One).Position - start;
        Assert.Equal(PlayerAvatar.Speed, moved.X, 0.5f);
        Assert.Equal(0f, moved.Y);
    }

    [Fact]
    public void OversizedMovement_IsLimitedToFullSpeed()
    {
        var (normal, normalRunner) = Start(seed: 1);
        var (cheater, cheaterRunner) = Start(seed: 1);
        normalRunner.AddPlayer(One, Constant(Vector2.UnitX));
        cheaterRunner.AddPlayer(One, Constant(new Vector2(100, 0)));

        for (int i = 0; i < 30; i++)
        {
            normalRunner.Step();
            cheaterRunner.Step();
        }

        Assert.Equal(normal.World.FindPlayer(One).Position, cheater.World.FindPlayer(One).Position);
    }

    [Fact]
    public void JoiningAndLeaving_RaiseEvents()
    {
        var (session, runner) = Start(seed: 1);
        runner.AddPlayer(One, Constant(Vector2.Zero));
        runner.Step();
        runner.RemovePlayer(One);
        runner.Step();

        Assert.Collection(session.Events.Pending,
            e => Assert.Equal(new PlayerJoined(0, One), e),
            e => Assert.Equal(new PlayerLeft(1, One), e));
        Assert.Null(session.World.FindPlayer(One));
    }

    [Fact]
    public void Players_SpawnApart()
    {
        var (session, runner) = Start(seed: 1);
        runner.AddPlayer(One, Constant(Vector2.Zero));
        runner.AddPlayer(Two, Constant(Vector2.Zero));
        runner.Step();

        Assert.NotEqual(session.World.FindPlayer(One).Position, session.World.FindPlayer(Two).Position);
    }
}
