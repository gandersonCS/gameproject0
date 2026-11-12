# MechaHorde Documentation

MechaHorde is a starter project for MonoGame games, written for CIS580. It gives you the
parts every game needs but nobody wants to rewrite each time:

- menus with transitions, prompts, and an options screen
- input that players can rebind, including gamepads and two players sharing one keyboard
- a local multiplayer lobby, pausing, and gamepad-disconnect handling
- saved settings
- music and sound effects with volume controls
- "game feel" effects: screen shake, gamepad rumble, and hit-stop
- a fixed-timestep, deterministic simulation that you can later run over a network

It deliberately contains **no real gameplay**. The world holds a placeholder avatar (the text
`// TODO`) and a wandering placeholder entity (`Insert Gameplay Here`). Replacing them with your
game is the point.

These pages assume you are comfortable with intermediate C#: classes and interfaces, generics,
delegates and events, lambdas, records, and `switch` pattern matching. They also assume you know
the basics of MonoGame: the `Game` class, `Update`/`Draw`, `SpriteBatch`, and `ContentManager`.

## Contents

| Page | What it covers |
|---|---|
| [Getting Started](getting-started.md) | Building, running, testing, default controls, where settings are saved |
| [Simulation](simulation.md) | The game's rules and state: ticks, commands, events, determinism |
| [Input](input.md) | Devices, bindings, control schemes, rebinding, players, menus' input |
| [App Flow](flow.md) | High-level states (main menu, lobby, loading, playing, paused) |
| [User Interface](ui.md) | Layers, the layer stack, menus, prompts, transitions, themes |
| [Views & Cameras](views.md) | Drawing the world, cameras, world units, split screen |
| [Audio](audio.md) | Buses, sound cues, music with cross-fades, UI sounds |
| [Game Feel](feedback.md) | Screen shake, rumble, hit-stop |
| [Settings](settings.md) | Saving and loading player settings and controls |
| [Content](content.md) | The content builder, adding assets, fonts, `ASSETS.md` |
| [Testing](testing.md) | The test projects, and how to test your own code |
| [Walkthrough: Your First Feature](walkthrough.md) | Adding collectible coins, touching every part of the architecture |

If you only read two pages, read this one and the [walkthrough](walkthrough.md).

## The projects

The solution (`MechaHorde.slnx`) is split into a reusable **library** and a **game** that
uses it.

| Project | Namespaces | Uses MonoGame? | Contains |
|---|---|---|---|
| `CIS580/CIS580.Core` | `CIS580.Simulation`, `CIS580.Settings` | **No** | The simulation runner, commands, deterministic random numbers, the settings store |
| `CIS580/CIS580.MonoGame` | `CIS580.Input`, `CIS580.UI`, `CIS580.Flow`, `CIS580.Views`, `CIS580.Audio`, `CIS580.Feedback` | Yes (platform-neutral) | Everything that needs MonoGame but isn't specific to one game |
| `MechaHorde.Simulation` | `MechaHorde.Simulation` | **No** | *This game's* rules and world |
| `MechaHorde` | `MechaHorde.*` | Yes (DesktopGL) | *This game's* client: states, menus, views, audio, settings |
| `MechaHorde.Content` | — | Content pipeline | Builds `Assets/` into `.xnb` content |
| `Tests/CIS580.Tests` | | | Tests for the library |
| `Tests/MechaHorde.Tests` | | | Tests for the game, including architecture rules |

```
                    MechaHorde  (the game client, DesktopGL)
                   /        |         \
                  /         |          \ runs at build time
                 v          v           v
     CIS580.MonoGame   MechaHorde.Simulation   MechaHorde.Content
                 \          |
                  v         v
                   CIS580.Core
```

Two rules hold this structure together:

1. **Library code is game-agnostic.** Anything in `CIS580/` must work for *any* game. Code that
   knows about *this* game (its actions, its menus, its sounds) lives in the `MechaHorde*`
   projects. When you extend the project, almost everything you write goes in the game projects.
   Move code into the library only when you would reuse it in another game.
2. **The simulation never touches MonoGame.** `CIS580.Core` and `MechaHorde.Simulation`
   must be able to run inside a headless server, where there is no window, graphics, audio,
   or input. A unit test ([`ArchitectureTests`](../Tests/MechaHorde.Tests/ArchitectureTests.cs))
   fails the build if either one references MonoGame.

## The big picture

The architecture separates four jobs:

| Job | Who does it | Analogy |
|---|---|---|
| Holding the game's state and applying its rules | The **simulation** (`GameSession`, `World`) | Model |
| Deciding where the player is in the app (menu, lobby, playing, paused) | **App states** in an `AppFlow` | Controller |
| Drawing things and turning menu input into choices | **Layers** in a `LayerStack` | View |
| Reacting to what happens (sounds, shake, rumble) | **Presentation services** (`GameAudio`, `GameFeedback`) | Observers |

During play, data flows in one direction:

```
 keyboard / mouse / gamepads
          |
          v
     InputState ............. reads every device once per frame
          |
          v
     InputSource ............ reads the player's devices through their (rebindable) ControlScheme
          |
          v
     CommandBuilder ......... turns controls into a PlayerCommand (move, look, actions)
          |
          v
     LocalCommandSource ..... holds the command until the next simulation tick
          |
          v
     SessionRunner .......... runs fixed 60 Hz ticks, gathering every player's command
          |
          v
     GameSession.Tick ....... applies the rules; raises events such as PlayerJoined
          |
          v
     EventQueue<GameEvent>
          |
          v
     InGameState.GameEventRaised
          |---> WorldView      (spawn flash)
          |---> HudLayer       (join/leave messages)
          |---> GameAudio      (sounds)
          '---> GameFeedback   (shake, rumble, hit-stop)

  ...and every frame, WorldView and HudLayer read GameSession.World to draw it.
```

