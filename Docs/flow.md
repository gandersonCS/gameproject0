# App Flow

[← Overview](README.md)

The app flow answers "where is the player in the app?": in the main menu, in the lobby, loading,
playing, or paused. Each answer is an **app state**. States are the app's *controllers*: they
decide what is on screen, who the menus listen to, and what happens when the player chooses
something.

The code is in `CIS580.MonoGame/Flow/`, and the game's states are in `MechaHorde/States/`.

## `AppFlow`: a stack of states

[`AppFlow`](../CIS580/CIS580.MonoGame/Flow/AppFlow.cs) holds a **stack** of
[`AppState`](../CIS580/CIS580.MonoGame/Flow/AppState.cs)s:

| Request | Effect | Example |
|---|---|---|
| `Change(state)` | Removes every state, then adds this one | main menu → game |
| `Push(state)` | Puts a state on top; the one beneath stays | game → paused (the game is still there) |
| `Pop()` | Removes the top state, returning to the one beneath | paused → game |

**Requests are deferred.** They are queued and applied at the start of the next
`AppFlow.Update`, never partway through a frame. So a state, a layer callback, or an event
handler can call `Flow.Change(...)` at any point without breaking whatever is currently running.

`StateChanged` is raised after each change. Debug builds log it to the console
(`[AppFlow] Push PausedState`).

## `AppState`: lifecycle

```csharp
public abstract class AppState
{
    protected internal virtual void Enter();      // added to the flow: show layers, pick who menus listen to
    protected internal virtual void Exit();       // removed from the flow: its layers close automatically afterward
    protected internal virtual void Covered();    // another state was pushed on top
    protected internal virtual void Uncovered();  // the state above was popped
    public virtual void Update(GameTime gameTime);// every frame, for EVERY state, bottom to top

    public bool IsTop { get; }                    // is this the topmost state?
    protected T Show<T>(T layer);                 // push a layer that closes when this state exits
    protected void ListenTo(IEnumerable<InputSource> sources);  // who the menus listen to
    protected Game Game; protected LayerStack Layers; protected UiInput Ui; protected InputState Input;
}
```

Things to know:

- **`Update` runs for every state in the stack**, not just the top one. That lets the game keep
  running under a pause menu when the pause policy allows it. Check `IsTop` for anything that
  should only happen when the state is on top.
