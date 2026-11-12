using System;
using Microsoft.Xna.Framework;
using CIS580.Views;
using MechaHorde.Gameplay;

namespace MechaHorde.Views;

/// <summary>
/// Draws the level being played (the <see cref="Battlefield"/>): its parallax layers, the
/// player, their bullets, and the rain.
/// </summary>
/// <remarks>
/// It is a <see cref="WorldLayer"/>, so it fades in and out like the architecture's world view,
/// stays visible (dimmed) behind the pause and options menus, and can be shaken. The level
/// does its own camera and parallax math, so this draws it in screen space rather than through
/// the WorldLayer's cameras (which are only used here for their screen shake).
/// </remarks>
public class LevelView : WorldLayer
{
    private readonly Battlefield _battlefield;

    /// <param name="battlefield">The level to draw</param>
    public LevelView(Battlefield battlefield)
    {
        _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
        Transition.OnDuration = TimeSpan.FromSeconds(1.5);
        Transition.OffDuration = TimeSpan.FromSeconds(0.5);
    }

    public override void Draw(GameTime gameTime)
    {
        var graphics = Game.GraphicsDevice;
        graphics.Clear(Color.DarkRed);

        _battlefield.Draw(gameTime, SpriteBatch, graphics.Viewport, Cameras[0].Shake.Offset);

        // Fade in from black as the level comes on screen, and out to black as it leaves
        if (Transition.Alpha < 1f)
            Stack.FillScreen(Color.Black * (1f - Transition.Alpha));
    }

    // Not used: the level is drawn in screen space by Draw above
    protected override void DrawWorld(CIS580.Views.Camera2D camera, GameTime gameTime) { }
}
