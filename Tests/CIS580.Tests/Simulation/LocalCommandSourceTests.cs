using System.Numerics;
using CIS580.Simulation;

namespace CIS580.Tests.Simulation;

public class LocalCommandSourceTests
{
    private enum TestAction { Fire }

    private static PlayerCommand Firing => new() { Actions = ActionSet.Empty.With(TestAction.Fire) };

    [Fact]
    public void GetCommand_ReturnsTheLatestMovement()
    {
        var source = new LocalCommandSource();
        source.Submit(new PlayerCommand { Move = Vector2.UnitX });
        source.Submit(new PlayerCommand { Move = Vector2.UnitY });

        Assert.Equal(Vector2.UnitY, source.GetCommand(0).Move);
    }

    [Fact]
    public void ATapBetweenTicks_IsNotMissed()
    {
        var source = new LocalCommandSource();
        source.Submit(Firing);                     // Pressed in one frame...
        source.Submit(PlayerCommand.Empty);        // ...and released in the next, before a tick ran

        Assert.True(source.GetCommand(0).Actions.Contains(TestAction.Fire));
    }

    [Fact]
    public void ATap_IsOnlyReportedForOneTick()
    {
        var source = new LocalCommandSource();
        source.Submit(Firing);
        source.Submit(PlayerCommand.Empty);
        source.GetCommand(0);

        Assert.False(source.GetCommand(1).Actions.Contains(TestAction.Fire));
    }

    [Fact]
    public void AHeldAction_IsReportedEveryTick()
    {
        var source = new LocalCommandSource();
        source.Submit(Firing);

        Assert.True(source.GetCommand(0).Actions.Contains(TestAction.Fire));
        Assert.True(source.GetCommand(1).Actions.Contains(TestAction.Fire));
    }
}
