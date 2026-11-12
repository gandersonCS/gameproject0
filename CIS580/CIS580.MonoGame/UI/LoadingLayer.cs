using System;
using Microsoft.Xna.Framework;

namespace CIS580.UI;

/// <summary>
/// A "Loading..." message with a progress bar, shown by a LoadingState
/// </summary>
public class LoadingLayer : Layer
{
    private readonly bool _showMessage;

    /// <summary>
    /// How far loading has got (0 to 1)
    /// </summary>
    public float Progress { get; set; }

    /// <param name="showMessage">If false, the layer draws nothing (for quick loads)</param>
    public LoadingLayer(bool showMessage)
    {
        _showMessage = showMessage;
        InputMode = InputMode.None;
        Transition.OnDuration = TimeSpan.FromSeconds(0.2);
        Transition.OffDuration = TimeSpan.FromSeconds(0.2);
    }

    public override void Draw(GameTime gameTime)
    {
        if (!_showMessage)
            return;

        const string message = "Loading...";
        var viewport = Game.GraphicsDevice.Viewport;
        var font = Theme.TextFont;
        var textSize = font.MeasureString(message);
        var textPosition = (new Vector2(viewport.Width, viewport.Height) - textSize) / 2;

        var bar = new Rectangle((int)textPosition.X, (int)(textPosition.Y + textSize.Y + 8), (int)textSize.X, 4);
        var filled = bar with { Width = (int)(bar.Width * Progress) };

        SpriteBatch.Begin();
        SpriteBatch.DrawString(font, message, textPosition, Theme.TextColor * Transition.Alpha);
        SpriteBatch.Draw(Stack.BlankTexture, bar, Theme.DisabledColor * (0.5f * Transition.Alpha));
        SpriteBatch.Draw(Stack.BlankTexture, filled, Theme.TextColor * Transition.Alpha);
        SpriteBatch.End();
    }
}
