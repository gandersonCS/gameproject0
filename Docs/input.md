# Input

[← Overview](README.md)

The input system turns physical devices into two kinds of output:

- **`PlayerCommand`s for the simulation**, built from each player's movement, look, and action
  controls
- **menu navigation** (`UiInput`), for whichever players the current app state chooses

It supports rebinding, inverted axes, dead zones, a single player switching between keyboard and
gamepad, and two players sharing one keyboard. All of it is in `CIS580.MonoGame/Input/`, except
two game-specific classes in `MechaHorde/Input/`.

## How the pieces fit

```
 Keyboard+Mouse   Gamepad 0   Gamepad 1 ...
        \             |          /
         v            v         v
      InputState  (Current and Previous InputSnapshot)
                      |
                      v
      InputSource "Player"                          <- one per player
        - (KeyboardMouse, scheme "Keyboard")
        - (GamePad 0,     scheme "Gamepad")
                      |
                      |   Value("MoveLeft") = strongest binding on any of its devices
                      v
      CommandBuilder.Build<GameAction>(source, profile, input)
                      |   + InputProfile (invert Y, dead zone)
                      v
                PlayerCommand  --> LocalPlayer.Commands (LocalCommandSource) --> SessionRunner
```

Game code should almost never ask "is the A key down?". It asks "is this player's `Primary`
control down?", and the player's control scheme decides which keys, buttons, or sticks that means.

## Devices and snapshots

### `InputState`

[`InputState`](../CIS580/CIS580.MonoGame/Input/InputState.cs) reads the keyboard, the mouse, and
up to four gamepads **once per frame**, keeping this frame's and last frame's
[`InputSnapshot`](../CIS580/CIS580.MonoGame/Input/InputSnapshot.cs). Comparing the two tells you
whether something was *just* pressed.

- `Update()` reads the real devices. The game calls it first thing in `Update`.
- `Update(keyboard, mouse, pads...)` feeds it states you made yourself, for tests or for
  playing back recorded input.
- `GamePadWasDisconnected(index)` is true if a gamepad that was connected at some point has been
  unplugged. It is used to pause the game when a player's controller drops.

Gamepads are read with **no dead zone** (`GamePadDeadZone.None`). The player's `InputProfile`
applies the dead zone later.

`InputState` also has low-level helpers (`IsKeyDown`, `WasKeyPressed`, `IsButtonDown`,
`WasButtonPressed`). Prefer going through an `InputSource`, so the player's bindings are respected.

### `InputDevice`

[`InputDevice`](../CIS580/CIS580.MonoGame/Input/InputDevice.cs) identifies one device: the
keyboard and mouse (treated as one device), or a particular gamepad:

```csharp
InputDevice.KeyboardMouse
InputDevice.ForGamePad(0)
```

## Bindings

A [`Binding`](../CIS580/CIS580.MonoGame/Input/Bindings.cs) is one physical input. It reads as a
value from 0 (not pressed) to 1 (fully pressed). It counts as *pressed* at
`Binding.PressThreshold` (0.5) or above.

| Binding | Example | Reads |
|---|---|---|
| `KeyBinding(Keys key)` | `new KeyBinding(Keys.Space)` | 0 or 1 |
| `MouseButtonBinding(MouseButton button)` | `new MouseButtonBinding(MouseButton.Left)` | 0 or 1 |
| `GamePadButtonBinding(Buttons button)` | `new GamePadButtonBinding(Buttons.A)` | 0 or 1 |
| `GamePadAxisBinding(GamePadAxis axis, bool positive)` | `new GamePadAxisBinding(GamePadAxis.LeftStickX, false)` = left stick pushed left | 0 to 1 |

Bindings are records, so two bindings to the same input are equal. That equality is what lets
the rebinding code find conflicts. Each binding has a `DisplayName` for on-screen hints
(`"Esc"`, `"Left Stick Up"`), and JSON attributes so control schemes can be saved.

## Controls and control schemes

