# Settings

[← Overview](README.md)

Players' settings (difficulty, volumes, vibration), input preferences (invert Y), and rebound
controls are loaded when the game starts and saved when they change. The library part is
[`SettingsStore`](../CIS580/CIS580.Core/Settings/SettingsStore.cs) in `CIS580.Core`, which
doesn't use MonoGame, so a server could use it too. The game's part is in
`MechaHorde/Settings/`.

## `SettingsStore`

A `SettingsStore` saves and loads objects as JSON files in one folder, one file per kind of
setting:

```csharp
var store = new SettingsStore(SettingsStore.DefaultDirectory("MechaHorde"));

var settings = store.Load("settings.json", () => new GameSettings());   // the defaults if missing
store.Save("settings.json", settings);
```

| Member | Purpose |
|---|---|
| `DefaultDirectory(gameName)` | A folder named after the game in the user's local application data folder |
| `Load<T>(name, defaults)` | Loads, or calls `defaults()` if the file is missing or unreadable |
| `TryLoad<T>(name, out value)` | The same, telling you whether it loaded |
| `Save<T>(name, value)` | Saves; returns `false` on failure |
| `Delete(name)`, `Exists(name)`, `PathOf(name)` | Managing files |
| `LastError` | The last problem, or `null` |

Settings are a convenience, so **problems never stop the game**:

- A missing file loads as the defaults.
- A damaged file (invalid JSON, or from an incompatible version) loads as the defaults, and is
  renamed to `name.bad` so you can inspect it.
- A failed save is reported in `LastError` and the return value, never thrown.
- Saves are written to a temporary file and then moved into place, so a crash partway through
  saving can't leave a half-written file.

The JSON is human-readable: indented, with camelCase property names and enums written as names.

## The game's settings

[`UserSettings`](../MechaHorde/Settings/UserSettings.cs) gathers everything the player can
set up, in three files. A problem with one file doesn't lose the others:

| File | Contents | Type |
|---|---|---|
| `settings.json` | Difficulty, player color, sound, volumes, vibration | [`GameSettings`](../MechaHorde/Settings/GameSettings.cs) |
| `input.json` | Invert move/look Y, dead zone | `InputProfile` |
| `controls.json` | **Only the controls the player changed** (deleted when there are none) | `List<ControlScheme>` from `ControlSet.GetChanges()` |

For example:

```json
// settings.json
{
  "difficulty": "Normal",
  "playerColor": "Red",
  "soundEnabled": true,
  "musicVolume": 7,
  "effectsVolume": 10,
  "vibration": true
}
```

```json
// controls.json, after rebinding Primary to J on the keyboard
[
  {
    "name": "Keyboard",
    "controls": {
      "Primary": [ { "type": "key", "key": "J" } ]
    }
  }
]
```

**When they load and save:**

- The game's constructor calls `UserSettings.Load(...)` and registers `UserSettings`,
  `GameSettings`, `InputProfile`, and `GameControls` as services, so everything shares the same
  instances.
- `OptionsLayer` saves when the player leaves it (which covers the Controls screen too).
- The game saves again on exit (`OnExiting`), catching anything changed elsewhere.

Debug builds print the settings folder at startup. The locations are listed in
[Getting Started](getting-started.md#where-settings-are-saved).

`GameSettings` is a **placeholder**: its properties show the kinds of settings the options menu
can edit (an enum, a list of choices, a toggle, a number). Replace them with your game's.

## Extending settings

### Adding a setting

1. Add a property with a default to `GameSettings`. It's saved automatically.
   ```csharp
   public bool ShowTimer { get; set; } = true;
   ```
2. Add an item for it to [`OptionsLayer.Load`](../MechaHorde/UI/OptionsLayer.cs):
   ```csharp
   .Add(new ToggleItem("Show timer", () => settings.ShowTimer, value => settings.ShowTimer = value))
   ```
   Use `ChoiceItem<T>` for enums or lists and `RangeItem` for numbers (see [UI](ui.md#menu-and-menuitems)).
3. Use it. Get `GameSettings` from `Game.Services` (or pass it into whatever needs it) and read
   the property when you need it. Reading it each frame means changes take effect immediately,
   as `GameAudio.Update` does with the volumes.

Old settings files keep working when you **add** properties (missing ones get their defaults).
**Renaming** a property loses the player's old value. **Changing its type** can make the file
unreadable, in which case it's renamed to `.bad` and the defaults are used.

### Settings and the simulation

Settings belong to the client and can change at any moment, but the simulation must be
[deterministic](simulation.md#rules-for-staying-deterministic). So never read `GameSettings`
from inside a tick. When a setting affects gameplay, such as `Difficulty`, copy it into the
session when the game starts:

```csharp
// InGameState.Load
var settings = services.GetService<GameSettings>();
Session = new GameSession(seed, settings.Difficulty);   // a constructor parameter you add
```

In a networked game, the host would choose these values and send them to everyone.

### Saving other data

The same store can save anything that serializes to JSON: high scores, unlocked levels, a
save game. Give it its own type and file name:

```csharp
public sealed class HighScores { public List<int> Scores { get; set; } = new(); }

var scores = store.Load("scores.json", () => new HighScores());
scores.Scores.Add(42);
store.Save("scores.json", scores);
```

`UserSettings.Store` gives you the store the game already uses.

Serialization uses System.Text.Json, so give saved types public properties with getters and
setters and a parameterless constructor. For polymorphic types, follow the pattern in
[`Bindings.cs`](../CIS580/CIS580.MonoGame/Input/Bindings.cs): `[JsonPolymorphic]` and
`[JsonDerivedType]` on the base type, and `[JsonIgnore]` on **each override** of any computed
property you don't want saved.
