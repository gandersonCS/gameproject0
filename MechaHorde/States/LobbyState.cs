using System;
using System.Linq;
using Microsoft.Xna.Framework;
using CIS580.Flow;
using CIS580.Input;
using CIS580.UI;
using MechaHorde.Input;
using MechaHorde.UI;

namespace MechaHorde.States;

/// <summary>
/// Gathers players for a local multiplayer game. Each person joins on their own device (a
/// gamepad, or one half of a shared keyboard) and marks themselves ready; once everyone is
/// ready, a short countdown starts the game.
/// </summary>
public sealed class LobbyState : AppState
{
    /// <summary>
    /// How long the countdown lasts once everyone is ready (anyone can still back out)
    /// </summary>
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(3);

    private LocalLobby _lobby;
    private TimeSpan? _countdown;

    protected override void Enter()
    {
        // Menus listen to every device, so Esc (or a free gamepad's B) can go back
        var controls = Game.Services.GetService<GameControls>();
        ListenTo(controls.AllDevices());

        _lobby = new LocalLobby(controls.LobbyCandidates()) { MinPlayers = 2 };
        _lobby.Changed += OnLobbyChanged;

        Show(new BackgroundLayer());
        Show(new LobbyLayer(_lobby, () => _countdown, BackHint()));
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsTop)
            return;

        // Players joining, readying and leaving. A cancel that does none of those (e.g. Esc, or
        // B on a gamepad nobody joined with) goes back to the main menu.
        if (!_lobby.Update(Input) && Ui.WasPressed(UiControl.Cancel))
        {
            Layers.Theme.Sounds.Play(UiSound.Cancel);
            Flow.Change(new MainMenuState());
            return;
        }

        if (!_lobby.CanStart)
        {
            _countdown = null;
            return;
        }

        _countdown = (_countdown ?? StartDelay) - gameTime.ElapsedGameTime;
        if (_countdown <= TimeSpan.Zero)
            StartGame();
    }

    /// <summary>
    /// The main way to go back on each kind of device (e.g. "Esc / B"). Other cancel inputs
    /// such as Backspace also work, but on a shared keyboard they belong to a player.
    /// </summary>
    private string BackHint() => string.Join(" / ", Ui.Sources
        .Select(s => s.Assignments[0].Scheme.GetBindings(StandardControls.UiCancel).FirstOrDefault()?.DisplayName)
        .Where(name => name != null)
        .Distinct());

    private void OnLobbyChanged(LobbySeat seat, LobbyChange change)
    {
        var sound = change is LobbyChange.Joined or LobbyChange.Ready ? UiSound.Confirm : UiSound.Cancel;
        Layers.Theme.Sounds.Play(sound);
    }

    /// <summary>
    /// Starts the game, with one player for each seat, in the order they joined
    /// </summary>
    private void StartGame()
    {
        var sources = _lobby.Seats.Select(s => s.Input).ToArray();
        Flow.Change(new LoadingState(new InGameState(sources)));
    }
}