A **control** is anything a player can do, identified by a string name:

- the **standard controls** every game has, in
  [`StandardControls`](../CIS580/CIS580.MonoGame/Input/StandardControls.cs): `MoveLeft/Right/Up/Down`,
  `LookLeft/Right/Up/Down`, `Pause`, and the menu controls `UiUp/Down/Left/Right/Confirm/Cancel`
- the **game's actions**, named after the `GameAction` enum values: `"Primary"`, `"Secondary"`

A [`ControlScheme`](../CIS580/CIS580.MonoGame/Input/ControlScheme.cs) maps each control name to
a list of bindings:

```csharp
var scheme = new ControlScheme("Keyboard")
    .Bind(StandardControls.MoveLeft, new KeyBinding(Keys.A), new KeyBinding(Keys.Left))
    .Bind(nameof(GameAction.Primary), new KeyBinding(Keys.Space));
```

| Method | Purpose |
|---|---|
| `Bind(control, bindings...)` | Adds bindings (chainable) |
| `GetBindings(control)` | The bindings for a control (empty if none) |
| `SetBindings(control, bindings)` | Replaces a control's bindings |
| `Unbind(control, binding)` | Removes one binding |
| `Rebind(control, binding, group)` | "Press a key for Jump": replaces the control's bindings with one input, taking it from any other control in `group`. If that leaves the other control with nothing, the two controls swap. |
| `FindControlsUsing(binding)` | Finds conflicts |
| `Clone(name)` | An independent copy |

[`StandardSchemes`](../CIS580/CIS580.MonoGame/Input/StandardSchemes.cs) provides four defaults
for the standard controls: `Keyboard()`, `KeyboardLeft()`, `KeyboardRight()`, and `GamePad()`.
The game's [`DefaultControls`](../MechaHorde/Input/DefaultControls.cs) starts from each of
these and adds bindings for the game's own actions.

Menu controls and movement deliberately share keys (WASD moves the player *and* the menu
selection). They are never used at the same time.

### `ControlSet`: the schemes in use

[`ControlSet`](../CIS580/CIS580.MonoGame/Input/ControlSet.cs) holds the schemes the game
actually uses, one of each kind, plus a copy of the defaults. Every input source refers to these
*same* scheme objects, so rebinding a control changes it everywhere at once: in menus, in a game
in progress, and in on-screen hints.

- `ResetToDefaults(scheme)` / `ResetAllToDefaults()`
- `GetChanges()` returns only the controls the player changed. Those are what gets saved.
- `ApplyChanges(changes)` re-applies saved changes on top of the defaults when the game starts.

Because only *changes* are saved, a binding you add to `DefaultControls` in a later version still
reaches players who already have saved controls.

The game's [`GameControls`](../MechaHorde/Input/GameControls.cs) wraps the `ControlSet`
and is registered as a service. It gives the schemes names (`Keyboard`, `KeyboardLeft`,
`KeyboardRight`, `GamePad`) and builds the input sources the states need:

| Method | Returns |
|---|---|
| `SinglePlayer(gamePadIndex)` | One player using the whole keyboard **and** a gamepad, interchangeably |
| `SinglePlayerFor(chosenWith)` | The same, using whichever gamepad (if any) chose "Play Game" |
| `AllDevices()` | A source per device, for menus anyone may use |
| `LobbyCandidates()` | Each keyboard half and each gamepad, for joining local multiplayer |
| `Rebindable` | The controls shown on the Controls screen (movement, every `GameAction`, pause) |

## Input sources and players

### `InputSource`

An [`InputSource`](../CIS580/CIS580.MonoGame/Input/InputSource.cs) is where *one player's*
input comes from: one or more `(device, scheme)` assignments. A control's value is the strongest
value across all of them.

```csharp
// A solo player who can use the keyboard or gamepad 0
new InputSource("Player",
    new InputSource.Assignment(InputDevice.KeyboardMouse, controls.Keyboard),
    new InputSource.Assignment(InputDevice.ForGamePad(0), controls.GamePad));

// One of two players sharing the keyboard
new InputSource(InputDevice.KeyboardMouse, controls.KeyboardLeft);
```

