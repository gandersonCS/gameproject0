using System.Collections.Generic;
using Microsoft.Xna.Framework;
using CIS580.Flow;

namespace CIS580.Tests.Flow;

public class AppFlowTests
{
    private readonly List<string> _log = new();

    /// <summary>
    /// A state that records what the flow does to it
    /// </summary>
    private sealed class TestState : AppState
    {
        private readonly string _name;
        private readonly List<string> _log;

        public System.Action OnEnter { get; init; }

        public TestState(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        protected internal override void Enter() { _log.Add($"enter {_name}"); OnEnter?.Invoke(); }
        protected internal override void Exit() => _log.Add($"exit {_name}");
        protected internal override void Covered() => _log.Add($"covered {_name}");
        protected internal override void Uncovered() => _log.Add($"uncovered {_name}");
        public override void Update(GameTime gameTime) => _log.Add($"update {_name}{(IsTop ? " (top)" : "")}");
    }

    private TestState State(string name) => new(name, _log);

    // States in these tests don't show layers, so the flow needs no game or layer stack
    private static AppFlow CreateFlow() => new(null, null, null);

    [Fact]
    public void Requests_AreAppliedAtTheNextUpdate()
    {
        var flow = CreateFlow();

        flow.Change(State("menu"));
        Assert.Empty(_log);
        Assert.Null(flow.Current);

        flow.Update(new GameTime());
        Assert.Equal(new[] { "enter menu", "update menu (top)" }, _log);
    }

    [Fact]
    public void Push_KeepsTheStateBeneath_AndUpdatesEveryState()
    {
        var flow = CreateFlow();
        flow.Change(State("game"));
        flow.Update(new GameTime());
        _log.Clear();

        flow.Push(State("paused"));
        flow.Update(new GameTime());

        Assert.Equal(new[] { "covered game", "enter paused", "update game", "update paused (top)" }, _log);
        Assert.Equal(2, flow.States.Count);
    }

    [Fact]
    public void Pop_ReturnsToTheStateBeneath()
    {
        var flow = CreateFlow();
        flow.Change(State("game"));
        flow.Push(State("paused"));
        flow.Update(new GameTime());
        _log.Clear();

        flow.Pop();
        flow.Update(new GameTime());

        Assert.Equal(new[] { "exit paused", "uncovered game", "update game (top)" }, _log);
    }

    [Fact]
    public void Change_ExitsEveryState_TopFirst()
    {
        var flow = CreateFlow();
        flow.Change(State("game"));
        flow.Push(State("paused"));
        flow.Update(new GameTime());
        _log.Clear();

        flow.Change(State("menu"));
        flow.Update(new GameTime());

        Assert.Equal(new[] { "exit paused", "exit game", "enter menu", "update menu (top)" }, _log);
        Assert.Single(flow.States);
    }

    [Fact]
    public void RequestsMadeWhileEntering_WaitForTheNextUpdate()
    {
        var flow = CreateFlow();
        AppFlow captured = flow;
        var splash = new TestState("splash", _log) { OnEnter = () => captured.Change(State("menu")) };

        flow.Change(splash);
        flow.Update(new GameTime());
        Assert.Equal(new[] { "enter splash", "update splash (top)" }, _log);

        flow.Update(new GameTime());
        Assert.Contains("enter menu", _log);
    }

    [Fact]
    public void StateChanged_DescribesEachChange()
    {
        var flow = CreateFlow();
        var changes = new List<string>();
        flow.StateChanged += changes.Add;

        flow.Change(State("a"));
        flow.Push(State("b"));
        flow.Pop();
        flow.Update(new GameTime());

        Assert.Equal(new[] { "Change to TestState", "Push TestState", "Pop TestState" }, changes);
    }
}
