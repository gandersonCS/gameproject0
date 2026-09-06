using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gameproject0;

public class GameProject0Game : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    
    PlanetSprite planet;
    private SpriteFont titleFont;
    private SpriteFont buttonFont;
    MenuButtonSprite campaignButton;
MenuButtonSprite multiplayerButton;
MenuButtonSprite optionsButton;

MenuButtonSprite exitButton;


    private MenuButtonSprite[] menuButtons;

    /// <summary>
    /// A game demonstrating collision detection
    /// </summary>
    public GameProject0Game()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    /// <summary>
    /// Initializes the game 
    /// </summary>
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        planet = new PlanetSprite();
        int leftMargin = 50;
        //space between button list and title
    int startingY = 150;
    //space between button
    int verticalSpacing = 75;
        menuButtons = new MenuButtonSprite[]
        {
campaignButton = new MenuButtonSprite(new Vector2(leftMargin, startingY)) { Text = "CAMPAIGN" },
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
        planet.LoadContent(Content);
        // TODO: use this.Content to load your game content here
        foreach (var but in menuButtons) but.LoadContent(Content);
        titleFont = Content.Load<SpriteFont>("aquire");
        buttonFont = Content.Load<SpriteFont>("robotheroes");
    }

    /// <summary>
    /// Updates the game world
    /// </summary>
    /// <param name="gameTime">The game time</param>
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape) /*Or clicking on the exit button*/)
            Exit();

        // TODO: Add your update logic here
        foreach(var but in menuButtons) but.Update(gameTime);
        planet.Update(gameTime, GraphicsDevice.Viewport.Width);
  
    

        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the game world
    /// </summary>
    /// <param name="gameTime">The game time</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Navy);

        // TODO: Add your drawing code here
        
        spriteBatch.Begin();
        planet.Draw(gameTime, spriteBatch);
        foreach (var but in menuButtons) but.Draw(gameTime, spriteBatch);
       
        spriteBatch.DrawString(titleFont, $"GAME TITLE", new Vector2(2,2), Color.Gold);
        
        spriteBatch.End();

        base.Draw(gameTime);
    }
}
