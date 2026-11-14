# Assets

All assets used in this project, with their provenance and license.
Content assets live in `MechaHorde.Content/Assets/`.

## Textures

- `Textures/Sprites/Sara/SaraMain.png`, `Textures/Sprites/Sara/SaraShooting.png` - "Space Sara"
  (no helmet) main and shooting frames, created by Emcee Flesher (+ansimuz) of OpenGameArt,
  released under OGA-BY 3.0+ (as credited on the sheets). TODO: add the OpenGameArt page link
- `Textures/Sprites/M484SpaceSoldier.png` - created by Master484 of OpenGameArt
  [https://opengameart.org/content/space-soldier-m484-games](https://opengameart.org/content/space-soldier-m484-games),
  released under the public domain
- `Textures/Menu/futureui1.png` - created by bart of OpenGameArt
  [https://opengameart.org/content/sci-fi-ui-panels](https://opengameart.org/content/sci-fi-ui-panels),
  released under the public domain
- `Textures/Menu/planet1.png`, `Textures/Menu/Beta 6.png`, `Textures/Menu/noShadow.png` - created by
  StumpyStrust of OpenGameArt [https://opengameart.org/content/beta-6](https://opengameart.org/content/beta-6),
  released under the public domain
- `Textures/Level/background.png`, `Textures/Level/midground.png`, `Textures/Level/foreground.png` -
  Warped City, created by ansimuz of OpenGameArt
  [https://opengameart.org/content/warped-city](https://opengameart.org/content/warped-city),
  released under the public domain
- `Textures/Sprites/OpenGunnerMechs.png`, `Textures/Sprites/Enemies/GreenMech.png` (the green mech
  frames cut from it) - Open Gunner Mechs, created by Master484 of OpenGameArt, released under CC-BY 3.0
- `Textures/Sprites/Enemies/RobotGunner.png` - the robot gunner frames (robotgunner.zip) packed into
  one sheet. TODO: author, link and license
- `Textures/Sprites/Enemies/SecuriBot.png` - the Securi-Bot frames (securityrobot.png) packed into
  one sheet. TODO: author, link and license
- `Textures/Effects/drop.png` - rain drop particle
- `Textures/Pickups/spark_block.png` - the spark's texture: the "punch" block's ambient occlusion
  (`punch_a.png`) on dark metal, with its glow (`punch_emit.png`) added on top, from the Block Pack
  (see Models below), released under CC0 (public domain)
- `Textures/gradient.png` - procedurally generated for the CIS580 game architecture, released under
  the public domain

## Models

- `Models/scrap_gear.obj`, `Models/scrap_propeller.obj` - "BrokenGear" and "BrokenPropellor", made in
  Art of Illusion 2.8.1 (2011). Centred, scaled so their largest side is 2 units, and triangulated
  (with flat normals added to the gear) for the game. TODO: author, link and license
- `Models/spark_hip.obj`, `spark_kick.obj`, `spark_bomb.obj`, `spark_nuke.obj`, `spark_orb.obj` -
  `block-hip.fbx`, `block-kick.fbx`, `bomb.fbx`, `nuke.fbx` and `orb.fbx` from the Block Pack, converted
  the same way as `spark_block.obj` below, with matching textures in `Textures/Pickups/` (each made
  from the model's base and glow textures). CC0 (public domain)
- `Models/spark_block.obj` - `block-punch.fbx` from the Block Pack (block pack + shaders),
  released under CC0 (public domain). Converted to OBJ, centred, and scaled so its largest side is
  2 units. TODO: add the pack's page link

## Fonts

- `Fonts/LiberationSans-Regular.ttf` - Liberation Sans 2.1.5, from the Liberation Fonts project
  [https://github.com/liberationfonts/liberation-fonts](https://github.com/liberationfonts/liberation-fonts),
  released under the SIL Open Font License 1.1 (see `Fonts/LiberationSans-LICENSE.txt`).
  Used by all the `.spritefont` files (`aquire`, `robotheroes`, `menufont`, `gamefont`).
- The Aquire font (by Sesohq) and Robot Heroes font (by HAWTPIXEL), from Fontspace
  [https://www.fontspace.com/category/futuristic](https://www.fontspace.com/category/futuristic),
  give `aquire.spritefont` and `robotheroes.spritefont` their names.

## Audio

- `Audio/Music/uprising_intro.ogg`, `Audio/Music/uprising_loop.ogg` - cut from 'Uprising' by
  Scott Buckley – released under CC-BY 4.0. www.scottbuckley.com.au
- `Audio/Music/emergent_loop.ogg` - cut from 'Emergent' by Scott Buckley – released under
  CC-BY 4.0. www.scottbuckley.com.au
  [https://www.scottbuckley.com.au/library/emergent](https://www.scottbuckley.com.au/library/emergent)
- `Audio/Music/calamity_intro.ogg`, `Audio/Music/calamity_loop.ogg` - cut from "Calamity (Epic
  Orchestral Metal)" by Esiltir of OpenGameArt
  [https://opengameart.org/node/74577](https://opengameart.org/node/74577),
  released under Creative Commons Attribution 4.0 (CC-BY 4.0). (The author asks to be sent a
  message about games that use it.)
- `Audio/Music/promise.wav` (no longer played) - created by hothxcc of OpenGameArt
  [https://opengameart.org/content/promise](https://opengameart.org/content/promise),
  released under the public domain
- `Audio/Music/out.mp3` (no longer played) - created by G_P of OpenGameArt
  [https://opengameart.org/content/out](https://opengameart.org/content/out),
  released under the public domain
- `Audio/Sfx/Shots/*`, `Audio/Sfx/Launches/*`, `Audio/Sfx/cg1.wav` - created by Michel Baradari of
  OpenGameArt [https://opengameart.org/content/chaingun-pistol-rifle-shotgun-shots](https://opengameart.org/content/chaingun-pistol-rifle-shotgun-shots),
  released under Creative Commons
- `Audio/Sfx/Click_*.wav` - UI Sound Library, created by Little Robot Sound Factory of OpenGameArt
  [https://opengameart.org/content/ui-sound-effects-library](https://opengameart.org/content/ui-sound-effects-library),
  released under Creative Commons
- `Audio/Sfx/player_join.wav`, `Audio/Sfx/player_leave.wav` - procedurally generated for the CIS580
  game architecture, released under the public domain

Gameplay sound effects (converted to 44.1 kHz 16-bit WAV and peak-normalized; the footsteps and
heavy clangs were trimmed and faded from the cast iron recordings):

- `Audio/Sfx/Weapons/rifle_shot_1.wav`, `Audio/Sfx/Weapons/rifle_shot_2.wav` - "Futuristic Weapons /
  Assault Rifle", created by Michael Klier of OpenGameArt
  [https://opengameart.org/node/47021](https://opengameart.org/node/47021),
  released under CC-BY-SA 3.0
- `Audio/Sfx/Weapons/laser_rifle.wav` - "Laser rifle" (made for Red Eclipse), created by Skorpio of
  OpenGameArt [https://opengameart.org/content/laser-rifle](https://opengameart.org/content/laser-rifle),
  released under CC-BY-SA 3.0
- `Audio/Sfx/Weapons/burst_fire.wav` - "Laser weapon burst fire", created by celestialghost8 of
  OpenGameArt [https://opengameart.org/content/laser-weapon-burst-fire](https://opengameart.org/content/laser-weapon-burst-fire),
  released under CC0 (public domain)
- `Audio/Sfx/Robots/mechanical_explosion.wav` - "Mechanical Explosion", created by Spring Spring of
  OpenGameArt [https://opengameart.org/content/mechanical-explosion](https://opengameart.org/content/mechanical-explosion),
  released under CC0 (public domain)
- `Audio/Sfx/Impacts/clink_*.wav`, `Audio/Sfx/Impacts/thud_*.wav`, `Audio/Sfx/Player/pickup.wav`,
  `Audio/Sfx/Waves/wave_gong.wav` - "Metal Impact Sounds", created by Brian MacIntosh (BMacZero) of
  OpenGameArt [https://opengameart.org/content/metal-impact-sounds](https://opengameart.org/content/metal-impact-sounds),
  released under CC0 (public domain)
- `Audio/Sfx/Player/step_*.wav`, `Audio/Sfx/Impacts/clang_heavy_*.wav` - "33 metal clang sounds from
  cast iron pans", created by bart of OpenGameArt
  [https://opengameart.org/node/5249](https://opengameart.org/node/5249),
  released under CC0 (public domain)
- `Audio/Sfx/Player/hurt.wav`, `Audio/Sfx/Player/shield.wav`, `Audio/Sfx/Robots/ground_pound.wav`,
  `Audio/Sfx/Waves/warcry.wav` - "Osare.10 Sound Pack" (made for Osare.10 for Mr.Pfunked), submitted
  by HaelDB to OpenGameArt [https://opengameart.org/content/osare-10-sound-pack](https://opengameart.org/content/osare-10-sound-pack),
  released under CC-BY 3.0 (the page no longer shows its author or license; archived copies list
  CC-BY 3.0)

## Code

- The CIS580 game architecture [https://github.com/ksu-cis-580/game-architecture](https://github.com/ksu-cis-580/game-architecture)
- Debug assistance from Gemini

## Application icon

- `MechaHorde/Icon.ico`, `MechaHorde/Icon.bmp` - the default icons from the MonoGame project
  templates [https://monogame.net](https://monogame.net), released under the Microsoft Public License (Ms-PL).
