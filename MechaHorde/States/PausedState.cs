using CIS580.Flow;
using CIS580.Input;
using MechaHorde.Audio;
using MechaHorde.UI;

namespace MechaHorde.States;

/// <summary>
/// The game is paused. This state is pushed on top of the in-game state, which stays underneath
/// (and, depending on the pause policy, may keep running).
/// </summary>
public sealed class PausedState : AppState
{
    private readonly InGameState _game;
    private readonly LocalPlayer _pausedBy;
    private GameAudio _audio;

    /// <param name="game">The game being paused</param>
    /// <param name="pausedBy">The player who paused</param>
    public PausedState(InGameState game, LocalPlayer pausedBy)
    {
        _game = game;
        _pausedBy = pausedBy;
    }

    protected override void Enter()
    {
        // The pause policy decides who can use the pause menu
        ListenTo(_game.PausePolicy.MenuSources(_pausedBy, _game.Players));

        Show(new PauseLayer(
            onResume: () => Flow.Pop(),
            onQuitToMenu: () => Flow.Change(new MainMenuState()),
            onExit: () => Game.Exit()));

        // Turn the music down while paused. If the game stops, its sound effects pause too;
        // menu sounds play on the separate UI bus, so they still work.
        _audio = Game.Services.GetService<GameAudio>();
        _audio.Audio.MusicPlayer.Duck();
        _audio.Audio.Effects.Paused = _game.PausePolicy.PausesSimulation;
    }

    protected override void Exit()
    {
        _audio.Audio.MusicPlayer.Unduck();
        _audio.Audio.Effects.Paused = false;
    }
}
