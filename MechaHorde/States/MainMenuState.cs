using CIS580.Flow;
using CIS580.Input;
using MechaHorde.Audio;
using MechaHorde.Input;
using MechaHorde.UI;

namespace MechaHorde.States;

/// <summary>
/// The main menu, where the game starts. Any player, on any device, may use it.
/// </summary>
public sealed class MainMenuState : AppState
{
    protected override void Enter()
    {
        ListenTo(Controls.AllDevices());
        Game.Services.GetService<GameAudio>().PlayMenuMusic();

        Show(new BackgroundLayer());
        // For a single player game, pass null for onLocalMultiplayer to remove that choice
        Show(new MainMenuLayer(
            onPlay: StartGame,
            onLocalMultiplayer: _ => Flow.Change(new LobbyState()),
            onExit: () => Game.Exit()));
    }

    /// <summary>
    /// Starts a single player game for whoever chose "Play Game", using the device they chose it with
    /// </summary>
    private void StartGame(InputSource chosenWith)
    {
        var game = new InGameState(new[] { Controls.SinglePlayerFor(chosenWith) });
        Flow.Change(new LoadingState(game));
    }

    private GameControls Controls => Game.Services.GetService<GameControls>();
}
