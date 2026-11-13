using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using CIS580.Input;
using CIS580.UI;
using Microsoft.Xna.Framework.Graphics;
using MechaHorde.Gameplay;
using MechaHorde.Simulation;

namespace MechaHorde.Views;

/// <summary>
/// The heads-up display drawn over the world during play.
/// </summary>
/// <remarks>
/// The HUD is a popup (the world stays visible beneath it) that never takes input. It shows
/// MechaHorde's timer and player position in the top left, how to pause (using the player's
/// current bindings), and, only when several people are playing, who is who and when players
/// join or leave. (Single player games shouldn't show multiplayer details.)
/// </remarks>
public class HudLayer : Layer
{
    private static readonly TimeSpan MessageDuration = TimeSpan.FromSeconds(2.5);

    private readonly IReadOnlyList<LocalPlayer> _players;
    private readonly Battlefield _battlefield;
    private readonly List<(string Text, TimeSpan Shown)> _messages = new();
    private TimeSpan _now;

    /// <param name="players">The players on this machine</param>
    /// <param name="battlefield">The level being played</param>
    public HudLayer(IReadOnlyList<LocalPlayer> players, Battlefield battlefield)
    {
        _players = players ?? throw new ArgumentNullException(nameof(players));
        _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
        IsPopup = true;
        InputMode = InputMode.None;
    }

    private bool IsMultiplayer => _players.Count > 1;

    /// <summary>
    /// Reacts to something that happened in the simulation
    /// </summary>
    public void OnGameEvent(GameEvent gameEvent)
    {
        if (!IsMultiplayer)
            return;

        switch (gameEvent)
        {
            case PlayerJoined joined:
                _messages.Add(($"Player {joined.Player.Value} joined", _now));
                break;
            case PlayerLeft left:
                _messages.Add(($"Player {left.Player.Value} left", _now));
                break;
        }
    }

    public override void Update(GameTime gameTime)
    {
        _now = gameTime.TotalGameTime;
        _messages.RemoveAll(m => _now - m.Shown > MessageDuration);
    }

    public override void Draw(GameTime gameTime)
    {
        var font = Theme.TextFont;
        var viewport = Game.GraphicsDevice.Viewport;
        float alpha = Transition.Alpha;
        const float margin = 16;

        SpriteBatch.Begin();

        // The timer and the player's position, in the top left (the original MechaHorde HUD)
        string timerText = $"Time: {_battlefield.GameTimer:F2}";
        string playerPosition = $"\nPlayer X: {_battlefield.Player.position.X}\nPlayer Y: {_battlefield.Player.position.Y}";
        var spawner = _battlefield.Spawner;
        string waveText = spawner.Wave == 0
            ? $"\nFirst wave in {spawner.TimeUntilNextWave:F0}"
            : spawner.IsBetweenWaves
                ? $"\nWave {spawner.Wave} cleared! Next wave in {spawner.TimeUntilNextWave:F0}"
                : $"\nWave {spawner.Wave} - Enemies left: {spawner.EnemiesRemaining}";
        SpriteBatch.DrawString(Theme.TitleFont, timerText + playerPosition + waveText, new Vector2(10, 10),
            Color.Red * alpha, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

        // How to pause, in the top right, built from each player's current bindings
        var position = new Vector2(viewport.Width - margin, margin);
        foreach (var player in _players)
        {
            string prefix = IsMultiplayer ? $"Player {player.Id.Value}: " : "";
            string text = $"{prefix}{player.Input.DescribeBindings(StandardControls.Pause)} to pause";
            var size = font.MeasureString(text);
            var color = IsMultiplayer ? PlayerColors.For(player.Id) : Theme.TextColor;
            SpriteBatch.DrawString(font, text, position - new Vector2(size.X, 0), color * (0.6f * alpha));
            position.Y += font.LineSpacing;
        }

        // Join and leave messages, fading out, in the bottom left
        position = new Vector2(margin, viewport.Height - margin - font.LineSpacing);
        foreach (var (text, shown) in Enumerable.Reverse(_messages))
        {
            float fade = 1f - (float)((_now - shown) / MessageDuration);
            SpriteBatch.DrawString(font, text, position, Theme.TextColor * (fade * alpha));
            position.Y -= font.LineSpacing;
        }

        SpriteBatch.End();
    }
}
