namespace CIS580.Input;

/// <summary>
/// The names of the controls every game has. A game's own actions are added to control schemes
/// using the names of its action enum values (e.g. nameof(GameAction.Jump)).
/// </summary>
public static class StandardControls
{
    // Movement, read by the CommandBuilder into PlayerCommand.Move
    public const string MoveLeft = "MoveLeft";
    public const string MoveRight = "MoveRight";
    public const string MoveUp = "MoveUp";
    public const string MoveDown = "MoveDown";

    // Looking or aiming, read by the CommandBuilder into PlayerCommand.Look
    public const string LookLeft = "LookLeft";
    public const string LookRight = "LookRight";
    public const string LookUp = "LookUp";
    public const string LookDown = "LookDown";

    /// <summary>
    /// Opens the pause menu during play
    /// </summary>
    public const string Pause = "Pause";

    // Menu navigation, read by UiInput
    public const string UiUp = "UiUp";
    public const string UiDown = "UiDown";
    public const string UiLeft = "UiLeft";
    public const string UiRight = "UiRight";
    public const string UiConfirm = "UiConfirm";
    public const string UiCancel = "UiCancel";
}