| Member | Purpose |
|---|---|
| `Value(control, input)` | 0 to 1 |
| `IsDown`, `WasPressed`, `WasReleased` | Digital checks against `PressThreshold` |
| `DescribeBindings(control)` | e.g. `"Esc / Start"`, for on-screen hints that stay correct after rebinding |
| `HasLostDevice(input)` | True if one of its gamepads was unplugged |

### `InputProfile`

[`InputProfile`](../CIS580/CIS580.MonoGame/Input/InputProfile.cs) holds a player's
preferences: `InvertLookY`, `InvertMoveY`, and `DeadZone` (default 0.2). It is applied when
commands are built, never in the simulation, so one player's preferences can't affect anyone
else's game. There is currently one profile, shared by all local players, registered as a
service and saved in `input.json`.

### `CommandBuilder` and `LocalPlayer`

[`CommandBuilder.Build<TAction>`](../CIS580/CIS580.MonoGame/Input/CommandBuilder.cs) reads the
four move controls and the four look controls into vectors, with a circular dead zone and the
profile's inversion. It then reads each value of the action enum **using the control with the
same name**. That naming convention is the whole link between `GameAction.Primary` and the
`"Primary"` bindings.

[`LocalPlayer`](../CIS580/CIS580.MonoGame/Input/LocalPlayer.cs) bundles everything about a
player on this machine: their `PlayerId`, `InputSource`, `InputProfile`, and the
`LocalCommandSource` that feeds the runner. `InGameState` calls one of these every frame for
each player:

```csharp
player.SubmitCommand<GameAction>(Input);   // read input, build, and submit the command
player.SubmitIdle();                       // stand still (e.g. while using a menu)
```

## Menu input: `UiInput`

Menus never read devices. They ask [`UiInput`](../CIS580/CIS580.MonoGame/Input/UiInput.cs)
whether a navigation control (`UiControl.Up`, `Down`, `Left`, `Right`, `Confirm`, `Cancel`)
fired, and **which source** fired it:

```csharp
if (Ui.WasPressed(UiControl.Confirm, out InputSource who)) { ... }
```

- **Who is listened to** is decided by the current app state through `Sources`, usually with
  `AppState.ListenTo(...)`. The main menu listens to every device. The pause menu listens only
  to the player who paused (by default; see `PausePolicy`). During play, menus listen to no one.
- **Held directions repeat** after `RepeatDelay` (0.4 s), every `RepeatInterval` (0.1 s), so
  players can scroll long lists.
- **A control only counts if it was pressed while being listened to.** A button held down while a
  menu opens doesn't immediately trigger something in that menu.
- `DescribeBindings(control)` lists the bindings across the current sources (`"Enter, Space, A"`).
  `PromptLayer` uses it for its hints.

Knowing *who* pressed a button matters. When someone chooses *Play Game* with gamepad 2, the
game starts with that player on gamepad 2.

## Local multiplayer lobby

[`LocalLobby`](../CIS580/CIS580.MonoGame/Input/LocalLobby.cs) gathers players from a list of
candidate sources (`GameControls.LobbyCandidates()`):

- **confirm** on an unused candidate joins, taking the next seat
- **confirm** again marks that player ready
- **cancel** un-readies; cancel again leaves

`CanStart` is true when at least `MinPlayers` (2) have joined and everyone is ready. The lobby
only tracks seats. [`LobbyState`](../MechaHorde/States/LobbyState.cs) runs the countdown
and starts the game, and [`LobbyLayer`](../MechaHorde/UI/LobbyLayer.cs) draws it.

## Rebinding

