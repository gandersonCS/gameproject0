# User Interface

[← Overview](README.md)

Everything on screen, including the game world, is a **layer** in a **layer stack**. Menus,
prompts, the HUD, the background, the loading screen, and the world view are all layers. The
stack draws them bottom to top, sends input to the top, and animates them on and off screen.

This design comes from the XNA *GameStateManagement* sample, with the screen management made
simpler. In particular, deciding *what* to show is left to the [app states](flow.md); layers only
present things.

The library code is in `CIS580.MonoGame/UI/`, and the game's layers are in `MechaHorde/UI/`
and `MechaHorde/Views/`.

## `Layer`

[`Layer`](../CIS580/CIS580.MonoGame/UI/Layer.cs) is the base class. Override what you need:

| Member | When it's called / what it's for |
|---|---|
| `Load()` | When the layer is added to the stack (graphics are available). Load content and build menus here. |
| `Unload()` | When the layer is removed from the stack |
| `Update(GameTime)` | Every frame, whether or not the layer has focus or is visible |
| `HandleInput(GameTime)` | Only on frames when the layer **has focus** |
| `Draw(GameTime)` | Only when the layer is at least partly on screen |
| `Removed` (event) | After the layer has finished closing and left the stack |

And use what it provides:

| Member | Purpose |
|---|---|
| `Close()` | Transition off, then leave the stack, along with any layers this one `Open`ed |
| `Open(child)` | Push a layer that **belongs to this one** (a submenu or prompt) and closes with it |
| `Transition` | The on/off animation (see below) |
| `Game`, `Theme`, `Ui`, `Input`, `SpriteBatch` | Shortcuts to the game, the UI theme, menu input, device input, and a shared `SpriteBatch` |
| `Stack.BlankTexture` | A 1×1 white texture for drawing rectangles |
| `Stack.FillScreen(color)` | Fills the screen, e.g. to dim the layers beneath |
| `DrawMessagePanel(text)` | Draws a centered panel with text, dimming what's beneath (as prompts do) |

`Draw` must call `SpriteBatch.Begin()` and `End()` itself, so each layer can use whatever
`SpriteBatch` settings it needs.

### How a layer behaves in the stack

Three settings, made in the constructor, decide how a layer interacts with the ones around it:

| Setting | Effect | Examples |
|---|---|---|
| *(default)* | **Covers** the layers beneath: they transition off while it's up, and back on when it closes | `MainMenuLayer`, `OptionsLayer`, `ControlsLayer` |
| `IsPopup = true` | Appears **over** the layers beneath, leaving them visible | `PauseLayer`, `PromptLayer`, `HudLayer` |
| `IsBackdrop = true` | Is **never covered**; stays visible behind everything above it | `BackgroundLayer`, `WorldView` |

| `InputMode` | Effect | Examples |
|---|---|---|
| `Capture` *(default)* | Receives input; layers beneath don't | menus, prompts |
| `PassThrough` | Receives input, and so does the next layer beneath | a tooltip over a menu |
| `None` | Never receives input | HUD, background, world view |

The world view is `InputMode.None` because gameplay input isn't read by layers. `InGameState`
reads it from the players' input sources.

### What the stack does each frame

[`LayerStack`](../CIS580/CIS580.MonoGame/UI/LayerStack.cs) is a `DrawableGameComponent`. In
`Update` it:

1. works **down from the top**, marking layers covered (everything beneath a non-popup, except
   backdrops) and giving focus to the topmost input-taking layer (and further down, through any
   `PassThrough` layers). When the game window isn't active, no layer gets focus.
2. advances each layer's transition toward visible (if not covered or closing) or hidden
3. removes layers that are closing and fully hidden, calling `Unload` and raising `Removed`
4. if a `ReplaceAll` is pending and the stack is now empty, pushes the replacement layers
5. calls `Update` on every layer, and `HandleInput` on focused ones, from the top down

