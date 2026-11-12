using System;
using System.Collections.Generic;
using CIS580.UI;

namespace CIS580.Tests.UI;

public class LayerStackTests
{
    private sealed class TestLayer : Layer
    {
        public TestLayer(string name, bool popup = false, bool backdrop = false, InputMode input = InputMode.Capture)
        {
            Name = name;
            IsPopup = popup;
            IsBackdrop = backdrop;
            InputMode = input;
            Transition.OnDuration = TimeSpan.FromSeconds(1);
            Transition.OffDuration = TimeSpan.FromSeconds(1);
        }

        public string Name { get; }
        public override string ToString() => Name;
    }

    private static readonly TimeSpan Long = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Runs the stack's coverage and focus pass, long enough for every transition to finish
    /// </summary>
    private static void Settle(List<Layer> layers, bool windowActive = true)
        => LayerStack.UpdateLayerStates(layers, Long, windowActive);

    [Fact]
    public void ALayer_CoversTheLayersBeneathIt()
    {
        var menu = new TestLayer("menu");
        var options = new TestLayer("options");
        var layers = new List<Layer> { menu, options };

        Settle(layers);

        Assert.True(menu.IsCovered);
        Assert.Equal(TransitionState.Hidden, menu.Transition.State);
        Assert.Equal(TransitionState.Visible, options.Transition.State);
    }

    [Fact]
    public void APopup_LeavesTheLayersBeneathVisible()
    {
        var game = new TestLayer("game");
        var pause = new TestLayer("pause", popup: true);
        var layers = new List<Layer> { game, pause };

        Settle(layers);

        Assert.False(game.IsCovered);
        Assert.Equal(TransitionState.Visible, game.Transition.State);
    }

    [Fact]
    public void ABackdrop_IsNeverCovered()
    {
        var background = new TestLayer("background", backdrop: true, input: InputMode.None);
        var menu = new TestLayer("menu");
        var layers = new List<Layer> { background, menu };

        Settle(layers);

        Assert.Equal(TransitionState.Visible, background.Transition.State);
    }

    [Fact]
    public void OnlyTheTopCapturingLayer_HasFocus()
    {
        var game = new TestLayer("game");
        var pause = new TestLayer("pause", popup: true);
        var layers = new List<Layer> { game, pause };

        Settle(layers);

        Assert.True(pause.HasFocus);
        Assert.False(game.HasFocus);
    }

    [Fact]
    public void PassThroughLayers_ShareFocusWithTheLayerBeneath()
    {
        var game = new TestLayer("game");
        var overlay = new TestLayer("overlay", popup: true, input: InputMode.PassThrough);
        var layers = new List<Layer> { game, overlay };

        Settle(layers);

        Assert.True(overlay.HasFocus);
        Assert.True(game.HasFocus);
    }

    [Fact]
    public void LayersWithoutInput_AreSkippedForFocus()
    {
        var game = new TestLayer("game");
        var hud = new TestLayer("hud", popup: true, input: InputMode.None);
        var layers = new List<Layer> { game, hud };

        Settle(layers);

        Assert.False(hud.HasFocus);
        Assert.True(game.HasFocus);
    }

    [Fact]
    public void ClosingLayers_LoseFocusAndStopCovering()
    {
        var menu = new TestLayer("menu");
        var options = new TestLayer("options");
        var layers = new List<Layer> { menu, options };
        Settle(layers);

        options.IsClosing = true;
        Settle(layers);

        Assert.False(options.HasFocus);
        Assert.Equal(TransitionState.Hidden, options.Transition.State);
        Assert.True(menu.HasFocus);
        Assert.Equal(TransitionState.Visible, menu.Transition.State);
    }

    [Fact]
    public void NoLayerHasFocus_WhenTheWindowIsInactive()
    {
        var menu = new TestLayer("menu");
        var layers = new List<Layer> { menu };

        Settle(layers, windowActive: false);

        Assert.False(menu.HasFocus);
    }

    [Fact]
    public void CoveredLayers_TransitionOffGradually()
    {
        var menu = new TestLayer("menu");
        var layers = new List<Layer> { menu };
        Settle(layers);

        layers.Add(new TestLayer("options"));
        LayerStack.UpdateLayerStates(layers, TimeSpan.FromSeconds(0.25), true);

        Assert.Equal(TransitionState.Exiting, menu.Transition.State);
        Assert.Equal(0.75f, menu.Transition.Position, 0.001f);
    }
}
