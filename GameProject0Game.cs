using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;


namespace gameproject0;

public class GameProject0Game : Game
{

    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    private Camera2D camera;
    PlanetSprite planet;
    private SpriteFont titleFont;
    private SpriteFont buttonFont;
    MenuButtonSprite campaignButton;
    MenuButtonSprite multiplayerButton;
    MenuButtonSprite optionsButton;

    MenuButtonSprite exitButton;
    private Texture2D _pixel;
    private Gun playerGun;
    SoundEffect _playSound;
    Song _menu;
    Song _backgroundMusic;

    private float gameTimer = 0f;
    private bool started;

    private Player player;
    private Texture2D _foreground;
    private Texture2D _midground;
    private Texture2D _background;

    private MenuButtonSprite[] menuButtons;
    /// <summary>
    /// A game demonstrating collision detection
    /// </summary>
    public GameProject0Game()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        graphics.PreferredBackBufferWidth = Constants.GAME_WIDTH;
        graphics.PreferredBackBufferHeight = Constants.GAME_HEIGHT;
    }

    /// <summary>
    /// Initializes the game 
    /// </summary>
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        System.Random random = new System.Random();
        camera = new Camera2D();
        player = new Player();
        planet = new PlanetSprite();
        int leftMargin = 50;
        //Space between button list and title
        int startingY = 150;
        //Space between button
        int verticalSpacing = 75;
        menuButtons = new MenuButtonSprite[]
        {
campaignButton = new MenuButtonSprite(new Vector2(leftMargin, startingY))
    {
        Text = "CAMPAIGN",
        OnClick = () =>
        {
            started = true;
            MediaPlayer.Play(_backgroundMusic);
            
            // Generate and add the rain the exact moment the button is clicked
           // Start spawning rain at -500 to account for the diagonal wind, and make it wide enough to cover the whole 5120 level
RainParticleSystem rain = new RainParticleSystem(this, new Rectangle(-500, -20, 6000, 10));
Components.Add(rain);
        }
    },
    multiplayerButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + verticalSpacing)) { Text = "MULTIPLAYER" },
    optionsButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + (verticalSpacing * 2))) { Text = "OPTIONS" },
    exitButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + (verticalSpacing * 3))) { Text = "EXIT(Press ESC)", OnClick = () => Exit()}
        };
        if (started)
        {
            RainParticleSystem rain = new RainParticleSystem(this, new Rectangle(100, -20, 500, 10));
            Components.Add(rain);
        }


        base.Initialize();
    }

    /// <summary>
    /// Loads content for the game
    /// </summary>
    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        player.LoadContent(Content);
        planet.LoadContent(Content);

        // TODO: use this.Content to load your game content here
        foreach (var but in menuButtons) but.LoadContent(Content);
        titleFont = Content.Load<SpriteFont>("aquire");
        buttonFont = Content.Load<SpriteFont>("robotheroes");
        _playSound = Content.Load<SoundEffect>("cg1");
        _menu = Content.Load<Song>("promise");
        _backgroundMusic = Content.Load<Song>("out");
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Play(_menu);
        _foreground = Content.Load<Texture2D>("foreground");
        _midground = Content.Load<Texture2D>("midground");
        _background = Content.Load<Texture2D>("background");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        playerGun = new Gun(_pixel);
    }

    /// <summary>
    /// Updates the game world
    /// </summary>
    /// <param name="gameTime">The game time</param>
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            _playSound.Play();
            Exit();
        }

        if (started)
        {
            player.Update(gameTime);

            // 1. Trap the player at exactly 1000
            player.position.X = MathHelper.Clamp(player.position.X, 16f, 1000f);

            float scale = 6.0f;
            float screenCenterW = GraphicsDevice.Viewport.Width / 2f;

            // 2. The dynamic deadzone (Ensure you deleted the hardcoded 'float deadzone = 50f;')
            float deadzone = (screenCenterW / scale) - 20f;

            if (player.position.X > camera.Position.X + deadzone)
                camera.Position = new Vector2(player.position.X - deadzone, 100f);
            else if (player.position.X < camera.Position.X - deadzone)
                camera.Position = new Vector2(player.position.X + deadzone, 100f);

            // 3. Stop the camera from scrolling past the 1000 pixel mark
            float minCameraX = screenCenterW / scale;
            float maxCameraX = 1000f - minCameraX;

            camera.Position = new Vector2(
                MathHelper.Clamp(camera.Position.X, minCameraX, maxCameraX),
                100f
            );

            Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);

            // 1. Sync the offset math EXACTLY with the formula in your Draw method
            float offsetX = screenCenter.X - (camera.Position.X * scale);
            float floorScreenPosition = GraphicsDevice.Viewport.Height - 50f;
            float offsetY = floorScreenPosition - (100f * scale);

            // 2. Convert screen mouse coordinates into accurate world coordinates
            MouseState mouse = Mouse.GetState();
            Vector2 worldMousePosition = new Vector2(
                (mouse.X - offsetX) / scale,
                (mouse.Y - offsetY) / scale
            );

            // 3. Update the gun with the correct targeted mouse position
            playerGun.Update(gameTime, player.position, worldMousePosition);
        }
        else
        {

            foreach (var but in menuButtons) but.Update(gameTime);
            planet.Update(gameTime, GraphicsDevice.Viewport.Width);
        }






        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the game world
    /// </summary>
    /// <param name="gameTime">The game time</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkRed);
        Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);

        //Parallax movement and transformation


        if (started)
        {
            // 1. SCALES
            float scale = 6.0f;           // Uniform scale for player, foreground, and midground


            // 2. CAMERA MATH
            // Delete the 'scaledPlayerX' and 'MathHelper.Clamp' lines here!
            // Calculate the visual offset using the actual Camera object from your Update loop:
            float offsetX = screenCenter.X - (camera.Position.X * scale);

            // Y-Axis Anchor
            float floorScreenPosition = GraphicsDevice.Viewport.Height - 50f;
            float offsetY = floorScreenPosition - (100f * scale);

            // BATCH 1: Background 
            // FIX: Changed the Y translation from 'offsetY' to '0' so it anchors to the top of the screen
            Matrix bgTransform = Matrix.CreateScale(scale, scale, 1) * Matrix.CreateTranslation(offsetX * 0.33f, 0, 0);

            // 1. Change PointClamp to PointWrap
            spriteBatch.Begin(samplerState: SamplerState.PointWrap, transformMatrix: bgTransform);

            // 2. Add a massive destination rectangle (e.g., 5120 width) so it loops instead of cutting off
            spriteBatch.Draw(_background, Vector2.Zero, new Rectangle(0, 0, 1000, _background.Height), Color.DarkOrange);

            spriteBatch.End();

            // BATCH 2: Midground (Tiled to exactly 1000 pixels)
            Matrix mgTransform = Matrix.CreateScale(scale, scale, 1) * Matrix.CreateTranslation(offsetX * 0.66f, offsetY, 0);
            spriteBatch.Begin(samplerState: SamplerState.PointWrap, transformMatrix: mgTransform);

            // Set the Rectangle width to 1000 instead of the background's width
            spriteBatch.Draw(_midground, Vector2.Zero, new Rectangle(0, 0, 1000, _midground.Height), Color.DarkRed);
            spriteBatch.End();

            // BATCH 3 & 3.5: Foreground and Player (Tiled to exactly 1000 pixels)
            Matrix fgTransform = Matrix.CreateScale(scale, scale, 1) * Matrix.CreateTranslation(offsetX, offsetY, 0);
            spriteBatch.Begin(samplerState: SamplerState.PointWrap, transformMatrix: fgTransform);

            spriteBatch.Draw(_foreground, Vector2.Zero, new Rectangle(0, 0, 1000, _foreground.Height), Color.DarkSalmon);
            player.Draw(gameTime, spriteBatch);
            playerGun.Draw(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin();
            // Draws a dark grey rectangle starting exactly at the floor's screen position and filling the bottom 100 pixels
            spriteBatch.Draw(_pixel, new Rectangle(0, (int)floorScreenPosition, GraphicsDevice.Viewport.Width, 100), Color.SlateGray);
            spriteBatch.End();


            // BATCH 4: HUD
            spriteBatch.Begin();
            string timerText = $"Time: {gameTimer:F2}";
            string playerPosition = $"\nPlayer X: {player.position.X}\nPlayer Y: {player.position.Y}";
            spriteBatch.DrawString(
                titleFont,
                timerText + playerPosition,
                new Vector2(10, 10),
                Color.Red,
                0f,
                Vector2.Zero,
                0.5f,
                SpriteEffects.None,
                0f
            );
            spriteBatch.End();
        }
        else
        {
            //Draw the planet, buttons, and title for the menu
            spriteBatch.Begin();
            planet.Draw(gameTime, spriteBatch);
            foreach (var but in menuButtons) but.Draw(gameTime, spriteBatch);

            spriteBatch.DrawString(titleFont, "GAME TITLE", new Vector2(2, 2), Color.Gold);
            spriteBatch.End();
        }



        base.Draw(gameTime);
    }
}