In `Draw`, it draws every layer whose transition is above zero, from the bottom up.

Its public methods are `Push(layer)`, `Close(layer)`, and `ReplaceAll(layers...)` (close
everything, then show these once the old layers are gone, as `LoadingState` does). Most code
doesn't call these directly; see [who owns a layer](#who-owns-a-layer).

### An example stack

Paused, with the options menu open from the pause menu:

```
  top    OptionsLayer   (DimBackground)   covers the pause menu and HUD; has focus
         PauseLayer     (popup)           covered: transitioned off
         HudLayer       (popup, no input) covered: transitioned off
 bottom  WorldView      (backdrop)        still drawn, dimmed by the options menu
```

## Who owns a layer

There are three ways to put a layer on screen. Use the one that matches who should close it:

| Call | Owner | Closed when | Use for |
|---|---|---|---|
| `Show(layer)` in an `AppState` | The state | The state exits | A state's main layers |
| `Open(layer)` in a `Layer` | The layer that opened it | That layer closes | Submenus and prompts |
| `Stack.Push(layer)` | Nobody | You close it yourself | Rarely needed |

Ownership matters. If the pause menu pushed its "Are you sure?" prompt with `Stack.Push`, the
prompt would be left behind on screen after "Quit to Main Menu" changed the state.

## `Transition`

Each layer has a [`Transition`](../CIS580/CIS580.MonoGame/UI/Transition.cs): a single number,
`Position`, that moves from 0 (hidden) to 1 (visible) over `OnDuration`, and back over
`OffDuration`. Layers use it however they like when drawing:

| Property | Use |
|---|---|
| `Alpha` | Fading: multiply colors by it (`Color.White * Transition.Alpha`) |
| `Offset` | Sliding: 1 when hidden, easing to 0 when visible. Multiply by a distance. |
| `State` | `Hidden`, `Entering`, `Visible`, or `Exiting`, e.g. to slide out a different way than in |

```csharp
var position = new Vector2(centerX, 80 - Transition.Offset * 100);   // slides down into place
SpriteBatch.DrawString(font, "Hello", position, Color.White * Transition.Alpha);
```

Menus take their durations from the theme (`MenuTransition`, `PopupTransition`). Set your own
in the constructor or `Load()`.

## `UiTheme`

[`UiTheme`](../CIS580/CIS580.MonoGame/UI/UiTheme.cs) holds the interface's look and sound:
`TitleFont`, `TextFont`, `PanelTexture`, colors (`TitleColor`, `TextColor`, `SelectedColor`,
`DisabledColor`, `PanelColor`), `TitleScale`, `DimOpacity`, `PanelPadding`, positions
(`TitleTop`, `MenuTop`), transition durations, and `Sounds`.

The game builds its theme in [`GameTheme.Create`](../MechaHorde/UI/GameTheme.cs), which
the layer stack calls once content can be loaded. **Restyle the whole interface there**, not in
individual menus:

```csharp
public static UiTheme Create(ContentManager content, AudioManager audio) => new()
{
    TitleFont = content.Load<SpriteFont>("Fonts/menufont"),
    TextFont = content.Load<SpriteFont>("Fonts/menufont"),
    PanelTexture = content.Load<Texture2D>("Textures/gradient"),
    SelectedColor = Color.Orange,                  // e.g. change the highlight
    MenuTransition = TimeSpan.FromSeconds(0.3),    // e.g. snappier menus
    Sounds = new UiSoundSet { ... },
};
```

