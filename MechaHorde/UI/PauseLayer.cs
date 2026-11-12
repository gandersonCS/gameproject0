using System;
using CIS580.Input;
using CIS580.UI;
using CIS580.UI.Widgets;

namespace MechaHorde.UI;

/// <summary>
/// The pause menu. It is a popup, so the game stays visible (dimmed) beneath it. It only
/// presents choices: resuming, quitting, and exiting are up to the state that showed it.
/// </summary>
public class PauseLayer : MechaMenuLayer
{
    private readonly Action _onResume;

    /// <param name="onResume">Called when the player chooses to resume</param>
    /// <param name="onQuitToMenu">Called when the player confirms they want to return to the main menu</param>
    /// <param name="onExit">Called when the player confirms they want to exit the game entirely</param>
    public PauseLayer(Action onResume, Action onQuitToMenu, Action onExit) : base("Paused")
    {
        IsPopup = true;
        _onResume = onResume;

        Menu.Add(new ButtonItem("Resume Game", OnCancel))
            .Add(new ButtonItem("Options", _ => Open(new OptionsLayer(overGame: true))))
            .Add(new ButtonItem("Quit to Main Menu", _ => Confirm("Quit this game and return to the main menu?", onQuitToMenu)))
            .Add(new ButtonItem("Exit Game", _ => Confirm("Quit this game and exit?", onExit)));
    }

    /// <summary>
    /// Cancelling the pause menu resumes the game
    /// </summary>
    protected override void OnCancel(InputSource source) => _onResume();

    private void Confirm(string message, Action onAccepted)
    {
        var prompt = new PromptLayer(message);
        prompt.Accepted += _ => onAccepted();
        Open(prompt);
    }
}