The Controls screen ([`ControlsLayer`](../MechaHorde/UI/ControlsLayer.cs)) lists the
`GameControls.Rebindable` controls for a chosen scheme. Choosing one opens a
[`BindingCaptureLayer`](../CIS580/CIS580.MonoGame/UI/BindingCaptureLayer.cs), which uses
[`BindingCapture.TryCapture`](../CIS580/CIS580.MonoGame/Input/BindingCapture.cs) to find the
first newly pressed input on the scheme's devices. Pushing a stick captures an analog axis
binding, not a D-pad-like button. The capture is then applied with
`scheme.Rebind(control, binding, GameControls.Rebindable)`.

Design decisions you may want to revisit:

- **Menu controls can't be rebound**, so players can't lock themselves out of the menus.
- **Esc (keyboard) and Back (gamepad) always cancel** a capture, so they can't be bound there.
- **Rebinding replaces all of a control's inputs.** "Move up: W, Up" becomes just the new key.
- **Conflicts are checked within one scheme only.** The left and right keyboard halves can end
  up sharing a key.

## Extending input

### Adding a game action

1. Add a value to [`GameAction`](../MechaHorde.Simulation/GameAction.cs) (0–31):
   ```csharp
   public enum GameAction { Primary = 0, Secondary = 1, Jump = 2 }
   ```
2. Give it default bindings in **every** scheme in
   [`DefaultControls`](../MechaHorde/Input/DefaultControls.cs):
   ```csharp
   public static ControlScheme Keyboard() => StandardSchemes.Keyboard()
       // ...existing actions...
       .Bind(nameof(GameAction.Jump), Key(Keys.J));
   ```
   Check the keyboard halves don't share a key with each other.
3. Read it in the simulation: `input.WasPressed(GameAction.Jump)`.

That's all. `CommandBuilder` finds it by name, it appears on the Controls screen automatically
(`Rebindable` includes every `GameAction`), and existing players' saved controls pick up its
default bindings.

### Adding a client-only control

Some controls never reach the simulation: toggling a map, taking a screenshot, showing the
scoreboard. Give them a name, bind them, and read them from the state or layer that cares:

```csharp
public static class GameControlNames { public const string ToggleMap = "ToggleMap"; }

// DefaultControls.Keyboard():
    .Bind(GameControlNames.ToggleMap, Key(Keys.M))

// InGameState.Update:
foreach (var player in _players)
    if (player.Input.WasPressed(GameControlNames.ToggleMap, Input))
        _worldView.ShowMap = !_worldView.ShowMap;   // a property you'd add to WorldView
```

Add it to `GameControls.Rebindable` if players should be able to rebind it.

### Aiming with the mouse

`PlayerCommand.Look` is a *direction*, because the simulation has no idea where the screen is.
To aim with the mouse, convert the mouse position to world coordinates in the client, then turn
it into a direction before submitting the command:

```csharp
var command = CommandBuilder.Build<GameAction>(player.Input, player.Profile, Input);
var mouseWorld = camera.ScreenToWorld(Input.Current.Mouse.Position.ToVector2());
var toMouse = mouseWorld.ToNumerics() - avatar.Position;   // the simulation uses System.Numerics
if (toMouse.LengthSquared() > 1f)
    command.Look = System.Numerics.Vector2.Normalize(toMouse);
player.Commands.Submit(command);
```

(`camera` is the `Camera2D` showing that player; see [Views](views.md).)

### A new kind of binding

To read a new kind of input, such as the mouse wheel, derive from `Binding`, implement `Read`,
`DisplayName`, and `DeviceKind`, and register it for JSON with
`[JsonDerivedType(typeof(MyBinding), "myKind")]` on `Binding`. Put `[JsonIgnore]` on **each
override** of `DeviceKind` and `DisplayName`. System.Text.Json doesn't inherit `[JsonIgnore]`
from abstract members, and without it the computed properties end up in `controls.json`.

### Per-player profiles

All local players currently share one `InputProfile`. To give each player their own, create a
profile per seat (e.g. in a list saved in `input.json`) and pass the right one to each
`LocalPlayer` in `InGameState.Load`. The `LocalPlayer` constructor already takes a profile.
