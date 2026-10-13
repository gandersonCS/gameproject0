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
campaignButton = new MenuButtonSprite(new Vector2(leftMargin, startingY)) { Text = "CAMPAIGN", OnClick = () => {started = true; MediaPlayer.Play(_backgroundMusic);}},
    multiplayerButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + verticalSpacing)) { Text = "MULTIPLAYER" },
    optionsButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + (verticalSpacing * 2))) { Text = "OPTIONS" },
    exitButton = new MenuButtonSprite(new Vector2(leftMargin, startingY + (verticalSpacing * 3))) { Text = "EXIT(Press ESC)", OnClick = () => Exit()}
        };


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

            //Wall and floor bounds for Player
            player.position.X = MathHelper.Clamp(player.position.X, 0, 5120);
            player.position.Y = MathHelper.Clamp(player.position.Y, 0, 500);

            camera.Position = player.position;
            gameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
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
        GraphicsDevice.Clear(Color.Navy);
        Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);

        //Parallax movement and transformation


        if (started)
        {
            // 1. Camera Clamping
            // This locks the camera to the player, but forces the camera to stop scrolling
            // if the player goes further right than 5120.
            float cameraTargetX = MathHelper.Clamp(player.position.X, 300, 5120);
            
            // 2. Calculate the offset (using scale 1 to match the player's matrix)
            float offsetX = 300 - cameraTargetX; 

            // BATCH 1: Background (Stretched horizontally)
            Matrix bgTransform = Matrix.CreateScale(40, 5, 1) * Matrix.CreateTranslation(offsetX * 0.333f, 0, 0);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: bgTransform);
            spriteBatch.Draw(_background, Vector2.Zero, Color.White);
            spriteBatch.End();

            // BATCH 2: Midground (NOT stretched)
            Matrix mgTransform = Matrix.CreateScale(15, 15, 1) * Matrix.CreateTranslation(offsetX * 0.666f, 0, 0);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: mgTransform);
            spriteBatch.Draw(_midground, Vector2.Zero, Color.White);
            spriteBatch.End();

            // BATCH 3: Foreground (NOT stretched)
            Matrix fgTransform = Matrix.CreateScale(10, 10, 1) * Matrix.CreateTranslation(offsetX, 0, 0);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: fgTransform);
            spriteBatch.Draw(_foreground, Vector2.Zero, Color.White);
            spriteBatch.End();

            // BATCH 3.5: Player 
            Matrix playerTransform = Matrix.CreateScale(4, 4, 1) * Matrix.CreateTranslation(offsetX, 0, 0);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: playerTransform);
            player.Draw(gameTime, spriteBatch);
            spriteBatch.End();

            // BATCH 4: HUD
            spriteBatch.Begin();
            string timerText = $"Time: {gameTimer:F2}";
            string playerPosition = $"\nPlayer X: {player.position.X}\nPlayer Y: {player.position.Y}";
            spriteBatch.DrawString(
                titleFont,
                timerText,
                new Vector2(10, 10),
                Color.Red,
                0f,
                Vector2.Zero,
                0.5f,
                SpriteEffects.None,
                0f
            );
                        spriteBatch.DrawString(
                titleFont,
                playerPosition,
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
