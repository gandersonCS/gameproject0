using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CIS580.Input;
using CIS580.UI;
using MechaHorde.Views;

namespace MechaHorde.UI;

/// <summary>
/// Shows the local multiplayer lobby: a panel for each seat showing who has joined and who is
/// ready, and how to join. It only draws the lobby; the LobbyState handles the input.
/// </summary>
public class LobbyLayer : Layer
{
    private const string Title = "Local Multiplayer";
    private const float PanelHeight = 150;
    private const float PanelGap = 12;
    private const float SmallText = 0.75f;

    private readonly LocalLobby _lobby;
    private readonly Func<TimeSpan?> _countdown;
    private readonly string _backHint;

    /// <param name="lobby">The lobby to show</param>
    /// <param name="countdown">The time until the game starts, or null if it isn't starting</param>
    /// <param name="backHint">How to leave the lobby (e.g. "Esc / B")</param>
    public LobbyLayer(LocalLobby lobby, Func<TimeSpan?> countdown, string backHint)
    {
        _lobby = lobby ?? throw new ArgumentNullException(nameof(lobby));
        _countdown = countdown ?? (() => null);
        _backHint = backHint;
        InputMode = InputMode.None;
    }

    protected override void Load()
    {
        Transition.OnDuration = Theme.MenuTransition;
        Transition.OffDuration = Theme.MenuTransition;
    }

    public override void Draw(GameTime gameTime)
    {
        var viewport = Game.GraphicsDevice.Viewport;
        float alpha = Transition.Alpha;
        float centerX = viewport.Width / 2f;

        SpriteBatch.Begin();

        // The title slides down into place, as in the menus
        var titleFont = Theme.TitleFont;
        SpriteBatch.DrawString(titleFont, Title, new Vector2(centerX, Theme.TitleTop - Transition.Offset * 100),
            Theme.TitleColor * alpha, 0, titleFont.MeasureString(Title) / 2, Theme.TitleScale, SpriteEffects.None, 0);

        // A panel for each seat, in a row
        int count = _lobby.MaxPlayers;
        float panelWidth = Math.Min(200, (viewport.Width - 32 - PanelGap * (count - 1)) / count);
        float left = centerX - (panelWidth * count + PanelGap * (count - 1)) / 2;
        float top = Theme.MenuTop - 20;
        for (int i = 0; i < count; i++)
        {
            var panel = new Rectangle((int)(left + i * (panelWidth + PanelGap)), (int)top, (int)panelWidth, (int)PanelHeight);
            DrawSeat(i, panel, alpha);
        }

        // How to join, and whether the game is about to start
        float y = top + PanelHeight + 24;
        if (!_lobby.IsFull)
            y = DrawCentered(JoinHint(), centerX, y, Theme.TextColor * alpha, 1f);
        DrawCentered(Status(), centerX, y + 8, Theme.SelectedColor * alpha, 1f);
        DrawCentered($"{_backHint} to go back", centerX, viewport.Height - 40, Theme.DisabledColor * alpha, SmallText);

        SpriteBatch.End();
    }

    private void DrawSeat(int index, Rectangle panel, float alpha)
    {
        if (Theme.PanelTexture != null)
            SpriteBatch.Draw(Theme.PanelTexture, panel, Color.White * (alpha * 0.8f));
        else
            SpriteBatch.Draw(Stack.BlankTexture, panel, Theme.PanelColor * (alpha * 0.8f));

        float x = panel.Center.X;
        float y = panel.Top + 12;
        var color = PlayerColors.ForSeat(index);

        if (index >= _lobby.Seats.Count)
        {
            DrawCentered("Open", x, panel.Center.Y - Theme.TextFont.LineSpacing / 2f, Theme.DisabledColor * alpha, 1f);
            return;
        }

        float maxWidth = panel.Width - 12;
        var seat = _lobby.Seats[index];
        y = DrawCentered($"Player {index + 1}", x, y, color * alpha, 1f, maxWidth);
        y = DrawCentered(seat.Input.Name, x, y, Theme.TextColor * alpha, SmallText, maxWidth);
        y += 12;
        if (seat.IsReady)
        {
            y = DrawCentered("Ready!", x, y, color * alpha, 1f, maxWidth);
            DrawCentered($"{seat.Input.DescribeBindings(StandardControls.UiCancel)}: not ready", x, y, Theme.DisabledColor * alpha, SmallText, maxWidth);
        }
        else
        {
            y = DrawCentered($"{seat.Input.DescribeBindings(StandardControls.UiConfirm)}: ready", x, y, Theme.TextColor * alpha, SmallText, maxWidth);
            DrawCentered($"{seat.Input.DescribeBindings(StandardControls.UiCancel)}: leave", x, y, Theme.DisabledColor * alpha, SmallText, maxWidth);
        }
    }

    /// <summary>
    /// "Press Space, Enter or A to join", from the confirm bindings of the devices still free
    /// </summary>
    private string JoinHint()
    {
        var keys = _lobby.Available
            .Select(c => c.DescribeBindings(StandardControls.UiConfirm))
            .Distinct()
            .ToList();
        if (keys.Count == 0)
            return "";
        string list = keys.Count == 1 ? keys[0] : string.Join(", ", keys.Take(keys.Count - 1)) + " or " + keys[^1];
        return $"Press {list} to join";
    }

    private string Status()
    {
        if (_countdown() is { } remaining)
            return $"Starting in {Math.Ceiling(remaining.TotalSeconds)}...";
        if (_lobby.Seats.Count < _lobby.MinPlayers)
            return $"Waiting for players ({_lobby.MinPlayers} needed)";
        return "Waiting for everyone to be ready";
    }

    /// <summary>
    /// Draws a line of text centered on x, returning the y of the next line. Text wider than
    /// maxWidth is shrunk to fit.
    /// </summary>
    private float DrawCentered(string text, float x, float y, Color color, float scale, float maxWidth = float.MaxValue)
    {
        var font = Theme.TextFont;
        float width = font.MeasureString(text).X * scale;
        if (width > maxWidth)
        {
            scale *= maxWidth / width;
            width = maxWidth;
        }
        SpriteBatch.DrawString(font, text, new Vector2(x - width / 2, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        return y + font.LineSpacing * scale;
    }
}
