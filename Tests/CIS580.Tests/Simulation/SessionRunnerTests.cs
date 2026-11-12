using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CIS580.Simulation;

namespace CIS580.Tests.Simulation;

public class SessionRunnerTests
{
    private enum TestAction { Fire }

    /// <summary>
    /// A simulation that records everything the runner does to it
    /// </summary>
    private sealed class RecordingSimulation : ISimulation
    {
        public readonly List<string> Calls = new();
        public readonly List<PlayerInput[]> Inputs = new();
        public readonly List<float> DeltaSeconds = new();

        public void AddPlayer(PlayerId player) => Calls.Add($"add {player.Value}");
        public void RemovePlayer(PlayerId player) => Calls.Add($"remove {player.Value}");

        public void Tick(in TickContext context)
        {
            Calls.Add($"tick {context.Tick}");
            Inputs.Add(context.Inputs.ToArray());    // The runner reuses its list, so copy it
            DeltaSeconds.Add(context.DeltaSeconds);
        }
    }

    private static readonly ICommandSource Idle = new DelegateCommandSource(_ => PlayerCommand.Empty);

    [Fact]
    public void Update_RunsOneTickPerTickDuration()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim, ticksPerSecond: 60);

        int ticks = runner.Update(runner.TickDuration * 3);

        Assert.Equal(3, ticks);
        Assert.Equal(3, runner.Tick);
        Assert.Equal(new[] { "tick 0", "tick 1", "tick 2" }, sim.Calls);
    }

    [Fact]
    public void Update_GivesTheSimulationAFixedDeltaTime()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim, ticksPerSecond: 50);

        runner.Update(TimeSpan.FromSeconds(0.1));

        Assert.All(sim.DeltaSeconds, dt => Assert.Equal(0.02f, dt, 0.00001f));
    }

    [Fact]
    public void Update_CarriesLeftoverTimeToTheNextUpdate()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim);
        var half = runner.TickDuration / 2;

        Assert.Equal(0, runner.Update(half));
        Assert.Equal(0.5f, runner.Alpha, 0.001f);
        Assert.Equal(1, runner.Update(half));
    }

    [Fact]
    public void Update_DiscardsTimeBeyondMaxTicksPerUpdate()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim) { MaxTicksPerUpdate = 5 };

        Assert.Equal(5, runner.Update(TimeSpan.FromSeconds(1)));
        Assert.Equal(0, runner.Update(TimeSpan.Zero));     // The backlog was dropped, not saved for later
    }

    [Fact]
    public void Update_RejectsNegativeTime()
    {
        var runner = new SessionRunner(new RecordingSimulation());
        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Update(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void AddPlayer_TakesEffectJustBeforeTheNextTick()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim);

        runner.Step();
        runner.AddPlayer(new PlayerId(7), Idle);
        Assert.True(runner.HasPlayer(new PlayerId(7)));
        Assert.Equal(new[] { "tick 0" }, sim.Calls);        // Not added to the simulation yet

        runner.Step();

        Assert.Equal(new[] { "tick 0", "add 7", "tick 1" }, sim.Calls);
        Assert.Equal(new PlayerId(7), Assert.Single(sim.Inputs[1]).Player);
    }

    [Fact]
    public void AddPlayer_RejectsDuplicateIds()
    {
        var runner = new SessionRunner(new RecordingSimulation());
        runner.AddPlayer(new PlayerId(1), Idle);

        Assert.Throws<InvalidOperationException>(() => runner.AddPlayer(new PlayerId(1), Idle));
    }

    [Fact]
    public void Inputs_AreInJoinOrder()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim);
        runner.AddPlayer(new PlayerId(3), Idle);
        runner.AddPlayer(new PlayerId(1), Idle);
        runner.AddPlayer(new PlayerId(2), Idle);

        runner.Step();

        Assert.Equal(new[] { 3, 1, 2 }, sim.Inputs[0].Select(i => i.Player.Value));
    }

    [Fact]
    public void RemovePlayer_TakesEffectJustBeforeTheNextTick()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim);
        runner.AddPlayer(new PlayerId(1), Idle);
        runner.Step();

        runner.RemovePlayer(new PlayerId(1));
        Assert.False(runner.HasPlayer(new PlayerId(1)));
        runner.Step();

        Assert.Equal(new[] { "add 1", "tick 0", "remove 1", "tick 1" }, sim.Calls);
        Assert.Empty(sim.Inputs[1]);
    }

    [Fact]
    public void RemovePlayer_BeforeTheyJoin_MeansTheyNeverJoin()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim);
        runner.AddPlayer(new PlayerId(1), Idle);
        runner.RemovePlayer(new PlayerId(1));

        runner.Step();

        Assert.Equal(new[] { "tick 0" }, sim.Calls);
    }

    [Fact]
    public void RemoveThenAddSamePlayer_RejoinsWithNewSource()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim);
        runner.AddPlayer(new PlayerId(1), Idle);
        runner.Step();

        var moving = new DelegateCommandSource(_ => new PlayerCommand { Move = Vector2.UnitX });
        runner.RemovePlayer(new PlayerId(1));
        runner.AddPlayer(new PlayerId(1), moving);
        runner.Step();

        Assert.Equal(new[] { "add 1", "tick 0", "remove 1", "add 1", "tick 1" }, sim.Calls);
        Assert.Equal(Vector2.UnitX, Assert.Single(sim.Inputs[1]).Move);
    }

    [Fact]
    public void Inputs_IncludeThePreviousTicksCommand()
    {
        var sim = new RecordingSimulation();
        var runner = new SessionRunner(sim);
        // Fire is held down on tick 1 only
        var source = new DelegateCommandSource(tick => tick == 1
            ? new PlayerCommand { Actions = ActionSet.Empty.With(TestAction.Fire) }
            : PlayerCommand.Empty);
        runner.AddPlayer(new PlayerId(1), source);

        runner.Step();
        runner.Step();
        runner.Step();

        var tick0 = sim.Inputs[0][0];
        var tick1 = sim.Inputs[1][0];
        var tick2 = sim.Inputs[2][0];
        Assert.False(tick0.IsDown(TestAction.Fire));
        Assert.True(tick1.WasPressed(TestAction.Fire));
        Assert.True(tick1.IsDown(TestAction.Fire));
        Assert.False(tick2.IsDown(TestAction.Fire));
        Assert.True(tick2.WasReleased(TestAction.Fire));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveTickRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SessionRunner(new RecordingSimulation(), 0));
    }
}