`Sounds` is an `IUiSounds`. Widgets call `Theme.Sounds.Play(UiSound.Move / Confirm / Cancel)`,
and the audio system supplies the implementation (see [Audio](audio.md#ui-sounds)).

## Menus

### `Menu` and `MenuItem`s

[`Menu`](../CIS580/CIS580.MonoGame/UI/Widgets/Menu.cs) is a widget, not a layer: a vertical list
of [`MenuItem`](../CIS580/CIS580.MonoGame/UI/Widgets/MenuItems.cs)s. Up and down move the
selection (wrapping, and skipping disabled items), left and right *adjust* the selected item, and
confirm *activates* it. It scrolls when there are more items than `MaxVisibleItems`, with
`^`/`v` markers.

| Item | Shows | Confirm | Left / right |
|---|---|---|---|
| `ButtonItem(text, onActivate)` | `Play Game` | calls `onActivate(source)` | — |
| `ToggleItem(label, get, set)` | `Sound: On` | toggles | toggles |
| `ChoiceItem<T>(label, choices, get, set, format)` | `Difficulty: Normal` | next choice | previous / next, wrapping |
| `RangeItem(label, min, max, step, get, set, format)` | `Music volume: 7` | steps up, wrapping to min | steps, stopping at the ends |

Items read and write their values through `get`/`set` delegates, so they always show the current
value and need no syncing. Every item has `IsEnabled`; disabled items are greyed out and skipped.

### `MenuLayer`

[`MenuLayer`](../CIS580/CIS580.MonoGame/UI/MenuLayer.cs) is a layer showing a titled `Menu`.
Derive from it and add items:

| Member | Purpose |
|---|---|
| `Menu` | The items |
| `Title` | Drawn above the menu, sliding into place |
| `DimBackground` | Darken the layers beneath (popups always do) |
| `FitToScreen` | Scroll when the menu is too tall for the window (default true) |
| `OnCancel(source)` | Called when the player cancels; closes the menu by default |
| `DrawBackdrop(gameTime)` | Override to draw something behind the menu |

You can add items in the constructor, or in `Load()` when they need services or settings (as
`OptionsLayer` does). If you override `Load()`, call `base.Load()`, which sets the transition
durations.

### `PromptLayer`

[`PromptLayer`](../CIS580/CIS580.MonoGame/UI/PromptLayer.cs) is a confirm/cancel popup. Its hint
text is built from the current bindings, so it stays correct after rebinding:

```csharp
var prompt = new PromptLayer("Are you sure you want to exit?");
prompt.Accepted += source => _onExit();
prompt.Cancelled += source => { /* optional */ };
Open(prompt);
```

### Other library layers

- [`BindingCaptureLayer`](../CIS580/CIS580.MonoGame/UI/BindingCaptureLayer.cs): "Press a key
  for Jump". Raises `Captured(binding)` or `Cancelled`, and gives up after `Timeout` (5 s).
- [`LoadingLayer`](../CIS580/CIS580.MonoGame/UI/LoadingLayer.cs): "Loading..." with a progress bar.
- [`WorldLayer`](../CIS580/CIS580.MonoGame/Views/WorldLayer.cs): draws the world through cameras
  (see [Views](views.md)).

## This game's layers

| Layer | Kind | Notes |
|---|---|---|
| [`BackgroundLayer`](../MechaHorde/UI/BackgroundLayer.cs) | backdrop, no input | Behind the menus; fades with its transition |
| [`MainMenuLayer`](../MechaHorde/UI/MainMenuLayer.cs) | menu | Callbacks for play, local multiplayer (pass `null` to remove it), exit. Cancel asks to confirm exiting. |
| [`OptionsLayer`](../MechaHorde/UI/OptionsLayer.cs) | menu | One item per `GameSettings` property; saves settings when closed |
| [`ControlsLayer`](../MechaHorde/UI/ControlsLayer.cs) | menu | Rebinding, with a custom `BindingItem` |
| [`PauseLayer`](../MechaHorde/UI/PauseLayer.cs) | popup menu | Cancel resumes; quit and exit ask to confirm |
| [`LobbyLayer`](../MechaHorde/UI/LobbyLayer.cs) | no input | Draws the lobby; `LobbyState` handles input |
| [`HudLayer`](../MechaHorde/Views/HudLayer.cs) | popup, no input | *Placeholder.* Pause hint; player labels and join/leave messages only in multiplayer |
| [`WorldView`](../MechaHorde/Views/WorldView.cs) | backdrop, no input | The game world (see [Views](views.md)) |

Some layers (`OptionsLayer`, `ControlsLayer`) take an `overGame` flag. When they're opened
from the pause menu, they are full (non-popup) menus with `DimBackground`, so they cover the pause
menu while the world (a backdrop) stays dimly visible.

> **Don't open a popup menu from a popup menu.** A popup leaves everything beneath it drawn, so
> both menus' text overlaps. Make the submenu a normal menu with `DimBackground = true` instead.

## Extending the UI

### Adding a menu

A game-over menu, used by the `GameOverState` on the [App Flow](flow.md#adding-a-state) page:

```csharp
using System;
using CIS580.Input;
using CIS580.UI;
using CIS580.UI.Widgets;

namespace MechaHorde.UI;

/// <summary>
/// The game-over menu. Like the other menus, it only presents choices; the state decides what they do.
/// </summary>
public class GameOverLayer : MenuLayer
{
    private readonly Action _onMainMenu;

    public GameOverLayer(int score, Action<InputSource> onPlayAgain, Action onMainMenu)
        : base($"Game Over - Score {score}")
    {
        _onMainMenu = onMainMenu;
        Menu.Add(new ButtonItem("Play Again", onPlayAgain))
            .Add(new ButtonItem("Main Menu", OnCancel));
    }

    // Cancelling goes to the main menu, rather than just closing this layer
    protected override void OnCancel(InputSource source) => _onMainMenu();
}
```

Notice that the layer takes **callbacks** and never touches `AppFlow`. Override `OnCancel` when
"back" should do something other than close the menu, as `PauseLayer` and `MainMenuLayer` do.

### Adding a custom menu item

Derive from `MenuItem` and override `Text`, plus `Activate` and/or `Adjust` as needed.
`ControlsLayer.BindingItem` is a full example. Here is a non-selectable heading:

```csharp
public sealed class LabelItem : MenuItem
{
    private readonly string _text;
    public LabelItem(string text) { _text = text; IsEnabled = false; }
    public override string Text => _text;
}
```

Selection starts on item 0, so don't make a disabled item the first one (or set
`Menu.SelectedIndex` to the first enabled item).

### Adding a popup with its own input

Derive from `Layer`, set `IsPopup = true`, and read `Ui` in `HandleInput`:

```csharp
public class MessageLayer : Layer
{
    private readonly string _message;
    public MessageLayer(string message) { _message = message; IsPopup = true; }

    public override void HandleInput(GameTime gameTime)
    {
        if (Ui.WasPressed(UiControl.Confirm) || Ui.WasPressed(UiControl.Cancel))
        {
            Theme.Sounds.Play(UiSound.Confirm);
            Close();
        }
    }

    public override void Draw(GameTime gameTime) => DrawMessagePanel(_message);
}
```

### Changing the HUD

[`HudLayer`](../MechaHorde/Views/HudLayer.cs) is a placeholder. It draws in **screen
space** (pixels), over the world. Give it whatever it needs to read (the session, the local
players) through its constructor, which `InGameState.Enter` calls, and draw from that state each
frame. For things that *happen* (a score popping up, a "Round 2" banner), subscribe it to
`GameEventRaised` as it already does for join/leave messages. The [walkthrough](walkthrough.md)
adds a score display.

To label something in the world (a name over a player), convert its position with
`camera.WorldToScreen(position)`.

Single-player games should never show multiplayer details. The HUD only shows player labels when
there is more than one local player; keep to that rule.

### Unloading content safely

A layer keeps drawing while it transitions off, after it was asked to close. Don't unload content
it draws with until its `Removed` event fires. `InGameState.Exit` shows the pattern.
