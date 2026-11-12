using System;
using CIS580.Simulation;

namespace CIS580.Tests.Simulation;

public class ActionSetTests
{
    private enum TestAction { Jump = 0, Fire = 1, Last = 31 }

    private enum TooBig { OutOfRange = 32 }

    [Fact]
    public void Empty_ContainsNothing()
    {
        Assert.True(ActionSet.Empty.IsEmpty);
        Assert.False(ActionSet.Empty.Contains(TestAction.Jump));
    }

    [Fact]
    public void With_AddsOnlyThatAction()
    {
        var set = ActionSet.Empty.With(TestAction.Fire);

        Assert.True(set.Contains(TestAction.Fire));
        Assert.False(set.Contains(TestAction.Jump));
    }

    [Fact]
    public void Without_RemovesOnlyThatAction()
    {
        var set = ActionSet.Empty.With(TestAction.Fire).With(TestAction.Jump).Without(TestAction.Fire);

        Assert.False(set.Contains(TestAction.Fire));
        Assert.True(set.Contains(TestAction.Jump));
    }

    [Fact]
    public void Union_CombinesBothSets()
    {
        var set = ActionSet.Empty.With(TestAction.Fire).Union(ActionSet.Empty.With(TestAction.Last));

        Assert.True(set.Contains(TestAction.Fire));
        Assert.True(set.Contains(TestAction.Last));
    }

    [Fact]
    public void ActionsOutsideTheCapacity_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ActionSet.Empty.With(TooBig.OutOfRange));
    }
}