The simulation sees only `PlayerCommand`s, never devices. It never plays sounds or draws
anything; it raises events and lets the client decide how to present them. This one-way flow
makes the simulation easy to test. It also lets the same simulation be driven by local players,
AI, a replay, or a network connection.

## One frame

[`MechaHordeGame`](../MechaHorde/MechaHordeGame.cs) is kept
deliberately small. Each frame it:

1. reads the input devices (`InputState.Update`, then `UiInput.Update` for menu navigation)
2. updates the app flow (`AppFlow.Update`): applies any requested state changes, then updates
   every state. During play, `InGameState` builds the players' commands and runs the simulation here.
3. updates the layer stack (`base.Update`, since `LayerStack` is a `DrawableGameComponent`):
   decides which layers are covered or focused, advances transitions, and lets the focused layers
   handle input
4. updates the audio (`GameAudio.Update`), applying any volume changes from the options menu
5. draws the layers, bottom to top (`base.Draw`)

Input is read *before* the simulation runs, so a press affects the very next tick.

## Shared services

There is no dependency-injection framework. Objects that many parts of the game need are
registered in MonoGame's built-in service container in the game's constructor, and retrieved
with `Game.Services.GetService<T>()`:

| Service | What it is |
|---|---|
| `InputState` | The current and previous state of every input device |
| `UiInput` | Menu navigation input, from whichever sources the current state chooses |
| `UserSettings` | Everything saved: game settings, input profile, controls |
| `GameSettings` | The game's own settings (difficulty, volumes, ...) |
| `InputProfile` | Input preferences (invert Y, dead zone) |
| `GameControls` | The control schemes, including the player's rebinding |
| `PausePolicy` | Who may pause, and whether pausing stops the simulation |
| `AudioManager` | Audio buses and the music player |
| `GameAudio` | This game's music and sound effects (registered in `LoadContent`) |

Avoid `static` fields for shared mutable state. Register a service instead, so there is exactly
one instance and it's obvious where it was created.

## Where to find things

```
CIS580/
  CIS580.Core/
    Simulation/      SessionRunner, PlayerCommand, ActionSet, DeterministicRandom, EventQueue, ...
    Settings/        SettingsStore
  CIS580.MonoGame/
    Input/           InputState, Bindings, ControlScheme, ControlSet, InputSource, UiInput, LocalLobby, ...
    UI/              Layer, LayerStack, MenuLayer, PromptLayer, Transition, UiTheme, Widgets/
    Flow/            AppFlow, AppState, LoadingState, PausePolicy
    Views/           Camera2D, ViewLayout, WorldLayer
    Audio/           AudioManager, AudioBus, SoundCue, MusicPlayer, UiSoundSet
    Feedback/        ScreenShake, Rumble, HitStop
  Build/             CIS580.Content.targets (runs the content builder)
MechaHorde.Simulation/   GameSession, World, PlayerAvatar, Wanderer, GameAction, GameEvents
MechaHorde/
  States/            MainMenuState, LobbyState, InGameState, PausedState
  UI/                GameTheme, BackgroundLayer, MainMenuLayer, OptionsLayer, ControlsLayer, PauseLayer, LobbyLayer
  Views/             WorldView, HudLayer, PlayerColors
  Input/             DefaultControls, GameControls
  Audio/             GameAudio
  Feedback/          GameFeedback
  Settings/          GameSettings, UserSettings
MechaHorde.Content/
  Builder.cs         which assets are built, and how
  Assets/            Fonts/, Textures/, Audio/
Tests/
ASSETS.md            provenance and license of every asset
```

## Conventions

- **Library vs. game.** Library types are in `CIS580.*` namespaces under `CIS580/`. Game types
  are in `MechaHorde.*` at the top level.
- **Comments.** Public members have XML doc comments that explain *why* as well as *what*. The
  source is the most detailed reference; these pages explain how the pieces fit together.
- **Policy belongs to the game.** Choices that differ between games, such as who can pause,
  which devices control the menus, or how many players can join, are settings or virtual methods
  in the library. They are not hard-coded.
- **Placeholders say so.** Anything you are expected to replace (the avatar, the wanderer, the
  HUD text, `GameSettings`) is marked as a placeholder in its comments.
- **Assets are documented.** Every asset is listed in [`ASSETS.md`](../ASSETS.md) with where it
  came from and its license. Update it whenever you add or remove one.

## Glossary

| Term | Meaning |
|---|---|
| **Tick** | One fixed-length step of the simulation (1/60 s by default) |
| **Command** | What one player wants to do during one tick (`PlayerCommand`) |
| **Action** | A game-specific button-like input (`GameAction.Primary`), stored as a bit in an `ActionSet` |
| **Control** | A named thing a player can do, such as `"MoveLeft"` or `"Primary"`, bound to inputs |
| **Binding** | One physical input (a key, button, or stick direction) bound to a control |
| **Control scheme** | A named set of controls and their bindings, e.g. "Keyboard (Left)" |
| **Input source** | Where one player's input comes from: one or more devices, each with a scheme |
| **App state** | Where the player is in the app (main menu, playing, paused, ...) |
| **Layer** | One piece of UI or view in the layer stack (a menu, the HUD, the world) |
| **Popup** | A layer that leaves the layers beneath it visible |
| **Backdrop** | A layer that stays visible even when covered |
| **Bus** | A group of sounds sharing a volume control |
| **Cue** | A sound effect, plus rules for how it plays |
| **Trauma** | How hard the screen is shaking, from 0 to 1 |