- **Use `Show(layer)` for a state's layers**, so they close automatically when the state exits.
  Layers that *a layer* opens (submenus, prompts) belong to that layer instead (see
  [UI](ui.md#who-owns-a-layer)).
- **Call `ListenTo` in `Enter`** (and in `Uncovered`, if the state beneath changed it). If you
  forget, the menus keep listening to whatever the previous state chose. After `InGameState`
  that is *nobody*, and your menu will appear frozen.

## States and layers work together

States don't draw, and layers don't make decisions. A state shows layers and gives them
**callbacks**; the layer calls back when the player chooses something; the state decides what that
means:

```csharp
// MainMenuState.Enter
Show(new BackgroundLayer());
Show(new MainMenuLayer(
    onPlay: StartGame,                                         // Action<InputSource>
    onLocalMultiplayer: _ => Flow.Change(new LobbyState()),
    onExit: () => Game.Exit()));
```

`MainMenuLayer` doesn't know what "Play" does. That keeps menus reusable (the same `PauseLayer`
could serve a different game) and puts all the app's navigation logic in the states, where it's
easy to follow.

## Loading: `LoadingState` and `ILoadable`

A state that needs to load first implements [`ILoadable`](../CIS580/CIS580.MonoGame/Flow/ILoadable.cs):

```csharp
public IEnumerable<float> Load()
{
    _content = new ContentManager(Game.Services, "Content");
    _gameFont = _content.Load<SpriteFont>("Fonts/gamefont");
    yield return 0.5f;                       // progress so far, 0..1

    Session = new GameSession(seed);
    yield return 1f;
}
```

and is started through a [`LoadingState`](../CIS580/CIS580.MonoGame/Flow/LoadingState.cs):

```csharp
Flow.Change(new LoadingState(new InGameState(players)));
```

The loading state:

1. closes **every** layer on screen (`LayerStack.ReplaceAll`) and brings on a `LoadingLayer`
2. once the old layers have transitioned off, runs the target's `Load()` steps on the main
   thread, as many per frame as fit in `FrameBudget` (10 ms), so the loading bar keeps animating
3. calls `Flow.Change(target)` when the steps run out

Keep each step short: load one asset, or build one part of the level. The target can already use
`Game`, its services, and content during `Load()`, but its `Enter()` hasn't run yet.
`new LoadingState(target, showMessage: false)` hides the "Loading..." text, which looks better
for quick loads.

## Pausing: `PausePolicy`

Every game pauses differently, so the rules are a [`PausePolicy`](../CIS580/CIS580.MonoGame/Flow/PausePolicy.cs)
registered as a service in the game's constructor:

| Property | Default | Options |
|---|---|---|
| `WhoCanPause` | `AnyPlayer` | `AnyPlayer`, `FirstPlayer` (e.g. the host), `NoOne` |
| `MenuControl` | `PausingPlayer` | `PausingPlayer` (only they can use the pause menu), `AnyPlayer` |
| `PausesSimulation` | `true` | `false` for networked games, where one player can't stop the shared game |
| `PauseWhenDeviceLost` | `true` | Pause when a player's gamepad is unplugged |

To change them, edit the line in `MechaHordeGame`:

```csharp
Services.AddService(new PausePolicy { WhoCanPause = PauseRights.FirstPlayer });
```

For rules these settings can't express, derive from `PausePolicy` and override `CanPause` or
`MenuSources`.

## This game's states

| State | Shows | Listens to | Goes to |
|---|---|---|---|
| [`MainMenuState`](../MechaHorde/States/MainMenuState.cs) | `BackgroundLayer`, `MainMenuLayer` | every device | `LoadingState(InGameState)` (Play Game), `LobbyState`, or exits |
| [`LobbyState`](../MechaHorde/States/LobbyState.cs) | `BackgroundLayer`, `LobbyLayer` | every device | `LoadingState(InGameState)` after the countdown, or `MainMenuState` on cancel |
| `LoadingState` | `LoadingLayer` | nobody | the target state |
| [`InGameState`](../MechaHorde/States/InGameState.cs) | `WorldView`, `HudLayer` | nobody | pushes `PausedState` |
| [`PausedState`](../MechaHorde/States/PausedState.cs) | `PauseLayer` | per `PausePolicy` | pops (resume), `MainMenuState`, or exits |

```
 MainMenuState --Play--------------------> LoadingState --> InGameState <--Pop/Resume--.
      |   ^                                    ^               |                       |
      |   |                                    |               '--Push (Pause)--> PausedState
      |   '--Back--- LobbyState --all ready----'                                       |
      '--Local Multiplayer--^                                                          |
      ^                                                                                |
      '-------------------------------Quit to Main Menu (Change)-----------------------'
```

### Inside `InGameState`

[`InGameState`](../MechaHorde/States/InGameState.cs) owns a game in progress, so it is
worth reading in full. In order:

- **`Load()`** creates its own `ContentManager` (so all gameplay content can be unloaded when the
  game ends), the `GameSession`, the `SessionRunner`, and a `LocalPlayer` per input source,
  numbered from 1.
- **`Enter()`** shows `WorldView` and `HudLayer`, subscribes them, `GameAudio`, and
  `GameFeedback` to `GameEventRaised`, and starts the game music.
- **`Update()`** each frame:
  1. returns early if covered and the policy pauses the simulation
  2. submits each player's command, or an idle one for players currently using a menu
  3. runs the simulation: `Runner.Update(_hitStop.Apply(elapsed))`
  4. raises `GameEventRaised` for each pending simulation event, then clears them
  5. updates rumble
  6. if on top, checks for pause presses and unplugged gamepads, pushing `PausedState`
- **`Covered()`** stops rumble. **`Uncovered()`** stops menus from listening again.
- **`Exit()`** stops effects and rumble, and unloads the game's content only once the
  `WorldView` has finished fading out (`Layer.Removed`). Unloading while a layer is still
  drawing with that content causes graphics errors.

### Inside `PausedState`

`PausedState` is *pushed* over `InGameState`, so the game is still there underneath. It asks the
pause policy who controls the menu, ducks the music, and pauses the Effects audio bus if the
simulation is paused. `Exit()` undoes both.

## Extending the flow

### Adding a state

Here is a game-over screen reached when the simulation raises a (hypothetical) `GameOver` event:

```csharp
using CIS580.Flow;
using MechaHorde.Input;
using MechaHorde.UI;

namespace MechaHorde.States;

public sealed class GameOverState : AppState
{
    private readonly int _score;

    public GameOverState(int score) => _score = score;

    protected override void Enter()
    {
        ListenTo(Game.Services.GetService<GameControls>().AllDevices());
        Show(new BackgroundLayer());
        Show(new GameOverLayer(_score,
            onPlayAgain: source => Flow.Change(new LoadingState(
                new InGameState(new[] { Game.Services.GetService<GameControls>().SinglePlayerFor(source) }))),
            onMainMenu: () => Flow.Change(new MainMenuState())));
    }
}
```

([`GameOverLayer`](ui.md#adding-a-menu) is built on the UI page.) Trigger it from `InGameState`,
for example in an event handler subscribed in `Enter()`:

```csharp
GameEventRaised += gameEvent =>
{
    if (gameEvent is GameOver over)
        Flow.Change(new GameOverState(over.Score));
};
```

Because flow requests are deferred, it's safe to call `Flow.Change` from inside the event loop.

### Adding a state that needs loading

Implement `ILoadable` and wrap the state in a `LoadingState`, as `InGameState` does. For content
that should be freed when the state ends, use a `ContentManager` of its own and unload it in
`Exit()` (after its layers are removed, as `InGameState` does).

### Overlay states

Use `Push` for anything that should return to exactly where the player was: pause, an inventory
screen, a shop, a dialogue. The covered state keeps all its data. Use `Change` for moves between
major parts of the app.

### Skipping straight to your state while developing

The game's constructor handles `--play`. Add a similar argument for whatever you're working on:

```csharp
if (args.Contains("--gameover"))
    _flow.Change(new GameOverState(score: 42));
```
