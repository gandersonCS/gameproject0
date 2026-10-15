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

        // 1. Keep player inside the physical level bounds
        player.position.X = MathHelper.Clamp(player.position.X, 0, 5120);

        // 2. Lock the camera to the player, but fix the Y position so the screen doesn't bounce
        camera.Position = new Vector2(player.position.X, 100f);

        // 3. Clamp the camera so it stops scrolling near the edges!
        // 300f is the left screen boundary, 4800f is the right screen boundary.
        camera.Position = new Vector2(
            MathHelper.Clamp(camera.Position.X, 300f, 4800f),
            camera.Position.Y
        );

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
        GraphicsDevice.Clear(Color.DarkRed);
        Vector2 screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);

        //Parallax movement and transformation


        if (started)
        {
        camera.Zoom = 5.0f; 

    // We need the minimum camera X again to align the parallax layers perfectly to the left edge
    float minCameraX = (GraphicsDevice.Viewport.Width / 2f) / camera.Zoom;

    // BATCH 1: Background (10% speed)
    // Calculate parallax: start at minCameraX, and only apply the 0.1f speed to movement beyond that point
    float bgCamX = minCameraX + ((camera.Position.X - minCameraX) * 0.1f);
    
    Matrix bgMatrix = Matrix.CreateTranslation(new Vector3(-bgCamX, -camera.Position.Y, 0)) *
                      Matrix.CreateScale(camera.Zoom) *
                      Matrix.CreateTranslation(new Vector3(screenCenter.X, screenCenter.Y, 0));
    
    spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: bgMatrix);
    spriteBatch.Draw(_background, new Rectangle(0, 0, 5120, _background.Height), Color.Salmon);
    spriteBatch.End();

    // BATCH 2: Midground (50% speed)
    float mgCamX = minCameraX + ((camera.Position.X - minCameraX) * 0.5f);
    
    Matrix mgMatrix = Matrix.CreateTranslation(new Vector3(-mgCamX, -camera.Position.Y, 0)) *
                      Matrix.CreateScale(camera.Zoom) *
                      Matrix.CreateTranslation(new Vector3(screenCenter.X, screenCenter.Y, 0));
                      
    spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: mgMatrix);
    spriteBatch.Draw(_midground, new Rectangle(0, 0, 5120, _midground.Height), Color.DarkRed);
    spriteBatch.End();

    // BATCH 3: Foreground and Player (100% speed - uses the normal camera matrix)
    spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camera.GetViewMatrix(screenCenter));
    spriteBatch.Draw(_foreground, new Rectangle(0, 0, 5120, _foreground.Height), Color.Red);
    player.Draw(gameTime, spriteBatch);
    spriteBatch.End();

    // BATCH 4: HUD
    spriteBatch.Begin();
    string timerText = $"Time: {gameTimer:F2}\nPlayer X: {player.position.X}\nPlayer Y: {player.position.Y}";
    spriteBatch.DrawString(titleFont, timerText, new Vector2(10, 10), Color.Red, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    spriteBatch.End();}
    
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
