using System.Linq;
using CIS580.Flow;
using CIS580.Input;
using CIS580.Simulation;

namespace CIS580.Tests.Flow;

public class PausePolicyTests
{
    private static LocalPlayer Player(int id, ControlScheme scheme) =>
        new(new PlayerId(id), new InputSource(InputDevice.KeyboardMouse, scheme), new InputProfile());

    private readonly LocalPlayer _first = Player(1, StandardSchemes.KeyboardLeft());
    private readonly LocalPlayer _second = Player(2, StandardSchemes.KeyboardRight());
    private LocalPlayer[] Players => new[] { _first, _second };

    [Fact]
    public void ByDefault_AnyoneCanPause_AndOnlyTheyControlTheMenu()
    {
        var policy = new PausePolicy();

        Assert.True(policy.CanPause(_second, Players));
        Assert.Equal(new[] { _second.Input }, policy.MenuSources(_second, Players));
    }

    [Fact]
    public void FirstPlayerOnly_StopsOthersPausing()
    {
        var policy = new PausePolicy { WhoCanPause = PauseRights.FirstPlayer };

        Assert.True(policy.CanPause(_first, Players));
        Assert.False(policy.CanPause(_second, Players));
    }

    [Fact]
    public void NoOne_CanPause()
    {
        var policy = new PausePolicy { WhoCanPause = PauseRights.NoOne };

        Assert.False(policy.CanPause(_first, Players));
    }

    [Fact]
    public void AnyPlayer_CanControlTheMenu()
    {
        var policy = new PausePolicy { MenuControl = PauseMenuControl.AnyPlayer };

        Assert.Equal(Players.Select(p => p.Input), policy.MenuSources(_first, Players));
    }
}
